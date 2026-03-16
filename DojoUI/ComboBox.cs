using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DojoUI;

/// <summary>
/// A text input field with a dropdown of suggestions. The user can type freely
/// or pick from the filtered suggestion list. Combines TextInputField + Dropdown behavior.
/// </summary>
public class ComboBox
{
    private static readonly Color ButtonBg = new(50, 50, 50);
    private static readonly Color ButtonBorder = new(120, 120, 120);
    private static readonly Color FocusedBorder = new(200, 200, 200);
    private static readonly Color PopupBg = new(40, 40, 40);
    private static readonly Color PopupBorder = new(100, 100, 100);
    private static readonly Color ItemHover = new(70, 70, 70);
    private static readonly Color TextColor = Color.White;
    private static readonly Color CursorColor = Color.White;
    private static readonly Color HintColor = new(120, 120, 120);

    private const int TextPadding = 6;
    private const int ArrowAreaW = 16;
    private const int MaxVisible = 6;

    private string _text;
    private int _cursorPos;
    private readonly int _maxLength;
    private double _cursorBlinkTimer;
    private bool _cursorVisible = true;
    private static readonly RasterizerState _scissorRasterizer = new() { ScissorTestEnable = true };

    private string[] _suggestions = Array.Empty<string>();
    private List<string> _filtered = new();
    private bool _isOpen;
    private Rectangle _popupBounds;
    private int _itemHeight;
    private int _hoveredPopupIndex = -1;

    public string Text => _text;
    public bool IsFocused { get; set; }

    public ComboBox(string defaultText = "", int maxLength = 256)
    {
        _text = defaultText;
        _cursorPos = defaultText.Length;
        _maxLength = maxLength;
    }

    public void SetText(string text)
    {
        _text = text ?? "";
        _cursorPos = _text.Length;
        ResetBlink();
    }

    public void SetSuggestions(string[] suggestions)
    {
        _suggestions = suggestions ?? Array.Empty<string>();
    }

    public void HandleCharacter(char c)
    {
        if (!IsFocused) return;
        if (char.IsControl(c)) return;
        if (_text.Length >= _maxLength) return;

        _text = _text.Insert(_cursorPos, c.ToString());
        _cursorPos++;
        ResetBlink();
        RebuildFiltered();
        _isOpen = _filtered.Count > 0;
    }

    public void HandleKey(Keys key)
    {
        if (!IsFocused) return;

        switch (key)
        {
            case Keys.Back:
                if (_cursorPos > 0)
                {
                    _text = _text.Remove(_cursorPos - 1, 1);
                    _cursorPos--;
                    RebuildFiltered();
                    _isOpen = _filtered.Count > 0;
                }
                break;
            case Keys.Delete:
                if (_cursorPos < _text.Length)
                {
                    _text = _text.Remove(_cursorPos, 1);
                    RebuildFiltered();
                    _isOpen = _filtered.Count > 0;
                }
                break;
            case Keys.Left:
                if (_cursorPos > 0) _cursorPos--;
                break;
            case Keys.Right:
                if (_cursorPos < _text.Length) _cursorPos++;
                break;
            case Keys.Home:
                _cursorPos = 0;
                break;
            case Keys.End:
                _cursorPos = _text.Length;
                break;
        }
        ResetBlink();
    }

    public void Update(GameTime gameTime)
    {
        _cursorBlinkTimer += gameTime.ElapsedGameTime.TotalSeconds;
        if (_cursorBlinkTimer >= 0.5)
        {
            _cursorBlinkTimer = 0;
            _cursorVisible = !_cursorVisible;
        }
    }

    /// <summary>
    /// Handles mouse interaction. Call every frame. Returns true if a suggestion was picked.
    /// </summary>
    public bool UpdateMouse(MouseState mouse, MouseState prevMouse, Rectangle bounds,
                            SpriteFont font, int screenW, int screenH)
    {
        bool clicked = mouse.LeftButton == ButtonState.Pressed &&
                       prevMouse.LeftButton == ButtonState.Released;

        // Compute popup geometry
        _itemHeight = font.LineSpacing + 8;
        int visibleCount = Math.Min(_filtered.Count, MaxVisible);
        int popupH = visibleCount * _itemHeight;

        int popupW = bounds.Width;
        foreach (var item in _filtered)
        {
            int itemW = (int)font.MeasureString(item).X + TextPadding * 2;
            if (itemW > popupW) popupW = itemW;
        }
        popupW = Math.Min(popupW, screenW - bounds.X);

        int popupY = (bounds.Bottom + popupH > screenH) ? bounds.Y - popupH : bounds.Bottom;
        _popupBounds = new Rectangle(bounds.X, popupY, popupW, popupH);

        if (_isOpen && visibleCount > 0)
        {
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
                    _text = _filtered[_hoveredPopupIndex];
                    _cursorPos = _text.Length;
                    _isOpen = false;
                    _hoveredPopupIndex = -1;
                    ResetBlink();
                    return true;
                }

                // Click on arrow area toggles popup
                var arrowRect = new Rectangle(bounds.Right - ArrowAreaW, bounds.Y, ArrowAreaW, bounds.Height);
                if (arrowRect.Contains(mouse.X, mouse.Y))
                {
                    _isOpen = false;
                    _hoveredPopupIndex = -1;
                    return false;
                }

                // Click on text area — keep open if focused
                if (bounds.Contains(mouse.X, mouse.Y))
                    return false;

                // Click outside — close
                _isOpen = false;
                _hoveredPopupIndex = -1;
                return false;
            }

            return false;
        }

        // Closed state
        if (clicked)
        {
            var arrowRect = new Rectangle(bounds.Right - ArrowAreaW, bounds.Y, ArrowAreaW, bounds.Height);
            if (arrowRect.Contains(mouse.X, mouse.Y) && _suggestions.Length > 0)
            {
                // Arrow click: show all suggestions (unfiltered)
                _filtered.Clear();
                _filtered.AddRange(_suggestions);
                _isOpen = _filtered.Count > 0;
                _hoveredPopupIndex = -1;
            }
        }

        return false;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                     Rectangle bounds, GameTime gameTime)
    {
        Update(gameTime);

        renderer.DrawRect(spriteBatch, bounds, ButtonBg);
        Color border = IsFocused ? FocusedBorder : ButtonBorder;
        renderer.DrawRectOutline(spriteBatch, bounds, border, 1);

        // Text area (excluding arrow)
        int textAreaW = bounds.Width - ArrowAreaW;
        float visibleWidth = textAreaW - TextPadding * 2;
        float textY = bounds.Y + (bounds.Height - font.LineSpacing) / 2f;

        string beforeCursor = _text[.._cursorPos];
        float cursorX = font.MeasureString(beforeCursor).X;
        float scrollOffset = 0;
        if (IsFocused && cursorX > visibleWidth)
            scrollOffset = cursorX - visibleWidth;

        var gd = spriteBatch.GraphicsDevice;
        var prevScissor = gd.ScissorRectangle;
        var prevRasterizer = gd.RasterizerState;
        spriteBatch.End();
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: _scissorRasterizer);
        var fieldClip = new Rectangle(bounds.X + TextPadding, bounds.Y, (int)visibleWidth, bounds.Height);
        gd.ScissorRectangle = Rectangle.Intersect(prevScissor, fieldClip);

        if (_text.Length > 0)
        {
            var textPos = new Vector2(bounds.X + TextPadding - scrollOffset, textY);
            spriteBatch.DrawString(font, _text, textPos, TextColor);
        }
        else if (!IsFocused && _suggestions.Length > 0)
        {
            spriteBatch.DrawString(font, "(select or type)",
                new Vector2(bounds.X + TextPadding, textY), HintColor);
        }

        if (IsFocused && _cursorVisible)
        {
            float cursorScreenX = bounds.X + TextPadding + cursorX - scrollOffset;
            float cy = bounds.Y + 4;
            float ch = bounds.Height - 8;
            renderer.DrawRect(spriteBatch, new Rectangle((int)cursorScreenX, (int)cy, 1, (int)ch), CursorColor);
        }

        spriteBatch.End();
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: prevRasterizer);
        gd.ScissorRectangle = prevScissor;

        // Down-arrow indicator (when suggestions available)
        if (_suggestions.Length > 0)
        {
            int arrowX = bounds.Right - ArrowAreaW;
            int arrowCX = arrowX + ArrowAreaW / 2;
            int arrowCY = bounds.Y + bounds.Height / 2;

            renderer.DrawRect(spriteBatch, new Rectangle(arrowCX - 4, arrowCY - 2, 9, 2), Color.White);
            renderer.DrawRect(spriteBatch, new Rectangle(arrowCX - 2, arrowCY, 5, 2), Color.White);
            renderer.DrawRect(spriteBatch, new Rectangle(arrowCX, arrowCY + 2, 1, 2), Color.White);
        }
    }

    public void DrawPopup(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer)
    {
        if (!_isOpen || _filtered.Count == 0) return;

        int visibleCount = Math.Min(_filtered.Count, MaxVisible);

        renderer.DrawRect(spriteBatch, _popupBounds, PopupBg);
        renderer.DrawRectOutline(spriteBatch, _popupBounds, PopupBorder, 1);

        for (int i = 0; i < visibleCount; i++)
        {
            int itemY = _popupBounds.Y + i * _itemHeight;
            var itemRect = new Rectangle(_popupBounds.X + 1, itemY, _popupBounds.Width - 2, _itemHeight);

            if (i == _hoveredPopupIndex)
                renderer.DrawRect(spriteBatch, itemRect, ItemHover);

            float ty = itemY + (_itemHeight - font.LineSpacing) / 2f;
            spriteBatch.DrawString(font, _filtered[i],
                new Vector2(_popupBounds.X + TextPadding, ty), Color.White);
        }
    }

    /// <summary>
    /// Returns true if the popup is open and should block clicks behind it.
    /// </summary>
    public bool IsPopupOpen => _isOpen && _filtered.Count > 0;

    public bool IsTextOverflowing(SpriteFont font, Rectangle bounds)
    {
        float visibleWidth = bounds.Width - TextPadding * 2 - ArrowAreaW;
        return font.MeasureString(_text).X > visibleWidth;
    }

    public void ClosePopup()
    {
        _isOpen = false;
        _hoveredPopupIndex = -1;
    }

    private void RebuildFiltered()
    {
        _filtered.Clear();
        if (_suggestions.Length == 0) return;

        string query = _text.Trim();
        if (query.Length == 0)
        {
            // Show all when text is empty
            _filtered.AddRange(_suggestions);
            return;
        }

        foreach (var s in _suggestions)
        {
            if (s.Contains(query, StringComparison.OrdinalIgnoreCase))
                _filtered.Add(s);
        }
    }

    private void ResetBlink()
    {
        _cursorBlinkTimer = 0;
        _cursorVisible = true;
    }
}
