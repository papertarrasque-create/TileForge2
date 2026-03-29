using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class ConditionEvaluatorTests
{
    private GameStateManager CreateGSM()
    {
        var gsm = new GameStateManager();
        gsm.State.Player = new PlayerState
        {
            Health = 100,
            MaxHealth = 100,
            Poise = 20,
            MaxPoise = 20,
            MaxAP = 2,
        };
        return gsm;
    }

    [Fact]
    public void EvaluateAll_NullConditions_ReturnsTrue()
    {
        var gsm = CreateGSM();
        Assert.True(ConditionEvaluator.EvaluateAll(null, gsm));
    }

    [Fact]
    public void EvaluateAll_EmptyConditions_ReturnsTrue()
    {
        var gsm = CreateGSM();
        Assert.True(ConditionEvaluator.EvaluateAll(new List<Condition>(), gsm));
    }

    [Fact]
    public void HasFlag_WhenFlagSet_ReturnsTrue()
    {
        var gsm = CreateGSM();
        gsm.SetFlag("talked_to_npc");
        var condition = new Condition { Type = "has_flag", Flag = "talked_to_npc" };
        Assert.True(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void HasFlag_WhenFlagNotSet_ReturnsFalse()
    {
        var gsm = CreateGSM();
        var condition = new Condition { Type = "has_flag", Flag = "talked_to_npc" };
        Assert.False(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void NotFlag_WhenFlagNotSet_ReturnsTrue()
    {
        var gsm = CreateGSM();
        var condition = new Condition { Type = "not_flag", Flag = "talked_to_npc" };
        Assert.True(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void NotFlag_WhenFlagSet_ReturnsFalse()
    {
        var gsm = CreateGSM();
        gsm.SetFlag("talked_to_npc");
        var condition = new Condition { Type = "not_flag", Flag = "talked_to_npc" };
        Assert.False(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void HasItem_WhenItemInInventory_ReturnsTrue()
    {
        var gsm = CreateGSM();
        gsm.AddToInventory("sword");
        var condition = new Condition { Type = "has_item", Item = "sword" };
        Assert.True(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void HasItem_WhenItemNotInInventory_ReturnsFalse()
    {
        var gsm = CreateGSM();
        var condition = new Condition { Type = "has_item", Item = "sword" };
        Assert.False(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void VariableEq_WhenMatches_ReturnsTrue()
    {
        var gsm = CreateGSM();
        gsm.SetVariable("quest_stage", "3");
        var condition = new Condition { Type = "variable_eq", Variable = "quest_stage", Value = "3" };
        Assert.True(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void VariableEq_WhenDoesNotMatch_ReturnsFalse()
    {
        var gsm = CreateGSM();
        gsm.SetVariable("quest_stage", "2");
        var condition = new Condition { Type = "variable_eq", Variable = "quest_stage", Value = "3" };
        Assert.False(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void VariableEq_WhenVariableNotSet_ReturnsFalse()
    {
        var gsm = CreateGSM();
        var condition = new Condition { Type = "variable_eq", Variable = "quest_stage", Value = "3" };
        Assert.False(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void VariableGte_WhenGreaterOrEqual_ReturnsTrue()
    {
        var gsm = CreateGSM();
        gsm.SetVariable("kills", "5");
        var condition = new Condition { Type = "variable_gte", Variable = "kills", Value = "5" };
        Assert.True(ConditionEvaluator.Evaluate(condition, gsm));

        gsm.SetVariable("kills", "10");
        Assert.True(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void VariableGte_WhenLessThan_ReturnsFalse()
    {
        var gsm = CreateGSM();
        gsm.SetVariable("kills", "3");
        var condition = new Condition { Type = "variable_gte", Variable = "kills", Value = "5" };
        Assert.False(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void VariableLt_WhenLessThan_ReturnsTrue()
    {
        var gsm = CreateGSM();
        gsm.SetVariable("kills", "3");
        var condition = new Condition { Type = "variable_lt", Variable = "kills", Value = "5" };
        Assert.True(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void VariableLt_WhenGreaterOrEqual_ReturnsFalse()
    {
        var gsm = CreateGSM();
        gsm.SetVariable("kills", "5");
        var condition = new Condition { Type = "variable_lt", Variable = "kills", Value = "5" };
        Assert.False(ConditionEvaluator.Evaluate(condition, gsm));

        gsm.SetVariable("kills", "10");
        Assert.False(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void VariableGte_WithNonNumericVariable_ReturnsFalse()
    {
        var gsm = CreateGSM();
        gsm.SetVariable("name", "alice");
        var condition = new Condition { Type = "variable_gte", Variable = "name", Value = "5" };
        Assert.False(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void QuestActive_WhenStartedButNotComplete_ReturnsTrue()
    {
        var gsm = CreateGSM();
        gsm.SetFlag("quest_started:find_sword");
        var condition = new Condition { Type = "quest_active", Value = "find_sword" };
        Assert.True(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void QuestActive_WhenBothStartedAndComplete_ReturnsFalse()
    {
        var gsm = CreateGSM();
        gsm.SetFlag("quest_started:find_sword");
        gsm.SetFlag("quest_complete:find_sword");
        var condition = new Condition { Type = "quest_active", Value = "find_sword" };
        Assert.False(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void QuestComplete_WhenFlagSet_ReturnsTrue()
    {
        var gsm = CreateGSM();
        gsm.SetFlag("quest_complete:find_sword");
        var condition = new Condition { Type = "quest_complete", Value = "find_sword" };
        Assert.True(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void QuestComplete_WhenFlagNotSet_ReturnsFalse()
    {
        var gsm = CreateGSM();
        var condition = new Condition { Type = "quest_complete", Value = "find_sword" };
        Assert.False(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void UnknownConditionType_ReturnsFalse()
    {
        var gsm = CreateGSM();
        var condition = new Condition { Type = "some_unknown_type", Value = "test" };
        Assert.False(ConditionEvaluator.Evaluate(condition, gsm));
    }

    [Fact]
    public void MultipleConditions_AllTrue_ReturnsTrue()
    {
        var gsm = CreateGSM();
        gsm.SetFlag("talked_to_npc");
        gsm.AddToInventory("sword");
        var conditions = new List<Condition>
        {
            new Condition { Type = "has_flag", Flag = "talked_to_npc" },
            new Condition { Type = "has_item", Item = "sword" },
        };
        Assert.True(ConditionEvaluator.EvaluateAll(conditions, gsm));
    }

    [Fact]
    public void MultipleConditions_OneFalse_ReturnsFalse()
    {
        var gsm = CreateGSM();
        gsm.SetFlag("talked_to_npc");
        // sword NOT in inventory
        var conditions = new List<Condition>
        {
            new Condition { Type = "has_flag", Flag = "talked_to_npc" },
            new Condition { Type = "has_item", Item = "sword" },
        };
        Assert.False(ConditionEvaluator.EvaluateAll(conditions, gsm));
    }
}

public class ActionExecutorTests
{
    private GameStateManager CreateGSM()
    {
        var gsm = new GameStateManager();
        gsm.State.Player = new PlayerState
        {
            Health = 100,
            MaxHealth = 100,
            Poise = 20,
            MaxPoise = 20,
            MaxAP = 2,
        };
        return gsm;
    }

    [Fact]
    public void ExecuteAll_NullActions_NoCrash()
    {
        var gsm = CreateGSM();
        ActionExecutor.ExecuteAll(null, gsm);
    }

    [Fact]
    public void ExecuteAll_EmptyActions_NoCrash()
    {
        var gsm = CreateGSM();
        ActionExecutor.ExecuteAll(new List<DialogueAction>(), gsm);
    }

    [Fact]
    public void SetFlag_SetsTheFlag()
    {
        var gsm = CreateGSM();
        var action = new DialogueAction { Type = "set_flag", Value = "talked_to_npc" };
        ActionExecutor.Execute(action, gsm);
        Assert.True(gsm.HasFlag("talked_to_npc"));
    }

    [Fact]
    public void ClearFlag_RemovesTheFlag()
    {
        var gsm = CreateGSM();
        gsm.SetFlag("guard_approved");
        Assert.True(gsm.HasFlag("guard_approved"));
        var action = new DialogueAction { Type = "clear_flag", Value = "guard_approved" };
        ActionExecutor.Execute(action, gsm);
        Assert.False(gsm.HasFlag("guard_approved"));
    }

    [Fact]
    public void ClearFlag_NonexistentFlag_NoCrash()
    {
        var gsm = CreateGSM();
        var action = new DialogueAction { Type = "clear_flag", Value = "nonexistent" };
        ActionExecutor.Execute(action, gsm);
        Assert.False(gsm.HasFlag("nonexistent"));
    }

    [Fact]
    public void SetVariable_SetsTheVariable()
    {
        var gsm = CreateGSM();
        var action = new DialogueAction { Type = "set_variable", Key = "quest_stage", Value = "2" };
        ActionExecutor.Execute(action, gsm);
        Assert.Equal("2", gsm.GetVariable("quest_stage"));
    }

    [Fact]
    public void Increment_IncrementsVariable()
    {
        var gsm = CreateGSM();
        var action = new DialogueAction { Type = "increment", Value = "kills" };

        ActionExecutor.Execute(action, gsm);
        Assert.Equal("1", gsm.GetVariable("kills"));

        ActionExecutor.Execute(action, gsm);
        Assert.Equal("2", gsm.GetVariable("kills"));
    }

    [Fact]
    public void GiveItem_AddsToInventory()
    {
        var gsm = CreateGSM();
        var action = new DialogueAction { Type = "give_item", Value = "sword" };
        ActionExecutor.Execute(action, gsm);
        Assert.True(gsm.HasItem("sword"));
    }

    [Fact]
    public void RemoveItem_RemovesFromInventory()
    {
        var gsm = CreateGSM();
        gsm.AddToInventory("key");
        var action = new DialogueAction { Type = "remove_item", Value = "key" };
        ActionExecutor.Execute(action, gsm);
        Assert.False(gsm.HasItem("key"));
    }

    [Fact]
    public void StartQuest_SetsQuestStartedFlag()
    {
        var gsm = CreateGSM();
        var action = new DialogueAction { Type = "start_quest", Value = "find_sword" };
        ActionExecutor.Execute(action, gsm);
        Assert.True(gsm.HasFlag("quest_started:find_sword"));
    }

    [Fact]
    public void CompleteObjective_SetsObjectiveCompleteFlag()
    {
        var gsm = CreateGSM();
        var action = new DialogueAction { Type = "complete_objective", Value = "find_sword" };
        ActionExecutor.Execute(action, gsm);
        Assert.True(gsm.HasFlag("objective_complete:find_sword"));
    }

    [Fact]
    public void Heal_IncreasesPlayerHealth()
    {
        var gsm = CreateGSM();
        gsm.State.Player.Health = 50;
        var action = new DialogueAction { Type = "heal", Value = "25" };
        ActionExecutor.Execute(action, gsm);
        Assert.Equal(75, gsm.State.Player.Health);
    }

    [Fact]
    public void Damage_DecreasesPlayerHealth()
    {
        var gsm = CreateGSM();
        // Poise absorbs damage first, so set poise to 0 for a clean health test
        gsm.State.Player.Poise = 0;
        var action = new DialogueAction { Type = "damage", Value = "30" };
        ActionExecutor.Execute(action, gsm);
        Assert.True(gsm.State.Player.Health < 100);
    }

    [Fact]
    public void Log_AddsMessageToGameLog()
    {
        var gsm = CreateGSM();
        var log = new GameLog();
        var action = new DialogueAction { Type = "log", Text = "You found a secret!", Color = null };
        ActionExecutor.Execute(action, gsm, log);
        Assert.Single(log.Entries);
        Assert.Equal("You found a secret!", log.Entries[0].Text);
    }

    [Fact]
    public void Log_WithYellowColor_SetsYellowOnEntry()
    {
        var gsm = CreateGSM();
        var log = new GameLog();
        var action = new DialogueAction { Type = "log", Text = "Warning!", Color = "yellow" };
        ActionExecutor.Execute(action, gsm, log);
        Assert.Single(log.Entries);
        Assert.Equal(Color.Yellow, log.Entries[0].Color);
    }

    [Fact]
    public void UnknownActionType_NoCrash()
    {
        var gsm = CreateGSM();
        var action = new DialogueAction { Type = "explode_universe", Value = "yes" };
        ActionExecutor.Execute(action, gsm);
        // No exception = pass
    }

    [Fact]
    public void MultipleActions_ExecutedInOrder()
    {
        var gsm = CreateGSM();
        var actions = new List<DialogueAction>
        {
            new DialogueAction { Type = "set_flag", Value = "step1" },
            new DialogueAction { Type = "set_variable", Key = "counter", Value = "10" },
            new DialogueAction { Type = "give_item", Value = "potion" },
        };
        ActionExecutor.ExecuteAll(actions, gsm);

        Assert.True(gsm.HasFlag("step1"));
        Assert.Equal("10", gsm.GetVariable("counter"));
        Assert.True(gsm.HasItem("potion"));
    }

    [Fact]
    public void MapTransition_SetsPendingTransition()
    {
        var gsm = CreateGSM();
        var action = new DialogueAction { Type = "map_transition", Value = "dungeon", Key = "5,3" };
        ActionExecutor.Execute(action, gsm);
        Assert.NotNull(gsm.PendingTransition);
        Assert.Equal("dungeon", gsm.PendingTransition.TargetMap);
        Assert.Equal(5, gsm.PendingTransition.TargetX);
        Assert.Equal(3, gsm.PendingTransition.TargetY);
    }

    [Fact]
    public void MapTransition_WithNoKey_DefaultsToZeroZero()
    {
        var gsm = CreateGSM();
        var action = new DialogueAction { Type = "map_transition", Value = "town" };
        ActionExecutor.Execute(action, gsm);
        Assert.NotNull(gsm.PendingTransition);
        Assert.Equal("town", gsm.PendingTransition.TargetMap);
        Assert.Equal(0, gsm.PendingTransition.TargetX);
        Assert.Equal(0, gsm.PendingTransition.TargetY);
    }

    [Fact]
    public void MapTransition_WithEmptyValue_DoesNotSetTransition()
    {
        var gsm = CreateGSM();
        var action = new DialogueAction { Type = "map_transition", Value = "" };
        ActionExecutor.Execute(action, gsm);
        Assert.Null(gsm.PendingTransition);
    }

    [Fact]
    public void MapTransition_WithSpacesInKey_ParsesCorrectly()
    {
        var gsm = CreateGSM();
        var action = new DialogueAction { Type = "map_transition", Value = "cave", Key = " 10 , 20 " };
        ActionExecutor.Execute(action, gsm);
        Assert.NotNull(gsm.PendingTransition);
        Assert.Equal(10, gsm.PendingTransition.TargetX);
        Assert.Equal(20, gsm.PendingTransition.TargetY);
    }
}
