using System.Collections.Generic;

namespace TileForge2.Data;

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
}
