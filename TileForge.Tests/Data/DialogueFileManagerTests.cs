using System.Collections.Generic;
using System.Text.Json;
using TileForge.Data;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Data;

public class DialogueFileManagerTests
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void ToJson_EmptyDialogue_ProducesValidJson()
    {
        var dialogue = new DialogueData { Id = "empty_dlg", Nodes = new List<DialogueNode>() };

        string json = DialogueFileManager.ToJson(dialogue);

        Assert.Contains("\"id\"", json);
        Assert.Contains("\"empty_dlg\"", json);
        Assert.Contains("\"nodes\"", json);
    }

    [Fact]
    public void ToJson_SingleNode_UsesCamelCaseKeys()
    {
        var dialogue = new DialogueData
        {
            Id = "test_dlg",
            Nodes = new List<DialogueNode>
            {
                new()
                {
                    Id = "start",
                    Speaker = "Guard",
                    Text = "Halt!",
                    NextNodeId = "end",
                    SetsFlag = "guard_met",
                    RequiresFlag = "has_pass",
                    SetsVariable = "met_guard=1",
                }
            }
        };

        string json = DialogueFileManager.ToJson(dialogue);

        Assert.Contains("\"nextNodeId\"", json);
        Assert.Contains("\"setsFlag\"", json);
        Assert.Contains("\"requiresFlag\"", json);
        Assert.Contains("\"setsVariable\"", json);
        Assert.DoesNotContain("\"NextNodeId\"", json);
        Assert.DoesNotContain("\"SetsFlag\"", json);
        Assert.DoesNotContain("\"RequiresFlag\"", json);
        Assert.DoesNotContain("\"SetsVariable\"", json);
    }

    [Fact]
    public void ToJson_WithChoices_PreservesAllFields()
    {
        var dialogue = new DialogueData
        {
            Id = "branching",
            Nodes = new List<DialogueNode>
            {
                new()
                {
                    Id = "start",
                    Speaker = "Elder",
                    Text = "What do you seek?",
                    Choices = new List<DialogueChoice>
                    {
                        new()
                        {
                            Text = "Adventure",
                            NextNodeId = "adventure",
                            SetsFlag = "chose_adventure",
                            RequiresFlag = "is_hero",
                        },
                        new()
                        {
                            Text = "Nothing",
                            NextNodeId = "farewell",
                        }
                    }
                }
            }
        };

        string json = DialogueFileManager.ToJson(dialogue);

        Assert.Contains("\"Adventure\"", json);
        Assert.Contains("\"adventure\"", json);
        Assert.Contains("\"chose_adventure\"", json);
        Assert.Contains("\"is_hero\"", json);
        Assert.Contains("\"Nothing\"", json);
        Assert.Contains("\"farewell\"", json);
        Assert.Contains("\"nextNodeId\"", json);
        Assert.Contains("\"setsFlag\"", json);
        Assert.Contains("\"requiresFlag\"", json);
    }

    [Fact]
    public void ToJson_NullFields_OmittedFromOutput()
    {
        var dialogue = new DialogueData
        {
            Id = "minimal",
            Nodes = new List<DialogueNode>
            {
                new()
                {
                    Id = "only",
                    Speaker = "Narrator",
                    Text = "The end.",
                    // NextNodeId, RequiresFlag, SetsFlag, SetsVariable, Choices all null
                }
            }
        };

        string json = DialogueFileManager.ToJson(dialogue);

        Assert.DoesNotContain("\"nextNodeId\"", json);
        Assert.DoesNotContain("\"requiresFlag\"", json);
        Assert.DoesNotContain("\"setsFlag\"", json);
        Assert.DoesNotContain("\"setsVariable\"", json);
        Assert.DoesNotContain("\"choices\"", json);
    }

    [Fact]
    public void ToJson_NullDialogue_ReturnsEmptyJson()
    {
        string json = DialogueFileManager.ToJson(null);

        // Should not throw; should produce valid JSON for an empty DialogueData
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        var deserialized = JsonSerializer.Deserialize<DialogueData>(json, ReadOptions);
        Assert.NotNull(deserialized);
    }

    [Fact]
    public void RoundTrip_LinearDialogue_PreservesFields()
    {
        var original = new DialogueData
        {
            Id = "linear_01",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "n1", Speaker = "Hero", Text = "Hello!", NextNodeId = "n2" },
                new() { Id = "n2", Speaker = "Merchant", Text = "Greetings, traveler." },
            }
        };

        string json = DialogueFileManager.ToJson(original);
        var loaded = JsonSerializer.Deserialize<DialogueData>(json, ReadOptions);

        Assert.NotNull(loaded);
        Assert.Equal("linear_01", loaded.Id);
        Assert.Equal(2, loaded.Nodes.Count);
        Assert.Equal("n1", loaded.Nodes[0].Id);
        Assert.Equal("Hero", loaded.Nodes[0].Speaker);
        Assert.Equal("Hello!", loaded.Nodes[0].Text);
        Assert.Equal("n2", loaded.Nodes[0].NextNodeId);
        Assert.Equal("n2", loaded.Nodes[1].Id);
        Assert.Equal("Merchant", loaded.Nodes[1].Speaker);
    }

    [Fact]
    public void RoundTrip_BranchingDialogue_PreservesChoices()
    {
        var original = new DialogueData
        {
            Id = "branching_rt",
            Nodes = new List<DialogueNode>
            {
                new()
                {
                    Id = "start",
                    Speaker = "Gatekeeper",
                    Text = "Do you have the key?",
                    Choices = new List<DialogueChoice>
                    {
                        new() { Text = "Yes", NextNodeId = "pass", SetsFlag = "gate_opened" },
                        new() { Text = "No", NextNodeId = "denied" },
                    }
                },
                new() { Id = "pass", Speaker = "Gatekeeper", Text = "You may enter." },
                new() { Id = "denied", Speaker = "Gatekeeper", Text = "Come back when you do." },
            }
        };

        string json = DialogueFileManager.ToJson(original);
        var loaded = JsonSerializer.Deserialize<DialogueData>(json, ReadOptions);

        Assert.NotNull(loaded);
        Assert.Equal("branching_rt", loaded.Id);
        Assert.Equal(3, loaded.Nodes.Count);

        var startNode = loaded.Nodes[0];
        Assert.Equal("start", startNode.Id);
        Assert.NotNull(startNode.Choices);
        Assert.Equal(2, startNode.Choices.Count);
        Assert.Equal("Yes", startNode.Choices[0].Text);
        Assert.Equal("pass", startNode.Choices[0].NextNodeId);
        Assert.Equal("gate_opened", startNode.Choices[0].SetsFlag);
        Assert.Equal("No", startNode.Choices[1].Text);
        Assert.Equal("denied", startNode.Choices[1].NextNodeId);
    }

    [Fact]
    public void RoundTrip_WithFlags_PreservesConditionals()
    {
        var original = new DialogueData
        {
            Id = "flags_rt",
            Nodes = new List<DialogueNode>
            {
                new()
                {
                    Id = "conditional",
                    Speaker = "Sage",
                    Text = "I see you carry the ancient relic.",
                    RequiresFlag = "has_relic",
                    SetsFlag = "sage_impressed",
                    SetsVariable = "sage_trust=5",
                    NextNodeId = "reward",
                },
                new()
                {
                    Id = "reward",
                    Speaker = "Sage",
                    Text = "Then you are worthy.",
                    Choices = new List<DialogueChoice>
                    {
                        new()
                        {
                            Text = "Thank you",
                            NextNodeId = "end",
                            RequiresFlag = "sage_impressed",
                            SetsFlag = "sage_quest_done",
                        }
                    }
                }
            }
        };

        string json = DialogueFileManager.ToJson(original);
        var loaded = JsonSerializer.Deserialize<DialogueData>(json, ReadOptions);

        Assert.NotNull(loaded);
        Assert.Equal("flags_rt", loaded.Id);
        Assert.Equal(2, loaded.Nodes.Count);

        var cond = loaded.Nodes[0];
        Assert.Equal("has_relic", cond.RequiresFlag);
        Assert.Equal("sage_impressed", cond.SetsFlag);
        Assert.Equal("sage_trust=5", cond.SetsVariable);
        Assert.Equal("reward", cond.NextNodeId);

        var reward = loaded.Nodes[1];
        Assert.NotNull(reward.Choices);
        Assert.Single(reward.Choices);
        Assert.Equal("sage_impressed", reward.Choices[0].RequiresFlag);
        Assert.Equal("sage_quest_done", reward.Choices[0].SetsFlag);
    }

    [Fact]
    public void GetDialoguesDir_ReturnsCorrectPath()
    {
        string dir = DialogueFileManager.GetDialoguesDir("/some/project");
        Assert.Equal("/some/project/dialogues", dir);
    }

    [Fact]
    public void LoadAll_NullDir_ReturnsEmptyList()
    {
        var result = DialogueFileManager.LoadAll(null);
        Assert.Empty(result);
    }

    [Fact]
    public void LoadAll_EmptyString_ReturnsEmptyList()
    {
        var result = DialogueFileManager.LoadAll("");
        Assert.Empty(result);
    }
}
