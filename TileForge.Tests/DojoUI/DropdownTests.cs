using System;
using DojoUI;
using Xunit;

namespace TileForge.Tests.DojoUI;

public class DropdownTests
{
    // -------------------------------------------------------------------------
    // 1. Constructor_DefaultIndex_Zero
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_DefaultIndex_Zero()
    {
        var dropdown = new Dropdown(new[] { "A", "B", "C" });
        Assert.Equal(0, dropdown.SelectedIndex);
    }

    // -------------------------------------------------------------------------
    // 2. Constructor_CustomIndex_Respected
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_CustomIndex_Respected()
    {
        var dropdown = new Dropdown(new[] { "A", "B", "C" }, 2);
        Assert.Equal(2, dropdown.SelectedIndex);
    }

    // -------------------------------------------------------------------------
    // 3. SelectedItem_ReturnsCorrectString
    // -------------------------------------------------------------------------

    [Fact]
    public void SelectedItem_ReturnsCorrectString()
    {
        var dropdown = new Dropdown(new[] { "A", "B", "C" }, 1);
        Assert.Equal("B", dropdown.SelectedItem);
    }

    // -------------------------------------------------------------------------
    // 4. SelectedItem_EmptyArray_ReturnsEmpty
    // -------------------------------------------------------------------------

    [Fact]
    public void SelectedItem_EmptyArray_ReturnsEmpty()
    {
        var dropdown = new Dropdown(Array.Empty<string>());
        Assert.Equal("", dropdown.SelectedItem);
    }

    // -------------------------------------------------------------------------
    // 5. SetItems_UpdatesList
    // -------------------------------------------------------------------------

    [Fact]
    public void SetItems_UpdatesList()
    {
        var dropdown = new Dropdown(new[] { "A", "B", "C" }, 0);
        dropdown.SetItems(new[] { "X", "Y" }, 1);
        Assert.Equal("Y", dropdown.SelectedItem);
    }

    // -------------------------------------------------------------------------
    // 6. SetItems_ClampsIndex
    // -------------------------------------------------------------------------

    [Fact]
    public void SetItems_ClampsIndex()
    {
        // Start at index 5 in a 6-item list
        var dropdown = new Dropdown(new[] { "A", "B", "C", "D", "E", "F" }, 5);
        Assert.Equal(5, dropdown.SelectedIndex);

        // Replace with 3-item list, keep current (-1) → clamps 5 → 2
        dropdown.SetItems(new[] { "X", "Y", "Z" });
        Assert.Equal(2, dropdown.SelectedIndex);
    }

    // -------------------------------------------------------------------------
    // 7. SetItems_KeepsIndex_WhenMinusOne
    // -------------------------------------------------------------------------

    [Fact]
    public void SetItems_KeepsIndex_WhenMinusOne()
    {
        var dropdown = new Dropdown(new[] { "A", "B", "C" }, 1);
        dropdown.SetItems(new[] { "X", "Y", "Z" }, -1);
        Assert.Equal(1, dropdown.SelectedIndex);
        Assert.Equal("Y", dropdown.SelectedItem);
    }

    // -------------------------------------------------------------------------
    // 8. SetItems_ExplicitIndex_Sets
    // -------------------------------------------------------------------------

    [Fact]
    public void SetItems_ExplicitIndex_Sets()
    {
        var dropdown = new Dropdown(new[] { "A", "B", "C" }, 0);
        dropdown.SetItems(new[] { "X", "Y", "Z" }, 1);
        Assert.Equal(1, dropdown.SelectedIndex);
        Assert.Equal("Y", dropdown.SelectedItem);
    }

    // -------------------------------------------------------------------------
    // 9. SelectedIndex_Setter_Clamps
    // -------------------------------------------------------------------------

    [Fact]
    public void SelectedIndex_Setter_Clamps()
    {
        var dropdown = new Dropdown(new[] { "A", "B", "C" });

        // Below zero → clamps to 0
        dropdown.SelectedIndex = -1;
        Assert.Equal(0, dropdown.SelectedIndex);

        // Beyond length → clamps to last valid index
        dropdown.SelectedIndex = 99;
        Assert.Equal(2, dropdown.SelectedIndex);

        // Valid value passes through unchanged
        dropdown.SelectedIndex = 1;
        Assert.Equal(1, dropdown.SelectedIndex);
    }

    // -------------------------------------------------------------------------
    // 10. Constructor_EmptyArray_NoException
    // -------------------------------------------------------------------------

    [Fact]
    public void Constructor_EmptyArray_NoException()
    {
        var exception = Record.Exception(() => new Dropdown(Array.Empty<string>()));
        Assert.Null(exception);
    }
}
