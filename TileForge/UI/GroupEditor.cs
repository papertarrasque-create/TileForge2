using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Data;
using TileForge.Editor;

namespace TileForge.UI;

public class GroupEditor
{
    private static readonly Color BackgroundColor = LayoutConstants.GroupEditorBackground;
    private static readonly Color HeaderColor = LayoutConstants.GroupEditorHeaderColor;
    private static readonly Color GridColor = LayoutConstants.GroupEditorGridColor;
    private static readonly Color SelectionFill = LayoutConstants.GroupEditorSelectionFill;
    private static readonly Color SelectionBorder = LayoutConstants.GroupEditorSelectionBorder;
    private static readonly Color HintColor = LayoutConstants.GroupEditorHintColor;
    private static readonly Color TypeActiveColor = LayoutConstants.GroupEditorTypeActiveColor;
    private static readonly Color TypeInactiveColor = LayoutConstants.GroupEditorTypeInactiveColor;

    private const int HeaderBaseHeight = LayoutConstants.GroupEditorHeaderBaseHeight;
    private int _headerHeight = HeaderBaseHeight;
    private const int TypeButtonWidth = LayoutConstants.GroupEditorTypeButtonWidth;

    // Mode
    private bool _isNew;
    private string _originalName;

    // Components
    private Camera _camera = new();
    private Selection _selection = new();
    private TextInputField _nameField;
    private GroupType _type = GroupType.Tile;

    // Pan tracking
    private bool _isPanning;
    private Point _panStart;
    private Vector2 _panOffsetStart;

    // Type button rects (computed in Draw, hit-tested in Update via stored values)
    private Rectangle _tileButtonRect;
    private Rectangle _entityButtonRect;

    // Property toggles
    private bool _isSolid;
    private bool _isPlayer;
    private Rectangle _solidButtonRect;
    private Rectangle _playerButtonRect;

    // Completion
    public bool IsComplete { get; private set; }
    public bool WasCancelled { get; private set; }
    public TileGroup Result { get; private set; }
    public string OriginalName => _originalName;
    public bool IsNew => _isNew;

    private GroupEditor() { }

    public static GroupEditor ForNewGroup()
    {
        var editor = new GroupEditor
        {
            _isNew = true,
            _nameField = new TextInputField("", maxLength: 32),
        };
        editor._nameField.IsFocused = true;
        return editor;
    }

    public static GroupEditor ForExistingGroup(TileGroup group)
    {
        var editor = new GroupEditor
        {
            _isNew = false,
            _originalName = group.Name,
            _nameField = new TextInputField(group.Name, maxLength: 32),
            _type = group.Type,
            _isSolid = group.IsSolid,
            _isPlayer = group.IsPlayer,
        };
        editor._nameField.IsFocused = false;

        // Pre-populate selection with exact sprite positions
        foreach (var s in group.Sprites)
            editor._selection.AddCell(s.Col, s.Row);

        return editor;
    }

    public void CenterOnSheet(ISpriteSheet sheet, Rectangle bounds)
    {
        var sheetArea = GetSheetArea(bounds);
        _camera.CenterOn(sheet.Texture.Width, sheet.Texture.Height, sheetArea.Width, sheetArea.Height);
        // Offset to account for sheet area position on screen
        _camera.Offset += new Vector2(sheetArea.X, sheetArea.Y);
    }

    public void OnTextInput(char character)
    {
        if (_nameField.IsFocused)
            _nameField.HandleCharacter(character);
    }

    public void Update(EditorState state, MouseState mouse, MouseState prevMouse,
                       KeyboardState keyboard, KeyboardState prevKeyboard,
                       Rectangle bounds)
    {
        if (state.Sheet == null) return;

        // Escape = cancel
        if (KeyPressed(keyboard, prevKeyboard, Keys.Escape))
        {
            IsComplete = true;
            WasCancelled = true;
            return;
        }

        // Enter = confirm
        if (KeyPressed(keyboard, prevKeyboard, Keys.Enter))
        {
            TryConfirm(state);
            return;
        }

        // Tab toggles focus
        if (KeyPressed(keyboard, prevKeyboard, Keys.Tab))
            _nameField.IsFocused = !_nameField.IsFocused;

        // T toggles type (when name field not focused)
        if (!_nameField.IsFocused && KeyPressed(keyboard, prevKeyboard, Keys.T))
        {
            _type = _type == GroupType.Tile ? GroupType.Entity : GroupType.Tile;
            if (_type == GroupType.Tile) _isPlayer = false;
        }

        // S toggles solid, P toggles player (when name field not focused)
        if (!_nameField.IsFocused && KeyPressed(keyboard, prevKeyboard, Keys.S))
            _isSolid = !_isSolid;
        if (!_nameField.IsFocused && KeyPressed(keyboard, prevKeyboard, Keys.P) && _type == GroupType.Entity)
            _isPlayer = !_isPlayer;

        // Route keys to name field when focused
        if (_nameField.IsFocused)
        {
            Keys[] fieldKeys = { Keys.Back, Keys.Delete, Keys.Left, Keys.Right, Keys.Home, Keys.End };
            foreach (var key in fieldKeys)
            {
                if (KeyPressed(keyboard, prevKeyboard, key))
                    _nameField.HandleKey(key);
            }
        }

        var headerRect = new Rectangle(bounds.X, bounds.Y, bounds.Width, _headerHeight);
        var sheetArea = GetSheetArea(bounds);

        // Type button clicks in header
        if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released)
        {
            if (_tileButtonRect.Contains(mouse.X, mouse.Y))
            {
                _type = GroupType.Tile;
                _isPlayer = false;
            }
            else if (_entityButtonRect.Contains(mouse.X, mouse.Y))
                _type = GroupType.Entity;
            else if (_solidButtonRect.Contains(mouse.X, mouse.Y))
                _isSolid = !_isSolid;
            else if (_playerButtonRect.Contains(mouse.X, mouse.Y) && _type == GroupType.Entity)
                _isPlayer = !_isPlayer;

            // Click in header focuses name field, click in sheet area unfocuses it
            if (headerRect.Contains(mouse.X, mouse.Y))
                _nameField.IsFocused = true;
            else if (sheetArea.Contains(mouse.X, mouse.Y))
                _nameField.IsFocused = false;
        }

        // Middle-mouse pan (allow continuation even outside bounds)
        if (mouse.MiddleButton == ButtonState.Pressed)
        {
            if (!_isPanning)
            {
                _isPanning = true;
                _panStart = new Point(mouse.X, mouse.Y);
                _panOffsetStart = _camera.Offset;
            }
            else
            {
                _camera.Offset = _panOffsetStart + new Vector2(mouse.X - _panStart.X, mouse.Y - _panStart.Y);
            }
        }
        else
        {
            _isPanning = false;
        }

        if (!sheetArea.Contains(mouse.X, mouse.Y))
            return;

        // Scroll zoom
        int scrollDelta = mouse.ScrollWheelValue - prevMouse.ScrollWheelValue;
        if (scrollDelta != 0)
            _camera.AdjustZoom(scrollDelta > 0 ? 1 : -1, sheetArea.Width, sheetArea.Height);

        // Left-click sprite selection
        if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released)
        {
            var worldPos = _camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));
            var (col, row) = state.Sheet.PixelToGrid(worldPos.X, worldPos.Y);

            if (state.Sheet.InBounds(col, row))
            {
                bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
                bool ctrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
                _selection.Select(col, row, shift, ctrl);
            }
        }
    }

    private void TryConfirm(EditorState state)
    {
        string name = _nameField.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        var selectedCells = _selection.GetSelectedCells();
        if (selectedCells.Count == 0) return;

        var sprites = new List<SpriteRef>();
        foreach (var (col, row) in selectedCells)
        {
            if (state.Sheet.InBounds(col, row))
                sprites.Add(new SpriteRef { Col = col, Row = row });
        }

        if (sprites.Count == 0) return;

        Result = new TileGroup
        {
            Name = name,
            Type = _type,
            Sprites = sprites,
            IsSolid = _isSolid,
            IsPlayer = _isPlayer,
        };

        IsComplete = true;
        WasCancelled = false;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                     Renderer renderer, Rectangle bounds, GameTime gameTime)
    {
        if (state.Sheet == null) return;

        var sheet = state.Sheet;

        // Pre-compute button layout and header height
        int typeX = bounds.X + LayoutConstants.GroupEditorTypeButtonsX;
        int typeY = bounds.Y + 4;
        _tileButtonRect = new Rectangle(typeX, typeY, TypeButtonWidth, LayoutConstants.GroupEditorNameFieldHeight);
        _entityButtonRect = new Rectangle(typeX + TypeButtonWidth + 4, typeY, TypeButtonWidth, LayoutConstants.GroupEditorNameFieldHeight);
        int propX = _entityButtonRect.Right + 12;
        _solidButtonRect = new Rectangle(propX, typeY, TypeButtonWidth, LayoutConstants.GroupEditorNameFieldHeight);
        _playerButtonRect = new Rectangle(propX + TypeButtonWidth + 4, typeY, TypeButtonWidth, LayoutConstants.GroupEditorNameFieldHeight);

        string hints = _type == GroupType.Entity
            ? "[Enter] Save  [Esc] Cancel  [T] Type  [S] Solid  [P] Player  Ctrl+Click multi"
            : "[Enter] Save  [Esc] Cancel  [T] Type  [S] Solid  Ctrl+Click multi";
        var hintsSize = font.MeasureString(hints);
        int lastButtonRight = _type == GroupType.Entity ? _playerButtonRect.Right : _solidButtonRect.Right;
        bool hintsOnFirstRow = lastButtonRight + 16 + hintsSize.X + 10 <= bounds.Right;
        _headerHeight = hintsOnFirstRow ? HeaderBaseHeight : HeaderBaseHeight + 4 + (int)font.LineSpacing;

        // Background
        renderer.DrawRect(spriteBatch, bounds, BackgroundColor);

        // --- Sheet area (drawn first so header paints over any overflow) ---
        var sheetArea = GetSheetArea(bounds);
        int zoom = _camera.Zoom;

        // Draw full spritesheet texture
        var sheetScreenPos = _camera.WorldToScreen(Vector2.Zero);
        var sheetDestRect = new Rectangle(
            (int)sheetScreenPos.X, (int)sheetScreenPos.Y,
            sheet.Texture.Width * zoom, sheet.Texture.Height * zoom);
        spriteBatch.Draw(sheet.Texture, sheetDestRect, Color.White);

        // Grid lines
        for (int col = 0; col <= sheet.Cols; col++)
        {
            var pos = _camera.WorldToScreen(new Vector2(col * sheet.StrideX, 0));
            int lineH = sheet.Rows * sheet.StrideY * zoom;
            renderer.DrawRect(spriteBatch, new Rectangle((int)pos.X, (int)sheetScreenPos.Y, 1, lineH), GridColor);
        }
        for (int row = 0; row <= sheet.Rows; row++)
        {
            var pos = _camera.WorldToScreen(new Vector2(0, row * sheet.StrideY));
            int lineW = sheet.Cols * sheet.StrideX * zoom;
            renderer.DrawRect(spriteBatch, new Rectangle((int)sheetScreenPos.X, (int)pos.Y, lineW, 1), GridColor);
        }

        // Selection highlight
        var selectedCells = _selection.GetSelectedCells();
        foreach (var (col, row) in selectedCells)
        {
            if (!sheet.InBounds(col, row)) continue;
            var cellScreen = _camera.WorldToScreen(new Vector2(col * sheet.StrideX, row * sheet.StrideY));
            var cellRect = new Rectangle(
                (int)cellScreen.X, (int)cellScreen.Y,
                sheet.TileWidth * zoom, sheet.TileHeight * zoom);
            renderer.DrawRect(spriteBatch, cellRect, SelectionFill);
            renderer.DrawRectOutline(spriteBatch, cellRect, SelectionBorder, 1);
        }

        if (selectedCells.Count > 0)
        {
            int count = _selection.Count;
            string countText = $"{count} sprite{(count != 1 ? "s" : "")} selected";
            spriteBatch.DrawString(font, countText,
                new Vector2(bounds.X + 8, bounds.Bottom - font.LineSpacing - 8),
                LayoutConstants.GroupEditorSpriteCountColor);
        }

        // --- Header (drawn last so it stays visible over zoomed spritesheet) ---
        var headerRect = new Rectangle(bounds.X, bounds.Y, bounds.Width, _headerHeight);
        renderer.DrawRect(spriteBatch, headerRect, HeaderColor);

        // Name field
        var nameRect = new Rectangle(bounds.X + LayoutConstants.GroupEditorNameFieldX, bounds.Y + 4, LayoutConstants.GroupEditorNameFieldWidth, LayoutConstants.GroupEditorNameFieldHeight);
        _nameField.Draw(spriteBatch, font, renderer, nameRect, gameTime);

        // Type toggle buttons (rects pre-computed above)
        renderer.DrawRect(spriteBatch, _tileButtonRect,
            _type == GroupType.Tile ? TypeActiveColor : TypeInactiveColor);
        renderer.DrawRect(spriteBatch, _entityButtonRect,
            _type == GroupType.Entity ? TypeActiveColor : TypeInactiveColor);

        string tileLabel = "Tile";
        string entityLabel = "Entity";
        var tileLabelSize = font.MeasureString(tileLabel);
        var entityLabelSize = font.MeasureString(entityLabel);
        spriteBatch.DrawString(font, tileLabel,
            new Vector2(_tileButtonRect.X + (_tileButtonRect.Width - tileLabelSize.X) / 2,
                         _tileButtonRect.Y + (_tileButtonRect.Height - tileLabelSize.Y) / 2),
            Color.White);
        spriteBatch.DrawString(font, entityLabel,
            new Vector2(_entityButtonRect.X + (_entityButtonRect.Width - entityLabelSize.X) / 2,
                         _entityButtonRect.Y + (_entityButtonRect.Height - entityLabelSize.Y) / 2),
            Color.White);

        // Property toggle buttons (rects pre-computed above)
        renderer.DrawRect(spriteBatch, _solidButtonRect,
            _isSolid ? TypeActiveColor : TypeInactiveColor);
        string solidLabel = "Solid";
        var solidLabelSize = font.MeasureString(solidLabel);
        spriteBatch.DrawString(font, solidLabel,
            new Vector2(_solidButtonRect.X + (_solidButtonRect.Width - solidLabelSize.X) / 2,
                         _solidButtonRect.Y + (_solidButtonRect.Height - solidLabelSize.Y) / 2),
            Color.White);

        if (_type == GroupType.Entity)
        {
            renderer.DrawRect(spriteBatch, _playerButtonRect,
                _isPlayer ? TypeActiveColor : TypeInactiveColor);
            string playerLabel = "Player";
            var playerLabelSize = font.MeasureString(playerLabel);
            spriteBatch.DrawString(font, playerLabel,
                new Vector2(_playerButtonRect.X + (_playerButtonRect.Width - playerLabelSize.X) / 2,
                             _playerButtonRect.Y + (_playerButtonRect.Height - playerLabelSize.Y) / 2),
                Color.White);
        }

        // Hints (layout pre-computed above)
        if (hintsOnFirstRow)
        {
            spriteBatch.DrawString(font, hints,
                new Vector2(bounds.Right - hintsSize.X - 10, bounds.Y + (HeaderBaseHeight - font.LineSpacing) / 2),
                HintColor);
        }
        else
        {
            spriteBatch.DrawString(font, hints,
                new Vector2(bounds.Right - hintsSize.X - 10, bounds.Y + HeaderBaseHeight + 2),
                HintColor);
        }
    }

    private Rectangle GetSheetArea(Rectangle bounds)
    {
        return new Rectangle(bounds.X, bounds.Y + _headerHeight, bounds.Width, bounds.Height - _headerHeight);
    }

    private static bool KeyPressed(KeyboardState current, KeyboardState prev, Keys key)
        => current.IsKeyDown(key) && prev.IsKeyUp(key);
}
