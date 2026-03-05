using System.Collections.Generic;

namespace TileForge.Game;

/// <summary>
/// Runtime representation of a map loaded from export JSON.
/// Contains all data needed to play a map: dimensions, layers, group definitions, and entities.
/// </summary>
public class LoadedMap
{
    public string Id { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public List<LoadedMapLayer> Layers { get; set; } = new();
    public List<Data.TileGroup> Groups { get; set; } = new();
    public List<EntityInstance> Entities { get; set; } = new();
}

/// <summary>
/// A single layer in a loaded map. Stores cell data as group name references.
/// </summary>
public class LoadedMapLayer
{
    public string Name { get; set; }
    public string[] Cells { get; set; }

    public string GetCell(int x, int y, int width)
    {
        int index = x + y * width;
        if (index < 0 || index >= Cells.Length) return null;
        return Cells[index];
    }
}
