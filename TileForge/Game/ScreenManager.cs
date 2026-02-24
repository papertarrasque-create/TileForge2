using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Game;

public class ScreenManager
{
    private readonly List<GameScreen> _screens = new();

    public bool HasScreens => _screens.Count > 0;

    public bool ExitRequested { get; set; } = false;

    public void Push(GameScreen screen)
    {
        screen.ScreenManager = this;
        screen.OnEnter();
        _screens.Add(screen);
    }

    public void Pop()
    {
        if (_screens.Count == 0)
            return;

        var top = _screens[_screens.Count - 1];
        top.OnExit();
        _screens.RemoveAt(_screens.Count - 1);
    }

    public void Clear()
    {
        for (int i = _screens.Count - 1; i >= 0; i--)
        {
            _screens[i].OnExit();
        }
        _screens.Clear();
    }

    public void Update(GameTime gameTime, GameInputManager input)
    {
        if (_screens.Count == 0)
            return;

        _screens[_screens.Count - 1].Update(gameTime, input);
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer, Rectangle canvasBounds)
    {
        if (_screens.Count == 0)
            return;

        int start = _screens.Count - 1;
        while (start > 0 && _screens[start].IsOverlay)
            start--;

        for (int i = start; i < _screens.Count; i++)
        {
            _screens[i].Draw(spriteBatch, font, renderer, canvasBounds);
        }
    }
}
