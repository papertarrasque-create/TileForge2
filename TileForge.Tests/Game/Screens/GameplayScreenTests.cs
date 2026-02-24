using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game.Screens;

public class GameplayScreenTests
{
    [Fact]
    public void CreateInlineDialogue_SinglePage_ProducesOneNode()
    {
        var result = GameplayScreen.CreateInlineDialogue("Frog", "I am a frog. Ribbit");

        Assert.Single(result.Nodes);
        Assert.Equal("I am a frog. Ribbit", result.Nodes[0].Text);
        Assert.Equal("Frog", result.Nodes[0].Speaker);
        Assert.Null(result.Nodes[0].NextNodeId);
    }

    [Fact]
    public void CreateInlineDialogue_SinglePage_IdContainsEntityName()
    {
        var result = GameplayScreen.CreateInlineDialogue("Elder", "Hello");

        Assert.Equal("inline_Elder", result.Id);
    }

    [Fact]
    public void CreateInlineDialogue_TwoPages_ProducesTwoLinkedNodes()
    {
        var result = GameplayScreen.CreateInlineDialogue("Sign", "Hello|World");

        Assert.Equal(2, result.Nodes.Count);
        Assert.Equal("page_0", result.Nodes[0].Id);
        Assert.Equal("page_1", result.Nodes[0].NextNodeId);
        Assert.Equal("page_1", result.Nodes[1].Id);
        Assert.Null(result.Nodes[1].NextNodeId);
    }

    [Fact]
    public void CreateInlineDialogue_ThreePages_ChainsAllNodes()
    {
        var result = GameplayScreen.CreateInlineDialogue("NPC", "A|B|C");

        Assert.Equal(3, result.Nodes.Count);
        Assert.Equal("page_1", result.Nodes[0].NextNodeId);
        Assert.Equal("page_2", result.Nodes[1].NextNodeId);
        Assert.Null(result.Nodes[2].NextNodeId);
    }

    [Fact]
    public void CreateInlineDialogue_TrimsWhitespace()
    {
        var result = GameplayScreen.CreateInlineDialogue("NPC", "Hello  |  World  ");

        Assert.Equal("Hello", result.Nodes[0].Text);
        Assert.Equal("World", result.Nodes[1].Text);
    }

    [Fact]
    public void CreateInlineDialogue_AllNodesSpeakerMatchesEntityName()
    {
        var result = GameplayScreen.CreateInlineDialogue("Frog", "Ribbit|Croak|Splash");

        Assert.All(result.Nodes, n => Assert.Equal("Frog", n.Speaker));
    }

    [Fact]
    public void CreateInlineDialogue_CompatibleWithDialogueScreen()
    {
        var result = GameplayScreen.CreateInlineDialogue("Elder", "Hello|Farewell");
        var gsm = new GameStateManager();
        gsm.State.Player = new PlayerState();

        var screen = new DialogueScreen(result, gsm);
        var manager = new ScreenManager();
        manager.Push(screen);

        Assert.Equal("page_0", screen.CurrentNodeId);
    }

    [Fact]
    public void CreateInlineDialogue_NoChoicesOnNodes()
    {
        var result = GameplayScreen.CreateInlineDialogue("Sign", "Beware|of lava");

        Assert.All(result.Nodes, n => Assert.Null(n.Choices));
    }
}
