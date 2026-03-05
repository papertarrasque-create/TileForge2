using System;

namespace TileForge.Infrastructure;

/// <summary>
/// Abstraction over System.IO file and directory operations.
/// Enables unit testing without real filesystem access.
/// </summary>
public interface IFileSystem
{
    string ReadAllText(string path);
    void WriteAllText(string path, string content);
    void AppendAllText(string path, string text);
    bool Exists(string path);
    void Delete(string path);
    DateTime GetLastWriteTimeUtc(string path);
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    string[] GetFiles(string directory, string searchPattern);
    string GetDirectoryName(string path);
}
