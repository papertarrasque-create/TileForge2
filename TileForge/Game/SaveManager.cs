using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TileForge.Game;

/// <summary>
/// Slot-based save/load system for GameState.
/// Saves to ~/.tileforge/saves/{slotName}.json by default.
/// </summary>
public class SaveManager
{
    private readonly string _savesDirectory;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public SaveManager(string savesDirectory = null)
    {
        _savesDirectory = savesDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".tileforge", "saves");
    }

    public void Save(GameState state, string slotName)
    {
        Directory.CreateDirectory(_savesDirectory);
        var path = GetSlotPath(slotName);
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(path, json);
    }

    public GameState Load(string slotName)
    {
        var path = GetSlotPath(slotName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Save slot '{slotName}' not found.", path);
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<GameState>(json);
    }

    public bool SlotExists(string slotName)
    {
        return File.Exists(GetSlotPath(slotName));
    }

    public List<string> GetSlots()
    {
        if (!Directory.Exists(_savesDirectory))
            return new List<string>();

        return Directory.GetFiles(_savesDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(n => n)
            .ToList();
    }

    public void Delete(string slotName)
    {
        var path = GetSlotPath(slotName);
        if (File.Exists(path))
            File.Delete(path);
    }

    private string GetSlotPath(string slotName)
    {
        return Path.Combine(_savesDirectory, slotName + ".json");
    }
}
