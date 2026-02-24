using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Game.Screens;

/// <summary>
/// Pause menu overlay. Sits on top of GameplayScreen, allowing the world to
/// render behind the dark tint. Provides five options: Resume, Save Game,
/// Load Game, Settings, and Return to Editor.
/// </summary>
public class PauseScreen : GameScreen
{
    private static readonly string[] _menuItems = { "Resume", "Save Game", "Load Game", "Settings", "Return to Editor" };

    private readonly SaveManager _saveManager;
    private readonly GameStateManager _gameStateManager;
    private readonly GameInputManager _inputManager;
    private readonly string _bindingsPath;
    private int _selectedIndex;

    public override bool IsOverlay => true;

    public PauseScreen(SaveManager saveManager, GameStateManager gameStateManager,
        GameInputManager inputManager, string bindingsPath)
    {
        _saveManager = saveManager;
        _gameStateManager = gameStateManager;
        _inputManager = inputManager;
        _bindingsPath = bindingsPath;
    }

    public override void Update(GameTime gameTime, GameInputManager input)
    {
        if (input.IsActionJustPressed(GameAction.MoveUp))
        {
            _selectedIndex--;
            if (_selectedIndex < 0)
                _selectedIndex = _menuItems.Length - 1;
        }

        if (input.IsActionJustPressed(GameAction.MoveDown))
        {
            _selectedIndex++;
            if (_selectedIndex >= _menuItems.Length)
                _selectedIndex = 0;
        }

        if (input.IsActionJustPressed(GameAction.Interact))
        {
            switch (_selectedIndex)
            {
                case 0: // Resume
                    ScreenManager.Pop();
                    break;
                case 1: // Save Game
                    ScreenManager.Push(new SaveLoadScreen(_saveManager, _gameStateManager, SaveLoadMode.Save));
                    break;
                case 2: // Load Game
                    ScreenManager.Push(new SaveLoadScreen(_saveManager, _gameStateManager, SaveLoadMode.Load));
                    break;
                case 3: // Settings
                    ScreenManager.Push(new SettingsScreen(_inputManager, _bindingsPath));
                    break;
                case 4: // Return to Editor
                    ScreenManager.ExitRequested = true;
                    ScreenManager.Clear();
                    break;
            }
            return;
        }

        // Escape (mapped to both Cancel and Pause) resumes the game
        if (input.IsActionJustPressed(GameAction.Cancel) || input.IsActionJustPressed(GameAction.Pause))
        {
            ScreenManager.Pop();
        }
    }

    public override void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer, Rectangle canvasBounds)
    {
        // Semi-transparent dark overlay so the world is visible behind the menu
        renderer.DrawRect(spriteBatch, canvasBounds, new Color(0, 0, 0, 150));

        // "PAUSED" title — centered horizontally, roughly 1/3 from the top
        var titleText = "PAUSED";
        var titleSize = font.MeasureString(titleText);
        var titlePos = new Vector2(
            canvasBounds.X + (canvasBounds.Width - titleSize.X) / 2f,
            canvasBounds.Y + canvasBounds.Height / 3f);
        spriteBatch.DrawString(font, titleText, titlePos, Color.White);

        // Menu items — centered below the title
        float menuStartY = titlePos.Y + titleSize.Y + 20f;
        for (int i = 0; i < _menuItems.Length; i++)
        {
            var itemText = _menuItems[i];
            var itemSize = font.MeasureString(itemText);
            var itemPos = new Vector2(
                canvasBounds.X + (canvasBounds.Width - itemSize.X) / 2f,
                menuStartY + i * (itemSize.Y + 8f));
            var color = i == _selectedIndex ? Color.Yellow : Color.White;
            spriteBatch.DrawString(font, itemText, itemPos, color);
        }
    }
}
