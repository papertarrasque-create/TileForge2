using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;
using TileForge.Game;
using TileForge.Data;

namespace TileForge.Tests.Game;

public class DialogueV2Tests
{
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // ========== 1. New Data Class Defaults ==========

    [Fact]
    public void Condition_Defaults_AllNull()
    {
        var c = new Condition();

        Assert.Null(c.Type);
        Assert.Null(c.Flag);
        Assert.Null(c.Item);
        Assert.Null(c.Variable);
        Assert.Null(c.Value);
        Assert.Null(c.Operator);
    }

    [Fact]
    public void DialogueAction_Defaults_AllNull()
    {
        var a = new DialogueAction();

        Assert.Null(a.Type);
        Assert.Null(a.Value);
        Assert.Null(a.Key);
        Assert.Null(a.Color);
        Assert.Null(a.Text);
    }

    [Fact]
    public void DialogueRoute_Defaults_NullStartNodeAndConditions()
    {
        var r = new DialogueRoute();

        Assert.Null(r.StartNode);
        Assert.Null(r.Conditions);
    }

    [Fact]
    public void DialogueData_V2Defaults_NullTypeRoutesOneShot_EmptyNodes()
    {
        var d = new DialogueData();

        Assert.Null(d.Type);
        Assert.Null(d.Routes);
        Assert.Null(d.OneShot);
        Assert.NotNull(d.Nodes);
        Assert.Empty(d.Nodes);
    }

    // ========== 2. V1 Migration (DialogueFileManager.MigrateV1ToV2) ==========

    [Fact]
    public void MigrateV1_NodeRequiresFlag_BecomesCondition()
    {
        var dialogue = new DialogueData
        {
            Nodes = new List<DialogueNode>
            {
                new DialogueNode { Id = "n1", RequiresFlag = "has_key" }
            }
        };

        DialogueFileManager.MigrateV1ToV2(dialogue);

        var node = dialogue.Nodes[0];
        Assert.Null(node.RequiresFlag);
        Assert.NotNull(node.Conditions);
        Assert.Single(node.Conditions);
        Assert.Equal("has_flag", node.Conditions[0].Type);
        Assert.Equal("has_key", node.Conditions[0].Flag);
    }

    [Fact]
    public void MigrateV1_NodeSetsFlag_BecomesAction()
    {
        var dialogue = new DialogueData
        {
            Nodes = new List<DialogueNode>
            {
                new DialogueNode { Id = "n1", SetsFlag = "quest_started" }
            }
        };

        DialogueFileManager.MigrateV1ToV2(dialogue);

        var node = dialogue.Nodes[0];
        Assert.Null(node.SetsFlag);
        Assert.NotNull(node.Actions);
        Assert.Single(node.Actions);
        Assert.Equal("set_flag", node.Actions[0].Type);
        Assert.Equal("quest_started", node.Actions[0].Value);
    }

    [Fact]
    public void MigrateV1_NodeSetsVariable_BecomesActionWithKeyValue()
    {
        var dialogue = new DialogueData
        {
            Nodes = new List<DialogueNode>
            {
                new DialogueNode { Id = "n1", SetsVariable = "quest_stage=2" }
            }
        };

        DialogueFileManager.MigrateV1ToV2(dialogue);

        var node = dialogue.Nodes[0];
        Assert.Null(node.SetsVariable);
        Assert.NotNull(node.Actions);
        Assert.Single(node.Actions);
        Assert.Equal("set_variable", node.Actions[0].Type);
        Assert.Equal("quest_stage", node.Actions[0].Key);
        Assert.Equal("2", node.Actions[0].Value);
    }

    [Fact]
    public void MigrateV1_ChoiceRequiresFlag_BecomesCondition()
    {
        var dialogue = new DialogueData
        {
            Nodes = new List<DialogueNode>
            {
                new DialogueNode
                {
                    Id = "n1",
                    Choices = new List<DialogueChoice>
                    {
                        new DialogueChoice { Text = "Secret option", RequiresFlag = "knows_secret" }
                    }
                }
            }
        };

        DialogueFileManager.MigrateV1ToV2(dialogue);

        var choice = dialogue.Nodes[0].Choices[0];
        Assert.Null(choice.RequiresFlag);
        Assert.NotNull(choice.Conditions);
        Assert.Single(choice.Conditions);
        Assert.Equal("has_flag", choice.Conditions[0].Type);
        Assert.Equal("knows_secret", choice.Conditions[0].Flag);
    }

    [Fact]
    public void MigrateV1_ChoiceSetsFlag_BecomesAction()
    {
        var dialogue = new DialogueData
        {
            Nodes = new List<DialogueNode>
            {
                new DialogueNode
                {
                    Id = "n1",
                    Choices = new List<DialogueChoice>
                    {
                        new DialogueChoice { Text = "Accept quest", SetsFlag = "quest_accepted" }
                    }
                }
            }
        };

        DialogueFileManager.MigrateV1ToV2(dialogue);

        var choice = dialogue.Nodes[0].Choices[0];
        Assert.Null(choice.SetsFlag);
        Assert.NotNull(choice.Actions);
        Assert.Single(choice.Actions);
        Assert.Equal("set_flag", choice.Actions[0].Type);
        Assert.Equal("quest_accepted", choice.Actions[0].Value);
    }

    [Fact]
    public void MigrateV1_ExistingConditions_NotOverwritten()
    {
        var existingCondition = new Condition { Type = "has_item", Item = "gold_key" };
        var dialogue = new DialogueData
        {
            Nodes = new List<DialogueNode>
            {
                new DialogueNode
                {
                    Id = "n1",
                    RequiresFlag = "some_flag",
                    Conditions = new List<Condition> { existingCondition }
                }
            }
        };

        DialogueFileManager.MigrateV1ToV2(dialogue);

        var node = dialogue.Nodes[0];
        // RequiresFlag should NOT be cleared because Conditions already existed
        Assert.Equal("some_flag", node.RequiresFlag);
        Assert.Single(node.Conditions);
        Assert.Equal("has_item", node.Conditions[0].Type);
        Assert.Equal("gold_key", node.Conditions[0].Item);
    }

    [Fact]
    public void MigrateV1_ExistingActions_SetsFlagAppended()
    {
        var existingAction = new DialogueAction { Type = "give_item", Key = "potion" };
        var dialogue = new DialogueData
        {
            Nodes = new List<DialogueNode>
            {
                new DialogueNode
                {
                    Id = "n1",
                    SetsFlag = "talked_to_npc",
                    Actions = new List<DialogueAction> { existingAction }
                }
            }
        };

        DialogueFileManager.MigrateV1ToV2(dialogue);

        var node = dialogue.Nodes[0];
        Assert.Null(node.SetsFlag);
        Assert.Equal(2, node.Actions.Count);
        Assert.Equal("give_item", node.Actions[0].Type);
        Assert.Equal("set_flag", node.Actions[1].Type);
        Assert.Equal("talked_to_npc", node.Actions[1].Value);
    }

    [Fact]
    public void MigrateV1_EmptyNodesAndNullRoutes_NoCrash()
    {
        var dialogue = new DialogueData
        {
            Nodes = new List<DialogueNode>(),
            Routes = null,
        };

        var ex = Record.Exception(() => DialogueFileManager.MigrateV1ToV2(dialogue));

        Assert.Null(ex);
        // No routes created because Nodes is empty
        Assert.Null(dialogue.Routes);
    }

    [Fact]
    public void MigrateV1_NullRoutes_CreatesDefaultRouteToFirstNode()
    {
        var dialogue = new DialogueData
        {
            Routes = null,
            Nodes = new List<DialogueNode>
            {
                new DialogueNode { Id = "start", Text = "Hello" },
                new DialogueNode { Id = "end", Text = "Goodbye" }
            }
        };

        DialogueFileManager.MigrateV1ToV2(dialogue);

        Assert.NotNull(dialogue.Routes);
        Assert.Single(dialogue.Routes);
        Assert.Equal("start", dialogue.Routes[0].StartNode);
        Assert.Null(dialogue.Routes[0].Conditions);
    }

    [Fact]
    public void MigrateV1_BothSetsFlagAndSetsVariable_BothMigratedToActions()
    {
        var dialogue = new DialogueData
        {
            Nodes = new List<DialogueNode>
            {
                new DialogueNode
                {
                    Id = "n1",
                    SetsFlag = "quest_started",
                    SetsVariable = "quest_stage=1"
                }
            }
        };

        DialogueFileManager.MigrateV1ToV2(dialogue);

        var node = dialogue.Nodes[0];
        Assert.Null(node.SetsFlag);
        Assert.Null(node.SetsVariable);
        Assert.NotNull(node.Actions);
        Assert.Equal(2, node.Actions.Count);

        var flagAction = node.Actions.First(a => a.Type == "set_flag");
        Assert.Equal("quest_started", flagAction.Value);

        var varAction = node.Actions.First(a => a.Type == "set_variable");
        Assert.Equal("quest_stage", varAction.Key);
        Assert.Equal("1", varAction.Value);
    }

    // ========== 3. V2 JSON Serialization ==========

    [Fact]
    public void V2Json_DialogueDataWithRoutes_RoundtripsViaToJson()
    {
        var original = new DialogueData
        {
            Id = "test_routes",
            Routes = new List<DialogueRoute>
            {
                new DialogueRoute
                {
                    StartNode = "branch_a",
                    Conditions = new List<Condition>
                    {
                        new Condition { Type = "has_flag", Flag = "met_wizard" }
                    }
                },
                new DialogueRoute { StartNode = "branch_b" }
            },
            Nodes = new List<DialogueNode>
            {
                new DialogueNode { Id = "branch_a", Text = "Ah, you again!" },
                new DialogueNode { Id = "branch_b", Text = "Who are you?" }
            }
        };

        string json = DialogueFileManager.ToJson(original);
        var deserialized = JsonSerializer.Deserialize<DialogueData>(json, CaseInsensitiveOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("test_routes", deserialized.Id);
        Assert.NotNull(deserialized.Routes);
        Assert.Equal(2, deserialized.Routes.Count);
        Assert.Equal("branch_a", deserialized.Routes[0].StartNode);
        Assert.NotNull(deserialized.Routes[0].Conditions);
        Assert.Single(deserialized.Routes[0].Conditions);
        Assert.Equal("has_flag", deserialized.Routes[0].Conditions[0].Type);
        Assert.Equal("met_wizard", deserialized.Routes[0].Conditions[0].Flag);
        Assert.Equal("branch_b", deserialized.Routes[1].StartNode);
    }

    [Fact]
    public void V2Json_NodeConditions_Roundtrip()
    {
        var original = new DialogueData
        {
            Id = "cond_test",
            Nodes = new List<DialogueNode>
            {
                new DialogueNode
                {
                    Id = "n1",
                    Text = "Conditional node",
                    Conditions = new List<Condition>
                    {
                        new Condition { Type = "has_item", Item = "magic_ring" },
                        new Condition { Type = "variable_gte", Variable = "level", Value = "5" }
                    }
                }
            }
        };

        string json = DialogueFileManager.ToJson(original);
        var deserialized = JsonSerializer.Deserialize<DialogueData>(json, CaseInsensitiveOptions);

        Assert.NotNull(deserialized);
        var node = deserialized.Nodes[0];
        Assert.NotNull(node.Conditions);
        Assert.Equal(2, node.Conditions.Count);
        Assert.Equal("has_item", node.Conditions[0].Type);
        Assert.Equal("magic_ring", node.Conditions[0].Item);
        Assert.Equal("variable_gte", node.Conditions[1].Type);
        Assert.Equal("level", node.Conditions[1].Variable);
        Assert.Equal("5", node.Conditions[1].Value);
    }

    [Fact]
    public void V2Json_NodeActions_Roundtrip()
    {
        var original = new DialogueData
        {
            Id = "action_test",
            Nodes = new List<DialogueNode>
            {
                new DialogueNode
                {
                    Id = "n1",
                    Text = "Action node",
                    Actions = new List<DialogueAction>
                    {
                        new DialogueAction { Type = "set_flag", Value = "talked" },
                        new DialogueAction { Type = "give_item", Key = "health_potion" },
                        new DialogueAction { Type = "log", Text = "Received a potion!", Color = "green" }
                    }
                }
            }
        };

        string json = DialogueFileManager.ToJson(original);
        var deserialized = JsonSerializer.Deserialize<DialogueData>(json, CaseInsensitiveOptions);

        Assert.NotNull(deserialized);
        var node = deserialized.Nodes[0];
        Assert.NotNull(node.Actions);
        Assert.Equal(3, node.Actions.Count);
        Assert.Equal("set_flag", node.Actions[0].Type);
        Assert.Equal("talked", node.Actions[0].Value);
        Assert.Equal("give_item", node.Actions[1].Type);
        Assert.Equal("health_potion", node.Actions[1].Key);
        Assert.Equal("log", node.Actions[2].Type);
        Assert.Equal("Received a potion!", node.Actions[2].Text);
        Assert.Equal("green", node.Actions[2].Color);
    }

    [Fact]
    public void V2Json_OneShot_Roundtrip()
    {
        var original = new DialogueData
        {
            Id = "oneshot_test",
            OneShot = true,
            Nodes = new List<DialogueNode>
            {
                new DialogueNode { Id = "n1", Text = "You can only hear this once." }
            }
        };

        string json = DialogueFileManager.ToJson(original);
        var deserialized = JsonSerializer.Deserialize<DialogueData>(json, CaseInsensitiveOptions);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.OneShot);
    }

    [Fact]
    public void V2Json_Type_Roundtrip()
    {
        var original = new DialogueData
        {
            Id = "type_test",
            Type = "conversation",
            Nodes = new List<DialogueNode>
            {
                new DialogueNode { Id = "n1", Text = "Hello." }
            }
        };

        string json = DialogueFileManager.ToJson(original);
        var deserialized = JsonSerializer.Deserialize<DialogueData>(json, CaseInsensitiveOptions);

        Assert.NotNull(deserialized);
        Assert.Equal("conversation", deserialized.Type);
    }

    [Fact]
    public void V2Json_NullV2Fields_OmittedFromJson()
    {
        var original = new DialogueData
        {
            Id = "minimal",
            // Type, Routes, OneShot all null
            Nodes = new List<DialogueNode>
            {
                new DialogueNode
                {
                    Id = "n1",
                    Text = "Simple node"
                    // Conditions, Actions, Tags all null
                }
            }
        };

        string json = DialogueFileManager.ToJson(original);

        // WriteOptions uses DefaultIgnoreCondition = WhenWritingNull
        Assert.DoesNotContain("\"type\"", json);
        Assert.DoesNotContain("\"routes\"", json);
        Assert.DoesNotContain("\"oneShot\"", json);
        Assert.DoesNotContain("\"conditions\"", json);
        Assert.DoesNotContain("\"actions\"", json);
        Assert.DoesNotContain("\"tags\"", json);
        Assert.DoesNotContain("\"requiresFlag\"", json);
        Assert.DoesNotContain("\"setsFlag\"", json);
        Assert.DoesNotContain("\"setsVariable\"", json);
    }

    [Fact]
    public void V2Json_V1FormatString_DeserializesCorrectly_ThenMigrates()
    {
        // Simulate a v1-format JSON string with camelCase v1 properties
        string v1Json = @"{
            ""id"": ""old_dialogue"",
            ""nodes"": [
                {
                    ""id"": ""start"",
                    ""speaker"": ""Guard"",
                    ""text"": ""Halt!"",
                    ""requiresFlag"": ""has_pass"",
                    ""setsFlag"": ""met_guard"",
                    ""setsVariable"": ""guard_visits=1"",
                    ""nextNodeId"": ""end""
                },
                {
                    ""id"": ""end"",
                    ""speaker"": ""Guard"",
                    ""text"": ""Move along.""
                }
            ]
        }";

        var dialogue = JsonSerializer.Deserialize<DialogueData>(v1Json, CaseInsensitiveOptions);
        Assert.NotNull(dialogue);
        Assert.Equal("old_dialogue", dialogue.Id);
        Assert.Equal(2, dialogue.Nodes.Count);

        // Before migration, v1 fields are populated
        Assert.Equal("has_pass", dialogue.Nodes[0].RequiresFlag);
        Assert.Equal("met_guard", dialogue.Nodes[0].SetsFlag);
        Assert.Equal("guard_visits=1", dialogue.Nodes[0].SetsVariable);

        DialogueFileManager.MigrateV1ToV2(dialogue);

        var startNode = dialogue.Nodes[0];
        // v1 fields cleared
        Assert.Null(startNode.RequiresFlag);
        Assert.Null(startNode.SetsFlag);
        Assert.Null(startNode.SetsVariable);
        // v2 fields populated
        Assert.NotNull(startNode.Conditions);
        Assert.Single(startNode.Conditions);
        Assert.Equal("has_flag", startNode.Conditions[0].Type);
        Assert.Equal("has_pass", startNode.Conditions[0].Flag);
        Assert.NotNull(startNode.Actions);
        Assert.Equal(2, startNode.Actions.Count);
    }

    // ========== 4. Backward Compatibility ==========

    [Fact]
    public void BackwardCompat_V1JsonWithChoices_MigratesToV2()
    {
        string v1Json = @"{
            ""id"": ""gate_guard"",
            ""nodes"": [
                {
                    ""id"": ""challenge"",
                    ""speaker"": ""Guard"",
                    ""text"": ""State your business."",
                    ""requiresFlag"": ""entered_city"",
                    ""setsFlag"": ""talked_to_guard"",
                    ""choices"": [
                        {
                            ""text"": ""I have a permit."",
                            ""nextNodeId"": ""permit_check"",
                            ""requiresFlag"": ""has_permit"",
                            ""setsFlag"": ""showed_permit""
                        },
                        {
                            ""text"": ""Just passing through."",
                            ""nextNodeId"": ""denied""
                        }
                    ]
                },
                {
                    ""id"": ""permit_check"",
                    ""speaker"": ""Guard"",
                    ""text"": ""Everything seems in order.""
                },
                {
                    ""id"": ""denied"",
                    ""speaker"": ""Guard"",
                    ""text"": ""No entry without a permit!""
                }
            ]
        }";

        var dialogue = JsonSerializer.Deserialize<DialogueData>(v1Json, CaseInsensitiveOptions);
        Assert.NotNull(dialogue);

        DialogueFileManager.MigrateV1ToV2(dialogue);

        // Node-level migration
        var challenge = dialogue.Nodes[0];
        Assert.Null(challenge.RequiresFlag);
        Assert.Null(challenge.SetsFlag);
        Assert.NotNull(challenge.Conditions);
        Assert.Single(challenge.Conditions);
        Assert.Equal("has_flag", challenge.Conditions[0].Type);
        Assert.Equal("entered_city", challenge.Conditions[0].Flag);
        Assert.NotNull(challenge.Actions);
        Assert.Single(challenge.Actions);
        Assert.Equal("set_flag", challenge.Actions[0].Type);
        Assert.Equal("talked_to_guard", challenge.Actions[0].Value);

        // Choice-level migration
        var permitChoice = challenge.Choices[0];
        Assert.Null(permitChoice.RequiresFlag);
        Assert.Null(permitChoice.SetsFlag);
        Assert.NotNull(permitChoice.Conditions);
        Assert.Single(permitChoice.Conditions);
        Assert.Equal("has_flag", permitChoice.Conditions[0].Type);
        Assert.Equal("has_permit", permitChoice.Conditions[0].Flag);
        Assert.NotNull(permitChoice.Actions);
        Assert.Single(permitChoice.Actions);
        Assert.Equal("set_flag", permitChoice.Actions[0].Type);
        Assert.Equal("showed_permit", permitChoice.Actions[0].Value);

        // Unmigrated choice (no v1 flags) stays clean
        var passingChoice = challenge.Choices[1];
        Assert.Null(passingChoice.RequiresFlag);
        Assert.Null(passingChoice.SetsFlag);
        Assert.Null(passingChoice.Conditions);
        Assert.Null(passingChoice.Actions);

        // Default route created
        Assert.NotNull(dialogue.Routes);
        Assert.Single(dialogue.Routes);
        Assert.Equal("challenge", dialogue.Routes[0].StartNode);
    }

    [Fact]
    public void MigrateV1_NullDialogue_NoCrash()
    {
        var ex = Record.Exception(() => DialogueFileManager.MigrateV1ToV2(null));
        Assert.Null(ex);
    }

    [Fact]
    public void MigrateV1_NullNodes_NoCrash()
    {
        var dialogue = new DialogueData { Nodes = null };
        var ex = Record.Exception(() => DialogueFileManager.MigrateV1ToV2(dialogue));
        Assert.Null(ex);
    }
}
