namespace TileForge2.Editor;

public interface ICommand
{
    void Execute();
    void Undo();
}
