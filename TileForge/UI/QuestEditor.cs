using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Game;

namespace TileForge.UI;

public class QuestEditor
{
    // Completion
    public bool IsComplete { get; private set; }
    public bool WasCancelled { get; private set; }
    public QuestDefinition Result { get; private set; }
    public bool IsNew { get; private set; }
    public string OriginalId { get; private set; }

    // Top-level fields
    private TextInputField _idField;
    private TextInputField _nameField;
    private TextInputField _descriptionField;
    private TextInputField _startFlagField;
    private TextInputField _completionFlagField;

    // Objectives
    private readonly List<ObjectiveRow> _objectives = new();

    // Rewards
    private TextInputField _rewardFlagsField;
    private TextInputField _rewardVariablesField;

    // Focus management
    private TextInputField _activeField;

    // Scroll + tooltip
    private readonly ScrollPanel _scrollPanel = new();
    private readonly TooltipManager _tooltipManager = new(delaySeconds: 0.4);

    // Layout constants
    private const int Padding = LayoutConstants.FormPadding;
    private const int FieldHeight = LayoutConstants.QuestEditorFieldHeight;
    private const int RowHeight = LayoutConstants.QuestEditorRowHeight;
    private const int LabelWidth = LayoutConstants.QuestEditorLabelWidth;
    private const int DefaultMaxWidth = LayoutConstants.QuestEditorMaxWidth;
    private const int DefaultMaxHeight = LayoutConstants.QuestEditorMaxHeight;
    private const int TypeDDWidth = 70;

    // Resize state
    private ModalResizeHandler _resize;

    // Colors
    private static readonly Color Overlay = LayoutConstants.QuestEditorOverlay;
    private static readonly Color PanelBg = LayoutConstants.QuestEditorPanelBg;
    private static readonly Color HeaderBg = LayoutConstants.QuestEditorHeaderBg;
    private static readonly Color LabelColor = LayoutConstants.QuestEditorLabelColor;
    private static readonly Color HintColor = LayoutConstants.QuestEditorHintColor;
    private static readonly Color SectionColor = LayoutConstants.QuestEditorSectionColor;
    private static readonly Color AddBtnBg = LayoutConstants.QuestEditorAddButtonBg;
    private static readonly Color AddBtnHoverBg = LayoutConstants.QuestEditorAddButtonHoverBg;
    private static readonly Color RemoveColor = LayoutConstants.QuestEditorRemoveColor;
    private static readonly Color RemoveHoverColor = LayoutConstants.QuestEditorRemoveHoverColor;

    // Objective type items
    private static readonly string[] ObjTypeItems = { "flag", "var>=", "var==" };
    private static readonly string[] ObjTypeValues = { "flag", "variable_gte", "variable_eq" };

    // Hit-test rectangles (computed during layout)
    private readonly List<Rectangle> _objectiveRemoveRects = new();
    private Rectangle _addObjectiveRect;
    private Rectangle _panelRect;

    // Field rectangle cache (built during Draw, used during next Update)
    private readonly List<Rectangle> _fieldRects = new();

    // Tooltip tracking
    private readonly List<(Rectangle Rect, TextInputField Field)> _tooltipFields = new();

    private QuestEditor() { }

    public static QuestEditor ForNewQuest()
    {
        var editor = new QuestEditor
        {
            IsNew = true,
            _idField = new TextInputField("", maxLength: 64),
            _nameField = new TextInputField("", maxLength: 128),
            _descriptionField = new TextInputField("", maxLength: 512),
            _startFlagField = new TextInputField("", maxLength: 128),
            _completionFlagField = new TextInputField("", maxLength: 128),
            _rewardFlagsField = new TextInputField("", maxLength: 256),
            _rewardVariablesField = new TextInputField("", maxLength: 256),
        };
        editor.FocusField(editor._idField);
        return editor;
    }

    public static QuestEditor ForExistingQuest(QuestDefinition quest)
    {
        var editor = new QuestEditor
        {
            IsNew = false,
            OriginalId = quest.Id,
            _idField = new TextInputField(quest.Id ?? "", maxLength: 64),
            _nameField = new TextInputField(quest.Name ?? "", maxLength: 128),
            _descriptionField = new TextInputField(quest.Description ?? "", maxLength: 512),
            _startFlagField = new TextInputField(quest.StartFlag ?? "", maxLength: 128),
            _completionFlagField = new TextInputField(quest.CompletionFlag ?? "", maxLength: 128),
            _rewardFlagsField = new TextInputField(FormatRewardFlags(quest.Rewards), maxLength: 256),
            _rewardVariablesField = new TextInputField(FormatRewardVariables(quest.Rewards), maxLength: 256),
        };

        foreach (var obj in quest.Objectives)
        {
            int typeIdx = Array.IndexOf(ObjTypeValues, obj.Type);
            if (typeIdx < 0) typeIdx = 0;

            editor._objectives.Add(new ObjectiveRow
            {
                DescriptionField = new TextInputField(obj.Description ?? "", maxLength: 256),
                TypeDD = new Dropdown(ObjTypeItems, typeIdx),
                FlagOrVariableField = new TextInputField(
                    typeIdx == 0 ? (obj.Flag ?? "") : (obj.Variable ?? ""), maxLength: 128),
                ValueField = new TextInputField(
                    typeIdx == 0 ? "" : obj.Value.ToString(), maxLength: 16),
            });
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
                       Rectangle bounds, List<QuestDefinition> existingQuests,
                       SpriteFont font = null, int screenW = 0, int screenH = 0)
    {
        if (KeyPressed(keyboard, prevKeyboard, Keys.Escape))
        {
            IsComplete = true;
            WasCancelled = true;
            return;
        }

        bool anyDDOpen = _objectives.Any(o => o.TypeDD.IsOpen);

        if (KeyPressed(keyboard, prevKeyboard, Keys.Tab) && !anyDDOpen)
            CycleFocus();

        if (KeyPressed(keyboard, prevKeyboard, Keys.Enter) && !anyDDOpen)
        {
            TryConfirm(existingQuests);
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

        // Compute panel rect + handle resize
        _panelRect = _resize.ComputePanelRect(DefaultMaxWidth, DefaultMaxHeight, bounds);
        _resize.HandleResize(mouse, prevMouse, bounds);

        int px = _panelRect.X, py = _panelRect.Y;
        int panelW = _panelRect.Width, panelH = _panelRect.Height;

        int contentTop = py + 30;
        int contentBottom = py + panelH - 26;
        var contentViewport = new Rectangle(px, contentTop, panelW, contentBottom - contentTop);

        // Scroll panel update
        _scrollPanel.UpdateScroll(mouse, prevMouse, contentViewport);

        // Tooltip update
        _tooltipManager.Update(mouse.X != prevMouse.X || mouse.Y != prevMouse.Y ? 0 : 1.0 / 60.0);

        int contentW = panelW - Padding * 2;
        int fieldW = contentW - LabelWidth;
        int y = contentTop + Padding - _scrollPanel.ScrollOffset;

        // Skip top-level fields (5 rows + gap + section header)
        y += 5 * RowHeight + 6 + RowHeight;

        // InputEvent for click consumption between controls
        var input = new InputEvent(mouse, prevMouse);

        // Update objective type dropdowns with computed positions
        _objectiveRemoveRects.Clear();
        if (font != null)
        {
            for (int i = 0; i < _objectives.Count; i++)
            {
                var obj = _objectives[i];
                int descFieldW = fieldW - TypeDDWidth - 8;
                var typeRect = new Rectangle(px + Padding + 20 + descFieldW + 4, y, TypeDDWidth, FieldHeight);
                obj.TypeDD.Update(input, typeRect, font, screenW, screenH);
                y += RowHeight;

                // Remove button rect
                int removeBtnW = 50;
                var removeRect = new Rectangle(px + panelW - Padding - removeBtnW, y, removeBtnW, FieldHeight);
                _objectiveRemoveRects.Add(removeRect);

                y += RowHeight + 8;
            }
        }

        // Mouse handling — skip if a dropdown consumed the click
        if (input.HasUnconsumedClick && !_resize.IsResizing)
        {
            bool clickHandled = false;

            // Check objective remove buttons
            for (int i = 0; i < _objectiveRemoveRects.Count && i < _objectives.Count; i++)
            {
                if (_objectiveRemoveRects[i].Contains(mouse.X, mouse.Y))
                {
                    RemoveObjective(i);
                    clickHandled = true;
                    break;
                }
            }

            // Check add objective button
            if (!clickHandled && _addObjectiveRect.Contains(mouse.X, mouse.Y))
            {
                AddObjective();
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

    private List<TextInputField> GetAllFields()
    {
        var fields = new List<TextInputField>
        {
            _idField, _nameField, _descriptionField, _startFlagField, _completionFlagField
        };
        foreach (var obj in _objectives)
        {
            fields.Add(obj.DescriptionField);
            fields.Add(obj.FlagOrVariableField);
            if (obj.TypeDD.SelectedIndex != 0)
                fields.Add(obj.ValueField);
        }
        fields.Add(_rewardFlagsField);
        fields.Add(_rewardVariablesField);
        return fields;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                     Rectangle bounds, GameTime gameTime)
    {
        _fieldRects.Clear();
        _tooltipFields.Clear();

        // Dim background
        renderer.DrawRect(spriteBatch, bounds, Overlay);

        // Panel rect (already computed in Update, recompute for Draw in case bounds changed)
        _panelRect = _resize.ComputePanelRect(DefaultMaxWidth, DefaultMaxHeight, bounds);
        int px = _panelRect.X, py = _panelRect.Y;
        int panelW = _panelRect.Width, panelH = _panelRect.Height;

        // Panel background
        renderer.DrawRect(spriteBatch, _panelRect, PanelBg);

        // Header
        var headerRect = new Rectangle(px, py, panelW, 30);
        renderer.DrawRect(spriteBatch, headerRect, HeaderBg);
        string title = IsNew ? "New Quest" : "Edit Quest";
        spriteBatch.DrawString(font, title, new Vector2(px + Padding, py + (30 - font.LineSpacing) / 2), Color.White);

        // Resize grip
        _resize.DrawResizeGrip(spriteBatch, renderer);

        // Content area
        int contentTop = py + 30;
        int contentBottom = py + panelH - 26;
        var contentViewport = new Rectangle(px, contentTop, panelW, contentBottom - contentTop);

        // Begin scrollable region
        int startY = _scrollPanel.BeginScroll(spriteBatch, contentViewport);

        int contentPadX = px + Padding;
        int contentW = panelW - Padding * 2;
        var layout = new FormLayout(contentPadX, contentW, startY + Padding,
                                     labelWidth: LabelWidth, fieldHeight: FieldHeight, rowHeight: RowHeight);

        // Top-level fields
        var idRect = layout.DrawLabeledField(spriteBatch, font, renderer, "Id:", _idField, gameTime, LabelColor);
        _fieldRects.Add(idRect);
        _tooltipFields.Add((idRect, _idField));

        var nameRect = layout.DrawLabeledField(spriteBatch, font, renderer, "Name:", _nameField, gameTime, LabelColor);
        _fieldRects.Add(nameRect);
        _tooltipFields.Add((nameRect, _nameField));

        var descRect = layout.DrawLabeledField(spriteBatch, font, renderer, "Description:", _descriptionField, gameTime, LabelColor);
        _fieldRects.Add(descRect);
        _tooltipFields.Add((descRect, _descriptionField));

        var startRect = layout.DrawLabeledField(spriteBatch, font, renderer, "Start Flag:", _startFlagField, gameTime, LabelColor);
        _fieldRects.Add(startRect);
        _tooltipFields.Add((startRect, _startFlagField));

        var compRect = layout.DrawLabeledField(spriteBatch, font, renderer, "Completion:", _completionFlagField, gameTime, LabelColor);
        _fieldRects.Add(compRect);
        _tooltipFields.Add((compRect, _completionFlagField));

        layout.Space(6);

        // Objectives section header + Add button
        int sectionLabelY = FormLayout.CenterTextY(layout.CursorY, FieldHeight, font.LineSpacing);
        spriteBatch.DrawString(font, "Objectives", new Vector2(contentPadX, sectionLabelY), SectionColor);

        int addBtnW = 60;
        _addObjectiveRect = new Rectangle(contentPadX + contentW - addBtnW, layout.CursorY, addBtnW, 18);
        DrawButton(spriteBatch, font, renderer, _addObjectiveRect, "+ Add", AddBtnBg, AddBtnHoverBg);

        layout.CursorY += RowHeight;

        // Objective rows
        int fieldW = contentW - LabelWidth;
        for (int i = 0; i < _objectives.Count; i++)
        {
            var obj = _objectives[i];

            // Row 1: number + Description + type dropdown
            int descFieldW = fieldW - TypeDDWidth - 8;
            int numLabelY = FormLayout.CenterTextY(layout.CursorY, FieldHeight, font.LineSpacing);
            spriteBatch.DrawString(font, $"{i + 1}.", new Vector2(contentPadX, numLabelY), LabelColor);

            var objDescRect = new Rectangle(contentPadX + 20, layout.CursorY, descFieldW, FieldHeight);
            obj.DescriptionField.Draw(spriteBatch, font, renderer, objDescRect, gameTime);
            _fieldRects.Add(objDescRect);
            _tooltipFields.Add((objDescRect, obj.DescriptionField));

            var typeRect = new Rectangle(objDescRect.Right + 4, layout.CursorY, TypeDDWidth, FieldHeight);
            obj.TypeDD.Draw(spriteBatch, font, renderer, typeRect);

            layout.CursorY += RowHeight;

            // Row 2: Flag/Variable + Value + Remove button
            string condLabel = obj.TypeDD.SelectedIndex == 0 ? "Flag:" : "Variable:";
            int condLblY = FormLayout.CenterTextY(layout.CursorY, FieldHeight, font.LineSpacing);
            spriteBatch.DrawString(font, condLabel, new Vector2(contentPadX + 20, condLblY), LabelColor);

            int condLabelW = (int)font.MeasureString(condLabel).X + 6;
            int removeBtnW = 50;
            int condFieldX = contentPadX + 20 + condLabelW;

            if (obj.TypeDD.SelectedIndex != 0)
            {
                // Flag/Variable + = + Value + Remove
                int valW = 40;
                int eqW = (int)font.MeasureString("=").X + 8;
                int condFieldW = contentW - 20 - condLabelW - eqW - valW - removeBtnW - 12;
                var condRect = new Rectangle(condFieldX, layout.CursorY, condFieldW, FieldHeight);
                obj.FlagOrVariableField.Draw(spriteBatch, font, renderer, condRect, gameTime);
                _fieldRects.Add(condRect);
                _tooltipFields.Add((condRect, obj.FlagOrVariableField));

                int eqX = condRect.Right + 4;
                spriteBatch.DrawString(font, "=", new Vector2(eqX, condLblY), LabelColor);

                var valRect = new Rectangle(eqX + eqW, layout.CursorY, valW, FieldHeight);
                obj.ValueField.Draw(spriteBatch, font, renderer, valRect, gameTime);
                _fieldRects.Add(valRect);
                _tooltipFields.Add((valRect, obj.ValueField));
            }
            else
            {
                // Flag field + Remove
                int condFieldW = contentW - 20 - condLabelW - removeBtnW - 8;
                var condRect = new Rectangle(condFieldX, layout.CursorY, condFieldW, FieldHeight);
                obj.FlagOrVariableField.Draw(spriteBatch, font, renderer, condRect, gameTime);
                _fieldRects.Add(condRect);
                _tooltipFields.Add((condRect, obj.FlagOrVariableField));
            }

            // Remove button
            var removeRect = new Rectangle(contentPadX + contentW - removeBtnW, layout.CursorY, removeBtnW, FieldHeight);
            DrawButton(spriteBatch, font, renderer, removeRect, "Del", RemoveColor, RemoveHoverColor);

            layout.CursorY += RowHeight + 4;
            renderer.DrawRect(spriteBatch, new Rectangle(contentPadX, layout.CursorY, contentW, 1), new Color(60, 60, 60));
            layout.CursorY += 4;
        }

        layout.Space(4);

        // Rewards section
        int rewardLabelY = FormLayout.CenterTextY(layout.CursorY, FieldHeight, font.LineSpacing);
        spriteBatch.DrawString(font, "Rewards", new Vector2(contentPadX, rewardLabelY), SectionColor);
        layout.CursorY += RowHeight - 4;

        var rewFlagsRect = layout.DrawLabeledField(spriteBatch, font, renderer, "Set Flags:", _rewardFlagsField, gameTime, LabelColor);
        _fieldRects.Add(rewFlagsRect);
        _tooltipFields.Add((rewFlagsRect, _rewardFlagsField));

        var rewVarsRect = layout.DrawLabeledField(spriteBatch, font, renderer, "Set Vars:", _rewardVariablesField, gameTime, LabelColor);
        _fieldRects.Add(rewVarsRect);
        _tooltipFields.Add((rewVarsRect, _rewardVariablesField));

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

        // Dropdown popups (z-ordering: drawn AFTER scroll region ends)
        foreach (var obj in _objectives)
            obj.TypeDD.DrawPopup(spriteBatch, font, renderer);

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

    private void TryConfirm(List<QuestDefinition> existingQuests)
    {
        string id = _idField.Text.Trim();
        string name = _nameField.Text.Trim();
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name)) return;

        if (existingQuests != null)
        {
            foreach (var existing in existingQuests)
            {
                if (existing.Id == id && (IsNew || id != OriginalId))
                    return;
            }
        }

        Result = BuildResult();
        IsComplete = true;
        WasCancelled = false;
    }

    private QuestDefinition BuildResult()
    {
        var quest = new QuestDefinition
        {
            Id = _idField.Text.Trim(),
            Name = _nameField.Text.Trim(),
            Description = _descriptionField.Text.Trim(),
            StartFlag = _startFlagField.Text.Trim(),
            CompletionFlag = _completionFlagField.Text.Trim(),
            Objectives = new List<QuestObjective>(),
            Rewards = new QuestRewards
            {
                SetFlags = ParseRewardFlags(_rewardFlagsField.Text),
                SetVariables = ParseRewardVariables(_rewardVariablesField.Text),
            },
        };

        foreach (var obj in _objectives)
        {
            int typeIdx = obj.TypeDD.SelectedIndex;
            var objective = new QuestObjective
            {
                Description = obj.DescriptionField.Text.Trim(),
                Type = ObjTypeValues[typeIdx],
            };

            if (typeIdx == 0)
                objective.Flag = obj.FlagOrVariableField.Text.Trim();
            else
            {
                objective.Variable = obj.FlagOrVariableField.Text.Trim();
                if (int.TryParse(obj.ValueField.Text.Trim(), out int val))
                    objective.Value = val;
            }

            quest.Objectives.Add(objective);
        }

        if (string.IsNullOrEmpty(quest.Description)) quest.Description = null;
        if (string.IsNullOrEmpty(quest.StartFlag)) quest.StartFlag = null;
        if (string.IsNullOrEmpty(quest.CompletionFlag)) quest.CompletionFlag = null;

        return quest;
    }

    private void AddObjective()
    {
        _objectives.Add(new ObjectiveRow
        {
            DescriptionField = new TextInputField("", maxLength: 256),
            TypeDD = new Dropdown(ObjTypeItems, 0),
            FlagOrVariableField = new TextInputField("", maxLength: 128),
            ValueField = new TextInputField("", maxLength: 16),
        });
    }

    private void RemoveObjective(int index)
    {
        if (index >= 0 && index < _objectives.Count)
        {
            var obj = _objectives[index];
            if (_activeField == obj.DescriptionField ||
                _activeField == obj.FlagOrVariableField ||
                _activeField == obj.ValueField)
                FocusField(null);
            _objectives.RemoveAt(index);
        }
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

    // ---- Static helpers for reward text parsing (public for testability) ----

    public static string FormatRewardFlags(QuestRewards rewards)
    {
        if (rewards?.SetFlags == null || rewards.SetFlags.Count == 0) return "";
        return string.Join(", ", rewards.SetFlags);
    }

    public static string FormatRewardVariables(QuestRewards rewards)
    {
        if (rewards?.SetVariables == null || rewards.SetVariables.Count == 0) return "";
        return string.Join(", ", rewards.SetVariables.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    public static List<string> ParseRewardFlags(string text)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return result;
        foreach (var part in text.Split(','))
        {
            string trimmed = part.Trim();
            if (!string.IsNullOrEmpty(trimmed)) result.Add(trimmed);
        }
        return result;
    }

    public static Dictionary<string, string> ParseRewardVariables(string text)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(text)) return result;
        foreach (var part in text.Split(','))
        {
            string trimmed = part.Trim();
            int eqIdx = trimmed.IndexOf('=');
            if (eqIdx > 0)
            {
                string key = trimmed[..eqIdx].Trim();
                string val = trimmed[(eqIdx + 1)..].Trim();
                if (!string.IsNullOrEmpty(key)) result[key] = val;
            }
        }
        return result;
    }

    private static bool KeyPressed(KeyboardState current, KeyboardState prev, Keys key) =>
        current.IsKeyDown(key) && prev.IsKeyUp(key);

    private class ObjectiveRow
    {
        public TextInputField DescriptionField;
        public Dropdown TypeDD;
        public TextInputField FlagOrVariableField;
        public TextInputField ValueField;
    }
}
