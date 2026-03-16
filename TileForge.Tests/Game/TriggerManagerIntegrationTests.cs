using System.Collections.Generic;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game;

public class TriggerManagerIntegrationTests
{
    private static GameStateManager CreateGSM()
    {
        var gsm = new GameStateManager();
        gsm.State.Player = new PlayerState();
        return gsm;
    }

    private static Dictionary<string, DialogueData> MakeDialogues(params DialogueData[] dialogues)
    {
        var dict = new Dictionary<string, DialogueData>();
        foreach (var d in dialogues) dict[d.Id] = d;
        return dict;
    }

    [Fact]
    public void TriggerManager_Fire_ThenFactory_Conversation()
    {
        var tm = new TriggerManager();
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "elder_01",
            Type = "conversation",
            Nodes = new() { new DialogueNode { Id = "start", Text = "Hello" } },
            Routes = new() { new DialogueRoute { StartNode = "start" } },
        };
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            EntityId = "npc_elder",
            Properties = new() { ["dialogue_id"] = "elder_01" },
        };

        var result = tm.Fire(evt, gsm, MakeDialogues(dialogue));
        Assert.NotNull(result);

        var screenResult = DialogueScreenFactory.Create(result.Dialogue, gsm);
        Assert.NotNull(screenResult.Screen);
        Assert.IsType<DialogueScreen>(screenResult.Screen);
        Assert.Null(screenResult.BarkOverlay);
    }

    [Fact]
    public void TriggerManager_Fire_ThenFactory_Bark()
    {
        var tm = new TriggerManager();
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "bark_01",
            Type = "bark",
            Nodes = new() { new DialogueNode { Id = "b1", Text = "Watch out!" } },
            Routes = new() { new DialogueRoute { StartNode = "b1" } },
        };
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            EntityId = "guard_1",
            Properties = new() { ["dialogue_id"] = "bark_01" },
        };

        var result = tm.Fire(evt, gsm, MakeDialogues(dialogue));
        Assert.NotNull(result);

        var screenResult = DialogueScreenFactory.Create(result.Dialogue, gsm);
        Assert.NotNull(screenResult.BarkOverlay);
        Assert.Null(screenResult.Screen);
    }

    [Fact]
    public void TriggerManager_Fire_ThenFactory_Inspect()
    {
        var tm = new TriggerManager();
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "inspect_01",
            Type = "inspect",
            Nodes = new() { new DialogueNode { Id = "i1", Text = "An old stone tablet." } },
            Routes = new() { new DialogueRoute { StartNode = "i1" } },
        };
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            EntityId = "tablet_1",
            Properties = new() { ["dialogue_id"] = "inspect_01" },
        };

        var result = tm.Fire(evt, gsm, MakeDialogues(dialogue));
        Assert.NotNull(result);

        var screenResult = DialogueScreenFactory.Create(result.Dialogue, gsm);
        Assert.NotNull(screenResult.Screen);
        Assert.IsType<InspectOverlay>(screenResult.Screen);
    }

    [Fact]
    public void TriggerManager_Fire_InlineDialogue_WorksWithFactory()
    {
        var tm = new TriggerManager();
        var gsm = CreateGSM();
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            EntityId = "sign_1",
            Properties = new() { ["dialogue"] = "Welcome to town|Enjoy your stay" },
        };

        var result = tm.Fire(evt, gsm, new Dictionary<string, DialogueData>());
        Assert.NotNull(result);
        Assert.Equal(2, result.Dialogue.Nodes.Count);

        var screenResult = DialogueScreenFactory.Create(result.Dialogue, gsm);
        // Inline dialogue has no Type set, defaults to "conversation"
        Assert.NotNull(screenResult.Screen);
        Assert.IsType<DialogueScreen>(screenResult.Screen);
    }

    [Fact]
    public void TriggerManager_Fire_OneShotBlocks()
    {
        var tm = new TriggerManager();
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "oneshot_01",
            OneShot = true,
            Nodes = new() { new DialogueNode { Id = "s1", Text = "First time only" } },
            Routes = new() { new DialogueRoute { StartNode = "s1" } },
        };
        var dialogues = MakeDialogues(dialogue);
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            EntityId = "npc_1",
            Properties = new() { ["dialogue_id"] = "oneshot_01" },
        };

        // First fire succeeds
        var result1 = tm.Fire(evt, gsm, dialogues);
        Assert.NotNull(result1);

        // Simulate oneShot flag being set after dialogue completes
        gsm.SetFlag("dialogue_shown:oneshot_01");

        // Second fire blocked
        var result2 = tm.Fire(evt, gsm, dialogues);
        Assert.Null(result2);
    }

    [Fact]
    public void TriggerManager_Fire_PickupSource()
    {
        var tm = new TriggerManager();
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "pickup_msg",
            Nodes = new() { new DialogueNode { Id = "p1", Text = "You found a key!" } },
            Routes = new() { new DialogueRoute { StartNode = "p1" } },
        };
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Pickup,
            EntityId = "key_1",
            Properties = new() { ["dialogue_id"] = "pickup_msg" },
        };

        var result = tm.Fire(evt, gsm, MakeDialogues(dialogue));
        Assert.NotNull(result);
        Assert.Equal("pickup_msg", result.Dialogue.Id);
        Assert.Equal("p1", result.StartNodeId);
    }
}
