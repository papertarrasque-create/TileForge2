using System;
using System.Collections.Generic;
using System.Linq;
using TileForge.Game;

namespace TileForge.Data;

/// <summary>
/// Pure-logic helpers for world grid adjacency and spawn position computation.
/// No MonoGame dependencies — fully testable.
/// </summary>
public static class WorldLayoutHelper
{
    /// <summary>
    /// Returns which direction the player is trying to exit, or null if the target position is in-bounds.
    /// </summary>
    public static Direction? GetExitDirection(int targetX, int targetY, int mapWidth, int mapHeight)
    {
        if (targetY < 0) return Direction.Up;
        if (targetY >= mapHeight) return Direction.Down;
        if (targetX < 0) return Direction.Left;
        if (targetX >= mapWidth) return Direction.Right;
        return null;
    }

    /// <summary>
    /// Returns the name of the map adjacent to the given map in the given direction, or null.
    /// </summary>
    public static string GetNeighbor(WorldLayout layout, string mapName, Direction direction)
    {
        if (layout?.Maps == null || !layout.Maps.TryGetValue(mapName, out var placement))
            return null;

        var (dx, dy) = DirectionToGridOffset(direction);
        int targetGridX = placement.GridX + dx;
        int targetGridY = placement.GridY + dy;

        return GetMapAtCell(layout, targetGridX, targetGridY);
    }

    /// <summary>
    /// Computes the spawn position on the target map when transitioning via an edge.
    /// If entryOverride is non-null, uses the custom spawn point.
    /// Otherwise uses the default: opposite edge, parallel coordinate clamped to target bounds.
    /// </summary>
    public static (int X, int Y) ComputeSpawnPosition(
        Direction exitDirection,
        int sourceWidth, int sourceHeight,
        int playerX, int playerY,
        int targetWidth, int targetHeight,
        EdgeSpawn entryOverride)
    {
        if (entryOverride != null)
            return (entryOverride.X, entryOverride.Y);

        return exitDirection switch
        {
            // Exited right (east) -> spawn on left (west) edge of target, same Y clamped
            Direction.Right => (0, Math.Clamp(playerY, 0, targetHeight - 1)),
            // Exited left (west) -> spawn on right (east) edge of target, same Y clamped
            Direction.Left => (targetWidth - 1, Math.Clamp(playerY, 0, targetHeight - 1)),
            // Exited down (south) -> spawn on top (north) edge of target, same X clamped
            Direction.Down => (Math.Clamp(playerX, 0, targetWidth - 1), 0),
            // Exited up (north) -> spawn on bottom (south) edge of target, same X clamped
            Direction.Up => (Math.Clamp(playerX, 0, targetWidth - 1), targetHeight - 1),
            _ => (0, 0),
        };
    }

    /// <summary>
    /// Returns the map name occupying the given grid cell, or null if empty.
    /// </summary>
    public static string GetMapAtCell(WorldLayout layout, int gridX, int gridY)
    {
        if (layout?.Maps == null) return null;

        foreach (var kvp in layout.Maps)
        {
            if (kvp.Value.GridX == gridX && kvp.Value.GridY == gridY)
                return kvp.Key;
        }
        return null;
    }

    /// <summary>
    /// Returns true if the given grid cell is occupied by a map.
    /// </summary>
    public static bool IsCellOccupied(WorldLayout layout, int gridX, int gridY)
    {
        return GetMapAtCell(layout, gridX, gridY) != null;
    }

    /// <summary>
    /// Returns map names from allMapNames that are NOT placed in the layout.
    /// </summary>
    public static List<string> GetUnplacedMaps(WorldLayout layout, IEnumerable<string> allMapNames)
    {
        if (layout?.Maps == null)
            return allMapNames.ToList();

        return allMapNames.Where(name => !layout.Maps.ContainsKey(name)).ToList();
    }

    /// <summary>
    /// Returns all placed maps as (Name, GridX, GridY) tuples.
    /// </summary>
    public static IReadOnlyList<(string Name, int GridX, int GridY)> GetPlacedMaps(WorldLayout layout)
    {
        if (layout?.Maps == null)
            return Array.Empty<(string, int, int)>();

        return layout.Maps.Select(kvp => (kvp.Key, kvp.Value.GridX, kvp.Value.GridY)).ToList();
    }

    /// <summary>
    /// Gets the EdgeSpawn for the entry direction on the target map.
    /// exitDirection is the direction the player is traveling (e.g., Right = going east).
    /// Returns the target map's entry override for the edge they're entering from.
    /// </summary>
    public static EdgeSpawn GetEntrySpawn(MapPlacement targetPlacement, Direction exitDirection)
    {
        if (targetPlacement == null) return null;

        // Player exits Right (east) -> enters target from its West side
        // Player exits Left (west) -> enters target from its East side
        // Player exits Down (south) -> enters target from its North side
        // Player exits Up (north) -> enters target from its South side
        return exitDirection switch
        {
            Direction.Right => targetPlacement.WestEntry,
            Direction.Left => targetPlacement.EastEntry,
            Direction.Down => targetPlacement.NorthEntry,
            Direction.Up => targetPlacement.SouthEntry,
            _ => null,
        };
    }

    /// <summary>
    /// Gets the exit point for a given direction on the source map.
    /// Returns null if no custom exit point is defined (default = edge-of-map only).
    /// </summary>
    public static EdgeSpawn GetExitPoint(MapPlacement placement, Direction direction)
    {
        if (placement == null) return null;

        return direction switch
        {
            Direction.Up => placement.NorthExit,
            Direction.Down => placement.SouthExit,
            Direction.Left => placement.WestExit,
            Direction.Right => placement.EastExit,
            _ => null,
        };
    }

    private static (int dx, int dy) DirectionToGridOffset(Direction direction)
    {
        return direction switch
        {
            Direction.Up => (0, -1),
            Direction.Down => (0, 1),
            Direction.Left => (-1, 0),
            Direction.Right => (1, 0),
            _ => (0, 0),
        };
    }
}
