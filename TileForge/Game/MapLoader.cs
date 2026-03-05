using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TileForge.Data;
using TileForge.Export;

namespace TileForge.Game;

/// <summary>
/// Loads exported map JSON into runtime-ready LoadedMap structures.
/// Converts ExportData groups into TileGroups and entities into EntityInstances.
/// </summary>
public class MapLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public LoadedMap Load(string json, string mapId = null)
    {
        var data = JsonSerializer.Deserialize<ExportData>(json, JsonOptions);
        if (data == null)
            throw new InvalidOperationException("Failed to deserialize map JSON.");
        return LoadFromExportData(data, mapId);
    }

    public LoadedMap LoadFromExportData(ExportData data, string mapId = null)
    {
        var map = new LoadedMap
        {
            Id = mapId,
            Width = data.Width,
            Height = data.Height,
        };

        // Convert export groups to TileGroups
        if (data.Groups != null)
        {
            foreach (var eg in data.Groups)
            {
                var group = new TileGroup
                {
                    Name = eg.Name,
                    Type = eg.Type == "Entity" ? GroupType.Entity : GroupType.Tile,
                    IsSolid = eg.IsSolid ?? false,
                    IsPassable = eg.IsPassable ?? true,
                    IsHazardous = eg.IsHazardous ?? false,
                    MovementCost = eg.MovementCost ?? 1.0f,
                    DamageType = eg.DamageType,
                    DamagePerTick = eg.DamagePerTick ?? 0,
                    IsPlayer = eg.IsPlayer ?? false,
                    EntityType = Enum.TryParse<EntityType>(eg.EntityType, out var et)
                        ? et
                        : EntityType.Interactable,
                    DefaultProperties = eg.DefaultProperties != null
                        ? new Dictionary<string, string>(eg.DefaultProperties)
                        : new(),
                    Sprites = eg.Sprites?.Select(s => new SpriteRef { Col = s.Col, Row = s.Row }).ToList()
                        ?? new List<SpriteRef>(),
                };
                map.Groups.Add(group);
            }
        }

        // Convert layers
        if (data.Layers != null)
        {
            foreach (var layer in data.Layers)
            {
                map.Layers.Add(new LoadedMapLayer
                {
                    Name = layer.Name,
                    Cells = layer.Cells != null ? (string[])layer.Cells.Clone() : Array.Empty<string>(),
                });
            }
        }

        // Convert entities to EntityInstances
        if (data.Entities != null)
        {
            foreach (var entity in data.Entities)
            {
                map.Entities.Add(new EntityInstance
                {
                    Id = entity.Id,
                    DefinitionName = entity.GroupName,
                    X = entity.X,
                    Y = entity.Y,
                    Properties = entity.Properties != null
                        ? new Dictionary<string, string>(entity.Properties)
                        : new Dictionary<string, string>(),
                    IsActive = true,
                });
            }
        }

        return map;
    }
}
