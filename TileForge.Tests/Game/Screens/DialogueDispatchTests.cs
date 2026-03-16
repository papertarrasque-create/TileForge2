using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game.Screens;

public class DialogueDispatchTests
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

    private static DialogueData MakeDialogue(string type)
    {
        return new DialogueData
        {
            Id = "test_dispatch",
            Type = type,
            Nodes = new List<DialogueNode>
            {
                new() { Id = "n1", Speaker = "Test", Text = "Hello" }
            }
        };
    }

    // =========================================================================
    // Create_Conversation_ReturnsDialogueScreen
    // =========================================================================

    [Fact]
    public void Create_Conversation_ReturnsDialogueScreen()
    {
        var gsm = CreateGSM();
        var dialogue = MakeDialogue("conversation");

        var result = DialogueScreenFactory.Create(dialogue, gsm);

        Assert.NotNull(result.Screen);
        Assert.IsType<DialogueScreen>(result.Screen);
        Assert.Null(result.BarkOverlay);
    }

    // =========================================================================
    // Create_Bark_ReturnsBarkOverlay
    // =========================================================================

    [Fact]
    public void Create_Bark_ReturnsBarkOverlay()
    {
        var gsm = CreateGSM();
        var dialogue = MakeDialogue("bark");

        var result = DialogueScreenFactory.Create(dialogue, gsm);

        Assert.NotNull(result.BarkOverlay);
        Assert.Null(result.Screen);
    }

    // =========================================================================
    // Create_Inspect_ReturnsInspectOverlay
    // =========================================================================

    [Fact]
    public void Create_Inspect_ReturnsInspectOverlay()
    {
        var gsm = CreateGSM();
        var dialogue = MakeDialogue("inspect");

        var result = DialogueScreenFactory.Create(dialogue, gsm);

        Assert.NotNull(result.Screen);
        Assert.IsType<InspectOverlay>(result.Screen);
    }

    // =========================================================================
    // Create_Cutscene_ReturnsCutsceneScreen
    // =========================================================================

    [Fact]
    public void Create_Cutscene_ReturnsCutsceneScreen()
    {
        var gsm = CreateGSM();
        var dialogue = MakeDialogue("cutscene");

        var result = DialogueScreenFactory.Create(dialogue, gsm);

        Assert.NotNull(result.Screen);
        Assert.IsType<CutsceneScreen>(result.Screen);
    }

    // =========================================================================
    // Create_NullType_DefaultsToDialogueScreen
    // =========================================================================

    [Fact]
    public void Create_NullType_DefaultsToDialogueScreen()
    {
        var gsm = CreateGSM();
        var dialogue = MakeDialogue(null);

        var result = DialogueScreenFactory.Create(dialogue, gsm);

        Assert.NotNull(result.Screen);
        Assert.IsType<DialogueScreen>(result.Screen);
        Assert.Null(result.BarkOverlay);
    }

    // =========================================================================
    // Create_UnknownType_DefaultsToDialogueScreen
    // =========================================================================

    [Fact]
    public void Create_UnknownType_DefaultsToDialogueScreen()
    {
        var gsm = CreateGSM();
        var dialogue = MakeDialogue("unknown_type");

        var result = DialogueScreenFactory.Create(dialogue, gsm);

        Assert.NotNull(result.Screen);
        Assert.IsType<DialogueScreen>(result.Screen);
        Assert.Null(result.BarkOverlay);
    }
}
