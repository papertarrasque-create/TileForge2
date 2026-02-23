using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Editor;

namespace TileForge.UI;

public class Toolbar
{
    public const int Height = LayoutConstants.ToolbarHeight;

    private static readonly Color BackgroundColor = LayoutConstants.ToolbarBackground;
    private static readonly Color ButtonColor = LayoutConstants.ToolbarButtonColor;
    private static readonly Color ButtonActiveColor = LayoutConstants.ToolbarButtonActiveColor;
    private static readonly Color ButtonHoverColor = LayoutConstants.ToolbarButtonHoverColor;
    private static readonly Color ButtonDisabledColor = LayoutConstants.ToolbarButtonDisabledColor;
    private static readonly Color IconColor = LayoutConstants.ToolbarIconColor;
    private static readonly Color IconDimColor = LayoutConstants.ToolbarIconDimColor;
    private static readonly Color DimTextColor = LayoutConstants.ToolbarDimTextColor;
    private static readonly Color SeparatorColor = LayoutConstants.ToolbarSeparatorColor;

    private const int ButtonSize = LayoutConstants.ToolbarButtonSize;
    private const int ButtonPadding = LayoutConstants.ToolbarButtonPadding;

    // 4 buttons: Undo, Redo, Save, Play
    private int _hoverButton = -1;

    // Action signals
    public bool WantsUndo { get; private set; }
    public bool WantsRedo { get; private set; }
    public bool WantsSave { get; private set; }
    public bool WantsPlayToggle { get; private set; }

    public void Update(EditorState state, MouseState mouse, MouseState prevMouse,
                       int screenWidth, SpriteFont font)
    {
        _hoverButton = -1;
        WantsUndo = false;
        WantsRedo = false;
        WantsSave = false;
        WantsPlayToggle = false;

        if (mouse.Y > Height) return;

        bool leftClick = mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released;

        for (int i = 0; i < 4; i++)
        {
            var rect = GetButtonRect(i);
            if (rect.Contains(mouse.X, mouse.Y))
            {
                _hoverButton = i;

                if (leftClick)
                {
                    switch (i)
                    {
                        case 0: if (state.UndoStack.CanUndo) WantsUndo = true; break;
                        case 1: if (state.UndoStack.CanRedo) WantsRedo = true; break;
                        case 2: WantsSave = true; break;
                        case 3: WantsPlayToggle = true; break;
                    }
                }
                break;
            }
        }
    }

    private static Rectangle GetButtonRect(int index)
    {
        int x = ButtonPadding + index * (ButtonSize + ButtonPadding);
        return new Rectangle(x, 2, ButtonSize, ButtonSize);
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, EditorState state, Renderer renderer, int screenWidth)
    {
        var barRect = new Rectangle(0, 0, screenWidth, Height);
        renderer.DrawRect(spriteBatch, barRect, BackgroundColor);

        // Separator under toolbar
        renderer.DrawRect(spriteBatch, new Rectangle(0, Height - 1, screenWidth, 1), SeparatorColor);

        // Play mode display
        if (state.IsPlayMode)
        {
            string playMsg = "PLAY MODE - F5 to return";
            var msgSize = font.MeasureString(playMsg);
            spriteBatch.DrawString(font, playMsg,
                new Vector2((screenWidth - msgSize.X) / 2, (Height - msgSize.Y) / 2),
                LayoutConstants.ToolbarPlayModeTextColor);

            // Still show play/stop button
            DrawButton(spriteBatch, renderer, 3, _hoverButton == 3, true, state);
            return;
        }

        // Action buttons
        bool canUndo = state.UndoStack.CanUndo;
        bool canRedo = state.UndoStack.CanRedo;

        DrawButton(spriteBatch, renderer, 0, _hoverButton == 0, canUndo, state);   // Undo
        DrawButton(spriteBatch, renderer, 1, _hoverButton == 1, canRedo, state);   // Redo
        DrawButton(spriteBatch, renderer, 2, _hoverButton == 2, true, state);      // Save
        DrawButton(spriteBatch, renderer, 3, _hoverButton == 3, true, state);      // Play

        // Title
        string title = "TileForge";
        var titleSize = font.MeasureString(title);
        spriteBatch.DrawString(font, title,
            new Vector2(screenWidth - titleSize.X - 10, (Height - titleSize.Y) / 2), DimTextColor);
    }

    private static void DrawButton(SpriteBatch sb, Renderer r, int index, bool hovered, bool enabled, EditorState state)
    {
        var rect = GetButtonRect(index);
        var bgColor = !enabled ? ButtonDisabledColor : (hovered ? ButtonHoverColor : ButtonColor);
        r.DrawRect(sb, rect, bgColor);

        var iconColor = enabled ? IconColor : IconDimColor;
        int cx = rect.X + ButtonSize / 2;
        int cy = rect.Y + ButtonSize / 2;

        switch (index)
        {
            case 0: DrawUndoIcon(sb, r, cx, cy, iconColor); break;
            case 1: DrawRedoIcon(sb, r, cx, cy, iconColor); break;
            case 2: DrawSaveIcon(sb, r, cx, cy, iconColor); break;
            case 3: DrawPlayIcon(sb, r, cx, cy, iconColor, state.IsPlayMode); break;
        }
    }

    private static void DrawUndoIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        // Left-pointing curved arrow
        r.DrawRect(sb, new Rectangle(cx - 6, cy - 1, 10, 2), c);  // Shaft
        r.DrawRect(sb, new Rectangle(cx - 6, cy - 5, 2, 4), c);   // Arrow head top
        r.DrawRect(sb, new Rectangle(cx - 6, cy + 1, 2, 4), c);   // Arrow head bottom
        r.DrawRect(sb, new Rectangle(cx + 4, cy - 5, 2, 4), c);   // Curve uptick
    }

    private static void DrawRedoIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        // Right-pointing curved arrow (mirror of undo)
        r.DrawRect(sb, new Rectangle(cx - 4, cy - 1, 10, 2), c);  // Shaft
        r.DrawRect(sb, new Rectangle(cx + 4, cy - 5, 2, 4), c);   // Arrow head top
        r.DrawRect(sb, new Rectangle(cx + 4, cy + 1, 2, 4), c);   // Arrow head bottom
        r.DrawRect(sb, new Rectangle(cx - 6, cy - 5, 2, 4), c);   // Curve uptick
    }

    private static void DrawSaveIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        // Floppy disk
        r.DrawRect(sb, new Rectangle(cx - 6, cy - 6, 12, 12), c);                                // Body
        r.DrawRect(sb, new Rectangle(cx - 4, cy - 6, 8, 4), new Color(c.R * 3 / 4, c.G * 3 / 4, c.B * 3 / 4, c.A)); // Label (darker)
        r.DrawRect(sb, new Rectangle(cx - 3, cy + 1, 6, 4), new Color(c.R / 2, c.G / 2, c.B / 2, c.A));              // Window (darkest)
    }

    private static void DrawPlayIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c, bool isPlaying)
    {
        if (isPlaying)
        {
            // Stop icon: filled square
            r.DrawRect(sb, new Rectangle(cx - 4, cy - 4, 8, 8), c);
        }
        else
        {
            // Play icon: right-pointing triangle (stepped)
            r.DrawRect(sb, new Rectangle(cx - 4, cy - 5, 2, 10), c);
            r.DrawRect(sb, new Rectangle(cx - 2, cy - 4, 2, 8), c);
            r.DrawRect(sb, new Rectangle(cx, cy - 3, 2, 6), c);
            r.DrawRect(sb, new Rectangle(cx + 2, cy - 2, 2, 4), c);
            r.DrawRect(sb, new Rectangle(cx + 4, cy - 1, 2, 2), c);
        }
    }
}
