using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game.Screens;

public class BarkOverlayTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private static GameStateManager CreateGSM()
    {
        var gsm = new GameStateManager();
        gsm.State.Player = new PlayerState();
        return gsm;
    }

    private static DialogueData MakeBark(string text, string id = "bark_01")
    {
        return new DialogueData
        {
            Id = id,
            Nodes = new List<DialogueNode>
            {
                new() { Id = "n1", Text = text }
            }
        };
    }

    // =========================================================================
    // BarkOverlay_InactiveByDefault
    // =========================================================================

    [Fact]
    public void BarkOverlay_InactiveByDefault()
    {
        var gsm = CreateGSM();
        var dialogue = MakeBark("Halt!");
        var bark = new BarkOverlay(dialogue, gsm, Vector2.Zero);

        Assert.False(bark.IsActive);
        Assert.Null(bark.DisplayText);
    }

    // =========================================================================
    // BarkOverlay_ShowsText
    // =========================================================================

    [Fact]
    public void BarkOverlay_ShowsText()
    {
        var gsm = CreateGSM();
        var dialogue = MakeBark("Halt!");
        var bark = new BarkOverlay(dialogue, gsm, Vector2.Zero);

        bark.Start();

        Assert.True(bark.IsActive);
        Assert.Equal("Halt!", bark.DisplayText);
    }

    // =========================================================================
    // BarkOverlay_AutoDismissesAfterDuration
    // =========================================================================

    [Fact]
    public void BarkOverlay_AutoDismissesAfterDuration()
    {
        var gsm = CreateGSM();
        var dialogue = MakeBark("Stay back!");
        var bark = new BarkOverlay(dialogue, gsm, Vector2.Zero, durationSeconds: 2f);

        bark.Start();
        Assert.True(bark.IsActive);

        bark.Update(1.0f);
        Assert.True(bark.IsActive);

        bark.Update(1.5f);
        Assert.False(bark.IsActive);
    }

    // =========================================================================
    // BarkOverlay_ExecutesActionsOnStart
    // =========================================================================

    [Fact]
    public void BarkOverlay_ExecutesActionsOnStart()
    {
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "bark_action",
            Nodes = new List<DialogueNode>
            {
                new()
                {
                    Id = "n1",
                    Text = "Guards!",
                    Actions = new List<DialogueAction>
                    {
                        new() { Type = "set_flag", Value = "guard_alerted" }
                    }
                }
            }
        };
        var bark = new BarkOverlay(dialogue, gsm, Vector2.Zero);

        bark.Start();

        Assert.True(gsm.HasFlag("guard_alerted"));
    }

    // =========================================================================
    // BarkOverlay_OneShotSetsFlagOnDismiss
    // =========================================================================

    [Fact]
    public void BarkOverlay_OneShotSetsFlagOnDismiss()
    {
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "bark_oneshot",
            OneShot = true,
            Nodes = new List<DialogueNode>
            {
                new() { Id = "n1", Text = "Welcome, traveler!" }
            }
        };
        var bark = new BarkOverlay(dialogue, gsm, Vector2.Zero, durationSeconds: 1f);

        bark.Start();
        Assert.False(gsm.HasFlag("dialogue_shown:bark_oneshot"));

        bark.Update(1.5f);
        Assert.False(bark.IsActive);
        Assert.True(gsm.HasFlag("dialogue_shown:bark_oneshot"));
    }

    // =========================================================================
    // BarkOverlay_RandomTag_SelectsRandomNode
    // =========================================================================

    [Fact]
    public void BarkOverlay_RandomTag_SelectsRandomNode()
    {
        var gsm = CreateGSM();
        var validTexts = new HashSet<string> { "Line A", "Line B", "Line C" };
        var dialogue = new DialogueData
        {
            Id = "bark_random",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "n1", Text = "Line A", Tags = new List<string> { "random" } },
                new() { Id = "n2", Text = "Line B", Tags = new List<string> { "random" } },
                new() { Id = "n3", Text = "Line C", Tags = new List<string> { "random" } },
            }
        };
        var bark = new BarkOverlay(dialogue, gsm, Vector2.Zero);

        bark.Start();

        Assert.True(bark.IsActive);
        Assert.Contains(bark.DisplayText, validTexts);
    }

    // =========================================================================
    // BarkOverlay_ConditionsEvaluated
    // =========================================================================

    [Fact]
    public void BarkOverlay_ConditionsEvaluated()
    {
        var gsm = CreateGSM();
        gsm.SetFlag("quest_started");

        var dialogue = new DialogueData
        {
            Id = "bark_conditional",
            Routes = new List<DialogueRoute>
            {
                new()
                {
                    StartNode = "quest_node",
                    Conditions = new List<Condition>
                    {
                        new() { Type = "has_flag", Flag = "quest_started" }
                    }
                },
                new()
                {
                    StartNode = "default_node",
                    Conditions = null
                }
            },
            Nodes = new List<DialogueNode>
            {
                new() { Id = "quest_node", Text = "You must find the sword!" },
                new() { Id = "default_node", Text = "Hello there." },
            }
        };
        var bark = new BarkOverlay(dialogue, gsm, Vector2.Zero);

        bark.Start();

        Assert.Equal("You must find the sword!", bark.DisplayText);
    }
}
