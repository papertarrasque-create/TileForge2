using System;
using System.Collections.Generic;
using TileForge.Data;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class TileRegistryTests
{
    private static List<TileGroup> SampleGroups() => new()
    {
        new TileGroup { Name = "grass", Type = GroupType.Tile },
        new TileGroup { Name = "lava", Type = GroupType.Tile, IsPassable = false, IsHazardous = true, MovementCost = 2.0f, DamageType = "fire", DamagePerTick = 5 },
        new TileGroup { Name = "swamp", Type = GroupType.Tile, MovementCost = 2.0f },
        new TileGroup { Name = "npc", Type = GroupType.Entity },  // should be excluded
    };

    [Fact]
    public void Constructor_FiltersOnlyTileGroups()
    {
        var registry = new TileRegistry(SampleGroups());
        Assert.Equal(3, registry.Count);
        Assert.False(registry.Contains("npc"));
    }

    [Fact]
    public void Get_ExistingTile_ReturnsTileGroup()
    {
        var registry = new TileRegistry(SampleGroups());
        var grass = registry.Get("grass");
        Assert.Equal("grass", grass.Name);
        Assert.Equal(GroupType.Tile, grass.Type);
    }

    [Fact]
    public void Get_MissingTile_ThrowsKeyNotFoundException()
    {
        var registry = new TileRegistry(SampleGroups());
        Assert.Throws<KeyNotFoundException>(() => registry.Get("missing"));
    }

    [Fact]
    public void IsPassable_DefaultTile_ReturnsTrue()
    {
        var registry = new TileRegistry(SampleGroups());
        Assert.True(registry.IsPassable("grass"));
    }

    [Fact]
    public void IsPassable_ImpassableTile_ReturnsFalse()
    {
        var registry = new TileRegistry(SampleGroups());
        Assert.False(registry.IsPassable("lava"));
    }

    [Fact]
    public void GetMovementCost_DefaultTile_Returns1()
    {
        var registry = new TileRegistry(SampleGroups());
        Assert.Equal(1.0f, registry.GetMovementCost("grass"));
    }

    [Fact]
    public void GetMovementCost_SlowTile_ReturnsCustomValue()
    {
        var registry = new TileRegistry(SampleGroups());
        Assert.Equal(2.0f, registry.GetMovementCost("swamp"));
    }

    [Fact]
    public void IsHazardous_SafeTile_ReturnsFalse()
    {
        var registry = new TileRegistry(SampleGroups());
        Assert.False(registry.IsHazardous("grass"));
    }

    [Fact]
    public void IsHazardous_HazardousTile_ReturnsTrue()
    {
        var registry = new TileRegistry(SampleGroups());
        Assert.True(registry.IsHazardous("lava"));
    }

    [Fact]
    public void GetDamagePerTick_NoDamage_ReturnsZero()
    {
        var registry = new TileRegistry(SampleGroups());
        Assert.Equal(0, registry.GetDamagePerTick("grass"));
    }

    [Fact]
    public void GetDamagePerTick_HazardousTile_ReturnsDamage()
    {
        var registry = new TileRegistry(SampleGroups());
        Assert.Equal(5, registry.GetDamagePerTick("lava"));
    }

    [Fact]
    public void GetDamageType_NoDamage_ReturnsNull()
    {
        var registry = new TileRegistry(SampleGroups());
        Assert.Null(registry.GetDamageType("grass"));
    }

    [Fact]
    public void GetDamageType_HazardousTile_ReturnsDamageType()
    {
        var registry = new TileRegistry(SampleGroups());
        Assert.Equal("fire", registry.GetDamageType("lava"));
    }

    [Fact]
    public void Contains_ExistingTile_ReturnsTrue()
    {
        var registry = new TileRegistry(SampleGroups());
        Assert.True(registry.Contains("grass"));
    }

    [Fact]
    public void Contains_MissingTile_ReturnsFalse()
    {
        var registry = new TileRegistry(SampleGroups());
        Assert.False(registry.Contains("missing"));
    }

    [Fact]
    public void EmptyGroups_ProducesEmptyRegistry()
    {
        var registry = new TileRegistry(new List<TileGroup>());
        Assert.Equal(0, registry.Count);
    }
}
