using System.Collections.Generic;
using System.Linq;

namespace DojoUI;

public class TileAtlas
{
    private readonly Dictionary<string, TileEntry> _byName = new();
    private readonly Dictionary<(int col, int row), TileEntry> _byPosition = new();

    public IEnumerable<TileEntry> AllEntries => _byPosition.Values;

    public static TileAtlas Generate(SpriteSheet sheet)
    {
        var atlas = new TileAtlas();

        for (int row = 0; row < sheet.Rows; row++)
        {
            for (int col = 0; col < sheet.Cols; col++)
            {
                var rect = sheet.GetTileRect(col, row);
                string name = $"r{row:D2}_c{col:D3}";

                var entry = new TileEntry
                {
                    Name = name,
                    Section = null,
                    Col = col,
                    Row = row,
                    X = rect.X,
                    Y = rect.Y,
                    W = rect.Width,
                    H = rect.Height,
                };

                atlas._byName[name] = entry;
                atlas._byPosition[(col, row)] = entry;
            }
        }

        return atlas;
    }

    public TileEntry GetByPosition(int col, int row)
    {
        _byPosition.TryGetValue((col, row), out var entry);
        return entry;
    }

    public TileEntry GetByName(string name)
    {
        _byName.TryGetValue(name, out var entry);
        return entry;
    }

    public static string AutoName(int col, int row) => $"r{row:D2}_c{col:D3}";

    public static bool IsAutoName(TileEntry entry) => entry.Name == AutoName(entry.Col, entry.Row);

    public bool Rename(int col, int row, string newName)
    {
        if (!_byPosition.TryGetValue((col, row), out var entry))
            return false;

        if (_byName.ContainsKey(newName) && _byName[newName] != entry)
            return false; // name already taken by a different tile

        _byName.Remove(entry.Name);
        entry.Name = newName;
        _byName[newName] = entry;
        return true;
    }
}
