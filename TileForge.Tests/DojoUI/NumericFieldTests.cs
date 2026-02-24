using DojoUI;
using Xunit;

namespace TileForge.Tests.DojoUI;

public class NumericFieldTests
{
    [Fact]
    public void Constructor_SetsDefaultValue()
    {
        var field = new NumericField(42, 0, 100);
        Assert.Equal(42, field.Value);
    }

    [Fact]
    public void Constructor_ClampsToMin()
    {
        var field = new NumericField(-5, 0, 100);
        Assert.Equal(0, field.Value);
    }

    [Fact]
    public void Constructor_ClampsToMax()
    {
        var field = new NumericField(200, 0, 100);
        Assert.Equal(100, field.Value);
    }

    [Fact]
    public void Value_SetAndGet()
    {
        var field = new NumericField(0, 0, 100);
        field.Value = 50;
        Assert.Equal(50, field.Value);
    }

    [Fact]
    public void Value_Setter_ClampsToRange()
    {
        var field = new NumericField(0, 10, 50);
        field.Value = 5;
        Assert.Equal(10, field.Value);
        field.Value = 100;
        Assert.Equal(50, field.Value);
    }

    [Fact]
    public void HandleCharacter_AcceptsDigit()
    {
        var field = new NumericField(0, 0, 9999);
        field.IsFocused = true;
        // Clear the default "0" text first
        field.Value = 0;
        field.HandleCharacter('5');
        // Text should contain the digit
        Assert.Contains("5", field.Text);
    }

    [Fact]
    public void ClampValue_ClampsTextToRange()
    {
        var field = new NumericField(0, 10, 50);
        field.IsFocused = true;
        // Force-set via Value then clamp
        field.Value = 5;
        field.ClampValue();
        Assert.Equal(10, field.Value);
    }

    [Fact]
    public void Text_ReturnsCurrentFieldText()
    {
        var field = new NumericField(42, 0, 100);
        Assert.Equal("42", field.Text);
    }

    [Fact]
    public void IsFocused_DefaultFalse()
    {
        var field = new NumericField();
        Assert.False(field.IsFocused);
    }

    [Fact]
    public void IsFocused_CanSetTrue()
    {
        var field = new NumericField();
        field.IsFocused = true;
        Assert.True(field.IsFocused);
    }
}
