using TileForge.Data;

namespace TileForge.Editor.Commands;

public class ReorderLayerCommand : ICommand
{
    private readonly MapData _map;
    private readonly int _fromIndex;
    private readonly int _toIndex;

    public ReorderLayerCommand(MapData map, int fromIndex, int toIndex)
    {
        _map = map;
        _fromIndex = fromIndex;
        _toIndex = toIndex;
    }

    public void Execute()
    {
        var layer = _map.Layers[_fromIndex];
        _map.Layers.RemoveAt(_fromIndex);
        _map.Layers.Insert(_toIndex, layer);
    }

    public void Undo()
    {
        var layer = _map.Layers[_toIndex];
        _map.Layers.RemoveAt(_toIndex);
        _map.Layers.Insert(_fromIndex, layer);
    }
}
