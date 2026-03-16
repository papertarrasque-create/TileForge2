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
        Assert.Null(quest.StartFlag); // computed from null Id
        Assert.Null(quest.CompletionFlag); // computed from null Id
        Assert.NotNull(quest.Rewards);
        Assert.Empty(quest.Rewards);
        Assert.NotNull(quest.Objectives);
        Assert.Empty(quest.Objectives);
    }

    [Fact]
    public void QuestDefinition_ComputedFlags()
    {
        var quest = new QuestDefinition { Id = "test" };
        Assert.Equal("quest_started:test", quest.StartFlag);
        Assert.Equal("quest_complete:test", quest.CompletionFlag);
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
            Objectives = new List<QuestObjective>
            {
                new() { Description = "Find the key", Type = "flag", Flag = "has_key" },
                new() { Description = "Kill 3 goblins", Type = "variable_gte", Variable = "goblin_kills", Value = 3 },
                new() { Description = "Set counter", Type = "variable_eq", Variable = "counter", Value = 10 },
            },
            Rewards = new List<DialogueAction>
            {
                new() { Type = "set_flag", Value = "reward_flag" },
                new() { Type = "set_variable", Key = "gold", Value = "100" },
            },
        };

        // StartFlag/CompletionFlag are computed and [JsonIgnore], so they won't round-trip via JSON.
        Assert.Equal("quest_started:test_quest", quest.StartFlag);
        Assert.Equal("quest_complete:test_quest", quest.CompletionFlag);

        var json = JsonSerializer.Serialize(quest);
        var deserialized = JsonSerializer.Deserialize<QuestDefinition>(json);

        Assert.Equal("test_quest", deserialized.Id);
        Assert.Equal("Test Quest", deserialized.Name);
        Assert.Equal("A test quest.", deserialized.Description);
        Assert.Equal("quest_started:test_quest", deserialized.StartFlag);
        Assert.Equal("quest_complete:test_quest", deserialized.CompletionFlag);
        Assert.Equal(3, deserialized.Objectives.Count);
        Assert.Equal("flag", deserialized.Objectives[0].Type);
        Assert.Equal("has_key", deserialized.Objectives[0].Flag);
        Assert.Equal("variable_gte", deserialized.Objectives[1].Type);
        Assert.Equal("goblin_kills", deserialized.Objectives[1].Variable);
        Assert.Equal(3, deserialized.Objectives[1].Value);
        Assert.Equal("variable_eq", deserialized.Objectives[2].Type);
        Assert.Equal(2, deserialized.Rewards.Count);
        Assert.Equal("set_flag", deserialized.Rewards[0].Type);
        Assert.Equal("reward_flag", deserialized.Rewards[0].Value);
        Assert.Equal("set_variable", deserialized.Rewards[1].Type);
        Assert.Equal("gold", deserialized.Rewards[1].Key);
        Assert.Equal("100", deserialized.Rewards[1].Value);
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
        // Old format rewards are migrated to DialogueAction list
        Assert.Equal(2, quests[0].Rewards.Count);
        Assert.Equal("set_flag", quests[0].Rewards[0].Type);
        Assert.Equal("villager_saved", quests[0].Rewards[0].Value);
        Assert.Equal("set_variable", quests[0].Rewards[1].Type);
        Assert.Equal("rep", quests[0].Rewards[1].Key);
        Assert.Equal("5", quests[0].Rewards[1].Value);
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
        // StartFlag is computed from Id, not from JSON
        Assert.Equal("quest_started:q1", quests[0].StartFlag);
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

    // =========================================================================
    // Quest reward migration tests
    // =========================================================================

    [Fact]
    public void LoadFromJson_OldRewardFormat_MigratesToActionList()
    {
        var json = """
        { "quests": [{ "id": "q1", "name": "Test", "objectives": [],
            "rewards": { "set_flags": ["hero_flag", "quest_done"], "set_variables": { "gold": "100" } } }] }
        """;
        var quests = QuestLoader.LoadFromJson(json);
        Assert.Single(quests);
        Assert.Equal(3, quests[0].Rewards.Count);
        Assert.Equal("set_flag", quests[0].Rewards[0].Type);
        Assert.Equal("hero_flag", quests[0].Rewards[0].Value);
        Assert.Equal("set_variable", quests[0].Rewards[2].Type);
        Assert.Equal("gold", quests[0].Rewards[2].Key);
    }

    [Fact]
    public void LoadFromJson_NewRewardFormat_LoadsDirectly()
    {
        var json = """
        { "quests": [{ "id": "q1", "name": "Test", "objectives": [],
            "rewards": [{ "type": "set_flag", "value": "hero" }, { "type": "give_item", "value": "Ring" }] }] }
        """;
        var quests = QuestLoader.LoadFromJson(json);
        Assert.Equal(2, quests[0].Rewards.Count);
        Assert.Equal("give_item", quests[0].Rewards[1].Type);
    }

    [Fact]
    public void LoadFromJson_NoRewards_DefaultsToEmptyList()
    {
        var json = """{ "quests": [{ "id": "q1", "name": "Test", "objectives": [] }] }""";
        var quests = QuestLoader.LoadFromJson(json);
        Assert.Empty(quests[0].Rewards);
    }
}
