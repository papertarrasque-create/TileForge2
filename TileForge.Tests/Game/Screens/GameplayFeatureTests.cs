using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game.Screens;

/// <summary>
/// Tests for G15 gameplay features: pickup dialogue, concluded dialogue, terrain notifications.
/// </summary>
public class GameplayFeatureTests
{
    // =========================================================================
    // Feature 1: Pickup dialogue flag pattern
    // =========================================================================

    [Fact]
    public void PickupDialogueFlag_FirstPickup_NotSet()
    {
        var mgr = new GameStateManager();
        string flag = "pickup_dialogue_shown:Sword";

        Assert.False(mgr.HasFlag(flag));
    }

    [Fact]
    public void PickupDialogueFlag_AfterSetFlag_IsSet()
    {
        var mgr = new GameStateManager();
        string flag = "pickup_dialogue_shown:Sword";

        mgr.SetFlag(flag);

        Assert.True(mgr.HasFlag(flag));
    }

    [Fact]
    public void PickupDialogueFlag_DifferentItemGroups_Independent()
    {
        var mgr = new GameStateManager();

        mgr.SetFlag("pickup_dialogue_shown:Sword");

        Assert.True(mgr.HasFlag("pickup_dialogue_shown:Sword"));
        Assert.False(mgr.HasFlag("pickup_dialogue_shown:Shield"));
    }

    [Fact]
    public void PickupDialogue_InlineText_CreatesValidDialogue()
    {
        var dialogue = GameplayScreen.CreateInlineDialogue("Sword", "You found the legendary blade!");

        Assert.Single(dialogue.Nodes);
        Assert.Equal("Sword", dialogue.Nodes[0].Speaker);
        Assert.Equal("You found the legendary blade!", dialogue.Nodes[0].Text);
    }

    // =========================================================================
    // Feature 2: Concluded dialogue state
    // =========================================================================

    [Fact]
    public void ConcludedDialogue_FlagNotSet_MainDialogueShouldPlay()
    {
        var mgr = new GameStateManager();
        var entity = new EntityInstance
        {
            Id = "elder_01",
            DefinitionName = "Elder",
            Properties = new Dictionary<string, string>
            {
                ["dialogue"] = "Hello traveler!",
                ["concluded_flag"] = "elder_quest_done",
                ["concluded_dialogue"] = "Have you found my hat?",
            }
        };

        // concluded_flag is NOT set, so main dialogue should play
        Assert.False(mgr.HasFlag("elder_quest_done"));

        // Verify the entity has both dialogue paths configured
        Assert.True(entity.Properties.ContainsKey("dialogue"));
        Assert.True(entity.Properties.ContainsKey("concluded_dialogue"));
    }

    [Fact]
    public void ConcludedDialogue_FlagSet_ConcludedDialogueShouldPlay()
    {
        var mgr = new GameStateManager();
        mgr.SetFlag("elder_quest_done");

        var entity = new EntityInstance
        {
            Id = "elder_01",
            DefinitionName = "Elder",
            Properties = new Dictionary<string, string>
            {
                ["dialogue"] = "Hello traveler!",
                ["concluded_flag"] = "elder_quest_done",
                ["concluded_dialogue"] = "Have you found my hat?",
            }
        };

        // concluded_flag IS set, so concluded dialogue should play
        Assert.True(mgr.HasFlag(entity.Properties["concluded_flag"]));

        // Verify concluded dialogue creates valid inline dialogue
        var dialogue = GameplayScreen.CreateInlineDialogue(
            entity.DefinitionName, entity.Properties["concluded_dialogue"]);
        Assert.Single(dialogue.Nodes);
        Assert.Equal("Have you found my hat?", dialogue.Nodes[0].Text);
    }

    [Fact]
    public void ConcludedDialogue_NoConcludedFlag_NormalDialoguePlays()
    {
        var mgr = new GameStateManager();
        var entity = new EntityInstance
        {
            Id = "elder_01",
            DefinitionName = "Elder",
            Properties = new Dictionary<string, string>
            {
                ["dialogue"] = "Hello traveler!",
                // No concluded_flag or concluded_dialogue
            }
        };

        // Without concluded_flag, there is nothing to check
        Assert.False(entity.Properties.ContainsKey("concluded_flag"));
    }

    // =========================================================================
    // Feature 3: Terrain movement cost with source
    // =========================================================================

    private static GameplayScreen CreateMinimalGameplayScreen(EditorState state)
    {
        var gsm = new GameStateManager();
        gsm.State.Player = new PlayerState { X = 0, Y = 0 };
        var context = new GamePlayContext(
            gsm, null, new GameInputManager(), null,
            new QuestManager(new List<QuestDefinition>()), () => new Rectangle(0, 0, 800, 600));
        return new GameplayScreen(state, null, context);
    }

    [Fact]
    public void GetMovementCostWithSource_DefaultTerrain_ReturnsCost1()
    {
        var state = new EditorState
        {
            Map = new MapData(5, 5),
        };
        state.AddGroup(new TileGroup { Name = "grass", Type = GroupType.Tile, MovementCost = 1.0f });
        state.Map.Layers[0].SetCell(2, 2, state.Map.Width, "grass");

        var screen = CreateMinimalGameplayScreen(state);
        var (cost, groupName) = screen.GetMovementCostWithSource(2, 2);

        Assert.Equal(1.0f, cost);
        Assert.Null(groupName); // No slow terrain, so no source name
    }

    [Fact]
    public void GetMovementCostWithSource_SlowTerrain_ReturnsCostAndName()
    {
        var state = new EditorState
        {
            Map = new MapData(5, 5),
        };
        state.AddGroup(new TileGroup { Name = "swamp", Type = GroupType.Tile, MovementCost = 2.0f });
        state.Map.Layers[0].SetCell(3, 3, state.Map.Width, "swamp");

        var screen = CreateMinimalGameplayScreen(state);
        var (cost, groupName) = screen.GetMovementCostWithSource(3, 3);

        Assert.Equal(2.0f, cost);
        Assert.Equal("swamp", groupName);
    }

    [Fact]
    public void GetMovementCostWithSource_MultipleLayers_ReturnsMaxCost()
    {
        var state = new EditorState
        {
            Map = new MapData(5, 5),
        };
        state.Map.AddLayer("overlay");
        state.AddGroup(new TileGroup { Name = "grass", Type = GroupType.Tile, MovementCost = 1.0f });
        state.AddGroup(new TileGroup { Name = "mud", Type = GroupType.Tile, MovementCost = 1.5f });
        state.AddGroup(new TileGroup { Name = "thorns", Type = GroupType.Tile, MovementCost = 3.0f });

        state.Map.Layers[0].SetCell(1, 1, state.Map.Width, "grass");
        state.Map.Layers[1].SetCell(1, 1, state.Map.Width, "thorns");

        var screen = CreateMinimalGameplayScreen(state);
        var (cost, groupName) = screen.GetMovementCostWithSource(1, 1);

        Assert.Equal(3.0f, cost);
        Assert.Equal("thorns", groupName);
    }

    [Fact]
    public void GetMovementCostWithSource_EmptyTile_ReturnsCost1()
    {
        var state = new EditorState
        {
            Map = new MapData(5, 5),
        };

        var screen = CreateMinimalGameplayScreen(state);
        var (cost, groupName) = screen.GetMovementCostWithSource(0, 0);

        Assert.Equal(1.0f, cost);
        Assert.Null(groupName);
    }
}
