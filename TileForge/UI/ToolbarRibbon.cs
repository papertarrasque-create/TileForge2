using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Editor;

namespace TileForge.UI;

public class ToolbarRibbon
{
    public const int Height = LayoutConstants.ToolbarRibbonHeight;

    private static readonly Color BackgroundColor = LayoutConstants.ToolbarBackground;
    private static readonly Color ButtonColor = LayoutConstants.ToolbarButtonColor;
    private static readonly Color ButtonActiveColor = LayoutConstants.ToolbarButtonActiveColor;
    private static readonly Color ButtonHoverColor = LayoutConstants.ToolbarButtonHoverColor;
    private static readonly Color ButtonDisabledColor = LayoutConstants.ToolbarButtonDisabledColor;
    private static readonly Color IconColor = LayoutConstants.ToolbarIconColor;
    private static readonly Color IconDimColor = LayoutConstants.ToolbarIconDimColor;
    private static readonly Color SeparatorColor = LayoutConstants.ToolbarSeparatorColor;
    private static readonly Color PlayModeTextColor = LayoutConstants.ToolbarPlayModeTextColor;

    private const int BtnSize = LayoutConstants.ToolbarButtonSize;
    private const int BtnPad = LayoutConstants.ToolbarButtonPadding;
    private const int GroupGap = 12;
    private const int SeparatorWidth = 1;

    // Tool names must match ITool.Name for active-state highlight
    private static readonly string[] ToolNames = { "Brush", "Eraser", "Fill", "Entity", "Picker", "Selection" };

    // Button groups: [New, Open, Save] | [Undo, Redo] | [6 tools] | [Play] | [Export]
    // Total: 3 + 2 + 6 + 1 + 1 = 13 buttons, 4 separators
    private const int TotalButtons = 13;

    private Rectangle[] _buttonRects = new Rectangle[TotalButtons];
    private int _hoverIndex = -1;
    private int _topOffset; // Y offset (MenuBar.Height in editor mode, 0 in play mode)

    // Tooltip state
    private int _tooltipIndex = -1;
    private double _tooltipTimer;
    private const double TooltipDelay = 0.5;

    // Signals
    public bool WantsNew { get; private set; }
    public bool WantsOpen { get; private set; }
    public bool WantsSave { get; private set; }
    public bool WantsUndo { get; private set; }
    public bool WantsRedo { get; private set; }
    public int WantsToolIndex { get; private set; } = -1;
    public bool WantsPlayToggle { get; private set; }
    public bool WantsExport { get; private set; }

    private static readonly string[] ButtonLabels =
    {
        "New", "Open", "Save",
        "Undo", "Redo",
        "Brush", "Eraser", "Fill", "Entity", "Picker", "Selection",
        "Play", "Export"
    };

    public void Update(EditorState state, MouseState mouse, MouseState prevMouse,
                       int screenWidth, SpriteFont font, GameTime gameTime)
    {
        WantsNew = false;
        WantsOpen = false;
        WantsSave = false;
        WantsUndo = false;
        WantsRedo = false;
        WantsToolIndex = -1;
        WantsPlayToggle = false;
        WantsExport = false;

        _topOffset = state.IsPlayMode ? 0 : MenuBar.Height;
        ComputeButtonRects(state.IsPlayMode);

        // Hover detection
        int prevHover = _hoverIndex;
        _hoverIndex = -1;
        for (int i = 0; i < TotalButtons; i++)
        {
            if (_buttonRects[i].Width > 0 && _buttonRects[i].Contains(mouse.X, mouse.Y))
            {
                _hoverIndex = i;
                break;
            }
        }

        // Tooltip timer
        if (_hoverIndex >= 0 && _hoverIndex == prevHover)
        {
            _tooltipTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (_tooltipTimer >= TooltipDelay)
                _tooltipIndex = _hoverIndex;
        }
        else
        {
            _tooltipTimer = 0;
            _tooltipIndex = -1;
        }

        // Click handling
        bool leftClick = mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released;
        if (!leftClick || _hoverIndex < 0) return;

        if (state.IsPlayMode)
        {
            // In play mode only the play/stop button is active
            if (_hoverIndex == 11) WantsPlayToggle = true;
            return;
        }

        switch (_hoverIndex)
        {
            case 0: WantsNew = true; break;
            case 1: WantsOpen = true; break;
            case 2: WantsSave = true; break;
            case 3: if (state.UndoStack.CanUndo) WantsUndo = true; break;
            case 4: if (state.UndoStack.CanRedo) WantsRedo = true; break;
            case 5: case 6: case 7: case 8: case 9: case 10:
                WantsToolIndex = _hoverIndex - 5;
                break;
            case 11: WantsPlayToggle = true; break;
            case 12: WantsExport = true; break;
        }
    }

    /// <summary>
    /// InputEvent-aware update. Consumes button clicks so they don't
    /// propagate to panels or canvas.
    /// </summary>
    public void Update(EditorState state, InputEvent input,
                       int screenWidth, SpriteFont font, GameTime gameTime)
    {
        Update(state, input.Mouse, input.PrevMouse, screenWidth, font, gameTime);

        // Consume the button click for cross-component consumption
        if (_hoverIndex >= 0 && _buttonRects[_hoverIndex].Width > 0)
            input.TryConsumeClick(_buttonRects[_hoverIndex]);
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                     Renderer renderer, int screenWidth)
    {
        var barRect = new Rectangle(0, _topOffset, screenWidth, Height);
        renderer.DrawRect(spriteBatch, barRect, BackgroundColor);
        renderer.DrawRect(spriteBatch, new Rectangle(0, _topOffset + Height - 1, screenWidth, 1), SeparatorColor);

        if (state.IsPlayMode)
        {
            DrawPlayModeRibbon(spriteBatch, font, state, renderer, screenWidth);
            return;
        }

        ComputeButtonRects(false);

        // Draw buttons by group
        bool canUndo = state.UndoStack.CanUndo;
        bool canRedo = state.UndoStack.CanRedo;

        // Group 1: New, Open, Save
        DrawIconButton(spriteBatch, renderer, 0, true, state);
        DrawIconButton(spriteBatch, renderer, 1, true, state);
        DrawIconButton(spriteBatch, renderer, 2, true, state);
        DrawSeparator(spriteBatch, renderer, _buttonRects[2].Right + GroupGap / 2);

        // Group 2: Undo, Redo
        DrawIconButton(spriteBatch, renderer, 3, canUndo, state);
        DrawIconButton(spriteBatch, renderer, 4, canRedo, state);
        DrawSeparator(spriteBatch, renderer, _buttonRects[4].Right + GroupGap / 2);

        // Group 3: Tools
        for (int i = 5; i <= 10; i++)
            DrawToolButton(spriteBatch, renderer, i, state);
        DrawSeparator(spriteBatch, renderer, _buttonRects[10].Right + GroupGap / 2);

        // Group 4: Play
        DrawIconButton(spriteBatch, renderer, 11, true, state);
        DrawSeparator(spriteBatch, renderer, _buttonRects[11].Right + GroupGap / 2);

        // Group 5: Export
        DrawIconButton(spriteBatch, renderer, 12, state.Sheet != null, state);

        // Title right-aligned
        string title = "TileForge";
        var titleSize = font.MeasureString(title);
        spriteBatch.DrawString(font, title,
            new Vector2(screenWidth - titleSize.X - 10, _topOffset + (Height - titleSize.Y) / 2),
            LayoutConstants.ToolbarDimTextColor);

        // Tooltip
        if (_tooltipIndex >= 0 && _tooltipIndex < ButtonLabels.Length)
        {
            DrawTooltip(spriteBatch, font, renderer, _tooltipIndex);
        }
    }

    private void DrawPlayModeRibbon(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                                     Renderer renderer, int screenWidth)
    {
        ComputeButtonRects(true);

        // Play/Stop button (index 11)
        DrawIconButton(spriteBatch, renderer, 11, true, state);

        // Centered text
        string playMsg = "PLAY MODE - F5 to return";
        var msgSize = font.MeasureString(playMsg);
        spriteBatch.DrawString(font, playMsg,
            new Vector2((screenWidth - msgSize.X) / 2, _topOffset + (Height - msgSize.Y) / 2),
            PlayModeTextColor);
    }

    private void ComputeButtonRects(bool playMode)
    {
        // Reset all rects
        for (int i = 0; i < TotalButtons; i++)
            _buttonRects[i] = Rectangle.Empty;

        int y = _topOffset + (Height - BtnSize) / 2;

        if (playMode)
        {
            // Only the Play/Stop button (index 11)
            _buttonRects[11] = new Rectangle(BtnPad, y, BtnSize, BtnSize);
            return;
        }

        int x = BtnPad;

        // Group 1: New, Open, Save
        for (int i = 0; i <= 2; i++)
        {
            _buttonRects[i] = new Rectangle(x, y, BtnSize, BtnSize);
            x += BtnSize + BtnPad;
        }
        x += GroupGap;

        // Group 2: Undo, Redo
        for (int i = 3; i <= 4; i++)
        {
            _buttonRects[i] = new Rectangle(x, y, BtnSize, BtnSize);
            x += BtnSize + BtnPad;
        }
        x += GroupGap;

        // Group 3: Tools (6)
        for (int i = 5; i <= 10; i++)
        {
            _buttonRects[i] = new Rectangle(x, y, BtnSize, BtnSize);
            x += BtnSize + BtnPad;
        }
        x += GroupGap;

        // Group 4: Play
        _buttonRects[11] = new Rectangle(x, y, BtnSize, BtnSize);
        x += BtnSize + BtnPad + GroupGap;

        // Group 5: Export
        _buttonRects[12] = new Rectangle(x, y, BtnSize, BtnSize);
    }

    private void DrawIconButton(SpriteBatch sb, Renderer r, int index, bool enabled, EditorState state)
    {
        var rect = _buttonRects[index];
        if (rect.Width == 0) return;

        var bgColor = !enabled ? ButtonDisabledColor
            : (_hoverIndex == index ? ButtonHoverColor : ButtonColor);
        r.DrawRect(sb, rect, bgColor);

        var iconColor = enabled ? IconColor : IconDimColor;
        int cx = rect.X + BtnSize / 2;
        int cy = rect.Y + BtnSize / 2;

        switch (index)
        {
            case 0: DrawNewIcon(sb, r, cx, cy, iconColor); break;
            case 1: DrawOpenIcon(sb, r, cx, cy, iconColor); break;
            case 2: DrawSaveIcon(sb, r, cx, cy, iconColor); break;
            case 3: DrawUndoIcon(sb, r, cx, cy, iconColor); break;
            case 4: DrawRedoIcon(sb, r, cx, cy, iconColor); break;
            case 11: DrawPlayIcon(sb, r, cx, cy, iconColor, state.IsPlayMode); break;
            case 12: DrawExportIcon(sb, r, cx, cy, iconColor); break;
        }
    }

    private void DrawToolButton(SpriteBatch sb, Renderer r, int index, EditorState state)
    {
        var rect = _buttonRects[index];
        if (rect.Width == 0) return;

        int toolIdx = index - 5;
        bool isActive = state.ActiveTool?.Name == ToolNames[toolIdx];
        var bgColor = isActive ? ButtonActiveColor
            : (_hoverIndex == index ? ButtonHoverColor : ButtonColor);
        r.DrawRect(sb, rect, bgColor);

        var iconColor = isActive ? IconColor : IconDimColor;
        int cx = rect.X + BtnSize / 2;
        int cy = rect.Y + BtnSize / 2;

        switch (toolIdx)
        {
            case 0: DrawBrushIcon(sb, r, cx, cy, iconColor); break;
            case 1: DrawEraserIcon(sb, r, cx, cy, iconColor); break;
            case 2: DrawFillIcon(sb, r, cx, cy, iconColor); break;
            case 3: DrawEntityIcon(sb, r, cx, cy, iconColor); break;
            case 4: DrawPickerIcon(sb, r, cx, cy, iconColor); break;
            case 5: DrawSelectionIcon(sb, r, cx, cy, iconColor); break;
        }
    }

    private void DrawSeparator(SpriteBatch sb, Renderer r, int x)
    {
        r.DrawRect(sb, new Rectangle(x, _topOffset + 4, SeparatorWidth, Height - 8), SeparatorColor);
    }

    private void DrawTooltip(SpriteBatch sb, SpriteFont font, Renderer r, int index)
    {
        var rect = _buttonRects[index];
        string label = ButtonLabels[index];
        var textSize = font.MeasureString(label);
        int tipX = rect.X + (rect.Width - (int)textSize.X) / 2 - 4;
        int tipY = _topOffset + Height + 2;
        int tipW = (int)textSize.X + 8;
        int tipH = (int)textSize.Y + 4;

        r.DrawRect(sb, new Rectangle(tipX, tipY, tipW, tipH), new Color(30, 30, 30, 240));
        r.DrawRectOutline(sb, new Rectangle(tipX, tipY, tipW, tipH), new Color(80, 80, 80), 1);
        sb.DrawString(font, label, new Vector2(tipX + 4, tipY + 2), Color.White);
    }

    // --- Icon drawing ---

    private static void DrawNewIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        // Document with folded corner
        r.DrawRect(sb, new Rectangle(cx - 5, cy - 6, 10, 12), c);
        r.DrawRect(sb, new Rectangle(cx + 2, cy - 6, 3, 3),
            new Color(c.R / 2, c.G / 2, c.B / 2, c.A)); // Corner fold (darker)
    }

    private static void DrawOpenIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        // Folder shape
        r.DrawRect(sb, new Rectangle(cx - 6, cy - 4, 4, 2), c); // Tab
        r.DrawRect(sb, new Rectangle(cx - 6, cy - 2, 12, 8), c); // Body
    }

    private static void DrawSaveIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        r.DrawRect(sb, new Rectangle(cx - 6, cy - 6, 12, 12), c);
        r.DrawRect(sb, new Rectangle(cx - 4, cy - 6, 8, 4),
            new Color(c.R * 3 / 4, c.G * 3 / 4, c.B * 3 / 4, c.A));
        r.DrawRect(sb, new Rectangle(cx - 3, cy + 1, 6, 4),
            new Color(c.R / 2, c.G / 2, c.B / 2, c.A));
    }

    private static void DrawUndoIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        r.DrawRect(sb, new Rectangle(cx - 6, cy - 1, 10, 2), c);
        r.DrawRect(sb, new Rectangle(cx - 6, cy - 5, 2, 4), c);
        r.DrawRect(sb, new Rectangle(cx - 6, cy + 1, 2, 4), c);
        r.DrawRect(sb, new Rectangle(cx + 4, cy - 5, 2, 4), c);
    }

    private static void DrawRedoIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        r.DrawRect(sb, new Rectangle(cx - 4, cy - 1, 10, 2), c);
        r.DrawRect(sb, new Rectangle(cx + 4, cy - 5, 2, 4), c);
        r.DrawRect(sb, new Rectangle(cx + 4, cy + 1, 2, 4), c);
        r.DrawRect(sb, new Rectangle(cx - 6, cy - 5, 2, 4), c);
    }

    private static void DrawPlayIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c, bool isPlaying)
    {
        if (isPlaying)
        {
            r.DrawRect(sb, new Rectangle(cx - 4, cy - 4, 8, 8), c);
        }
        else
        {
            r.DrawRect(sb, new Rectangle(cx - 4, cy - 5, 2, 10), c);
            r.DrawRect(sb, new Rectangle(cx - 2, cy - 4, 2, 8), c);
            r.DrawRect(sb, new Rectangle(cx, cy - 3, 2, 6), c);
            r.DrawRect(sb, new Rectangle(cx + 2, cy - 2, 2, 4), c);
            r.DrawRect(sb, new Rectangle(cx + 4, cy - 1, 2, 2), c);
        }
    }

    private static void DrawExportIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        // Down arrow + box (export)
        r.DrawRect(sb, new Rectangle(cx - 1, cy - 6, 2, 8), c);  // Shaft
        r.DrawRect(sb, new Rectangle(cx - 3, cy, 2, 2), c);       // Arrow left
        r.DrawRect(sb, new Rectangle(cx + 1, cy, 2, 2), c);       // Arrow right
        r.DrawRect(sb, new Rectangle(cx - 5, cy + 3, 10, 2), c);  // Box bottom
        r.DrawRect(sb, new Rectangle(cx - 5, cy - 1, 2, 4), c);   // Box left
        r.DrawRect(sb, new Rectangle(cx + 3, cy - 1, 2, 4), c);   // Box right
    }

    // Tool icons

    private static void DrawBrushIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        r.DrawRect(sb, new Rectangle(cx - 1, cy - 10, 2, 6), c);
        r.DrawRect(sb, new Rectangle(cx - 2, cy - 4, 4, 2), c);
        r.DrawRect(sb, new Rectangle(cx - 3, cy - 2, 6, 4), c);
        r.DrawRect(sb, new Rectangle(cx - 4, cy + 2, 8, 4), c);
        r.DrawRect(sb, new Rectangle(cx - 3, cy + 6, 6, 2), c);
    }

    private static void DrawEraserIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        r.DrawRect(sb, new Rectangle(cx - 6, cy - 6, 12, 10), c);
        r.DrawRect(sb, new Rectangle(cx - 6, cy + 4, 12, 4),
            new Color(c.R * 3 / 4, c.G * 3 / 4, c.B * 3 / 4, c.A));
        r.DrawRectOutline(sb, new Rectangle(cx - 6, cy - 6, 12, 14), c, 1);
    }

    private static void DrawFillIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        r.DrawRect(sb, new Rectangle(cx - 5, cy - 2, 10, 8), c);
        r.DrawRect(sb, new Rectangle(cx + 5, cy - 4, 2, 6), c);
        r.DrawRect(sb, new Rectangle(cx - 2, cy - 6, 2, 4), c);
        r.DrawRect(sb, new Rectangle(cx - 3, cy - 8, 4, 2), c);
    }

    private static void DrawEntityIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        r.DrawRect(sb, new Rectangle(cx - 4, cy - 6, 8, 8), c);
        r.DrawRect(sb, new Rectangle(cx - 2, cy - 8, 4, 2), c);
        r.DrawRect(sb, new Rectangle(cx - 2, cy + 2, 4, 4), c);
        r.DrawRect(sb, new Rectangle(cx - 1, cy + 6, 2, 3), c);
    }

    private static void DrawPickerIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        r.DrawRect(sb, new Rectangle(cx - 1, cy + 5, 2, 4), c);
        r.DrawRect(sb, new Rectangle(cx - 2, cy + 1, 4, 4), c);
        r.DrawRect(sb, new Rectangle(cx - 3, cy - 3, 6, 4), c);
        r.DrawRect(sb, new Rectangle(cx - 2, cy - 7, 4, 4), c);
        r.DrawRect(sb, new Rectangle(cx - 1, cy - 9, 2, 2), c);
    }

    private static void DrawSelectionIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        r.DrawRectOutline(sb, new Rectangle(cx - 7, cy - 7, 14, 14), c, 1);
        r.DrawRect(sb, new Rectangle(cx - 7, cy - 7, 4, 1), c);
        r.DrawRect(sb, new Rectangle(cx - 7, cy - 7, 1, 4), c);
        r.DrawRect(sb, new Rectangle(cx + 3, cy - 7, 4, 1), c);
        r.DrawRect(sb, new Rectangle(cx + 6, cy - 7, 1, 4), c);
        r.DrawRect(sb, new Rectangle(cx - 7, cy + 3, 1, 4), c);
        r.DrawRect(sb, new Rectangle(cx - 7, cy + 6, 4, 1), c);
        r.DrawRect(sb, new Rectangle(cx + 6, cy + 3, 1, 4), c);
        r.DrawRect(sb, new Rectangle(cx + 3, cy + 6, 4, 1), c);
    }
}
