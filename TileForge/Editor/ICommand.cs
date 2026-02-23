namespace TileForge.Editor;

public interface ICommand
{
    void Execute();
    void Undo();
}
