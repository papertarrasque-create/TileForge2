using System;
using System.IO;

namespace TileForge;

/// <summary>
/// Minimal file logger for crash diagnosis. Writes to ~/.tileforge/debug.log.
/// </summary>
public static class DebugLog
{
    private static readonly string LogPath;
    private static readonly object Lock = new();

    static DebugLog()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".tileforge");
        Directory.CreateDirectory(dir);
        LogPath = Path.Combine(dir, "debug.log");

        // Start fresh each launch so the file only contains the latest run
        File.WriteAllText(LogPath, $"=== TileForge launch {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");
    }

    public static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
        lock (Lock)
        {
            File.AppendAllText(LogPath, line);
        }
    }

    public static void Error(string message, Exception ex)
    {
        Log($"ERROR: {message}");
        Log($"  Exception: {ex.GetType().Name}: {ex.Message}");
        Log($"  Stack trace:\n{ex.StackTrace}");
        if (ex.InnerException != null)
        {
            Log($"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            Log($"  Inner stack:\n{ex.InnerException.StackTrace}");
        }
    }
}
