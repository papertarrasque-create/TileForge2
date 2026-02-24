using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DojoUI;

public class Dropdown
{
    private static readonly Color ButtonBg       = new(50, 50, 50);
    private static readonly Color ButtonBgHover  = new(60, 60, 60);
    private static readonly Color ButtonBorder   = new(100, 100, 100);
    private static readonly Color ButtonBorderOpen = new(100, 160, 255);
    private static readonly Color PopupBg        = new(40, 40, 40);
    private static readonly Color PopupBorder    = new(100, 100, 100);
    private static readonly Color ItemHover      = new(70, 70, 70);
    private static readonly Color ItemSelected   = new(55, 65, 85);

    private const int TextPadding  = 6;
    private const int ArrowAreaW   = 16;
    private const int MaxVisible   = 8;

    private string[] _items;
    private int _selectedIndex;
    private bool _isOpen;

    // Popup geometry — computed in Update, read in DrawPopup
    private Rectangle _popupBounds;
    private int _itemHeight;
    private int _hoveredPopupIndex = -1;

    // Button hover tracking
    private bool _buttonHovered;

    // -------------------------------------------------------------------------
    // Construction
    // -------------------------------------------------------------------------

    public Dropdown(string[] items, int selectedIndex = 0)
    {
        _items = items ?? Array.Empty<string>();
        _selectedIndex = ClampIndex(_selectedIndex = selectedIndex, _items.Length);
    }

    // -------------------------------------------------------------------------
    // State
    // -------------------------------------------------------------------------

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => _selectedIndex = ClampIndex(value, _items.Length);
    }

    public string SelectedItem =>
        (_items.Length > 0 && _selectedIndex >= 0 && _selectedIndex < _items.Length)
            ? _items[_selectedIndex]
            : "";

    public bool IsOpen => _isOpen;

    // -------------------------------------------------------------------------
    // Update — returns true if selection changed this frame
    // -------------------------------------------------------------------------

    public bool Update(MouseState mouse, MouseState prevMouse, Rectangle bounds,
                       SpriteFont font, int screenW, int screenH)
    {
        bool clicked = mouse.LeftButton == ButtonState.Pressed &&
                       prevMouse.LeftButton == ButtonState.Released;
        bool rightClicked = mouse.RightButton == ButtonState.Pressed &&
                            prevMouse.RightButton == ButtonState.Released;

        // Recompute popup geometry every frame so DrawPopup is always current
        _itemHeight = font.LineSpacing + 8;
        int visibleCount = Math.Min(_items.Length, MaxVisible);
        int popupH = visibleCount * _itemHeight;
        int popupY = (bounds.Bottom + popupH > screenH) ? bounds.Y - popupH : bounds.Bottom;
        _popupBounds = new Rectangle(bounds.X, popupY, bounds.Width, popupH);

        _buttonHovered = bounds.Contains(mouse.X, mouse.Y);

        if (_isOpen)
        {
            // Update hovered popup item
            _hoveredPopupIndex = -1;
            if (_popupBounds.Contains(mouse.X, mouse.Y))
            {
                int idx = (mouse.Y - _popupBounds.Y) / _itemHeight;
                if (idx >= 0 && idx < visibleCount)
                    _hoveredPopupIndex = idx;
            }

            if (clicked)
            {
                if (_hoveredPopupIndex >= 0)
                {
                    int prev = _selectedIndex;
                    _selectedIndex = _hoveredPopupIndex;
                    _isOpen = false;
                    _hoveredPopupIndex = -1;
                    return _selectedIndex != prev;
                }

                // Click outside popup (including button) — close
                _isOpen = false;
                _hoveredPopupIndex = -1;
                return false;
            }

            if (rightClicked && _hoveredPopupIndex < 0)
            {
                _isOpen = false;
                _hoveredPopupIndex = -1;
            }

            return false;
        }

        // Closed state — open on click
        if (clicked && _buttonHovered && _items.Length > 0)
        {
            _isOpen = true;
            _hoveredPopupIndex = -1;
        }

        return false;
    }

    // -------------------------------------------------------------------------
    // Draw (closed button — always call)
    // -------------------------------------------------------------------------

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer, Rectangle bounds)
    {
        Color bg     = _buttonHovered ? ButtonBgHover : ButtonBg;
        Color border = _isOpen        ? ButtonBorderOpen : ButtonBorder;

        renderer.DrawRect(spriteBatch, bounds, bg);
        renderer.DrawRectOutline(spriteBatch, bounds, border, 1);

        // Selected item text
        if (_items.Length > 0 && _selectedIndex >= 0 && _selectedIndex < _items.Length)
        {
            float textY = bounds.Y + (bounds.Height - font.LineSpacing) / 2f;
            spriteBatch.DrawString(font, _items[_selectedIndex],
                new Vector2(bounds.X + TextPadding, textY), Color.White);
        }

        // Down-arrow: three stacked horizontal rects narrowing toward bottom
        // Drawn inside the right-side ArrowAreaW zone, centred vertically
        int arrowX = bounds.Right - ArrowAreaW;
        int arrowCX = arrowX + ArrowAreaW / 2;
        int arrowCY = bounds.Y + bounds.Height / 2;

        renderer.DrawRect(spriteBatch, new Rectangle(arrowCX - 4, arrowCY - 2, 9, 2), Color.White);
        renderer.DrawRect(spriteBatch, new Rectangle(arrowCX - 2, arrowCY,     5, 2), Color.White);
        renderer.DrawRect(spriteBatch, new Rectangle(arrowCX,     arrowCY + 2, 1, 2), Color.White);
    }

    // -------------------------------------------------------------------------
    // DrawPopup (call AFTER all other draws for z-ordering)
    // -------------------------------------------------------------------------

    public void DrawPopup(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer)
    {
        if (!_isOpen || _items.Length == 0) return;

        renderer.DrawRect(spriteBatch, _popupBounds, PopupBg);
        renderer.DrawRectOutline(spriteBatch, _popupBounds, PopupBorder, 1);

        int visibleCount = Math.Min(_items.Length, MaxVisible);
        for (int i = 0; i < visibleCount; i++)
        {
            int itemY = _popupBounds.Y + i * _itemHeight;
            var itemRect = new Rectangle(_popupBounds.X + 1, itemY, _popupBounds.Width - 2, _itemHeight);

            if (i == _hoveredPopupIndex)
                renderer.DrawRect(spriteBatch, itemRect, ItemHover);
            else if (i == _selectedIndex)
                renderer.DrawRect(spriteBatch, itemRect, ItemSelected);

            float textY = itemY + (_itemHeight - font.LineSpacing) / 2f;
            spriteBatch.DrawString(font, _items[i],
                new Vector2(_popupBounds.X + TextPadding, textY), Color.White);
        }
    }

    // -------------------------------------------------------------------------
    // SetItems
    // -------------------------------------------------------------------------

    public void SetItems(string[] items, int selectedIndex = -1)
    {
        _items = items ?? Array.Empty<string>();

        if (selectedIndex >= 0)
            _selectedIndex = ClampIndex(selectedIndex, _items.Length);
        else
            _selectedIndex = ClampIndex(_selectedIndex, _items.Length);

        // Close popup whenever items change — stale hover indices are invalid
        _isOpen = false;
        _hoveredPopupIndex = -1;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static int ClampIndex(int index, int length)
    {
        if (length == 0) return 0;
        return Math.Max(0, Math.Min(index, length - 1));
    }
}
