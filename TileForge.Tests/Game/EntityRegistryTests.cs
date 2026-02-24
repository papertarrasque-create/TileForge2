using System;
using System.Collections.Generic;
using TileForge.Data;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class EntityRegistryTests
{
    private static List<TileGroup> SampleGroups() => new()
    {
        new TileGroup { Name = "grass", Type = GroupType.Tile },  // should be excluded
        new TileGroup { Name = "npc_elder", Type = GroupType.Entity, EntityType = EntityType.NPC },
        new TileGroup { Name = "health_potion", Type = GroupType.Entity, EntityType = EntityType.Item },
        new TileGroup { Name = "fire_trap", Type = GroupType.Entity, EntityType = EntityType.Trap },
        new TileGroup { Name = "door", Type = GroupType.Entity, EntityType = EntityType.Trigger },
        new TileGroup { Name = "chest", Type = GroupType.Entity },  // default = Interactable
    };

    [Fact]
    public void Constructor_FiltersOnlyEntityGroups()
    {
        var registry = new EntityRegistry(SampleGroups());
        Assert.Equal(5, registry.Count);
        Assert.False(registry.Contains("grass"));
    }

    [Fact]
    public void Get_ExistingEntity_ReturnsTileGroup()
    {
        var registry = new EntityRegistry(SampleGroups());
        var npc = registry.Get("npc_elder");
        Assert.Equal("npc_elder", npc.Name);
        Assert.Equal(GroupType.Entity, npc.Type);
    }

    [Fact]
    public void Get_MissingEntity_ThrowsKeyNotFoundException()
    {
        var registry = new EntityRegistry(SampleGroups());
        Assert.Throws<KeyNotFoundException>(() => registry.Get("missing"));
    }

    [Fact]
    public void GetEntityType_NPC_ReturnsNPC()
    {
        var registry = new EntityRegistry(SampleGroups());
        Assert.Equal(EntityType.NPC, registry.GetEntityType("npc_elder"));
    }

    [Fact]
    public void GetEntityType_Item_ReturnsItem()
    {
        var registry = new EntityRegistry(SampleGroups());
        Assert.Equal(EntityType.Item, registry.GetEntityType("health_potion"));
    }

    [Fact]
    public void GetEntityType_Trap_ReturnsTrap()
    {
        var registry = new EntityRegistry(SampleGroups());
        Assert.Equal(EntityType.Trap, registry.GetEntityType("fire_trap"));
    }

    [Fact]
    public void GetEntityType_Trigger_ReturnsTrigger()
    {
        var registry = new EntityRegistry(SampleGroups());
        Assert.Equal(EntityType.Trigger, registry.GetEntityType("door"));
    }

    [Fact]
    public void GetEntityType_Default_ReturnsInteractable()
    {
        var registry = new EntityRegistry(SampleGroups());
        Assert.Equal(EntityType.Interactable, registry.GetEntityType("chest"));
    }

    [Fact]
    public void Contains_ExistingEntity_ReturnsTrue()
    {
        var registry = new EntityRegistry(SampleGroups());
        Assert.True(registry.Contains("npc_elder"));
    }

    [Fact]
    public void Contains_MissingEntity_ReturnsFalse()
    {
        var registry = new EntityRegistry(SampleGroups());
        Assert.False(registry.Contains("missing"));
    }

    [Fact]
    public void EmptyGroups_ProducesEmptyRegistry()
    {
        var registry = new EntityRegistry(new List<TileGroup>());
        Assert.Equal(0, registry.Count);
    }
}
