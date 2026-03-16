using System.Collections.Generic;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class CrossReferenceValidatorTests
{
    // =========================================================================
    // FindDialoguesReferencingQuest
    // =========================================================================

    [Fact]
    public void FindDialoguesReferencingQuest_FindsStartQuestAction()
    {
        // 3 dialogues, 2 reference "quest_a" via start_quest and complete_objective actions
        var dialogues = new List<DialogueData>
        {
            new()
            {
                Id = "dlg_elder",
                Nodes = new()
                {
                    new DialogueNode
                    {
                        Id = "n1",
                        Text = "Go on a quest!",
                        Actions = new() { new DialogueAction { Type = "start_quest", Value = "quest_a" } }
                    }
                }
            },
            new()
            {
                Id = "dlg_guard",
                Nodes = new()
                {
                    new DialogueNode
                    {
                        Id = "n1",
                        Text = "You did it!",
                        Actions = new() { new DialogueAction { Type = "complete_objective", Value = "quest_a:obj_1" } }
                    }
                }
            },
            new()
            {
                Id = "dlg_merchant",
                Nodes = new()
                {
                    new DialogueNode
                    {
                        Id = "n1",
                        Text = "Buy something.",
                        Actions = new() { new DialogueAction { Type = "start_quest", Value = "quest_b" } }
                    }
                }
            },
        };

        var refs = CrossReferenceValidator.FindDialoguesReferencingQuest("quest_a", dialogues);

        Assert.Equal(2, refs.Count);
        var ids = refs.ConvertAll(r => r.DialogueId);
        Assert.Contains("dlg_elder", ids);
        Assert.Contains("dlg_guard", ids);
        Assert.DoesNotContain("dlg_merchant", ids);
    }

    [Fact]
    public void FindDialoguesReferencingQuest_NoReferences_ReturnsEmpty()
    {
        var dialogues = new List<DialogueData>
        {
            new()
            {
                Id = "dlg_npc",
                Nodes = new()
                {
                    new DialogueNode
                    {
                        Id = "n1",
                        Text = "Hello.",
                        Actions = new() { new DialogueAction { Type = "set_flag", Value = "met_npc" } }
                    }
                }
            }
        };

        var refs = CrossReferenceValidator.FindDialoguesReferencingQuest("quest_a", dialogues);

        Assert.Empty(refs);
    }

    // =========================================================================
    // FindQuestsReferencedByDialogue
    // =========================================================================

    [Fact]
    public void FindQuestsReferencedByDialogue_FindsStartQuestActions()
    {
        // dialogue with start_quest and complete_objective actions
        var dialogue = new DialogueData
        {
            Id = "dlg_elder",
            Nodes = new()
            {
                new DialogueNode
                {
                    Id = "n1",
                    Text = "Here is your task.",
                    Actions = new()
                    {
                        new DialogueAction { Type = "start_quest", Value = "quest_a" },
                        new DialogueAction { Type = "complete_objective", Value = "quest_b:find_sword" }
                    }
                },
                new DialogueNode
                {
                    Id = "n2",
                    Text = "Good luck.",
                    Choices = new()
                    {
                        new DialogueChoice
                        {
                            Text = "Thanks",
                            Actions = new() { new DialogueAction { Type = "start_quest", Value = "quest_c" } }
                        }
                    }
                }
            }
        };

        var quests = new List<QuestDefinition>
        {
            new() { Id = "quest_a", Name = "Quest A", Objectives = new() { new QuestObjective { Description = "Do thing" } } },
            new() { Id = "quest_b", Name = "Quest B", Objectives = new() { new QuestObjective { Description = "Do thing" } } },
            new() { Id = "quest_c", Name = "Quest C", Objectives = new() { new QuestObjective { Description = "Do thing" } } },
        };

        var refs = CrossReferenceValidator.FindQuestsReferencedByDialogue(dialogue, quests);

        Assert.Equal(3, refs.Count);
        var questIds = refs.ConvertAll(r => r.QuestId);
        Assert.Contains("quest_a", questIds);
        Assert.Contains("quest_b", questIds);
        Assert.Contains("quest_c", questIds);
    }

    // =========================================================================
    // FindBrokenQuestReferences
    // =========================================================================

    [Fact]
    public void FindBrokenQuestReferences_DetectsMissingQuest()
    {
        // dialogue references nonexistent quest
        var dialogues = new List<DialogueData>
        {
            new()
            {
                Id = "dlg_elder",
                Nodes = new()
                {
                    new DialogueNode
                    {
                        Id = "n1",
                        Text = "Go find it.",
                        Actions = new() { new DialogueAction { Type = "start_quest", Value = "missing_quest" } }
                    }
                }
            }
        };

        var quests = new List<QuestDefinition>
        {
            new() { Id = "quest_a", Name = "Quest A", Objectives = new() { new QuestObjective { Description = "Do thing" } } }
        };

        var broken = CrossReferenceValidator.FindBrokenQuestReferences(dialogues, quests);

        Assert.Single(broken);
        Assert.Equal("dlg_elder", broken[0].DialogueId);
        Assert.Equal("missing_quest", broken[0].ReferencedId);
    }

    [Fact]
    public void FindBrokenQuestReferences_NoBrokenRefs_ReturnsEmpty()
    {
        var dialogues = new List<DialogueData>
        {
            new()
            {
                Id = "dlg_elder",
                Nodes = new()
                {
                    new DialogueNode
                    {
                        Id = "n1",
                        Text = "Your quest begins.",
                        Actions = new() { new DialogueAction { Type = "start_quest", Value = "quest_a" } }
                    }
                }
            }
        };

        var quests = new List<QuestDefinition>
        {
            new() { Id = "quest_a", Name = "Quest A", Objectives = new() { new QuestObjective { Description = "Do thing" } } }
        };

        var broken = CrossReferenceValidator.FindBrokenQuestReferences(dialogues, quests);

        Assert.Empty(broken);
    }

    // =========================================================================
    // FindBrokenDialogueReferences
    // =========================================================================

    [Fact]
    public void FindBrokenDialogueReferences_DetectsMissingDialogue()
    {
        // entity refs a dialogue that doesn't exist
        var entityDialogueIds = new List<string> { "dlg_elder", "dlg_missing", "dlg_guard" };

        var dialogues = new List<DialogueData>
        {
            new() { Id = "dlg_elder" },
            new() { Id = "dlg_guard" },
        };

        var broken = CrossReferenceValidator.FindBrokenDialogueReferences(entityDialogueIds, dialogues);

        Assert.Single(broken);
        Assert.Equal("dlg_missing", broken[0]);
    }
}
