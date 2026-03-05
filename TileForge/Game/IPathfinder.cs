namespace TileForge.Game;

public interface IPathfinder
{
    /// Returns the next step toward target, or null if no path exists.
    (int x, int y)? GetNextStep(int fromX, int fromY, int toX, int toY);

    /// Returns true if there's a clear line from A to B (for ranged attacks later).
    bool HasLineOfSight(int fromX, int fromY, int toX, int toY);
}
