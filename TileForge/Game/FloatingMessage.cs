using Microsoft.Xna.Framework;

namespace TileForge.Game;

public class FloatingMessage
{
    public string Text { get; set; }
    public Color Color { get; set; }
    public int TileX { get; set; }
    public int TileY { get; set; }
    public float Timer { get; set; }
    public float VerticalOffset { get; set; }

    public const float Duration = 1.0f;
    public const float DriftPixels = 16f;
}
