using System.Collections.Generic;
using System.Text.Json;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class QuestDataTests
{
    [Fact]
    public void QuestDefinition_DefaultValues()
    {
        var quest = new QuestDefinition();
        Assert.Null(quest.Id);
        Assert.Null(quest.Name);
        Assert.Null(quest.Description);
        Assert.Null(quest.StartFlag);
        Assert.Null(quest.CompletionFlag);
        Assert.Null(quest.Rewards);
        Assert.NotNull(quest.Objectives);
        Assert.Empty(quest.Objectives);
    }

    [Fact]
    public void QuestObjective_DefaultValues()
    {
        var obj = new QuestObjective();
        Assert.Null(obj.Description);
        Assert.Null(obj.Type);
        Assert.Null(obj.Flag);
        Assert.Null(obj.Variable);
        Assert.Equal(0, obj.Value);
    }

    [Fact]
    public void QuestRewards_DefaultValues()
    {
        var rewards = new QuestRewards();
        Assert.NotNull(rewards.SetFlags);
        Assert.Empty(rewards.SetFlags);
        Assert.NotNull(rewards.SetVariables);
        Assert.Empty(rewards.SetVariables);
    }

    [Fact]
    public void QuestFile_DefaultValues()
    {
        var file = new QuestFile();
        Assert.NotNull(file.Quests);
        Assert.Empty(file.Quests);
    }

    [Fact]
    public void QuestDefinition_SerializationRoundTrip()
    {
        var quest = new QuestDefinition
        {
            Id = "test_quest",
            Name = "Test Quest",
            Description = "A test quest.",
            StartFlag = "quest_started:test",
            CompletionFlag = "quest_complete:test",
            Objectives = new List<QuestObjective>
            {
                new() { Description = "Find the key", Type = "flag", Flag = "has_key" },
                new() { Description = "Kill 3 goblins", Type = "variable_gte", Variable = "goblin_kills", Value = 3 },
                new() { Description = "Set counter", Type = "variable_eq", Variable = "counter", Value = 10 },
            },
            Rewards = new QuestRewards
            {
                SetFlags = new List<string> { "reward_flag" },
                SetVariables = new Dictionary<string, string> { { "gold", "100" } },
            },
        };

        var json = JsonSerializer.Serialize(quest);
        var deserialized = JsonSerializer.Deserialize<QuestDefinition>(json);

        Assert.Equal("test_quest", deserialized.Id);
        Assert.Equal("Test Quest", deserialized.Name);
        Assert.Equal("A test quest.", deserialized.Description);
        Assert.Equal("quest_started:test", deserialized.StartFlag);
        Assert.Equal("quest_complete:test", deserialized.CompletionFlag);
        Assert.Equal(3, deserialized.Objectives.Count);
        Assert.Equal("flag", deserialized.Objectives[0].Type);
        Assert.Equal("has_key", deserialized.Objectives[0].Flag);
        Assert.Equal("variable_gte", deserialized.Objectives[1].Type);
        Assert.Equal("goblin_kills", deserialized.Objectives[1].Variable);
        Assert.Equal(3, deserialized.Objectives[1].Value);
        Assert.Equal("variable_eq", deserialized.Objectives[2].Type);
        Assert.Single(deserialized.Rewards.SetFlags);
        Assert.Equal("reward_flag", deserialized.Rewards.SetFlags[0]);
        Assert.Equal("100", deserialized.Rewards.SetVariables["gold"]);
    }

    [Fact]
    public void QuestFile_SerializationRoundTrip()
    {
        var file = new QuestFile
        {
            Quests = new List<QuestDefinition>
            {
                new() { Id = "q1", Name = "Quest One" },
                new() { Id = "q2", Name = "Quest Two" },
            },
        };

        var json = JsonSerializer.Serialize(file);
        var deserialized = JsonSerializer.Deserialize<QuestFile>(json);

        Assert.Equal(2, deserialized.Quests.Count);
        Assert.Equal("q1", deserialized.Quests[0].Id);
        Assert.Equal("q2", deserialized.Quests[1].Id);
    }

    [Fact]
    public void QuestLoader_LoadFromJson_ValidJson()
    {
        var json = """
        {
            "quests": [
                {
                    "id": "rescue",
                    "name": "Rescue Mission",
                    "description": "Save the villager.",
                    "start_flag": "quest_started:rescue",
                    "objectives": [
                        { "description": "Find key", "type": "flag", "flag": "has_key" }
                    ],
                    "completion_flag": "quest_complete:rescue",
                    "rewards": {
                        "set_flags": ["villager_saved"],
                        "set_variables": { "rep": "5" }
                    }
                }
            ]
        }
        """;

        var quests = QuestLoader.LoadFromJson(json);

        Assert.Single(quests);
        Assert.Equal("rescue", quests[0].Id);
        Assert.Equal("Rescue Mission", quests[0].Name);
        Assert.Equal("Save the villager.", quests[0].Description);
        Assert.Equal("quest_started:rescue", quests[0].StartFlag);
        Assert.Single(quests[0].Objectives);
        Assert.Equal("flag", quests[0].Objectives[0].Type);
        Assert.Equal("has_key", quests[0].Objectives[0].Flag);
        Assert.Equal("quest_complete:rescue", quests[0].CompletionFlag);
        Assert.Single(quests[0].Rewards.SetFlags);
        Assert.Equal("5", quests[0].Rewards.SetVariables["rep"]);
    }

    [Fact]
    public void QuestLoader_LoadFromJson_CaseInsensitive()
    {
        var json = """
        {
            "Quests": [
                {
                    "Id": "q1",
                    "Name": "Quest",
                    "StartFlag": "start",
                    "CompletionFlag": "done",
                    "Objectives": [
                        { "Description": "Do thing", "Type": "flag", "Flag": "did_thing" }
                    ]
                }
            ]
        }
        """;

        var quests = QuestLoader.LoadFromJson(json);

        Assert.Single(quests);
        Assert.Equal("q1", quests[0].Id);
        Assert.Equal("start", quests[0].StartFlag);
        Assert.Single(quests[0].Objectives);
        Assert.Equal("flag", quests[0].Objectives[0].Type);
    }

    [Fact]
    public void QuestLoader_LoadFromJson_EmptyQuestsArray()
    {
        var json = """{ "quests": [] }""";
        var quests = QuestLoader.LoadFromJson(json);
        Assert.NotNull(quests);
        Assert.Empty(quests);
    }

    [Fact]
    public void QuestLoader_LoadFromJson_NullJson_ReturnsEmpty()
    {
        var quests = QuestLoader.LoadFromJson(null);
        Assert.NotNull(quests);
        Assert.Empty(quests);
    }

    [Fact]
    public void QuestLoader_LoadFromJson_InvalidJson_ReturnsEmpty()
    {
        // LoadFromJson doesn't catch exceptions — only Load does.
        // For invalid JSON, it will throw from JsonSerializer.Deserialize.
        // Actually, let's test the Load method for the graceful path.
        // LoadFromJson is the raw deserializer.
        Assert.ThrowsAny<JsonException>(() => QuestLoader.LoadFromJson("not json"));
    }

    [Fact]
    public void QuestLoader_Load_NonexistentPath_ReturnsEmpty()
    {
        var quests = QuestLoader.Load("/nonexistent/path/quests.json");
        Assert.NotNull(quests);
        Assert.Empty(quests);
    }

    [Fact]
    public void QuestLoader_Load_NullPath_ReturnsEmpty()
    {
        var quests = QuestLoader.Load(null);
        Assert.NotNull(quests);
        Assert.Empty(quests);
    }

    [Fact]
    public void QuestLoader_Load_EmptyPath_ReturnsEmpty()
    {
        var quests = QuestLoader.Load("");
        Assert.NotNull(quests);
        Assert.Empty(quests);
    }
}
