using System.Collections.Generic;
using Xunit;
using Microsoft.Xna.Framework;
using TileForge.Game;
using TileForge.UI;

namespace TileForge.Tests.UI;

public class DialogueNodeWidgetTests
{
    [Fact]
    public void FromNode_NoChoices_SingleOutputPort()
    {
        var node = new DialogueNode
        {
            Id = "greeting",
            Speaker = "Elder",
            Text = "Hello traveler!",
            NextNodeId = "quest",
            EditorX = 100,
            EditorY = 200,
        };

        var widget = DialogueNodeWidget.FromNode(node, 0);

        Assert.Equal("greeting", widget.NodeId);
        Assert.Equal(0, widget.NodeIndex);
        Assert.Equal("Elder", widget.SpeakerLabel);
        Assert.Equal("Hello traveler!", widget.TextPreview);
        Assert.Single(widget.OutputPorts);
        Assert.Equal("quest", widget.OutputTargets[0]);
        Assert.Equal("→", widget.OutputLabels[0]);
        Assert.Equal(100, widget.Bounds.X);
        Assert.Equal(200, widget.Bounds.Y);
    }

    [Fact]
    public void FromNode_WithChoices_MultipleOutputPorts()
    {
        var node = new DialogueNode
        {
            Id = "question",
            Speaker = "NPC",
            Text = "Choose wisely.",
            EditorX = 0,
            EditorY = 0,
            Choices = new List<DialogueChoice>
            {
                new() { Text = "Yes", NextNodeId = "accept" },
                new() { Text = "No", NextNodeId = "decline" },
                new() { Text = "Maybe", NextNodeId = "ponder" },
            }
        };

        var widget = DialogueNodeWidget.FromNode(node, 1);

        Assert.Equal(3, widget.OutputPorts.Count);
        Assert.Equal("accept", widget.OutputTargets[0]);
        Assert.Equal("decline", widget.OutputTargets[1]);
        Assert.Equal("ponder", widget.OutputTargets[2]);
        Assert.Equal("Yes", widget.OutputLabels[0]);
        Assert.Equal("No", widget.OutputLabels[1]);
        Assert.Equal("Maybe", widget.OutputLabels[2]);
    }

    [Fact]
    public void FromNode_LongText_TruncatedTo30Chars()
    {
        var node = new DialogueNode
        {
            Id = "long",
            Text = "This is a very long dialogue text that should be truncated",
            EditorX = 0,
            EditorY = 0,
        };

        var widget = DialogueNodeWidget.FromNode(node, 0);

        Assert.True(widget.TextPreview.Length <= DialogueNodeWidget.MaxPreviewLength);
        Assert.EndsWith("...", widget.TextPreview);
    }

    [Fact]
    public void FromNode_NullSpeaker_DefaultLabel()
    {
        var node = new DialogueNode { Id = "test", EditorX = 0, EditorY = 0 };
        var widget = DialogueNodeWidget.FromNode(node, 0);

        Assert.Equal("(no speaker)", widget.SpeakerLabel);
    }

    [Fact]
    public void FromNode_NullId_FallbackName()
    {
        var node = new DialogueNode { EditorX = 0, EditorY = 0 };
        var widget = DialogueNodeWidget.FromNode(node, 3);

        Assert.Equal("node_3", widget.NodeId);
    }

    [Fact]
    public void HitTestOutputPort_InsidePort_ReturnsIndex()
    {
        var node = new DialogueNode
        {
            Id = "test",
            NextNodeId = "next",
            EditorX = 100,
            EditorY = 100,
        };

        var widget = DialogueNodeWidget.FromNode(node, 0);
        var portCenter = widget.GetOutputPortCenter(0);
        int result = widget.HitTestOutputPort((int)portCenter.X, (int)portCenter.Y);

        Assert.Equal(0, result);
    }

    [Fact]
    public void HitTestOutputPort_OutsidePort_ReturnsMinusOne()
    {
        var node = new DialogueNode
        {
            Id = "test",
            NextNodeId = "next",
            EditorX = 100,
            EditorY = 100,
        };

        var widget = DialogueNodeWidget.FromNode(node, 0);
        int result = widget.HitTestOutputPort(0, 0);

        Assert.Equal(-1, result);
    }

    [Fact]
    public void HitTestInputPort_InsidePort_ReturnsTrue()
    {
        var node = new DialogueNode
        {
            Id = "test",
            EditorX = 100,
            EditorY = 100,
        };

        var widget = DialogueNodeWidget.FromNode(node, 0);
        var portCenter = widget.GetInputPortCenter();
        bool result = widget.HitTestInputPort((int)portCenter.X, (int)portCenter.Y);

        Assert.True(result);
    }

    [Fact]
    public void HitTestBody_InsideNode_ReturnsTrue()
    {
        var node = new DialogueNode
        {
            Id = "test",
            EditorX = 100,
            EditorY = 100,
        };

        var widget = DialogueNodeWidget.FromNode(node, 0);
        bool result = widget.HitTestBody(150, 120);

        Assert.True(result);
    }

    [Fact]
    public void HitTestBody_OutsideNode_ReturnsFalse()
    {
        var node = new DialogueNode
        {
            Id = "test",
            EditorX = 100,
            EditorY = 100,
        };

        var widget = DialogueNodeWidget.FromNode(node, 0);
        bool result = widget.HitTestBody(0, 0);

        Assert.False(result);
    }

    [Fact]
    public void BoundsHeight_ScalesWithChoiceCount()
    {
        var nodeNoChoices = new DialogueNode { Id = "a", EditorX = 0, EditorY = 0 };
        var nodeWithChoices = new DialogueNode
        {
            Id = "b",
            EditorX = 0,
            EditorY = 0,
            Choices = new List<DialogueChoice>
            {
                new() { Text = "A" },
                new() { Text = "B" },
                new() { Text = "C" },
                new() { Text = "D" },
            }
        };

        var w1 = DialogueNodeWidget.FromNode(nodeNoChoices, 0);
        var w2 = DialogueNodeWidget.FromNode(nodeWithChoices, 1);

        Assert.True(w2.Bounds.Height > w1.Bounds.Height);
    }
}
