using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using TileForge.Data;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class StatusEffectProcessingTests
{
    private GameStateManager CreateInitializedManager()
    {
        var mgr = new GameStateManager();
        var map = new MapData(10, 10);
        var player = new TileGroup
        {
            Name = "player",
            Type = GroupType.Entity,
            IsPlayer = true,
            Sprites = { new SpriteRef { Col = 0, Row = 0 } },
        };
        map.Entities.Add(new Entity { GroupName = "player", X = 5, Y = 5 });
        var groups = new Dictionary<string, TileGroup> { ["player"] = player };
        mgr.Initialize(map, groups);
        return mgr;
    }

    // ========== ApplyStatusEffect ==========

    [Fact]
    public void ApplyStatusEffect_AddsToActiveEffects()
    {
        var mgr = CreateInitializedManager();

        mgr.ApplyStatusEffect("fire", 3, 1, 1.0f);

        var effects = mgr.State.Player.ActiveEffects;
        var effect = Assert.Single(effects);
        Assert.Equal("fire", effect.Type);
        Assert.Equal(3, effect.RemainingSteps);
        Assert.Equal(1, effect.DamagePerStep);
        Assert.Equal(1.0f, effect.MovementMultiplier);
    }

    [Fact]
    public void ApplyStatusEffect_ReplacesExistingType()
    {
        var mgr = CreateInitializedManager();

        mgr.ApplyStatusEffect("fire", 3, 1, 1.0f);
        mgr.ApplyStatusEffect("fire", 5, 2, 1.5f);

        var effects = mgr.State.Player.ActiveEffects;
        var effect = Assert.Single(effects);
        Assert.Equal("fire", effect.Type);
        Assert.Equal(5, effect.RemainingSteps);
        Assert.Equal(2, effect.DamagePerStep);
        Assert.Equal(1.5f, effect.MovementMultiplier);
    }

    [Fact]
    public void ApplyStatusEffect_MultipleDifferentTypes()
    {
        var mgr = CreateInitializedManager();

        mgr.ApplyStatusEffect("fire", 3, 1, 1.0f);
        mgr.ApplyStatusEffect("poison", 6, 1, 1.0f);
        mgr.ApplyStatusEffect("ice", 3, 0, 2.0f);

        Assert.Equal(3, mgr.State.Player.ActiveEffects.Count);
    }

    // ========== ProcessStatusEffects ==========

    [Fact]
    public void ProcessStatusEffects_AppliesDamage()
    {
        var mgr = CreateInitializedManager();
        mgr.State.Player.Poise = 0;
        mgr.ApplyStatusEffect("fire", 3, 5, 1.0f);
        int healthBefore = mgr.State.Player.Health;

        mgr.ProcessStatusEffects();

        Assert.Equal(healthBefore - 5, mgr.State.Player.Health);
    }

    [Fact]
    public void ProcessStatusEffects_DecrementsRemainingSteps()
    {
        var mgr = CreateInitializedManager();
        mgr.ApplyStatusEffect("fire", 3, 1, 1.0f);

        mgr.ProcessStatusEffects();

        Assert.Equal(2, mgr.State.Player.ActiveEffects[0].RemainingSteps);
    }

    [Fact]
    public void ProcessStatusEffects_RemovesExpiredEffects()
    {
        var mgr = CreateInitializedManager();
        mgr.ApplyStatusEffect("fire", 1, 1, 1.0f);

        mgr.ProcessStatusEffects();

        Assert.Empty(mgr.State.Player.ActiveEffects);
    }

    [Fact]
    public void ProcessStatusEffects_ReturnsMessages_DamageAndExpiry()
    {
        var mgr = CreateInitializedManager();
        mgr.ApplyStatusEffect("fire", 2, 5, 1.0f);

        // Step 1: damage only, effect not yet expired
        var messages1 = mgr.ProcessStatusEffects();
        Assert.Contains("fire dealt 5 damage!", messages1);
        Assert.DoesNotContain("fire effect wore off.", messages1);

        // Step 2: damage AND expiry (RemainingSteps reaches 0)
        var messages2 = mgr.ProcessStatusEffects();
        Assert.Contains("fire dealt 5 damage!", messages2);
        Assert.Contains("fire effect wore off.", messages2);
    }

    [Fact]
    public void ProcessStatusEffects_NoDamageEffect_NoMessage()
    {
        var mgr = CreateInitializedManager();
        mgr.ApplyStatusEffect("ice", 3, 0, 2.0f);

        var messages = mgr.ProcessStatusEffects();

        Assert.DoesNotContain(messages, m => m.Contains("damage"));
    }

    [Fact]
    public void ProcessStatusEffects_MultipleEffects_AllProcessed()
    {
        var mgr = CreateInitializedManager();
        mgr.State.Player.Poise = 0;
        mgr.ApplyStatusEffect("fire", 3, 5, 1.0f);
        mgr.ApplyStatusEffect("poison", 6, 3, 1.0f);
        int healthBefore = mgr.State.Player.Health;

        mgr.ProcessStatusEffects();

        // Both fire (5) and poison (3) should deal damage
        Assert.Equal(healthBefore - 8, mgr.State.Player.Health);
    }

    [Fact]
    public void ProcessStatusEffects_PlayerDiesFromEffect()
    {
        var mgr = CreateInitializedManager();
        mgr.State.Player.Poise = 0;
        mgr.DamagePlayer(99); // Health = 1
        mgr.ApplyStatusEffect("fire", 3, 5, 1.0f);

        mgr.ProcessStatusEffects();

        Assert.Equal(0, mgr.State.Player.Health);
    }

    // ========== GetEffectiveMovementMultiplier ==========

    [Fact]
    public void GetEffectiveMovementMultiplier_NoEffects_Returns1()
    {
        var mgr = CreateInitializedManager();

        Assert.Equal(1.0f, mgr.GetEffectiveMovementMultiplier());
    }

    [Fact]
    public void GetEffectiveMovementMultiplier_SingleIceEffect()
    {
        var mgr = CreateInitializedManager();
        mgr.ApplyStatusEffect("ice", 3, 0, 2.0f);

        Assert.Equal(2.0f, mgr.GetEffectiveMovementMultiplier());
    }

    [Fact]
    public void GetEffectiveMovementMultiplier_MultipleEffects_Multiplies()
    {
        var mgr = CreateInitializedManager();
        mgr.ApplyStatusEffect("ice", 3, 0, 2.0f);
        mgr.ApplyStatusEffect("mud", 4, 0, 1.5f);

        Assert.Equal(3.0f, mgr.GetEffectiveMovementMultiplier(), precision: 5);
    }

    // ========== Integration ==========

    [Fact]
    public void ActiveEffects_SurviveMapTransition()
    {
        var mgr = CreateInitializedManager();
        mgr.ApplyStatusEffect("poison", 6, 1, 1.0f);

        var loadedMap = new LoadedMap { Id = "test", Width = 10, Height = 10 };
        loadedMap.Groups.Add(new TileGroup { Name = "player", IsPlayer = true, Sprites = { new SpriteRef() } });
        mgr.SwitchMap(loadedMap, 3, 3);

        Assert.Single(mgr.State.Player.ActiveEffects);
        Assert.Equal("poison", mgr.State.Player.ActiveEffects[0].Type);
        Assert.Equal(6, mgr.State.Player.ActiveEffects[0].RemainingSteps);
    }

    [Fact]
    public void ActiveEffects_SerializationRoundtrip()
    {
        var mgr = CreateInitializedManager();
        mgr.ApplyStatusEffect("fire", 3, 1, 1.0f);
        mgr.ApplyStatusEffect("ice", 2, 0, 2.0f);

        var json = JsonSerializer.Serialize(mgr.State);
        var deserialized = JsonSerializer.Deserialize<GameState>(json);

        Assert.NotNull(deserialized);
        var effects = deserialized.Player.ActiveEffects;
        Assert.Equal(2, effects.Count);

        var fire = effects.First(e => e.Type == "fire");
        Assert.Equal(3, fire.RemainingSteps);
        Assert.Equal(1, fire.DamagePerStep);
        Assert.Equal(1.0f, fire.MovementMultiplier);

        var ice = effects.First(e => e.Type == "ice");
        Assert.Equal(2, ice.RemainingSteps);
        Assert.Equal(0, ice.DamagePerStep);
        Assert.Equal(2.0f, ice.MovementMultiplier);
    }
}
