using TileForge2.Data;

namespace TileForge2.Editor.Commands;

public class PlaceEntityCommand : ICommand
{
    private readonly MapData _map;
    private readonly Entity _entity;

    public PlaceEntityCommand(MapData map, Entity entity)
    {
        _map = map;
        _entity = entity;
    }

    public void Execute()
    {
        if (!_map.Entities.Contains(_entity))
            _map.Entities.Add(_entity);
    }

    public void Undo()
    {
        _map.Entities.Remove(_entity);
    }
}
