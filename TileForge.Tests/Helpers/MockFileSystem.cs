using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TileForge.Infrastructure;

namespace TileForge.Tests.Helpers;

/// <summary>
/// In-memory IFileSystem implementation for unit testing.
/// Stores files as string key-value pairs. Directories are tracked separately.
/// Uses case-insensitive path comparison for Windows compatibility.
/// </summary>
public class MockFileSystem : IFileSystem
{
    private readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _writeTimes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Pre-populate a file in the mock filesystem.
    /// </summary>
    public void AddFile(string path, string content)
    {
        var normalized = NormalizePath(path);
        _files[normalized] = content;
        _writeTimes[normalized] = DateTime.UtcNow;

        // Auto-create parent directory
        var dir = Path.GetDirectoryName(normalized);
        if (!string.IsNullOrEmpty(dir))
            _directories.Add(dir);
    }

    /// <summary>
    /// Pre-create a directory in the mock filesystem.
    /// </summary>
    public void AddDirectory(string path)
    {
        _directories.Add(NormalizePath(path));
    }

    public string ReadAllText(string path)
    {
        var normalized = NormalizePath(path);
        if (!_files.TryGetValue(normalized, out var content))
            throw new FileNotFoundException($"File not found: {path}", path);
        return content;
    }

    public void WriteAllText(string path, string content)
    {
        var normalized = NormalizePath(path);
        _files[normalized] = content;
        _writeTimes[normalized] = DateTime.UtcNow;

        var dir = Path.GetDirectoryName(normalized);
        if (!string.IsNullOrEmpty(dir))
            _directories.Add(dir);
    }

    public void AppendAllText(string path, string text)
    {
        var normalized = NormalizePath(path);
        if (_files.TryGetValue(normalized, out var existing))
            _files[normalized] = existing + text;
        else
            _files[normalized] = text;
        _writeTimes[normalized] = DateTime.UtcNow;
    }

    public bool Exists(string path)
    {
        return _files.ContainsKey(NormalizePath(path));
    }

    public void Delete(string path)
    {
        var normalized = NormalizePath(path);
        _files.Remove(normalized);
        _writeTimes.Remove(normalized);
    }

    public DateTime GetLastWriteTimeUtc(string path)
    {
        var normalized = NormalizePath(path);
        if (_writeTimes.TryGetValue(normalized, out var time))
            return time;
        // Match System.IO behavior: returns a default date for nonexistent files
        return new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    public bool DirectoryExists(string path)
    {
        return _directories.Contains(NormalizePath(path));
    }

    public void CreateDirectory(string path)
    {
        _directories.Add(NormalizePath(path));
    }

    public string[] GetFiles(string directory, string searchPattern)
    {
        var normalizedDir = NormalizePath(directory);
        // Simple wildcard matching: *.json -> endsWith .json
        string extension = null;
        if (searchPattern.StartsWith("*"))
            extension = searchPattern.Substring(1);

        return _files.Keys
            .Where(f =>
            {
                var dir = Path.GetDirectoryName(f);
                if (!string.Equals(dir, normalizedDir, StringComparison.OrdinalIgnoreCase))
                    return false;
                if (extension != null)
                    return f.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
                return true;
            })
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public string GetDirectoryName(string path)
    {
        return Path.GetDirectoryName(path);
    }

    /// <summary>
    /// Returns all files currently in the mock filesystem.
    /// </summary>
    public IReadOnlyDictionary<string, string> Files => _files;

    private static string NormalizePath(string path)
    {
        // Normalize to consistent separators
        return path.Replace('/', Path.DirectorySeparatorChar)
                   .Replace('\\', Path.DirectorySeparatorChar)
                   .TrimEnd(Path.DirectorySeparatorChar);
    }
}
