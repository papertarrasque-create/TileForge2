using System.Collections.Generic;
using TileForge.Data;

namespace TileForge.Editor.Commands;

public readonly record struct CellChange(int X, int Y, string LayerName, string OldGroup, string NewGroup);

public class CellStrokeCommand : ICommand
{
    private readonly MapData _map;
    private readonly List<CellChange> _changes;

    public CellStrokeCommand(MapData map, List<CellChange> changes)
    {
        _map = map;
        _changes = changes;
    }

    public bool IsEmpty => _changes.Count == 0;

    public void Execute()
    {
        foreach (var c in _changes)
        {
            var layer = _map.GetLayer(c.LayerName);
            layer?.SetCell(c.X, c.Y, _map.Width, c.NewGroup);
        }
    }

    public void Undo()
    {
        for (int i = _changes.Count - 1; i >= 0; i--)
        {
            var c = _changes[i];
            var layer = _map.GetLayer(c.LayerName);
            layer?.SetCell(c.X, c.Y, _map.Width, c.OldGroup);
        }
    }
}
