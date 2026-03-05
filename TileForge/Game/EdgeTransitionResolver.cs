using System.Collections.Generic;
using TileForge.Data;

namespace TileForge.Game;

/// <summary>
/// Resolves edge-based map transitions using the project's WorldLayout.
/// When the player tries to move off the map boundary, checks for an adjacent map
/// on the world grid and computes the spawn position on the target map.
/// Pure logic — no MonoGame dependencies, fully testable.
/// </summary>
public class EdgeTransitionResolver
{
    private readonly WorldLayout _layout;
    private readonly IReadOnlyDictionary<string, LoadedMap> _projectMaps;

    public EdgeTransitionResolver(WorldLayout layout, IReadOnlyDictionary<string, LoadedMap> projectMaps)
    {
        _layout = layout;
        _projectMaps = projectMaps;
    }

    /// <summary>
    /// Checks if the target position matches a custom exit point on the current map.
    /// Returns a MapTransitionRequest if a matching exit point with a valid neighbor exists, null otherwise.
    /// Exit points are portal-style: they trigger when the player tries to step onto the tile.
    /// </summary>
    public MapTransitionRequest ResolveExitPoint(string currentMapName, int targetX, int targetY)
    {
        if (_layout?.Maps == null || string.IsNullOrEmpty(currentMapName))
            return null;
        if (!_layout.Maps.TryGetValue(currentMapName, out var placement))
            return null;

        Direction[] directions = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
        foreach (var dir in directions)
        {
            var exit = WorldLayoutHelper.GetExitPoint(placement, dir);
            if (exit == null || exit.X != targetX || exit.Y != targetY)
                continue;

            string neighbor = WorldLayoutHelper.GetNeighbor(_layout, currentMapName, dir);
            if (neighbor == null) continue;
            if (!_projectMaps.TryGetValue(neighbor, out var targetMap)) continue;

            _layout.Maps.TryGetValue(neighbor, out var neighborPlacement);
            var entryOverride = WorldLayoutHelper.GetEntrySpawn(neighborPlacement, dir);
            var (spawnX, spawnY) = WorldLayoutHelper.ComputeSpawnPosition(
                dir, 0, 0, targetX, targetY,
                targetMap.Width, targetMap.Height, entryOverride);

            return new MapTransitionRequest
            {
                TargetMap = neighbor,
                TargetX = spawnX,
                TargetY = spawnY,
            };
        }

        return null;
    }

    /// <summary>
    /// Checks if the player is trying to move off the map edge and resolves the transition.
    /// Returns a MapTransitionRequest if a neighbor exists, null otherwise.
    /// </summary>
    /// <param name="currentMapName">Name of the current map (matches WorldLayout key)</param>
    /// <param name="targetX">The X coordinate the player is trying to move to (may be out of bounds)</param>
    /// <param name="targetY">The Y coordinate the player is trying to move to (may be out of bounds)</param>
    /// <param name="playerX">The player's current X coordinate</param>
    /// <param name="playerY">The player's current Y coordinate</param>
    /// <param name="mapWidth">Width of the current map</param>
    /// <param name="mapHeight">Height of the current map</param>
    public MapTransitionRequest Resolve(
        string currentMapName,
        int targetX, int targetY,
        int playerX, int playerY,
        int mapWidth, int mapHeight)
    {
        if (_layout == null || _projectMaps == null || string.IsNullOrEmpty(currentMapName))
            return null;

        var exitDir = WorldLayoutHelper.GetExitDirection(targetX, targetY, mapWidth, mapHeight);
        if (exitDir == null)
            return null;

        string neighbor = WorldLayoutHelper.GetNeighbor(_layout, currentMapName, exitDir.Value);
        if (neighbor == null)
            return null;

        if (!_projectMaps.TryGetValue(neighbor, out var targetMap))
            return null;

        _layout.Maps.TryGetValue(neighbor, out var targetPlacement);
        var entryOverride = WorldLayoutHelper.GetEntrySpawn(targetPlacement, exitDir.Value);

        var (spawnX, spawnY) = WorldLayoutHelper.ComputeSpawnPosition(
            exitDir.Value,
            mapWidth, mapHeight,
            playerX, playerY,
            targetMap.Width, targetMap.Height,
            entryOverride);

        return new MapTransitionRequest
        {
            TargetMap = neighbor,
            TargetX = spawnX,
            TargetY = spawnY,
        };
    }
}
