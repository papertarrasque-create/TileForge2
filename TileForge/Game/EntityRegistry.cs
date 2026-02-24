using System;
using System.Collections.Generic;
using TileForge.Data;

namespace TileForge.Game;

public class EntityRegistry
{
    private readonly Dictionary<string, TileGroup> _entities = new();

    public EntityRegistry(IEnumerable<TileGroup> groups)
    {
        foreach (var g in groups)
        {
            if (g.Type == GroupType.Entity)
                _entities[g.Name] = g;
        }
    }

    public TileGroup Get(string name)
    {
        if (_entities.TryGetValue(name, out var group))
            return group;
        throw new KeyNotFoundException($"Entity group '{name}' not found in registry.");
    }

    public bool Contains(string name) => _entities.ContainsKey(name);

    public EntityType GetEntityType(string name) => Get(name).EntityType;

    public int Count => _entities.Count;
}
