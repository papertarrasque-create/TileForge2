using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge2.Editor;
using TileForge2.Editor.Tools;

namespace TileForge2.UI;

public class ToolPanel : Panel
{
    public override string Title => "Tools";
    public override PanelSizeMode SizeMode => PanelSizeMode.Fixed;
    public override int PreferredHeight => 2 * (ButtonSize + ButtonPadding) + ButtonPadding;

    private const int ButtonSize = 36;
    private const int ButtonPadding = 6;

    private static readonly Color ButtonColor = new(55, 55, 55);
    private static readonly Color ButtonActiveColor = new(70, 90, 130);
    private static readonly Color ButtonHoverColor = new(65, 65, 65);
    private static readonly Color IconColor = new(200, 200, 200);
    private static readonly Color IconDimColor = new(140, 140, 140);

    private static readonly string[] ToolNames = { "Brush", "Eraser", "Fill", "Entity" };

    private int _hoverIndex = -1;

    public override void UpdateContent(EditorState state, MouseState mouse, MouseState prevMouse,
                                        SpriteFont font, GameTime gameTime, int screenW, int screenH)
    {
        _hoverIndex = -1;

        if (!ContentBounds.Contains(mouse.X, mouse.Y)) return;

        int hitIndex = GetButtonIndex(mouse.X, mouse.Y);
        if (hitIndex < 0 || hitIndex >= ToolNames.Length) return;

        _hoverIndex = hitIndex;

        if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released)
        {
            switch (hitIndex)
            {
                case 0: state.ActiveTool = new BrushTool(); break;
                case 1: state.ActiveTool = new EraserTool(); break;
                case 2: state.ActiveTool = new FillTool(); break;
                case 3: state.ActiveTool = new EntityTool(); break;
            }
        }
    }

    public override void DrawContent(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                                      Renderer renderer)
    {
        for (int i = 0; i < ToolNames.Length; i++)
        {
            var btnRect = GetButtonRect(i);

            bool isActive = state.ActiveTool?.Name == ToolNames[i];
            bool isHover = _hoverIndex == i;

            var bgColor = isActive ? ButtonActiveColor : (isHover ? ButtonHoverColor : ButtonColor);
            renderer.DrawRect(spriteBatch, btnRect, bgColor);

            // Draw procedural icon
            var iconColor = isActive ? IconColor : IconDimColor;
            DrawToolIcon(spriteBatch, renderer, i, btnRect, iconColor);
        }
    }

    private int GetButtonIndex(int mouseX, int mouseY)
    {
        int relX = mouseX - ContentBounds.X - ButtonPadding;
        int relY = mouseY - ContentBounds.Y - ButtonPadding;

        if (relX < 0 || relY < 0) return -1;

        int col = relX / (ButtonSize + ButtonPadding);
        int row = relY / (ButtonSize + ButtonPadding);

        // Check within button bounds (not in padding)
        int inCol = relX % (ButtonSize + ButtonPadding);
        int inRow = relY % (ButtonSize + ButtonPadding);
        if (inCol >= ButtonSize || inRow >= ButtonSize) return -1;

        if (col > 1 || row > 1) return -1;

        return row * 2 + col;
    }

    private Rectangle GetButtonRect(int index)
    {
        int col = index % 2;
        int row = index / 2;
        int x = ContentBounds.X + ButtonPadding + col * (ButtonSize + ButtonPadding);
        int y = ContentBounds.Y + ButtonPadding + row * (ButtonSize + ButtonPadding);
        return new Rectangle(x, y, ButtonSize, ButtonSize);
    }

    private static void DrawToolIcon(SpriteBatch spriteBatch, Renderer renderer,
                                      int toolIndex, Rectangle btnRect, Color color)
    {
        // Center icon area within button
        int cx = btnRect.X + ButtonSize / 2;
        int cy = btnRect.Y + ButtonSize / 2;

        switch (toolIndex)
        {
            case 0: DrawBrushIcon(spriteBatch, renderer, cx, cy, color); break;
            case 1: DrawEraserIcon(spriteBatch, renderer, cx, cy, color); break;
            case 2: DrawFillIcon(spriteBatch, renderer, cx, cy, color); break;
            case 3: DrawEntityIcon(spriteBatch, renderer, cx, cy, color); break;
        }
    }

    private static void DrawBrushIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        // Pencil/brush shape: thin handle on top, widening bristles at bottom
        r.DrawRect(sb, new Rectangle(cx - 1, cy - 10, 2, 6), c);   // Handle
        r.DrawRect(sb, new Rectangle(cx - 2, cy - 4, 4, 2), c);    // Ferrule
        r.DrawRect(sb, new Rectangle(cx - 3, cy - 2, 6, 4), c);    // Bristles upper
        r.DrawRect(sb, new Rectangle(cx - 4, cy + 2, 8, 4), c);    // Bristles lower
        r.DrawRect(sb, new Rectangle(cx - 3, cy + 6, 6, 2), c);    // Tip
    }

    private static void DrawEraserIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        // Eraser block with tip stripe
        r.DrawRect(sb, new Rectangle(cx - 6, cy - 6, 12, 10), c);  // Body
        r.DrawRect(sb, new Rectangle(cx - 6, cy + 4, 12, 4), new Color(c.R * 3 / 4, c.G * 3 / 4, c.B * 3 / 4, c.A)); // Darker tip
        r.DrawRectOutline(sb, new Rectangle(cx - 6, cy - 6, 12, 14), c, 1); // Outline
    }

    private static void DrawFillIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        // Paint bucket: body + handle + drip
        r.DrawRect(sb, new Rectangle(cx - 5, cy - 2, 10, 8), c);   // Bucket body
        r.DrawRect(sb, new Rectangle(cx + 5, cy - 4, 2, 6), c);    // Handle
        r.DrawRect(sb, new Rectangle(cx - 2, cy - 6, 2, 4), c);    // Drip stream
        r.DrawRect(sb, new Rectangle(cx - 3, cy - 8, 4, 2), c);    // Drip top
    }

    private static void DrawEntityIcon(SpriteBatch sb, Renderer r, int cx, int cy, Color c)
    {
        // Map marker / pin shape
        r.DrawRect(sb, new Rectangle(cx - 4, cy - 6, 8, 8), c);    // Body
        r.DrawRect(sb, new Rectangle(cx - 2, cy - 8, 4, 2), c);    // Top
        r.DrawRect(sb, new Rectangle(cx - 2, cy + 2, 4, 4), c);    // Lower taper
        r.DrawRect(sb, new Rectangle(cx - 1, cy + 6, 2, 3), c);    // Pin point
    }
}
