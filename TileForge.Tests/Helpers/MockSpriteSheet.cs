using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Tests.Helpers;

/// <summary>
/// A mock implementation of ISpriteSheet for unit testing.
/// Does not require a GraphicsDevice or any MonoGame runtime resources.
/// All properties are configurable; Texture is always null (tests don't render).
/// </summary>
public class MockSpriteSheet : ISpriteSheet
{
    public Texture2D Texture => null;
    public int TileWidth { get; set; } = 16;
    public int TileHeight { get; set; } = 16;
    public int Padding { get; set; } = 0;
    public int StrideX => TileWidth + Padding;
    public int StrideY => TileHeight + Padding;
    public int Cols { get; set; } = 16;
    public int Rows { get; set; } = 16;
    public string FileName { get; set; } = "mock_sheet.png";

    public MockSpriteSheet() { }

    public MockSpriteSheet(int tileWidth, int tileHeight, int cols = 16, int rows = 16, int padding = 0)
    {
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        Cols = cols;
        Rows = rows;
        Padding = padding;
    }

    public (int col, int row) PixelToGrid(float worldX, float worldY)
    {
        int col = (int)System.Math.Floor(worldX / StrideX);
        int row = (int)System.Math.Floor(worldY / StrideY);
        return (col, row);
    }

    public Rectangle GetTileRect(int col, int row)
    {
        return new Rectangle(col * StrideX, row * StrideY, TileWidth, TileHeight);
    }

    public bool InBounds(int col, int row)
    {
        return col >= 0 && col < Cols && row >= 0 && row < Rows;
    }
}
