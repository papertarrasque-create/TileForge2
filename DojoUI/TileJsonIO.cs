using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace DojoUI;

public static class TileJsonIO
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static void Export(string path, SpriteSheet sheet, TileAtlas atlas)
    {
        var document = new TileGridDocument
        {
            File = sheet.FileName,
            Width = sheet.Texture.Width,
            Height = sheet.Texture.Height,
            Background = null,
            Grid = new GridInfo
            {
                TileWidth = sheet.TileWidth,
                TileHeight = sheet.TileHeight,
                Padding = sheet.Padding,
                Cols = sheet.Cols,
                Rows = sheet.Rows,
            },
            Tiles = new Dictionary<string, TileInfo>(),
        };

        foreach (var entry in atlas.AllEntries)
        {
            document.Tiles[entry.Name] = new TileInfo
            {
                Name = entry.Name,
                Section = entry.Section,
                Col = entry.Col,
                Row = entry.Row,
                X = entry.X,
                Y = entry.Y,
                W = entry.W,
                H = entry.H,
            };
        }

        string json = JsonSerializer.Serialize(document, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static void ExportSelection(string path, SpriteSheet sheet, TileAtlas atlas, Rectangle range)
    {
        var document = new TileGridDocument
        {
            File = sheet.FileName,
            Width = sheet.Texture.Width,
            Height = sheet.Texture.Height,
            Background = null,
            Grid = new GridInfo
            {
                TileWidth = sheet.TileWidth,
                TileHeight = sheet.TileHeight,
                Padding = sheet.Padding,
                Cols = sheet.Cols,
                Rows = sheet.Rows,
            },
            Tiles = new Dictionary<string, TileInfo>(),
        };

        for (int r = range.Y; r < range.Y + range.Height; r++)
        {
            for (int c = range.X; c < range.X + range.Width; c++)
            {
                if (!sheet.InBounds(c, r)) continue;
                var entry = atlas.GetByPosition(c, r);
                if (entry == null) continue;

                document.Tiles[entry.Name] = new TileInfo
                {
                    Name = entry.Name,
                    Section = entry.Section,
                    Col = entry.Col,
                    Row = entry.Row,
                    X = entry.X,
                    Y = entry.Y,
                    W = entry.W,
                    H = entry.H,
                };
            }
        }

        string json = JsonSerializer.Serialize(document, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static int Import(string path, TileAtlas atlas)
    {
        string json = File.ReadAllText(path);
        var document = JsonSerializer.Deserialize<TileGridDocument>(json, JsonOptions);
        if (document?.Tiles == null) return 0;

        int applied = 0;
        foreach (var kvp in document.Tiles)
        {
            var tile = kvp.Value;
            var existing = atlas.GetByPosition(tile.Col, tile.Row);
            if (existing == null) continue;

            // Only rename if the JSON name differs from what's already there
            if (existing.Name != tile.Name && atlas.Rename(tile.Col, tile.Row, tile.Name))
                applied++;
        }

        return applied;
    }

    public static string DefaultExportPath(string sheetPath)
    {
        string dir = Path.GetDirectoryName(sheetPath) ?? ".";
        string name = Path.GetFileNameWithoutExtension(sheetPath);
        return Path.Combine(dir, $"{name}.tilegrid.json");
    }

    public static string ExpandPath(string path)
    {
        if (path.StartsWith('~'))
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path[1..].TrimStart('/'));

        if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            path += ".tilegrid.json";

        return Path.GetFullPath(path);
    }

    // --- JSON model classes ---

    private class TileGridDocument
    {
        public string File { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public object Background { get; set; }
        public GridInfo Grid { get; set; }
        public Dictionary<string, TileInfo> Tiles { get; set; }
    }

    private class GridInfo
    {
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public int Padding { get; set; }
        public int Cols { get; set; }
        public int Rows { get; set; }
    }

    private class TileInfo
    {
        public string Name { get; set; }
        public int? Section { get; set; }
        public int Col { get; set; }
        public int Row { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
    }
}
