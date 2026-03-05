using System.Collections.Generic;
using System.Linq;
using TileForge.Data;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class QuestManagerTests
{
    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Creates a minimal initialized GameStateManager following the same pattern
    /// used in GameStateManagerTests and AttackTests.
    /// </summary>
    private static GameStateManager CreateManager()
    {
        var map = new MapData(10, 10);
        var playerGroup = new TileGroup { Name = "player", Type = GroupType.Entity, IsPlayer = true };
        map.Entities.Add(new Entity { Id = "p1", GroupName = "player", X = 5, Y = 5 });
        var groups = new Dictionary<string, TileGroup> { ["player"] = playerGroup };
        var manager = new GameStateManager();
        manager.Initialize(map, groups);
        return manager;
    }

    private static QuestDefinition MakeQuest(string id, string startFlag, string completionFlag,
        List<QuestObjective> objectives = null, QuestRewards rewards = null)
    {
        return new QuestDefinition
        {
            Id = id,
            Name = $"Quest: {id}",
            StartFlag = startFlag,
            CompletionFlag = completionFlag,
            Objectives = objectives ?? new List<QuestObjective>(),
            Rewards = rewards,
        };
    }

    private static QuestObjective FlagObjective(string description, string flag)
        => new() { Description = description, Type = "flag", Flag = flag };

    private static QuestObjective VarGteObjective(string description, string variable, int value)
        => new() { Description = description, Type = "variable_gte", Variable = variable, Value = value };

    private static QuestObjective VarEqObjective(string description, string variable, int value)
        => new() { Description = description, Type = "variable_eq", Variable = variable, Value = value };

    // =========================================================================
    // 1. Quest_NotStarted_WhenStartFlagMissing
    // =========================================================================

    [Fact]
    public void Quest_NotStarted_WhenStartFlagMissing()
    {
        var gsm = CreateManager();
        var quest = MakeQuest("q1", startFlag: "quest_started:q1", completionFlag: "quest_done:q1",
            objectives: new List<QuestObjective> { FlagObjective("Do thing", "did_thing") });
        var manager = new QuestManager(new List<QuestDefinition> { quest });

        var events = manager.CheckForUpdates(gsm);

        Assert.Empty(events);
    }

    // =========================================================================
    // 2. Quest_Started_WhenStartFlagSet_ReturnsQuestStartedEvent
    // =========================================================================

    [Fact]
    public void Quest_Started_WhenStartFlagSet_ReturnsQuestStartedEvent()
    {
        var gsm = CreateManager();
        gsm.SetFlag("quest_started:q1");
        var quest = MakeQuest("q1", startFlag: "quest_started:q1", completionFlag: "quest_done:q1",
            objectives: new List<QuestObjective> { FlagObjective("Do thing", "did_thing") });
        var manager = new QuestManager(new List<QuestDefinition> { quest });

        var events = manager.CheckForUpdates(gsm);

        Assert.Contains(events, e => e.Type == QuestEventType.QuestStarted && e.QuestId == "q1");
    }

    // =========================================================================
    // 3. Quest_StartEvent_ReportedOnlyOnce
    // =========================================================================

    [Fact]
    public void Quest_StartEvent_ReportedOnlyOnce()
    {
        var gsm = CreateManager();
        gsm.SetFlag("quest_started:q1");
        var quest = MakeQuest("q1", startFlag: "quest_started:q1", completionFlag: "quest_done:q1",
            objectives: new List<QuestObjective> { FlagObjective("Do thing", "did_thing") });
        var manager = new QuestManager(new List<QuestDefinition> { quest });

        var firstEvents = manager.CheckForUpdates(gsm);
        var secondEvents = manager.CheckForUpdates(gsm);

        Assert.Single(firstEvents.Where(e => e.Type == QuestEventType.QuestStarted));
        Assert.DoesNotContain(secondEvents, e => e.Type == QuestEventType.QuestStarted);
    }

    // =========================================================================
    // 4. FlagObjective_Met_WhenFlagSet
    // =========================================================================

    [Fact]
    public void FlagObjective_Met_WhenFlagSet()
    {
        var gsm = CreateManager();
        gsm.SetFlag("did_thing");

        var objective = FlagObjective("Do thing", "did_thing");
        Assert.True(QuestManager.EvaluateObjective(objective, gsm));
    }

    // =========================================================================
    // 5. FlagObjective_NotMet_WhenFlagMissing
    // =========================================================================

    [Fact]
    public void FlagObjective_NotMet_WhenFlagMissing()
    {
        var gsm = CreateManager();
        // flag NOT set

        var objective = FlagObjective("Do thing", "did_thing");
        Assert.False(QuestManager.EvaluateObjective(objective, gsm));
    }

    // =========================================================================
    // 6. FlagObjective_NotMet_WhenFlagIsNull
    // =========================================================================

    [Fact]
    public void FlagObjective_NotMet_WhenFlagIsNull()
    {
        var gsm = CreateManager();

        var objective = new QuestObjective { Description = "Bad objective", Type = "flag", Flag = null };
        Assert.False(QuestManager.EvaluateObjective(objective, gsm));
    }

    // =========================================================================
    // 7. VariableGteObjective_Met_WhenVariableGTE
    // =========================================================================

    [Fact]
    public void VariableGteObjective_Met_WhenVariableGTE()
    {
        var gsm = CreateManager();
        gsm.SetVariable("kills", "5");

        var objective = VarGteObjective("Kill 3", "kills", 3);
        Assert.True(QuestManager.EvaluateObjective(objective, gsm));
    }

    // =========================================================================
    // 8. VariableGteObjective_Met_WhenVariableEqual
    // =========================================================================

    [Fact]
    public void VariableGteObjective_Met_WhenVariableEqual()
    {
        var gsm = CreateManager();
        gsm.SetVariable("kills", "3");

        var objective = VarGteObjective("Kill 3", "kills", 3);
        Assert.True(QuestManager.EvaluateObjective(objective, gsm));
    }

    // =========================================================================
    // 9. VariableGteObjective_NotMet_WhenVariableLess
    // =========================================================================

    [Fact]
    public void VariableGteObjective_NotMet_WhenVariableLess()
    {
        var gsm = CreateManager();
        gsm.SetVariable("kills", "2");

        var objective = VarGteObjective("Kill 3", "kills", 3);
        Assert.False(QuestManager.EvaluateObjective(objective, gsm));
    }

    // =========================================================================
    // 10. VariableGteObjective_ReturnsZero_ForMissingVariable
    // =========================================================================

    [Fact]
    public void VariableGteObjective_ReturnsZero_ForMissingVariable()
    {
        var gsm = CreateManager();
        // variable "kills" not set at all

        // Target is 0, so missing variable (treated as 0) should satisfy >= 0
        var objectiveMet = VarGteObjective("Kill 0", "kills", 0);
        Assert.True(QuestManager.EvaluateObjective(objectiveMet, gsm));

        // Target is 1, so missing variable (treated as 0) should NOT satisfy >= 1
        var objectiveNotMet = VarGteObjective("Kill 1", "kills", 1);
        Assert.False(QuestManager.EvaluateObjective(objectiveNotMet, gsm));
    }

    // =========================================================================
    // 11. VariableEqObjective_Met_WhenEqual
    // =========================================================================

    [Fact]
    public void VariableEqObjective_Met_WhenEqual()
    {
        var gsm = CreateManager();
        gsm.SetVariable("stage", "3");

        var objective = VarEqObjective("Stage 3", "stage", 3);
        Assert.True(QuestManager.EvaluateObjective(objective, gsm));
    }

    // =========================================================================
    // 12. VariableEqObjective_NotMet_WhenNotEqual
    // =========================================================================

    [Fact]
    public void VariableEqObjective_NotMet_WhenNotEqual()
    {
        var gsm = CreateManager();
        gsm.SetVariable("stage", "2");

        var objective = VarEqObjective("Stage 3", "stage", 3);
        Assert.False(QuestManager.EvaluateObjective(objective, gsm));
    }

    // =========================================================================
    // 13. UnknownObjectiveType_EvaluatesFalse
    // =========================================================================

    [Fact]
    public void UnknownObjectiveType_EvaluatesFalse()
    {
        var gsm = CreateManager();

        var objective = new QuestObjective { Description = "Unknown", Type = "unsupported_type" };
        Assert.False(QuestManager.EvaluateObjective(objective, gsm));
    }

    // =========================================================================
    // 14. AllObjectivesMet_TriggersQuestCompleted
    // =========================================================================

    [Fact]
    public void AllObjectivesMet_TriggersQuestCompleted()
    {
        var gsm = CreateManager();
        gsm.SetFlag("quest_started:q1");
        gsm.SetFlag("did_thing");
        var quest = MakeQuest("q1", startFlag: "quest_started:q1", completionFlag: "quest_done:q1",
            objectives: new List<QuestObjective> { FlagObjective("Do thing", "did_thing") });
        var manager = new QuestManager(new List<QuestDefinition> { quest });

        var events = manager.CheckForUpdates(gsm);

        Assert.Contains(events, e => e.Type == QuestEventType.QuestCompleted && e.QuestId == "q1");
    }

    // =========================================================================
    // 15. CompletionSets_CompletionFlag
    // =========================================================================

    [Fact]
    public void CompletionSets_CompletionFlag()
    {
        var gsm = CreateManager();
        gsm.SetFlag("quest_started:q1");
        gsm.SetFlag("did_thing");
        var quest = MakeQuest("q1", startFlag: "quest_started:q1", completionFlag: "quest_done:q1",
            objectives: new List<QuestObjective> { FlagObjective("Do thing", "did_thing") });
        var manager = new QuestManager(new List<QuestDefinition> { quest });

        manager.CheckForUpdates(gsm);

        Assert.True(gsm.HasFlag("quest_done:q1"));
    }

    // =========================================================================
    // 16. CompletionApplies_RewardFlags
    // =========================================================================

    [Fact]
    public void CompletionApplies_RewardFlags()
    {
        var gsm = CreateManager();
        gsm.SetFlag("quest_started:q1");
        gsm.SetFlag("did_thing");
        var rewards = new QuestRewards
        {
            SetFlags = new List<string> { "reward_unlocked", "npc_friendly" },
        };
        var quest = MakeQuest("q1", startFlag: "quest_started:q1", completionFlag: "quest_done:q1",
            objectives: new List<QuestObjective> { FlagObjective("Do thing", "did_thing") },
            rewards: rewards);
        var manager = new QuestManager(new List<QuestDefinition> { quest });

        manager.CheckForUpdates(gsm);

        Assert.True(gsm.HasFlag("reward_unlocked"));
        Assert.True(gsm.HasFlag("npc_friendly"));
    }

    // =========================================================================
    // 17. CompletionApplies_RewardVariables
    // =========================================================================

    [Fact]
    public void CompletionApplies_RewardVariables()
    {
        var gsm = CreateManager();
        gsm.SetFlag("quest_started:q1");
        gsm.SetFlag("did_thing");
        var rewards = new QuestRewards
        {
            SetVariables = new Dictionary<string, string> { ["gold"] = "100", ["rep"] = "5" },
        };
        var quest = MakeQuest("q1", startFlag: "quest_started:q1", completionFlag: "quest_done:q1",
            objectives: new List<QuestObjective> { FlagObjective("Do thing", "did_thing") },
            rewards: rewards);
        var manager = new QuestManager(new List<QuestDefinition> { quest });

        manager.CheckForUpdates(gsm);

        Assert.Equal("100", gsm.GetVariable("gold"));
        Assert.Equal("5", gsm.GetVariable("rep"));
    }

    // =========================================================================
    // 18. AlreadyCompletedQuest_IsSkipped
    // =========================================================================

    [Fact]
    public void AlreadyCompletedQuest_IsSkipped()
    {
        var gsm = CreateManager();
        gsm.SetFlag("quest_started:q1");
        gsm.SetFlag("quest_done:q1"); // already completed
        var quest = MakeQuest("q1", startFlag: "quest_started:q1", completionFlag: "quest_done:q1",
            objectives: new List<QuestObjective> { FlagObjective("Do thing", "did_thing") });
        var manager = new QuestManager(new List<QuestDefinition> { quest });

        var events = manager.CheckForUpdates(gsm);

        Assert.Empty(events);
    }

    // =========================================================================
    // 19. ObjectiveCompletionEvent_ReportedOnlyOnce
    // =========================================================================

    [Fact]
    public void ObjectiveCompletionEvent_ReportedOnlyOnce()
    {
        var gsm = CreateManager();
        gsm.SetFlag("quest_started:q1");
        // Objective requires two things; only one is met so quest won't complete
        var quest = MakeQuest("q1", startFlag: "quest_started:q1", completionFlag: "quest_done:q1",
            objectives: new List<QuestObjective>
            {
                FlagObjective("Do thing A", "did_a"),
                FlagObjective("Do thing B", "did_b"),
            });
        var manager = new QuestManager(new List<QuestDefinition> { quest });

        gsm.SetFlag("did_a");
        var firstEvents = manager.CheckForUpdates(gsm);
        var secondEvents = manager.CheckForUpdates(gsm);

        var firstObjectiveEvents = firstEvents.Where(e => e.Type == QuestEventType.ObjectiveCompleted).ToList();
        var secondObjectiveEvents = secondEvents.Where(e => e.Type == QuestEventType.ObjectiveCompleted).ToList();

        Assert.Single(firstObjectiveEvents);
        Assert.Equal("Do thing A", firstObjectiveEvents[0].ObjectiveDescription);
        Assert.Empty(secondObjectiveEvents);
    }

    // =========================================================================
    // 20. QuestWithNoObjectives_NeverCompletes
    // =========================================================================

    [Fact]
    public void QuestWithNoObjectives_NeverCompletes()
    {
        var gsm = CreateManager();
        gsm.SetFlag("quest_started:q1");
        // Quest has no objectives — the allComplete branch requires Objectives.Count > 0
        var quest = MakeQuest("q1", startFlag: "quest_started:q1", completionFlag: "quest_done:q1",
            objectives: new List<QuestObjective>());
        var manager = new QuestManager(new List<QuestDefinition> { quest });

        var events = manager.CheckForUpdates(gsm);

        Assert.DoesNotContain(events, e => e.Type == QuestEventType.QuestCompleted);
        Assert.False(gsm.HasFlag("quest_done:q1"));
    }

    // =========================================================================
    // 21. GetQuestStatus_NotStarted
    // =========================================================================

    [Fact]
    public void GetQuestStatus_NotStarted()
    {
        var gsm = CreateManager();
        // start flag NOT set
        var quest = MakeQuest("q1", startFlag: "quest_started:q1", completionFlag: "quest_done:q1");
        var manager = new QuestManager(new List<QuestDefinition> { quest });

        var status = manager.GetQuestStatus(quest, gsm);

        Assert.Equal(QuestStatus.NotStarted, status);
    }

    // =========================================================================
    // 22. GetQuestStatus_Active
    // =========================================================================

    [Fact]
    public void GetQuestStatus_Active()
    {
        var gsm = CreateManager();
        gsm.SetFlag("quest_started:q1");
        // completion flag NOT set
        var quest = MakeQuest("q1", startFlag: "quest_started:q1", completionFlag: "quest_done:q1");
        var manager = new QuestManager(new List<QuestDefinition> { quest });

        var status = manager.GetQuestStatus(quest, gsm);

        Assert.Equal(QuestStatus.Active, status);
    }

    // =========================================================================
    // 23. GetQuestStatus_Completed
    // =========================================================================

    [Fact]
    public void GetQuestStatus_Completed()
    {
        var gsm = CreateManager();
        gsm.SetFlag("quest_started:q1");
        gsm.SetFlag("quest_done:q1");
        var quest = MakeQuest("q1", startFlag: "quest_started:q1", completionFlag: "quest_done:q1");
        var manager = new QuestManager(new List<QuestDefinition> { quest });

        var status = manager.GetQuestStatus(quest, gsm);

        Assert.Equal(QuestStatus.Completed, status);
    }

    // =========================================================================
    // 24. MultipleQuests_EvaluatedIndependently
    // =========================================================================

    [Fact]
    public void MultipleQuests_EvaluatedIndependently()
    {
        var gsm = CreateManager();
        // q1 started and ready to complete; q2 not yet started
        gsm.SetFlag("quest_started:q1");
        gsm.SetFlag("did_q1_thing");

        var quest1 = MakeQuest("q1", startFlag: "quest_started:q1", completionFlag: "quest_done:q1",
            objectives: new List<QuestObjective> { FlagObjective("Do q1 thing", "did_q1_thing") });
        var quest2 = MakeQuest("q2", startFlag: "quest_started:q2", completionFlag: "quest_done:q2",
            objectives: new List<QuestObjective> { FlagObjective("Do q2 thing", "did_q2_thing") });
        var manager = new QuestManager(new List<QuestDefinition> { quest1, quest2 });

        var events = manager.CheckForUpdates(gsm);

        // q1 should have: QuestStarted, ObjectiveCompleted, QuestCompleted
        Assert.Contains(events, e => e.Type == QuestEventType.QuestStarted && e.QuestId == "q1");
        Assert.Contains(events, e => e.Type == QuestEventType.ObjectiveCompleted && e.QuestId == "q1");
        Assert.Contains(events, e => e.Type == QuestEventType.QuestCompleted && e.QuestId == "q1");

        // q2 should produce no events
        Assert.DoesNotContain(events, e => e.QuestId == "q2");

        Assert.True(gsm.HasFlag("quest_done:q1"));
        Assert.False(gsm.HasFlag("quest_done:q2"));
    }

    // =========================================================================
    // 25. MixedObjectives_QuestStaysActive
    // =========================================================================

    [Fact]
    public void MixedObjectives_QuestStaysActive()
    {
        var gsm = CreateManager();
        gsm.SetFlag("quest_started:q1");
        // Meet the flag objective and the variable_gte objective,
        // but NOT the variable_eq objective — quest should NOT complete.
        gsm.SetFlag("has_key");
        gsm.SetVariable("kills", "5");
        // "stage" variable not set → variable_eq vs. 3 will be 0 == 3 → false

        var quest = MakeQuest("q1", startFlag: "quest_started:q1", completionFlag: "quest_done:q1",
            objectives: new List<QuestObjective>
            {
                FlagObjective("Find the key", "has_key"),
                VarGteObjective("Kill 3 enemies", "kills", 3),
                VarEqObjective("Reach stage 3", "stage", 3),
            });
        var manager = new QuestManager(new List<QuestDefinition> { quest });

        var events = manager.CheckForUpdates(gsm);

        // Two objectives should fire as completed (flag + variable_gte)
        var objectiveEvents = events.Where(e => e.Type == QuestEventType.ObjectiveCompleted).ToList();
        Assert.Equal(2, objectiveEvents.Count);

        // Quest should NOT be completed
        Assert.DoesNotContain(events, e => e.Type == QuestEventType.QuestCompleted);
        Assert.False(gsm.HasFlag("quest_done:q1"));

        // Now satisfy the remaining objective and call again
        gsm.SetVariable("stage", "3");
        var secondEvents = manager.CheckForUpdates(gsm);

        // The third objective should now fire (only once — new)
        var newObjectiveEvents = secondEvents.Where(e => e.Type == QuestEventType.ObjectiveCompleted).ToList();
        Assert.Single(newObjectiveEvents);
        Assert.Equal("Reach stage 3", newObjectiveEvents[0].ObjectiveDescription);

        // Quest should now complete
        Assert.Contains(secondEvents, e => e.Type == QuestEventType.QuestCompleted);
        Assert.True(gsm.HasFlag("quest_done:q1"));
    }
}
