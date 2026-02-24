using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DojoUI;

public class Checkbox
{
    private static readonly Color BoxColor = new(60, 60, 60);
    private static readonly Color BorderColor = new(120, 120, 120);
    private static readonly Color HoverBorderColor = new(160, 160, 160);
    private static readonly Color CheckColor = new(100, 160, 255);  // Accent blue

    public bool IsChecked { get; set; }

    private bool _isHovered;

    /// <summary>
    /// Returns true if the checkbox was toggled this frame.
    /// </summary>
    public bool Update(MouseState mouse, MouseState prevMouse, Rectangle bounds)
    {
        _isHovered = bounds.Contains(mouse.X, mouse.Y);

        if (_isHovered &&
            mouse.LeftButton == ButtonState.Pressed &&
            prevMouse.LeftButton == ButtonState.Released)
        {
            IsChecked = !IsChecked;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Draws the checkbox within the given bounds.
    /// The box is drawn as a 14x14 square centered in the bounds.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Renderer renderer, Rectangle bounds)
    {
        // Center 14x14 box within bounds
        int boxSize = 14;
        int bx = bounds.X + (bounds.Width - boxSize) / 2;
        int by = bounds.Y + (bounds.Height - boxSize) / 2;
        var boxRect = new Rectangle(bx, by, boxSize, boxSize);

        // Background
        renderer.DrawRect(spriteBatch, boxRect, BoxColor);

        // Border
        var borderColor = _isHovered ? HoverBorderColor : BorderColor;
        renderer.DrawRectOutline(spriteBatch, boxRect, borderColor, 1);

        // Check mark (filled inner square when checked)
        if (IsChecked)
        {
            var checkRect = new Rectangle(bx + 3, by + 3, boxSize - 6, boxSize - 6);
            renderer.DrawRect(spriteBatch, checkRect, CheckColor);
        }
    }
}
