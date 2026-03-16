using System.Collections.Generic;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class TriggerManagerTests
{
    private static GameStateManager CreateGSM()
    {
        var gsm = new GameStateManager();
        gsm.State.Player = new PlayerState
        {
            Health = 100, MaxHealth = 100,
            Poise = 20, MaxPoise = 20, MaxAP = 2,
        };
        return gsm;
    }

    private static Dictionary<string, DialogueData> MakeDialogues(params DialogueData[] dialogues)
    {
        var dict = new Dictionary<string, DialogueData>();
        foreach (var d in dialogues) dict[d.Id] = d;
        return dict;
    }

    private static DialogueData SimpleDialogue(string id, string startNodeId = "start")
    {
        return new DialogueData
        {
            Id = id,
            Nodes = new() { new DialogueNode { Id = startNodeId, Text = "Hello" } },
            Routes = new() { new DialogueRoute { StartNode = startNodeId } },
        };
    }

    [Fact]
    public void Fire_NoDialogueId_ReturnsNull()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["behavior"] = "idle" },
        };
        Assert.Null(manager.Fire(evt, gsm, new Dictionary<string, DialogueData>()));
    }

    [Fact]
    public void Fire_WithDialogueId_ReturnsDialogueAndStartNode()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        var dialogue = SimpleDialogue("elder_01");
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "elder_01" },
        };
        var result = manager.Fire(evt, gsm, MakeDialogues(dialogue));
        Assert.NotNull(result);
        Assert.Equal("elder_01", result.Dialogue.Id);
        Assert.Equal("start", result.StartNodeId);
    }

    [Fact]
    public void Fire_OneShotAlreadyShown_ReturnsNull()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        gsm.SetFlag("dialogue_shown:greeting");
        var dialogue = SimpleDialogue("greeting");
        dialogue.OneShot = true;
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "greeting" },
        };
        Assert.Null(manager.Fire(evt, gsm, MakeDialogues(dialogue)));
    }

    [Fact]
    public void Fire_OneShotNotYetShown_ReturnsDialogue()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        var dialogue = SimpleDialogue("greeting");
        dialogue.OneShot = true;
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "greeting" },
        };
        Assert.NotNull(manager.Fire(evt, gsm, MakeDialogues(dialogue)));
    }

    [Fact]
    public void Fire_RoutesEvaluated_FirstMatchWins()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        gsm.SetFlag("quest_complete");
        var dialogue = new DialogueData
        {
            Id = "elder_01",
            Routes = new()
            {
                new DialogueRoute
                {
                    StartNode = "thanks",
                    Conditions = new() { new Condition { Type = "has_flag", Flag = "quest_complete" } }
                },
                new DialogueRoute { StartNode = "start" },
            },
            Nodes = new()
            {
                new DialogueNode { Id = "thanks", Text = "Thank you!" },
                new DialogueNode { Id = "start", Text = "Hello" },
            },
        };
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "elder_01" },
        };
        Assert.Equal("thanks", manager.Fire(evt, gsm, MakeDialogues(dialogue)).StartNodeId);
    }

    [Fact]
    public void Fire_RoutesEvaluated_FallsToDefault()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "elder_01",
            Routes = new()
            {
                new DialogueRoute
                {
                    StartNode = "thanks",
                    Conditions = new() { new Condition { Type = "has_flag", Flag = "quest_complete" } }
                },
                new DialogueRoute { StartNode = "start" },
            },
            Nodes = new()
            {
                new DialogueNode { Id = "thanks", Text = "Thank you!" },
                new DialogueNode { Id = "start", Text = "Hello" },
            },
        };
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "elder_01" },
        };
        Assert.Equal("start", manager.Fire(evt, gsm, MakeDialogues(dialogue)).StartNodeId);
    }

    [Fact]
    public void Fire_InlineDialogue_CreatesFromText()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            EntityId = "npc_1",
            Properties = new() { ["dialogue"] = "Hello there|Welcome to town" },
        };
        var result = manager.Fire(evt, gsm, new Dictionary<string, DialogueData>());
        Assert.NotNull(result);
        Assert.Equal(2, result.Dialogue.Nodes.Count);
        Assert.Equal("Hello there", result.Dialogue.Nodes[0].Text);
        Assert.Equal("Welcome to town", result.Dialogue.Nodes[1].Text);
    }

    [Fact]
    public void Fire_DialogueIdNotFound_ReturnsNull()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "nonexistent" },
        };
        Assert.Null(manager.Fire(evt, gsm, new Dictionary<string, DialogueData>()));
    }

    [Fact]
    public void Fire_DialogueIdTakesPrecedenceOverInline()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        var dialogue = SimpleDialogue("elder_01");
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "elder_01", ["dialogue"] = "fallback" },
        };
        Assert.Equal("elder_01", manager.Fire(evt, gsm, MakeDialogues(dialogue)).Dialogue.Id);
    }

    [Fact]
    public void Fire_NoRoutesOnDialogue_DefaultsToFirstNode()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "simple",
            Nodes = new()
            {
                new DialogueNode { Id = "node1", Text = "Hi" },
                new DialogueNode { Id = "node2", Text = "Bye" },
            },
        };
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "simple" },
        };
        var result = manager.Fire(evt, gsm, MakeDialogues(dialogue));
        Assert.NotNull(result);
        Assert.Equal("node1", result.StartNodeId);
    }
}
