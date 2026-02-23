using Xunit;

namespace TileForge.Tests;

/// <summary>
/// Tests for ProjectManager.
///
/// Most of ProjectManager is untestable without MonoGame runtime because it depends on
/// GraphicsDevice (for SpriteSheet construction), GameWindow (for title updates), and
/// several UI components (MapCanvas, PanelDock, MapPanel) that require a running game.
///
/// The key testable method is ParseTileSize, which is marked `internal static`.
/// However, the TileForge project does not currently have an [InternalsVisibleTo]
/// attribute exposing internals to TileForge.Tests.
///
/// To make ParseTileSize testable, add to TileForge.csproj or an AssemblyInfo.cs:
///   [assembly: InternalsVisibleTo("TileForge.Tests")]
///
/// Alternatively, ParseTileSize could be made public since it is a pure utility method
/// with no side effects.
///
/// What WOULD be tested if ParseTileSize were accessible:
///
///   ParseTileSize("16")       -> (16, 16, 0)
///   ParseTileSize("16x24")    -> (16, 24, 0)
///   ParseTileSize("16+1")     -> (16, 16, 1)
///   ParseTileSize("16x24+2")  -> (16, 24, 2)
///   ParseTileSize("abc")      -> returns false
///   ParseTileSize("0")        -> returns false
///   ParseTileSize("")          -> returns false
///   ParseTileSize(" 16 ")     -> (16, 16, 0)  [trims whitespace]
///   ParseTileSize("-5")       -> returns false  [negative values]
///
/// Methods that would need MonoGame to test:
///   - Save()        : requires _state.Sheet (SpriteSheet), _window (GameWindow)
///   - Open()        : invokes dialog with GraphicsDevice-dependent load path
///   - Load()        : creates SpriteSheet (needs GraphicsDevice)
///   - LoadSpritesheet() : creates SpriteSheet (needs GraphicsDevice)
/// </summary>
public class ProjectManagerTests
{
    // Placeholder test to validate the test class compiles and the documentation above is accurate.
    // Remove once InternalsVisibleTo is configured.
    [Fact]
    public void ProjectManager_ParseTileSize_DocumentedAsInternalStatic()
    {
        // ParseTileSize is internal static on ProjectManager.
        // Verify via reflection that the method exists and has the expected signature.
        var method = typeof(ProjectManager).GetMethod(
            "ParseTileSize",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        Assert.NotNull(method);
        Assert.True(method.IsStatic);

        // The method should have 4 parameters: string input, out int width, out int height, out int padding
        var parameters = method.GetParameters();
        Assert.Equal(4, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
    }

    // Since we can't call internal methods directly, use reflection to invoke ParseTileSize.
    // This is acceptable for unit testing internal methods when InternalsVisibleTo is not configured.

    private static bool InvokeParseTileSize(string input, out int width, out int height, out int padding)
    {
        var method = typeof(ProjectManager).GetMethod(
            "ParseTileSize",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

        object[] args = new object[] { input, 0, 0, 0 };
        bool result = (bool)method!.Invoke(null, args)!;
        width = (int)args[1];
        height = (int)args[2];
        padding = (int)args[3];
        return result;
    }

    [Theory]
    [InlineData("16", true, 16, 16, 0)]
    [InlineData("32", true, 32, 32, 0)]
    [InlineData("1", true, 1, 1, 0)]
    public void ParseTileSize_SquareInput_ReturnsSquareDimensions(
        string input, bool expectedResult, int expectedWidth, int expectedHeight, int expectedPadding)
    {
        bool result = InvokeParseTileSize(input, out int w, out int h, out int p);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedWidth, w);
        Assert.Equal(expectedHeight, h);
        Assert.Equal(expectedPadding, p);
    }

    [Theory]
    [InlineData("16x24", true, 16, 24, 0)]
    [InlineData("32x48", true, 32, 48, 0)]
    [InlineData("8x16", true, 8, 16, 0)]
    public void ParseTileSize_RectangularInput_ReturnsDifferentWidthAndHeight(
        string input, bool expectedResult, int expectedWidth, int expectedHeight, int expectedPadding)
    {
        bool result = InvokeParseTileSize(input, out int w, out int h, out int p);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedWidth, w);
        Assert.Equal(expectedHeight, h);
        Assert.Equal(expectedPadding, p);
    }

    [Theory]
    [InlineData("16+1", true, 16, 16, 1)]
    [InlineData("32+2", true, 32, 32, 2)]
    [InlineData("16+0", true, 16, 16, 0)]
    public void ParseTileSize_SquareWithPadding_ReturnsPaddingValue(
        string input, bool expectedResult, int expectedWidth, int expectedHeight, int expectedPadding)
    {
        bool result = InvokeParseTileSize(input, out int w, out int h, out int p);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedWidth, w);
        Assert.Equal(expectedHeight, h);
        Assert.Equal(expectedPadding, p);
    }

    [Theory]
    [InlineData("16x24+2", true, 16, 24, 2)]
    [InlineData("32x48+1", true, 32, 48, 1)]
    [InlineData("8x16+3", true, 8, 16, 3)]
    public void ParseTileSize_RectangularWithPadding_ReturnsAllValues(
        string input, bool expectedResult, int expectedWidth, int expectedHeight, int expectedPadding)
    {
        bool result = InvokeParseTileSize(input, out int w, out int h, out int p);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedWidth, w);
        Assert.Equal(expectedHeight, h);
        Assert.Equal(expectedPadding, p);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("hello")]
    [InlineData("16xabc")]
    [InlineData("abcx16")]
    [InlineData("16+abc")]
    public void ParseTileSize_InvalidInput_ReturnsFalse(string input)
    {
        bool result = InvokeParseTileSize(input, out _, out _, out _);

        Assert.False(result);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0x0")]
    [InlineData("0x16")]
    [InlineData("16x0")]
    public void ParseTileSize_ZeroDimension_ReturnsFalse(string input)
    {
        bool result = InvokeParseTileSize(input, out _, out _, out _);

        Assert.False(result);
    }

    [Theory]
    [InlineData("-5")]
    [InlineData("-1x16")]
    [InlineData("16x-1")]
    public void ParseTileSize_NegativeDimension_ReturnsFalse(string input)
    {
        bool result = InvokeParseTileSize(input, out _, out _, out _);

        Assert.False(result);
    }

    [Fact]
    public void ParseTileSize_WhitespaceAroundInput_TrimsAndParses()
    {
        bool result = InvokeParseTileSize(" 16 ", out int w, out int h, out int p);

        Assert.True(result);
        Assert.Equal(16, w);
        Assert.Equal(16, h);
        Assert.Equal(0, p);
    }

    [Fact]
    public void ParseTileSize_LargeValues_ParsesCorrectly()
    {
        bool result = InvokeParseTileSize("256x512+4", out int w, out int h, out int p);

        Assert.True(result);
        Assert.Equal(256, w);
        Assert.Equal(512, h);
        Assert.Equal(4, p);
    }
}
