using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Game;

namespace TileForge.UI;

public class DialogueEditor
{
    // Completion
    public bool IsComplete { get; private set; }
    public bool WasCancelled { get; private set; }
    public DialogueData Result { get; private set; }
    public bool IsNew { get; private set; }
    public string OriginalId { get; private set; }

    // Top-level field
    private TextInputField _idField;

    // Nodes
    private readonly List<NodeRow> _nodes = new();

    // Focus management
    private TextInputField _activeField;

    // Scroll + tooltip
    private readonly ScrollPanel _scrollPanel = new();
    private readonly TooltipManager _tooltipManager = new(delaySeconds: 0.4);

    // Layout constants
    private const int Padding = LayoutConstants.FormPadding;
    private const int FieldHeight = LayoutConstants.DialogueEditorFieldHeight;
    private const int RowHeight = LayoutConstants.DialogueEditorRowHeight;
    private const int LabelWidth = LayoutConstants.DialogueEditorLabelWidth;
    private const int DefaultMaxWidth = LayoutConstants.DialogueEditorMaxWidth;
    private const int DefaultMaxHeight = LayoutConstants.DialogueEditorMaxHeight;

    // Resize state
    private int? _userWidth;
    private int? _userHeight;
    private bool _resizing;
    private ResizeEdge _resizeEdge;
    private Point _resizeDragStart;
    private int _resizeDragStartW;
    private int _resizeDragStartH;

    // Colors
    private static readonly Color Overlay = LayoutConstants.DialogueEditorOverlay;
    private static readonly Color PanelBg = LayoutConstants.DialogueEditorPanelBg;
    private static readonly Color HeaderBg = LayoutConstants.DialogueEditorHeaderBg;
    private static readonly Color LabelColor = LayoutConstants.DialogueEditorLabelColor;
    private static readonly Color HintColor = LayoutConstants.DialogueEditorHintColor;
    private static readonly Color SectionColor = LayoutConstants.DialogueEditorSectionColor;
    private static readonly Color NodeSectionColor = LayoutConstants.DialogueEditorNodeSectionColor;
    private static readonly Color AddBtnBg = LayoutConstants.DialogueEditorAddButtonBg;
    private static readonly Color AddBtnHoverBg = LayoutConstants.DialogueEditorAddButtonHoverBg;
    private static readonly Color RemoveColor = LayoutConstants.DialogueEditorRemoveColor;
    private static readonly Color RemoveHoverColor = LayoutConstants.DialogueEditorRemoveHoverColor;
    private static readonly Color SeparatorColor = LayoutConstants.DialogueEditorNodeSeparator;

    // Hit-test rectangles (computed during Draw)
    private readonly List<Rectangle> _nodeRemoveRects = new();
    private readonly List<Rectangle> _addChoiceRects = new();
    private readonly List<List<Rectangle>> _choiceRemoveRects = new();
    private Rectangle _addNodeRect;
    private Rectangle _panelRect;

    // Field rectangle cache (built during Draw, used during next Update)
    private readonly List<Rectangle> _fieldRects = new();

    // Tooltip: track which field rects + fields map for overflow detection
    private readonly List<(Rectangle Rect, TextInputField Field)> _tooltipFields = new();

    private enum ResizeEdge { None, Right, Bottom, BottomRight }

    private DialogueEditor() { }

    public static DialogueEditor ForNewDialogue()
    {
        var editor = new DialogueEditor
        {
            IsNew = true,
            _idField = new TextInputField("", maxLength: 64),
        };
        editor.FocusField(editor._idField);
        return editor;
    }

    public static DialogueEditor ForExistingDialogue(DialogueData dialogue)
    {
        var editor = new DialogueEditor
        {
            IsNew = false,
            OriginalId = dialogue.Id,
            _idField = new TextInputField(dialogue.Id ?? "", maxLength: 64),
        };

        foreach (var node in dialogue.Nodes)
        {
            var row = new NodeRow
            {
                IdField = new TextInputField(node.Id ?? "", maxLength: 64),
                SpeakerField = new TextInputField(node.Speaker ?? "", maxLength: 128),
                TextField = new TextInputField(node.Text ?? "", maxLength: 512),
                NextNodeIdField = new TextInputField(node.NextNodeId ?? "", maxLength: 64),
                RequiresFlagField = new TextInputField(node.RequiresFlag ?? "", maxLength: 128),
                SetsFlagField = new TextInputField(node.SetsFlag ?? "", maxLength: 128),
                SetsVariableField = new TextInputField(node.SetsVariable ?? "", maxLength: 128),
            };

            if (node.Choices != null)
            {
                foreach (var choice in node.Choices)
                {
                    row.Choices.Add(new ChoiceRow
                    {
                        TextField = new TextInputField(choice.Text ?? "", maxLength: 256),
                        NextNodeIdField = new TextInputField(choice.NextNodeId ?? "", maxLength: 64),
                        RequiresFlagField = new TextInputField(choice.RequiresFlag ?? "", maxLength: 128),
                        SetsFlagField = new TextInputField(choice.SetsFlag ?? "", maxLength: 128),
                    });
                }
            }

            editor._nodes.Add(row);
        }

        editor.FocusField(null);
        return editor;
    }

    public void OnTextInput(char character)
    {
        _activeField?.HandleCharacter(character);
    }

    public void Update(MouseState mouse, MouseState prevMouse,
                       KeyboardState keyboard, KeyboardState prevKeyboard,
                       Rectangle bounds, List<DialogueData> existingDialogues,
                       SpriteFont font = null, int screenW = 0, int screenH = 0)
    {
        if (KeyPressed(keyboard, prevKeyboard, Keys.Escape))
        {
            IsComplete = true;
            WasCancelled = true;
            return;
        }

        if (KeyPressed(keyboard, prevKeyboard, Keys.Tab))
            CycleFocus();

        if (KeyPressed(keyboard, prevKeyboard, Keys.Enter))
        {
            TryConfirm(existingDialogues);
            return;
        }

        // Route keys to active field
        if (_activeField != null)
        {
            foreach (var key in new[] { Keys.Back, Keys.Delete, Keys.Left, Keys.Right, Keys.Home, Keys.End })
            {
                if (KeyPressed(keyboard, prevKeyboard, key))
                    _activeField.HandleKey(key);
            }
        }

        // Compute panel rect for hit-testing
        int maxW = _userWidth ?? DefaultMaxWidth;
        int maxH = _userHeight ?? DefaultMaxHeight;
        int panelW = Math.Min(maxW, bounds.Width - 40);
        int panelH = Math.Min(maxH, bounds.Height - 40);
        int px = bounds.X + (bounds.Width - panelW) / 2;
        int py = bounds.Y + (bounds.Height - panelH) / 2;
        _panelRect = new Rectangle(px, py, panelW, panelH);

        // Handle resize
        HandleResize(mouse, prevMouse, bounds);

        // Scroll panel update
        int contentTop = py + 30;
        int contentBottom = py + panelH - 26;
        var contentViewport = new Rectangle(px, contentTop, panelW, contentBottom - contentTop);
        _scrollPanel.UpdateScroll(mouse, prevMouse, contentViewport);

        // Tooltip update
        _tooltipManager.Update(mouse.X != prevMouse.X || mouse.Y != prevMouse.Y ? 0 : 1.0 / 60.0);

        // Mouse handling
        bool leftClick = mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released;

        if (leftClick && !_resizing)
        {
            bool clickHandled = false;

            // Check node remove buttons
            for (int i = 0; i < _nodeRemoveRects.Count && i < _nodes.Count; i++)
            {
                if (_nodeRemoveRects[i].Contains(mouse.X, mouse.Y))
                {
                    RemoveNode(i);
                    clickHandled = true;
                    break;
                }
            }

            // Check choice remove buttons
            if (!clickHandled)
            {
                for (int n = 0; n < _choiceRemoveRects.Count && n < _nodes.Count; n++)
                {
                    for (int c = 0; c < _choiceRemoveRects[n].Count && c < _nodes[n].Choices.Count; c++)
                    {
                        if (_choiceRemoveRects[n][c].Contains(mouse.X, mouse.Y))
                        {
                            RemoveChoice(n, c);
                            clickHandled = true;
                            break;
                        }
                    }
                    if (clickHandled) break;
                }
            }

            // Check add choice buttons
            if (!clickHandled)
            {
                for (int n = 0; n < _addChoiceRects.Count && n < _nodes.Count; n++)
                {
                    if (_addChoiceRects[n].Contains(mouse.X, mouse.Y))
                    {
                        AddChoice(n);
                        clickHandled = true;
                        break;
                    }
                }
            }

            // Check add node button
            if (!clickHandled && _addNodeRect.Contains(mouse.X, mouse.Y))
            {
                AddNode();
                clickHandled = true;
            }

            // Check text field clicks
            if (!clickHandled)
            {
                var allFields = GetAllFields();
                var allRects = _fieldRects;
                bool fieldClicked = false;
                for (int i = 0; i < allRects.Count && i < allFields.Count; i++)
                {
                    if (allRects[i].Contains(mouse.X, mouse.Y))
                    {
                        FocusField(allFields[i]);
                        fieldClicked = true;
                        break;
                    }
                }
                if (!fieldClicked && _panelRect.Contains(mouse.X, mouse.Y))
                    FocusField(null);
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                     Rectangle bounds, GameTime gameTime)
    {
        _fieldRects.Clear();
        _nodeRemoveRects.Clear();
        _addChoiceRects.Clear();
        _choiceRemoveRects.Clear();
        _tooltipFields.Clear();

        // Dim background
        renderer.DrawRect(spriteBatch, bounds, Overlay);

        // Compute panel rect (centered, respecting user resize)
        int maxW = _userWidth ?? DefaultMaxWidth;
        int maxH = _userHeight ?? DefaultMaxHeight;
        int panelW = Math.Min(maxW, bounds.Width - 40);
        int panelH = Math.Min(maxH, bounds.Height - 40);
        int px = bounds.X + (bounds.Width - panelW) / 2;
        int py = bounds.Y + (bounds.Height - panelH) / 2;
        _panelRect = new Rectangle(px, py, panelW, panelH);

        // Panel background
        renderer.DrawRect(spriteBatch, _panelRect, PanelBg);

        // Header
        var headerRect = new Rectangle(px, py, panelW, 30);
        renderer.DrawRect(spriteBatch, headerRect, HeaderBg);
        string title = IsNew ? "New Dialogue" : "Edit Dialogue";
        spriteBatch.DrawString(font, title, new Vector2(px + Padding, py + (30 - font.LineSpacing) / 2), Color.White);

        // Resize grip indicator (bottom-right corner)
        DrawResizeGrip(spriteBatch, renderer);

        // Content area
        int contentTop = py + 30;
        int contentBottom = py + panelH - 26;
        var contentViewport = new Rectangle(px, contentTop, panelW, contentBottom - contentTop);

        // Begin scrollable region
        int startY = _scrollPanel.BeginScroll(spriteBatch, contentViewport);

        // Create FormLayout for the content
        int contentPadX = px + Padding;
        int contentW = panelW - Padding * 2;
        var layout = new FormLayout(contentPadX, contentW, startY + Padding,
                                     labelWidth: LabelWidth, fieldHeight: FieldHeight, rowHeight: RowHeight);

        // Dialogue Id field
        var idRect = layout.DrawLabeledField(spriteBatch, font, renderer, "Id:", _idField, gameTime, LabelColor);
        _fieldRects.Add(idRect);
        _tooltipFields.Add((idRect, _idField));

        layout.Space(6);

        // Nodes section header + Add button
        int labelY = FormLayout.CenterTextY(layout.CursorY, FieldHeight, font.LineSpacing);
        spriteBatch.DrawString(font, "Nodes", new Vector2(contentPadX, labelY), SectionColor);

        int addBtnW = 60;
        int addBtnH = 18;
        _addNodeRect = new Rectangle(contentPadX + contentW - addBtnW, layout.CursorY, addBtnW, addBtnH);
        DrawButton(spriteBatch, font, renderer, _addNodeRect, "+ Add", AddBtnBg, AddBtnHoverBg);

        layout.CursorY += RowHeight;

        // Node rows
        for (int i = 0; i < _nodes.Count; i++)
        {
            var node = _nodes[i];
            int nodeIndent = 20;

            // Node header: number + Del button
            int hdrY = FormLayout.CenterTextY(layout.CursorY, FieldHeight, font.LineSpacing);
            spriteBatch.DrawString(font, $"Node {i + 1}", new Vector2(contentPadX, hdrY), NodeSectionColor);

            int removeBtnW = 50;
            var removeRect = new Rectangle(contentPadX + contentW - removeBtnW, layout.CursorY, removeBtnW, FieldHeight);
            _nodeRemoveRects.Add(removeRect);
            DrawButton(spriteBatch, font, renderer, removeRect, "Del", RemoveColor, RemoveHoverColor);

            layout.CursorY += RowHeight;

            // Indented layout for node fields
            var nodeLayout = new FormLayout(contentPadX + nodeIndent, contentW - nodeIndent, layout.CursorY,
                                             labelWidth: LabelWidth, fieldHeight: FieldHeight, rowHeight: RowHeight);

            // Row 1: Id (full width)
            var nodeIdRect = nodeLayout.DrawLabeledField(spriteBatch, font, renderer, "Id:", node.IdField, gameTime, LabelColor);
            _fieldRects.Add(nodeIdRect);
            _tooltipFields.Add((nodeIdRect, node.IdField));

            // Row 2: Speaker (full width)
            var speakerRect = nodeLayout.DrawLabeledField(spriteBatch, font, renderer, "Speaker:", node.SpeakerField, gameTime, LabelColor);
            _fieldRects.Add(speakerRect);
            _tooltipFields.Add((speakerRect, node.SpeakerField));

            // Row 3: Text (full width)
            var textRect = nodeLayout.DrawLabeledField(spriteBatch, font, renderer, "Text:", node.TextField, gameTime, LabelColor);
            _fieldRects.Add(textRect);
            _tooltipFields.Add((textRect, node.TextField));

            // Row 4: Next (full width)
            var nextRect = nodeLayout.DrawLabeledField(spriteBatch, font, renderer, "Next:", node.NextNodeIdField, gameTime, LabelColor);
            _fieldRects.Add(nextRect);
            _tooltipFields.Add((nextRect, node.NextNodeIdField));

            // Row 5: Requires + Sets Flag (side by side, measured labels)
            var (reqRect, flagRect) = nodeLayout.DrawTwoFieldRow(spriteBatch, font, renderer,
                "Requires:", node.RequiresFlagField, "Sets Flag:", node.SetsFlagField,
                gameTime, LabelColor, LayoutConstants.FormTwoFieldGap);
            _fieldRects.Add(reqRect);
            _fieldRects.Add(flagRect);
            _tooltipFields.Add((reqRect, node.RequiresFlagField));
            _tooltipFields.Add((flagRect, node.SetsFlagField));

            // Row 6: Sets Var (full width)
            var varRect = nodeLayout.DrawLabeledField(spriteBatch, font, renderer, "Sets Var:", node.SetsVariableField, gameTime, LabelColor);
            _fieldRects.Add(varRect);
            _tooltipFields.Add((varRect, node.SetsVariableField));

            // Choices sub-section
            int choiceLabelY = FormLayout.CenterTextY(nodeLayout.CursorY, FieldHeight, font.LineSpacing);
            spriteBatch.DrawString(font, "Choices:", new Vector2(contentPadX + nodeIndent, choiceLabelY), LabelColor);

            int choiceAddW = 50;
            var addChoiceRect = new Rectangle(contentPadX + nodeIndent + 70, nodeLayout.CursorY, choiceAddW, 18);
            _addChoiceRects.Add(addChoiceRect);
            DrawButton(spriteBatch, font, renderer, addChoiceRect, "+ Add", AddBtnBg, AddBtnHoverBg);

            nodeLayout.CursorY += RowHeight;

            var nodeChoiceRects = new List<Rectangle>();

            int choiceIndent = nodeIndent + 16;
            for (int c = 0; c < node.Choices.Count; c++)
            {
                var choice = node.Choices[c];

                // Choice row 1: letter + Text (full width)
                char letter = (char)('a' + (c < 26 ? c : 25));
                int cLabelY = FormLayout.CenterTextY(nodeLayout.CursorY, FieldHeight, font.LineSpacing);
                spriteBatch.DrawString(font, $"{letter})", new Vector2(contentPadX + choiceIndent, cLabelY), LabelColor);

                int choiceFieldX = contentPadX + choiceIndent + 20;
                int choiceFieldW = contentW - choiceIndent - 20;
                var choiceTextRect = new Rectangle(choiceFieldX, nodeLayout.CursorY, choiceFieldW, FieldHeight);
                choice.TextField.Draw(spriteBatch, font, renderer, choiceTextRect, gameTime);
                _fieldRects.Add(choiceTextRect);
                _tooltipFields.Add((choiceTextRect, choice.TextField));

                nodeLayout.CursorY += RowHeight;

                // Choice row 2: Next + Requires (side by side, measured labels)
                var choiceLayout = new FormLayout(contentPadX + choiceIndent + 20, contentW - choiceIndent - 20, nodeLayout.CursorY,
                                                   labelWidth: LabelWidth, fieldHeight: FieldHeight, rowHeight: RowHeight);
                var (cNextRect, cReqRect) = choiceLayout.DrawTwoFieldRow(spriteBatch, font, renderer,
                    "Next:", choice.NextNodeIdField, "Requires:", choice.RequiresFlagField,
                    gameTime, LabelColor, LayoutConstants.FormTwoFieldGap);
                _fieldRects.Add(cNextRect);
                _fieldRects.Add(cReqRect);
                _tooltipFields.Add((cNextRect, choice.NextNodeIdField));
                _tooltipFields.Add((cReqRect, choice.RequiresFlagField));
                nodeLayout.CursorY = choiceLayout.CursorY;

                // Choice row 3: Sets Flag + Del button
                int flagLblY = FormLayout.CenterTextY(nodeLayout.CursorY, FieldHeight, font.LineSpacing);
                spriteBatch.DrawString(font, "Sets Flag:", new Vector2(contentPadX + choiceIndent + 20, flagLblY), LabelColor);
                int flagLblW = (int)font.MeasureString("Sets Flag:").X + 6;
                int cFlagX = contentPadX + choiceIndent + 20 + flagLblW;
                int cRemoveW = 36;
                int cFlagW = contentW - choiceIndent - 20 - flagLblW - cRemoveW - 8;
                var cFlagRect = new Rectangle(cFlagX, nodeLayout.CursorY, cFlagW, FieldHeight);
                choice.SetsFlagField.Draw(spriteBatch, font, renderer, cFlagRect, gameTime);
                _fieldRects.Add(cFlagRect);
                _tooltipFields.Add((cFlagRect, choice.SetsFlagField));

                var choiceRemoveRect = new Rectangle(contentPadX + contentW - cRemoveW, nodeLayout.CursorY, cRemoveW, FieldHeight);
                nodeChoiceRects.Add(choiceRemoveRect);
                DrawButton(spriteBatch, font, renderer, choiceRemoveRect, "Del", RemoveColor, RemoveHoverColor);

                nodeLayout.CursorY += RowHeight + 2;
            }

            _choiceRemoveRects.Add(nodeChoiceRects);

            // Node separator
            renderer.DrawRect(spriteBatch, new Rectangle(contentPadX, nodeLayout.CursorY, contentW, 1), SeparatorColor);
            nodeLayout.CursorY += 8;

            layout.CursorY = nodeLayout.CursorY;
        }

        // Calculate total content height and end scroll
        int totalContentH = layout.CursorY - (startY + Padding) + Padding;
        _scrollPanel.EndScroll(spriteBatch, renderer, totalContentH);

        // Hints (drawn outside scroll region)
        string hints = "[Enter] Save    [Esc] Cancel    [Tab] Next Field";
        var hintSize = font.MeasureString(hints);
        int hintY = py + panelH - 22;
        spriteBatch.DrawString(font, hints, new Vector2(px + panelW - hintSize.X - Padding, hintY), HintColor);

        // Panel border
        renderer.DrawRectOutline(spriteBatch, _panelRect, new Color(80, 80, 80), 1);

        // Overflow tooltip (drawn last, on top of everything)
        UpdateTooltipHover(font);
        _tooltipManager.Draw(spriteBatch, font, renderer, bounds.Width);
    }

    private void UpdateTooltipHover(SpriteFont font)
    {
        var ms = Mouse.GetState();
        bool foundOverflow = false;
        foreach (var (rect, field) in _tooltipFields)
        {
            if (rect.Contains(ms.X, ms.Y) && field.IsTextOverflowing(font, rect))
            {
                _tooltipManager.SetHover(field.Text, ms.X, ms.Y);
                foundOverflow = true;
                break;
            }
        }
        if (!foundOverflow)
            _tooltipManager.ClearHover();
    }

    private void HandleResize(MouseState mouse, MouseState prevMouse, Rectangle bounds)
    {
        bool leftDown = mouse.LeftButton == ButtonState.Pressed;
        bool leftClick = leftDown && prevMouse.LeftButton == ButtonState.Released;
        int grab = LayoutConstants.ModalEdgeGrabSize;

        if (_resizing)
        {
            if (!leftDown)
            {
                _resizing = false;
                return;
            }

            int dx = mouse.X - _resizeDragStart.X;
            int dy = mouse.Y - _resizeDragStart.Y;

            if (_resizeEdge == ResizeEdge.Right || _resizeEdge == ResizeEdge.BottomRight)
                _userWidth = Math.Clamp(_resizeDragStartW + dx * 2, LayoutConstants.ModalMinWidth, bounds.Width - 40);
            if (_resizeEdge == ResizeEdge.Bottom || _resizeEdge == ResizeEdge.BottomRight)
                _userHeight = Math.Clamp(_resizeDragStartH + dy * 2, LayoutConstants.ModalMinHeight, bounds.Height - 40);
            return;
        }

        if (leftClick)
        {
            var edge = DetectResizeEdge(mouse.X, mouse.Y, grab);
            if (edge != ResizeEdge.None)
            {
                _resizing = true;
                _resizeEdge = edge;
                _resizeDragStart = new Point(mouse.X, mouse.Y);
                _resizeDragStartW = _panelRect.Width;
                _resizeDragStartH = _panelRect.Height;
            }
        }
    }

    private ResizeEdge DetectResizeEdge(int mx, int my, int grab)
    {
        bool nearRight = mx >= _panelRect.Right - grab && mx <= _panelRect.Right + grab &&
                         my >= _panelRect.Y && my <= _panelRect.Bottom;
        bool nearBottom = my >= _panelRect.Bottom - grab && my <= _panelRect.Bottom + grab &&
                          mx >= _panelRect.X && mx <= _panelRect.Right;

        if (nearRight && nearBottom) return ResizeEdge.BottomRight;
        if (nearRight) return ResizeEdge.Right;
        if (nearBottom) return ResizeEdge.Bottom;
        return ResizeEdge.None;
    }

    private void DrawResizeGrip(SpriteBatch spriteBatch, Renderer renderer)
    {
        // Small diagonal lines in bottom-right corner
        int gx = _panelRect.Right - 12;
        int gy = _panelRect.Bottom - 12;
        var gripColor = new Color(80, 80, 80);
        renderer.DrawRect(spriteBatch, new Rectangle(gx + 8, gy + 8, 2, 2), gripColor);
        renderer.DrawRect(spriteBatch, new Rectangle(gx + 4, gy + 8, 2, 2), gripColor);
        renderer.DrawRect(spriteBatch, new Rectangle(gx + 8, gy + 4, 2, 2), gripColor);
        renderer.DrawRect(spriteBatch, new Rectangle(gx, gy + 8, 2, 2), gripColor);
        renderer.DrawRect(spriteBatch, new Rectangle(gx + 4, gy + 4, 2, 2), gripColor);
        renderer.DrawRect(spriteBatch, new Rectangle(gx + 8, gy, 2, 2), gripColor);
    }

    private static void DrawButton(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                                    Rectangle rect, string label, Color bg, Color hoverBg)
    {
        var ms = Mouse.GetState();
        bool hovered = rect.Contains(ms.X, ms.Y);
        renderer.DrawRect(spriteBatch, rect, hovered ? hoverBg : bg);
        var size = font.MeasureString(label);
        spriteBatch.DrawString(font, label,
            new Vector2(rect.X + (rect.Width - size.X) / 2, rect.Y + (rect.Height - size.Y) / 2), Color.White);
    }

    private void TryConfirm(List<DialogueData> existingDialogues)
    {
        string id = _idField.Text.Trim();
        if (string.IsNullOrEmpty(id)) return;

        if (existingDialogues != null)
        {
            foreach (var existing in existingDialogues)
            {
                if (existing.Id == id && (IsNew || id != OriginalId))
                    return;
            }
        }

        Result = BuildResult();
        IsComplete = true;
        WasCancelled = false;
    }

    private DialogueData BuildResult()
    {
        var dialogue = new DialogueData
        {
            Id = _idField.Text.Trim(),
            Nodes = new List<DialogueNode>(),
        };

        foreach (var row in _nodes)
        {
            var node = new DialogueNode
            {
                Id = NullIfEmpty(row.IdField.Text),
                Speaker = NullIfEmpty(row.SpeakerField.Text),
                Text = NullIfEmpty(row.TextField.Text),
                NextNodeId = NullIfEmpty(row.NextNodeIdField.Text),
                RequiresFlag = NullIfEmpty(row.RequiresFlagField.Text),
                SetsFlag = NullIfEmpty(row.SetsFlagField.Text),
                SetsVariable = NullIfEmpty(row.SetsVariableField.Text),
            };

            if (row.Choices.Count > 0)
            {
                node.Choices = new List<DialogueChoice>();
                foreach (var cr in row.Choices)
                {
                    node.Choices.Add(new DialogueChoice
                    {
                        Text = NullIfEmpty(cr.TextField.Text),
                        NextNodeId = NullIfEmpty(cr.NextNodeIdField.Text),
                        RequiresFlag = NullIfEmpty(cr.RequiresFlagField.Text),
                        SetsFlag = NullIfEmpty(cr.SetsFlagField.Text),
                    });
                }
            }

            dialogue.Nodes.Add(node);
        }

        return dialogue;
    }

    private void AddNode()
    {
        _nodes.Add(new NodeRow
        {
            IdField = new TextInputField("", maxLength: 64),
            SpeakerField = new TextInputField("", maxLength: 128),
            TextField = new TextInputField("", maxLength: 512),
            NextNodeIdField = new TextInputField("", maxLength: 64),
            RequiresFlagField = new TextInputField("", maxLength: 128),
            SetsFlagField = new TextInputField("", maxLength: 128),
            SetsVariableField = new TextInputField("", maxLength: 128),
        });
    }

    private void RemoveNode(int index)
    {
        if (index >= 0 && index < _nodes.Count)
        {
            ClearFocusIfInNode(_nodes[index]);
            _nodes.RemoveAt(index);
        }
    }

    private void AddChoice(int nodeIndex)
    {
        if (nodeIndex >= 0 && nodeIndex < _nodes.Count)
        {
            _nodes[nodeIndex].Choices.Add(new ChoiceRow
            {
                TextField = new TextInputField("", maxLength: 256),
                NextNodeIdField = new TextInputField("", maxLength: 64),
                RequiresFlagField = new TextInputField("", maxLength: 128),
                SetsFlagField = new TextInputField("", maxLength: 128),
            });
        }
    }

    private void RemoveChoice(int nodeIndex, int choiceIndex)
    {
        if (nodeIndex >= 0 && nodeIndex < _nodes.Count)
        {
            var node = _nodes[nodeIndex];
            if (choiceIndex >= 0 && choiceIndex < node.Choices.Count)
            {
                var choice = node.Choices[choiceIndex];
                if (_activeField == choice.TextField ||
                    _activeField == choice.NextNodeIdField ||
                    _activeField == choice.RequiresFlagField ||
                    _activeField == choice.SetsFlagField)
                    FocusField(null);
                node.Choices.RemoveAt(choiceIndex);
            }
        }
    }

    private void ClearFocusIfInNode(NodeRow node)
    {
        if (_activeField == node.IdField ||
            _activeField == node.SpeakerField ||
            _activeField == node.TextField ||
            _activeField == node.NextNodeIdField ||
            _activeField == node.RequiresFlagField ||
            _activeField == node.SetsFlagField ||
            _activeField == node.SetsVariableField)
        {
            FocusField(null);
            return;
        }

        foreach (var choice in node.Choices)
        {
            if (_activeField == choice.TextField ||
                _activeField == choice.NextNodeIdField ||
                _activeField == choice.RequiresFlagField ||
                _activeField == choice.SetsFlagField)
            {
                FocusField(null);
                return;
            }
        }
    }

    private List<TextInputField> GetAllFields()
    {
        var fields = new List<TextInputField> { _idField };
        foreach (var node in _nodes)
        {
            fields.Add(node.IdField);
            fields.Add(node.SpeakerField);
            fields.Add(node.TextField);
            fields.Add(node.NextNodeIdField);
            fields.Add(node.RequiresFlagField);
            fields.Add(node.SetsFlagField);
            fields.Add(node.SetsVariableField);
            foreach (var choice in node.Choices)
            {
                fields.Add(choice.TextField);
                fields.Add(choice.NextNodeIdField);
                fields.Add(choice.RequiresFlagField);
                fields.Add(choice.SetsFlagField);
            }
        }
        return fields;
    }

    private void CycleFocus()
    {
        var allFields = GetAllFields();
        if (_activeField == null)
        {
            FocusField(allFields.Count > 0 ? allFields[0] : null);
            return;
        }
        int idx = allFields.IndexOf(_activeField);
        if (idx < 0 || idx >= allFields.Count - 1)
            FocusField(null);
        else
            FocusField(allFields[idx + 1]);
    }

    private void FocusField(TextInputField field)
    {
        if (_activeField != null) _activeField.IsFocused = false;
        _activeField = field;
        if (_activeField != null) _activeField.IsFocused = true;
    }

    private static string NullIfEmpty(string text)
    {
        string trimmed = text?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static bool KeyPressed(KeyboardState current, KeyboardState prev, Keys key) =>
        current.IsKeyDown(key) && prev.IsKeyUp(key);

    private class NodeRow
    {
        public TextInputField IdField;
        public TextInputField SpeakerField;
        public TextInputField TextField;
        public TextInputField NextNodeIdField;
        public TextInputField RequiresFlagField;
        public TextInputField SetsFlagField;
        public TextInputField SetsVariableField;
        public List<ChoiceRow> Choices = new();
    }

    private class ChoiceRow
    {
        public TextInputField TextField;
        public TextInputField NextNodeIdField;
        public TextInputField RequiresFlagField;
        public TextInputField SetsFlagField;
    }
}
