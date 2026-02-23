using TileForge.Data;

namespace TileForge.Editor.Commands;

public class MoveEntityCommand : ICommand
{
    private readonly Entity _entity;
    private readonly int _oldX, _oldY;
    private readonly int _newX, _newY;

    public MoveEntityCommand(Entity entity, int oldX, int oldY, int newX, int newY)
    {
        _entity = entity;
        _oldX = oldX;
        _oldY = oldY;
        _newX = newX;
        _newY = newY;
    }

    public bool IsNoOp => _oldX == _newX && _oldY == _newY;

    public void Execute()
    {
        _entity.X = _newX;
        _entity.Y = _newY;
    }

    public void Undo()
    {
        _entity.X = _oldX;
        _entity.Y = _oldY;
    }
}
