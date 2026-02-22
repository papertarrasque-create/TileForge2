using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DojoUI;

public class ContextMenu
{
    private static readonly Color BackgroundColor = new(40, 40, 40);
    private static readonly Color BorderColor = new(100, 100, 100);
    private static readonly Color HoverColor = new(70, 70, 70);

    private readonly string[] _items;
    private int _x, _y;
    private int _tileCol, _tileRow;
    private bool _isVisible;
    private int _hoveredIndex = -1;
    private int _menuWidth, _menuHeight, _itemHeight;

    private const int Padding = 8;

    public bool IsVisible => _isVisible;
    public int TileCol => _tileCol;
    public int TileRow => _tileRow;

    public ContextMenu(params string[] items)
    {
        _items = items;
    }

    public void Show(int screenX, int screenY, int col, int row,
        SpriteFont font, int screenWidth, int screenHeight)
    {
        _tileCol = col;
        _tileRow = row;
        _isVisible = true;
        _hoveredIndex = -1;

        // Calculate menu dimensions from font
        _itemHeight = font.LineSpacing + Padding;
        float maxTextWidth = 0;
        foreach (var item in _items)
        {
            var size = font.MeasureString(item);
            if (size.X > maxTextWidth) maxTextWidth = size.X;
        }

        _menuWidth = (int)maxTextWidth + Padding * 2;
        _menuHeight = _items.Length * _itemHeight;

        // Clamp to screen bounds
        _x = Math.Min(screenX, screenWidth - _menuWidth);
        _y = Math.Min(screenY, screenHeight - _menuHeight);
        _x = Math.Max(_x, 0);
        _y = Math.Max(_y, 0);
    }

    public void Hide()
    {
        _isVisible = false;
        _hoveredIndex = -1;
    }

    /// <summary>
    /// Returns clicked item index (0+), or -1 if no action this frame.
    /// Dismisses on click outside the menu.
    /// </summary>
    public int Update(MouseState mouse, MouseState prevMouse)
    {
        if (!_isVisible) return -1;

        // Hit test
        _hoveredIndex = -1;
        if (mouse.X >= _x && mouse.X < _x + _menuWidth &&
            mouse.Y >= _y && mouse.Y < _y + _menuHeight)
        {
            int index = (mouse.Y - _y) / _itemHeight;
            if (index >= 0 && index < _items.Length)
                _hoveredIndex = index;
        }

        // Left-click
        if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released)
        {
            if (_hoveredIndex >= 0)
            {
                int clicked = _hoveredIndex;
                Hide();
                return clicked;
            }

            // Clicked outside — dismiss
            Hide();
            return -1;
        }

        // Right-click outside — dismiss
        if (mouse.RightButton == ButtonState.Pressed && prevMouse.RightButton == ButtonState.Released)
        {
            if (_hoveredIndex < 0)
            {
                Hide();
                return -1;
            }
        }

        // Escape — dismiss (handled by TileGridGame before here)
        return -1;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer)
    {
        if (!_isVisible) return;

        var menuRect = new Rectangle(_x, _y, _menuWidth, _menuHeight);

        // Background + border
        renderer.DrawRect(spriteBatch, menuRect, BackgroundColor);
        renderer.DrawRectOutline(spriteBatch, menuRect, BorderColor, 1);

        // Items
        for (int i = 0; i < _items.Length; i++)
        {
            int itemY = _y + i * _itemHeight;

            if (i == _hoveredIndex)
            {
                var hoverRect = new Rectangle(_x + 1, itemY, _menuWidth - 2, _itemHeight);
                renderer.DrawRect(spriteBatch, hoverRect, HoverColor);
            }

            spriteBatch.DrawString(font, _items[i],
                new Vector2(_x + Padding, itemY + Padding / 2), Color.White);
        }
    }
}
