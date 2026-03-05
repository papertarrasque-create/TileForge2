using System.Collections.Generic;
using System.IO;
using Xunit;
using TileForge.Game;
using TileForge.Tests.Helpers;

namespace TileForge.Tests.Infrastructure;

public class SaveManagerWithMockFsTests
{
    private readonly MockFileSystem _fs;
    private readonly SaveManager _manager;
    private const string SavesDir = "/mock/saves";

    public SaveManagerWithMockFsTests()
    {
        _fs = new MockFileSystem();
        _manager = new SaveManager(SavesDir, _fs);
    }

    [Fact]
    public void Save_WritesJsonToFileSystem()
    {
        var state = new GameState { CurrentMapId = "test_map" };

        _manager.Save(state, "slot1");

        Assert.True(_fs.Exists(Path.Combine(SavesDir, "slot1.json")));
    }

    [Fact]
    public void Save_CreatesDirectory()
    {
        _manager.Save(new GameState(), "slot1");

        Assert.True(_fs.DirectoryExists(SavesDir));
    }

    [Fact]
    public void Load_RestoresGameState()
    {
        var state = new GameState
        {
            CurrentMapId = "dungeon_01",
            Player = new PlayerState { X = 5, Y = 10, Health = 80, MaxHealth = 100 },
            Flags = new HashSet<string> { "chest_opened" },
        };

        _manager.Save(state, "slot1");
        var loaded = _manager.Load("slot1");

        Assert.Equal("dungeon_01", loaded.CurrentMapId);
        Assert.Equal(5, loaded.Player.X);
        Assert.Equal(80, loaded.Player.Health);
        Assert.Contains("chest_opened", loaded.Flags);
    }

    [Fact]
    public void Load_NonExistentSlot_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => _manager.Load("nonexistent"));
    }

    [Fact]
    public void Save_OverwritesExisting()
    {
        _manager.Save(new GameState { CurrentMapId = "map_a" }, "slot1");
        _manager.Save(new GameState { CurrentMapId = "map_b" }, "slot1");

        var loaded = _manager.Load("slot1");
        Assert.Equal("map_b", loaded.CurrentMapId);
    }

    [Fact]
    public void SlotExists_TrueForSaved()
    {
        _manager.Save(new GameState(), "slot1");
        Assert.True(_manager.SlotExists("slot1"));
    }

    [Fact]
    public void SlotExists_FalseForMissing()
    {
        Assert.False(_manager.SlotExists("nonexistent"));
    }

    [Fact]
    public void GetSlots_EmptyWhenNoSaves()
    {
        Assert.Empty(_manager.GetSlots());
    }

    [Fact]
    public void GetSlots_ListsSavedSlots()
    {
        _manager.Save(new GameState(), "save_01");
        _manager.Save(new GameState(), "save_02");

        var slots = _manager.GetSlots();

        Assert.Equal(2, slots.Count);
        Assert.Contains("save_01", slots);
        Assert.Contains("save_02", slots);
    }

    [Fact]
    public void Delete_RemovesSlot()
    {
        _manager.Save(new GameState(), "slot1");
        _manager.Delete("slot1");

        Assert.False(_manager.SlotExists("slot1"));
    }

    [Fact]
    public void Delete_NonExistent_NoError()
    {
        var ex = Record.Exception(() => _manager.Delete("nonexistent"));
        Assert.Null(ex);
    }

    [Fact]
    public void Delete_OnlyAffectsTargetSlot()
    {
        _manager.Save(new GameState(), "slot1");
        _manager.Save(new GameState(), "slot2");

        _manager.Delete("slot1");

        Assert.False(_manager.SlotExists("slot1"));
        Assert.True(_manager.SlotExists("slot2"));
    }
}
