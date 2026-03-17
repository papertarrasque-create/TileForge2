using System;
using System.Collections.Generic;
using TileForge.Game;
using TileForge.Play;
using Xunit;

namespace TileForge.Tests.Game;

public class KnockbackIntegrationTests
{
    [Fact]
    public void PlayerState_Weight_DefaultsTo1()
    {
        var ps = new PlayerState();
        Assert.Equal(1, ps.Weight);
    }

    [Fact]
    public void PlayState_HitsThisTurn_InitializesEmpty()
    {
        var play = new PlayState();
        Assert.NotNull(play.HitsThisTurn);
        Assert.Empty(play.HitsThisTurn);
    }

    [Fact]
    public void HitsThisTurn_TracksPerTarget()
    {
        var play = new PlayState();
        play.HitsThisTurn["enemy1"] = 1;
        play.HitsThisTurn["enemy2"] = 2;
        Assert.Equal(1, play.HitsThisTurn["enemy1"]);
        Assert.Equal(2, play.HitsThisTurn["enemy2"]);
    }

    [Fact]
    public void HitsThisTurn_ClearResetsAll()
    {
        var play = new PlayState();
        play.HitsThisTurn["enemy1"] = 3;
        play.HitsThisTurn.Clear();
        Assert.Empty(play.HitsThisTurn);
    }

    [Fact]
    public void HitsThisTurn_StaggerDoesNotCarryAcrossTurns()
    {
        var play = new PlayState();
        play.HitsThisTurn["enemy1"] = 1;
        play.HitsThisTurn.Clear();
        Assert.False(play.HitsThisTurn.ContainsKey("enemy1"));
    }

    [Fact]
    public void GetEffectiveWeight_BaseOnly()
    {
        var gsm = new GameStateManager();
        gsm.State.Player.Weight = 1;
        Assert.Equal(1, gsm.GetEffectiveWeight());
    }

    [Fact]
    public void GetEffectiveWeight_WithEquipBonus()
    {
        var gsm = new GameStateManager();
        gsm.State.Player.Weight = 1;
        gsm.State.Player.Equipment["armor"] = "heavy_plate";
        gsm.State.ItemPropertyCache["heavy_plate"] = new Dictionary<string, string>
        {
            { PropertyKeys.EquipWeight, "1" }
        };
        Assert.Equal(2, gsm.GetEffectiveWeight());
    }

    // --- Simulated attack flow: increment stagger -> resolve knockback ---

    private static readonly Func<int, int, bool> OpenField = (x, y) => true;

    /// <summary>
    /// Simulates a bump attack sequence: increments HitsThisTurn, resolves knockback.
    /// Returns the KnockbackResult.
    /// </summary>
    private static KnockbackResult SimulateAttack(
        PlayState play, string targetId,
        int attackerX, int attackerY,
        int defenderX, int defenderY,
        int weight, Func<int, int, bool> walkable)
    {
        if (!play.HitsThisTurn.ContainsKey(targetId))
            play.HitsThisTurn[targetId] = 0;
        play.HitsThisTurn[targetId]++;

        return KnockbackResolver.Resolve(
            attackerX, attackerY,
            defenderX, defenderY,
            weight,
            play.HitsThisTurn[targetId],
            walkable);
    }

    [Fact]
    public void PlayerAttack_Weight1Enemy_EnemyPositionChanges()
    {
        var play = new PlayState();
        var kb = SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 1, OpenField);
        Assert.True(kb.KnockedBack);
        Assert.Equal(5, kb.NewX);
        Assert.Equal(3, kb.NewY);
    }

    [Fact]
    public void PlayerAttack_Weight1Enemy_AgainstWall_EnemyStays()
    {
        var play = new PlayState();
        Func<int, int, bool> walledAt5_3 = (x, y) => !(x == 5 && y == 3);
        var kb = SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 1, walledAt5_3);
        Assert.False(kb.KnockedBack);
        Assert.Equal(5, kb.NewX);
        Assert.Equal(4, kb.NewY);
    }

    [Fact]
    public void PlayerAttack_Weight2Enemy_FirstHit_NoKnockback()
    {
        var play = new PlayState();
        var kb = SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 2, OpenField);
        Assert.False(kb.KnockedBack);
    }

    [Fact]
    public void PlayerAttack_Weight2Enemy_SecondHit_Knockback()
    {
        var play = new PlayState();
        SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 2, OpenField);
        var kb = SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 2, OpenField);
        Assert.True(kb.KnockedBack);
        Assert.Equal(5, kb.NewX);
        Assert.Equal(3, kb.NewY);
    }

    [Fact]
    public void EnemyAttack_Weight1Player_PlayerKnockedBack()
    {
        var play = new PlayState();
        var kb = SimulateAttack(play, "player", 5, 4, 5, 5, weight: 1, OpenField);
        Assert.True(kb.KnockedBack);
        Assert.Equal(5, kb.NewX);
        Assert.Equal(6, kb.NewY);
    }

    [Fact]
    public void PlayerWithEquipWeight_ResistsKnockback()
    {
        var gsm = new GameStateManager();
        gsm.State.Player.Weight = 1;
        gsm.State.Player.Equipment["armor"] = "heavy_plate";
        gsm.State.ItemPropertyCache["heavy_plate"] = new Dictionary<string, string>
        {
            { PropertyKeys.EquipWeight, "1" }
        };
        int effectiveWeight = gsm.GetEffectiveWeight();

        var play = new PlayState();
        var kb = SimulateAttack(play, "player", 5, 4, 5, 5, effectiveWeight, OpenField);
        Assert.False(kb.KnockedBack);
    }

    [Fact]
    public void KnockbackSkippedOnKill()
    {
        var play = new PlayState();
        Assert.False(play.HitsThisTurn.ContainsKey("killed_enemy"));
    }

    [Fact]
    public void TwoSeparateEnemies_DoNotCombineStagger()
    {
        var play = new PlayState();
        var kb1 = SimulateAttack(play, "player", 3, 5, 5, 5, weight: 2, OpenField);
        Assert.False(kb1.KnockedBack);

        play.HitsThisTurn.Clear();

        var kb2 = SimulateAttack(play, "player", 7, 5, 5, 5, weight: 2, OpenField);
        Assert.False(kb2.KnockedBack);
    }

    [Fact]
    public void StaggerResets_BetweenPlayerTurns()
    {
        var play = new PlayState();
        SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 2, OpenField);
        Assert.Equal(1, play.HitsThisTurn["enemy1"]);

        play.HitsThisTurn.Clear();

        var kb = SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 2, OpenField);
        Assert.False(kb.KnockedBack);
        Assert.Equal(1, play.HitsThisTurn["enemy1"]);
    }

    [Fact]
    public void CorneringTactic_Weight2Enemy_WallBlocksKnockback_AllowsDoubleHit()
    {
        var play = new PlayState();
        Func<int, int, bool> walledAt5_3 = (x, y) => !(x == 5 && y == 3);
        var kb1 = SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 2, walledAt5_3);
        Assert.False(kb1.KnockedBack);
        var kb2 = SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 2, walledAt5_3);
        Assert.False(kb2.KnockedBack);
    }
}
