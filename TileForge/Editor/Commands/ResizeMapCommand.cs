using System.Collections.Generic;
using TileForge.Data;

namespace TileForge.Editor.Commands;

public class ResizeMapCommand : ICommand
{
    private readonly MapData _map;
    private readonly int _oldWidth;
    private readonly int _oldHeight;
    private readonly int _newWidth;
    private readonly int _newHeight;
    private readonly Dictionary<string, string[]> _oldCells;
    private List<Entity> _removedEntities;

    public ResizeMapCommand(MapData map, int newWidth, int newHeight)
    {
        _map = map;
        _oldWidth = map.Width;
        _oldHeight = map.Height;
        _newWidth = newWidth;
        _newHeight = newHeight;

        // Snapshot all layer cells before resize
        _oldCells = new Dictionary<string, string[]>();
        foreach (var layer in map.Layers)
        {
            var backup = new string[layer.Cells.Length];
            layer.Cells.CopyTo(backup, 0);
            _oldCells[layer.Name] = backup;
        }
    }

    public void Execute()
    {
        _removedEntities = _map.Resize(_newWidth, _newHeight);
    }

    public void Undo()
    {
        // Restore old dimensions by resizing each layer back
        foreach (var layer in _map.Layers)
        {
            if (_oldCells.TryGetValue(layer.Name, out var backup))
            {
                layer.Cells = new string[backup.Length];
                backup.CopyTo(layer.Cells, 0);
            }
        }

        _map.Width = _oldWidth;
        _map.Height = _oldHeight;

        // Restore removed entities
        if (_removedEntities != null)
        {
            foreach (var entity in _removedEntities)
                _map.Entities.Add(entity);
            _removedEntities = null;
        }
    }
}
