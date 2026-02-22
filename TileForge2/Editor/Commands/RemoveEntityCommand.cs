using TileForge2.Data;

namespace TileForge2.Editor.Commands;

public class RemoveEntityCommand : ICommand
{
    private readonly MapData _map;
    private readonly Entity _entity;
    private readonly EditorState _state;

    public RemoveEntityCommand(MapData map, Entity entity, EditorState state)
    {
        _map = map;
        _entity = entity;
        _state = state;
    }

    public void Execute()
    {
        _map.Entities.Remove(_entity);
        if (_state.SelectedEntityId == _entity.Id)
            _state.SelectedEntityId = null;
    }

    public void Undo()
    {
        if (!_map.Entities.Contains(_entity))
            _map.Entities.Add(_entity);
    }
}
