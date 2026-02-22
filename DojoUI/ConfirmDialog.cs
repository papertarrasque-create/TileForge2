using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DojoUI;

public class ConfirmDialog : IDialog
{
    private static readonly Color OverlayColor = new(0, 0, 0, 160);
    private static readonly Color PanelColor = new(40, 40, 40);
    private static readonly Color PanelBorder = new(100, 100, 100);
    private static readonly Color HintColor = new(140, 140, 140);

    private const int PanelWidth = 500;
    private const int PanelHeight = 80;

    private readonly string _message;

    public bool IsComplete { get; private set; }
    public bool WasCancelled { get; private set; }

    public ConfirmDialog(string message)
    {
        _message = message;
    }

    public void Update(KeyboardState keyboard, KeyboardState prevKeyboard, GameTime gameTime)
    {
        if (KeyPressed(keyboard, prevKeyboard, Keys.Y) || KeyPressed(keyboard, prevKeyboard, Keys.Enter))
        {
            IsComplete = true;
            WasCancelled = false;
        }
        else if (KeyPressed(keyboard, prevKeyboard, Keys.N) || KeyPressed(keyboard, prevKeyboard, Keys.Escape))
        {
            IsComplete = true;
            WasCancelled = true;
        }
    }

    public void OnTextInput(char character) { }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
        int screenWidth, int screenHeight, GameTime gameTime)
    {
        renderer.DrawRect(spriteBatch, new Rectangle(0, 0, screenWidth, screenHeight), OverlayColor);

        int px = (screenWidth - PanelWidth) / 2;
        int py = (screenHeight - PanelHeight) / 2;
        var panel = new Rectangle(px, py, PanelWidth, PanelHeight);
        renderer.DrawRect(spriteBatch, panel, PanelColor);
        renderer.DrawRectOutline(spriteBatch, panel, PanelBorder, 1);

        spriteBatch.DrawString(font, _message, new Vector2(px + 10, py + 10), Color.White);

        string hint = "[Y/Enter] Yes    [N/Esc] No";
        var hintSize = font.MeasureString(hint);
        spriteBatch.DrawString(font, hint,
            new Vector2(px + PanelWidth - hintSize.X - 10, py + PanelHeight - font.LineSpacing - 8), HintColor);
    }

    private static bool KeyPressed(KeyboardState current, KeyboardState prev, Keys key)
    {
        return current.IsKeyDown(key) && prev.IsKeyUp(key);
    }
}
