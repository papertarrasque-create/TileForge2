using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TileForge;

/// <summary>
/// Tracks recently opened project files. Persists to a JSON settings file.
/// </summary>
public class RecentFilesManager
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".tileforge");
    private static readonly string SettingsPath = Path.Combine(SettingsDir, "recent.json");

    private const int MaxRecentFiles = 10;

    public List<string> RecentFiles { get; private set; } = new();

    public RecentFilesManager()
    {
        Load();
    }

    /// <summary>
    /// Adds a project path to the top of the recent files list.
    /// Removes duplicates and trims to MaxRecentFiles.
    /// </summary>
    public void AddRecent(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath)) return;

        string fullPath = Path.GetFullPath(projectPath);

        // Remove if already present (will re-add at top)
        RecentFiles.RemoveAll(p => string.Equals(p, fullPath, StringComparison.OrdinalIgnoreCase));

        RecentFiles.Insert(0, fullPath);

        if (RecentFiles.Count > MaxRecentFiles)
            RecentFiles.RemoveRange(MaxRecentFiles, RecentFiles.Count - MaxRecentFiles);

        Save();
    }

    /// <summary>
    /// Removes entries that no longer exist on disk.
    /// </summary>
    public void PruneNonExistent()
    {
        int removed = RecentFiles.RemoveAll(p => !File.Exists(p));
        if (removed > 0) Save();
    }

    private void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                RecentFiles = JsonSerializer.Deserialize<List<string>>(json) ?? new();
            }
        }
        catch
        {
            RecentFiles = new();
        }
    }

    private void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            string json = JsonSerializer.Serialize(RecentFiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently ignore save failures for settings
        }
    }
}
