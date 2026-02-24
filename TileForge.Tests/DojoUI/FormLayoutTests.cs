using DojoUI;
using Xunit;

namespace TileForge.Tests.DojoUI;

public class FormLayoutTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var layout = new FormLayout(100, 500, 50, labelWidth: 110, fieldHeight: 22, rowHeight: 28);
        Assert.Equal(100, layout.ContentX);
        Assert.Equal(500, layout.ContentWidth);
        Assert.Equal(50, layout.CursorY);
        Assert.Equal(110, layout.LabelWidth);
        Assert.Equal(22, layout.FieldHeight);
        Assert.Equal(28, layout.RowHeight);
    }

    [Fact]
    public void FieldX_IsContentXPlusLabelWidth()
    {
        var layout = new FormLayout(100, 500, 0, labelWidth: 120);
        Assert.Equal(220, layout.FieldX);
    }

    [Fact]
    public void FieldWidth_IsContentWidthMinusLabelWidth()
    {
        var layout = new FormLayout(100, 500, 0, labelWidth: 120);
        Assert.Equal(380, layout.FieldWidth);
    }

    [Fact]
    public void Space_AdvancesCursorY()
    {
        var layout = new FormLayout(0, 400, 100);
        layout.Space(15);
        Assert.Equal(115, layout.CursorY);
    }

    [Fact]
    public void Space_Zero_NoChange()
    {
        var layout = new FormLayout(0, 400, 100);
        layout.Space(0);
        Assert.Equal(100, layout.CursorY);
    }

    [Fact]
    public void CenterTextY_CentersVertically()
    {
        // Container at Y=100, height 22, text height 14
        // Expected: 100 + (22 - 14) / 2 = 104
        int result = FormLayout.CenterTextY(100, 22, 14);
        Assert.Equal(104, result);
    }

    [Fact]
    public void CenterTextY_ExactFit_NoOffset()
    {
        int result = FormLayout.CenterTextY(50, 20, 20);
        Assert.Equal(50, result);
    }

    [Fact]
    public void CenterTextY_TextTallerThanContainer_NegativeOffset()
    {
        // Text taller than container — centers above container top
        int result = FormLayout.CenterTextY(100, 10, 20);
        Assert.Equal(95, result);
    }

    [Fact]
    public void DefaultLabelWidth_Is110()
    {
        var layout = new FormLayout(0, 400, 0);
        Assert.Equal(110, layout.LabelWidth);
    }

    [Fact]
    public void DefaultFieldHeight_Is22()
    {
        var layout = new FormLayout(0, 400, 0);
        Assert.Equal(22, layout.FieldHeight);
    }

    [Fact]
    public void DefaultRowHeight_Is28()
    {
        var layout = new FormLayout(0, 400, 0);
        Assert.Equal(28, layout.RowHeight);
    }
}
