using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game.Screens;

public class InspectOverlayTests
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

    private static GameInputManager SimulateKeyPress(Keys key)
    {
        var input = new GameInputManager();
        input.Update(new KeyboardState());
        input.Update(new KeyboardState(key));
        return input;
    }

    private static GameInputManager NoInput()
    {
        var input = new GameInputManager();
        input.Update(new KeyboardState());
        input.Update(new KeyboardState());
        return input;
    }

    private static readonly GameTime ShortTime =
        new GameTime(System.TimeSpan.FromSeconds(0), System.TimeSpan.FromMilliseconds(16));

    // =========================================================================
    // InspectOverlay_IsOverlay
    // =========================================================================

    [Fact]
    public void InspectOverlay_IsOverlay()
    {
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "sign_01",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "n1", Text = "You see a wooden sign." }
            }
        };
        var overlay = new InspectOverlay(dialogue, gsm);

        Assert.True(overlay.IsOverlay);
    }

    // =========================================================================
    // InspectOverlay_DismissesOnInteract
    // =========================================================================

    [Fact]
    public void InspectOverlay_DismissesOnInteract()
    {
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "sign_02",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "n1", Text = "A notice board." }
            }
        };
        var overlay = new InspectOverlay(dialogue, gsm);
        var sm = new ScreenManager();
        sm.Push(overlay);

        var interact = SimulateKeyPress(Keys.Z);
        overlay.Update(ShortTime, interact);

        Assert.False(sm.HasScreens);
    }

    // =========================================================================
    // InspectOverlay_DismissesOnCancel
    // =========================================================================

    [Fact]
    public void InspectOverlay_DismissesOnCancel()
    {
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "sign_03",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "n1", Text = "Some ancient text." }
            }
        };
        var overlay = new InspectOverlay(dialogue, gsm);
        var sm = new ScreenManager();
        sm.Push(overlay);

        var cancel = SimulateKeyPress(Keys.Escape);
        overlay.Update(ShortTime, cancel);

        Assert.False(sm.HasScreens);
    }

    // =========================================================================
    // InspectOverlay_ExecutesActionsOnEnter
    // =========================================================================

    [Fact]
    public void InspectOverlay_ExecutesActionsOnEnter()
    {
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "trigger_tile",
            Nodes = new List<DialogueNode>
            {
                new()
                {
                    Id = "n1",
                    Text = "Something seems amiss about that rock...",
                    Actions = new List<DialogueAction>
                    {
                        new() { Type = "set_flag", Value = "rock_inspected" }
                    }
                }
            }
        };
        var overlay = new InspectOverlay(dialogue, gsm);
        var sm = new ScreenManager();
        sm.Push(overlay);

        Assert.True(gsm.HasFlag("rock_inspected"));
    }

    // =========================================================================
    // InspectOverlay_OneShotSetsFlagOnDismiss
    // =========================================================================

    [Fact]
    public void InspectOverlay_OneShotSetsFlagOnDismiss()
    {
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "book_oneshot",
            OneShot = true,
            Nodes = new List<DialogueNode>
            {
                new() { Id = "n1", Text = "An old tome." }
            }
        };
        var overlay = new InspectOverlay(dialogue, gsm);
        var sm = new ScreenManager();
        sm.Push(overlay);

        Assert.False(gsm.HasFlag("dialogue_shown:book_oneshot"));

        var interact = SimulateKeyPress(Keys.Z);
        overlay.Update(ShortTime, interact);

        Assert.False(sm.HasScreens);
        Assert.True(gsm.HasFlag("dialogue_shown:book_oneshot"));
    }

    // =========================================================================
    // InspectOverlay_ExposesDisplayText
    // =========================================================================

    [Fact]
    public void InspectOverlay_ExposesDisplayText()
    {
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "item_desc",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "n1", Text = "A rusty sword. Seen better days." }
            }
        };
        var overlay = new InspectOverlay(dialogue, gsm);
        var sm = new ScreenManager();
        sm.Push(overlay);

        Assert.Equal("A rusty sword. Seen better days.", overlay.DisplayText);
    }

    // =========================================================================
    // InspectOverlay_MultiNode_AdvancesOnInteract
    // =========================================================================

    [Fact]
    public void InspectOverlay_MultiNode_AdvancesOnInteract()
    {
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "book_pages",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "p1", Text = "Page one of the ancient tome.", NextNodeId = "p2" },
                new() { Id = "p2", Text = "Page two: the secret is revealed." },
            }
        };
        var overlay = new InspectOverlay(dialogue, gsm);
        var sm = new ScreenManager();
        sm.Push(overlay);

        // Starts on page 1
        Assert.Equal("Page one of the ancient tome.", overlay.DisplayText);

        // First Interact: advances to page 2
        var interact = SimulateKeyPress(Keys.Z);
        overlay.Update(ShortTime, interact);

        Assert.True(sm.HasScreens);
        Assert.Equal("Page two: the secret is revealed.", overlay.DisplayText);

        // Second Interact: dismisses (no NextNodeId on p2)
        interact = SimulateKeyPress(Keys.Z);
        overlay.Update(ShortTime, interact);

        Assert.False(sm.HasScreens);
    }
}
