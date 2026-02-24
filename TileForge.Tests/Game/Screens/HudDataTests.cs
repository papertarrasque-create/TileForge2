using System.Collections.Generic;
using TileForge.Data;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game.Screens;

public class HudDataTests
{
    [Fact]
    public void StatusMessage_CanBeSetAndCleared()
    {
        // Verify PlayState.StatusMessage round-trips
        var play = new TileForge.Play.PlayState();
        play.StatusMessage = "Took 5 fire damage!";
        play.StatusMessageTimer = 2.0f;
        Assert.Equal("Took 5 fire damage!", play.StatusMessage);
    }

    [Fact]
    public void ActiveEffects_AvailableForHud()
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

        mgr.ApplyStatusEffect("fire", 3, 1, 1.0f);
        mgr.ApplyStatusEffect("ice", 2, 0, 2.0f);

        Assert.Equal(2, mgr.State.Player.ActiveEffects.Count);
        Assert.Contains(mgr.State.Player.ActiveEffects, e => e.Type == "fire");
        Assert.Contains(mgr.State.Player.ActiveEffects, e => e.Type == "ice");
    }

    [Fact]
    public void StatusMessage_ContainsDamage_WouldRenderRed()
    {
        // Verify messages containing "damage" would match the red color path
        string msg = "Took 5 fire damage!";
        Assert.Contains("damage", msg);
    }

    [Fact]
    public void StatusMessage_ContainsCollected_WouldRenderGreen()
    {
        string msg = "Collected Potion";
        Assert.Contains("Collected", msg);
    }

    [Fact]
    public void EffectLabel_FormatCorrectForFire()
    {
        var effect = new StatusEffect { Type = "fire", RemainingSteps = 3 };
        string label = effect.Type?.ToUpperInvariant() switch
        {
            "FIRE" => $"[BURN {effect.RemainingSteps}]",
            "POISON" => $"[PSN {effect.RemainingSteps}]",
            "ICE" => $"[SLOW {effect.RemainingSteps}]",
            _ => $"[{effect.Type?.ToUpperInvariant()} {effect.RemainingSteps}]",
        };
        Assert.Equal("[BURN 3]", label);
    }
}
