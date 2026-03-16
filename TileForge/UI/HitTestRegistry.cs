using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileForge.UI;

public class HitTestRegistry
{
    private readonly Dictionary<string, List<(Rectangle Rect, int Index)>> _zones = new();

    public void Register(string zone, Rectangle rect, int index)
    {
        if (!_zones.TryGetValue(zone, out var list))
        {
            list = new List<(Rectangle, int)>();
            _zones[zone] = list;
        }
        list.Add((rect, index));
    }

    public int HitTest(string zone, Point position)
    {
        if (!_zones.TryGetValue(zone, out var list))
            return -1;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].Rect.Contains(position))
                return list[i].Index;
        }
        return -1;
    }

    public void Clear()
    {
        foreach (var list in _zones.Values)
            list.Clear();
    }
}
