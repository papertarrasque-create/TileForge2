using System.Collections.Generic;
using System.Linq;
using Xunit;
using TileForge.Data;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class MapTransitionTests
{
    private static (MapData map, Dictionary<string, TileGroup> groups) BuildBasicMap()
    {
        var map = new MapData(10, 10);
        var playerGroup = new TileGroup { Name = "player", Type = GroupType.Entity, IsPlayer = true };
        var npcGroup = new TileGroup { Name = "npc", Type = GroupType.Entity, EntityType = EntityType.NPC };
        var itemGroup = new TileGroup { Name = "potion", Type = GroupType.Entity, EntityType = EntityType.Item };
        map.Entities.Add(new Entity { Id = "p1", GroupName = "player", X = 3, Y = 4 });
        map.Entities.Add(new Entity { Id = "n1", GroupName = "npc", X = 7, Y = 8 });
        map.Entities.Add(new Entity { Id = "i1", GroupName = "potion", X = 5, Y = 5 });
        var groups = new Dictionary<string, TileGroup>
        {
            ["player"] = playerGroup,
            ["npc"] = npcGroup,
            ["potion"] = itemGroup,
        };
        return (map, groups);
    }

    private static LoadedMap BuildLoadedMap(string id = "dungeon_01")
    {
        return new LoadedMap
        {
            Id = id,
            Width = 8,
            Height = 8,
            Layers = new List<LoadedMapLayer>
            {
                new() { Name = "Ground", Cells = new string[64] },
            },
            Groups = new List<TileGroup>
            {
                new() { Name = "player", Type = GroupType.Entity, IsPlayer = true },
                new() { Name = "goblin", Type = GroupType.Entity, EntityType = EntityType.NPC },
                new() { Name = "chest", Type = GroupType.Entity, EntityType = EntityType.Item },
                new() { Name = "door", Type = GroupType.Entity, EntityType = EntityType.Trigger },
            },
            Entities = new List<EntityInstance>
            {
                new() { Id = "p2", DefinitionName = "player", X = 0, Y = 0, IsActive = true },
                new() { Id = "g1", DefinitionName = "goblin", X = 3, Y = 3, IsActive = true,
                    Properties = new Dictionary<string, string> { ["dialogue"] = "grr" } },
                new() { Id = "c1", DefinitionName = "chest", X = 6, Y = 6, IsActive = true },
                new() { Id = "d1", DefinitionName = "door", X = 7, Y = 0, IsActive = true,
                    Properties = new Dictionary<string, string>
                    {
                        ["target_map"] = "overworld.json",
                        ["target_x"] = "2",
                        ["target_y"] = "3",
                    } },
            },
        };
    }

    // ========== SwitchMap — Player position ==========

    [Fact]
    public void SwitchMap_UpdatesPlayerPosition()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();
        manager.Initialize(map, groups);

        var targetMap = BuildLoadedMap();
        manager.SwitchMap(targetMap, 8, 9);

        Assert.Equal(8, manager.State.Player.X);
        Assert.Equal(9, manager.State.Player.Y);
    }

    [Fact]
    public void SwitchMap_UpdatesCurrentMapId()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();
        manager.Initialize(map, groups);

        var targetMap = BuildLoadedMap("dungeon_02");
        manager.SwitchMap(targetMap, 0, 0);

        Assert.Equal("dungeon_02", manager.State.CurrentMapId);
    }

    [Fact]
    public void SwitchMap_SetsVisitedMapFlag()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();
        manager.Initialize(map, groups);

        var targetMap = BuildLoadedMap("cave");
        manager.SwitchMap(targetMap, 0, 0);

        Assert.True(manager.HasFlag("visited_map:cave"));
    }

    // ========== SwitchMap — State preservation ==========

    [Fact]
    public void SwitchMap_PreservesFlags()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();
        manager.Initialize(map, groups);
        manager.SetFlag("quest_complete");
        manager.SetFlag("talked_to_elder");

        var targetMap = BuildLoadedMap();
        manager.SwitchMap(targetMap, 0, 0);

        Assert.True(manager.HasFlag("quest_complete"));
        Assert.True(manager.HasFlag("talked_to_elder"));
    }

    [Fact]
    public void SwitchMap_PreservesVariables()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();
        manager.Initialize(map, groups);
        manager.SetVariable("quest_stage", "3");

        var targetMap = BuildLoadedMap();
        manager.SwitchMap(targetMap, 0, 0);

        Assert.Equal("3", manager.GetVariable("quest_stage"));
    }

    [Fact]
    public void SwitchMap_PreservesInventory()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();
        manager.Initialize(map, groups);
        manager.AddToInventory("iron_key");
        manager.AddToInventory("health_potion");

        var targetMap = BuildLoadedMap();
        manager.SwitchMap(targetMap, 0, 0);

        Assert.True(manager.HasItem("iron_key"));
        Assert.True(manager.HasItem("health_potion"));
    }

    [Fact]
    public void SwitchMap_PreservesHealth()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();
        manager.Initialize(map, groups);
        manager.DamagePlayer(30);

        var targetMap = BuildLoadedMap();
        manager.SwitchMap(targetMap, 0, 0);

        Assert.Equal(70, manager.State.Player.Health);
    }

    // ========== SwitchMap — Active entities ==========

    [Fact]
    public void SwitchMap_RebuildActiveEntities()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();
        manager.Initialize(map, groups);
        Assert.Equal(2, manager.State.ActiveEntities.Count); // npc + potion

        var targetMap = BuildLoadedMap();
        manager.SwitchMap(targetMap, 0, 0);

        // New map has goblin, chest, door (player excluded)
        Assert.Equal(3, manager.State.ActiveEntities.Count);
    }

    [Fact]
    public void SwitchMap_ExcludesPlayerFromActiveEntities()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();
        manager.Initialize(map, groups);

        var targetMap = BuildLoadedMap();
        manager.SwitchMap(targetMap, 0, 0);

        Assert.DoesNotContain(manager.State.ActiveEntities, e => e.DefinitionName == "player");
    }

    [Fact]
    public void SwitchMap_NewEntitiesHaveCorrectProperties()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();
        manager.Initialize(map, groups);

        var targetMap = BuildLoadedMap();
        manager.SwitchMap(targetMap, 0, 0);

        var goblin = manager.State.ActiveEntities.First(e => e.Id == "g1");
        Assert.Equal("goblin", goblin.DefinitionName);
        Assert.Equal(3, goblin.X);
        Assert.Equal(3, goblin.Y);
        Assert.Equal("grr", goblin.Properties["dialogue"]);
    }

    [Fact]
    public void SwitchMap_NewEntitiesCopyProperties()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();
        manager.Initialize(map, groups);

        var targetMap = BuildLoadedMap();
        manager.SwitchMap(targetMap, 0, 0);

        // Mutating the source entity shouldn't affect the game state copy
        var sourceEntity = targetMap.Entities.First(e => e.Id == "g1");
        sourceEntity.Properties["dialogue"] = "changed";

        var goblin = manager.State.ActiveEntities.First(e => e.Id == "g1");
        Assert.Equal("grr", goblin.Properties["dialogue"]);
    }

    // ========== Entity persistence via flags ==========

    [Fact]
    public void SwitchMap_EntityPersistence_InactiveViaFlag()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();
        manager.Initialize(map, groups);

        // Simulate collecting the chest on a previous visit
        manager.SetFlag(GameStateManager.EntityInactivePrefix + "c1");

        var targetMap = BuildLoadedMap();
        manager.SwitchMap(targetMap, 0, 0);

        var chest = manager.State.ActiveEntities.First(e => e.Id == "c1");
        Assert.False(chest.IsActive);
    }

    [Fact]
    public void SwitchMap_EntityPersistence_ActiveWithoutFlag()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();
        manager.Initialize(map, groups);

        var targetMap = BuildLoadedMap();
        manager.SwitchMap(targetMap, 0, 0);

        var chest = manager.State.ActiveEntities.First(e => e.Id == "c1");
        Assert.True(chest.IsActive);
    }

    [Fact]
    public void SwitchMap_EntityPersistence_MultipleFlagged()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();
        manager.Initialize(map, groups);

        // Flag both chest and goblin as inactive
        manager.SetFlag(GameStateManager.EntityInactivePrefix + "c1");
        manager.SetFlag(GameStateManager.EntityInactivePrefix + "g1");

        var targetMap = BuildLoadedMap();
        manager.SwitchMap(targetMap, 0, 0);

        Assert.False(manager.State.ActiveEntities.First(e => e.Id == "c1").IsActive);
        Assert.False(manager.State.ActiveEntities.First(e => e.Id == "g1").IsActive);
        // Door should still be active
        Assert.True(manager.State.ActiveEntities.First(e => e.Id == "d1").IsActive);
    }

    // ========== DeactivateEntity ==========

    [Fact]
    public void DeactivateEntity_SetsInactiveAndFlag()
    {
        var manager = new GameStateManager();
        var entity = new EntityInstance { Id = "item_01", IsActive = true };

        manager.DeactivateEntity(entity);

        Assert.False(entity.IsActive);
        Assert.True(manager.HasFlag(GameStateManager.EntityInactivePrefix + "item_01"));
    }

    [Fact]
    public void DeactivateEntity_PersistsAcrossMapSwitch()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();
        manager.Initialize(map, groups);

        // Deactivate the potion on the first map
        var potion = manager.State.ActiveEntities.First(e => e.Id == "i1");
        manager.DeactivateEntity(potion);

        // Build a second map that also has entity i1
        var targetMap = new LoadedMap
        {
            Id = "map2",
            Width = 5,
            Height = 5,
            Layers = new List<LoadedMapLayer>
            {
                new() { Name = "Ground", Cells = new string[25] },
            },
            Groups = new List<TileGroup>
            {
                new() { Name = "player", Type = GroupType.Entity, IsPlayer = true },
                new() { Name = "potion", Type = GroupType.Entity, EntityType = EntityType.Item },
            },
            Entities = new List<EntityInstance>
            {
                new() { Id = "i1", DefinitionName = "potion", X = 2, Y = 2, IsActive = true },
            },
        };

        manager.SwitchMap(targetMap, 0, 0);

        // The potion should still be inactive on the new map
        var potionOnNewMap = manager.State.ActiveEntities.First(e => e.Id == "i1");
        Assert.False(potionOnNewMap.IsActive);
    }

    // ========== PendingTransition ==========

    [Fact]
    public void PendingTransition_DefaultsToNull()
    {
        var manager = new GameStateManager();

        Assert.Null(manager.PendingTransition);
    }

    [Fact]
    public void PendingTransition_CanBeSetAndRead()
    {
        var manager = new GameStateManager();

        manager.PendingTransition = new MapTransitionRequest
        {
            TargetMap = "dungeon_01.json",
            TargetX = 5,
            TargetY = 12,
        };

        Assert.NotNull(manager.PendingTransition);
        Assert.Equal("dungeon_01.json", manager.PendingTransition.TargetMap);
        Assert.Equal(5, manager.PendingTransition.TargetX);
        Assert.Equal(12, manager.PendingTransition.TargetY);
    }

    [Fact]
    public void PendingTransition_CanBeCleared()
    {
        var manager = new GameStateManager();
        manager.PendingTransition = new MapTransitionRequest
        {
            TargetMap = "test.json",
            TargetX = 0,
            TargetY = 0,
        };

        manager.PendingTransition = null;

        Assert.Null(manager.PendingTransition);
    }

    // ========== Full flow: Initialize → SwitchMap → SwitchMap ==========

    [Fact]
    public void MultipleTransitions_StateAccumulates()
    {
        var (map, groups) = BuildBasicMap();
        var manager = new GameStateManager();
        manager.Initialize(map, groups);

        // Collect item on first map
        var potion = manager.State.ActiveEntities.First(e => e.Id == "i1");
        manager.DeactivateEntity(potion);
        manager.AddToInventory("potion");
        manager.SetFlag("visited_overworld");

        // Transition to dungeon
        var dungeonMap = BuildLoadedMap("dungeon");
        manager.SwitchMap(dungeonMap, 1, 1);
        Assert.Equal("dungeon", manager.State.CurrentMapId);
        Assert.True(manager.HasItem("potion"));
        Assert.True(manager.HasFlag("visited_overworld"));

        // Collect chest in dungeon
        var chest = manager.State.ActiveEntities.First(e => e.Id == "c1");
        manager.DeactivateEntity(chest);
        manager.SetFlag("dungeon_cleared");

        // Transition to another map
        var townMap = new LoadedMap
        {
            Id = "town",
            Width = 5,
            Height = 5,
            Layers = new List<LoadedMapLayer>
            {
                new() { Name = "Ground", Cells = new string[25] },
            },
            Groups = new List<TileGroup>
            {
                new() { Name = "player", Type = GroupType.Entity, IsPlayer = true },
                new() { Name = "shopkeeper", Type = GroupType.Entity, EntityType = EntityType.NPC },
            },
            Entities = new List<EntityInstance>
            {
                new() { Id = "s1", DefinitionName = "shopkeeper", X = 2, Y = 2, IsActive = true },
            },
        };

        manager.SwitchMap(townMap, 3, 3);

        Assert.Equal("town", manager.State.CurrentMapId);
        Assert.Equal(3, manager.State.Player.X);
        Assert.Equal(3, manager.State.Player.Y);
        Assert.True(manager.HasItem("potion"));
        Assert.True(manager.HasFlag("visited_overworld"));
        Assert.True(manager.HasFlag("dungeon_cleared"));
        Assert.True(manager.HasFlag(GameStateManager.EntityInactivePrefix + "i1"));
        Assert.True(manager.HasFlag(GameStateManager.EntityInactivePrefix + "c1"));
        Assert.Single(manager.State.ActiveEntities);
        Assert.Equal("shopkeeper", manager.State.ActiveEntities[0].DefinitionName);
    }
}
