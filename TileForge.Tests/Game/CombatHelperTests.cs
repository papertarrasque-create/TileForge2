using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class CombatHelperTests
{
    // CalculateDamage tests

    [Fact]
    public void CalculateDamage_AttackGreaterThanDefense_ReturnsDifference()
    {
        Assert.Equal(3, CombatHelper.CalculateDamage(5, 2));
    }

    [Fact]
    public void CalculateDamage_AttackEqualsDefense_ReturnsOne()
    {
        Assert.Equal(1, CombatHelper.CalculateDamage(4, 4));
    }

    [Fact]
    public void CalculateDamage_AttackLessThanDefense_ReturnsOne()
    {
        Assert.Equal(1, CombatHelper.CalculateDamage(2, 10));
    }

    [Fact]
    public void CalculateDamage_HighAttack_ReturnsCorrectValue()
    {
        Assert.Equal(990, CombatHelper.CalculateDamage(1000, 10));
    }

    // GetEntityIntProperty tests

    [Fact]
    public void GetEntityIntProperty_KeyExists_ReturnsIntValue()
    {
        var manager = new GameStateManager();
        var entity = new EntityInstance { Properties = new Dictionary<string, string> { ["hp"] = "42" } };

        var result = manager.GetEntityIntProperty(entity, "hp");

        Assert.Equal(42, result);
    }

    [Fact]
    public void GetEntityIntProperty_KeyMissing_ReturnsDefault()
    {
        var manager = new GameStateManager();
        var entity = new EntityInstance { Properties = new Dictionary<string, string>() };

        var result = manager.GetEntityIntProperty(entity, "missing", 7);

        Assert.Equal(7, result);
    }

    [Fact]
    public void GetEntityIntProperty_ValueNotParseable_ReturnsDefault()
    {
        var manager = new GameStateManager();
        var entity = new EntityInstance { Properties = new Dictionary<string, string> { ["hp"] = "not_a_number" } };

        var result = manager.GetEntityIntProperty(entity, "hp", 99);

        Assert.Equal(99, result);
    }

    // SetEntityIntProperty tests

    [Fact]
    public void SetEntityIntProperty_SetsValueCorrectly()
    {
        var manager = new GameStateManager();
        var entity = new EntityInstance { Properties = new Dictionary<string, string>() };

        manager.SetEntityIntProperty(entity, "hp", 50);

        Assert.Equal("50", entity.Properties["hp"]);
    }

    [Fact]
    public void SetEntityIntProperty_OverwritesExistingValue()
    {
        var manager = new GameStateManager();
        var entity = new EntityInstance { Properties = new Dictionary<string, string> { ["hp"] = "10" } };

        manager.SetEntityIntProperty(entity, "hp", 75);

        Assert.Equal("75", entity.Properties["hp"]);
    }

    // PlayerState Attack/Defense default tests

    [Fact]
    public void PlayerState_Attack_DefaultIsFive()
    {
        var player = new PlayerState();

        Assert.Equal(5, player.Attack);
    }

    [Fact]
    public void PlayerState_Defense_DefaultIsTwo()
    {
        var player = new PlayerState();

        Assert.Equal(2, player.Defense);
    }

    [Fact]
    public void PlayerState_AttackAndDefense_SerializeDeserializeRoundtrip()
    {
        var player = new PlayerState { Attack = 12, Defense = 7 };

        var json = JsonSerializer.Serialize(player);
        var deserialized = JsonSerializer.Deserialize<PlayerState>(json);

        Assert.Equal(12, deserialized.Attack);
        Assert.Equal(7, deserialized.Defense);
    }
}
