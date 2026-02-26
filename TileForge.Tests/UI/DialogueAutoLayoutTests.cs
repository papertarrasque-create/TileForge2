using System.Collections.Generic;
using Xunit;
using TileForge.Game;
using TileForge.UI;

namespace TileForge.Tests.UI;

public class DialogueAutoLayoutTests
{
    [Fact]
    public void ApplyLayout_SingleNode_PlacedAtOrigin()
    {
        var dialogue = new DialogueData
        {
            Id = "test",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "start" }
            }
        };

        DialogueAutoLayout.ApplyLayout(dialogue);

        Assert.Equal(0, dialogue.Nodes[0].EditorX);
        Assert.Equal(0, dialogue.Nodes[0].EditorY);
    }

    [Fact]
    public void ApplyLayout_LinearChain_PlacedInColumns()
    {
        var dialogue = new DialogueData
        {
            Id = "test",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "a", NextNodeId = "b" },
                new() { Id = "b", NextNodeId = "c" },
                new() { Id = "c" }
            }
        };

        DialogueAutoLayout.ApplyLayout(dialogue);

        Assert.Equal(0, dialogue.Nodes[0].EditorX);
        Assert.Equal(DialogueAutoLayout.DefaultColumnSpacing, dialogue.Nodes[1].EditorX);
        Assert.Equal(DialogueAutoLayout.DefaultColumnSpacing * 2, dialogue.Nodes[2].EditorX);
    }

    [Fact]
    public void ApplyLayout_BranchingNode_ChildrenInSameColumnDifferentRows()
    {
        var dialogue = new DialogueData
        {
            Id = "test",
            Nodes = new List<DialogueNode>
            {
                new()
                {
                    Id = "root",
                    Choices = new List<DialogueChoice>
                    {
                        new() { Text = "Yes", NextNodeId = "yes" },
                        new() { Text = "No", NextNodeId = "no" }
                    }
                },
                new() { Id = "yes" },
                new() { Id = "no" }
            }
        };

        DialogueAutoLayout.ApplyLayout(dialogue);

        // Root at column 0
        Assert.Equal(0, dialogue.Nodes[0].EditorX);
        // Both children at column 1
        Assert.Equal(DialogueAutoLayout.DefaultColumnSpacing, dialogue.Nodes[1].EditorX);
        Assert.Equal(DialogueAutoLayout.DefaultColumnSpacing, dialogue.Nodes[2].EditorX);
        // Different rows
        Assert.NotEqual(dialogue.Nodes[1].EditorY, dialogue.Nodes[2].EditorY);
    }

    [Fact]
    public void ApplyLayout_DisconnectedNodes_PlacedInLastColumn()
    {
        var dialogue = new DialogueData
        {
            Id = "test",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "start", NextNodeId = "end" },
                new() { Id = "end" },
                new() { Id = "orphan" }
            }
        };

        DialogueAutoLayout.ApplyLayout(dialogue);

        // start → column 0, end → column 1, orphan → column 2 (last + 1)
        Assert.Equal(0, dialogue.Nodes[0].EditorX);
        Assert.Equal(DialogueAutoLayout.DefaultColumnSpacing, dialogue.Nodes[1].EditorX);
        // orphan should be in a column after end
        Assert.True(dialogue.Nodes[2].EditorX > dialogue.Nodes[1].EditorX);
    }

    [Fact]
    public void ApplyLayout_EmptyDialogue_NoOp()
    {
        var dialogue = new DialogueData { Id = "test", Nodes = new List<DialogueNode>() };
        DialogueAutoLayout.ApplyLayout(dialogue); // should not throw
    }

    [Fact]
    public void ApplyLayout_NullDialogue_NoOp()
    {
        DialogueAutoLayout.ApplyLayout(null); // should not throw
    }

    [Fact]
    public void ApplyLayout_StartNodePreferred_AsRoot()
    {
        var dialogue = new DialogueData
        {
            Id = "test",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "other" },
                new() { Id = "start", NextNodeId = "other" }
            }
        };

        DialogueAutoLayout.ApplyLayout(dialogue);

        // "start" node should be at column 0 (root), "other" at column 1
        Assert.Equal(DialogueAutoLayout.DefaultColumnSpacing, dialogue.Nodes[0].EditorX); // "other"
        Assert.Equal(0, dialogue.Nodes[1].EditorX); // "start"
    }

    [Fact]
    public void ApplyLayout_CustomSpacing_Applied()
    {
        var dialogue = new DialogueData
        {
            Id = "test",
            Nodes = new List<DialogueNode>
            {
                new() { Id = "a", NextNodeId = "b" },
                new() { Id = "b" }
            }
        };

        DialogueAutoLayout.ApplyLayout(dialogue, columnSpacing: 100, rowSpacing: 50);

        Assert.Equal(0, dialogue.Nodes[0].EditorX);
        Assert.Equal(100, dialogue.Nodes[1].EditorX);
    }
}
