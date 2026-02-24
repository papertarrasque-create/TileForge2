using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Game.Screens;

public class GameOverScreen : GameScreen
{
    private static readonly string[] MenuItems = { "Restart", "Return to Editor" };
    private int _selectedIndex;
    private readonly GameStateManager _gameStateManager;

    public override bool IsOverlay => false;

    public GameOverScreen(GameStateManager gameStateManager)
    {
        _gameStateManager = gameStateManager;
    }

    public override void Update(GameTime gameTime, GameInputManager input)
    {
        if (input.IsActionJustPressed(GameAction.MoveUp))
        {
            _selectedIndex--;
            if (_selectedIndex < 0) _selectedIndex = MenuItems.Length - 1;
        }
        if (input.IsActionJustPressed(GameAction.MoveDown))
        {
            _selectedIndex++;
            if (_selectedIndex >= MenuItems.Length) _selectedIndex = 0;
        }

        if (input.IsActionJustPressed(GameAction.Cancel))
        {
            ScreenManager.ExitRequested = true;
            ScreenManager.Clear();
            return;
        }

        if (input.IsActionJustPressed(GameAction.Interact))
        {
            switch (_selectedIndex)
            {
                case 0: // Restart
                    _gameStateManager.RestartRequested = true;
                    ScreenManager.Clear();
                    break;
                case 1: // Return to Editor
                    ScreenManager.ExitRequested = true;
                    ScreenManager.Clear();
                    break;
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch, SpriteFont font,
        Renderer renderer, Rectangle canvasBounds)
    {
        // Dark background
        renderer.DrawRect(spriteBatch, canvasBounds, new Color(0, 0, 0, 220));

        // Title
        var titleText = "GAME OVER";
        var titleSize = font.MeasureString(titleText);
        var titlePos = new Vector2(
            canvasBounds.X + (canvasBounds.Width - titleSize.X) / 2f,
            canvasBounds.Y + canvasBounds.Height * 0.25f);
        spriteBatch.DrawString(font, titleText, titlePos, Color.White);

        // Menu items
        float startY = titlePos.Y + titleSize.Y + 30f;
        for (int i = 0; i < MenuItems.Length; i++)
        {
            var text = MenuItems[i];
            var size = font.MeasureString(text);
            var pos = new Vector2(
                canvasBounds.X + (canvasBounds.Width - size.X) / 2f,
                startY + i * (size.Y + 8f));
            var color = i == _selectedIndex ? Color.Yellow : Color.White;
            spriteBatch.DrawString(font, text, pos, color);
        }
    }
}
