using System;
using System.Collections.Generic;
using TileForge.Data;

namespace TileForge.Game;

public class SimplePathfinder : IPathfinder
{
    private readonly LoadedMap _map;
    private readonly IReadOnlyDictionary<string, TileGroup> _groupsByName;
    private readonly IReadOnlyList<EntityInstance> _activeEntities;
    private readonly PlayerState _player;

    public SimplePathfinder(
        LoadedMap map,
        IReadOnlyDictionary<string, TileGroup> groupsByName,
        IReadOnlyList<EntityInstance> activeEntities,
        PlayerState player)
    {
        _map = map;
        _groupsByName = groupsByName;
        _activeEntities = activeEntities;
        _player = player;
    }

    /// <summary>
    /// Returns the next step toward the target using axis-priority movement,
    /// or null if already at target or both axes are blocked.
    /// </summary>
    public (int x, int y)? GetNextStep(int fromX, int fromY, int toX, int toY)
    {
        if (fromX == toX && fromY == toY)
            return null;

        int dx = toX - fromX;
        int dy = toY - fromY;

        int signX = Math.Sign(dx);
        int signY = Math.Sign(dy);

        int absDx = Math.Abs(dx);
        int absDy = Math.Abs(dy);

        // Primary axis = axis with greater absolute distance; ties go to X
        bool xIsPrimary = absDx >= absDy;

        if (xIsPrimary)
        {
            // Try X first
            if (signX != 0 && IsWalkable(fromX + signX, fromY, fromX, fromY))
                return (fromX + signX, fromY);
            // Try Y as secondary
            if (signY != 0 && IsWalkable(fromX, fromY + signY, fromX, fromY))
                return (fromX, fromY + signY);
        }
        else
        {
            // Try Y first
            if (signY != 0 && IsWalkable(fromX, fromY + signY, fromX, fromY))
                return (fromX, fromY + signY);
            // Try X as secondary
            if (signX != 0 && IsWalkable(fromX + signX, fromY, fromX, fromY))
                return (fromX + signX, fromY);
        }

        // Both axes blocked
        return null;
    }

    /// <summary>
    /// Returns true if there is a clear line of sight from A to B using Bresenham's algorithm.
    /// Intermediate tiles (excluding start and end) must all be passable.
    /// </summary>
    public bool HasLineOfSight(int fromX, int fromY, int toX, int toY)
    {
        // Collect all tiles along the line using Bresenham's algorithm
        int x = fromX;
        int y = fromY;
        int dx = Math.Abs(toX - fromX);
        int dy = Math.Abs(toY - fromY);
        int sx = fromX < toX ? 1 : -1;
        int sy = fromY < toY ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            bool isStart = x == fromX && y == fromY;
            bool isEnd = x == toX && y == toY;

            // Check intermediate tiles only (exclude start and end)
            if (!isStart && !isEnd)
            {
                if (!IsTilePassable(x, y))
                    return false;
            }

            if (isEnd)
                break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }

        return true;
    }

    private bool IsWalkable(int x, int y, int callerX, int callerY)
    {
        // Out of bounds
        if (x < 0 || x >= _map.Width || y < 0 || y >= _map.Height)
            return false;

        // Check tile layers for solid tiles
        foreach (var layer in _map.Layers)
        {
            string groupName = layer.GetCell(x, y, _map.Width);
            if (groupName != null && _groupsByName.TryGetValue(groupName, out var group) && group.IsSolid)
                return false;
        }

        // Check player position
        if (_player != null && _player.X == x && _player.Y == y)
            return false;

        // Check other active entities (excluding the one at callerX, callerY)
        foreach (var entity in _activeEntities)
        {
            if (!entity.IsActive) continue;
            if (entity.X == callerX && entity.Y == callerY) continue; // skip self
            if (entity.X == x && entity.Y == y) return false;
        }

        return true;
    }

    private bool IsTilePassable(int x, int y)
    {
        if (x < 0 || x >= _map.Width || y < 0 || y >= _map.Height)
            return false;

        foreach (var layer in _map.Layers)
        {
            string groupName = layer.GetCell(x, y, _map.Width);
            if (groupName != null && _groupsByName.TryGetValue(groupName, out var group) && group.IsSolid)
                return false;
        }

        return true;
    }
}
