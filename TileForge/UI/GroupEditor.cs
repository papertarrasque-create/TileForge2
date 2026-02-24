using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Game;

namespace TileForge.UI;

public class GroupEditor
{
    private static readonly Color BgColor = LayoutConstants.GroupEditorBackground;
    private static readonly Color HeaderBg = LayoutConstants.GroupEditorHeaderColor;
    private static readonly Color GridColor = LayoutConstants.GroupEditorGridColor;
    private static readonly Color SelFill = LayoutConstants.GroupEditorSelectionFill;
    private static readonly Color SelBorder = LayoutConstants.GroupEditorSelectionBorder;
    private static readonly Color HintColor = LayoutConstants.GroupEditorHintColor;
    private static readonly Color LabelColor = new(200, 200, 200);

    private const int HeaderBaseH = LayoutConstants.GroupEditorHeaderBaseHeight;
    private const int PropRowH = 28;
    private const int PropLabelW = 100;
    private const int PropFieldH = 22;
    private const int DropW = 160;
    private const int NumW = 80;
    private const int CbW = 22;

    // Mode
    private bool _isNew;
    private string _originalName;

    // Core components
    private Camera _camera = new();
    private Selection _selection = new();
    private TextInputField _nameField;
    private IProjectContext _projectContext;

    // Pan
    private bool _isPanning;
    private Point _panStart;
    private Vector2 _panOffsetStart;

    // Header controls
    private Dropdown _typeDD;
    private Checkbox _solidCB = new();
    private Checkbox _playerCB = new();

    // Tile row 2
    private Checkbox _passableCB = new() { IsChecked = true };
    private Checkbox _hazardCB = new();
    private Dropdown _costDD;
    private Dropdown _dmgTypeDD;
    private Dropdown _dmgTickDD;

    // Entity row 2
    private Dropdown _entityTypeDD;

    // Entity property fields
    private List<PropField> _propFields = new();
    private int _headerHeight = HeaderBaseH;

    // Focus tracking
    private TextInputField _focusedText;
    private NumericField _focusedNumeric;

    // Layout rects (computed by ComputeLayout)
    private Rectangle _typeRect, _solidRect, _playerRect;
    private Rectangle _passRect, _hazRect, _costRect, _dmgTypeRect, _dmgTickRect;
    private Rectangle _entityTypeRect;
    private int _row2Y;

    // Signals
    public bool WantsCreateMap { get; private set; }
    public bool WantsCreateDialogue { get; private set; }

    // Static data
    private static readonly string[] TypeItems = { "Tile", "Entity" };
    private static readonly string[] CostItems = { "0.5", "1.0", "1.5", "2.0", "3.0", "5.0" };
    private static readonly float[] CostValues = { 0.5f, 1.0f, 1.5f, 2.0f, 3.0f, 5.0f };
    private static readonly string[] DmgTypeItems = { "none", "fire", "poison", "spikes", "ice" };
    private static readonly string[] DmgTypeValues = { null, "fire", "poison", "spikes", "ice" };
    private static readonly string[] DmgTickItems = { "0", "1", "2", "5", "10", "25", "50" };
    private static readonly int[] DmgTickValues = { 0, 1, 2, 5, 10, 25, 50 };
    private static readonly string[] EntTypeItems = { "NPC", "Item", "Trap", "Trigger", "Interactable" };
    private static readonly string[] BehaviorItems = { "idle", "chase", "patrol", "chase_patrol" };

    private static readonly Dictionary<EntityType, string[]> Presets = new()
    {
        { EntityType.NPC, new[] { "dialogue", "health", "attack", "defense", "behavior", "aggro_range", "on_kill_set_flag", "on_kill_increment" } },
        { EntityType.Item, new[] { "heal", "on_collect_set_flag", "on_collect_increment" } },
        { EntityType.Trap, new[] { "damage", "health", "on_kill_set_flag", "on_kill_increment" } },
        { EntityType.Trigger, new[] { "target_map", "target_x", "target_y" } },
        { EntityType.Interactable, new[] { "dialogue" } },
    };

    private static readonly Dictionary<string, (int Min, int Max)> NumericSpecs = new()
    {
        { "health", (1, 9999) }, { "attack", (0, 999) }, { "defense", (0, 999) },
        { "aggro_range", (1, 50) }, { "damage", (1, 9999) }, { "heal", (1, 9999) },
        { "target_x", (0, 999) }, { "target_y", (0, 999) },
    };

    // Completion
    public bool IsComplete { get; private set; }
    public bool WasCancelled { get; private set; }
    public TileGroup Result { get; private set; }
    public string OriginalName => _originalName;
    public bool IsNew => _isNew;

    // --- PropField ---
    private enum PFK { Text, Numeric, Dropdown }

    private class PropField
    {
        public string Key;
        public PFK Kind;
        public TextInputField TF;
        public NumericField NF;
        public Dropdown DD;
        public Rectangle Bounds;

        public string GetValue() => Kind switch
        {
            PFK.Numeric => NF.Value.ToString(),
            PFK.Dropdown => DD?.SelectedItem == ProjectContext.CreateNewItem ? "" : (DD?.SelectedItem ?? ""),
            _ => TF?.Text ?? "",
        };
    }

    // --- Construction ---

    private GroupEditor()
    {
        _typeDD = new Dropdown(TypeItems, 0);
        _costDD = new Dropdown(CostItems, 1);
        _dmgTypeDD = new Dropdown(DmgTypeItems, 0);
        _dmgTickDD = new Dropdown(DmgTickItems, 0);
        _entityTypeDD = new Dropdown(EntTypeItems, 4);
    }

    public static GroupEditor ForNewGroup(IProjectContext context = null)
    {
        var ed = new GroupEditor
        {
            _isNew = true,
            _nameField = new TextInputField("", maxLength: 32),
            _projectContext = context,
        };
        ed.SetFocus(ed._nameField, null);
        ed.RebuildPropertyFields(new Dictionary<string, string>());
        return ed;
    }

    public static GroupEditor ForExistingGroup(TileGroup group, IProjectContext context = null)
    {
        var ed = new GroupEditor
        {
            _isNew = false,
            _originalName = group.Name,
            _nameField = new TextInputField(group.Name, maxLength: 32),
            _projectContext = context,
        };
        ed._typeDD = new Dropdown(TypeItems, group.Type == GroupType.Entity ? 1 : 0);
        ed._solidCB.IsChecked = group.IsSolid;
        ed._playerCB.IsChecked = group.IsPlayer;
        ed._passableCB.IsChecked = group.IsPassable;
        ed._hazardCB.IsChecked = group.IsHazardous;
        ed._costDD = new Dropdown(CostItems, FindClosestIndex(CostValues, group.MovementCost));
        ed._dmgTypeDD = new Dropdown(DmgTypeItems, FindDmgTypeIdx(group.DamageType));
        ed._dmgTickDD = new Dropdown(DmgTickItems, FindClosestIndex(DmgTickValues, group.DamagePerTick));
        ed._entityTypeDD = new Dropdown(EntTypeItems, (int)group.EntityType);
        ed.SetFocus(null, null);
        ed.RebuildPropertyFields(group.DefaultProperties);
        foreach (var s in group.Sprites) ed._selection.AddCell(s.Col, s.Row);
        return ed;
    }

    public void CenterOnSheet(ISpriteSheet sheet, Rectangle bounds)
    {
        var sa = GetSheetArea(bounds);
        _camera.CenterOn(sheet.Texture.Width, sheet.Texture.Height, sa.Width, sa.Height);
        _camera.Offset += new Vector2(sa.X, sa.Y);
    }

    public void OnTextInput(char c)
    {
        _focusedText?.HandleCharacter(c);
        _focusedNumeric?.HandleCharacter(c);
    }

    public void RefreshBrowseField(string key, string selectValue)
    {
        foreach (var pf in _propFields)
        {
            if (pf.Key == key && pf.Kind == PFK.Dropdown)
            {
                var items = GetDropdownItems(key);
                int idx = Array.IndexOf(items, selectValue);
                pf.DD.SetItems(items, idx >= 0 ? idx : 0);
                break;
            }
        }
    }

    // --- Update ---

    public void Update(EditorState state, MouseState mouse, MouseState prevMouse,
                       KeyboardState kb, KeyboardState prevKb,
                       Rectangle bounds, SpriteFont font, int screenW, int screenH)
    {
        if (state.Sheet == null) return;
        WantsCreateMap = false;
        WantsCreateDialogue = false;

        if (KP(kb, prevKb, Keys.Escape)) { IsComplete = true; WasCancelled = true; return; }
        if (KP(kb, prevKb, Keys.Enter) && !AnyDropdownOpen()) { TryConfirm(state); return; }
        if (KP(kb, prevKb, Keys.Tab) && !AnyDropdownOpen()) CycleFocus();

        bool noFocus = _focusedText == null && _focusedNumeric == null;
        bool noDD = !AnyDropdownOpen();

        // Keyboard shortcuts (only when no field focused and no dropdown open)
        if (noFocus && noDD)
        {
            if (KP(kb, prevKb, Keys.T))
            {
                int prev = _typeDD.SelectedIndex;
                _typeDD.SelectedIndex = prev == 0 ? 1 : 0;
                if (_typeDD.SelectedIndex == 0) _playerCB.IsChecked = false;
                if (_typeDD.SelectedIndex == 1 && prev == 0)
                    RebuildPropertyFields(CollectCurrentProperties());
            }
            if (KP(kb, prevKb, Keys.S)) _solidCB.IsChecked = !_solidCB.IsChecked;
            if (KP(kb, prevKb, Keys.P) && _typeDD.SelectedIndex == 1)
                _playerCB.IsChecked = !_playerCB.IsChecked;
        }

        // Route keys to focused field
        if (!noFocus)
        {
            foreach (var key in new[] { Keys.Back, Keys.Delete, Keys.Left, Keys.Right, Keys.Home, Keys.End })
            {
                if (KP(kb, prevKb, key))
                {
                    _focusedText?.HandleKey(key);
                    _focusedNumeric?.HandleKey(key);
                }
            }
        }

        // Compute layout for this frame
        ComputeLayout(bounds, font);

        bool isEntity = _typeDD.SelectedIndex == 1;
        bool clicked = mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released;
        var sheetArea = GetSheetArea(bounds);

        // Update header controls
        int prevType = _typeDD.SelectedIndex;
        if (_typeDD.Update(mouse, prevMouse, _typeRect, font, screenW, screenH))
        {
            if (_typeDD.SelectedIndex == 0) _playerCB.IsChecked = false;
            if (_typeDD.SelectedIndex == 1 && prevType == 0)
                RebuildPropertyFields(CollectCurrentProperties());
        }
        _solidCB.Update(mouse, prevMouse, _solidRect);
        if (isEntity) _playerCB.Update(mouse, prevMouse, _playerRect);

        // Row 2 controls
        if (!isEntity)
        {
            _passableCB.Update(mouse, prevMouse, _passRect);
            _hazardCB.Update(mouse, prevMouse, _hazRect);
            _costDD.Update(mouse, prevMouse, _costRect, font, screenW, screenH);
            _dmgTypeDD.Update(mouse, prevMouse, _dmgTypeRect, font, screenW, screenH);
            _dmgTickDD.Update(mouse, prevMouse, _dmgTickRect, font, screenW, screenH);
        }
        else
        {
            int prevET = _entityTypeDD.SelectedIndex;
            if (_entityTypeDD.Update(mouse, prevMouse, _entityTypeRect, font, screenW, screenH))
            {
                if (_entityTypeDD.SelectedIndex != prevET)
                    RebuildPropertyFields(CollectCurrentProperties());
            }
        }

        // Update property field dropdowns
        foreach (var pf in _propFields)
        {
            if (pf.Kind == PFK.Dropdown)
            {
                if (pf.DD.Update(mouse, prevMouse, pf.Bounds, font, screenW, screenH))
                {
                    if (pf.DD.SelectedItem == ProjectContext.CreateNewItem)
                    {
                        if (pf.Key == "target_map") WantsCreateMap = true;
                        else if (pf.Key is "dialogue" or "dialogue_id") WantsCreateDialogue = true;
                    }
                }
            }
        }

        // Click-to-focus for text/numeric fields
        if (clicked)
        {
            bool focusHandled = false;
            foreach (var pf in _propFields)
            {
                if (pf.Bounds.Contains(mouse.X, mouse.Y) && pf.Kind is PFK.Text or PFK.Numeric)
                {
                    SetFocus(pf.Kind == PFK.Text ? pf.TF : null, pf.Kind == PFK.Numeric ? pf.NF : null);
                    focusHandled = true;
                    break;
                }
            }
            if (!focusHandled)
            {
                var nameRect = new Rectangle(bounds.X + LayoutConstants.GroupEditorNameFieldX, bounds.Y + 4,
                    LayoutConstants.GroupEditorNameFieldWidth, LayoutConstants.GroupEditorNameFieldHeight);
                if (nameRect.Contains(mouse.X, mouse.Y))
                    SetFocus(_nameField, null);
                else if (sheetArea.Contains(mouse.X, mouse.Y))
                    SetFocus(null, null);
            }
        }

        // Middle-mouse pan
        if (mouse.MiddleButton == ButtonState.Pressed)
        {
            if (!_isPanning) { _isPanning = true; _panStart = new(mouse.X, mouse.Y); _panOffsetStart = _camera.Offset; }
            else _camera.Offset = _panOffsetStart + new Vector2(mouse.X - _panStart.X, mouse.Y - _panStart.Y);
        }
        else _isPanning = false;

        if (!sheetArea.Contains(mouse.X, mouse.Y)) return;

        // Scroll zoom
        int scroll = mouse.ScrollWheelValue - prevMouse.ScrollWheelValue;
        if (scroll != 0) _camera.AdjustZoom(scroll > 0 ? 1 : -1, sheetArea.Width, sheetArea.Height);

        // Left-click sprite selection
        if (clicked)
        {
            var wp = _camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));
            var (col, row) = state.Sheet.PixelToGrid(wp.X, wp.Y);
            if (state.Sheet.InBounds(col, row))
            {
                bool shift = kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift);
                bool ctrl = kb.IsKeyDown(Keys.LeftControl) || kb.IsKeyDown(Keys.RightControl);
                _selection.Select(col, row, shift, ctrl);
            }
        }
    }

    // --- Draw ---

    public void Draw(SpriteBatch sb, SpriteFont font, EditorState state,
                     Renderer r, Rectangle bounds, GameTime gt)
    {
        if (state.Sheet == null) return;
        var sheet = state.Sheet;
        bool isEntity = _typeDD.SelectedIndex == 1;

        ComputeLayout(bounds, font);

        // Background
        r.DrawRect(sb, bounds, BgColor);

        // Sheet area
        var sheetArea = GetSheetArea(bounds);
        int zoom = _camera.Zoom;
        var ssp = _camera.WorldToScreen(Vector2.Zero);
        sb.Draw(sheet.Texture, new Rectangle((int)ssp.X, (int)ssp.Y,
            sheet.Texture.Width * zoom, sheet.Texture.Height * zoom), Color.White);

        // Grid
        for (int c = 0; c <= sheet.Cols; c++)
        {
            var p = _camera.WorldToScreen(new Vector2(c * sheet.StrideX, 0));
            r.DrawRect(sb, new Rectangle((int)p.X, (int)ssp.Y, 1, sheet.Rows * sheet.StrideY * zoom), GridColor);
        }
        for (int rw = 0; rw <= sheet.Rows; rw++)
        {
            var p = _camera.WorldToScreen(new Vector2(0, rw * sheet.StrideY));
            r.DrawRect(sb, new Rectangle((int)ssp.X, (int)p.Y, sheet.Cols * sheet.StrideX * zoom, 1), GridColor);
        }

        // Selection highlight
        var cells = _selection.GetSelectedCells();
        foreach (var (col, row) in cells)
        {
            if (!sheet.InBounds(col, row)) continue;
            var cs = _camera.WorldToScreen(new Vector2(col * sheet.StrideX, row * sheet.StrideY));
            var cr = new Rectangle((int)cs.X, (int)cs.Y, sheet.TileWidth * zoom, sheet.TileHeight * zoom);
            r.DrawRect(sb, cr, SelFill);
            r.DrawRectOutline(sb, cr, SelBorder, 1);
        }
        if (cells.Count > 0)
        {
            string ct = $"{_selection.Count} sprite{(_selection.Count != 1 ? "s" : "")} selected";
            sb.DrawString(font, ct, new Vector2(bounds.X + 8, bounds.Bottom - font.LineSpacing - 8),
                LayoutConstants.GroupEditorSpriteCountColor);
        }

        // Header background
        r.DrawRect(sb, new Rectangle(bounds.X, bounds.Y, bounds.Width, _headerHeight), HeaderBg);

        // Row 1: Name field
        var nameR = new Rectangle(bounds.X + LayoutConstants.GroupEditorNameFieldX, bounds.Y + 4,
            LayoutConstants.GroupEditorNameFieldWidth, LayoutConstants.GroupEditorNameFieldHeight);
        _nameField.Draw(sb, font, r, nameR, gt);

        // Row 1: Type dropdown
        _typeDD.Draw(sb, font, r, _typeRect);

        // Row 1: Solid checkbox + label
        _solidCB.Draw(sb, r, _solidRect);
        DrawLabel(sb, font, "Solid", _solidRect.Right + 2, _solidRect.Y, _solidRect.Height);

        // Row 1: Player checkbox + label (entity only)
        if (isEntity)
        {
            _playerCB.Draw(sb, r, _playerRect);
            DrawLabel(sb, font, "Player", _playerRect.Right + 2, _playerRect.Y, _playerRect.Height);
        }

        // Row 2
        int ddH = 22;
        if (!isEntity)
        {
            _passableCB.Draw(sb, r, _passRect);
            DrawLabel(sb, font, "Pass", _passRect.Right + 1, _row2Y, ddH);

            _hazardCB.Draw(sb, r, _hazRect);
            DrawLabel(sb, font, "Hazard", _hazRect.Right + 1, _row2Y, ddH);

            DrawLabel(sb, font, "Cost:", _costRect.X - (int)font.MeasureString("Cost:").X - 2, _row2Y, ddH);
            _costDD.Draw(sb, font, r, _costRect);

            DrawLabel(sb, font, "Dmg:", _dmgTypeRect.X - (int)font.MeasureString("Dmg:").X - 2, _row2Y, ddH);
            _dmgTypeDD.Draw(sb, font, r, _dmgTypeRect);

            DrawLabel(sb, font, "Hit:", _dmgTickRect.X - (int)font.MeasureString("Hit:").X - 2, _row2Y, ddH);
            _dmgTickDD.Draw(sb, font, r, _dmgTickRect);
        }
        else
        {
            DrawLabel(sb, font, "Type:", _entityTypeRect.X - (int)font.MeasureString("Type:").X - 4, _row2Y, ddH);
            _entityTypeDD.Draw(sb, font, r, _entityTypeRect);
        }

        // Entity property fields
        if (isEntity && _propFields.Count > 0)
        {
            foreach (var pf in _propFields)
            {
                DrawLabel(sb, font, pf.Key + ":", bounds.X + 8,
                    pf.Bounds.Y, pf.Bounds.Height);
                switch (pf.Kind)
                {
                    case PFK.Text: pf.TF.Draw(sb, font, r, pf.Bounds, gt); break;
                    case PFK.Numeric: pf.NF.Draw(sb, font, r, pf.Bounds, gt); break;
                    case PFK.Dropdown: pf.DD.Draw(sb, font, r, pf.Bounds); break;
                }
            }
        }

        // Hints
        string hints = isEntity
            ? "[Enter] Save  [Esc] Cancel  [Tab] Fields"
            : "[Enter] Save  [Esc] Cancel  Ctrl+Click multi";
        var hs = font.MeasureString(hints);
        sb.DrawString(font, hints, new Vector2(bounds.Right - hs.X - 10,
            _row2Y + (ddH - font.LineSpacing) / 2), HintColor);

        // Dropdown popups (z-ordering: drawn LAST)
        _typeDD.DrawPopup(sb, font, r);
        if (!isEntity)
        {
            _costDD.DrawPopup(sb, font, r);
            _dmgTypeDD.DrawPopup(sb, font, r);
            _dmgTickDD.DrawPopup(sb, font, r);
        }
        else
        {
            _entityTypeDD.DrawPopup(sb, font, r);
            foreach (var pf in _propFields)
                if (pf.Kind == PFK.Dropdown) pf.DD.DrawPopup(sb, font, r);
        }
    }

    // --- Layout ---

    private void ComputeLayout(Rectangle bounds, SpriteFont font)
    {
        bool isEntity = _typeDD.SelectedIndex == 1;
        int ddH = 22;
        int r1Y = bounds.Y + 4;

        _typeRect = new Rectangle(bounds.X + LayoutConstants.GroupEditorTypeButtonsX, r1Y, 100, ddH);
        _solidRect = new Rectangle(_typeRect.Right + 8, r1Y, CbW, ddH);
        int plX = _solidRect.Right + (int)font.MeasureString("Solid").X + 8;
        _playerRect = new Rectangle(plX, r1Y, CbW, ddH);

        _row2Y = bounds.Y + HeaderBaseH + 2;
        if (!isEntity)
        {
            int px = bounds.X + 8;
            _passRect = new Rectangle(px, _row2Y, CbW, ddH);
            px = _passRect.Right + (int)font.MeasureString("Pass").X + 6;
            _hazRect = new Rectangle(px, _row2Y, CbW, ddH);
            px = _hazRect.Right + (int)font.MeasureString("Hazard").X + 8;
            px += (int)font.MeasureString("Cost:").X + 2;
            _costRect = new Rectangle(px, _row2Y, 70, ddH);
            px = _costRect.Right + 4 + (int)font.MeasureString("Dmg:").X + 2;
            _dmgTypeRect = new Rectangle(px, _row2Y, 90, ddH);
            px = _dmgTypeRect.Right + 4 + (int)font.MeasureString("Hit:").X + 2;
            _dmgTickRect = new Rectangle(px, _row2Y, 60, ddH);
        }
        else
        {
            int px = bounds.X + 8 + (int)font.MeasureString("Type:").X + 4;
            _entityTypeRect = new Rectangle(px, _row2Y, 120, ddH);
        }

        // Property field bounds
        if (isEntity && _propFields.Count > 0)
        {
            int baseH = HeaderBaseH + PropRowH;
            int startY = bounds.Y + baseH + 2;
            int fieldX = bounds.X + 8 + PropLabelW + 4;
            int fieldW = Math.Min(DropW, bounds.Width - PropLabelW - 20);
            for (int i = 0; i < _propFields.Count; i++)
            {
                int w = _propFields[i].Kind == PFK.Numeric ? NumW : fieldW;
                _propFields[i].Bounds = new Rectangle(fieldX, startY + i * PropRowH, w, PropFieldH);
            }
        }

        // Header height
        int baseHeaderH = HeaderBaseH + PropRowH;
        int propH = isEntity && _propFields.Count > 0 ? _propFields.Count * PropRowH + 4 : 0;
        _headerHeight = baseHeaderH + propH;
    }

    // --- Property field management ---

    private void RebuildPropertyFields(Dictionary<string, string> existing)
    {
        _propFields.Clear();
        if (_typeDD.SelectedIndex != 1) return;

        var et = (EntityType)_entityTypeDD.SelectedIndex;
        var keys = Presets.TryGetValue(et, out var pk) ? pk : Array.Empty<string>();

        foreach (var key in keys)
        {
            string val = existing.TryGetValue(key, out var v) ? v : "";
            _propFields.Add(CreatePropField(key, val));
        }
        foreach (var kvp in existing)
        {
            if (!keys.Contains(kvp.Key))
                _propFields.Add(CreatePropField(kvp.Key, kvp.Value));
        }
    }

    private PropField CreatePropField(string key, string value)
    {
        if (NumericSpecs.TryGetValue(key, out var spec))
        {
            int v = int.TryParse(value, out int parsed) ? parsed : spec.Min;
            return new PropField { Key = key, Kind = PFK.Numeric, NF = new NumericField(v, spec.Min, spec.Max) };
        }
        if (key == "behavior")
        {
            int idx = Array.IndexOf(BehaviorItems, value);
            return new PropField { Key = key, Kind = PFK.Dropdown, DD = new Dropdown(BehaviorItems, Math.Max(0, idx)) };
        }
        if (key == "target_map")
        {
            var items = GetDropdownItems(key);
            int idx = Array.IndexOf(items, value);
            return new PropField { Key = key, Kind = PFK.Dropdown, DD = new Dropdown(items, Math.Max(0, idx)) };
        }
        if (key is "dialogue" or "dialogue_id")
        {
            var items = GetDropdownItems(key);
            int idx = Array.IndexOf(items, value);
            return new PropField { Key = key, Kind = PFK.Dropdown, DD = new Dropdown(items, Math.Max(0, idx)) };
        }
        return new PropField { Key = key, Kind = PFK.Text, TF = new TextInputField(value, maxLength: 512) };
    }

    private string[] GetDropdownItems(string key)
    {
        if (_projectContext == null) return new[] { ProjectContext.CreateNewItem };
        if (key == "target_map") return _projectContext.GetAvailableMaps();
        if (key is "dialogue" or "dialogue_id") return _projectContext.GetAvailableDialogues();
        return Array.Empty<string>();
    }

    private Dictionary<string, string> CollectCurrentProperties()
    {
        var result = new Dictionary<string, string>();
        foreach (var pf in _propFields)
        {
            string val = pf.GetValue().Trim();
            if (!string.IsNullOrEmpty(val)) result[pf.Key] = val;
        }
        return result;
    }

    private void TryConfirm(EditorState state)
    {
        string name = _nameField.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;
        var cells = _selection.GetSelectedCells();
        if (cells.Count == 0) return;

        var sprites = new List<SpriteRef>();
        foreach (var (col, row) in cells)
            if (state.Sheet.InBounds(col, row))
                sprites.Add(new SpriteRef { Col = col, Row = row });
        if (sprites.Count == 0) return;

        bool isEntity = _typeDD.SelectedIndex == 1;

        Result = new TileGroup
        {
            Name = name,
            Type = isEntity ? GroupType.Entity : GroupType.Tile,
            Sprites = sprites,
            IsSolid = _solidCB.IsChecked,
            IsPlayer = _playerCB.IsChecked,
            IsPassable = _passableCB.IsChecked,
            IsHazardous = _hazardCB.IsChecked,
            MovementCost = CostValues[_costDD.SelectedIndex],
            DamageType = DmgTypeValues[_dmgTypeDD.SelectedIndex],
            DamagePerTick = DmgTickValues[_dmgTickDD.SelectedIndex],
            EntityType = (EntityType)_entityTypeDD.SelectedIndex,
            DefaultProperties = isEntity ? CollectCurrentProperties() : new(),
        };
        IsComplete = true;
        WasCancelled = false;
    }

    // --- Focus ---

    private void SetFocus(TextInputField tf, NumericField nf)
    {
        if (_focusedText != null) _focusedText.IsFocused = false;
        if (_focusedNumeric != null) { _focusedNumeric.ClampValue(); _focusedNumeric.IsFocused = false; }
        _focusedText = tf;
        _focusedNumeric = nf;
        if (_focusedText != null) _focusedText.IsFocused = true;
        if (_focusedNumeric != null) _focusedNumeric.IsFocused = true;
    }

    private void CycleFocus()
    {
        var focusable = new List<(TextInputField T, NumericField N)> { (_nameField, null) };
        if (_typeDD.SelectedIndex == 1)
        {
            foreach (var pf in _propFields)
            {
                if (pf.Kind == PFK.Text) focusable.Add((pf.TF, null));
                else if (pf.Kind == PFK.Numeric) focusable.Add((null, pf.NF));
            }
        }

        int cur = -1;
        for (int i = 0; i < focusable.Count; i++)
        {
            if (focusable[i].T == _focusedText && _focusedText != null) { cur = i; break; }
            if (focusable[i].N == _focusedNumeric && _focusedNumeric != null) { cur = i; break; }
        }

        if (cur < 0) SetFocus(focusable[0].T, focusable[0].N);
        else if (cur >= focusable.Count - 1) SetFocus(null, null);
        else { var next = focusable[cur + 1]; SetFocus(next.T, next.N); }
    }

    private bool AnyDropdownOpen()
    {
        if (_typeDD.IsOpen || _costDD.IsOpen || _dmgTypeDD.IsOpen ||
            _dmgTickDD.IsOpen || _entityTypeDD.IsOpen)
            return true;
        return _propFields.Any(pf => pf.Kind == PFK.Dropdown && pf.DD.IsOpen);
    }

    // --- Helpers ---

    private Rectangle GetSheetArea(Rectangle bounds) =>
        new(bounds.X, bounds.Y + _headerHeight, bounds.Width, bounds.Height - _headerHeight);

    private static void DrawLabel(SpriteBatch sb, SpriteFont font, string text, int x, int y, int h)
    {
        sb.DrawString(font, text, new Vector2(x, y + (h - font.LineSpacing) / 2f), LabelColor);
    }

    private static int FindClosestIndex(float[] values, float target)
    {
        int best = 0; float bd = Math.Abs(values[0] - target);
        for (int i = 1; i < values.Length; i++) { float d = Math.Abs(values[i] - target); if (d < bd) { best = i; bd = d; } }
        return best;
    }

    private static int FindClosestIndex(int[] values, int target)
    {
        int best = 0; int bd = Math.Abs(values[0] - target);
        for (int i = 1; i < values.Length; i++) { int d = Math.Abs(values[i] - target); if (d < bd) { best = i; bd = d; } }
        return best;
    }

    private static int FindDmgTypeIdx(string dt)
    {
        if (string.IsNullOrEmpty(dt)) return 0;
        for (int i = 1; i < DmgTypeValues.Length; i++)
            if (string.Equals(DmgTypeValues[i], dt, StringComparison.OrdinalIgnoreCase)) return i;
        return 0;
    }

    private static bool KP(KeyboardState c, KeyboardState p, Keys k) => c.IsKeyDown(k) && p.IsKeyUp(k);
}
