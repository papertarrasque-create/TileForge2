using System;
using System.IO;
using TileForge.Editor;

namespace TileForge;

/// <summary>
/// Manages periodic auto-save to a sidecar .autosave file.
/// Call Update() each frame. When the project is dirty and enough time has passed,
/// it saves to {projectPath}.autosave. On load, call CheckForRecovery() to detect
/// and optionally recover from an autosave.
/// </summary>
public class AutoSaveManager
{
    private readonly EditorState _state;
    private readonly Func<string> _getProjectPath;
    private readonly Action<string> _doSave;

    private double _timeSinceLastSave;
    private bool _wasDirtyAtLastCheck;

    /// <summary>Auto-save interval in seconds. Default: 120 (2 minutes).</summary>
    public double IntervalSeconds { get; set; } = 120;

    /// <summary>Whether auto-save is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>The last time an auto-save was performed, or null if never.</summary>
    public DateTime? LastAutoSave { get; private set; }

    public AutoSaveManager(EditorState state, Func<string> getProjectPath, Action<string> doSave)
    {
        _state = state;
        _getProjectPath = getProjectPath;
        _doSave = doSave;
    }

    /// <summary>
    /// Call each frame with elapsed seconds. Performs auto-save when conditions are met.
    /// </summary>
    public void Update(double elapsedSeconds)
    {
        if (!Enabled) return;

        string projectPath = _getProjectPath();
        if (projectPath == null) return;
        if (!_state.IsDirty) { _timeSinceLastSave = 0; return; }

        _timeSinceLastSave += elapsedSeconds;
        if (_timeSinceLastSave >= IntervalSeconds)
        {
            PerformAutoSave(projectPath);
            _timeSinceLastSave = 0;
        }
    }

    /// <summary>
    /// Gets the autosave sidecar path for a given project path.
    /// </summary>
    public static string GetAutoSavePath(string projectPath)
    {
        return projectPath + ".autosave";
    }

    /// <summary>
    /// Checks if an autosave file exists that is newer than the project file.
    /// Returns the autosave path if recovery is available, null otherwise.
    /// </summary>
    public static string CheckForRecovery(string projectPath)
    {
        string autoSavePath = GetAutoSavePath(projectPath);
        if (!File.Exists(autoSavePath)) return null;

        try
        {
            var projectTime = File.GetLastWriteTimeUtc(projectPath);
            var autoSaveTime = File.GetLastWriteTimeUtc(autoSavePath);
            return autoSaveTime > projectTime ? autoSavePath : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Removes the autosave sidecar file if it exists.
    /// Call after a successful manual save.
    /// </summary>
    public static void CleanupAutoSave(string projectPath)
    {
        string autoSavePath = GetAutoSavePath(projectPath);
        try
        {
            if (File.Exists(autoSavePath))
                File.Delete(autoSavePath);
        }
        catch
        {
            // Silently ignore cleanup failures
        }
    }

    private void PerformAutoSave(string projectPath)
    {
        string autoSavePath = GetAutoSavePath(projectPath);
        try
        {
            _doSave(autoSavePath);
            LastAutoSave = DateTime.UtcNow;
        }
        catch
        {
            // Auto-save failure should never disrupt the editor
        }
    }
}
