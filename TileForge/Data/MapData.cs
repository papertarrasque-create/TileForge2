using System.Collections.Generic;

namespace TileForge.Data;

public class MapData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public List<MapLayer> Layers { get; set; } = new();
    public List<Entity> Entities { get; set; } = new();

    /// <summary>
    /// Entities render after the layer at this index.
    /// Default: last layer index, so new layers added later render above entities.
    /// </summary>
    public int EntityRenderOrder { get; set; }

    public MapData(int width, int height)
    {
        Width = width;
        Height = height;
        Layers.Add(new MapLayer("Ground", width, height));
        Layers.Add(new MapLayer("Objects", width, height));
        EntityRenderOrder = 0;
    }

    public MapLayer GetLayer(string name)
    {
        for (int i = 0; i < Layers.Count; i++)
            if (Layers[i].Name == name)
                return Layers[i];
        return null;
    }

    public bool InBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public bool HasLayer(string name)
    {
        for (int i = 0; i < Layers.Count; i++)
            if (Layers[i].Name == name)
                return true;
        return false;
    }

    public MapLayer AddLayer(string name)
    {
        var layer = new MapLayer(name, Width, Height);
        Layers.Add(layer);
        return layer;
    }

    /// <summary>
    /// Resizes the map to the given dimensions, anchoring at top-left (0,0).
    /// Existing cell data is preserved where coordinates overlap.
    /// Cells beyond old bounds are null; cells beyond new bounds are discarded.
    /// Entities that fall outside the new bounds are removed and returned.
    /// </summary>
    public List<Entity> Resize(int newWidth, int newHeight)
    {
        if (newWidth < 1) newWidth = 1;
        if (newHeight < 1) newHeight = 1;

        int oldWidth = Width;
        int oldHeight = Height;

        foreach (var layer in Layers)
        {
            var oldCells = layer.Cells;
            var newCells = new string[newWidth * newHeight];

            int copyW = System.Math.Min(oldWidth, newWidth);
            int copyH = System.Math.Min(oldHeight, newHeight);

            for (int y = 0; y < copyH; y++)
            {
                for (int x = 0; x < copyW; x++)
                {
                    newCells[x + y * newWidth] = oldCells[x + y * oldWidth];
                }
            }

            layer.Cells = newCells;
        }

        Width = newWidth;
        Height = newHeight;

        // Remove entities outside the new bounds
        var removed = Entities.FindAll(e => e.X < 0 || e.X >= newWidth || e.Y < 0 || e.Y >= newHeight);
        Entities.RemoveAll(e => e.X < 0 || e.X >= newWidth || e.Y < 0 || e.Y >= newHeight);

        return removed;
    }
}
