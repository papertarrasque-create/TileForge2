using System;
using System.IO;
using Xunit;

namespace TileForge.Tests;

/// <summary>
/// Tests for RecentFilesManager logic.
/// Uses temp directories to avoid polluting real settings.
/// Note: RecentFilesManager uses static paths so we test the Add/Prune logic
/// by constructing instances and manipulating the RecentFiles list directly
/// where the static Load/Save would interfere.
/// </summary>
public class RecentFilesManagerTests
{
    [Fact]
    public void AddRecent_AddsPathToTop()
    {
        var mgr = new RecentFilesManager();
        // Clear any loaded state
        mgr.RecentFiles.Clear();

        string tempPath = Path.Combine(Path.GetTempPath(), "test_project_a.tileforge");
        mgr.AddRecent(tempPath);

        Assert.Single(mgr.RecentFiles);
        Assert.Equal(Path.GetFullPath(tempPath), mgr.RecentFiles[0]);
    }

    [Fact]
    public void AddRecent_DeduplicatesExistingEntries()
    {
        var mgr = new RecentFilesManager();
        mgr.RecentFiles.Clear();

        string path = Path.Combine(Path.GetTempPath(), "test_dedup.tileforge");
        mgr.AddRecent(path);
        mgr.AddRecent(path); // duplicate

        Assert.Single(mgr.RecentFiles);
    }

    [Fact]
    public void AddRecent_MovesExistingEntryToTop()
    {
        var mgr = new RecentFilesManager();
        mgr.RecentFiles.Clear();

        string pathA = Path.Combine(Path.GetTempPath(), "test_a.tileforge");
        string pathB = Path.Combine(Path.GetTempPath(), "test_b.tileforge");
        mgr.AddRecent(pathA);
        mgr.AddRecent(pathB);

        Assert.Equal(Path.GetFullPath(pathB), mgr.RecentFiles[0]);

        // Re-add A, should move to top
        mgr.AddRecent(pathA);
        Assert.Equal(Path.GetFullPath(pathA), mgr.RecentFiles[0]);
        Assert.Equal(2, mgr.RecentFiles.Count);
    }

    [Fact]
    public void AddRecent_TrimsToMaxEntries()
    {
        var mgr = new RecentFilesManager();
        mgr.RecentFiles.Clear();

        // Add 12 entries (max is 10)
        for (int i = 0; i < 12; i++)
        {
            string path = Path.Combine(Path.GetTempPath(), $"test_trim_{i}.tileforge");
            mgr.AddRecent(path);
        }

        Assert.Equal(10, mgr.RecentFiles.Count);
    }

    [Fact]
    public void AddRecent_NullOrWhiteSpace_IsIgnored()
    {
        var mgr = new RecentFilesManager();
        mgr.RecentFiles.Clear();

        mgr.AddRecent(null);
        mgr.AddRecent("");
        mgr.AddRecent("   ");

        Assert.Empty(mgr.RecentFiles);
    }

    [Fact]
    public void PruneNonExistent_RemovesDeadPaths()
    {
        var mgr = new RecentFilesManager();
        mgr.RecentFiles.Clear();

        string existingFile = Path.GetTempFileName();
        string deadFile = Path.Combine(Path.GetTempPath(), "nonexistent_prune_test.tileforge");

        try
        {
            mgr.RecentFiles.Add(existingFile);
            mgr.RecentFiles.Add(deadFile);

            mgr.PruneNonExistent();

            Assert.Single(mgr.RecentFiles);
            Assert.Equal(existingFile, mgr.RecentFiles[0]);
        }
        finally
        {
            if (File.Exists(existingFile)) File.Delete(existingFile);
        }
    }

    [Fact]
    public void PruneNonExistent_AllExist_RemovesNothing()
    {
        var mgr = new RecentFilesManager();
        mgr.RecentFiles.Clear();

        string tempA = Path.GetTempFileName();
        string tempB = Path.GetTempFileName();

        try
        {
            mgr.RecentFiles.Add(tempA);
            mgr.RecentFiles.Add(tempB);

            mgr.PruneNonExistent();

            Assert.Equal(2, mgr.RecentFiles.Count);
        }
        finally
        {
            File.Delete(tempA);
            File.Delete(tempB);
        }
    }

    [Fact]
    public void AddRecent_NormalizesPath()
    {
        var mgr = new RecentFilesManager();
        mgr.RecentFiles.Clear();

        string path = Path.Combine(Path.GetTempPath(), "subdir", "..", "test_normalize.tileforge");
        mgr.AddRecent(path);

        string expected = Path.GetFullPath(path);
        Assert.Equal(expected, mgr.RecentFiles[0]);
    }
}
