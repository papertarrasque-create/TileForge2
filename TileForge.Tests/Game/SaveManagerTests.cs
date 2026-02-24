using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class SaveManagerTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SaveManager _manager;

    public SaveManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "tileforge_test_" + Guid.NewGuid().ToString("N"));
        _manager = new SaveManager(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    // ========== Save ==========

    [Fact]
    public void Save_CreatesFile()
    {
        var state = new GameState { CurrentMapId = "test_map" };

        _manager.Save(state, "slot1");

        Assert.True(_manager.SlotExists("slot1"));
    }

    [Fact]
    public void Save_CreatesDirectory()
    {
        var state = new GameState();

        _manager.Save(state, "slot1");

        Assert.True(Directory.Exists(_tempDir));
    }

    [Fact]
    public void Save_OverwritesExisting()
    {
        _manager.Save(new GameState { CurrentMapId = "map_a" }, "slot1");
        _manager.Save(new GameState { CurrentMapId = "map_b" }, "slot1");

        var loaded = _manager.Load("slot1");
        Assert.Equal("map_b", loaded.CurrentMapId);
    }

    // ========== Load ==========

    [Fact]
    public void Load_RestoresGameState()
    {
        var state = new GameState
        {
            Version = 1,
            CurrentMapId = "dungeon_01",
            Player = new PlayerState
            {
                X = 5, Y = 10,
                Facing = Direction.Left,
                Health = 80, MaxHealth = 100,
                Inventory = new List<string> { "sword", "potion" },
            },
            Flags = new HashSet<string> { "chest_opened", "boss_defeated" },
            Variables = new Dictionary<string, string> { ["quest_stage"] = "3" },
        };
        state.ActiveEntities.Add(new EntityInstance
        {
            Id = "e1", DefinitionName = "chest",
            X = 3, Y = 7, IsActive = false,
            Properties = new Dictionary<string, string> { ["loot"] = "gold" },
        });

        _manager.Save(state, "slot1");
        var loaded = _manager.Load("slot1");

        Assert.Equal(1, loaded.Version);
        Assert.Equal("dungeon_01", loaded.CurrentMapId);
        Assert.Equal(5, loaded.Player.X);
        Assert.Equal(10, loaded.Player.Y);
        Assert.Equal(Direction.Left, loaded.Player.Facing);
        Assert.Equal(80, loaded.Player.Health);
        Assert.Equal(100, loaded.Player.MaxHealth);
        Assert.Contains("sword", loaded.Player.Inventory);
        Assert.Contains("potion", loaded.Player.Inventory);
        Assert.Contains("chest_opened", loaded.Flags);
        Assert.Contains("boss_defeated", loaded.Flags);
        Assert.Equal("3", loaded.Variables["quest_stage"]);
        Assert.Single(loaded.ActiveEntities);
        Assert.Equal("e1", loaded.ActiveEntities[0].Id);
        Assert.False(loaded.ActiveEntities[0].IsActive);
        Assert.Equal("gold", loaded.ActiveEntities[0].Properties["loot"]);
    }

    [Fact]
    public void Load_NonExistentSlot_Throws()
    {
        Assert.Throws<FileNotFoundException>(() => _manager.Load("nonexistent"));
    }

    [Fact]
    public void Load_VersionFieldPreserved()
    {
        var state = new GameState { Version = 1 };
        _manager.Save(state, "slot1");

        var loaded = _manager.Load("slot1");

        Assert.Equal(1, loaded.Version);
    }

    // ========== SlotExists ==========

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

    // ========== GetSlots ==========

    [Fact]
    public void GetSlots_EmptyWhenNoSaves()
    {
        var slots = _manager.GetSlots();

        Assert.Empty(slots);
    }

    [Fact]
    public void GetSlots_ListsSavedSlots()
    {
        _manager.Save(new GameState(), "save_01");
        _manager.Save(new GameState(), "save_02");
        _manager.Save(new GameState(), "save_03");

        var slots = _manager.GetSlots();

        Assert.Equal(3, slots.Count);
        Assert.Contains("save_01", slots);
        Assert.Contains("save_02", slots);
        Assert.Contains("save_03", slots);
    }

    [Fact]
    public void GetSlots_ReturnsSortedNames()
    {
        _manager.Save(new GameState(), "save_03");
        _manager.Save(new GameState(), "save_01");
        _manager.Save(new GameState(), "save_02");

        var slots = _manager.GetSlots();

        Assert.Equal("save_01", slots[0]);
        Assert.Equal("save_02", slots[1]);
        Assert.Equal("save_03", slots[2]);
    }

    // ========== Delete ==========

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

    // ========== Multiple saves ==========

    [Fact]
    public void MultipleSlots_IndependentData()
    {
        _manager.Save(new GameState { CurrentMapId = "map_a" }, "slot1");
        _manager.Save(new GameState { CurrentMapId = "map_b" }, "slot2");

        var loaded1 = _manager.Load("slot1");
        var loaded2 = _manager.Load("slot2");

        Assert.Equal("map_a", loaded1.CurrentMapId);
        Assert.Equal("map_b", loaded2.CurrentMapId);
    }
}
