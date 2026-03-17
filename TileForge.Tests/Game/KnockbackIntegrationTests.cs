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
}
