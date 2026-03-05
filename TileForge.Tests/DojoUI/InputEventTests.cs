using DojoUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Xunit;

namespace TileForge.Tests.DojoUI;

public class InputEventTests
{
    private static MouseState MakeMouse(int x, int y, ButtonState left = ButtonState.Released,
                                         ButtonState right = ButtonState.Released)
    {
        return new MouseState(x, y, 0, left, ButtonState.Released, right, ButtonState.Released, ButtonState.Released);
    }

    private static InputEvent MakeClickAt(int x, int y)
    {
        var prev = MakeMouse(x, y, ButtonState.Released);
        var curr = MakeMouse(x, y, ButtonState.Pressed);
        return new InputEvent(curr, prev);
    }

    private static InputEvent MakeRightClickAt(int x, int y)
    {
        var prev = MakeMouse(x, y, right: ButtonState.Released);
        var curr = MakeMouse(x, y, right: ButtonState.Pressed);
        return new InputEvent(curr, prev);
    }

    private static InputEvent MakeNoClick(int x, int y)
    {
        var prev = MakeMouse(x, y, ButtonState.Released);
        var curr = MakeMouse(x, y, ButtonState.Released);
        return new InputEvent(curr, prev);
    }

    private static readonly Rectangle Bounds = new(100, 100, 200, 200);

    // --- TryConsumeClick ---

    [Fact]
    public void TryConsumeClick_WithinBounds_ReturnsTrue()
    {
        var input = MakeClickAt(150, 150);
        Assert.True(input.TryConsumeClick(Bounds));
    }

    [Fact]
    public void TryConsumeClick_OutsideBounds_ReturnsFalse()
    {
        var input = MakeClickAt(50, 50);
        Assert.False(input.TryConsumeClick(Bounds));
    }

    [Fact]
    public void TryConsumeClick_NoClick_ReturnsFalse()
    {
        var input = MakeNoClick(150, 150);
        Assert.False(input.TryConsumeClick(Bounds));
    }

    [Fact]
    public void TryConsumeClick_AlreadyConsumed_ReturnsFalse()
    {
        var input = MakeClickAt(150, 150);
        input.TryConsumeClick(Bounds);
        Assert.False(input.TryConsumeClick(Bounds));
    }

    [Fact]
    public void TryConsumeClick_SetsConsumedFlag()
    {
        var input = MakeClickAt(150, 150);
        Assert.False(input.Consumed);
        input.TryConsumeClick(Bounds);
        Assert.True(input.Consumed);
    }

    [Fact]
    public void TryConsumeClick_OutsideBounds_DoesNotConsume()
    {
        var input = MakeClickAt(50, 50);
        input.TryConsumeClick(Bounds);
        Assert.False(input.Consumed);
    }

    // --- TryConsumeRightClick ---

    [Fact]
    public void TryConsumeRightClick_WithinBounds_ReturnsTrue()
    {
        var input = MakeRightClickAt(150, 150);
        Assert.True(input.TryConsumeRightClick(Bounds));
    }

    [Fact]
    public void TryConsumeRightClick_OutsideBounds_ReturnsFalse()
    {
        var input = MakeRightClickAt(50, 50);
        Assert.False(input.TryConsumeRightClick(Bounds));
    }

    // --- ConsumeClick ---

    [Fact]
    public void ConsumeClick_SetsConsumedFlag()
    {
        var input = MakeClickAt(150, 150);
        input.ConsumeClick();
        Assert.True(input.Consumed);
    }

    [Fact]
    public void ConsumeClick_BlocksSubsequentTryConsume()
    {
        var input = MakeClickAt(150, 150);
        input.ConsumeClick();
        Assert.False(input.TryConsumeClick(Bounds));
    }

    // --- HasUnconsumedClick ---

    [Fact]
    public void HasUnconsumedClick_TrueWhenClickAndNotConsumed()
    {
        var input = MakeClickAt(150, 150);
        Assert.True(input.HasUnconsumedClick);
    }

    [Fact]
    public void HasUnconsumedClick_FalseWhenNoClick()
    {
        var input = MakeNoClick(150, 150);
        Assert.False(input.HasUnconsumedClick);
    }

    [Fact]
    public void HasUnconsumedClick_FalseWhenConsumed()
    {
        var input = MakeClickAt(150, 150);
        input.ConsumeClick();
        Assert.False(input.HasUnconsumedClick);
    }

    // --- Multi-control scenario ---

    [Fact]
    public void MultipleControls_FirstConsumesSecondDoesNot()
    {
        var input = MakeClickAt(150, 150);

        var bounds1 = new Rectangle(100, 100, 200, 200);
        var bounds2 = new Rectangle(100, 100, 200, 200);

        Assert.True(input.TryConsumeClick(bounds1));
        Assert.False(input.TryConsumeClick(bounds2));
    }

    [Fact]
    public void MultipleControls_FirstMissesSecondConsumes()
    {
        var input = MakeClickAt(150, 150);

        var missBounds = new Rectangle(0, 0, 50, 50);
        var hitBounds = new Rectangle(100, 100, 200, 200);

        Assert.False(input.TryConsumeClick(missBounds));
        Assert.True(input.TryConsumeClick(hitBounds));
    }

    // --- Reference semantics ---

    [Fact]
    public void ReferenceSemantics_SharedInstanceSeesConsumed()
    {
        var input = MakeClickAt(150, 150);
        var sameRef = input;

        input.TryConsumeClick(Bounds);
        Assert.True(sameRef.Consumed);
    }
}
