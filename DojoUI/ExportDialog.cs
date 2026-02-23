using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DojoUI;

public class ExportDialog : IDialog
{
    public enum ExportFormat { Json, Png }

    private static readonly Color OverlayColor = new(0, 0, 0, 160);
    private static readonly Color PanelColor = new(40, 40, 40);
    private static readonly Color PanelBorder = new(100, 100, 100);
    private static readonly Color HintColor = new(140, 140, 140);
    private static readonly Color FormatActiveColor = new(70, 90, 130);
    private static readonly Color FormatInactiveColor = new(55, 55, 55);
    private static readonly Color LabelColor = new(180, 180, 180);

    private const int PanelWidth = 500;
    private const int PanelHeight = 140;

    private readonly TextInputField _pathField;

    public bool IsComplete { get; private set; }
    public bool WasCancelled { get; private set; }
    public ExportFormat SelectedFormat { get; private set; } = ExportFormat.Json;
    public string OutputPath => _pathField.Text;

    public ExportDialog(string defaultPath)
    {
        _pathField = new TextInputField(defaultPath, maxLength: 512);
        _pathField.IsFocused = true;
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
            if (_pathField.Text.Length > 0)
            {
                IsComplete = true;
                WasCancelled = false;
            }
            return;
        }

        // Tab toggles format
        if (KeyPressed(keyboard, prevKeyboard, Keys.Tab))
        {
            SelectedFormat = SelectedFormat == ExportFormat.Json ? ExportFormat.Png : ExportFormat.Json;
            // Update file extension
            string path = _pathField.Text;
            string newExt = SelectedFormat == ExportFormat.Json ? ".json" : ".png";
            int dotIdx = path.LastIndexOf('.');
            if (dotIdx >= 0)
                _pathField.SetText(path[..dotIdx] + newExt);
            return;
        }

        if (KeyPressed(keyboard, prevKeyboard, Keys.Back))
            _pathField.HandleKey(Keys.Back);
        if (KeyPressed(keyboard, prevKeyboard, Keys.Delete))
            _pathField.HandleKey(Keys.Delete);
        if (KeyPressed(keyboard, prevKeyboard, Keys.Left))
            _pathField.HandleKey(Keys.Left);
        if (KeyPressed(keyboard, prevKeyboard, Keys.Right))
            _pathField.HandleKey(Keys.Right);
        if (KeyPressed(keyboard, prevKeyboard, Keys.Home))
            _pathField.HandleKey(Keys.Home);
        if (KeyPressed(keyboard, prevKeyboard, Keys.End))
            _pathField.HandleKey(Keys.End);
    }

    public void OnTextInput(char character)
    {
        _pathField.HandleCharacter(character);
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                     int screenWidth, int screenHeight, GameTime gameTime)
    {
        renderer.DrawRect(spriteBatch, new Rectangle(0, 0, screenWidth, screenHeight), OverlayColor);

        int px = (screenWidth - PanelWidth) / 2;
        int py = (screenHeight - PanelHeight) / 2;
        var panel = new Rectangle(px, py, PanelWidth, PanelHeight);
        renderer.DrawRect(spriteBatch, panel, PanelColor);
        renderer.DrawRectOutline(spriteBatch, panel, PanelBorder, 1);

        // Title
        spriteBatch.DrawString(font, "Export Map", new Vector2(px + 10, py + 8), Color.White);

        // Format buttons
        int formatY = py + 8 + font.LineSpacing + 8;
        spriteBatch.DrawString(font, "Format:", new Vector2(px + 10, formatY + 4), LabelColor);

        int btnX = px + 80;
        var jsonRect = new Rectangle(btnX, formatY, 60, 24);
        var pngRect = new Rectangle(btnX + 70, formatY, 60, 24);

        renderer.DrawRect(spriteBatch, jsonRect,
            SelectedFormat == ExportFormat.Json ? FormatActiveColor : FormatInactiveColor);
        renderer.DrawRect(spriteBatch, pngRect,
            SelectedFormat == ExportFormat.Png ? FormatActiveColor : FormatInactiveColor);

        var jsonTextPos = new Vector2(jsonRect.X + (jsonRect.Width - font.MeasureString("JSON").X) / 2,
                                      jsonRect.Y + (jsonRect.Height - font.LineSpacing) / 2);
        var pngTextPos = new Vector2(pngRect.X + (pngRect.Width - font.MeasureString("PNG").X) / 2,
                                     pngRect.Y + (pngRect.Height - font.LineSpacing) / 2);
        spriteBatch.DrawString(font, "JSON", jsonTextPos, Color.White);
        spriteBatch.DrawString(font, "PNG", pngTextPos, Color.White);

        // Path field
        int pathY = formatY + 34;
        spriteBatch.DrawString(font, "Path:", new Vector2(px + 10, pathY + 4), LabelColor);
        var pathBounds = new Rectangle(px + 60, pathY, PanelWidth - 70, 26);
        _pathField.Draw(spriteBatch, font, renderer, pathBounds, gameTime);

        // Hints
        string hint = "[Tab] toggle format    [Enter] export    [Esc] cancel";
        var hintSize = font.MeasureString(hint);
        spriteBatch.DrawString(font, hint,
            new Vector2(px + PanelWidth - hintSize.X - 10, py + PanelHeight - font.LineSpacing - 6), HintColor);
    }

    private static bool KeyPressed(KeyboardState current, KeyboardState prev, Keys key)
        => current.IsKeyDown(key) && prev.IsKeyUp(key);
}
