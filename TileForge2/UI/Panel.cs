using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge2.Editor;

namespace TileForge2.UI;

public enum PanelSizeMode
{
    Fixed,
    Flexible,
}

public abstract class Panel
{
    public abstract string Title { get; }
    public abstract PanelSizeMode SizeMode { get; }
    public abstract int PreferredHeight { get; }

    public bool IsCollapsed { get; set; }

    // Layout — set by PanelDock each frame
    public Rectangle Bounds { get; set; }
    public Rectangle HeaderBounds { get; set; }
    public Rectangle ContentBounds { get; set; }

    public bool HeaderHovered { get; set; }

    public const int HeaderHeight = 24;

    private static readonly Color HeaderColor = new(45, 45, 45);
    private static readonly Color HeaderHoverColor = new(55, 55, 55);
    private static readonly Color HeaderTextColor = new(180, 180, 180);
    private static readonly Color ArrowColor = new(140, 140, 140);
    private static readonly Color SeparatorColor = new(60, 60, 60);

    public abstract void UpdateContent(EditorState state, MouseState mouse, MouseState prevMouse,
                                        SpriteFont font, GameTime gameTime, int screenW, int screenH);

    public abstract void DrawContent(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                                      Renderer renderer);

    public virtual void DrawHeader(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer)
    {
        var bgColor = HeaderHovered ? HeaderHoverColor : HeaderColor;
        renderer.DrawRect(spriteBatch, HeaderBounds, bgColor);

        // Separator line at bottom of header
        renderer.DrawRect(spriteBatch, new Rectangle(HeaderBounds.X, HeaderBounds.Bottom - 1,
                                                      HeaderBounds.Width, 1), SeparatorColor);

        // Collapse arrow
        int arrowX = HeaderBounds.X + 8;
        int arrowY = HeaderBounds.Y + (HeaderHeight - 8) / 2;
        DrawCollapseArrow(spriteBatch, renderer, arrowX, arrowY, IsCollapsed);

        // Title text
        spriteBatch.DrawString(font, Title,
            new Vector2(HeaderBounds.X + 22, HeaderBounds.Y + (HeaderHeight - font.LineSpacing) / 2),
            HeaderTextColor);
    }

    private static void DrawCollapseArrow(SpriteBatch spriteBatch, Renderer renderer,
                                           int x, int y, bool collapsed)
    {
        if (collapsed)
        {
            // Right-pointing arrow
            renderer.DrawRect(spriteBatch, new Rectangle(x, y, 2, 8), ArrowColor);
            renderer.DrawRect(spriteBatch, new Rectangle(x + 2, y + 1, 2, 6), ArrowColor);
            renderer.DrawRect(spriteBatch, new Rectangle(x + 4, y + 2, 2, 4), ArrowColor);
            renderer.DrawRect(spriteBatch, new Rectangle(x + 6, y + 3, 2, 2), ArrowColor);
        }
        else
        {
            // Down-pointing arrow
            renderer.DrawRect(spriteBatch, new Rectangle(x, y, 8, 2), ArrowColor);
            renderer.DrawRect(spriteBatch, new Rectangle(x + 1, y + 2, 6, 2), ArrowColor);
            renderer.DrawRect(spriteBatch, new Rectangle(x + 2, y + 4, 4, 2), ArrowColor);
            renderer.DrawRect(spriteBatch, new Rectangle(x + 3, y + 6, 2, 2), ArrowColor);
        }
    }
}
