using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DojoUI;

public class TooltipManager
{
    private static readonly Color BackgroundColor = new(30, 30, 30, 240);
    private static readonly Color BorderColor = new(80, 80, 80);
    private const int Padding = 4;
    private const int CursorOffsetY = 20;

    private readonly double _delaySeconds;

    private string _currentText = "";
    private int _hoverX;
    private int _hoverY;
    private double _timer;
    private bool _visible;

    public bool IsVisible => _visible;

    public TooltipManager(double delaySeconds = 0.5)
    {
        _delaySeconds = delaySeconds;
    }

    /// <summary>
    /// Call each frame while hovering over an element.
    /// Resets the delay timer if the text or position changes.
    /// </summary>
    public void SetHover(string text, int x, int y)
    {
        if (text != _currentText || x != _hoverX || y != _hoverY)
        {
            _currentText = text;
            _hoverX = x;
            _hoverY = y;
            _timer = 0;
            _visible = false;
        }
    }

    /// <summary>
    /// Hides the tooltip immediately and resets state.
    /// </summary>
    public void ClearHover()
    {
        _currentText = "";
        _timer = 0;
        _visible = false;
    }

    /// <summary>
    /// Advances the delay timer. Call once per frame with elapsed seconds.
    /// </summary>
    public void Update(double elapsedSeconds)
    {
        if (_currentText.Length == 0) return;

        _timer += elapsedSeconds;
        if (_timer >= _delaySeconds)
            _visible = true;
    }

    /// <summary>
    /// Draws the tooltip if visible. Shifts left if it would exceed screenWidth.
    /// Pass screenWidth = 0 to skip the off-screen clamp.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer, int screenWidth = 0)
    {
        if (!_visible || _currentText.Length == 0) return;

        var textSize = font.MeasureString(_currentText);
        int boxWidth  = (int)textSize.X + Padding * 2;
        int boxHeight = (int)textSize.Y + Padding * 2;

        int drawX = _hoverX;
        int drawY = _hoverY + CursorOffsetY;

        // Shift left if it would go off the right edge
        if (screenWidth > 0 && drawX + boxWidth > screenWidth)
            drawX = screenWidth - boxWidth;

        var bgRect = new Rectangle(drawX, drawY, boxWidth, boxHeight);
        renderer.DrawRect(spriteBatch, bgRect, BackgroundColor);
        renderer.DrawRectOutline(spriteBatch, bgRect, BorderColor, 1);

        spriteBatch.DrawString(font, _currentText,
            new Vector2(drawX + Padding, drawY + Padding), Color.White);
    }
}
