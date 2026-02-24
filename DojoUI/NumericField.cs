using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DojoUI;

public class NumericField
{
    private readonly TextInputField _field;
    private readonly int _min;
    private readonly int _max;

    public NumericField(int defaultValue = 0, int min = 0, int max = 9999)
    {
        _min = min;
        _max = max;
        int clamped = Math.Clamp(defaultValue, min, max);
        _field = new TextInputField(clamped.ToString(), 8, c => char.IsDigit(c) || c == '-');
    }

    public int Value
    {
        get
        {
            if (int.TryParse(_field.Text, out int val))
                return Math.Clamp(val, _min, _max);
            return _min;
        }
        set
        {
            int clamped = Math.Clamp(value, _min, _max);
            _field.SetText(clamped.ToString());
        }
    }

    public string Text => _field.Text;

    public bool IsFocused
    {
        get => _field.IsFocused;
        set => _field.IsFocused = value;
    }

    public void HandleCharacter(char c)
    {
        _field.HandleCharacter(c);
    }

    public void HandleKey(Keys key)
    {
        _field.HandleKey(key);
    }

    /// <summary>
    /// Clamp the current text value to [min, max]. Call this when field loses focus.
    /// </summary>
    public void ClampValue()
    {
        int val = Value;  // triggers parse + clamp
        _field.SetText(val.ToString());
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                     Rectangle bounds, GameTime gameTime)
    {
        _field.Draw(spriteBatch, font, renderer, bounds, gameTime);
    }
}
