using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DojoUI;

/// <summary>
/// Abstraction over SpriteSheet to allow mock injection in tests.
/// Covers all properties and methods used by TileForge logic and rendering code.
/// </summary>
public interface ISpriteSheet
{
    Texture2D Texture { get; }
    int TileWidth { get; }
    int TileHeight { get; }
    int Padding { get; }
    int StrideX { get; }
    int StrideY { get; }
    int Cols { get; }
    int Rows { get; }
    string FileName { get; }

    (int col, int row) PixelToGrid(float worldX, float worldY);
    Rectangle GetTileRect(int col, int row);
    bool InBounds(int col, int row);
}
