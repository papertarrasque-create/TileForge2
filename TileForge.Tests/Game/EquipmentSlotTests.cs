using System;
using System.Collections.Generic;
using System.Text.Json;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class EquipmentSlotTests
{
    [Fact]
    public void EquipmentSlot_HasThreeValues()
    {
        var values = Enum.GetValues(typeof(EquipmentSlot));
        Assert.Equal(3, values.Length);
    }

    [Fact]
    public void EquipmentSlot_ContainsWeapon()
    {
        Assert.True(Enum.IsDefined(typeof(EquipmentSlot), EquipmentSlot.Weapon));
    }

    [Fact]
    public void EquipmentSlot_ContainsArmor()
    {
        Assert.True(Enum.IsDefined(typeof(EquipmentSlot), EquipmentSlot.Armor));
    }

    [Fact]
    public void EquipmentSlot_ContainsAccessory()
    {
        Assert.True(Enum.IsDefined(typeof(EquipmentSlot), EquipmentSlot.Accessory));
    }

    [Fact]
    public void PlayerState_Equipment_DefaultsToEmptyDictionary()
    {
        var player = new PlayerState();
        Assert.NotNull(player.Equipment);
        Assert.Empty(player.Equipment);
    }

    [Fact]
    public void PlayerState_Equipment_CanStoreBySlotKey()
    {
        var player = new PlayerState();
        player.Equipment[EquipmentSlot.Weapon.ToString()] = "Iron Sword";
        player.Equipment[EquipmentSlot.Armor.ToString()] = "Leather Vest";
        player.Equipment[EquipmentSlot.Accessory.ToString()] = "Lucky Ring";

        Assert.Equal("Iron Sword", player.Equipment["Weapon"]);
        Assert.Equal("Leather Vest", player.Equipment["Armor"]);
        Assert.Equal("Lucky Ring", player.Equipment["Accessory"]);
    }

    [Fact]
    public void PlayerState_SerializeRoundtrip_WithEquipment()
    {
        var player = new PlayerState
        {
            X = 3,
            Y = 7,
            Health = 80
        };
        player.Equipment["Weapon"] = "Fire Staff";
        player.Equipment["Armor"] = "Chain Mail";

        var json = JsonSerializer.Serialize(player);
        var deserialized = JsonSerializer.Deserialize<PlayerState>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(deserialized);
        Assert.Equal(3, deserialized.X);
        Assert.Equal(7, deserialized.Y);
        Assert.Equal(80, deserialized.Health);
        Assert.Equal("Fire Staff", deserialized.Equipment["Weapon"]);
        Assert.Equal("Chain Mail", deserialized.Equipment["Armor"]);
    }

    [Fact]
    public void PlayerState_SerializeRoundtrip_EmptyEquipment()
    {
        var player = new PlayerState();

        var json = JsonSerializer.Serialize(player);
        var deserialized = JsonSerializer.Deserialize<PlayerState>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(deserialized);
        Assert.NotNull(deserialized.Equipment);
        Assert.Empty(deserialized.Equipment);
    }

    [Fact]
    public void GameState_Version_DefaultIs2()
    {
        var state = new GameState();
        Assert.Equal(2, state.Version);
    }

    [Fact]
    public void GameState_V1Json_LoadsWithEmptyEquipment()
    {
        // Simulate a v1 save that has no Equipment field on the Player
        var v1Json = """
            {
                "Version": 1,
                "Player": {
                    "X": 5,
                    "Y": 10,
                    "Health": 90
                },
                "CurrentMapId": "town",
                "ActiveEntities": [],
                "Flags": [],
                "Variables": {},
                "ItemPropertyCache": {}
            }
            """;

        var state = JsonSerializer.Deserialize<GameState>(v1Json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(state);
        Assert.NotNull(state.Player);
        Assert.NotNull(state.Player.Equipment);
        Assert.Empty(state.Player.Equipment);
    }

    [Fact]
    public void GameState_SerializeRoundtrip_WithEquipment()
    {
        var state = new GameState
        {
            CurrentMapId = "dungeon_1"
        };
        state.Player.Equipment["Weapon"] = "Excalibur";
        state.Player.Equipment["Accessory"] = "Amulet of Vitality";
        state.Flags.Add("boss_defeated");

        var json = JsonSerializer.Serialize(state);
        var deserialized = JsonSerializer.Deserialize<GameState>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized.Version);
        Assert.Equal("dungeon_1", deserialized.CurrentMapId);
        Assert.Equal("Excalibur", deserialized.Player.Equipment["Weapon"]);
        Assert.Equal("Amulet of Vitality", deserialized.Player.Equipment["Accessory"]);
        Assert.Contains("boss_defeated", deserialized.Flags);
    }
}
