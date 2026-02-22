using Microsoft.Xna.Framework;
using TileForge2.Data;

namespace TileForge2.Play;

public class PlayState
{
    public Entity PlayerEntity { get; set; }

    /// <summary>
    /// Visual position in grid coordinates (fractional during lerp).
    /// </summary>
    public Vector2 RenderPos { get; set; }

    // Movement lerp
    public bool IsMoving { get; set; }
    public Vector2 MoveFrom { get; set; }
    public Vector2 MoveTo { get; set; }
    public float MoveProgress { get; set; }
    public const float MoveDuration = 0.15f;

    // Status bar message for entity interaction feedback
    public string StatusMessage { get; set; }
    public float StatusMessageTimer { get; set; }
    public const float StatusMessageDuration = 2.0f;
}
