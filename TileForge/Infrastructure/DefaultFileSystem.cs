using System;
using System.IO;

namespace TileForge.Infrastructure;

/// <summary>
/// Default IFileSystem implementation that delegates to System.IO.
/// </summary>
public class DefaultFileSystem : IFileSystem
{
    public string ReadAllText(string path) => File.ReadAllText(path);

    public void WriteAllText(string path, string content) => File.WriteAllText(path, content);

    public void AppendAllText(string path, string text) => File.AppendAllText(path, text);

    public bool Exists(string path) => File.Exists(path);

    public void Delete(string path) => File.Delete(path);

    public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public string[] GetFiles(string directory, string searchPattern)
        => Directory.GetFiles(directory, searchPattern);

    public string GetDirectoryName(string path) => Path.GetDirectoryName(path);
}
