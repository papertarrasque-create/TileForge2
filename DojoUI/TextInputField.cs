using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DojoUI;

public class TextInputField
{
    private static readonly Color BackgroundColor = new(50, 50, 50);
    private static readonly Color BorderColor = new(120, 120, 120);
    private static readonly Color FocusedBorderColor = new(200, 200, 200);
    private static readonly Color TextColor = Color.White;
    private static readonly Color CursorColor = Color.White;

    private string _text;
    private int _cursorPos;
    private readonly int _maxLength;
    private readonly Func<char, bool> _charFilter;
    private double _cursorBlinkTimer;
    private bool _cursorVisible = true;
    private static readonly RasterizerState _scissorRasterizer = new() { ScissorTestEnable = true };

    public string Text => _text;
    public bool IsFocused { get; set; }

    public void SetText(string text)
    {
        _text = text ?? "";
        _cursorPos = _text.Length;
        ResetBlink();
    }

    public TextInputField(string defaultText = "", int maxLength = 256, Func<char, bool> charFilter = null)
    {
        _text = defaultText;
        _cursorPos = defaultText.Length;
        _maxLength = maxLength;
        _charFilter = charFilter;
    }

    public void HandleCharacter(char c)
    {
        if (!IsFocused) return;
        if (char.IsControl(c)) return;
        if (_text.Length >= _maxLength) return;
        if (_charFilter != null && !_charFilter(c)) return;

        _text = _text.Insert(_cursorPos, c.ToString());
        _cursorPos++;
        ResetBlink();
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
                }
                break;
            case Keys.Delete:
                if (_cursorPos < _text.Length)
                    _text = _text.Remove(_cursorPos, 1);
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

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer, Rectangle bounds, GameTime gameTime)
    {
        _cursorBlinkTimer += gameTime.ElapsedGameTime.TotalSeconds;
        if (_cursorBlinkTimer >= 0.5)
        {
            _cursorBlinkTimer = 0;
            _cursorVisible = !_cursorVisible;
        }

        renderer.DrawRect(spriteBatch, bounds, BackgroundColor);

        Color border = IsFocused ? FocusedBorderColor : BorderColor;
        renderer.DrawRectOutline(spriteBatch, bounds, border, 1);

        int pad = 6;
        float visibleWidth = bounds.Width - pad * 2;
        float textY = bounds.Y + (bounds.Height - font.LineSpacing) / 2f;

        string beforeCursor = _text[.._cursorPos];
        float cursorX = font.MeasureString(beforeCursor).X;
        float scrollOffset = 0;
        if (IsFocused && cursorX > visibleWidth)
            scrollOffset = cursorX - visibleWidth;

        var prevScissor = spriteBatch.GraphicsDevice.ScissorRectangle;
        spriteBatch.End();
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: _scissorRasterizer);
        var fieldClip = new Rectangle(bounds.X + pad, bounds.Y, (int)visibleWidth, bounds.Height);
        spriteBatch.GraphicsDevice.ScissorRectangle = Rectangle.Intersect(prevScissor, fieldClip);

        var textPos = new Vector2(bounds.X + pad - scrollOffset, textY);
        spriteBatch.DrawString(font, _text, textPos, TextColor);

        if (IsFocused && _cursorVisible)
        {
            float cursorScreenX = bounds.X + pad + cursorX - scrollOffset;
            float cursorY = bounds.Y + 4;
            float cursorH = bounds.Height - 8;
            renderer.DrawRect(spriteBatch, new Rectangle((int)cursorScreenX, (int)cursorY, 1, (int)cursorH), CursorColor);
        }

        spriteBatch.End();
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: _scissorRasterizer);
        spriteBatch.GraphicsDevice.ScissorRectangle = prevScissor;
    }

    /// <summary>
    /// Returns true if the text is wider than the given field bounds (minus padding).
    /// Used by editors to trigger tooltip display on hover.
    /// </summary>
    public bool IsTextOverflowing(SpriteFont font, Rectangle bounds)
    {
        float visibleWidth = bounds.Width - 12; // 6px padding each side
        return font.MeasureString(_text).X > visibleWidth;
    }

    private void ResetBlink()
    {
        _cursorBlinkTimer = 0;
        _cursorVisible = true;
    }
}
