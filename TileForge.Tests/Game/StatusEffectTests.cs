using System.Collections.Generic;
using System.Text.Json;
using Xunit;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class StatusEffectTests
{
    // ========== StatusEffect defaults ==========

    [Fact]
    public void StatusEffect_Defaults()
    {
        var effect = new StatusEffect();

        Assert.Null(effect.Type);
        Assert.Equal(0, effect.RemainingSteps);
        Assert.Equal(0, effect.DamagePerStep);
        Assert.Equal(1.0f, effect.MovementMultiplier);
    }

    // ========== StatusEffect type construction ==========

    [Fact]
    public void StatusEffect_FireType()
    {
        var effect = new StatusEffect
        {
            Type = "fire",
            RemainingSteps = 3,
            DamagePerStep = 1
        };

        Assert.Equal("fire", effect.Type);
        Assert.Equal(3, effect.RemainingSteps);
        Assert.Equal(1, effect.DamagePerStep);
        Assert.Equal(1.0f, effect.MovementMultiplier);
    }

    [Fact]
    public void StatusEffect_PoisonType()
    {
        var effect = new StatusEffect
        {
            Type = "poison",
            RemainingSteps = 6,
            DamagePerStep = 1
        };

        Assert.Equal("poison", effect.Type);
        Assert.Equal(6, effect.RemainingSteps);
        Assert.Equal(1, effect.DamagePerStep);
        Assert.Equal(1.0f, effect.MovementMultiplier);
    }

    [Fact]
    public void StatusEffect_IceType()
    {
        var effect = new StatusEffect
        {
            Type = "ice",
            RemainingSteps = 3,
            MovementMultiplier = 2.0f
        };

        Assert.Equal("ice", effect.Type);
        Assert.Equal(3, effect.RemainingSteps);
        Assert.Equal(0, effect.DamagePerStep);
        Assert.Equal(2.0f, effect.MovementMultiplier);
    }

    // ========== StatusEffect serialization ==========

    [Fact]
    public void StatusEffect_SerializationRoundtrip()
    {
        var original = new StatusEffect
        {
            Type = "fire",
            RemainingSteps = 3,
            DamagePerStep = 1,
            MovementMultiplier = 1.5f
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<StatusEffect>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Type, deserialized.Type);
        Assert.Equal(original.RemainingSteps, deserialized.RemainingSteps);
        Assert.Equal(original.DamagePerStep, deserialized.DamagePerStep);
        Assert.Equal(original.MovementMultiplier, deserialized.MovementMultiplier);
    }

    // ========== PlayerState.ActiveEffects defaults ==========

    [Fact]
    public void PlayerState_ActiveEffects_DefaultsToEmptyList()
    {
        var player = new PlayerState();

        Assert.NotNull(player.ActiveEffects);
        Assert.Empty(player.ActiveEffects);
    }

    // ========== PlayerState.ActiveEffects add/remove ==========

    [Fact]
    public void PlayerState_ActiveEffects_CanAddEffect()
    {
        var player = new PlayerState();
        var effect = new StatusEffect { Type = "fire", RemainingSteps = 3, DamagePerStep = 1 };

        player.ActiveEffects.Add(effect);

        Assert.Single(player.ActiveEffects);
        Assert.Equal("fire", player.ActiveEffects[0].Type);
    }

    [Fact]
    public void PlayerState_ActiveEffects_CanAddMultipleEffects()
    {
        var player = new PlayerState();
        player.ActiveEffects.Add(new StatusEffect { Type = "fire", RemainingSteps = 3, DamagePerStep = 1 });
        player.ActiveEffects.Add(new StatusEffect { Type = "poison", RemainingSteps = 6, DamagePerStep = 1 });
        player.ActiveEffects.Add(new StatusEffect { Type = "ice", RemainingSteps = 3, MovementMultiplier = 2.0f });

        Assert.Equal(3, player.ActiveEffects.Count);
        Assert.Equal("fire", player.ActiveEffects[0].Type);
        Assert.Equal("poison", player.ActiveEffects[1].Type);
        Assert.Equal("ice", player.ActiveEffects[2].Type);
    }

    [Fact]
    public void PlayerState_ActiveEffects_CanRemoveEffect()
    {
        var player = new PlayerState();
        var fire = new StatusEffect { Type = "fire", RemainingSteps = 3, DamagePerStep = 1 };
        var poison = new StatusEffect { Type = "poison", RemainingSteps = 6, DamagePerStep = 1 };
        player.ActiveEffects.Add(fire);
        player.ActiveEffects.Add(poison);

        player.ActiveEffects.Remove(fire);

        Assert.Single(player.ActiveEffects);
        Assert.Equal("poison", player.ActiveEffects[0].Type);
    }

    // ========== PlayerState with ActiveEffects serialization ==========

    [Fact]
    public void PlayerState_WithActiveEffects_SerializationRoundtrip()
    {
        var original = new PlayerState
        {
            X = 5,
            Y = 10,
            Facing = Direction.Right,
            Health = 75,
            MaxHealth = 100,
            Inventory = new List<string> { "potion" },
            ActiveEffects = new List<StatusEffect>
            {
                new StatusEffect { Type = "fire", RemainingSteps = 3, DamagePerStep = 1, MovementMultiplier = 1.0f },
                new StatusEffect { Type = "ice", RemainingSteps = 2, DamagePerStep = 0, MovementMultiplier = 2.0f }
            }
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
        Assert.Equal(2, deserialized.ActiveEffects.Count);
        Assert.Equal("fire", deserialized.ActiveEffects[0].Type);
        Assert.Equal(3, deserialized.ActiveEffects[0].RemainingSteps);
        Assert.Equal(1, deserialized.ActiveEffects[0].DamagePerStep);
        Assert.Equal(1.0f, deserialized.ActiveEffects[0].MovementMultiplier);
        Assert.Equal("ice", deserialized.ActiveEffects[1].Type);
        Assert.Equal(2, deserialized.ActiveEffects[1].RemainingSteps);
        Assert.Equal(0, deserialized.ActiveEffects[1].DamagePerStep);
        Assert.Equal(2.0f, deserialized.ActiveEffects[1].MovementMultiplier);
    }
}
