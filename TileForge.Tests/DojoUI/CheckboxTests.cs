using DojoUI;
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
}
