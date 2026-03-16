using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Data;
using TileForge.Game;

namespace TileForge.UI;

public class DialogueTreeEditor
{
    // === Completion ===
    public bool IsComplete { get; private set; }
    public bool WasCancelled { get; private set; }
    public DialogueData Result { get; private set; }
    public bool IsNew { get; private set; }
    public string OriginalId { get; private set; }

    // === Working data ===
    private readonly DialogueData _data;
    private readonly TextInputField _idField;

    // === Tree view model ===
    private readonly List<TreeItem> _treeRoots = new();
    private readonly List<TreeItem> _flatRows = new();  // Flattened visible rows
    private int _selectedRowIndex = -1;
    private string _selectedNodeId;
    private bool _treeDirty = true;

    // === Properties panel: node fields ===
    private readonly TextInputField _nodeIdField = new("", maxLength: 64);
    private readonly TextInputField _nodeSpeakerField = new("", maxLength: 128);
    private readonly TextInputField _nodeTextField = new("", maxLength: 512);
    private readonly TextInputField _nodeNextField = new("", maxLength: 64);

    // === Properties panel: v2 conditions + actions ===
    private readonly List<ConditionFields> _nodeConditionFields = new();
    private readonly List<ActionFields> _nodeActionFields = new();

    // === Properties panel: routes (shown when no node selected) ===
    private readonly List<RouteFields> _routeFields = new();
    private bool _routesPopulated;

    // === Properties panel: choice fields ===
    private readonly List<ChoiceFields> _choiceFields = new();

    // === Condition/Action type dropdowns ===
    private static readonly string[] ConditionTypes = { "has_flag", "not_flag", "has_item", "variable_eq", "variable_gte", "variable_lt", "quest_active", "quest_complete" };
    private static readonly string[] ActionTypes = { "set_flag", "set_variable", "increment", "give_item", "remove_item", "start_quest", "complete_objective", "heal", "damage", "log" };

    // === Properties panel: focus + scroll ===
    private object _activeField; // TextInputField or ComboBox
    private readonly ScrollPanel _propsScroll = new();
    private readonly ScrollPanel _treeScroll = new();
    private readonly TooltipManager _tooltipManager = new(delaySeconds: 0.4);

    // === Hit-test rects (computed in Draw, used in next Update) ===
    private readonly List<Rectangle> _choiceRemoveRects = new();
    private Rectangle _addChoiceRect;
    private readonly List<(Rectangle Rect, object Field)> _tooltipFields = new();
    private readonly List<(ComboBox CB, Rectangle Rect)> _pendingComboBoxes = new();
    private Rectangle _addConditionRect;
    private Rectangle _addActionRect;
    private readonly List<Rectangle> _conditionRemoveRects = new();
    private readonly List<Rectangle> _actionRemoveRects = new();
    private readonly List<Rectangle> _choiceAddConditionRects = new();
    private readonly List<Rectangle> _choiceAddActionRects = new();
    private readonly List<List<Rectangle>> _choiceConditionRemoveRects = new();
    private readonly List<List<Rectangle>> _choiceActionRemoveRects = new();
    private Rectangle _addRouteRect;
    private readonly List<Rectangle> _routeRemoveRects = new();
    private readonly List<Rectangle> _routeAddConditionRects = new();
    private readonly List<List<Rectangle>> _routeConditionRemoveRects = new();
    private readonly List<(Dropdown DD, Rectangle Rect)> _pendingDropdowns = new();

    // === Suggestion data (rebuilt each frame from context) ===
    private string[] _knownFlags = Array.Empty<string>();
    private string[] _knownVariables = Array.Empty<string>();
    private string[] _knownItems = Array.Empty<string>();
    private string[] _knownQuests = Array.Empty<string>();

    // Tree panel hit-test rects
    private readonly List<Rectangle> _treeRowRects = new();
    private readonly List<Rectangle> _treeExpandRects = new();
    private Rectangle _addNodeRect;
    private Rectangle _deleteNodeRect;

    // === Layout ===
    private ModalResizeHandler _resize;
    private Rectangle _panelRect;
    private Rectangle _treeRect;
    private Rectangle _propsRect;
    private SpriteFont _cachedFont;
    private Point _cachedMousePos;

    // === Constants ===
    private const int HeaderH = LayoutConstants.DialogueTreeHeaderHeight;
    private const int HintH = LayoutConstants.DialogueTreeHintHeight;
    private const int Padding = LayoutConstants.FormPadding;
    private const int FieldHeight = LayoutConstants.FormFieldHeight;
    private const int RowHeight = LayoutConstants.FormRowHeight;
    private const int LabelWidth = LayoutConstants.DialogueTreeLabelWidth;
    private const int DefaultMaxWidth = LayoutConstants.DialogueTreeMaxWidth;
    private const int DefaultMaxHeight = LayoutConstants.DialogueTreeMaxHeight;
    private const float CanvasSplit = LayoutConstants.DialogueTreeCanvasSplit;
    private const int TreeRowH = LayoutConstants.DialogueTreeRowHeight;
    private const int TreeIndent = LayoutConstants.DialogueTreeIndent;
    private const int TreeIconW = LayoutConstants.DialogueTreeIconWidth;

    // === Colors ===
    private static readonly Color Overlay = LayoutConstants.DialogueTreeOverlay;
    private static readonly Color PanelBg = LayoutConstants.DialogueTreePanelBg;
    private static readonly Color HeaderBg = LayoutConstants.DialogueTreeHeaderBg;
    private static readonly Color TreeBg = LayoutConstants.DialogueTreeCanvasBg;
    private static readonly Color DividerColor = LayoutConstants.DialogueTreeDividerColor;
    private static readonly Color HintColor = LayoutConstants.DialogueTreeHintColor;
    private static readonly Color PropsBg = LayoutConstants.DialogueTreePropsBg;
    private static readonly Color PropsSectionColor = LayoutConstants.DialogueTreePropsSectionColor;
    private static readonly Color LabelColor = LayoutConstants.DialogueTreeLabelColor;
    private static readonly Color RowHoverBg = LayoutConstants.DialogueTreeRowHoverBg;
    private static readonly Color RowSelectedBg = LayoutConstants.DialogueTreeRowSelectedBg;
    private static readonly Color SectionHeaderBg = LayoutConstants.DialogueTreeSectionHeaderBg;
    private static readonly Color SectionHeaderColor = LayoutConstants.DialogueTreeSectionHeaderColor;
    private static readonly Color NodeIdColor = LayoutConstants.DialogueTreeNodeIdColor;
    private static readonly Color NodeTextColor = LayoutConstants.DialogueTreeNodeTextColor;
    private static readonly Color ChoiceColor = LayoutConstants.DialogueTreeChoiceColor;
    private static readonly Color TreeLineColor = LayoutConstants.DialogueTreeLineColor;
    private static readonly Color OrphanColor = LayoutConstants.DialogueTreeOrphanColor;
    private static readonly Color CycleColor = LayoutConstants.DialogueTreeCycleColor;
    private static readonly Color ExpandColor = LayoutConstants.DialogueTreeExpandColor;
    private static readonly Color RouteColor = LayoutConstants.DialogueTreeRouteColor;
    private static readonly Color AddBtnBg = LayoutConstants.DialogueTreeAddButtonBg;
    private static readonly Color AddBtnHoverBg = LayoutConstants.DialogueTreeAddButtonHoverBg;
    private static readonly Color RemoveColor = LayoutConstants.DialogueTreeRemoveColor;
    private static readonly Color RemoveHoverColor = LayoutConstants.DialogueTreeRemoveHoverColor;

    private DialogueTreeEditor(DialogueData data)
    {
        _data = data;
        _idField = new TextInputField(data.Id ?? "", maxLength: 64);
    }

    // === Factory Methods ===

    public static DialogueTreeEditor ForNewDialogue()
    {
        var data = new DialogueData { Id = "", Nodes = new List<DialogueNode>() };
        var editor = new DialogueTreeEditor(data) { IsNew = true };
        editor.FocusField(editor._idField);
        return editor;
    }

    public static DialogueTreeEditor ForExistingDialogue(DialogueData dialogue)
    {
        var data = DeepCopy(dialogue);
        var editor = new DialogueTreeEditor(data)
        {
            IsNew = false,
            OriginalId = dialogue.Id,
        };
        editor.FocusField(null);
        return editor;
    }

    public void OnTextInput(char character)
    {
        switch (_activeField)
        {
            case TextInputField tif: tif.HandleCharacter(character); break;
            case ComboBox cb: cb.HandleCharacter(character); break;
        }
    }

    // === Update ===

    public void Update(MouseState mouse, MouseState prevMouse,
                       KeyboardState keyboard, KeyboardState prevKeyboard,
                       Rectangle bounds, List<DialogueData> existingDialogues,
                       SpriteFont font = null, int screenW = 0, int screenH = 0,
                       GameTime gameTime = null,
                       IProjectContext projectContext = null,
                       List<QuestDefinition> quests = null,
                       List<TileGroup> groups = null)
    {
        _cachedMousePos = new Point(mouse.X, mouse.Y);
        if (font != null) _cachedFont = font;

        // Update cursor blink for all text fields
        if (gameTime != null)
        {
            _idField.Update(gameTime);
            _nodeIdField.Update(gameTime);
            _nodeSpeakerField.Update(gameTime);
            _nodeTextField.Update(gameTime);
            _nodeNextField.Update(gameTime);
            foreach (var cf in _nodeConditionFields)
                cf.ValueCombo.Update(gameTime);
            foreach (var af in _nodeActionFields)
            {
                af.ValueCombo.Update(gameTime);
                af.KeyField.Update(gameTime);
            }
            foreach (var cf in _choiceFields)
            {
                cf.TextField.Update(gameTime);
                cf.NextField.Update(gameTime);
                foreach (var cond in cf.ConditionFields)
                    cond.ValueCombo.Update(gameTime);
                foreach (var act in cf.ActionFields)
                {
                    act.ValueCombo.Update(gameTime);
                    act.KeyField.Update(gameTime);
                }
            }
            foreach (var rf in _routeFields)
            {
                rf.StartNodeField.Update(gameTime);
                foreach (var cond in rf.ConditionFields)
                    cond.ValueCombo.Update(gameTime);
            }
        }

        // Update condition/action dropdowns
        foreach (var (dd, rect) in _pendingDropdowns)
            dd.Update(mouse, prevMouse, rect, _cachedFont ?? font, bounds.Width, bounds.Height);

        // Update ComboBox mouse interactions
        foreach (var (cb, rect) in _pendingComboBoxes)
            cb.UpdateMouse(mouse, prevMouse, rect, _cachedFont ?? font, screenW, screenH);

        // Rebuild suggestion lists from context
        RebuildSuggestions(projectContext, quests, groups);

        // Populate route fields once
        if (!_routesPopulated)
        {
            PopulateRouteFields();
            _routesPopulated = true;
        }

        // Flush properties -> data
        FlushSelectedNode();
        FlushRoutes();

        // Rebuild tree if data changed
        if (_treeDirty)
        {
            RebuildTree();
            _treeDirty = false;
        }

        // Compute panel layout
        _panelRect = _resize.ComputePanelRect(DefaultMaxWidth, DefaultMaxHeight, bounds);
        _resize.HandleResize(mouse, prevMouse, bounds);
        ComputeSubRects();

        // Keyboard shortcuts
        if (KeyPressed(keyboard, prevKeyboard, Keys.Escape))
        {
            IsComplete = true;
            WasCancelled = true;
            return;
        }

        if (KeyPressed(keyboard, prevKeyboard, Keys.Enter) && _activeField == null)
        {
            TryConfirm(existingDialogues);
            return;
        }

        if (KeyPressed(keyboard, prevKeyboard, Keys.Tab))
            CycleFocus();

        // Route keys to active field
        if (_activeField != null)
        {
            foreach (var key in new[] { Keys.Back, Keys.Delete, Keys.Left, Keys.Right, Keys.Home, Keys.End })
            {
                if (KeyPressed(keyboard, prevKeyboard, key))
                {
                    switch (_activeField)
                    {
                        case TextInputField tif: tif.HandleKey(key); break;
                        case ComboBox cb: cb.HandleKey(key); break;
                    }
                }
            }
        }

        // Arrow key navigation in tree (only when no text field is focused)
        if (_activeField == null)
        {
            if (KeyPressed(keyboard, prevKeyboard, Keys.Up) && _selectedRowIndex > 0)
                SelectRow(_selectedRowIndex - 1);
            else if (KeyPressed(keyboard, prevKeyboard, Keys.Down) && _selectedRowIndex < _flatRows.Count - 1)
                SelectRow(_selectedRowIndex + 1);
            else if (KeyPressed(keyboard, prevKeyboard, Keys.Left) && _selectedRowIndex >= 0)
            {
                var item = _flatRows[_selectedRowIndex];
                if (item.HasChildren && item.Expanded)
                {
                    item.Expanded = false;
                    FlattenTree();
                }
            }
            else if (KeyPressed(keyboard, prevKeyboard, Keys.Right) && _selectedRowIndex >= 0)
            {
                var item = _flatRows[_selectedRowIndex];
                if (item.HasChildren && !item.Expanded)
                {
                    item.Expanded = true;
                    FlattenTree();
                }
            }
        }

        // Tooltip
        bool mouseMoved = mouse.X != prevMouse.X || mouse.Y != prevMouse.Y;
        _tooltipManager.Update(mouseMoved ? 0 : 1.0 / 60.0);

        // Scroll
        _treeScroll.UpdateScroll(mouse, prevMouse, _treeRect);
        if (_selectedNodeId != null || _routeFields.Count > 0)
            _propsScroll.UpdateScroll(mouse, prevMouse, _propsRect);

        // Handle clicks
        bool leftClick = mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released;

        if (leftClick && !_resize.IsResizing)
        {
            // Tree panel clicks
            if (_treeRect.Contains(mouse.X, mouse.Y))
            {
                HandleTreePanelClick(mouse);
            }
            // Properties panel clicks
            else if (_propsRect.Contains(mouse.X, mouse.Y))
            {
                if (_selectedNodeId != null)
                    HandlePropertiesPanelClick(mouse);
                else
                    HandleRoutesPanelClick(mouse);
            }
        }

        // Tooltip hover
        if (_cachedFont != null)
            UpdateTooltipHover(_cachedFont, mouse);
    }

    // === Tree Building ===

    private void RebuildTree()
    {
        _treeRoots.Clear();

        // Build node lookup
        var nodeById = new Dictionary<string, DialogueNode>();
        foreach (var node in _data.Nodes)
        {
            if (!string.IsNullOrEmpty(node.Id))
                nodeById[node.Id] = node;
        }

        // Determine route start nodes
        var routeStartIds = new List<string>();
        if (_data.Routes != null && _data.Routes.Count > 0)
        {
            foreach (var route in _data.Routes)
            {
                if (!string.IsNullOrEmpty(route.StartNode))
                    routeStartIds.Add(route.StartNode);
            }
        }
        else if (_data.Nodes.Count > 0 && !string.IsNullOrEmpty(_data.Nodes[0].Id))
        {
            routeStartIds.Add(_data.Nodes[0].Id);
        }

        // Track visited nodes across all trees
        var visited = new HashSet<string>();

        // Build a tree for each route
        for (int r = 0; r < routeStartIds.Count; r++)
        {
            string startId = routeStartIds[r];
            string condSummary = GetRouteConditionSummary(r);

            // Section header for route
            var routeHeader = new TreeItem
            {
                Type = TreeItemType.RouteHeader,
                Label = condSummary != null
                    ? $"Route {r + 1}: {startId} [{condSummary}]"
                    : $"Route {r + 1}: {startId} (default)",
                Depth = 0,
                Expanded = true,
            };

            if (nodeById.TryGetValue(startId, out var startNode))
            {
                var walkVisited = new HashSet<string>();
                BuildSubtree(routeHeader, startNode, nodeById, visited, walkVisited, 1);
            }

            _treeRoots.Add(routeHeader);
        }

        // Orphan nodes
        var orphans = new List<DialogueNode>();
        foreach (var node in _data.Nodes)
        {
            if (!string.IsNullOrEmpty(node.Id) && !visited.Contains(node.Id))
                orphans.Add(node);
        }

        if (orphans.Count > 0)
        {
            var orphanHeader = new TreeItem
            {
                Type = TreeItemType.SectionHeader,
                Label = $"Orphans ({orphans.Count})",
                Depth = 0,
                Expanded = true,
            };

            foreach (var orphan in orphans)
            {
                var item = new TreeItem
                {
                    Type = TreeItemType.Node,
                    NodeId = orphan.Id,
                    Node = orphan,
                    Label = FormatNodeLabel(orphan),
                    Depth = 1,
                    IsOrphan = true,
                    Expanded = true,
                };
                // Don't recurse orphans deeply — just show them flat
                orphanHeader.Children.Add(item);
                visited.Add(orphan.Id);
            }

            _treeRoots.Add(orphanHeader);
        }

        FlattenTree();

        // Restore selection
        if (_selectedNodeId != null)
        {
            _selectedRowIndex = -1;
            for (int i = 0; i < _flatRows.Count; i++)
            {
                if (_flatRows[i].NodeId == _selectedNodeId)
                {
                    _selectedRowIndex = i;
                    break;
                }
            }
        }
    }

    private void BuildSubtree(TreeItem parent, DialogueNode node,
        Dictionary<string, DialogueNode> nodeById,
        HashSet<string> globalVisited, HashSet<string> walkVisited, int depth)
    {
        if (node == null || string.IsNullOrEmpty(node.Id)) return;

        if (walkVisited.Contains(node.Id))
        {
            // Cycle detected
            parent.Children.Add(new TreeItem
            {
                Type = TreeItemType.CycleRef,
                Label = $"-> {node.Id} (cycle)",
                NodeId = node.Id,
                Node = node,
                Depth = depth,
            });
            return;
        }

        globalVisited.Add(node.Id);
        walkVisited.Add(node.Id);

        var item = new TreeItem
        {
            Type = TreeItemType.Node,
            NodeId = node.Id,
            Node = node,
            Label = FormatNodeLabel(node),
            Depth = depth,
            Expanded = true,
        };

        // Add text preview as child
        if (!string.IsNullOrEmpty(node.Text))
        {
            string preview = TruncateText(node.Text, 50);
            item.Children.Add(new TreeItem
            {
                Type = TreeItemType.TextPreview,
                Label = $"\"{preview}\"",
                NodeId = node.Id,
                Node = node,
                Depth = depth + 1,
            });
        }

        // Follow children
        bool hasChoices = node.Choices != null && node.Choices.Count > 0;
        if (hasChoices)
        {
            for (int c = 0; c < node.Choices.Count; c++)
            {
                var choice = node.Choices[c];
                string choiceLabel = TruncateText(choice.Text, 40) ?? "...";
                var choiceItem = new TreeItem
                {
                    Type = TreeItemType.Choice,
                    Label = $"[{c + 1}] {choiceLabel}",
                    NodeId = node.Id,
                    Node = node,
                    ChoiceIndex = c,
                    Depth = depth + 1,
                    Expanded = true,
                };

                if (!string.IsNullOrEmpty(choice.NextNodeId) && nodeById.TryGetValue(choice.NextNodeId, out var choiceTarget))
                {
                    BuildSubtree(choiceItem, choiceTarget, nodeById, globalVisited, walkVisited, depth + 2);
                }
                else if (!string.IsNullOrEmpty(choice.NextNodeId))
                {
                    choiceItem.Children.Add(new TreeItem
                    {
                        Type = TreeItemType.MissingRef,
                        Label = $"-> {choice.NextNodeId} (missing)",
                        Depth = depth + 2,
                    });
                }

                item.Children.Add(choiceItem);
            }
        }
        else if (!string.IsNullOrEmpty(node.NextNodeId))
        {
            if (nodeById.TryGetValue(node.NextNodeId, out var nextNode))
                BuildSubtree(item, nextNode, nodeById, globalVisited, walkVisited, depth + 1);
            else
            {
                item.Children.Add(new TreeItem
                {
                    Type = TreeItemType.MissingRef,
                    Label = $"-> {node.NextNodeId} (missing)",
                    Depth = depth + 1,
                });
            }
        }

        walkVisited.Remove(node.Id);
        parent.Children.Add(item);
    }

    private void FlattenTree()
    {
        _flatRows.Clear();
        foreach (var root in _treeRoots)
            FlattenItem(root);
    }

    private void FlattenItem(TreeItem item)
    {
        _flatRows.Add(item);
        if (item.Expanded)
        {
            foreach (var child in item.Children)
                FlattenItem(child);
        }
    }

    private string GetRouteConditionSummary(int routeIndex)
    {
        if (_data.Routes == null || routeIndex >= _data.Routes.Count) return null;
        var route = _data.Routes[routeIndex];
        if (route.Conditions == null || route.Conditions.Count == 0) return null;

        var parts = new List<string>();
        foreach (var c in route.Conditions)
        {
            string val = c.Type switch
            {
                "has_flag" or "not_flag" => $"{c.Type}: {c.Flag}",
                "has_item" => $"has_item: {c.Item}",
                _ => c.Type ?? "?",
            };
            parts.Add(val);
        }
        return string.Join(", ", parts);
    }

    private static string FormatNodeLabel(DialogueNode node)
    {
        string speaker = node.Speaker;
        if (!string.IsNullOrEmpty(speaker))
            return $"{node.Id} ({speaker})";
        return node.Id ?? "???";
    }

    // === Draw ===

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                     Rectangle bounds, GameTime gameTime)
    {
        _cachedFont = font;

        // Recompute layout
        _panelRect = _resize.ComputePanelRect(DefaultMaxWidth, DefaultMaxHeight, bounds);
        ComputeSubRects();

        // Clear hit-test state
        _choiceRemoveRects.Clear();
        _tooltipFields.Clear();
        _addChoiceRect = Rectangle.Empty;
        _addRouteRect = Rectangle.Empty;
        _addNodeRect = Rectangle.Empty;
        _deleteNodeRect = Rectangle.Empty;
        _treeRowRects.Clear();
        _treeExpandRects.Clear();

        // Dim background
        renderer.DrawRect(spriteBatch, bounds, Overlay);

        // Panel background
        renderer.DrawRect(spriteBatch, _panelRect, PanelBg);

        // Header
        DrawHeader(spriteBatch, font, renderer, gameTime);

        // Tree panel
        DrawTreePanel(spriteBatch, font, renderer);

        // Divider
        int divX = _treeRect.Right;
        renderer.DrawRect(spriteBatch, new Rectangle(divX, _treeRect.Y, 1, _treeRect.Height), DividerColor);

        // Properties panel
        DrawPropertiesPanel(spriteBatch, font, renderer, gameTime);

        // Hints bar
        string hints = _activeField != null
            ? "[Enter] Save    [Esc] Cancel    [Tab] Next Field"
            : "[Enter] Save    [Esc] Cancel    [Up/Down] Navigate    [Left/Right] Collapse/Expand";
        var hintSize = font.MeasureString(hints);
        int hintY = _panelRect.Bottom - HintH;
        renderer.DrawRect(spriteBatch, new Rectangle(_panelRect.X, hintY, _panelRect.Width, HintH), HeaderBg);
        spriteBatch.DrawString(font, hints,
            new Vector2(_panelRect.Right - hintSize.X - Padding, hintY + (HintH - font.LineSpacing) / 2), HintColor);

        // Tooltips
        _tooltipManager.Draw(spriteBatch, font, renderer, bounds.Width);

        // Panel border + resize grip
        renderer.DrawRectOutline(spriteBatch, _panelRect, new Color(80, 80, 80), 1);
        _resize.DrawResizeGrip(spriteBatch, renderer);
    }

    // === Drawing Helpers ===

    private void DrawHeader(SpriteBatch sb, SpriteFont font, Renderer renderer, GameTime gt)
    {
        var headerRect = new Rectangle(_panelRect.X, _panelRect.Y, _panelRect.Width, HeaderH);
        renderer.DrawRect(sb, headerRect, HeaderBg);

        string title = IsNew ? "New Dialogue" : "Edit Dialogue";
        sb.DrawString(font, title, new Vector2(_panelRect.X + Padding, _panelRect.Y + (HeaderH - font.LineSpacing) / 2), Color.White);

        int titleW = (int)font.MeasureString(title).X + 12;
        int idLabelX = _panelRect.X + Padding + titleW;
        sb.DrawString(font, "Id:", new Vector2(idLabelX, _panelRect.Y + (HeaderH - font.LineSpacing) / 2), LabelColor);
        int idFieldX = idLabelX + (int)font.MeasureString("Id:").X + 6;
        int idFieldW = 180;
        var idRect = new Rectangle(idFieldX, _panelRect.Y + (HeaderH - FieldHeight) / 2, idFieldW, FieldHeight);
        _idField.Draw(sb, font, renderer, idRect, gt);
        _tooltipFields.Add((idRect, _idField));

        // Add Node / Delete Node buttons
        int btnY = _panelRect.Y + (HeaderH - 20) / 2;
        int btnX = idFieldX + idFieldW + 16;
        _addNodeRect = new Rectangle(btnX, btnY, 75, 20);
        DrawButton(sb, font, renderer, _addNodeRect, "+ Node", AddBtnBg, AddBtnHoverBg);

        if (_selectedNodeId != null)
        {
            _deleteNodeRect = new Rectangle(btnX + 80, btnY, 75, 20);
            DrawButton(sb, font, renderer, _deleteNodeRect, "Del Node", RemoveColor, RemoveHoverColor);
        }
    }

    private void DrawTreePanel(SpriteBatch sb, SpriteFont font, Renderer renderer)
    {
        renderer.DrawRect(sb, _treeRect, TreeBg);

        var scrollViewport = new Rectangle(_treeRect.X, _treeRect.Y, _treeRect.Width, _treeRect.Height);
        int startY = _treeScroll.BeginScroll(sb, scrollViewport);

        int y = startY;

        for (int i = 0; i < _flatRows.Count; i++)
        {
            var item = _flatRows[i];
            var rowRect = new Rectangle(_treeRect.X, y, _treeRect.Width - ScrollPanel.ScrollBarWidth, TreeRowH);
            _treeRowRects.Add(rowRect);

            bool isSelected = i == _selectedRowIndex;
            bool isHovered = rowRect.Contains(_cachedMousePos);

            // Row background
            if (isSelected)
                renderer.DrawRect(sb, rowRect, RowSelectedBg);
            else if (isHovered)
                renderer.DrawRect(sb, rowRect, RowHoverBg);

            int x = _treeRect.X + Padding + item.Depth * TreeIndent;
            int textY = y + (TreeRowH - font.LineSpacing) / 2;

            // Draw tree lines
            if (item.Depth > 0)
            {
                int lineX = _treeRect.X + Padding + (item.Depth - 1) * TreeIndent + TreeIconW / 2;
                // Vertical line segment
                renderer.DrawRect(sb, new Rectangle(lineX, y, 1, TreeRowH / 2), TreeLineColor);
                // Horizontal connector
                renderer.DrawRect(sb, new Rectangle(lineX, y + TreeRowH / 2, TreeIndent / 2, 1), TreeLineColor);
            }

            // Expand/collapse icon
            if (item.HasChildren)
            {
                var expandRect = new Rectangle(x, y + 2, TreeIconW, TreeRowH - 4);
                _treeExpandRects.Add(expandRect);
                string icon = item.Expanded ? "v" : ">";
                sb.DrawString(font, icon, new Vector2(x + 2, textY), ExpandColor);
                x += TreeIconW;
            }
            else
            {
                _treeExpandRects.Add(Rectangle.Empty);
                x += TreeIconW;
            }

            // Draw label with appropriate color
            Color labelColor = item.Type switch
            {
                TreeItemType.RouteHeader => RouteColor,
                TreeItemType.SectionHeader => SectionHeaderColor,
                TreeItemType.Node when item.IsOrphan => OrphanColor,
                TreeItemType.Node => NodeIdColor,
                TreeItemType.TextPreview => NodeTextColor,
                TreeItemType.Choice => ChoiceColor,
                TreeItemType.CycleRef => CycleColor,
                TreeItemType.MissingRef => CycleColor,
                _ => NodeIdColor,
            };

            // Prefix icon for nodes
            string prefix = item.Type switch
            {
                TreeItemType.Node => "",
                TreeItemType.Choice => "",
                _ => "",
            };

            string displayLabel = prefix + item.Label;
            int maxLabelW = rowRect.Right - x - 4;
            if (maxLabelW > 0)
            {
                string truncated = TruncateToFit(displayLabel, font, maxLabelW);
                sb.DrawString(font, truncated, new Vector2(x, textY), labelColor);
            }

            y += TreeRowH;
        }

        if (_flatRows.Count == 0)
        {
            string empty = "No nodes. Click '+ Node' to add one.";
            sb.DrawString(font, empty, new Vector2(_treeRect.X + Padding, y + Padding), HintColor);
            y += TreeRowH;
        }

        int totalH = y - startY;
        _treeScroll.EndScroll(sb, renderer, totalH);
    }

    private void DrawPropertiesPanel(SpriteBatch sb, SpriteFont font, Renderer renderer, GameTime gt)
    {
        _pendingDropdowns.Clear();
        _pendingComboBoxes.Clear();
        renderer.DrawRect(sb, _propsRect, PropsBg);

        if (_selectedNodeId == null)
        {
            DrawRoutesPanel(sb, font, renderer, gt);
            return;
        }

        // Find selected node
        var selectedNode = _data.Nodes.FirstOrDefault(n => n.Id == _selectedNodeId);
        if (selectedNode == null)
        {
            DrawRoutesPanel(sb, font, renderer, gt);
            return;
        }

        // Scrollable properties area
        var scrollViewport = new Rectangle(_propsRect.X, _propsRect.Y, _propsRect.Width, _propsRect.Height);
        int startY = _propsScroll.BeginScroll(sb, scrollViewport);

        int contentX = _propsRect.X + Padding;
        int contentW = _propsRect.Width - Padding * 2 - ScrollPanel.ScrollBarWidth;
        var layout = new FormLayout(contentX, contentW, startY + Padding,
            labelWidth: LabelWidth, fieldHeight: FieldHeight, rowHeight: RowHeight);

        // Section: Node Properties
        layout.DrawSectionHeader(sb, font, "Node Properties", PropsSectionColor);

        var idRect = layout.DrawLabeledField(sb, font, renderer, "Id:", _nodeIdField, gt, LabelColor);
        _tooltipFields.Add((idRect, _nodeIdField));

        var speakerRect = layout.DrawLabeledField(sb, font, renderer, "Speaker:", _nodeSpeakerField, gt, LabelColor);
        _tooltipFields.Add((speakerRect, _nodeSpeakerField));

        var textRect = layout.DrawLabeledField(sb, font, renderer, "Text:", _nodeTextField, gt, LabelColor);
        _tooltipFields.Add((textRect, _nodeTextField));

        var nextRect = layout.DrawLabeledField(sb, font, renderer, "Next:", _nodeNextField, gt, LabelColor);
        _tooltipFields.Add((nextRect, _nodeNextField));

        layout.Space(4);

        // Section: Conditions
        _conditionRemoveRects.Clear();
        DrawConditionActionHeader(sb, font, renderer, ref layout, contentX, "Conditions", ref _addConditionRect);
        for (int i = 0; i < _nodeConditionFields.Count; i++)
            DrawConditionRow(sb, font, renderer, gt, ref layout, contentX, contentW, _nodeConditionFields[i], _conditionRemoveRects);

        layout.Space(4);

        // Section: Actions
        _actionRemoveRects.Clear();
        DrawConditionActionHeader(sb, font, renderer, ref layout, contentX, "Actions", ref _addActionRect);
        for (int i = 0; i < _nodeActionFields.Count; i++)
            DrawActionRow(sb, font, renderer, gt, ref layout, contentX, contentW, _nodeActionFields[i], _actionRemoveRects);

        layout.Space(8);

        // Section: Choices
        _choiceAddConditionRects.Clear();
        _choiceAddActionRects.Clear();
        _choiceConditionRemoveRects.Clear();
        _choiceActionRemoveRects.Clear();
        int choiceLabelY = FormLayout.CenterTextY(layout.CursorY, FieldHeight, font.LineSpacing);
        sb.DrawString(font, "Choices", new Vector2(contentX, choiceLabelY), PropsSectionColor);

        int addBtnW = 50;
        _addChoiceRect = new Rectangle(contentX + 70, layout.CursorY, addBtnW, 18);
        DrawButton(sb, font, renderer, _addChoiceRect, "+ Add", AddBtnBg, AddBtnHoverBg);
        layout.CursorY += RowHeight;

        for (int c = 0; c < _choiceFields.Count; c++)
        {
            var cf = _choiceFields[c];
            char letter = (char)('a' + (c < 26 ? c : 25));

            int cLabelY = FormLayout.CenterTextY(layout.CursorY, FieldHeight, font.LineSpacing);
            sb.DrawString(font, $"{letter})", new Vector2(contentX, cLabelY), LabelColor);
            int cFieldX = contentX + 20;
            int cFieldW = contentW - 20;
            int delW = 36;
            var cTextRect = new Rectangle(cFieldX, layout.CursorY, cFieldW - delW - 4, FieldHeight);
            cf.TextField.Draw(sb, font, renderer, cTextRect, gt);
            _tooltipFields.Add((cTextRect, cf.TextField));

            var delRect = new Rectangle(cFieldX + cFieldW - delW, layout.CursorY, delW, FieldHeight);
            _choiceRemoveRects.Add(delRect);
            DrawButton(sb, font, renderer, delRect, "Del", RemoveColor, RemoveHoverColor);
            layout.CursorY += RowHeight;

            // Next
            var choiceLayout = new FormLayout(cFieldX, cFieldW, layout.CursorY,
                labelWidth: LabelWidth, fieldHeight: FieldHeight, rowHeight: RowHeight);
            var cNextRect = choiceLayout.DrawLabeledField(sb, font, renderer, "Next:", cf.NextField, gt, LabelColor);
            _tooltipFields.Add((cNextRect, cf.NextField));
            layout.CursorY = choiceLayout.CursorY;

            // Choice conditions
            var choiceCondRems = new List<Rectangle>();
            _choiceConditionRemoveRects.Add(choiceCondRems);
            var addCondRect = Rectangle.Empty;
            DrawConditionActionHeader(sb, font, renderer, ref layout, cFieldX, "Conditions", ref addCondRect);
            _choiceAddConditionRects.Add(addCondRect);
            for (int ci = 0; ci < cf.ConditionFields.Count; ci++)
                DrawConditionRow(sb, font, renderer, gt, ref layout, cFieldX, cFieldW, cf.ConditionFields[ci], choiceCondRems);

            // Choice actions
            var choiceActRems = new List<Rectangle>();
            _choiceActionRemoveRects.Add(choiceActRems);
            var addActRect = Rectangle.Empty;
            DrawConditionActionHeader(sb, font, renderer, ref layout, cFieldX, "Actions", ref addActRect);
            _choiceAddActionRects.Add(addActRect);
            for (int ai = 0; ai < cf.ActionFields.Count; ai++)
                DrawActionRow(sb, font, renderer, gt, ref layout, cFieldX, cFieldW, cf.ActionFields[ai], choiceActRems);

            layout.CursorY += 4;
        }

        int totalH = layout.CursorY - (startY + Padding) + Padding;
        _propsScroll.EndScroll(sb, renderer, totalH);

        // Draw dropdown popups on top
        foreach (var (dd, _) in _pendingDropdowns)
        {
            if (dd.IsOpen)
                dd.DrawPopup(sb, font, renderer);
        }
        foreach (var (cb, _) in _pendingComboBoxes)
        {
            if (cb.IsPopupOpen)
                cb.DrawPopup(sb, font, renderer);
        }
    }

    private void DrawRoutesPanel(SpriteBatch sb, SpriteFont font, Renderer renderer, GameTime gt)
    {
        _routeRemoveRects.Clear();
        _routeAddConditionRects.Clear();
        _routeConditionRemoveRects.Clear();

        var scrollViewport = new Rectangle(_propsRect.X, _propsRect.Y, _propsRect.Width, _propsRect.Height);
        int startY = _propsScroll.BeginScroll(sb, scrollViewport);

        int contentX = _propsRect.X + Padding;
        int contentW = _propsRect.Width - Padding * 2 - ScrollPanel.ScrollBarWidth;
        var layout = new FormLayout(contentX, contentW, startY + Padding,
            labelWidth: LabelWidth, fieldHeight: FieldHeight, rowHeight: RowHeight);

        // Header: Routes
        layout.DrawSectionHeader(sb, font, "Routes", PropsSectionColor);
        string routeHint = "Routes determine which node starts the dialogue based on conditions.";
        sb.DrawString(font, routeHint, new Vector2(contentX, layout.CursorY), HintColor);
        layout.CursorY += RowHeight;

        int addBtnW = 65;
        _addRouteRect = new Rectangle(contentX, layout.CursorY, addBtnW, 18);
        DrawButton(sb, font, renderer, _addRouteRect, "+ Route", AddBtnBg, AddBtnHoverBg);
        layout.CursorY += RowHeight;

        for (int r = 0; r < _routeFields.Count; r++)
        {
            var rf = _routeFields[r];

            int delW = 36;
            bool isLast = r == _routeFields.Count - 1;
            string routeLabel = isLast && (rf.ConditionFields.Count == 0)
                ? $"Route {r + 1} (default)"
                : $"Route {r + 1}";
            int rLabelY = FormLayout.CenterTextY(layout.CursorY, FieldHeight, font.LineSpacing);
            sb.DrawString(font, routeLabel, new Vector2(contentX, rLabelY), PropsSectionColor);

            var delRect = new Rectangle(contentX + contentW - delW, layout.CursorY, delW, FieldHeight);
            _routeRemoveRects.Add(delRect);
            DrawButton(sb, font, renderer, delRect, "Del", RemoveColor, RemoveHoverColor);
            layout.CursorY += RowHeight;

            // Start Node field
            int indent = 12;
            int indentedX = contentX + indent;
            int indentedW = contentW - indent;
            var routeLayout = new FormLayout(indentedX, indentedW, layout.CursorY,
                labelWidth: LabelWidth, fieldHeight: FieldHeight, rowHeight: RowHeight);
            var startRect = routeLayout.DrawLabeledField(sb, font, renderer, "Start:", rf.StartNodeField, gt, LabelColor);
            _tooltipFields.Add((startRect, rf.StartNodeField));
            layout.CursorY = routeLayout.CursorY;

            // Conditions
            var condRems = new List<Rectangle>();
            _routeConditionRemoveRects.Add(condRems);
            var addCondRect = Rectangle.Empty;
            DrawConditionActionHeader(sb, font, renderer, ref layout, indentedX, "Conditions", ref addCondRect);
            _routeAddConditionRects.Add(addCondRect);
            for (int ci = 0; ci < rf.ConditionFields.Count; ci++)
                DrawConditionRow(sb, font, renderer, gt, ref layout, indentedX, indentedW, rf.ConditionFields[ci], condRems);

            layout.Space(8);
        }

        if (_routeFields.Count == 0)
        {
            string noRoutes = "No routes. First node is used as start.";
            sb.DrawString(font, noRoutes, new Vector2(contentX, layout.CursorY), HintColor);
            layout.CursorY += RowHeight;
        }

        layout.Space(Padding);
        string selectHint = "Select a node in the tree to edit it.";
        sb.DrawString(font, selectHint, new Vector2(contentX, layout.CursorY), HintColor);
        layout.CursorY += RowHeight;

        int totalH = layout.CursorY - (startY + Padding) + Padding;
        _propsScroll.EndScroll(sb, renderer, totalH);

        foreach (var (dd, _) in _pendingDropdowns)
        {
            if (dd.IsOpen)
                dd.DrawPopup(sb, font, renderer);
        }
        foreach (var (cb, _) in _pendingComboBoxes)
        {
            if (cb.IsPopupOpen)
                cb.DrawPopup(sb, font, renderer);
        }
    }

    // === Condition/Action Draw Helpers ===

    private void DrawConditionActionHeader(SpriteBatch sb, SpriteFont font, Renderer renderer,
        ref FormLayout layout, int x, string label, ref Rectangle addRect)
    {
        int lblY = FormLayout.CenterTextY(layout.CursorY, FieldHeight, font.LineSpacing);
        sb.DrawString(font, label, new Vector2(x, lblY), PropsSectionColor);
        int lblW = (int)font.MeasureString(label).X + 6;
        addRect = new Rectangle(x + lblW, layout.CursorY, 50, 18);
        DrawButton(sb, font, renderer, addRect, "+ Add", AddBtnBg, AddBtnHoverBg);
        layout.CursorY += RowHeight;
    }

    private void DrawConditionRow(SpriteBatch sb, SpriteFont font, Renderer renderer, GameTime gt,
        ref FormLayout layout, int x, int w, ConditionFields cf, List<Rectangle> removeRects)
    {
        // Update suggestions based on current type
        cf.ValueCombo.SetSuggestions(GetConditionSuggestions(cf.TypeDD.SelectedItem));

        int delW = 30;
        int ddW = Math.Min(120, (w - delW - 8) / 3);
        int valW = w - ddW - delW - 8;

        var ddRect = new Rectangle(x, layout.CursorY, ddW, FieldHeight);
        cf.TypeDD.Draw(sb, font, renderer, ddRect);
        _pendingDropdowns.Add((cf.TypeDD, ddRect));

        var valRect = new Rectangle(x + ddW + 4, layout.CursorY, valW, FieldHeight);
        cf.ValueCombo.Draw(sb, font, renderer, valRect, gt);
        _pendingComboBoxes.Add((cf.ValueCombo, valRect));
        _tooltipFields.Add((valRect, cf.ValueCombo));

        var delRect = new Rectangle(x + w - delW, layout.CursorY, delW, FieldHeight);
        removeRects.Add(delRect);
        DrawButton(sb, font, renderer, delRect, "X", RemoveColor, RemoveHoverColor);

        layout.CursorY += RowHeight;
    }

    private void DrawActionRow(SpriteBatch sb, SpriteFont font, Renderer renderer, GameTime gt,
        ref FormLayout layout, int x, int w, ActionFields af, List<Rectangle> removeRects)
    {
        // Update suggestions based on current type
        af.ValueCombo.SetSuggestions(GetActionSuggestions(af.TypeDD.SelectedItem));

        int delW = 30;
        int ddW = Math.Min(140, (w - delW - 8) / 3);
        int remainW = w - ddW - delW - 12;
        bool needsKey = NeedsKeyField(af.TypeDD.SelectedItem);
        int valW = needsKey ? remainW / 2 : remainW;
        int keyW = needsKey ? remainW - valW - 4 : 0;

        var ddRect = new Rectangle(x, layout.CursorY, ddW, FieldHeight);
        af.TypeDD.Draw(sb, font, renderer, ddRect);
        _pendingDropdowns.Add((af.TypeDD, ddRect));

        var valRect = new Rectangle(x + ddW + 4, layout.CursorY, valW, FieldHeight);
        af.ValueCombo.Draw(sb, font, renderer, valRect, gt);
        _pendingComboBoxes.Add((af.ValueCombo, valRect));
        _tooltipFields.Add((valRect, af.ValueCombo));

        if (needsKey)
        {
            var keyRect = new Rectangle(x + ddW + 4 + valW + 4, layout.CursorY, keyW, FieldHeight);
            af.KeyField.Draw(sb, font, renderer, keyRect, gt);
            _tooltipFields.Add((keyRect, af.KeyField));
        }

        var delRect = new Rectangle(x + w - delW, layout.CursorY, delW, FieldHeight);
        removeRects.Add(delRect);
        DrawButton(sb, font, renderer, delRect, "X", RemoveColor, RemoveHoverColor);

        layout.CursorY += RowHeight;
    }

    private static bool NeedsKeyField(string actionType)
    {
        return actionType is "set_variable" or "log";
    }

    // === Layout ===

    private void ComputeSubRects()
    {
        int contentTop = _panelRect.Y + HeaderH;
        int contentBottom = _panelRect.Bottom - HintH;
        int contentH = contentBottom - contentTop;

        int treeW = (int)(_panelRect.Width * CanvasSplit);
        _treeRect = new Rectangle(_panelRect.X, contentTop, treeW, contentH);
        _propsRect = new Rectangle(_panelRect.X + treeW + 1, contentTop, _panelRect.Width - treeW - 1, contentH);
    }

    // === Selection ===

    private void SelectRow(int rowIndex)
    {
        FlushSelectedNode();

        _selectedRowIndex = rowIndex;

        if (rowIndex >= 0 && rowIndex < _flatRows.Count)
        {
            var item = _flatRows[rowIndex];
            if (item.Type == TreeItemType.Node || item.Type == TreeItemType.TextPreview ||
                item.Type == TreeItemType.Choice || item.Type == TreeItemType.CycleRef)
            {
                string nodeId = item.NodeId;
                if (nodeId != _selectedNodeId)
                {
                    _selectedNodeId = nodeId;
                    _propsScroll.ScrollOffset = 0;
                    int nodeIndex = _data.Nodes.FindIndex(n => n.Id == nodeId);
                    if (nodeIndex >= 0)
                        PopulatePropertiesPanel(nodeIndex);
                }
            }
            else
            {
                _selectedNodeId = null;
                _propsScroll.ScrollOffset = 0;
            }
        }
        else
        {
            _selectedNodeId = null;
            _propsScroll.ScrollOffset = 0;
        }
    }

    private void SelectNodeById(string nodeId)
    {
        _selectedNodeId = nodeId;
        _propsScroll.ScrollOffset = 0;
        if (nodeId != null)
        {
            int nodeIndex = _data.Nodes.FindIndex(n => n.Id == nodeId);
            if (nodeIndex >= 0)
                PopulatePropertiesPanel(nodeIndex);
        }

        // Find row
        _selectedRowIndex = -1;
        for (int i = 0; i < _flatRows.Count; i++)
        {
            if (_flatRows[i].NodeId == nodeId && _flatRows[i].Type == TreeItemType.Node)
            {
                _selectedRowIndex = i;
                break;
            }
        }
    }

    // === Data Operations ===

    private void PopulatePropertiesPanel(int index)
    {
        var node = _data.Nodes[index];
        _nodeIdField.SetText(node.Id ?? "");
        _nodeSpeakerField.SetText(node.Speaker ?? "");
        _nodeTextField.SetText(node.Text ?? "");
        _nodeNextField.SetText(node.NextNodeId ?? "");

        _nodeConditionFields.Clear();
        if (node.Conditions != null)
            foreach (var c in node.Conditions)
                _nodeConditionFields.Add(ConditionFields.FromCondition(c));

        _nodeActionFields.Clear();
        if (node.Actions != null)
            foreach (var a in node.Actions)
                _nodeActionFields.Add(ActionFields.FromAction(a));

        _choiceFields.Clear();
        if (node.Choices != null)
        {
            foreach (var choice in node.Choices)
            {
                var cf = new ChoiceFields
                {
                    TextField = new TextInputField(choice.Text ?? "", maxLength: 256),
                    NextField = new TextInputField(choice.NextNodeId ?? "", maxLength: 64),
                };
                if (choice.Conditions != null)
                    foreach (var c in choice.Conditions)
                        cf.ConditionFields.Add(ConditionFields.FromCondition(c));
                if (choice.Actions != null)
                    foreach (var a in choice.Actions)
                        cf.ActionFields.Add(ActionFields.FromAction(a));
                _choiceFields.Add(cf);
            }
        }
    }

    private void FlushSelectedNode()
    {
        if (_selectedNodeId == null) return;
        int index = _data.Nodes.FindIndex(n => n.Id == _selectedNodeId);
        if (index < 0) return;

        var node = _data.Nodes[index];
        string oldId = node.Id;
        string newId = NullIfEmpty(_nodeIdField.Text);
        node.Id = newId;
        node.Speaker = NullIfEmpty(_nodeSpeakerField.Text);
        node.Text = NullIfEmpty(_nodeTextField.Text);
        node.NextNodeId = NullIfEmpty(_nodeNextField.Text);

        // If node ID changed, update references and selection
        if (oldId != newId && oldId != null && newId != null)
        {
            foreach (var other in _data.Nodes)
            {
                if (other == node) continue;
                if (other.NextNodeId == oldId)
                    other.NextNodeId = newId;
                if (other.Choices != null)
                    foreach (var ch in other.Choices)
                        if (ch.NextNodeId == oldId)
                            ch.NextNodeId = newId;
            }
            if (_data.Routes != null)
                foreach (var route in _data.Routes)
                    if (route.StartNode == oldId)
                        route.StartNode = newId;
            _selectedNodeId = newId;
            _treeDirty = true;
        }

        node.Conditions = _nodeConditionFields.Count > 0
            ? _nodeConditionFields.ConvertAll(cf => cf.ToCondition())
            : null;
        node.Actions = _nodeActionFields.Count > 0
            ? _nodeActionFields.ConvertAll(af => af.ToAction())
            : null;
        node.RequiresFlag = null;
        node.SetsFlag = null;
        node.SetsVariable = null;

        if (_choiceFields.Count > 0)
        {
            node.Choices ??= new List<DialogueChoice>();
            node.Choices.Clear();
            foreach (var cf in _choiceFields)
            {
                node.Choices.Add(new DialogueChoice
                {
                    Text = NullIfEmpty(cf.TextField.Text),
                    NextNodeId = NullIfEmpty(cf.NextField.Text),
                    Conditions = cf.ConditionFields.Count > 0
                        ? cf.ConditionFields.ConvertAll(c => c.ToCondition())
                        : null,
                    Actions = cf.ActionFields.Count > 0
                        ? cf.ActionFields.ConvertAll(a => a.ToAction())
                        : null,
                    RequiresFlag = null,
                    SetsFlag = null,
                });
            }
            _treeDirty = true;
        }
        else
        {
            if (node.Choices != null && node.Choices.Count > 0)
                _treeDirty = true;
            node.Choices = null;
        }
    }

    private void PopulateRouteFields()
    {
        _routeFields.Clear();
        if (_data.Routes == null) return;
        foreach (var route in _data.Routes)
        {
            var rf = new RouteFields
            {
                StartNodeField = new TextInputField(route.StartNode ?? "", maxLength: 64),
            };
            if (route.Conditions != null)
                foreach (var c in route.Conditions)
                    rf.ConditionFields.Add(ConditionFields.FromCondition(c));
            _routeFields.Add(rf);
        }
    }

    private void FlushRoutes()
    {
        if (_routeFields.Count == 0)
        {
            if (_data.Routes != null)
                _treeDirty = true;
            _data.Routes = null;
            return;
        }

        _data.Routes = new List<DialogueRoute>();
        foreach (var rf in _routeFields)
        {
            var route = new DialogueRoute
            {
                StartNode = NullIfEmpty(rf.StartNodeField.Text),
                Conditions = rf.ConditionFields.Count > 0
                    ? rf.ConditionFields.ConvertAll(cf => cf.ToCondition())
                    : null,
            };
            _data.Routes.Add(route);
        }
        _treeDirty = true;
    }

    private void AddNode()
    {
        string newId = $"node_{_data.Nodes.Count}";
        // Ensure unique
        while (_data.Nodes.Any(n => n.Id == newId))
            newId = $"node_{_data.Nodes.Count + 1}";

        _data.Nodes.Add(new DialogueNode { Id = newId });
        _treeDirty = true;
        RebuildTree();
        SelectNodeById(newId);
        FocusField(_nodeIdField);
    }

    private void DeleteSelectedNode()
    {
        if (_selectedNodeId == null) return;
        int index = _data.Nodes.FindIndex(n => n.Id == _selectedNodeId);
        if (index < 0) return;

        string deletedId = _data.Nodes[index].Id;

        // Clear references
        foreach (var node in _data.Nodes)
        {
            if (node.NextNodeId == deletedId)
                node.NextNodeId = null;
            if (node.Choices != null)
                foreach (var choice in node.Choices)
                    if (choice.NextNodeId == deletedId)
                        choice.NextNodeId = null;
        }

        _data.Nodes.RemoveAt(index);
        _selectedNodeId = null;
        _selectedRowIndex = -1;
        _treeDirty = true;
        RebuildTree();
        FocusField(null);
    }

    // === Click Handling ===

    private void HandleTreePanelClick(MouseState mouse)
    {
        // Add node button (in header)
        if (_addNodeRect.Contains(mouse.X, mouse.Y))
        {
            FlushSelectedNode();
            AddNode();
            return;
        }

        // Delete node button (in header)
        if (_deleteNodeRect != Rectangle.Empty && _deleteNodeRect.Contains(mouse.X, mouse.Y))
        {
            DeleteSelectedNode();
            return;
        }

        // Check expand/collapse clicks first
        for (int i = 0; i < _treeExpandRects.Count && i < _flatRows.Count; i++)
        {
            if (_treeExpandRects[i] != Rectangle.Empty && _treeExpandRects[i].Contains(mouse.X, mouse.Y))
            {
                var item = _flatRows[i];
                if (item.HasChildren)
                {
                    item.Expanded = !item.Expanded;
                    FlattenTree();
                    // Adjust selection if it was in collapsed subtree
                    if (_selectedRowIndex >= _flatRows.Count)
                        _selectedRowIndex = _flatRows.Count - 1;
                    return;
                }
            }
        }

        // Check row clicks
        for (int i = 0; i < _treeRowRects.Count && i < _flatRows.Count; i++)
        {
            if (_treeRowRects[i].Contains(mouse.X, mouse.Y))
            {
                SelectRow(i);
                return;
            }
        }
    }

    private void HandlePropertiesPanelClick(MouseState mouse)
    {
        // Node condition add/remove
        if (_addConditionRect.Contains(mouse.X, mouse.Y))
        {
            _nodeConditionFields.Add(new ConditionFields());
            return;
        }
        for (int i = 0; i < _conditionRemoveRects.Count && i < _nodeConditionFields.Count; i++)
        {
            if (_conditionRemoveRects[i].Contains(mouse.X, mouse.Y))
            {
                _nodeConditionFields.RemoveAt(i);
                return;
            }
        }

        // Node action add/remove
        if (_addActionRect.Contains(mouse.X, mouse.Y))
        {
            _nodeActionFields.Add(new ActionFields());
            return;
        }
        for (int i = 0; i < _actionRemoveRects.Count && i < _nodeActionFields.Count; i++)
        {
            if (_actionRemoveRects[i].Contains(mouse.X, mouse.Y))
            {
                _nodeActionFields.RemoveAt(i);
                return;
            }
        }

        // Choice remove buttons
        for (int c = 0; c < _choiceRemoveRects.Count && c < _choiceFields.Count; c++)
        {
            if (_choiceRemoveRects[c].Contains(mouse.X, mouse.Y))
            {
                RemoveChoice(c);
                return;
            }
        }

        // Choice condition/action add/remove
        for (int c = 0; c < _choiceFields.Count; c++)
        {
            if (c < _choiceAddConditionRects.Count && _choiceAddConditionRects[c].Contains(mouse.X, mouse.Y))
            {
                _choiceFields[c].ConditionFields.Add(new ConditionFields());
                return;
            }
            if (c < _choiceAddActionRects.Count && _choiceAddActionRects[c].Contains(mouse.X, mouse.Y))
            {
                _choiceFields[c].ActionFields.Add(new ActionFields());
                return;
            }
            if (c < _choiceConditionRemoveRects.Count)
            {
                var rems = _choiceConditionRemoveRects[c];
                for (int i = 0; i < rems.Count && i < _choiceFields[c].ConditionFields.Count; i++)
                {
                    if (rems[i].Contains(mouse.X, mouse.Y))
                    {
                        _choiceFields[c].ConditionFields.RemoveAt(i);
                        return;
                    }
                }
            }
            if (c < _choiceActionRemoveRects.Count)
            {
                var rems = _choiceActionRemoveRects[c];
                for (int i = 0; i < rems.Count && i < _choiceFields[c].ActionFields.Count; i++)
                {
                    if (rems[i].Contains(mouse.X, mouse.Y))
                    {
                        _choiceFields[c].ActionFields.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        // Add choice button
        if (_addChoiceRect.Contains(mouse.X, mouse.Y))
        {
            AddChoice();
            return;
        }

        // Check field clicks
        foreach (var (rect, field) in _tooltipFields)
        {
            if (rect.Contains(mouse.X, mouse.Y))
            {
                FocusField(field);
                return;
            }
        }

        FocusField(null);
    }

    private void HandleRoutesPanelClick(MouseState mouse)
    {
        if (_addRouteRect.Contains(mouse.X, mouse.Y))
        {
            _routeFields.Add(new RouteFields());
            _treeDirty = true;
            return;
        }

        for (int r = 0; r < _routeRemoveRects.Count && r < _routeFields.Count; r++)
        {
            if (_routeRemoveRects[r].Contains(mouse.X, mouse.Y))
            {
                var rf = _routeFields[r];
                if (_activeField == rf.StartNodeField)
                    FocusField(null);
                _routeFields.RemoveAt(r);
                _treeDirty = true;
                return;
            }
        }

        for (int r = 0; r < _routeFields.Count; r++)
        {
            if (r < _routeAddConditionRects.Count && _routeAddConditionRects[r].Contains(mouse.X, mouse.Y))
            {
                _routeFields[r].ConditionFields.Add(new ConditionFields());
                return;
            }
            if (r < _routeConditionRemoveRects.Count)
            {
                var rems = _routeConditionRemoveRects[r];
                for (int i = 0; i < rems.Count && i < _routeFields[r].ConditionFields.Count; i++)
                {
                    if (rems[i].Contains(mouse.X, mouse.Y))
                    {
                        _routeFields[r].ConditionFields.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        foreach (var (rect, field) in _tooltipFields)
        {
            if (rect.Contains(mouse.X, mouse.Y))
            {
                FocusField(field);
                return;
            }
        }

        FocusField(null);
    }

    private void AddChoice()
    {
        _choiceFields.Add(new ChoiceFields
        {
            TextField = new TextInputField("", maxLength: 256),
            NextField = new TextInputField("", maxLength: 64),
        });
        _treeDirty = true;
    }

    private void RemoveChoice(int index)
    {
        if (index >= 0 && index < _choiceFields.Count)
        {
            var cf = _choiceFields[index];
            if (_activeField == cf.TextField || _activeField == cf.NextField)
                FocusField(null);
            _choiceFields.RemoveAt(index);
            _treeDirty = true;
        }
    }

    // === Confirmation ===

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

        FlushSelectedNode();
        FlushRoutes();
        _data.Id = id;
        Result = _data;
        IsComplete = true;
        WasCancelled = false;
    }

    // === Focus Management ===

    private List<object> GetPropertiesFields()
    {
        var fields = new List<object>(_tooltipFields.Count);
        foreach (var (_, field) in _tooltipFields)
            fields.Add(field);
        return fields;
    }

    private void CycleFocus()
    {
        var allFields = GetPropertiesFields();
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

    private void FocusField(object field)
    {
        switch (_activeField)
        {
            case TextInputField tif: tif.IsFocused = false; break;
            case ComboBox cb: cb.IsFocused = false; cb.ClosePopup(); break;
        }
        _activeField = field;
        switch (_activeField)
        {
            case TextInputField tif: tif.IsFocused = true; break;
            case ComboBox cb: cb.IsFocused = true; break;
        }
    }

    // === Tooltip ===

    private void UpdateTooltipHover(SpriteFont font, MouseState ms)
    {
        bool foundOverflow = false;
        foreach (var (rect, field) in _tooltipFields)
        {
            if (!rect.Contains(ms.X, ms.Y)) continue;
            bool overflows = field switch
            {
                TextInputField tif => tif.IsTextOverflowing(font, rect),
                ComboBox cb => cb.IsTextOverflowing(font, rect),
                _ => false,
            };
            if (overflows)
            {
                string text = field switch
                {
                    TextInputField tif => tif.Text,
                    ComboBox cb => cb.Text,
                    _ => "",
                };
                _tooltipManager.SetHover(text, ms.X, ms.Y);
                foundOverflow = true;
                break;
            }
        }
        if (!foundOverflow)
            _tooltipManager.ClearHover();
    }

    // === UI Helpers ===

    private void DrawButton(SpriteBatch sb, SpriteFont font, Renderer renderer,
        Rectangle rect, string label, Color bg, Color hoverBg)
    {
        bool hovered = rect.Contains(_cachedMousePos);
        renderer.DrawRect(sb, rect, hovered ? hoverBg : bg);
        var size = font.MeasureString(label);
        sb.DrawString(font, label,
            new Vector2(rect.X + (rect.Width - size.X) / 2, rect.Y + (rect.Height - size.Y) / 2), Color.White);
    }

    private static string NullIfEmpty(string text)
    {
        string trimmed = text?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static bool KeyPressed(KeyboardState current, KeyboardState prev, Keys key) =>
        current.IsKeyDown(key) && prev.IsKeyUp(key);

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return null;
        if (text.Length <= maxLength) return text;
        return text.Substring(0, maxLength - 3) + "...";
    }

    private static string TruncateToFit(string text, SpriteFont font, int maxWidth)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (font.MeasureString(text).X <= maxWidth) return text;
        for (int len = text.Length - 1; len > 0; len--)
        {
            string truncated = text.Substring(0, len) + "...";
            if (font.MeasureString(truncated).X <= maxWidth)
                return truncated;
        }
        return "...";
    }

    // === Deep Copy ===

    private static DialogueData DeepCopy(DialogueData src)
    {
        var copy = new DialogueData
        {
            Id = src.Id,
            Type = src.Type,
            OneShot = src.OneShot,
            Nodes = new List<DialogueNode>(),
        };

        if (src.Routes != null)
        {
            copy.Routes = new List<DialogueRoute>();
            foreach (var route in src.Routes)
            {
                var routeCopy = new DialogueRoute { StartNode = route.StartNode };
                if (route.Conditions != null)
                {
                    routeCopy.Conditions = new List<Condition>();
                    foreach (var c in route.Conditions)
                        routeCopy.Conditions.Add(new Condition
                        {
                            Type = c.Type, Flag = c.Flag, Item = c.Item,
                            Variable = c.Variable, Value = c.Value, Operator = c.Operator,
                        });
                }
                copy.Routes.Add(routeCopy);
            }
        }

        foreach (var node in src.Nodes)
        {
            var nodeCopy = new DialogueNode
            {
                Id = node.Id,
                Speaker = node.Speaker,
                Text = node.Text,
                NextNodeId = node.NextNodeId,
                RequiresFlag = node.RequiresFlag,
                SetsFlag = node.SetsFlag,
                SetsVariable = node.SetsVariable,
                EditorX = node.EditorX,
                EditorY = node.EditorY,
                Tags = node.Tags != null ? new List<string>(node.Tags) : null,
            };

            if (node.Conditions != null)
            {
                nodeCopy.Conditions = new List<Condition>();
                foreach (var c in node.Conditions)
                    nodeCopy.Conditions.Add(new Condition
                    {
                        Type = c.Type, Flag = c.Flag, Item = c.Item,
                        Variable = c.Variable, Value = c.Value, Operator = c.Operator,
                    });
            }

            if (node.Actions != null)
            {
                nodeCopy.Actions = new List<DialogueAction>();
                foreach (var a in node.Actions)
                    nodeCopy.Actions.Add(new DialogueAction
                    {
                        Type = a.Type, Value = a.Value, Key = a.Key,
                        Color = a.Color, Text = a.Text,
                    });
            }

            if (node.Choices != null)
            {
                nodeCopy.Choices = new List<DialogueChoice>();
                foreach (var choice in node.Choices)
                {
                    var choiceCopy = new DialogueChoice
                    {
                        Text = choice.Text,
                        NextNodeId = choice.NextNodeId,
                        RequiresFlag = choice.RequiresFlag,
                        SetsFlag = choice.SetsFlag,
                    };
                    if (choice.Conditions != null)
                    {
                        choiceCopy.Conditions = new List<Condition>();
                        foreach (var c in choice.Conditions)
                            choiceCopy.Conditions.Add(new Condition
                            {
                                Type = c.Type, Flag = c.Flag, Item = c.Item,
                                Variable = c.Variable, Value = c.Value, Operator = c.Operator,
                            });
                    }
                    if (choice.Actions != null)
                    {
                        choiceCopy.Actions = new List<DialogueAction>();
                        foreach (var a in choice.Actions)
                            choiceCopy.Actions.Add(new DialogueAction
                            {
                                Type = a.Type, Value = a.Value, Key = a.Key,
                                Color = a.Color, Text = a.Text,
                            });
                    }
                    nodeCopy.Choices.Add(choiceCopy);
                }
            }

            copy.Nodes.Add(nodeCopy);
        }
        return copy;
    }

    // === Suggestions ===

    private void RebuildSuggestions(IProjectContext ctx, List<QuestDefinition> quests, List<TileGroup> groups)
    {
        if (ctx == null) return;

        _knownFlags = ctx.GetKnownFlags(quests, groups);
        _knownVariables = ctx.GetKnownVariables(quests, groups);

        // Collect item names from entity groups
        if (groups != null)
        {
            var items = new List<string>();
            foreach (var g in groups)
            {
                if (g.EntityType == EntityType.Item && !string.IsNullOrWhiteSpace(g.Name))
                    items.Add(g.Name);
            }
            items.Sort(StringComparer.OrdinalIgnoreCase);
            _knownItems = items.ToArray();
        }

        // Collect quest IDs
        if (quests != null)
        {
            var ids = new List<string>();
            foreach (var q in quests)
            {
                if (!string.IsNullOrWhiteSpace(q.Id))
                    ids.Add(q.Id);
            }
            ids.Sort(StringComparer.OrdinalIgnoreCase);
            _knownQuests = ids.ToArray();
        }
    }

    private string[] GetConditionSuggestions(string conditionType)
    {
        return conditionType switch
        {
            "has_flag" or "not_flag" => _knownFlags,
            "has_item" => _knownItems,
            "variable_eq" or "variable_gte" or "variable_lt" => _knownVariables,
            "quest_active" or "quest_complete" => _knownQuests,
            _ => Array.Empty<string>(),
        };
    }

    private string[] GetActionSuggestions(string actionType)
    {
        return actionType switch
        {
            "set_flag" => _knownFlags,
            "set_variable" or "increment" => _knownVariables,
            "give_item" or "remove_item" => _knownItems,
            "start_quest" or "complete_objective" => _knownQuests,
            _ => Array.Empty<string>(),
        };
    }

    // === Inner Classes ===

    private enum TreeItemType
    {
        RouteHeader,
        SectionHeader,
        Node,
        TextPreview,
        Choice,
        CycleRef,
        MissingRef,
    }

    private class TreeItem
    {
        public TreeItemType Type;
        public string NodeId;
        public DialogueNode Node;
        public string Label;
        public int Depth;
        public int ChoiceIndex;
        public bool Expanded = true;
        public bool IsOrphan;
        public List<TreeItem> Children = new();

        public bool HasChildren => Children.Count > 0;
    }

    private class ChoiceFields
    {
        public TextInputField TextField = new("", maxLength: 256);
        public TextInputField NextField = new("", maxLength: 64);
        public List<ConditionFields> ConditionFields = new();
        public List<ActionFields> ActionFields = new();
    }

    private class RouteFields
    {
        public TextInputField StartNodeField = new("", maxLength: 64);
        public List<ConditionFields> ConditionFields = new();
    }

    private class ConditionFields
    {
        public Dropdown TypeDD = new(ConditionTypes, 0);
        public ComboBox ValueCombo = new("", maxLength: 128);

        public static ConditionFields FromCondition(Condition c)
        {
            int idx = Array.IndexOf(ConditionTypes, c.Type ?? "has_flag");
            if (idx < 0) idx = 0;
            string val = c.Type switch
            {
                "has_flag" or "not_flag" => c.Flag ?? "",
                "has_item" => c.Item ?? "",
                "variable_eq" or "variable_gte" or "variable_lt" => $"{c.Variable}={c.Value}",
                "quest_active" or "quest_complete" => c.Value ?? "",
                _ => c.Flag ?? c.Value ?? "",
            };
            return new ConditionFields
            {
                TypeDD = new Dropdown(ConditionTypes, idx),
                ValueCombo = new ComboBox(val, maxLength: 128),
            };
        }

        public Condition ToCondition()
        {
            string type = TypeDD.SelectedItem ?? "has_flag";
            string val = ValueCombo.Text?.Trim() ?? "";
            var c = new Condition { Type = type };
            switch (type)
            {
                case "has_flag":
                case "not_flag":
                    c.Flag = val;
                    break;
                case "has_item":
                    c.Item = val;
                    break;
                case "variable_eq":
                case "variable_gte":
                case "variable_lt":
                    int eq = val.IndexOf('=');
                    if (eq > 0)
                    {
                        c.Variable = val.Substring(0, eq);
                        c.Value = val.Substring(eq + 1);
                    }
                    else
                    {
                        c.Variable = val;
                        c.Value = "";
                    }
                    break;
                case "quest_active":
                case "quest_complete":
                    c.Value = val;
                    break;
            }
            return c;
        }
    }

    private class ActionFields
    {
        public Dropdown TypeDD = new(ActionTypes, 0);
        public ComboBox ValueCombo = new("", maxLength: 256);
        public TextInputField KeyField = new("", maxLength: 128);

        public static ActionFields FromAction(DialogueAction a)
        {
            int idx = Array.IndexOf(ActionTypes, a.Type ?? "set_flag");
            if (idx < 0) idx = 0;
            string val = a.Value ?? "";
            string key = a.Key ?? "";
            if (a.Type == "log")
            {
                val = a.Text ?? "";
                key = a.Color ?? "";
            }
            return new ActionFields
            {
                TypeDD = new Dropdown(ActionTypes, idx),
                ValueCombo = new ComboBox(val, maxLength: 256),
                KeyField = new TextInputField(key, maxLength: 128),
            };
        }

        public DialogueAction ToAction()
        {
            string type = TypeDD.SelectedItem ?? "set_flag";
            string val = ValueCombo.Text?.Trim() ?? "";
            string key = KeyField.Text?.Trim() ?? "";
            var a = new DialogueAction { Type = type };
            switch (type)
            {
                case "set_variable":
                    a.Key = val;
                    a.Value = key;
                    break;
                case "log":
                    a.Text = val;
                    a.Color = string.IsNullOrEmpty(key) ? null : key;
                    break;
                default:
                    a.Value = val;
                    break;
            }
            return a;
        }
    }
}
