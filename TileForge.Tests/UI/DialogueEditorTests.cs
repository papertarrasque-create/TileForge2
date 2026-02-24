using System.Collections.Generic;
using TileForge.Game;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class DialogueEditorTests
{
    [Fact]
    public void ForNewDialogue_IsNew_True()
    {
        var editor = DialogueEditor.ForNewDialogue();
        Assert.True(editor.IsNew);
    }

    [Fact]
    public void ForNewDialogue_IsNotComplete()
    {
        var editor = DialogueEditor.ForNewDialogue();
        Assert.False(editor.IsComplete);
    }

    [Fact]
    public void ForNewDialogue_ResultIsNull()
    {
        var editor = DialogueEditor.ForNewDialogue();
        Assert.Null(editor.Result);
    }

    [Fact]
    public void ForNewDialogue_WasCancelled_False()
    {
        var editor = DialogueEditor.ForNewDialogue();
        Assert.False(editor.WasCancelled);
    }

    [Fact]
    public void ForExistingDialogue_IsNew_False()
    {
        var dialogue = new DialogueData
        {
            Id = "test_dlg",
            Nodes = new List<DialogueNode>
            {
                new DialogueNode { Id = "start", Speaker = "NPC", Text = "Hello" }
            }
        };
        var editor = DialogueEditor.ForExistingDialogue(dialogue);
        Assert.False(editor.IsNew);
    }

    [Fact]
    public void ForExistingDialogue_OriginalId_Set()
    {
        var dialogue = new DialogueData
        {
            Id = "my_dialogue",
            Nodes = new List<DialogueNode>()
        };
        var editor = DialogueEditor.ForExistingDialogue(dialogue);
        Assert.Equal("my_dialogue", editor.OriginalId);
    }

    [Fact]
    public void ForExistingDialogue_IsNotComplete()
    {
        var dialogue = new DialogueData
        {
            Id = "test",
            Nodes = new List<DialogueNode>()
        };
        var editor = DialogueEditor.ForExistingDialogue(dialogue);
        Assert.False(editor.IsComplete);
    }

    [Fact]
    public void ForExistingDialogue_WithChoices_CreatesSuccessfully()
    {
        var dialogue = new DialogueData
        {
            Id = "branching",
            Nodes = new List<DialogueNode>
            {
                new DialogueNode
                {
                    Id = "start",
                    Speaker = "Elder",
                    Text = "Choose wisely.",
                    Choices = new List<DialogueChoice>
                    {
                        new DialogueChoice { Text = "Option A", NextNodeId = "a" },
                        new DialogueChoice { Text = "Option B", NextNodeId = "b", RequiresFlag = "has_key", SetsFlag = "chose_b" },
                    }
                }
            }
        };
        var editor = DialogueEditor.ForExistingDialogue(dialogue);
        Assert.False(editor.IsNew);
        Assert.Equal("branching", editor.OriginalId);
        Assert.False(editor.IsComplete);
    }
}
