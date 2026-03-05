using System;
using System.IO;
using TileForge;
using TileForge.Editor;
using Xunit;

namespace TileForge.Tests;

public class AutoSaveManagerTests
{
    private static EditorState CreateState()
    {
        return new EditorState();
    }

    [Fact]
    public void GetAutoSavePath_AppendsDotAutosave()
    {
        string result = AutoSaveManager.GetAutoSavePath("/tmp/project.tileforge");
        Assert.Equal("/tmp/project.tileforge.autosave", result);
    }

    [Fact]
    public void Update_NotDirty_DoesNotSave()
    {
        var state = CreateState();
        bool saved = false;
        var mgr = new AutoSaveManager(state, () => "/tmp/test.tileforge", _ => saved = true);
        mgr.IntervalSeconds = 1;

        mgr.Update(5); // 5 seconds, but not dirty

        Assert.False(saved);
    }

    [Fact]
    public void Update_DirtyButBelowInterval_DoesNotSave()
    {
        var state = CreateState();
        state.MarkDirty();
        bool saved = false;
        var mgr = new AutoSaveManager(state, () => "/tmp/test.tileforge", _ => saved = true);
        mgr.IntervalSeconds = 10;

        mgr.Update(5); // 5s < 10s interval

        Assert.False(saved);
    }

    [Fact]
    public void Update_DirtyAndPastInterval_SavesAutoSave()
    {
        var state = CreateState();
        state.MarkDirty();
        string savedPath = null;
        var mgr = new AutoSaveManager(state, () => "/tmp/test.tileforge", p => savedPath = p);
        mgr.IntervalSeconds = 10;
        mgr.Enabled = true;

        mgr.Update(11); // 11s > 10s interval

        Assert.Equal("/tmp/test.tileforge.autosave", savedPath);
    }

    [Fact]
    public void Update_NullProjectPath_DoesNotSave()
    {
        var state = CreateState();
        state.MarkDirty();
        bool saved = false;
        var mgr = new AutoSaveManager(state, () => null, _ => saved = true);
        mgr.IntervalSeconds = 1;

        mgr.Update(5);

        Assert.False(saved);
    }

    [Fact]
    public void Update_Disabled_DoesNotSave()
    {
        var state = CreateState();
        state.MarkDirty();
        bool saved = false;
        var mgr = new AutoSaveManager(state, () => "/tmp/test.tileforge", _ => saved = true);
        mgr.IntervalSeconds = 1;
        mgr.Enabled = false;

        mgr.Update(5);

        Assert.False(saved);
    }

    [Fact]
    public void Update_AfterSave_ResetsTimer()
    {
        var state = CreateState();
        state.MarkDirty();
        int saveCount = 0;
        var mgr = new AutoSaveManager(state, () => "/tmp/test.tileforge", _ => saveCount++);
        mgr.IntervalSeconds = 10;
        mgr.Enabled = true;

        mgr.Update(11); // Triggers save
        Assert.Equal(1, saveCount);

        mgr.Update(5); // Only 5s since last save, should not trigger
        Assert.Equal(1, saveCount);
    }

    [Fact]
    public void Update_AccumulatesTime()
    {
        var state = CreateState();
        state.MarkDirty();
        int saveCount = 0;
        var mgr = new AutoSaveManager(state, () => "/tmp/test.tileforge", _ => saveCount++);
        mgr.IntervalSeconds = 10;
        mgr.Enabled = true;

        mgr.Update(3);
        mgr.Update(3);
        mgr.Update(3);
        Assert.Equal(0, saveCount);

        mgr.Update(2); // Total: 11s
        Assert.Equal(1, saveCount);
    }

    [Fact]
    public void LastAutoSave_SetAfterSave()
    {
        var state = CreateState();
        state.MarkDirty();
        var mgr = new AutoSaveManager(state, () => "/tmp/test.tileforge", _ => { });
        mgr.IntervalSeconds = 1;
        mgr.Enabled = true;

        Assert.Null(mgr.LastAutoSave);

        mgr.Update(2);

        Assert.NotNull(mgr.LastAutoSave);
    }

    [Fact]
    public void CheckForRecovery_NoAutoSaveFile_ReturnsNull()
    {
        string tempFile = Path.GetTempFileName();
        try
        {
            string result = AutoSaveManager.CheckForRecovery(tempFile);
            Assert.Null(result);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void CleanupAutoSave_RemovesSidecarFile()
    {
        string tempFile = Path.GetTempFileName();
        string autoSaveFile = tempFile + ".autosave";
        File.WriteAllText(autoSaveFile, "test");

        try
        {
            Assert.True(File.Exists(autoSaveFile));
            AutoSaveManager.CleanupAutoSave(tempFile);
            Assert.False(File.Exists(autoSaveFile));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
            if (File.Exists(autoSaveFile)) File.Delete(autoSaveFile);
        }
    }

    [Fact]
    public void CleanupAutoSave_NoFile_DoesNotThrow()
    {
        var ex = Record.Exception(() => AutoSaveManager.CleanupAutoSave("/tmp/nonexistent.tileforge"));
        Assert.Null(ex);
    }
}
