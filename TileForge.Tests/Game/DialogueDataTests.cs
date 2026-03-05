using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class DialogueDataTests
{
    // ========== Default values ==========

    [Fact]
    public void DialogueData_Defaults()
    {
        var data = new DialogueData();

        Assert.Null(data.Id);
        Assert.NotNull(data.Nodes);
        Assert.Empty(data.Nodes);
    }

    [Fact]
    public void DialogueNode_Defaults()
    {
        var node = new DialogueNode();

        Assert.Null(node.Id);
        Assert.Null(node.Speaker);
        Assert.Null(node.Text);
        Assert.Null(node.Choices);
        Assert.Null(node.NextNodeId);
        Assert.Null(node.RequiresFlag);
        Assert.Null(node.SetsFlag);
        Assert.Null(node.SetsVariable);
    }

    [Fact]
    public void DialogueChoice_Defaults()
    {
        var choice = new DialogueChoice();

        Assert.Null(choice.Text);
        Assert.Null(choice.NextNodeId);
        Assert.Null(choice.RequiresFlag);
        Assert.Null(choice.SetsFlag);
    }

    // ========== Linear dialogue chain ==========

    [Fact]
    public void LinearDialogue_ThreeNodeChain_NextNodeIdLinks()
    {
        var node1 = new DialogueNode { Id = "n1", Speaker = "Elder", Text = "Welcome, traveler.", NextNodeId = "n2" };
        var node2 = new DialogueNode { Id = "n2", Speaker = "Elder", Text = "The village is in danger.", NextNodeId = "n3" };
        var node3 = new DialogueNode { Id = "n3", Speaker = "Elder", Text = "Will you help us?", NextNodeId = null };

        var dialogue = new DialogueData
        {
            Id = "elder_intro",
            Nodes = new List<DialogueNode> { node1, node2, node3 }
        };

        Assert.Equal(3, dialogue.Nodes.Count);
        Assert.Equal("n2", dialogue.Nodes[0].NextNodeId);
        Assert.Equal("n3", dialogue.Nodes[1].NextNodeId);
        Assert.Null(dialogue.Nodes[2].NextNodeId);
    }

    [Fact]
    public void LinearDialogue_NoChoices_OnLinearNodes()
    {
        var node1 = new DialogueNode { Id = "n1", Text = "Hello.", NextNodeId = "n2" };
        var node2 = new DialogueNode { Id = "n2", Text = "Goodbye." };

        Assert.Null(node1.Choices);
        Assert.Null(node2.Choices);
    }

    // ========== Branching dialogue ==========

    [Fact]
    public void BranchingDialogue_TwoChoicesPointToDifferentNodes()
    {
        var node = new DialogueNode
        {
            Id = "quest_offer",
            Speaker = "Elder",
            Text = "Will you accept the quest?",
            Choices = new List<DialogueChoice>
            {
                new DialogueChoice { Text = "Yes, I will help!", NextNodeId = "accept" },
                new DialogueChoice { Text = "No, I must refuse.", NextNodeId = "refuse" }
            }
        };

        Assert.NotNull(node.Choices);
        Assert.Equal(2, node.Choices.Count);
        Assert.Equal("accept", node.Choices[0].NextNodeId);
        Assert.Equal("refuse", node.Choices[1].NextNodeId);
        Assert.Equal("Yes, I will help!", node.Choices[0].Text);
        Assert.Equal("No, I must refuse.", node.Choices[1].Text);
    }

    // ========== Conditional node ==========

    [Fact]
    public void ConditionalNode_RequiresFlagSet()
    {
        var node = new DialogueNode
        {
            Id = "secret_info",
            Speaker = "Spy",
            Text = "Since you have the badge, here's the password...",
            RequiresFlag = "has_spy_badge"
        };

        Assert.Equal("has_spy_badge", node.RequiresFlag);
    }

    // ========== Flag-setting node ==========

    [Fact]
    public void FlagSettingNode_SetsFlagWhenShown()
    {
        var node = new DialogueNode
        {
            Id = "tutorial_complete",
            Speaker = "Guide",
            Text = "You've learned the basics!",
            SetsFlag = "tutorial_done"
        };

        Assert.Equal("tutorial_done", node.SetsFlag);
    }

    // ========== Variable-setting node ==========

    [Fact]
    public void VariableSettingNode_SetsVariableInKeyValueFormat()
    {
        var node = new DialogueNode
        {
            Id = "stage_advance",
            Speaker = "Elder",
            Text = "The quest advances.",
            SetsVariable = "quest_stage=2"
        };

        Assert.Equal("quest_stage=2", node.SetsVariable);
        var parts = node.SetsVariable.Split('=');
        Assert.Equal("quest_stage", parts[0]);
        Assert.Equal("2", parts[1]);
    }

    // ========== JSON roundtrip — full dialogue tree ==========

    [Fact]
    public void DialogueData_JsonRoundtrip_LinearDialogue()
    {
        var original = new DialogueData
        {
            Id = "inn_keeper",
            Nodes = new List<DialogueNode>
            {
                new DialogueNode
                {
                    Id = "greet",
                    Speaker = "Innkeeper",
                    Text = "Welcome to the Rusty Anchor!",
                    NextNodeId = "offer",
                    SetsFlag = "met_innkeeper"
                },
                new DialogueNode
                {
                    Id = "offer",
                    Speaker = "Innkeeper",
                    Text = "Need a room for the night?",
                    NextNodeId = null,
                    RequiresFlag = "met_innkeeper",
                    SetsVariable = "inn_visited=true"
                }
            }
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<DialogueData>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("inn_keeper", deserialized.Id);
        Assert.Equal(2, deserialized.Nodes.Count);

        var greet = deserialized.Nodes[0];
        Assert.Equal("greet", greet.Id);
        Assert.Equal("Innkeeper", greet.Speaker);
        Assert.Equal("Welcome to the Rusty Anchor!", greet.Text);
        Assert.Equal("offer", greet.NextNodeId);
        Assert.Equal("met_innkeeper", greet.SetsFlag);
        Assert.Null(greet.RequiresFlag);
        Assert.Null(greet.Choices);

        var offer = deserialized.Nodes[1];
        Assert.Equal("offer", offer.Id);
        Assert.Null(offer.NextNodeId);
        Assert.Equal("met_innkeeper", offer.RequiresFlag);
        Assert.Equal("inn_visited=true", offer.SetsVariable);
    }

    [Fact]
    public void DialogueData_JsonRoundtrip_WithChoices()
    {
        var original = new DialogueData
        {
            Id = "guard_gate",
            Nodes = new List<DialogueNode>
            {
                new DialogueNode
                {
                    Id = "challenge",
                    Speaker = "Guard",
                    Text = "Halt! State your business.",
                    Choices = new List<DialogueChoice>
                    {
                        new DialogueChoice
                        {
                            Text = "I am a merchant.",
                            NextNodeId = "merchant_path",
                            SetsFlag = "claimed_merchant"
                        },
                        new DialogueChoice
                        {
                            Text = "I carry a royal writ.",
                            NextNodeId = "writ_path",
                            RequiresFlag = "has_royal_writ"
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<DialogueData>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("guard_gate", deserialized.Id);
        Assert.Single(deserialized.Nodes);

        var node = deserialized.Nodes[0];
        Assert.Equal("challenge", node.Id);
        Assert.Equal("Guard", node.Speaker);
        Assert.NotNull(node.Choices);
        Assert.Equal(2, node.Choices.Count);

        var choice0 = node.Choices[0];
        Assert.Equal("I am a merchant.", choice0.Text);
        Assert.Equal("merchant_path", choice0.NextNodeId);
        Assert.Equal("claimed_merchant", choice0.SetsFlag);
        Assert.Null(choice0.RequiresFlag);

        var choice1 = node.Choices[1];
        Assert.Equal("I carry a royal writ.", choice1.Text);
        Assert.Equal("writ_path", choice1.NextNodeId);
        Assert.Equal("has_royal_writ", choice1.RequiresFlag);
        Assert.Null(choice1.SetsFlag);
    }

    // ========== DialogueChoice JSON roundtrip ==========

    [Fact]
    public void DialogueChoice_JsonRoundtrip_WithRequiresFlag()
    {
        var original = new DialogueChoice
        {
            Text = "Ask about the secret passage.",
            NextNodeId = "secret_node",
            RequiresFlag = "knows_about_passage",
            SetsFlag = "asked_about_passage"
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<DialogueChoice>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("Ask about the secret passage.", deserialized.Text);
        Assert.Equal("secret_node", deserialized.NextNodeId);
        Assert.Equal("knows_about_passage", deserialized.RequiresFlag);
        Assert.Equal("asked_about_passage", deserialized.SetsFlag);
    }

    // ========== Node lookup by Id ==========

    [Fact]
    public void DialogueData_NodeLookup_FindsNodeById()
    {
        var dialogue = new DialogueData
        {
            Id = "wizard_convo",
            Nodes = new List<DialogueNode>
            {
                new DialogueNode { Id = "start", Text = "Greetings." },
                new DialogueNode { Id = "middle", Text = "The spell requires three components." },
                new DialogueNode { Id = "end", Text = "Farewell, adventurer." }
            }
        };

        var found = dialogue.Nodes.FirstOrDefault(n => n.Id == "middle");

        Assert.NotNull(found);
        Assert.Equal("middle", found.Id);
        Assert.Equal("The spell requires three components.", found.Text);
    }

    [Fact]
    public void DialogueData_NodeLookup_ReturnsNullForMissingId()
    {
        var dialogue = new DialogueData
        {
            Id = "simple",
            Nodes = new List<DialogueNode>
            {
                new DialogueNode { Id = "only_node", Text = "I am alone." }
            }
        };

        var found = dialogue.Nodes.FirstOrDefault(n => n.Id == "nonexistent");

        Assert.Null(found);
    }

    [Fact]
    public void DialogueData_NodeLookup_EmptyNodes_ReturnsNull()
    {
        var dialogue = new DialogueData { Id = "empty" };

        var found = dialogue.Nodes.FirstOrDefault(n => n.Id == "any");

        Assert.Null(found);
    }
}
