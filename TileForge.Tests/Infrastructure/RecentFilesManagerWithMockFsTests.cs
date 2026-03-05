using System.IO;
using Xunit;
using TileForge.Infrastructure;
using TileForge.Tests.Helpers;

namespace TileForge.Tests.Infrastructure;

public class MockPathResolver : IPathResolver
{
    public string SettingsDirectory { get; set; } = "/mock/.tileforge";
    public string SavesDirectory { get; set; } = "/mock/.tileforge/saves";
    public string KeybindingsPath { get; set; } = "/mock/.tileforge/keybindings.json";
    public string RecentFilesPath { get; set; } = "/mock/.tileforge/recent.json";
}

public class RecentFilesManagerWithMockFsTests
{
    private readonly MockFileSystem _fs;
    private readonly MockPathResolver _paths;

    public RecentFilesManagerWithMockFsTests()
    {
        _fs = new MockFileSystem();
        _paths = new MockPathResolver();
    }

    [Fact]
    public void Constructor_EmptyFileSystem_HasNoRecentFiles()
    {
        var manager = new RecentFilesManager(_fs, _paths);
        Assert.Empty(manager.RecentFiles);
    }

    [Fact]
    public void AddRecent_PersistsToFileSystem()
    {
        var manager = new RecentFilesManager(_fs, _paths);

        manager.AddRecent(Path.Combine("/projects", "test.tileforge"));

        Assert.True(_fs.Exists(_paths.RecentFilesPath));
    }

    [Fact]
    public void AddRecent_LoadsOnNextConstruction()
    {
        var manager = new RecentFilesManager(_fs, _paths);
        manager.AddRecent(Path.Combine("/projects", "test.tileforge"));

        var manager2 = new RecentFilesManager(_fs, _paths);
        Assert.Single(manager2.RecentFiles);
    }

    [Fact]
    public void PruneNonExistent_RemovesMissingFiles()
    {
        var manager = new RecentFilesManager(_fs, _paths);
        // Manually add paths that don't exist in the mock filesystem
        manager.RecentFiles.Add("/projects/missing.tileforge");
        manager.RecentFiles.Add("/projects/also_missing.tileforge");

        manager.PruneNonExistent();

        Assert.Empty(manager.RecentFiles);
    }

    [Fact]
    public void PruneNonExistent_KeepsExistingFiles()
    {
        string existingPath = Path.GetFullPath(Path.Combine("/projects", "exists.tileforge"));
        _fs.AddFile(existingPath, "data");

        var manager = new RecentFilesManager(_fs, _paths);
        manager.RecentFiles.Add(existingPath);

        manager.PruneNonExistent();

        Assert.Single(manager.RecentFiles);
    }

    [Fact]
    public void Constructor_CorruptSettingsFile_StartsEmpty()
    {
        _fs.AddFile(_paths.RecentFilesPath, "not valid json {{{");

        var manager = new RecentFilesManager(_fs, _paths);

        Assert.Empty(manager.RecentFiles);
    }
}
