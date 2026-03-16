using System.Collections.Generic;
using TileForge.Game;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class ConditionListEditorTests
{
    [Fact]
    public void FromConditions_Null_CreatesEmptyList()
    {
        var editor = new ConditionListEditor();
        editor.LoadFromConditions(null);
        Assert.Empty(editor.ToConditions());
    }

    [Fact]
    public void FromConditions_EmptyList_CreatesEmptyList()
    {
        var editor = new ConditionListEditor();
        editor.LoadFromConditions(new List<Condition>());
        Assert.Empty(editor.ToConditions());
    }

    [Fact]
    public void FromConditions_HasFlag_RoundTrips()
    {
        var conditions = new List<Condition>
        {
            new() { Type = "has_flag", Flag = "talked_to_npc" }
        };
        var editor = new ConditionListEditor();
        editor.LoadFromConditions(conditions);
        var result = editor.ToConditions();
        Assert.Single(result);
        Assert.Equal("has_flag", result[0].Type);
        Assert.Equal("talked_to_npc", result[0].Flag);
    }

    [Fact]
    public void FromConditions_VariableEq_UsesSeparateFields()
    {
        var conditions = new List<Condition>
        {
            new() { Type = "variable_eq", Variable = "quest_stage", Value = "3" }
        };
        var editor = new ConditionListEditor();
        editor.LoadFromConditions(conditions);
        var result = editor.ToConditions();
        Assert.Single(result);
        Assert.Equal("variable_eq", result[0].Type);
        Assert.Equal("quest_stage", result[0].Variable);
        Assert.Equal("3", result[0].Value);
    }

    [Fact]
    public void FromConditions_MultipleConditions_PreservesOrder()
    {
        var conditions = new List<Condition>
        {
            new() { Type = "has_flag", Flag = "first" },
            new() { Type = "has_item", Item = "sword" },
            new() { Type = "not_flag", Flag = "done" },
        };
        var editor = new ConditionListEditor();
        editor.LoadFromConditions(conditions);
        var result = editor.ToConditions();
        Assert.Equal(3, result.Count);
        Assert.Equal("has_flag", result[0].Type);
        Assert.Equal("has_item", result[1].Type);
        Assert.Equal("not_flag", result[2].Type);
    }

    [Fact]
    public void AddCondition_AppendsDefault()
    {
        var editor = new ConditionListEditor();
        editor.LoadFromConditions(new List<Condition>());
        editor.AddCondition();
        var result = editor.ToConditions();
        Assert.Single(result);
        Assert.Equal("has_flag", result[0].Type);
    }

    [Fact]
    public void RemoveConditionAt_RemovesCorrectIndex()
    {
        var conditions = new List<Condition>
        {
            new() { Type = "has_flag", Flag = "a" },
            new() { Type = "has_flag", Flag = "b" },
            new() { Type = "has_flag", Flag = "c" },
        };
        var editor = new ConditionListEditor();
        editor.LoadFromConditions(conditions);
        editor.RemoveConditionAt(1);
        var result = editor.ToConditions();
        Assert.Equal(2, result.Count);
        Assert.Equal("a", result[0].Flag);
        Assert.Equal("c", result[1].Flag);
    }

    [Fact]
    public void QuestActive_RoundTrips()
    {
        var conditions = new List<Condition>
        {
            new() { Type = "quest_active", Value = "cave_quest" }
        };
        var editor = new ConditionListEditor();
        editor.LoadFromConditions(conditions);
        var result = editor.ToConditions();
        Assert.Single(result);
        Assert.Equal("quest_active", result[0].Type);
        Assert.Equal("cave_quest", result[0].Value);
    }
}
