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

    // PropertyAccess.GetInt tests

    [Fact]
    public void GetInt_KeyExists_ReturnsIntValue()
    {
        var props = new Dictionary<string, string> { ["hp"] = "42" };

        var result = PropertyAccess.GetInt(props, "hp");

        Assert.Equal(42, result);
    }

    [Fact]
    public void GetInt_KeyMissing_ReturnsDefault()
    {
        var props = new Dictionary<string, string>();

        var result = PropertyAccess.GetInt(props, "missing", 7);

        Assert.Equal(7, result);
    }

    [Fact]
    public void GetInt_ValueNotParseable_ReturnsDefault()
    {
        var props = new Dictionary<string, string> { ["hp"] = "not_a_number" };

        var result = PropertyAccess.GetInt(props, "hp", 99);

        Assert.Equal(99, result);
    }

    // PropertyAccess.SetInt tests

    [Fact]
    public void SetInt_SetsValueCorrectly()
    {
        var props = new Dictionary<string, string>();

        PropertyAccess.SetInt(props, "hp", 50);

        Assert.Equal("50", props["hp"]);
    }

    [Fact]
    public void SetInt_OverwritesExistingValue()
    {
        var props = new Dictionary<string, string> { ["hp"] = "10" };

        PropertyAccess.SetInt(props, "hp", 75);

        Assert.Equal("75", props["hp"]);
    }

    // PlayerState Attack/Defense default tests

    [Fact]
    public void PlayerState_Attack_DefaultIsFive()
    {
        var player = new PlayerState();

        Assert.Equal(5, player.Attack);
    }

    [Fact]
    public void PlayerState_Defense_DefaultIsThree()
    {
        var player = new PlayerState();

        Assert.Equal(3, player.Defense);
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
