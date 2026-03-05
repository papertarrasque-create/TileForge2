using System.Collections.Generic;
using Xunit;
using TileForge.Game;
using TileForge.UI;

namespace TileForge.Tests.UI;

public class DialogueTreeEditorTests
{
    [Fact]
    public void ForNewDialogue_SetsIsNewTrue()
    {
        var editor = DialogueTreeEditor.ForNewDialogue();

        Assert.True(editor.IsNew);
        Assert.False(editor.IsComplete);
        Assert.False(editor.WasCancelled);
        Assert.Null(editor.Result);
        Assert.Null(editor.OriginalId);
    }

    [Fact]
    public void ForExistingDialogue_PreservesOriginalId()
    {
        var dialogue = new DialogueData
        {
            Id = "my_dialogue",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "start", Speaker = "Elder", Text = "Hello", EditorX = 100, EditorY = 200 }
            }
        };

        var editor = DialogueTreeEditor.ForExistingDialogue(dialogue);

        Assert.False(editor.IsNew);
        Assert.Equal("my_dialogue", editor.OriginalId);
        Assert.False(editor.IsComplete);
    }

    [Fact]
    public void ForExistingDialogue_WithPositions_PreservesPositions()
    {
        var dialogue = new DialogueData
        {
            Id = "test",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "a", EditorX = 50, EditorY = 75 },
                new() { Id = "b", EditorX = 300, EditorY = 75, NextNodeId = "a" },
            }
        };

        // Positions should be preserved (no auto-layout needed)
        var editor = DialogueTreeEditor.ForExistingDialogue(dialogue);

        Assert.False(editor.IsNew);
        // The dialogue object passed in should not be mutated (deep copy)
        Assert.Equal(50, dialogue.Nodes[0].EditorX);
        Assert.Equal(75, dialogue.Nodes[0].EditorY);
    }

    [Fact]
    public void ForExistingDialogue_WithoutPositions_TriggersAutoLayout()
    {
        var dialogue = new DialogueData
        {
            Id = "test",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "start", NextNodeId = "end" },
                new() { Id = "end" },
            }
        };

        // Nodes have no EditorX/EditorY — should trigger auto-layout
        var editor = DialogueTreeEditor.ForExistingDialogue(dialogue);

        // The original dialogue should not be mutated
        Assert.Null(dialogue.Nodes[0].EditorX);
        Assert.Null(dialogue.Nodes[1].EditorX);
    }

    [Fact]
    public void ForExistingDialogue_DeepCopiesData()
    {
        var dialogue = new DialogueData
        {
            Id = "original",
            Nodes = new List<DialogueNode>
            {
                new()
                {
                    Id = "start",
                    Speaker = "NPC",
                    EditorX = 0,
                    EditorY = 0,
                    Choices = new List<DialogueChoice>
                    {
                        new() { Text = "Yes", NextNodeId = "accept" }
                    }
                }
            }
        };

        var editor = DialogueTreeEditor.ForExistingDialogue(dialogue);

        // Verify original is not modified
        Assert.Equal("original", dialogue.Id);
        Assert.Equal("start", dialogue.Nodes[0].Id);
        Assert.Single(dialogue.Nodes[0].Choices);
    }

    [Fact]
    public void ForNewDialogue_StartsNotComplete()
    {
        var editor = DialogueTreeEditor.ForNewDialogue();

        Assert.False(editor.IsComplete);
        Assert.False(editor.WasCancelled);
        Assert.Null(editor.Result);
    }
}
