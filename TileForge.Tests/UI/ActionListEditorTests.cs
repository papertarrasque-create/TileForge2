using System.Collections.Generic;
using TileForge.Game;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class ActionListEditorTests
{
    [Fact]
    public void FromActions_Null_CreatesEmptyList()
    {
        var editor = new ActionListEditor();
        editor.LoadFromActions(null);
        Assert.Empty(editor.ToActions());
    }

    [Fact]
    public void FromActions_SetFlag_RoundTrips()
    {
        var actions = new List<DialogueAction>
        {
            new() { Type = "set_flag", Value = "quest_accepted" }
        };
        var editor = new ActionListEditor();
        editor.LoadFromActions(actions);
        var result = editor.ToActions();
        Assert.Single(result);
        Assert.Equal("set_flag", result[0].Type);
        Assert.Equal("quest_accepted", result[0].Value);
    }

    [Fact]
    public void FromActions_SetVariable_RoundTrips()
    {
        var actions = new List<DialogueAction>
        {
            new() { Type = "set_variable", Key = "gold", Value = "100" }
        };
        var editor = new ActionListEditor();
        editor.LoadFromActions(actions);
        var result = editor.ToActions();
        Assert.Single(result);
        Assert.Equal("set_variable", result[0].Type);
        Assert.Equal("gold", result[0].Key);
        Assert.Equal("100", result[0].Value);
    }

    [Fact]
    public void FromActions_GiveItem_RoundTrips()
    {
        var actions = new List<DialogueAction>
        {
            new() { Type = "give_item", Value = "Potion" }
        };
        var editor = new ActionListEditor();
        editor.LoadFromActions(actions);
        var result = editor.ToActions();
        Assert.Single(result);
        Assert.Equal("give_item", result[0].Type);
        Assert.Equal("Potion", result[0].Value);
    }

    [Fact]
    public void FromActions_Log_PreservesColorAndText()
    {
        var actions = new List<DialogueAction>
        {
            new() { Type = "log", Text = "Quest accepted!", Color = "yellow" }
        };
        var editor = new ActionListEditor();
        editor.LoadFromActions(actions);
        var result = editor.ToActions();
        Assert.Single(result);
        Assert.Equal("log", result[0].Type);
        Assert.Equal("Quest accepted!", result[0].Text);
        Assert.Equal("yellow", result[0].Color);
    }

    [Fact]
    public void FromActions_MultipleActions_PreservesOrder()
    {
        var actions = new List<DialogueAction>
        {
            new() { Type = "set_flag", Value = "flag1" },
            new() { Type = "give_item", Value = "Sword" },
            new() { Type = "start_quest", Value = "main_quest" },
        };
        var editor = new ActionListEditor();
        editor.LoadFromActions(actions);
        var result = editor.ToActions();
        Assert.Equal(3, result.Count);
        Assert.Equal("set_flag", result[0].Type);
        Assert.Equal("give_item", result[1].Type);
        Assert.Equal("start_quest", result[2].Type);
    }

    [Fact]
    public void AddAction_AppendsDefault()
    {
        var editor = new ActionListEditor();
        editor.LoadFromActions(new List<DialogueAction>());
        editor.AddAction();
        var result = editor.ToActions();
        Assert.Single(result);
        Assert.Equal("set_flag", result[0].Type);
    }

    [Fact]
    public void RemoveActionAt_RemovesCorrectIndex()
    {
        var actions = new List<DialogueAction>
        {
            new() { Type = "set_flag", Value = "a" },
            new() { Type = "set_flag", Value = "b" },
            new() { Type = "set_flag", Value = "c" },
        };
        var editor = new ActionListEditor();
        editor.LoadFromActions(actions);
        editor.RemoveActionAt(1);
        var result = editor.ToActions();
        Assert.Equal(2, result.Count);
        Assert.Equal("a", result[0].Value);
        Assert.Equal("c", result[1].Value);
    }

    [Fact]
    public void HealAndDamage_RoundTrip()
    {
        var actions = new List<DialogueAction>
        {
            new() { Type = "heal", Value = "20" },
            new() { Type = "damage", Value = "5" },
        };
        var editor = new ActionListEditor();
        editor.LoadFromActions(actions);
        var result = editor.ToActions();
        Assert.Equal(2, result.Count);
        Assert.Equal("heal", result[0].Type);
        Assert.Equal("20", result[0].Value);
        Assert.Equal("damage", result[1].Type);
        Assert.Equal("5", result[1].Value);
    }
}
