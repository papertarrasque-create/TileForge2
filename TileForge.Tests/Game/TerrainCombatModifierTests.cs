using System.Collections.Generic;
using TileForge.Data;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class TerrainCombatModifierTests
{
    [Fact]
    public void CalculateDamage_WithTerrainBonus_ReducesDamage()
    {
        // attack=10, defense=2, terrain=3 → max(1, 10 - (2+3)) = 5
        Assert.Equal(5, CombatHelper.CalculateDamage(10, 2, 3));
    }

    [Fact]
    public void CalculateDamage_TerrainBonus_FloorOfOne()
    {
        // attack=3, defense=2, terrain=5 → max(1, 3 - (2+5)) = max(1, -4) = 1
        Assert.Equal(1, CombatHelper.CalculateDamage(3, 2, 5));
    }

    [Fact]
    public void CalculateDamage_ZeroTerrainBonus_SameAsOriginal()
    {
        Assert.Equal(CombatHelper.CalculateDamage(8, 3),
                     CombatHelper.CalculateDamage(8, 3, 0));
    }

    [Fact]
    public void TileGroup_DefenseBonus_DefaultsToZero()
    {
        var group = new TileGroup { Name = "grass" };
        Assert.Equal(0, group.DefenseBonus);
    }

    [Fact]
    public void TileGroup_DefenseBonus_CanBeSet()
    {
        var group = new TileGroup { Name = "forest", DefenseBonus = 2 };
        Assert.Equal(2, group.DefenseBonus);
    }

    [Fact]
    public void TileGroup_NoiseLevel_DefaultsToOne()
    {
        var group = new TileGroup { Name = "grass" };
        Assert.Equal(1, group.NoiseLevel);
    }

    [Fact]
    public void TileGroup_NoiseLevel_CanBeSet()
    {
        var group = new TileGroup { Name = "carpet", NoiseLevel = 0 };
        Assert.Equal(0, group.NoiseLevel);
    }

    [Fact]
    public void AttackEntity_WithTerrainBonus_ReducesDamage()
    {
        var mgr = new GameStateManager();
        var entity = new EntityInstance
        {
            Id = "e1", DefinitionName = "goblin", X = 3, Y = 3, IsActive = true,
            Properties = new Dictionary<string, string>
            {
                ["health"] = "50", ["max_health"] = "50", ["defense"] = "2"
            }
        };
        mgr.State.ActiveEntities.Add(entity);

        // attack=10, defense=2, terrain=3, position=1.0 → damage = max(1, 10-(2+3)) = 5
        var result = mgr.AttackEntity(entity, 10, 3, 1.0f);
        Assert.Equal(5, result.DamageDealt);
        Assert.Equal(45, result.RemainingHealth);
    }

    [Fact]
    public void AttackEntity_WithTerrainBonus_StillMinimumOne()
    {
        var mgr = new GameStateManager();
        var entity = new EntityInstance
        {
            Id = "e1", DefinitionName = "knight", X = 3, Y = 3, IsActive = true,
            Properties = new Dictionary<string, string>
            {
                ["health"] = "50", ["max_health"] = "50", ["defense"] = "10"
            }
        };
        mgr.State.ActiveEntities.Add(entity);

        // attack=5, defense=10, terrain=5 → damage = max(1, 5-(10+5)) = 1
        var result = mgr.AttackEntity(entity, 5, 5, 1.0f);
        Assert.Equal(1, result.DamageDealt);
        Assert.Equal(49, result.RemainingHealth);
    }

    [Fact]
    public void TileGroup_DefenseBonus_SerializationRoundTrip()
    {
        var original = new TileGroup { Name = "forest", DefenseBonus = 3, NoiseLevel = 2 };
        var json = System.Text.Json.JsonSerializer.Serialize(original);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TileGroup>(json);

        Assert.Equal(3, deserialized.DefenseBonus);
        Assert.Equal(2, deserialized.NoiseLevel);
    }

    [Fact]
    public void TileGroup_DefenseBonus_DeserializeMissing_DefaultsToZero()
    {
        // Simulate old JSON without DefenseBonus
        var json = "{\"Name\":\"grass\",\"Type\":0,\"IsSolid\":false,\"IsPassable\":true}";
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<TileGroup>(json);

        Assert.Equal(0, deserialized.DefenseBonus);
    }

    [Fact]
    public void CalculateDamage_FullOverload_TerrainAndPosition()
    {
        // attack=10, defense=2, terrain=2, mult=1.5 (flank) → base=max(1,10-(2+2))=6, final=max(1,(int)(6*1.5))=9
        Assert.Equal(9, CombatHelper.CalculateDamage(10, 2, 2, 1.5f));
    }
}
