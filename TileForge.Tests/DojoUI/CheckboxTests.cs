using DojoUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Xunit;

namespace TileForge.Tests.DojoUI;

public class CheckboxTests
{
    [Fact]
    public void Constructor_DefaultUnchecked()
    {
        var cb = new Checkbox();
        Assert.False(cb.IsChecked);
    }

    [Fact]
    public void IsChecked_SetTrue_ReturnsTrue()
    {
        var cb = new Checkbox();
        cb.IsChecked = true;
        Assert.True(cb.IsChecked);
    }

    [Fact]
    public void IsChecked_SetFalse_ReturnsFalse()
    {
        var cb = new Checkbox();
        cb.IsChecked = true;
        cb.IsChecked = false;
        Assert.False(cb.IsChecked);
    }

    [Fact]
    public void IsChecked_ToggleMultiple_Consistent()
    {
        var cb = new Checkbox();
        cb.IsChecked = true;
        Assert.True(cb.IsChecked);
        cb.IsChecked = false;
        Assert.False(cb.IsChecked);
        cb.IsChecked = true;
        Assert.True(cb.IsChecked);
    }

    // --- InputEvent overload tests ---

    private static MouseState MakeMouse(int x, int y, ButtonState left = ButtonState.Released)
    {
        return new MouseState(x, y, 0, left, ButtonState.Released, ButtonState.Released,
                              ButtonState.Released, ButtonState.Released);
    }

    private static InputEvent MakeClickAt(int x, int y)
    {
        return new InputEvent(
            MakeMouse(x, y, ButtonState.Pressed),
            MakeMouse(x, y, ButtonState.Released));
    }

    private static readonly Rectangle CbBounds = new(100, 100, 22, 22);

    [Fact]
    public void UpdateInputEvent_ClickInBounds_Toggles()
    {
        var cb = new Checkbox();
        var input = MakeClickAt(110, 110);
        bool toggled = cb.Update(input, CbBounds);
        Assert.True(toggled);
        Assert.True(cb.IsChecked);
    }

    [Fact]
    public void UpdateInputEvent_ClickInBounds_ConsumesClick()
    {
        var cb = new Checkbox();
        var input = MakeClickAt(110, 110);
        cb.Update(input, CbBounds);
        Assert.True(input.Consumed);
    }

    [Fact]
    public void UpdateInputEvent_ClickOutsideBounds_DoesNotToggle()
    {
        var cb = new Checkbox();
        var input = MakeClickAt(50, 50);
        bool toggled = cb.Update(input, CbBounds);
        Assert.False(toggled);
        Assert.False(cb.IsChecked);
        Assert.False(input.Consumed);
    }

    [Fact]
    public void UpdateInputEvent_AlreadyConsumed_DoesNotToggle()
    {
        var cb = new Checkbox();
        var input = MakeClickAt(110, 110);
        input.ConsumeClick();
        bool toggled = cb.Update(input, CbBounds);
        Assert.False(toggled);
        Assert.False(cb.IsChecked);
    }
}
