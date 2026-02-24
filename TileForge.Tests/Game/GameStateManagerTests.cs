using System.Collections.Generic;
using System.Linq;
using Xunit;
using TileForge.Game;
using TileForge.Data;

namespace TileForge.Tests.Game;

public class GameStateManagerTests
{
    private static (MapData map, Dictionary<string, TileGroup> groups) BuildBasicMap()
    {
        var map = new MapData(10, 10);
        var playerGroup = new TileGroup { Name = "player", Type = GroupType.Entity, IsPlayer = true };
        var npcGroup = new TileGroup { Name = "npc", Type = GroupType.Entity, IsPlayer = false };
        map.Entities.Add(new Entity { Id = "p1", GroupName = "player", X = 3, Y = 4 });
        map.Entities.Add(new Entity { Id = "n1", GroupName = "npc", X = 7, Y = 8, Properties = new() { ["dialogue"] = "hello" } });
        var groups = new Dictionary<string, TileGroup> { ["player"] = playerGroup, ["npc"] = npcGroup };
        return (map, groups);
    }

    [Fact]
    public void Initialize_FindsPlayerEntity()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();

        manager.Initialize(map, groups);

        Assert.Equal(3, manager.State.Player.X);
        Assert.Equal(4, manager.State.Player.Y);
    }

    [Fact]
    public void Initialize_BuildsActiveEntities()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();

        manager.Initialize(map, groups);

        Assert.Single(manager.State.ActiveEntities);
        var entity = manager.State.ActiveEntities[0];
        Assert.Equal("n1", entity.Id);
        Assert.Equal("npc", entity.DefinitionName);
        Assert.Equal(7, entity.X);
        Assert.Equal(8, entity.Y);
    }

    [Fact]
    public void Initialize_ExcludesPlayerFromActiveEntities()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();

        manager.Initialize(map, groups);

        Assert.DoesNotContain(manager.State.ActiveEntities, e => e.Id == "p1");
    }

    [Fact]
    public void Initialize_CopiesEntityProperties()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();

        manager.Initialize(map, groups);

        var sourceEntity = map.Entities.First(e => e.Id == "n1");
        var instanceEntity = manager.State.ActiveEntities[0];

        Assert.Equal("hello", instanceEntity.Properties["dialogue"]);

        // Mutating the source should not affect the instance (copied, not referenced)
        sourceEntity.Properties["dialogue"] = "changed";
        Assert.Equal("hello", instanceEntity.Properties["dialogue"]);
    }

    [Fact]
    public void SetFlag_HasFlag()
    {
        var manager = new GameStateManager();

        manager.SetFlag("chest_opened");

        Assert.True(manager.HasFlag("chest_opened"));
    }

    [Fact]
    public void HasFlag_ReturnsFalseForUnsetFlag()
    {
        var manager = new GameStateManager();

        Assert.False(manager.HasFlag("chest_opened"));
    }

    [Fact]
    public void SetVariable_GetVariable()
    {
        var manager = new GameStateManager();

        manager.SetVariable("quest_stage", "3");

        Assert.Equal("3", manager.GetVariable("quest_stage"));
    }

    [Fact]
    public void GetVariable_ReturnsNullForMissing()
    {
        var manager = new GameStateManager();

        Assert.Null(manager.GetVariable("nonexistent_var"));
    }

    [Fact]
    public void DamagePlayer_ReducesHealth()
    {
        var manager = new GameStateManager();

        manager.DamagePlayer(30);

        Assert.Equal(70, manager.State.Player.Health);
    }

    [Fact]
    public void DamagePlayer_ClampsAtZero()
    {
        var manager = new GameStateManager();

        manager.DamagePlayer(999);

        Assert.Equal(0, manager.State.Player.Health);
    }

    [Fact]
    public void HealPlayer_IncreasesHealth()
    {
        var manager = new GameStateManager();
        manager.DamagePlayer(50);

        manager.HealPlayer(20);

        Assert.Equal(70, manager.State.Player.Health);
    }

    [Fact]
    public void HealPlayer_ClampsAtMaxHealth()
    {
        var manager = new GameStateManager();
        manager.DamagePlayer(10);

        manager.HealPlayer(999);

        Assert.Equal(manager.State.Player.MaxHealth, manager.State.Player.Health);
    }

    [Fact]
    public void IsPlayerAlive_TrueWhenHealthPositive()
    {
        var manager = new GameStateManager();

        Assert.True(manager.IsPlayerAlive());
    }

    [Fact]
    public void IsPlayerAlive_FalseWhenHealthZero()
    {
        var manager = new GameStateManager();
        manager.DamagePlayer(100);

        Assert.False(manager.IsPlayerAlive());
    }

    [Fact]
    public void AddToInventory_HasItem()
    {
        var manager = new GameStateManager();

        manager.AddToInventory("iron_key");

        Assert.True(manager.HasItem("iron_key"));
    }

    [Fact]
    public void HasItem_ReturnsFalseForMissing()
    {
        var manager = new GameStateManager();

        Assert.False(manager.HasItem("iron_key"));
    }

    [Fact]
    public void IncrementVariable_NewVariable_SetsToOne()
    {
        var manager = new GameStateManager();

        manager.IncrementVariable("test_counter");

        Assert.Equal("1", manager.GetVariable("test_counter"));
    }

    [Fact]
    public void IncrementVariable_ExistingVariable_Increments()
    {
        var manager = new GameStateManager();
        manager.SetVariable("counter", "5");

        manager.IncrementVariable("counter");

        Assert.Equal("6", manager.GetVariable("counter"));
    }

    [Fact]
    public void IncrementVariable_NonNumericVariable_SetsToOne()
    {
        var manager = new GameStateManager();
        manager.SetVariable("counter", "abc");

        manager.IncrementVariable("counter");

        Assert.Equal("1", manager.GetVariable("counter"));
    }
}
