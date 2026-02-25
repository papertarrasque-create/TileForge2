using Xunit;
using System.Text.Json;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class GameStateTests
{
    [Fact]
    public void GameState_Defaults()
    {
        var state = new GameState();

        Assert.Equal(2, state.Version);
        Assert.NotNull(state.Player);
        Assert.NotNull(state.ActiveEntities);
        Assert.Empty(state.ActiveEntities);
        Assert.NotNull(state.Flags);
        Assert.Empty(state.Flags);
        Assert.NotNull(state.Variables);
        Assert.Empty(state.Variables);
        Assert.Null(state.CurrentMapId);
    }

    [Fact]
    public void PlayerState_Defaults()
    {
        var player = new PlayerState();

        Assert.Equal(100, player.Health);
        Assert.Equal(100, player.MaxHealth);
        Assert.Equal(Direction.Down, player.Facing);
        Assert.NotNull(player.Inventory);
        Assert.Empty(player.Inventory);
    }

    [Fact]
    public void EntityInstance_Defaults()
    {
        var entity = new EntityInstance();

        Assert.True(entity.IsActive);
        Assert.NotNull(entity.Properties);
        Assert.Empty(entity.Properties);
    }

    [Fact]
    public void GameState_SerializationRoundtrip()
    {
        var original = new GameState
        {
            Version = 1,
            CurrentMapId = "map_01",
            Player = new PlayerState
            {
                X = 5,
                Y = 10,
                Facing = Direction.Left,
                Health = 80,
                MaxHealth = 100,
                Inventory = new List<string> { "sword", "potion" }
            },
            ActiveEntities = new List<EntityInstance>
            {
                new EntityInstance
                {
                    Id = "e1",
                    DefinitionName = "chest",
                    X = 3,
                    Y = 7,
                    IsActive = false,
                    Properties = new Dictionary<string, string> { ["loot"] = "gold" }
                }
            },
            Flags = new HashSet<string> { "talked_to_npc", "door_opened" },
            Variables = new Dictionary<string, string> { ["quest_stage"] = "2", ["player_name"] = "Arwen" }
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<GameState>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Version, deserialized.Version);
        Assert.Equal(original.CurrentMapId, deserialized.CurrentMapId);
        Assert.Equal(original.Player.X, deserialized.Player.X);
        Assert.Equal(original.Player.Y, deserialized.Player.Y);
        Assert.Equal(original.Player.Facing, deserialized.Player.Facing);
        Assert.Equal(original.Player.Health, deserialized.Player.Health);
        Assert.Equal(original.Player.MaxHealth, deserialized.Player.MaxHealth);
        Assert.Equal(original.Player.Inventory, deserialized.Player.Inventory);
        Assert.Single(deserialized.ActiveEntities);
        Assert.Equal("e1", deserialized.ActiveEntities[0].Id);
        Assert.Equal("chest", deserialized.ActiveEntities[0].DefinitionName);
        Assert.Equal(3, deserialized.ActiveEntities[0].X);
        Assert.Equal(7, deserialized.ActiveEntities[0].Y);
        Assert.False(deserialized.ActiveEntities[0].IsActive);
        Assert.Equal("gold", deserialized.ActiveEntities[0].Properties["loot"]);
        Assert.Contains("talked_to_npc", deserialized.Flags);
        Assert.Contains("door_opened", deserialized.Flags);
        Assert.Equal("2", deserialized.Variables["quest_stage"]);
        Assert.Equal("Arwen", deserialized.Variables["player_name"]);
    }

    [Fact]
    public void PlayerState_SerializationRoundtrip()
    {
        var original = new PlayerState
        {
            X = 12,
            Y = 34,
            Facing = Direction.Up,
            Health = 60,
            MaxHealth = 120,
            Inventory = new List<string> { "shield", "map", "key" }
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<PlayerState>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.X, deserialized.X);
        Assert.Equal(original.Y, deserialized.Y);
        Assert.Equal(original.Facing, deserialized.Facing);
        Assert.Equal(original.Health, deserialized.Health);
        Assert.Equal(original.MaxHealth, deserialized.MaxHealth);
        Assert.Equal(original.Inventory, deserialized.Inventory);
    }

    [Fact]
    public void EntityInstance_SerializationRoundtrip()
    {
        var original = new EntityInstance
        {
            Id = "npc_42",
            DefinitionName = "villager",
            X = 9,
            Y = 2,
            IsActive = false,
            Properties = new Dictionary<string, string>
            {
                ["dialogue"] = "Good day!",
                ["quest"] = "fetch_herbs"
            }
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<EntityInstance>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Id, deserialized.Id);
        Assert.Equal(original.DefinitionName, deserialized.DefinitionName);
        Assert.Equal(original.X, deserialized.X);
        Assert.Equal(original.Y, deserialized.Y);
        Assert.Equal(original.IsActive, deserialized.IsActive);
        Assert.Equal(original.Properties["dialogue"], deserialized.Properties["dialogue"]);
        Assert.Equal(original.Properties["quest"], deserialized.Properties["quest"]);
    }

    [Fact]
    public void GameState_VersionFieldInJson()
    {
        var state = new GameState();

        var json = JsonSerializer.Serialize(state);

        Assert.Contains("\"Version\":2", json);
    }

    [Fact]
    public void Direction_AllValues()
    {
        var values = Enum.GetValues<Direction>();

        Assert.Contains(Direction.Up, values);
        Assert.Contains(Direction.Down, values);
        Assert.Contains(Direction.Left, values);
        Assert.Contains(Direction.Right, values);
        Assert.Equal(4, values.Length);
    }
}
