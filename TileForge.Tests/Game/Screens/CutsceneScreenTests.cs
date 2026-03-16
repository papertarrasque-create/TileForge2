using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game.Screens;

public class CutsceneScreenTests
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

    private static GameTime MakeTime(float seconds)
    {
        return new GameTime(TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(seconds));
    }

    private static DialogueData MakeCutscene(string id = "cut_01")
    {
        return new DialogueData
        {
            Id = id,
            Nodes = new List<DialogueNode>
            {
                new() { Id = "n1", Speaker = "Narrator", Text = "Part 1", NextNodeId = "n2" },
                new() { Id = "n2", Speaker = "Narrator", Text = "Part 2" },
            }
        };
    }

    // =========================================================================
    // CutsceneScreen_IsOverlay
    // =========================================================================

    [Fact]
    public void CutsceneScreen_IsOverlay()
    {
        var dialogue = MakeCutscene();
        var gsm = CreateGSM();
        var screen = new CutsceneScreen(dialogue, gsm);
        Assert.True(screen.IsOverlay);
    }

    // =========================================================================
    // CutsceneScreen_AutoAdvancesAfterDelay
    // =========================================================================

    [Fact]
    public void CutsceneScreen_AutoAdvancesAfterDelay()
    {
        var dialogue = MakeCutscene();
        var gsm = CreateGSM();
        var screen = new CutsceneScreen(dialogue, gsm, autoAdvanceSeconds: 1f);
        var manager = new ScreenManager();
        manager.Push(screen);

        var noInput = NoInput();

        // After 0.5s — still on Part 1
        screen.Update(MakeTime(0.5f), noInput);
        Assert.Equal("Part 1", screen.DisplayText);

        // After 1.5s total — should advance to Part 2
        screen.Update(MakeTime(1.0f), noInput);
        Assert.Equal("Part 2", screen.DisplayText);

        // After another 1.5s — should be dismissed
        screen.Update(MakeTime(1.5f), noInput);
        // Next update pops the screen when current node is null
        screen.Update(MakeTime(0.01f), noInput);
        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // CutsceneScreen_ManualAdvanceOnInteract
    // =========================================================================

    [Fact]
    public void CutsceneScreen_ManualAdvanceOnInteract()
    {
        var dialogue = MakeCutscene();
        var gsm = CreateGSM();
        // Long auto-advance (10s) — won't fire during test
        var screen = new CutsceneScreen(dialogue, gsm, autoAdvanceSeconds: 10f);
        var manager = new ScreenManager();
        manager.Push(screen);

        var noInput = NoInput();
        var interact = SimulateKeyPress(Keys.Z); // Interact key

        // Initially on Part 1
        Assert.Equal("Part 1", screen.DisplayText);

        // First E advances to Part 2
        screen.Update(MakeTime(0.1f), interact);
        Assert.Equal("Part 2", screen.DisplayText);

        // Second E dismisses
        screen.Update(MakeTime(0.1f), interact);
        // Next update triggers the pop
        screen.Update(MakeTime(0.01f), noInput);
        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // CutsceneScreen_CancelExitsImmediately
    // =========================================================================

    [Fact]
    public void CutsceneScreen_CancelExitsImmediately()
    {
        var dialogue = new DialogueData
        {
            Id = "cut_cancel",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "n1", Text = "Part 1", NextNodeId = "n2" },
                new() { Id = "n2", Text = "Part 2", NextNodeId = "n3" },
                new() { Id = "n3", Text = "Part 3" },
            }
        };
        var gsm = CreateGSM();
        var screen = new CutsceneScreen(dialogue, gsm, autoAdvanceSeconds: 10f);
        var manager = new ScreenManager();
        manager.Push(screen);

        // Still on Part 1
        Assert.Equal("Part 1", screen.DisplayText);

        // Press Cancel (Escape)
        var cancel = SimulateKeyPress(Keys.X); // Cancel key
        screen.Update(MakeTime(0.1f), cancel);

        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // CutsceneScreen_ExecutesActionsPerNode
    // =========================================================================

    [Fact]
    public void CutsceneScreen_ExecutesActionsPerNode()
    {
        var dialogue = new DialogueData
        {
            Id = "cut_actions",
            Nodes = new List<DialogueNode>
            {
                new()
                {
                    Id = "n1",
                    Text = "Part 1",
                    NextNodeId = "n2",
                    Actions = new List<DialogueAction>
                    {
                        new() { Type = "set_flag", Value = "flag_1" }
                    }
                },
                new()
                {
                    Id = "n2",
                    Text = "Part 2",
                    Actions = new List<DialogueAction>
                    {
                        new() { Type = "set_flag", Value = "flag_2" }
                    }
                },
            }
        };
        var gsm = CreateGSM();
        var screen = new CutsceneScreen(dialogue, gsm, autoAdvanceSeconds: 1f);
        var manager = new ScreenManager();
        manager.Push(screen);

        var noInput = NoInput();

        // flag_1 should be set immediately on entry to n1
        Assert.True(gsm.HasFlag("flag_1"));
        // flag_2 should NOT be set yet
        Assert.False(gsm.HasFlag("flag_2"));

        // Auto-advance past 1s — enters n2, executes flag_2
        screen.Update(MakeTime(1.5f), noInput);
        Assert.True(gsm.HasFlag("flag_2"));
    }

    // =========================================================================
    // CutsceneScreen_OneShotSetsFlagOnEnd
    // =========================================================================

    [Fact]
    public void CutsceneScreen_OneShotSetsFlagOnEnd()
    {
        var dialogue = new DialogueData
        {
            Id = "cut_oneshot",
            OneShot = true,
            Nodes = new List<DialogueNode>
            {
                new() { Id = "n1", Text = "Part 1", NextNodeId = "n2" },
                new() { Id = "n2", Text = "Part 2" },
            }
        };
        var gsm = CreateGSM();
        var screen = new CutsceneScreen(dialogue, gsm, autoAdvanceSeconds: 1f);
        var manager = new ScreenManager();
        manager.Push(screen);

        var noInput = NoInput();

        // Flag should not be set yet
        Assert.False(gsm.HasFlag("dialogue_shown:cut_oneshot"));

        // Advance past n1
        screen.Update(MakeTime(1.5f), noInput);

        // Advance past n2
        screen.Update(MakeTime(1.5f), noInput);

        // Pop happens on next update
        screen.Update(MakeTime(0.01f), noInput);
        Assert.False(manager.HasScreens);

        // OneShot flag should be set
        Assert.True(gsm.HasFlag("dialogue_shown:cut_oneshot"));
    }

    // =========================================================================
    // CutsceneScreen_CustomAutoAdvanceTag
    // =========================================================================

    [Fact]
    public void CutsceneScreen_CustomAutoAdvanceTag()
    {
        var dialogue = new DialogueData
        {
            Id = "cut_tag",
            Nodes = new List<DialogueNode>
            {
                new()
                {
                    Id = "n1",
                    Text = "Quick flash",
                    NextNodeId = "n2",
                    Tags = new List<string> { "auto_advance_ms:500" }
                },
                new()
                {
                    Id = "n2",
                    Text = "Slow atmosphere",
                    Tags = new List<string> { "auto_advance_ms:5000" }
                },
            }
        };
        var gsm = CreateGSM();
        // Default 3s is irrelevant — per-node tags override it
        var screen = new CutsceneScreen(dialogue, gsm, autoAdvanceSeconds: 3f);
        var manager = new ScreenManager();
        manager.Push(screen);

        var noInput = NoInput();

        // Initially on n1 (500ms auto-advance)
        Assert.Equal("Quick flash", screen.DisplayText);

        // After 0.6s — n1 should have advanced (500ms elapsed)
        screen.Update(MakeTime(0.6f), noInput);
        Assert.Equal("Slow atmosphere", screen.DisplayText);

        // After another 1s (1.6s total) — still on n2 (needs 5000ms)
        screen.Update(MakeTime(1.0f), noInput);
        Assert.Equal("Slow atmosphere", screen.DisplayText);
    }
}
