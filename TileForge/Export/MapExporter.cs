using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using TileForge.Data;

namespace TileForge.Export;

public static class MapExporter
{
    private static readonly JsonSerializerOptions ExportJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string ExportJson(MapData map, List<TileGroup> groups)
    {
        var export = new ExportData
        {
            Width = map.Width,
            Height = map.Height,
            Layers = map.Layers.Select(l => new ExportLayer
            {
                Name = l.Name,
                Cells = l.Cells,
            }).ToList(),
            Groups = groups.Select(g => new ExportGroup
            {
                Name = g.Name,
                Type = g.Type.ToString(),
                IsSolid = g.IsSolid ? true : null,
                Sprites = g.Sprites.Select(s => new ExportSpriteRef { Col = s.Col, Row = s.Row }).ToList(),
            }).ToList(),
            Entities = map.Entities.Select(e => new ExportEntity
            {
                Id = e.Id,
                GroupName = e.GroupName,
                X = e.X,
                Y = e.Y,
                Properties = e.Properties.Count > 0 ? e.Properties : null,
            }).ToList(),
        };

        return JsonSerializer.Serialize(export, ExportJsonOptions);
    }
}

public class ExportData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public List<ExportLayer> Layers { get; set; }
    public List<ExportGroup> Groups { get; set; }
    public List<ExportEntity> Entities { get; set; }
}

public class ExportLayer
{
    public string Name { get; set; }
    public string[] Cells { get; set; }
}

public class ExportGroup
{
    public string Name { get; set; }
    public string Type { get; set; }
    public bool? IsSolid { get; set; }
    public List<ExportSpriteRef> Sprites { get; set; }
}

public class ExportSpriteRef
{
    public int Col { get; set; }
    public int Row { get; set; }
}

public class ExportEntity
{
    public string Id { get; set; }
    public string GroupName { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public Dictionary<string, string> Properties { get; set; }
}
