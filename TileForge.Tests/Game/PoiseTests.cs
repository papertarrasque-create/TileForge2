using System.Collections.Generic;
using TileForge.Data;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class PoiseTests
{
    private GameStateManager CreateManagerWithPlayer(int health = 100, int poise = 20, int maxPoise = 20)
    {
        var mgr = new GameStateManager();
        var map = new MapData(10, 10);
        var player = new TileGroup
        {
            Name = "player", Type = GroupType.Entity, IsPlayer = true,
            Sprites = { new SpriteRef { Col = 0, Row = 0 } },
        };
        map.Entities.Add(new Entity { GroupName = "player", X = 5, Y = 5 });
        mgr.Initialize(map, new Dictionary<string, TileGroup> { ["player"] = player });
        mgr.State.Player.Health = health;
        mgr.State.Player.MaxHealth = health;
        mgr.State.Player.Poise = poise;
        mgr.State.Player.MaxPoise = maxPoise;
        return mgr;
    }

    [Fact]
    public void DamagePlayer_FullAbsorption_HealthUnchanged()
    {
        var mgr = CreateManagerWithPlayer(poise: 20);
        mgr.DamagePlayer(10);

        Assert.Equal(100, mgr.State.Player.Health);
        Assert.Equal(10, mgr.State.Player.Poise);
    }

    [Fact]
    public void DamagePlayer_PartialAbsorption_RemainderHitsHealth()
    {
        var mgr = CreateManagerWithPlayer(poise: 5);
        mgr.DamagePlayer(15);

        Assert.Equal(0, mgr.State.Player.Poise);
        Assert.Equal(90, mgr.State.Player.Health); // 15 - 5 absorbed = 10 to health
    }

    [Fact]
    public void DamagePlayer_NoPoise_AllToHealth()
    {
        var mgr = CreateManagerWithPlayer(poise: 0);
        mgr.DamagePlayer(10);

        Assert.Equal(0, mgr.State.Player.Poise);
        Assert.Equal(90, mgr.State.Player.Health);
    }

    [Fact]
    public void DamagePlayer_ExactBreak_PoiseZeroHealthUnchanged()
    {
        var mgr = CreateManagerWithPlayer(poise: 10);
        mgr.DamagePlayer(10);

        Assert.Equal(0, mgr.State.Player.Poise);
        Assert.Equal(100, mgr.State.Player.Health);
    }

    [Fact]
    public void DamagePlayer_PoiseBroken_FlagSet()
    {
        var mgr = CreateManagerWithPlayer(poise: 5);
        mgr.DamagePlayer(5);

        Assert.True(mgr.LastDamageBrokePoise);
    }

    [Fact]
    public void DamagePlayer_PoiseNotBroken_FlagNotSet()
    {
        var mgr = CreateManagerWithPlayer(poise: 20);
        mgr.DamagePlayer(5);

        Assert.False(mgr.LastDamageBrokePoise);
    }

    [Fact]
    public void DamagePlayer_AlreadyZeroPoise_FlagNotSet()
    {
        var mgr = CreateManagerWithPlayer(poise: 0);
        mgr.DamagePlayer(5);

        Assert.False(mgr.LastDamageBrokePoise);
    }

    [Fact]
    public void DamagePlayer_FlagResetsOnNextCall()
    {
        var mgr = CreateManagerWithPlayer(poise: 5);
        mgr.DamagePlayer(5); // Breaks poise
        Assert.True(mgr.LastDamageBrokePoise);

        mgr.DamagePlayer(5); // Poise already zero
        Assert.False(mgr.LastDamageBrokePoise);
    }

    [Fact]
    public void RegeneratePoise_RestoresQuarterOfMax()
    {
        var mgr = CreateManagerWithPlayer(poise: 0, maxPoise: 20);
        int amount = mgr.RegeneratePoise();

        Assert.Equal(5, amount);
        Assert.Equal(5, mgr.State.Player.Poise);
    }

    [Fact]
    public void RegeneratePoise_CapsAtMax()
    {
        var mgr = CreateManagerWithPlayer(poise: 18, maxPoise: 20);
        int amount = mgr.RegeneratePoise();

        Assert.Equal(2, amount);
        Assert.Equal(20, mgr.State.Player.Poise);
    }

    [Fact]
    public void RegeneratePoise_AlreadyFull_ReturnsZero()
    {
        var mgr = CreateManagerWithPlayer(poise: 20, maxPoise: 20);
        int amount = mgr.RegeneratePoise();

        Assert.Equal(0, amount);
        Assert.Equal(20, mgr.State.Player.Poise);
    }

    [Fact]
    public void RegeneratePoise_MinimumOnePerTick()
    {
        var mgr = CreateManagerWithPlayer(poise: 0, maxPoise: 3);
        // max(1, 3/4) = max(1, 0) = 1
        int amount = mgr.RegeneratePoise();

        Assert.Equal(1, amount);
        Assert.Equal(1, mgr.State.Player.Poise);
    }

    [Fact]
    public void GetEffectiveMaxPoise_BaseOnly()
    {
        var mgr = CreateManagerWithPlayer(maxPoise: 20);
        Assert.Equal(20, mgr.GetEffectiveMaxPoise());
    }

    [Fact]
    public void GetEffectiveMaxPoise_WithEquipment()
    {
        var mgr = CreateManagerWithPlayer(maxPoise: 20);
        mgr.State.Player.Equipment["Armor"] = "shield";
        mgr.State.ItemPropertyCache["shield"] = new Dictionary<string, string>
        {
            ["equip_poise"] = "10"
        };

        Assert.Equal(30, mgr.GetEffectiveMaxPoise());
    }

    [Fact]
    public void EntityPoise_AbsorbsDamage()
    {
        var mgr = new GameStateManager();
        var entity = new EntityInstance
        {
            Id = "e1", DefinitionName = "knight", X = 3, Y = 3, IsActive = true,
            Properties = new Dictionary<string, string>
            {
                ["health"] = "50", ["max_health"] = "50", ["defense"] = "0", ["poise"] = "10"
            }
        };
        mgr.State.ActiveEntities.Add(entity);

        // attack=8, terrain=0, mult=1.0 → damage=8, poise absorbs 8 → health untouched
        var result = mgr.AttackEntity(entity, 8, 0, 1.0f);

        Assert.Equal(8, result.DamageDealt);
        Assert.Equal(50, result.RemainingHealth); // health unchanged
        Assert.Equal("2", entity.Properties["poise"]); // 10 - 8 = 2
    }

    [Fact]
    public void EntityPoise_PartialAbsorption_RemainderHitsHealth()
    {
        var mgr = new GameStateManager();
        var entity = new EntityInstance
        {
            Id = "e1", DefinitionName = "knight", X = 3, Y = 3, IsActive = true,
            Properties = new Dictionary<string, string>
            {
                ["health"] = "50", ["max_health"] = "50", ["defense"] = "0", ["poise"] = "3"
            }
        };
        mgr.State.ActiveEntities.Add(entity);

        // damage=10, poise absorbs 3 → 7 to health
        var result = mgr.AttackEntity(entity, 10, 0, 1.0f);

        Assert.Equal(10, result.DamageDealt);
        Assert.Equal(43, result.RemainingHealth);
        Assert.Equal("0", entity.Properties["poise"]);
    }

    [Fact]
    public void Initialize_SetsPoise()
    {
        var mgr = new GameStateManager();
        var map = new MapData(10, 10);
        var player = new TileGroup
        {
            Name = "player", Type = GroupType.Entity, IsPlayer = true,
            Sprites = { new SpriteRef { Col = 0, Row = 0 } },
        };
        map.Entities.Add(new Entity { GroupName = "player", X = 5, Y = 5 });
        mgr.Initialize(map, new Dictionary<string, TileGroup> { ["player"] = player });

        Assert.Equal(20, mgr.State.Player.Poise);
        Assert.Equal(20, mgr.State.Player.MaxPoise);
    }

    [Fact]
    public void LoadState_FixupZeroPoise()
    {
        var state = new GameState
        {
            Player = new PlayerState { MaxPoise = 0, Poise = 0, MaxAP = 2 }
        };

        var mgr = new GameStateManager();
        mgr.LoadState(state);

        Assert.Equal(20, mgr.State.Player.MaxPoise);
        Assert.Equal(20, mgr.State.Player.Poise);
    }

    [Fact]
    public void LoadState_PreserveExistingPoise()
    {
        var state = new GameState
        {
            Player = new PlayerState { MaxPoise = 30, Poise = 15, MaxAP = 2 }
        };

        var mgr = new GameStateManager();
        mgr.LoadState(state);

        Assert.Equal(30, mgr.State.Player.MaxPoise);
        Assert.Equal(15, mgr.State.Player.Poise);
    }

    [Fact]
    public void StatusEffect_DamageGoesThoughPoise()
    {
        var mgr = CreateManagerWithPlayer(health: 100, poise: 5);
        mgr.ApplyStatusEffect("fire", 3, 10, 1.0f);

        var messages = mgr.ProcessStatusEffects();

        // 10 damage: 5 absorbed by poise, 5 to health
        Assert.Equal(0, mgr.State.Player.Poise);
        Assert.Equal(95, mgr.State.Player.Health);
    }

    [Fact]
    public void PlayerState_Poise_SerializationRoundTrip()
    {
        var player = new PlayerState { Poise = 15, MaxPoise = 25 };
        var json = System.Text.Json.JsonSerializer.Serialize(player);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<PlayerState>(json);

        Assert.Equal(15, deserialized.Poise);
        Assert.Equal(25, deserialized.MaxPoise);
    }
}
