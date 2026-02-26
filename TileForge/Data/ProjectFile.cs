using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TileForge.Editor;

namespace TileForge.Data;

public static class ProjectFile
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    // --- JSON data model ---

    public class ProjectData
    {
        public int Version { get; set; } = 1;
        public SpritesheetInfo Spritesheet { get; set; }
        public List<GroupData> Groups { get; set; } = new();
        // V1 single-map fields (null when saving V2)
        public MapInfo Map { get; set; }
        public List<EntityData> Entities { get; set; }
        // V2 multimap
        public List<MapDocumentData> Maps { get; set; }
        public EditorStateData EditorState { get; set; }
    }

    public class MapDocumentData
    {
        public string Name { get; set; }
        public MapInfo Map { get; set; }
        public List<EntityData> Entities { get; set; } = new();
    }

    public class MapEditorStateData
    {
        public string MapName { get; set; }
        public float CameraX { get; set; }
        public float CameraY { get; set; }
        public int ZoomIndex { get; set; }
        public string ActiveLayer { get; set; }
        public List<string> CollapsedLayers { get; set; }
    }

    public class SpritesheetInfo
    {
        public string Path { get; set; }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public int Padding { get; set; }
    }

    public class GroupData
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public List<SpriteRefData> Sprites { get; set; } = new();
        public bool? IsSolid { get; set; }
        public bool? IsPlayer { get; set; }
        public string Layer { get; set; }
        // G1 gameplay properties
        public bool? IsPassable { get; set; }
        public bool? IsHazardous { get; set; }
        public float? MovementCost { get; set; }
        public string DamageType { get; set; }
        public int? DamagePerTick { get; set; }
        public string EntityType { get; set; }
        public Dictionary<string, string> DefaultProperties { get; set; }
    }

    public class SpriteRefData
    {
        public int Col { get; set; }
        public int Row { get; set; }
    }

    public class MapInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int EntityRenderOrder { get; set; }
        public List<LayerData> Layers { get; set; } = new();
    }

    public class LayerData
    {
        public string Name { get; set; }
        public bool Visible { get; set; } = true;
        public string[] Cells { get; set; }
    }

    public class EntityData
    {
        public string Id { get; set; }
        public string GroupName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public Dictionary<string, string> Properties { get; set; }
    }

    public class EditorStateData
    {
        public string ActiveLayer { get; set; }
        public float CameraX { get; set; }
        public float CameraY { get; set; }
        public int ZoomIndex { get; set; }
        public List<string> PanelOrder { get; set; }
        public List<string> CollapsedPanels { get; set; }
        public List<string> CollapsedLayers { get; set; }
        // V2 multimap
        public string ActiveMapName { get; set; }
        public List<MapEditorStateData> MapStates { get; set; }
    }

    // --- Save ---

    public static void Save(string projectPath, string sheetPath,
                            DojoUI.ISpriteSheet sheet, List<TileGroup> groups,
                            MapData map, EditorStateData editorState)
    {
        string projectDir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(projectPath));

        var data = new ProjectData
        {
            Spritesheet = new SpritesheetInfo
            {
                Path = System.IO.Path.GetRelativePath(projectDir, System.IO.Path.GetFullPath(sheetPath)),
                TileWidth = sheet.TileWidth,
                TileHeight = sheet.TileHeight,
                Padding = sheet.Padding,
            },
            EditorState = editorState,
        };

        // Groups
        SerializeGroups(data, groups);

        // Map
        data.Map = new MapInfo { Width = map.Width, Height = map.Height, EntityRenderOrder = map.EntityRenderOrder };
        foreach (var layer in map.Layers)
        {
            data.Map.Layers.Add(new LayerData
            {
                Name = layer.Name,
                Visible = layer.Visible,
                Cells = layer.Cells,
            });
        }

        // Entities
        data.Entities = new List<EntityData>();
        foreach (var entity in map.Entities)
        {
            var ed = new EntityData
            {
                Id = entity.Id,
                GroupName = entity.GroupName,
                X = entity.X,
                Y = entity.Y,
            };
            if (entity.Properties.Count > 0)
                ed.Properties = entity.Properties;
            data.Entities.Add(ed);
        }

        string json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(projectPath, json);
    }

    // --- Load ---

    public static ProjectData Load(string projectPath)
    {
        string json = File.ReadAllText(projectPath);
        var data = JsonSerializer.Deserialize<ProjectData>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize project file.");

        // Resolve spritesheet path relative to project file
        string projectDir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(projectPath));
        data.Spritesheet.Path = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(projectDir, data.Spritesheet.Path));

        return data;
    }

    // --- Restore helpers ---

    public static List<TileGroup> RestoreGroups(ProjectData data)
    {
        var groups = new List<TileGroup>();
        foreach (var gd in data.Groups)
        {
            var group = new TileGroup
            {
                Name = gd.Name,
                Type = Enum.TryParse<GroupType>(gd.Type, out var t) ? t : GroupType.Tile,
                IsSolid = gd.IsSolid ?? false,
                IsPlayer = gd.IsPlayer ?? false,
                LayerName = gd.Layer,
                IsPassable = gd.IsPassable ?? true,
                IsHazardous = gd.IsHazardous ?? false,
                MovementCost = gd.MovementCost ?? 1.0f,
                DamageType = gd.DamageType,
                DamagePerTick = gd.DamagePerTick ?? 0,
                EntityType = Enum.TryParse<Game.EntityType>(gd.EntityType, out var et) ? et : Game.EntityType.Interactable,
                DefaultProperties = gd.DefaultProperties != null
                    ? new Dictionary<string, string>(gd.DefaultProperties)
                    : new(),
            };
            foreach (var s in gd.Sprites)
                group.Sprites.Add(new SpriteRef { Col = s.Col, Row = s.Row });
            groups.Add(group);
        }
        return groups;
    }

    public static MapData RestoreMap(ProjectData data)
    {
        return RestoreMapFromInfo(data.Map, data.Entities ?? new());
    }

    public static MapData RestoreMapDocument(MapDocumentData doc)
    {
        return RestoreMapFromInfo(doc.Map, doc.Entities ?? new());
    }

    private static MapData RestoreMapFromInfo(MapInfo mapInfo, List<EntityData> entities)
    {
        var map = new MapData(mapInfo.Width, mapInfo.Height);
        map.EntityRenderOrder = mapInfo.EntityRenderOrder;
        map.Layers.Clear();

        foreach (var ld in mapInfo.Layers)
        {
            var layer = new MapLayer(ld.Name, mapInfo.Width, mapInfo.Height)
            {
                Visible = ld.Visible,
            };
            if (ld.Cells != null && ld.Cells.Length == layer.Cells.Length)
                Array.Copy(ld.Cells, layer.Cells, ld.Cells.Length);
            map.Layers.Add(layer);
        }

        foreach (var ed in entities)
        {
            map.Entities.Add(new Entity
            {
                Id = ed.Id,
                GroupName = ed.GroupName,
                X = ed.X,
                Y = ed.Y,
                Properties = ed.Properties ?? new(),
            });
        }

        return map;
    }

    // --- V2 Save (multimap) ---

    public static void Save(string projectPath, string sheetPath,
                            DojoUI.ISpriteSheet sheet, List<TileGroup> groups,
                            List<MapDocumentState> mapDocuments, EditorStateData editorState)
    {
        string projectDir = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(projectPath));

        var data = new ProjectData
        {
            Version = 2,
            Spritesheet = new SpritesheetInfo
            {
                Path = System.IO.Path.GetRelativePath(projectDir, System.IO.Path.GetFullPath(sheetPath)),
                TileWidth = sheet.TileWidth,
                TileHeight = sheet.TileHeight,
                Padding = sheet.Padding,
            },
            EditorState = editorState,
            // V1 fields null (omitted by WhenWritingNull)
            Map = null,
            Entities = null,
        };

        // Groups (shared)
        SerializeGroups(data, groups);

        // Maps (V2)
        data.Maps = new List<MapDocumentData>();
        foreach (var doc in mapDocuments)
        {
            var mapDoc = new MapDocumentData { Name = doc.Name };
            mapDoc.Map = new MapInfo
            {
                Width = doc.Map.Width,
                Height = doc.Map.Height,
                EntityRenderOrder = doc.Map.EntityRenderOrder,
            };
            foreach (var layer in doc.Map.Layers)
            {
                mapDoc.Map.Layers.Add(new LayerData
                {
                    Name = layer.Name,
                    Visible = layer.Visible,
                    Cells = layer.Cells,
                });
            }
            foreach (var entity in doc.Map.Entities)
            {
                var ed = new EntityData
                {
                    Id = entity.Id,
                    GroupName = entity.GroupName,
                    X = entity.X,
                    Y = entity.Y,
                };
                if (entity.Properties.Count > 0)
                    ed.Properties = entity.Properties;
                mapDoc.Entities.Add(ed);
            }
            data.Maps.Add(mapDoc);
        }

        string json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(projectPath, json);
    }

    // --- Shared group serialization ---

    private static void SerializeGroups(ProjectData data, List<TileGroup> groups)
    {
        foreach (var group in groups)
        {
            var gd = new GroupData
            {
                Name = group.Name,
                Type = group.Type.ToString(),
            };
            if (group.IsSolid) gd.IsSolid = true;
            if (group.IsPlayer) gd.IsPlayer = true;
            gd.Layer = group.LayerName;
            if (!group.IsPassable) gd.IsPassable = false;
            if (group.IsHazardous) gd.IsHazardous = true;
            if (group.MovementCost != 1.0f) gd.MovementCost = group.MovementCost;
            gd.DamageType = group.DamageType;
            if (group.DamagePerTick > 0) gd.DamagePerTick = group.DamagePerTick;
            if (group.Type == GroupType.Entity && group.EntityType != Game.EntityType.Interactable)
                gd.EntityType = group.EntityType.ToString();
            if (group.DefaultProperties.Count > 0)
                gd.DefaultProperties = group.DefaultProperties;
            foreach (var sprite in group.Sprites)
                gd.Sprites.Add(new SpriteRefData { Col = sprite.Col, Row = sprite.Row });
            data.Groups.Add(gd);
        }
    }
}
