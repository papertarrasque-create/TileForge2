using System;
using System.Collections.Generic;
using TileForge.Data;

namespace TileForge.Game;

public class TileRegistry
{
    private readonly Dictionary<string, TileGroup> _tiles = new();

    public TileRegistry(IEnumerable<TileGroup> groups)
    {
        foreach (var g in groups)
        {
            if (g.Type == GroupType.Tile)
                _tiles[g.Name] = g;
        }
    }

    public TileGroup Get(string name)
    {
        if (_tiles.TryGetValue(name, out var group))
            return group;
        throw new KeyNotFoundException($"Tile group '{name}' not found in registry.");
    }

    public bool Contains(string name) => _tiles.ContainsKey(name);

    public bool IsPassable(string name) => Get(name).IsPassable;

    public float GetMovementCost(string name) => Get(name).MovementCost;

    public bool IsHazardous(string name) => Get(name).IsHazardous;

    public int GetDamagePerTick(string name) => Get(name).DamagePerTick;

    public string GetDamageType(string name) => Get(name).DamageType;

    public int Count => _tiles.Count;
}
