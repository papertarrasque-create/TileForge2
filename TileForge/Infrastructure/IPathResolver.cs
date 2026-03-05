namespace TileForge.Infrastructure;

/// <summary>
/// Resolves well-known paths for TileForge settings and data.
/// Centralizes the ~/.tileforge directory structure.
/// </summary>
public interface IPathResolver
{
    string SettingsDirectory { get; }
    string SavesDirectory { get; }
    string KeybindingsPath { get; }
    string RecentFilesPath { get; }
}
