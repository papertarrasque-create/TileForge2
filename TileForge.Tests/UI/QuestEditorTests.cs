using System.Collections.Generic;
using TileForge.Game;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class QuestEditorTests
{
    // ---- ParseRewardFlags ----

    [Fact]
    public void ParseRewardFlags_CommaSeparated_ReturnsList()
    {
        var result = QuestEditor.ParseRewardFlags("flag_a, flag_b, flag_c");
        Assert.Equal(3, result.Count);
        Assert.Equal("flag_a", result[0]);
        Assert.Equal("flag_b", result[1]);
        Assert.Equal("flag_c", result[2]);
    }

    [Fact]
    public void ParseRewardFlags_Empty_ReturnsEmptyList()
    {
        var result = QuestEditor.ParseRewardFlags("");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseRewardFlags_Whitespace_ReturnsEmptyList()
    {
        var result = QuestEditor.ParseRewardFlags("   ");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseRewardFlags_Null_ReturnsEmptyList()
    {
        var result = QuestEditor.ParseRewardFlags(null);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseRewardFlags_TrailingComma_IgnoresEmpty()
    {
        var result = QuestEditor.ParseRewardFlags("flag_a, ");
        Assert.Single(result);
        Assert.Equal("flag_a", result[0]);
    }

    [Fact]
    public void ParseRewardFlags_SingleFlag_ReturnsSingle()
    {
        var result = QuestEditor.ParseRewardFlags("only_flag");
        Assert.Single(result);
        Assert.Equal("only_flag", result[0]);
    }

    // ---- ParseRewardVariables ----

    [Fact]
    public void ParseRewardVariables_KeyValuePairs_ReturnsDictionary()
    {
        var result = QuestEditor.ParseRewardVariables("gold=100, rep=5");
        Assert.Equal(2, result.Count);
        Assert.Equal("100", result["gold"]);
        Assert.Equal("5", result["rep"]);
    }

    [Fact]
    public void ParseRewardVariables_Empty_ReturnsEmpty()
    {
        var result = QuestEditor.ParseRewardVariables("");
        Assert.Empty(result);
    }

    [Fact]
    public void ParseRewardVariables_Null_ReturnsEmpty()
    {
        var result = QuestEditor.ParseRewardVariables(null);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseRewardVariables_NoEquals_SkipsEntry()
    {
        var result = QuestEditor.ParseRewardVariables("gold=100, invalid, rep=5");
        Assert.Equal(2, result.Count);
        Assert.Equal("100", result["gold"]);
        Assert.Equal("5", result["rep"]);
    }

    [Fact]
    public void ParseRewardVariables_SingleEntry_Works()
    {
        var result = QuestEditor.ParseRewardVariables("score=42");
        Assert.Single(result);
        Assert.Equal("42", result["score"]);
    }

    // ---- FormatRewardFlags ----

    [Fact]
    public void FormatRewardFlags_ListToCommaSeparated()
    {
        var rewards = new List<DialogueAction>
        {
            new() { Type = "set_flag", Value = "a" },
            new() { Type = "set_flag", Value = "b" },
            new() { Type = "set_flag", Value = "c" },
        };
        Assert.Equal("a, b, c", QuestEditor.FormatRewardFlags(rewards));
    }

    [Fact]
    public void FormatRewardFlags_Empty_ReturnsEmptyString()
    {
        var rewards = new List<DialogueAction>();
        Assert.Equal("", QuestEditor.FormatRewardFlags(rewards));
    }

    [Fact]
    public void FormatRewardFlags_Null_ReturnsEmptyString()
    {
        Assert.Equal("", QuestEditor.FormatRewardFlags(null));
    }

    // ---- FormatRewardVariables ----

    [Fact]
    public void FormatRewardVariables_DictToKeyValueString()
    {
        var rewards = new List<DialogueAction>
        {
            new() { Type = "set_variable", Key = "gold", Value = "100" },
        };
        Assert.Equal("gold=100", QuestEditor.FormatRewardVariables(rewards));
    }

    [Fact]
    public void FormatRewardVariables_Empty_ReturnsEmptyString()
    {
        var rewards = new List<DialogueAction>();
        Assert.Equal("", QuestEditor.FormatRewardVariables(rewards));
    }

    [Fact]
    public void FormatRewardVariables_Null_ReturnsEmptyString()
    {
        Assert.Equal("", QuestEditor.FormatRewardVariables(null));
    }

    // ---- Round-trip format ----

    [Fact]
    public void FormatAndParseFlags_RoundTrip()
    {
        var original = new List<DialogueAction>
        {
            new() { Type = "set_flag", Value = "flag1" },
            new() { Type = "set_flag", Value = "flag2" },
            new() { Type = "set_flag", Value = "flag3" },
        };
        string text = QuestEditor.FormatRewardFlags(original);
        var parsed = QuestEditor.ParseRewardFlags(text);

        Assert.Equal(3, parsed.Count);
        Assert.Equal("flag1", parsed[0]);
        Assert.Equal("flag2", parsed[1]);
        Assert.Equal("flag3", parsed[2]);
    }

    [Fact]
    public void FormatAndParseVariables_RoundTrip()
    {
        var original = new List<DialogueAction>
        {
            new() { Type = "set_variable", Key = "gold", Value = "100" },
            new() { Type = "set_variable", Key = "rep", Value = "5" },
        };
        string text = QuestEditor.FormatRewardVariables(original);
        var parsed = QuestEditor.ParseRewardVariables(text);

        Assert.Equal(2, parsed.Count);
        Assert.Equal("100", parsed["gold"]);
        Assert.Equal("5", parsed["rep"]);
    }

    // ---- Factory methods ----

    [Fact]
    public void ForNewQuest_IsNew_True()
    {
        var editor = QuestEditor.ForNewQuest();
        Assert.True(editor.IsNew);
        Assert.False(editor.IsComplete);
        Assert.False(editor.WasCancelled);
        Assert.Null(editor.Result);
    }

    [Fact]
    public void ForExistingQuest_IsNew_False()
    {
        var quest = new QuestDefinition
        {
            Id = "test",
            Name = "Test Quest",
            Objectives = new List<QuestObjective>(),
        };
        var editor = QuestEditor.ForExistingQuest(quest);
        Assert.False(editor.IsNew);
        Assert.Equal("test", editor.OriginalId);
        Assert.False(editor.IsComplete);
    }
}
