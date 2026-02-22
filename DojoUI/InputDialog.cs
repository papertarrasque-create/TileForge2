using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DojoUI;

public class InputDialog : IDialog
{
    private static readonly Color OverlayColor = new(0, 0, 0, 160);
    private static readonly Color PanelColor = new(40, 40, 40);
    private static readonly Color PanelBorder = new(100, 100, 100);
    private static readonly Color HintColor = new(140, 140, 140);

    private const int PanelWidth = 500;
    private const int PanelHeight = 100;

    private readonly string _title;
    private readonly TextInputField _input;

    public bool IsComplete { get; private set; }
    public bool WasCancelled { get; private set; }
    public string ResultText => _input.Text;

    public InputDialog(string title, string defaultText = "")
    {
        _title = title;
        _input = new TextInputField(defaultText, maxLength: 512);
        _input.IsFocused = true;
    }

    public void Update(KeyboardState keyboard, KeyboardState prevKeyboard, GameTime gameTime)
    {
        if (KeyPressed(keyboard, prevKeyboard, Keys.Escape))
        {
            IsComplete = true;
            WasCancelled = true;
            return;
        }

        if (KeyPressed(keyboard, prevKeyboard, Keys.Enter))
        {
            if (_input.Text.Length > 0)
            {
                IsComplete = true;
                WasCancelled = false;
            }
            return;
        }

        if (KeyPressed(keyboard, prevKeyboard, Keys.Back))
            _input.HandleKey(Keys.Back);
        if (KeyPressed(keyboard, prevKeyboard, Keys.Delete))
            _input.HandleKey(Keys.Delete);
        if (KeyPressed(keyboard, prevKeyboard, Keys.Left))
            _input.HandleKey(Keys.Left);
        if (KeyPressed(keyboard, prevKeyboard, Keys.Right))
            _input.HandleKey(Keys.Right);
        if (KeyPressed(keyboard, prevKeyboard, Keys.Home))
            _input.HandleKey(Keys.Home);
        if (KeyPressed(keyboard, prevKeyboard, Keys.End))
            _input.HandleKey(Keys.End);
    }

    public void OnTextInput(char character)
    {
        _input.HandleCharacter(character);
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer, int screenWidth, int screenHeight, GameTime gameTime)
    {
        renderer.DrawRect(spriteBatch, new Rectangle(0, 0, screenWidth, screenHeight), OverlayColor);

        int px = (screenWidth - PanelWidth) / 2;
        int py = (screenHeight - PanelHeight) / 2;
        var panel = new Rectangle(px, py, PanelWidth, PanelHeight);
        renderer.DrawRect(spriteBatch, panel, PanelColor);
        renderer.DrawRectOutline(spriteBatch, panel, PanelBorder, 1);

        spriteBatch.DrawString(font, _title, new Vector2(px + 10, py + 8), Color.White);

        int inputY = py + 8 + font.LineSpacing + 8;
        var inputBounds = new Rectangle(px + 10, inputY, PanelWidth - 20, 30);
        _input.Draw(spriteBatch, font, renderer, inputBounds, gameTime);

        string hint = "[Enter] Confirm    [Esc] Cancel";
        var hintSize = font.MeasureString(hint);
        spriteBatch.DrawString(font, hint, new Vector2(px + PanelWidth - hintSize.X - 10, py + PanelHeight - font.LineSpacing - 6), HintColor);
    }

    private static bool KeyPressed(KeyboardState current, KeyboardState prev, Keys key)
    {
        return current.IsKeyDown(key) && prev.IsKeyUp(key);
    }
}
