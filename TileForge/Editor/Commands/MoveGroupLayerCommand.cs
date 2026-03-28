using System.Collections.Generic;
using TileForge.Data;

namespace TileForge.Editor.Commands;

public class MoveGroupLayerCommand : ICommand
{
    private readonly TileGroup _group;
    private readonly string _oldLayerName;
    private readonly string _newLayerName;

    // Per-map: list of (cellIndex, oldValueOnNewLayer) for cells that were migrated
    private readonly List<(MapData Map, List<(int Index, string OldValueOnNewLayer)> Migrations)> _allMigrations;

    public int ConflictCount { get; }

    public MoveGroupLayerCommand(TileGroup group, string oldLayerName, string newLayerName, IEnumerable<MapData> maps)
    {
        _group = group;
        _oldLayerName = oldLayerName;
        _newLayerName = newLayerName;
        _allMigrations = new();

        int conflicts = 0;

        foreach (var map in maps)
        {
            var oldLayer = map.GetLayer(oldLayerName);
            var newLayer = map.GetLayer(newLayerName);
            if (oldLayer == null || newLayer == null) continue;

            var migrations = new List<(int Index, string OldValueOnNewLayer)>();
            for (int i = 0; i < oldLayer.Cells.Length; i++)
            {
                if (oldLayer.Cells[i] == group.Name)
                {
                    string existing = newLayer.Cells[i];
                    if (existing != null) conflicts++;
                    migrations.Add((i, existing));
                }
            }

            if (migrations.Count > 0)
                _allMigrations.Add((map, migrations));
        }

        ConflictCount = conflicts;
    }

    public void Execute()
    {
        foreach (var (map, migrations) in _allMigrations)
        {
            var oldLayer = map.GetLayer(_oldLayerName);
            var newLayer = map.GetLayer(_newLayerName);
            if (oldLayer == null || newLayer == null) continue;

            foreach (var (index, _) in migrations)
            {
                oldLayer.Cells[index] = null;
                newLayer.Cells[index] = _group.Name;
            }
        }

        _group.LayerName = _newLayerName;
    }

    public void Undo()
    {
        foreach (var (map, migrations) in _allMigrations)
        {
            var oldLayer = map.GetLayer(_oldLayerName);
            var newLayer = map.GetLayer(_newLayerName);
            if (oldLayer == null || newLayer == null) continue;

            foreach (var (index, oldValueOnNewLayer) in migrations)
            {
                oldLayer.Cells[index] = _group.Name;
                newLayer.Cells[index] = oldValueOnNewLayer;
            }
        }

        _group.LayerName = _oldLayerName;
    }
}
