using System.Collections.Generic;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class QuestStateTests
{
    [Fact]
    public void NewQuestState_DefaultsToNotStarted()
    {
        var qs = new QuestState { QuestId = "q1" };
        Assert.Equal(QuestStatus.NotStarted, qs.Status);
        Assert.Empty(qs.CompletedObjectives);
    }

    [Fact]
    public void CompletedObjectives_TracksIds()
    {
        var qs = new QuestState { QuestId = "q1", Status = QuestStatus.Active };
        qs.CompletedObjectives.Add("obj_1");
        qs.CompletedObjectives.Add("obj_2");
        Assert.Equal(2, qs.CompletedObjectives.Count);
        Assert.Contains("obj_1", qs.CompletedObjectives);
    }

    [Fact]
    public void QuestDefinition_StartFlag_IsComputed()
    {
        var quest = new QuestDefinition { Id = "cave_quest" };
        Assert.Equal("quest_started:cave_quest", quest.StartFlag);
    }

    [Fact]
    public void QuestDefinition_CompletionFlag_IsComputed()
    {
        var quest = new QuestDefinition { Id = "cave_quest" };
        Assert.Equal("quest_complete:cave_quest", quest.CompletionFlag);
    }

    [Fact]
    public void QuestDefinition_Rewards_IsList_OfDialogueAction()
    {
        var quest = new QuestDefinition
        {
            Id = "q1",
            Rewards = new List<DialogueAction>
            {
                new() { Type = "set_flag", Value = "hero" },
                new() { Type = "give_item", Value = "Gold Ring" },
            }
        };
        Assert.Equal(2, quest.Rewards.Count);
        Assert.Equal("set_flag", quest.Rewards[0].Type);
        Assert.Equal("give_item", quest.Rewards[1].Type);
    }
}
