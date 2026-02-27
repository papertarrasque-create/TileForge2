using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TileForge.Data;
using TileForge.Game;

namespace TileForge.Play;

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
    public float CurrentMoveDuration { get; set; } = MoveDuration;

    // Floating messages (universal — combat, items, quests, traps, etc.)
    public List<FloatingMessage> FloatingMessages { get; set; } = new();

    public void AddFloatingMessage(string text, Color color, int tileX, int tileY)
    {
        FloatingMessages.Add(new FloatingMessage
        {
            Text = text,
            Color = color,
            TileX = tileX,
            TileY = tileY,
            Timer = FloatingMessage.Duration,
            VerticalOffset = 0f,
        });
    }

    // AP (action point) turn system
    public int PlayerAP { get; set; }
    public bool IsPlayerTurn { get; set; } = true;

    public (int x, int y) GetFacingTile()
    {
        int px = PlayerEntity?.X ?? 0;
        int py = PlayerEntity?.Y ?? 0;
        return PlayerFacing switch
        {
            Direction.Up => (px, py - 1),
            Direction.Down => (px, py + 1),
            Direction.Left => (px - 1, py),
            Direction.Right => (px + 1, py),
            _ => (px, py),
        };
    }

    // Facing direction for sprite flipping and directional attacks
    public Direction PlayerFacing { get; set; } = Direction.Right;
    public Dictionary<string, Direction> EntityFacings { get; set; } = new();

    // Per-sprite damage flash (red on player, white on hit enemy)
    public float PlayerFlashTimer { get; set; }
    public float EntityFlashTimer { get; set; }
    public string FlashedEntityId { get; set; }
    public const float FlashDuration = 0.3f;
}
