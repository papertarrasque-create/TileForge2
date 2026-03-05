using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game.Screens;

public class DialogueScreenTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    private static GameStateManager CreateGameStateManager()
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

    private static readonly GameTime ShortTime =
        new GameTime(TimeSpan.FromSeconds(0), TimeSpan.FromMilliseconds(16));

    // Enough time for typewriter to fully reveal a short string
    private static readonly GameTime LongTime =
        new GameTime(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

    private static DialogueData MakeLinearDialogue(string text1, string text2, string text3 = null)
    {
        var nodes = new List<DialogueNode>
        {
            new() { Id = "n1", Speaker = "NPC", Text = text1, NextNodeId = "n2" },
            new() { Id = "n2", Speaker = "NPC", Text = text2, NextNodeId = text3 != null ? "n3" : null },
        };
        if (text3 != null)
            nodes.Add(new DialogueNode { Id = "n3", Speaker = "NPC", Text = text3 });
        return new DialogueData { Id = "test", Nodes = nodes };
    }

    private static DialogueData MakeBranchingDialogue()
    {
        return new DialogueData
        {
            Id = "branch_test",
            Nodes = new List<DialogueNode>
            {
                new()
                {
                    Id = "q1",
                    Speaker = "Elder",
                    Text = "Will you help?",
                    Choices = new List<DialogueChoice>
                    {
                        new() { Text = "Yes", NextNodeId = "yes_node", SetsFlag = "accepted_quest" },
                        new() { Text = "No", NextNodeId = "no_node" },
                    }
                },
                new() { Id = "yes_node", Speaker = "Elder", Text = "Thank you!" },
                new() { Id = "no_node", Speaker = "Elder", Text = "Come back anytime." },
            }
        };
    }

    /// <summary>
    /// Advances typewriter past full reveal and then presses a key.
    /// We use LongTime to fully reveal text, then SimulateKeyPress.
    /// </summary>
    private static void AdvancePastReveal(DialogueScreen screen, GameInputManager dummyInput)
    {
        // Tick with long enough time to reveal all text
        screen.Update(LongTime, dummyInput);
    }

    // =========================================================================
    // Construction & overlay
    // =========================================================================

    [Fact]
    public void IsOverlay_ReturnsTrue()
    {
        var dialogue = MakeLinearDialogue("Hello", "Bye");
        var gsm = CreateGameStateManager();
        var screen = new DialogueScreen(dialogue, gsm);
        Assert.True(screen.IsOverlay);
    }

    // =========================================================================
    // Linear dialogue progression
    // =========================================================================

    [Fact]
    public void LinearDialogue_StartsAtFirstNode()
    {
        var dialogue = MakeLinearDialogue("Hello", "Bye");
        var gsm = CreateGameStateManager();
        var screen = new DialogueScreen(dialogue, gsm);
        var manager = new ScreenManager();
        manager.Push(screen);

        Assert.Equal("n1", screen.CurrentNodeId);
    }

    [Fact]
    public void LinearDialogue_InteractAdvancesToNextNode()
    {
        var dialogue = MakeLinearDialogue("Hello", "Bye");
        var gsm = CreateGameStateManager();
        var screen = new DialogueScreen(dialogue, gsm);
        var manager = new ScreenManager();
        manager.Push(screen);

        // Use a no-key input for non-interaction frames
        var noInput = new GameInputManager();
        noInput.Update(new KeyboardState());

        // Advance past typewriter reveal
        AdvancePastReveal(screen, noInput);

        // Now press Interact to advance
        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(ShortTime, interact);

        Assert.Equal("n2", screen.CurrentNodeId);
    }

    [Fact]
    public void LinearDialogue_EndsAndPopsAfterLastNode()
    {
        var dialogue = MakeLinearDialogue("Hello", "Bye");
        var gsm = CreateGameStateManager();
        var screen = new DialogueScreen(dialogue, gsm);
        var manager = new ScreenManager();
        manager.Push(screen);

        var noInput = new GameInputManager();
        noInput.Update(new KeyboardState());

        // Advance through n1
        AdvancePastReveal(screen, noInput);
        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(ShortTime, interact);

        // Now at n2 — advance through n2
        AdvancePastReveal(screen, noInput);
        interact = SimulateKeyPress(Keys.Z);
        screen.Update(ShortTime, interact);

        // n2.NextNodeId is null → dialogue ends
        // Next update should pop
        screen.Update(ShortTime, noInput);

        Assert.False(manager.HasScreens);
    }

    // =========================================================================
    // Typewriter — Interact during reveal skips to full text
    // =========================================================================

    [Fact]
    public void TypewriterReveal_InteractSkipsToFullText()
    {
        var dialogue = MakeLinearDialogue("Hello world this is a long message", "Done");
        var gsm = CreateGameStateManager();
        var screen = new DialogueScreen(dialogue, gsm);
        var manager = new ScreenManager();
        manager.Push(screen);

        // Short time — text not fully revealed
        var noInput = new GameInputManager();
        noInput.Update(new KeyboardState());
        screen.Update(ShortTime, noInput);

        // Interact during reveal → skip to full text
        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(ShortTime, interact);

        // Still on n1 (didn't advance, just revealed full text)
        Assert.Equal("n1", screen.CurrentNodeId);

        // Now Interact again should advance
        interact = SimulateKeyPress(Keys.Z);
        screen.Update(ShortTime, interact);

        Assert.Equal("n2", screen.CurrentNodeId);
    }

    // =========================================================================
    // Branching dialogue
    // =========================================================================

    [Fact]
    public void BranchingDialogue_ShowsChoices()
    {
        var dialogue = MakeBranchingDialogue();
        var gsm = CreateGameStateManager();
        var screen = new DialogueScreen(dialogue, gsm);
        var manager = new ScreenManager();
        manager.Push(screen);

        // At q1 "Will you help?" with two choices
        Assert.Equal("q1", screen.CurrentNodeId);
    }

    [Fact]
    public void BranchingDialogue_SelectFirstChoice_GoesToYesNode()
    {
        var dialogue = MakeBranchingDialogue();
        var gsm = CreateGameStateManager();
        var screen = new DialogueScreen(dialogue, gsm);
        var manager = new ScreenManager();
        manager.Push(screen);

        var noInput = new GameInputManager();
        noInput.Update(new KeyboardState());

        // Reveal full text
        AdvancePastReveal(screen, noInput);

        // Default selection is 0 ("Yes"), press Interact
        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(ShortTime, interact);

        Assert.Equal("yes_node", screen.CurrentNodeId);
    }

    [Fact]
    public void BranchingDialogue_SelectFirstChoice_SetsFlag()
    {
        var dialogue = MakeBranchingDialogue();
        var gsm = CreateGameStateManager();
        var screen = new DialogueScreen(dialogue, gsm);
        var manager = new ScreenManager();
        manager.Push(screen);

        var noInput = new GameInputManager();
        noInput.Update(new KeyboardState());

        AdvancePastReveal(screen, noInput);

        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(ShortTime, interact);

        Assert.True(gsm.HasFlag("accepted_quest"));
    }

    [Fact]
    public void BranchingDialogue_SelectSecondChoice_GoesToNoNode()
    {
        var dialogue = MakeBranchingDialogue();
        var gsm = CreateGameStateManager();
        var screen = new DialogueScreen(dialogue, gsm);
        var manager = new ScreenManager();
        manager.Push(screen);

        var noInput = new GameInputManager();
        noInput.Update(new KeyboardState());

        AdvancePastReveal(screen, noInput);

        // Navigate down to "No" (index 1)
        var down = SimulateKeyPress(Keys.Down);
        screen.Update(ShortTime, down);

        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(ShortTime, interact);

        Assert.Equal("no_node", screen.CurrentNodeId);
        Assert.False(gsm.HasFlag("accepted_quest")); // "No" doesn't set the flag
    }

    // =========================================================================
    // Flag-setting node
    // =========================================================================

    [Fact]
    public void Node_WithSetsFlag_SetsFlagOnEntry()
    {
        var dialogue = new DialogueData
        {
            Id = "flag_test",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "n1", Text = "Hello", SetsFlag = "spoke_to_elder", NextNodeId = "n2" },
                new() { Id = "n2", Text = "Bye" },
            }
        };
        var gsm = CreateGameStateManager();
        var screen = new DialogueScreen(dialogue, gsm);
        var manager = new ScreenManager();
        manager.Push(screen);

        // Flag should be set immediately when node is entered
        Assert.True(gsm.HasFlag("spoke_to_elder"));
    }

    // =========================================================================
    // Variable-setting node
    // =========================================================================

    [Fact]
    public void Node_WithSetsVariable_SetsVariableOnEntry()
    {
        var dialogue = new DialogueData
        {
            Id = "var_test",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "n1", Text = "Quest begun", SetsVariable = "quest_stage=2" },
            }
        };
        var gsm = CreateGameStateManager();
        var screen = new DialogueScreen(dialogue, gsm);
        var manager = new ScreenManager();
        manager.Push(screen);

        Assert.Equal("2", gsm.GetVariable("quest_stage"));
    }

    // =========================================================================
    // Conditional node (RequiresFlag)
    // =========================================================================

    [Fact]
    public void ConditionalNode_FlagNotSet_SkipsNode()
    {
        var dialogue = new DialogueData
        {
            Id = "cond_test",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "n1", Text = "Start", NextNodeId = "n2" },
                new() { Id = "n2", Text = "Secret!", RequiresFlag = "secret_flag", NextNodeId = "n3" },
                new() { Id = "n3", Text = "End" },
            }
        };
        var gsm = CreateGameStateManager();
        var screen = new DialogueScreen(dialogue, gsm);
        var manager = new ScreenManager();
        manager.Push(screen);

        var noInput = new GameInputManager();
        noInput.Update(new KeyboardState());

        // At n1
        Assert.Equal("n1", screen.CurrentNodeId);

        // Advance to n2 — but n2 requires "secret_flag" which isn't set
        AdvancePastReveal(screen, noInput);
        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(ShortTime, interact);

        // Should skip n2 and go to n3
        Assert.Equal("n3", screen.CurrentNodeId);
    }

    [Fact]
    public void ConditionalNode_FlagSet_ShowsNode()
    {
        var dialogue = new DialogueData
        {
            Id = "cond_test",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "n1", Text = "Start", NextNodeId = "n2" },
                new() { Id = "n2", Text = "Secret!", RequiresFlag = "secret_flag", NextNodeId = "n3" },
                new() { Id = "n3", Text = "End" },
            }
        };
        var gsm = CreateGameStateManager();
        gsm.SetFlag("secret_flag"); // flag IS set
        var screen = new DialogueScreen(dialogue, gsm);
        var manager = new ScreenManager();
        manager.Push(screen);

        var noInput = new GameInputManager();
        noInput.Update(new KeyboardState());

        AdvancePastReveal(screen, noInput);
        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(ShortTime, interact);

        // Should show n2 (flag requirement met)
        Assert.Equal("n2", screen.CurrentNodeId);
    }

    // =========================================================================
    // Conditional choices (RequiresFlag on choice)
    // =========================================================================

    [Fact]
    public void ConditionalChoice_FlagNotSet_ChoiceHidden()
    {
        var dialogue = new DialogueData
        {
            Id = "choice_cond",
            Nodes = new List<DialogueNode>
            {
                new()
                {
                    Id = "q1", Text = "Options?",
                    Choices = new List<DialogueChoice>
                    {
                        new() { Text = "Always visible", NextNodeId = "a" },
                        new() { Text = "Secret option", NextNodeId = "b", RequiresFlag = "secret_flag" },
                    }
                },
                new() { Id = "a", Text = "You chose A" },
                new() { Id = "b", Text = "You chose B" },
            }
        };
        var gsm = CreateGameStateManager();
        // secret_flag NOT set → only "Always visible" should appear
        var screen = new DialogueScreen(dialogue, gsm);
        var manager = new ScreenManager();
        manager.Push(screen);

        var noInput = new GameInputManager();
        noInput.Update(new KeyboardState());
        AdvancePastReveal(screen, noInput);

        // Select the only visible choice (index 0)
        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(ShortTime, interact);

        Assert.Equal("a", screen.CurrentNodeId);
    }

    [Fact]
    public void ConditionalChoice_FlagSet_ChoiceVisible()
    {
        var dialogue = new DialogueData
        {
            Id = "choice_cond",
            Nodes = new List<DialogueNode>
            {
                new()
                {
                    Id = "q1", Text = "Options?",
                    Choices = new List<DialogueChoice>
                    {
                        new() { Text = "Always visible", NextNodeId = "a" },
                        new() { Text = "Secret option", NextNodeId = "b", RequiresFlag = "secret_flag" },
                    }
                },
                new() { Id = "a", Text = "You chose A" },
                new() { Id = "b", Text = "You chose B" },
            }
        };
        var gsm = CreateGameStateManager();
        gsm.SetFlag("secret_flag"); // flag IS set
        var screen = new DialogueScreen(dialogue, gsm);
        var manager = new ScreenManager();
        manager.Push(screen);

        var noInput = new GameInputManager();
        noInput.Update(new KeyboardState());
        AdvancePastReveal(screen, noInput);

        // Navigate to second choice ("Secret option")
        var down = SimulateKeyPress(Keys.Down);
        screen.Update(ShortTime, down);

        var interact = SimulateKeyPress(Keys.Z);
        screen.Update(ShortTime, interact);

        Assert.Equal("b", screen.CurrentNodeId);
    }

    // =========================================================================
    // Cancel exits dialogue
    // =========================================================================

    [Fact]
    public void Cancel_PopsDialogueScreen()
    {
        var dialogue = MakeLinearDialogue("Hello", "Bye");
        var gsm = CreateGameStateManager();
        var screen = new DialogueScreen(dialogue, gsm);
        var manager = new ScreenManager();
        manager.Push(screen);

        var cancel = SimulateKeyPress(Keys.X);
        screen.Update(ShortTime, cancel);

        Assert.False(manager.HasScreens);
    }
}
