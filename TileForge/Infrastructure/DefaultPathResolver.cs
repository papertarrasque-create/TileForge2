using System;
using System.IO;

namespace TileForge.Infrastructure;

/// <summary>
/// Default IPathResolver using ~/.tileforge directory structure.
/// </summary>
public class DefaultPathResolver : IPathResolver
{
    public string SettingsDirectory { get; }
    public string SavesDirectory { get; }
    public string KeybindingsPath { get; }
    public string RecentFilesPath { get; }

    public DefaultPathResolver()
    {
        SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".tileforge");
        SavesDirectory = Path.Combine(SettingsDirectory, "saves");
        KeybindingsPath = Path.Combine(SettingsDirectory, "keybindings.json");
        RecentFilesPath = Path.Combine(SettingsDirectory, "recent.json");
    }
}
