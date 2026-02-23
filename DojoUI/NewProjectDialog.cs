using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DojoUI;

/// <summary>
/// Multi-field dialog for creating a new project.
/// Fields: Spritesheet path (via browse), Tile size, Map dimensions.
/// </summary>
public class NewProjectDialog : IDialog
{
    private static readonly Color OverlayColor = new(0, 0, 0, 160);
    private static readonly Color PanelColor = new(40, 40, 40);
    private static readonly Color PanelBorder = new(100, 100, 100);
    private static readonly Color LabelColor = new(180, 180, 180);
    private static readonly Color HintColor = new(140, 140, 140);
    private static readonly Color BrowseButtonColor = new(55, 65, 75);
    private static readonly Color BrowseButtonHoverColor = new(65, 80, 100);
    private static readonly Color ErrorColor = new(255, 100, 100);

    private const int PanelWidth = 520;
    private const int PanelHeight = 200;

    private enum Field { TileSize, MapSize }
    private Field _activeField = Field.TileSize;

    private readonly TextInputField _tileSizeInput;
    private readonly TextInputField _mapSizeInput;

    private string _spritesheetPath;
    private bool _browseHovered;
    private string _errorMessage;

    private Action<Action<string>> _browseCallback;

    // Mouse tracking for click edge detection
    private Rectangle _browseRect;
    private bool _prevMouseLeft;

    public bool IsComplete { get; private set; }
    public bool WasCancelled { get; private set; }

    /// <summary>The selected spritesheet path.</summary>
    public string SpritesheetPath => _spritesheetPath;

    /// <summary>The tile size text (e.g. "16", "16x24", "16+1").</summary>
    public string TileSizeText => _tileSizeInput.Text;

    /// <summary>The map size text (e.g. "40x30").</summary>
    public string MapSizeText => _mapSizeInput.Text;

    /// <summary>
    /// Creates a new project dialog.
    /// browseCallback is called when the user clicks "Browse". It receives a callback
    /// that should be invoked with the selected file path.
    /// </summary>
    public NewProjectDialog(Action<Action<string>> browseCallback)
    {
        _browseCallback = browseCallback;
        _tileSizeInput = new TextInputField("16", maxLength: 20);
        _mapSizeInput = new TextInputField("40x30", maxLength: 20);
        _tileSizeInput.IsFocused = true;
    }

    public void Update(KeyboardState keyboard, KeyboardState prevKeyboard, GameTime gameTime)
    {
        // Mouse click handling for Browse button (edge-detected)
        var mouse = Mouse.GetState();
        bool mouseDown = mouse.LeftButton == ButtonState.Pressed;
        if (mouseDown && !_prevMouseLeft && _browseRect.Width > 0 && _browseRect.Contains(mouse.X, mouse.Y))
        {
            _browseCallback?.Invoke(path => { _spritesheetPath = path; });
        }
        _prevMouseLeft = mouseDown;

        if (KeyPressed(keyboard, prevKeyboard, Keys.Escape))
        {
            IsComplete = true;
            WasCancelled = true;
            return;
        }

        if (KeyPressed(keyboard, prevKeyboard, Keys.Tab))
        {
            _activeField = _activeField == Field.TileSize ? Field.MapSize : Field.TileSize;
            _tileSizeInput.IsFocused = _activeField == Field.TileSize;
            _mapSizeInput.IsFocused = _activeField == Field.MapSize;
            return;
        }

        if (KeyPressed(keyboard, prevKeyboard, Keys.Enter))
        {
            if (Validate())
            {
                IsComplete = true;
                WasCancelled = false;
            }
            return;
        }

        var activeInput = _activeField == Field.TileSize ? _tileSizeInput : _mapSizeInput;
        if (KeyPressed(keyboard, prevKeyboard, Keys.Back)) activeInput.HandleKey(Keys.Back);
        if (KeyPressed(keyboard, prevKeyboard, Keys.Delete)) activeInput.HandleKey(Keys.Delete);
        if (KeyPressed(keyboard, prevKeyboard, Keys.Left)) activeInput.HandleKey(Keys.Left);
        if (KeyPressed(keyboard, prevKeyboard, Keys.Right)) activeInput.HandleKey(Keys.Right);
        if (KeyPressed(keyboard, prevKeyboard, Keys.Home)) activeInput.HandleKey(Keys.Home);
        if (KeyPressed(keyboard, prevKeyboard, Keys.End)) activeInput.HandleKey(Keys.End);
    }

    public void OnTextInput(char character)
    {
        var activeInput = _activeField == Field.TileSize ? _tileSizeInput : _mapSizeInput;
        activeInput.HandleCharacter(character);
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

        int y = py + 10;

        // Title
        spriteBatch.DrawString(font, "New Project", new Vector2(px + 10, y), Color.White);
        y += font.LineSpacing + 10;

        // Spritesheet row
        spriteBatch.DrawString(font, "Spritesheet:", new Vector2(px + 10, y), LabelColor);
        string sheetDisplay = _spritesheetPath != null
            ? System.IO.Path.GetFileName(_spritesheetPath)
            : "(none - click Browse)";
        spriteBatch.DrawString(font, sheetDisplay, new Vector2(px + 120, y), _spritesheetPath != null ? Color.White : HintColor);

        // Browse button (layout stored for Update hit-testing)
        _browseRect = new Rectangle(px + PanelWidth - 80, y - 2, 70, font.LineSpacing + 4);
        var mouse = Mouse.GetState();
        _browseHovered = _browseRect.Contains(mouse.X, mouse.Y);
        renderer.DrawRect(spriteBatch, _browseRect, _browseHovered ? BrowseButtonHoverColor : BrowseButtonColor);
        spriteBatch.DrawString(font, "Browse", new Vector2(_browseRect.X + 8, _browseRect.Y + 2), Color.White);

        y += font.LineSpacing + 10;

        // Tile size row
        spriteBatch.DrawString(font, "Tile size:", new Vector2(px + 10, y), LabelColor);
        var tileSizeBounds = new Rectangle(px + 120, y, 150, font.LineSpacing + 8);
        _tileSizeInput.Draw(spriteBatch, font, renderer, tileSizeBounds, gameTime);
        spriteBatch.DrawString(font, "(e.g. 16, 16x24, 16+1)", new Vector2(px + 280, y), HintColor);
        y += font.LineSpacing + 10;

        // Map size row
        spriteBatch.DrawString(font, "Map size:", new Vector2(px + 10, y), LabelColor);
        var mapSizeBounds = new Rectangle(px + 120, y, 150, font.LineSpacing + 8);
        _mapSizeInput.Draw(spriteBatch, font, renderer, mapSizeBounds, gameTime);
        spriteBatch.DrawString(font, "(e.g. 40x30)", new Vector2(px + 280, y), HintColor);
        y += font.LineSpacing + 10;

        // Error message
        if (_errorMessage != null)
            spriteBatch.DrawString(font, _errorMessage, new Vector2(px + 10, y), ErrorColor);

        // Hint bar
        string hint = "[Tab] next field    [Enter] Create    [Esc] Cancel";
        var hintSize = font.MeasureString(hint);
        spriteBatch.DrawString(font, hint,
            new Vector2(px + PanelWidth - hintSize.X - 10, py + PanelHeight - font.LineSpacing - 6), HintColor);
    }

    /// <summary>Sets the spritesheet path programmatically (e.g. after a file browser).</summary>
    public void SetSpritesheetPath(string path) => _spritesheetPath = path;

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(_spritesheetPath))
        { _errorMessage = "Please select a spritesheet image."; return false; }

        if (string.IsNullOrWhiteSpace(_tileSizeInput.Text))
        { _errorMessage = "Please enter a tile size."; return false; }

        if (string.IsNullOrWhiteSpace(_mapSizeInput.Text))
        { _errorMessage = "Please enter a map size."; return false; }

        _errorMessage = null;
        return true;
    }

    private static bool KeyPressed(KeyboardState current, KeyboardState prev, Keys key)
        => current.IsKeyDown(key) && prev.IsKeyUp(key);
}
