namespace TileForge.Game;

/// <summary>
/// Represents a pending map transition triggered by stepping on a Trigger entity.
/// </summary>
public class MapTransitionRequest
{
    public string TargetMap { get; set; }
    public int TargetX { get; set; }
    public int TargetY { get; set; }
}
