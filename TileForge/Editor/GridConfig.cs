using Microsoft.Xna.Framework;

namespace TileForge.Editor;

/// <summary>
/// Configuration for the map canvas grid overlay.
/// </summary>
public class GridConfig
{
    /// <summary>Grid display mode: Off, Normal (full-tile), or Fine (half-tile subdivisions).</summary>
    public GridMode Mode { get; set; } = GridMode.Normal;

    /// <summary>Grid line color.</summary>
    public Color LineColor { get; set; } = LayoutConstants.CanvasGridColor;

    /// <summary>Map border color.</summary>
    public Color BorderColor { get; set; } = LayoutConstants.CanvasGridBorderColor;

    /// <summary>Subdivision line color (used in Fine mode).</summary>
    public Color SubdivisionColor { get; set; } = LayoutConstants.GridSubdivisionColor;

    /// <summary>Cycles through grid modes: Normal → Fine → Off → Normal.</summary>
    public void CycleMode()
    {
        Mode = Mode switch
        {
            GridMode.Normal => GridMode.Fine,
            GridMode.Fine => GridMode.Off,
            GridMode.Off => GridMode.Normal,
            _ => GridMode.Normal,
        };
    }
}

public enum GridMode
{
    Off,
    Normal,
    Fine,
}
