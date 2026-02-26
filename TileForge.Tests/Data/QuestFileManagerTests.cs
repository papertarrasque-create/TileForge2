using System.Collections.Generic;
using TileForge.Data;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Data;

public class QuestFileManagerTests
{
    [Fact]
    public void ToJson_EmptyList_ProducesValidJson()
    {
        string json = QuestFileManager.ToJson(new List<QuestDefinition>());
        Assert.Contains("\"quests\"", json);
        Assert.Contains("[]", json);
    }

    [Fact]
    public void ToJson_NullList_ProducesEmptyArray()
    {
        string json = QuestFileManager.ToJson(null);
        Assert.Contains("\"quests\"", json);
        Assert.Contains("[]", json);
    }

    [Fact]
    public void ToJson_SingleQuest_UsesSnakeCaseKeys()
    {
        var quests = new List<QuestDefinition>
        {
            new()
            {
                Id = "test_quest",
                Name = "Test",
                StartFlag = "quest_started",
                CompletionFlag = "quest_done",
                Objectives = new List<QuestObjective>(),
            }
        };

        string json = QuestFileManager.ToJson(quests);

        Assert.Contains("\"start_flag\"", json);
        Assert.Contains("\"completion_flag\"", json);
        Assert.DoesNotContain("\"StartFlag\"", json);
        Assert.DoesNotContain("\"CompletionFlag\"", json);
    }

    [Fact]
    public void ToJson_WithObjectives_PreservesAll()
    {
        var quests = new List<QuestDefinition>
        {
            new()
            {
                Id = "q1",
                Name = "Quest One",
                Objectives = new List<QuestObjective>
                {
                    new() { Description = "Flag obj", Type = "flag", Flag = "some_flag" },
                    new() { Description = "Var obj", Type = "variable_gte", Variable = "kills", Value = 5 },
                }
            }
        };

        string json = QuestFileManager.ToJson(quests);

        Assert.Contains("\"flag\"", json);
        Assert.Contains("\"some_flag\"", json);
        Assert.Contains("\"variable_gte\"", json);
        Assert.Contains("\"kills\"", json);
        Assert.Contains("5", json);
    }

    [Fact]
    public void ToJson_WithRewards_PreservesAll()
    {
        var quests = new List<QuestDefinition>
        {
            new()
            {
                Id = "q1",
                Name = "Quest",
                Objectives = new List<QuestObjective>(),
                Rewards = new QuestRewards
                {
                    SetFlags = new List<string> { "flag_a", "flag_b" },
                    SetVariables = new Dictionary<string, string> { { "gold", "100" } }
                }
            }
        };

        string json = QuestFileManager.ToJson(quests);

        Assert.Contains("\"set_flags\"", json);
        Assert.Contains("\"flag_a\"", json);
        Assert.Contains("\"flag_b\"", json);
        Assert.Contains("\"set_variables\"", json);
        Assert.Contains("\"gold\"", json);
        Assert.Contains("\"100\"", json);
    }

    [Fact]
    public void RoundTrip_ThroughQuestLoader_PreservesFields()
    {
        var original = new List<QuestDefinition>
        {
            new()
            {
                Id = "round_trip",
                Name = "Round Trip Quest",
                Description = "Test round trip",
                StartFlag = "start_rt",
                CompletionFlag = "complete_rt",
                Objectives = new List<QuestObjective>
                {
                    new() { Description = "Flag check", Type = "flag", Flag = "visited_cave" },
                    new() { Description = "Kill count", Type = "variable_gte", Variable = "kills", Value = 3 },
                    new() { Description = "Exact match", Type = "variable_eq", Variable = "score", Value = 10 },
                },
                Rewards = new QuestRewards
                {
                    SetFlags = new List<string> { "quest_done" },
                    SetVariables = new Dictionary<string, string> { { "rep", "5" } }
                }
            }
        };

        string json = QuestFileManager.ToJson(original);
        var loaded = QuestLoader.LoadFromJson(json);

        Assert.Single(loaded);
        var q = loaded[0];
        Assert.Equal("round_trip", q.Id);
        Assert.Equal("Round Trip Quest", q.Name);
        Assert.Equal("Test round trip", q.Description);
        Assert.Equal("start_rt", q.StartFlag);
        Assert.Equal("complete_rt", q.CompletionFlag);

        Assert.Equal(3, q.Objectives.Count);
        Assert.Equal("flag", q.Objectives[0].Type);
        Assert.Equal("visited_cave", q.Objectives[0].Flag);
        Assert.Equal("variable_gte", q.Objectives[1].Type);
        Assert.Equal("kills", q.Objectives[1].Variable);
        Assert.Equal(3, q.Objectives[1].Value);
        Assert.Equal("variable_eq", q.Objectives[2].Type);
        Assert.Equal(10, q.Objectives[2].Value);

        Assert.NotNull(q.Rewards);
        Assert.Contains("quest_done", q.Rewards.SetFlags);
        Assert.Equal("5", q.Rewards.SetVariables["rep"]);
    }

    [Fact]
    public void RoundTrip_MultipleQuests_PreservesAll()
    {
        var original = new List<QuestDefinition>
        {
            new() { Id = "q1", Name = "First", Objectives = new() },
            new() { Id = "q2", Name = "Second", Objectives = new() },
            new() { Id = "q3", Name = "Third", Objectives = new() },
        };

        string json = QuestFileManager.ToJson(original);
        var loaded = QuestLoader.LoadFromJson(json);

        Assert.Equal(3, loaded.Count);
        Assert.Equal("q1", loaded[0].Id);
        Assert.Equal("q2", loaded[1].Id);
        Assert.Equal("q3", loaded[2].Id);
    }

    [Fact]
    public void GetQuestPath_ReturnsCorrectPath()
    {
        string path = QuestFileManager.GetQuestPath("/some/dir");
        Assert.Equal(Path.Combine("/some/dir", "quests.json"), path);
    }

    [Fact]
    public void Load_EmptyDir_ReturnsEmptyList()
    {
        var result = QuestFileManager.Load("");
        Assert.Empty(result);
    }

    [Fact]
    public void Load_NullDir_ReturnsEmptyList()
    {
        var result = QuestFileManager.Load(null);
        Assert.Empty(result);
    }
}
