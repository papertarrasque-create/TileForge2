using Microsoft.Xna.Framework;
using TileForge.Game;
using TileForge.Play;
using Xunit;

namespace TileForge.Tests.Game;

public class FloatingMessageTests
{
    [Fact]
    public void AddFloatingMessage_CreatesMessageWithCorrectProperties()
    {
        var play = new PlayState();
        play.AddFloatingMessage("Hit Goblin for 5 damage!", Color.Gold, 3, 7);

        Assert.Single(play.FloatingMessages);
        var fm = play.FloatingMessages[0];
        Assert.Equal("Hit Goblin for 5 damage!", fm.Text);
        Assert.Equal(Color.Gold, fm.Color);
        Assert.Equal(3, fm.TileX);
        Assert.Equal(7, fm.TileY);
        Assert.Equal(FloatingMessage.Duration, fm.Timer);
        Assert.Equal(0f, fm.VerticalOffset);
    }

    [Fact]
    public void MultipleMessages_CoexistIndependently()
    {
        var play = new PlayState();
        play.AddFloatingMessage("Hit Goblin for 5!", Color.Gold, 3, 7);
        play.AddFloatingMessage("Goblin hit you for 3!", Color.Red, 5, 5);

        Assert.Equal(2, play.FloatingMessages.Count);
        Assert.Equal("Hit Goblin for 5!", play.FloatingMessages[0].Text);
        Assert.Equal("Goblin hit you for 3!", play.FloatingMessages[1].Text);
        Assert.Equal(Color.Gold, play.FloatingMessages[0].Color);
        Assert.Equal(Color.Red, play.FloatingMessages[1].Color);
    }

    [Fact]
    public void FloatingMessage_TimerExpires_MessageRemoved()
    {
        var play = new PlayState();
        play.AddFloatingMessage("Test", Color.White, 0, 0);

        // Simulate ticking past full duration
        var fm = play.FloatingMessages[0];
        fm.Timer -= FloatingMessage.Duration + 0.1f;

        // Simulate the removal logic from GameplayScreen.Update
        for (int i = play.FloatingMessages.Count - 1; i >= 0; i--)
        {
            if (play.FloatingMessages[i].Timer <= 0)
                play.FloatingMessages.RemoveAt(i);
        }

        Assert.Empty(play.FloatingMessages);
    }

    [Fact]
    public void FloatingMessage_DriftsUpward_OverLifetime()
    {
        var fm = new FloatingMessage
        {
            Text = "Test",
            Color = Color.White,
            TileX = 0,
            TileY = 0,
            Timer = FloatingMessage.Duration,
            VerticalOffset = 0f,
        };

        // Simulate a half-second tick
        float dt = 0.5f;
        fm.Timer -= dt;
        fm.VerticalOffset += FloatingMessage.DriftPixels * dt / FloatingMessage.Duration;

        Assert.True(fm.VerticalOffset > 0f);
        Assert.Equal(FloatingMessage.DriftPixels * 0.5f, fm.VerticalOffset, 0.01f);
    }

    [Fact]
    public void FloatingMessage_Constants_HaveExpectedValues()
    {
        Assert.Equal(1.0f, FloatingMessage.Duration);
        Assert.Equal(16f, FloatingMessage.DriftPixels);
    }

    [Fact]
    public void AddFloatingMessage_SetsTimerToDuration()
    {
        var play = new PlayState();
        play.AddFloatingMessage("Test", Color.White, 0, 0);

        Assert.Equal(FloatingMessage.Duration, play.FloatingMessages[0].Timer);
    }

    [Fact]
    public void FloatingMessage_PartialTick_StillActive()
    {
        var play = new PlayState();
        play.AddFloatingMessage("Test", Color.White, 0, 0);

        // Tick half the duration
        play.FloatingMessages[0].Timer -= FloatingMessage.Duration * 0.5f;

        Assert.True(play.FloatingMessages[0].Timer > 0);
    }
}
