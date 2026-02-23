using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        public MapInfo Map { get; set; }
        public List<EntityData> Entities { get; set; } = new();
        public EditorStateData EditorState { get; set; }
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
            foreach (var sprite in group.Sprites)
                gd.Sprites.Add(new SpriteRefData { Col = sprite.Col, Row = sprite.Row });
            data.Groups.Add(gd);
        }

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
            };
            foreach (var s in gd.Sprites)
                group.Sprites.Add(new SpriteRef { Col = s.Col, Row = s.Row });
            groups.Add(group);
        }
        return groups;
    }

    public static MapData RestoreMap(ProjectData data)
    {
        var map = new MapData(data.Map.Width, data.Map.Height);
        map.EntityRenderOrder = data.Map.EntityRenderOrder;
        map.Layers.Clear();

        foreach (var ld in data.Map.Layers)
        {
            var layer = new MapLayer(ld.Name, data.Map.Width, data.Map.Height)
            {
                Visible = ld.Visible,
            };
            if (ld.Cells != null && ld.Cells.Length == layer.Cells.Length)
                Array.Copy(ld.Cells, layer.Cells, ld.Cells.Length);
            map.Layers.Add(layer);
        }

        foreach (var ed in data.Entities)
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
}
