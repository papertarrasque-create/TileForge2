using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DojoUI;

public class SpriteSheet
{
    public Texture2D Texture { get; }
    public int TileWidth { get; }
    public int TileHeight { get; }
    public int Padding { get; }
    public int StrideX { get; }
    public int StrideY { get; }
    public int Cols { get; }
    public int Rows { get; }
    public string FileName { get; }

    public SpriteSheet(GraphicsDevice graphicsDevice, string path, int tileWidth, int tileHeight, int padding = 0)
    {
        using var stream = File.OpenRead(path);
        Texture = Texture2D.FromStream(graphicsDevice, stream);

        FileName = Path.GetFileName(path);
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        Padding = padding;
        StrideX = tileWidth + padding;
        StrideY = tileHeight + padding;
        Cols = Texture.Width / StrideX;
        Rows = Texture.Height / StrideY;
    }

    public (int col, int row) PixelToGrid(float worldX, float worldY)
    {
        int col = (int)Math.Floor(worldX / StrideX);
        int row = (int)Math.Floor(worldY / StrideY);
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
