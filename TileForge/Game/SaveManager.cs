using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TileForge.Infrastructure;

namespace TileForge.Game;

/// <summary>
/// Slot-based save/load system for GameState.
/// Saves to ~/.tileforge/saves/{slotName}.json by default.
/// </summary>
public class SaveManager
{
    private readonly string _savesDirectory;
    private readonly IFileSystem _fileSystem;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public SaveManager(string savesDirectory = null, IFileSystem fileSystem = null,
        IPathResolver pathResolver = null)
    {
        _savesDirectory = savesDirectory
            ?? pathResolver?.SavesDirectory
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".tileforge", "saves");
        _fileSystem = fileSystem ?? new DefaultFileSystem();
    }

    public void Save(GameState state, string slotName)
    {
        _fileSystem.CreateDirectory(_savesDirectory);
        var path = GetSlotPath(slotName);
        var json = JsonSerializer.Serialize(state, JsonOptions);
        _fileSystem.WriteAllText(path, json);
    }

    public GameState Load(string slotName)
    {
        var path = GetSlotPath(slotName);
        if (!_fileSystem.Exists(path))
            throw new FileNotFoundException($"Save slot '{slotName}' not found.", path);
        var json = _fileSystem.ReadAllText(path);
        return JsonSerializer.Deserialize<GameState>(json);
    }

    public bool SlotExists(string slotName)
    {
        return _fileSystem.Exists(GetSlotPath(slotName));
    }

    public List<string> GetSlots()
    {
        if (!_fileSystem.DirectoryExists(_savesDirectory))
            return new List<string>();

        return _fileSystem.GetFiles(_savesDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(n => n)
            .ToList();
    }

    public void Delete(string slotName)
    {
        var path = GetSlotPath(slotName);
        if (_fileSystem.Exists(path))
            _fileSystem.Delete(path);
    }

    private string GetSlotPath(string slotName)
    {
        return Path.Combine(_savesDirectory, slotName + ".json");
    }
}
