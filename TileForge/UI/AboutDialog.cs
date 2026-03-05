using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;

namespace TileForge.UI;

/// <summary>
/// A minimal informational dialog showing the application name, tagline, and a close hint.
/// Press Escape or Enter to close.
/// </summary>
public class AboutDialog : IDialog
{
    // ---- Colors ----
    private static readonly Color OverlayColor  = new(0, 0, 0, 160);
    private static readonly Color PanelColor    = new(40, 40, 40);
    private static readonly Color PanelBorder   = new(80, 80, 80);
    private static readonly Color SubtitleColor = new(160, 160, 160);
    private static readonly Color HintColor     = new(100, 100, 100);

    // ---- Layout ----
    private const int PanelWidth  = 300;
    private const int PanelHeight = 180;
    private const int Padding     = 16;

    // ---- IDialog ----

    public bool IsComplete  { get; private set; }
    public bool WasCancelled { get; private set; }

    public void OnTextInput(char character) { }

    public void Update(KeyboardState keyboard, KeyboardState prevKeyboard, GameTime gameTime)
    {
        if (KeyPressed(keyboard, prevKeyboard, Keys.Escape) ||
            KeyPressed(keyboard, prevKeyboard, Keys.Enter))
        {
            IsComplete = true;
            WasCancelled = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                     int screenWidth, int screenHeight, GameTime gameTime)
    {
        // Dim the background
        renderer.DrawRect(spriteBatch, new Rectangle(0, 0, screenWidth, screenHeight), OverlayColor);

        int px = (screenWidth  - PanelWidth)  / 2;
        int py = (screenHeight - PanelHeight) / 2;
        var panel = new Rectangle(px, py, PanelWidth, PanelHeight);

        renderer.DrawRect(spriteBatch, panel, PanelColor);
        renderer.DrawRectOutline(spriteBatch, panel, PanelBorder, 1);

        int y = py + Padding;

        // Title: "TileForge"
        string title = "TileForge";
        var titleSize = font.MeasureString(title);
        spriteBatch.DrawString(font, title,
            new Vector2(px + (PanelWidth - titleSize.X) / 2, y),
            Color.White);
        y += font.LineSpacing + 8;

        // Subtitle
        string subtitle = "Tile Map Editor & RPG Runtime";
        var subtitleSize = font.MeasureString(subtitle);
        spriteBatch.DrawString(font, subtitle,
            new Vector2(px + (PanelWidth - subtitleSize.X) / 2, y),
            SubtitleColor);
        y += font.LineSpacing + Padding * 2;

        // Hint
        string hint = "Press Escape to close";
        var hintSize = font.MeasureString(hint);
        spriteBatch.DrawString(font, hint,
            new Vector2(px + (PanelWidth - hintSize.X) / 2, y),
            HintColor);
    }

    private static bool KeyPressed(KeyboardState current, KeyboardState prev, Keys key)
        => current.IsKeyDown(key) && prev.IsKeyUp(key);
}
