using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace DojoUI;

/// <summary>
/// Wraps a mouse frame (current + previous MouseState) with a consumed flag.
/// Controls call TryConsumeClick() to claim a click; once consumed, no other
/// control on the same frame can claim it.
/// Must be a class (not struct) so all controls sharing the same instance
/// see the same Consumed flag via reference semantics.
/// </summary>
public class InputEvent
{
    public MouseState Mouse { get; }
    public MouseState PrevMouse { get; }
    public bool Consumed { get; private set; }

    public InputEvent(MouseState mouse, MouseState prevMouse)
    {
        Mouse = mouse;
        PrevMouse = prevMouse;
    }

    /// <summary>
    /// Returns true if a left-click just occurred (pressed this frame,
    /// released last frame), the click is within bounds, and the click
    /// has not yet been consumed. If all conditions are met, marks the
    /// click as consumed.
    /// </summary>
    public bool TryConsumeClick(Rectangle bounds)
    {
        if (Consumed) return false;
        if (Mouse.LeftButton != ButtonState.Pressed) return false;
        if (PrevMouse.LeftButton != ButtonState.Released) return false;
        if (!bounds.Contains(Mouse.X, Mouse.Y)) return false;

        Consumed = true;
        return true;
    }

    /// <summary>
    /// Returns true if a right-click just occurred, is within bounds,
    /// and has not been consumed.
    /// </summary>
    public bool TryConsumeRightClick(Rectangle bounds)
    {
        if (Consumed) return false;
        if (Mouse.RightButton != ButtonState.Pressed) return false;
        if (PrevMouse.RightButton != ButtonState.Released) return false;
        if (!bounds.Contains(Mouse.X, Mouse.Y)) return false;

        Consumed = true;
        return true;
    }

    /// <summary>
    /// Returns true if this frame has an unconsumed left-click anywhere.
    /// Does NOT consume it.
    /// </summary>
    public bool HasUnconsumedClick =>
        !Consumed &&
        Mouse.LeftButton == ButtonState.Pressed &&
        PrevMouse.LeftButton == ButtonState.Released;

    /// <summary>
    /// Explicitly mark click as consumed (e.g., when a dropdown popup is open
    /// and swallows all clicks regardless of position).
    /// </summary>
    public void ConsumeClick() => Consumed = true;
}
