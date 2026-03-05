using System.Collections.Generic;
using TileForge.Data;
using TileForge.Game;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class ProjectContextTests
{
    [Fact]
    public void ProjectDirectory_NullPath_ReturnsNull()
    {
        var ctx = new ProjectContext(() => null);
        Assert.Null(ctx.ProjectDirectory);
    }

    [Fact]
    public void GetAvailableMaps_NullPath_ReturnsCreateNewOnly()
    {
        var ctx = new ProjectContext(() => null);
        var maps = ctx.GetAvailableMaps();
        Assert.Single(maps);
        Assert.Equal(ProjectContext.CreateNewItem, maps[0]);
    }

    [Fact]
    public void GetAvailableDialogues_NullPath_ReturnsCreateNewOnly()
    {
        var ctx = new ProjectContext(() => null);
        var dialogues = ctx.GetAvailableDialogues();
        Assert.Single(dialogues);
        Assert.Equal(ProjectContext.CreateNewItem, dialogues[0]);
    }

    [Fact]
    public void GetKnownFlags_NullQuests_ReturnsEmpty()
    {
        var ctx = new ProjectContext(() => null);
        var flags = ctx.GetKnownFlags(null, null);
        Assert.Empty(flags);
    }

    [Fact]
    public void GetKnownFlags_ExtractsFromQuests()
    {
        var ctx = new ProjectContext(() => null);
        var quests = new List<QuestDefinition>
        {
            new()
            {
                Id = "q1",
                StartFlag = "quest_started",
                CompletionFlag = "quest_done",
                Objectives = new List<QuestObjective>
                {
                    new() { Type = "flag", Flag = "killed_boss" }
                },
                Rewards = new QuestRewards
                {
                    SetFlags = new List<string> { "reward_flag" }
                }
            }
        };
        var flags = ctx.GetKnownFlags(quests, null);
        Assert.Contains("quest_started", flags);
        Assert.Contains("quest_done", flags);
        Assert.Contains("killed_boss", flags);
        Assert.Contains("reward_flag", flags);
    }

    [Fact]
    public void GetKnownFlags_ExtractsFromGroups()
    {
        var ctx = new ProjectContext(() => null);
        var groups = new List<TileGroup>
        {
            new()
            {
                Name = "goblin",
                DefaultProperties = new Dictionary<string, string>
                {
                    { "on_kill_set_flag", "goblin_killed" },
                    { "on_collect_set_flag", "item_collected" }
                }
            }
        };
        var flags = ctx.GetKnownFlags(null, groups);
        Assert.Contains("goblin_killed", flags);
        Assert.Contains("item_collected", flags);
    }

    [Fact]
    public void GetKnownFlags_DeduplicatesFlags()
    {
        var ctx = new ProjectContext(() => null);
        var quests = new List<QuestDefinition>
        {
            new() { Id = "q1", StartFlag = "shared_flag", CompletionFlag = "shared_flag" }
        };
        var flags = ctx.GetKnownFlags(quests, null);
        Assert.Single(flags);
        Assert.Equal("shared_flag", flags[0]);
    }

    [Fact]
    public void GetKnownVariables_NullQuests_ReturnsEmpty()
    {
        var ctx = new ProjectContext(() => null);
        var vars = ctx.GetKnownVariables(null, null);
        Assert.Empty(vars);
    }

    [Fact]
    public void GetKnownVariables_ExtractsFromQuests()
    {
        var ctx = new ProjectContext(() => null);
        var quests = new List<QuestDefinition>
        {
            new()
            {
                Id = "q1",
                Objectives = new List<QuestObjective>
                {
                    new() { Type = "var>=", Variable = "kill_count" }
                },
                Rewards = new QuestRewards
                {
                    SetVariables = new Dictionary<string, string> { { "gold", "100" } }
                }
            }
        };
        var vars = ctx.GetKnownVariables(quests, null);
        Assert.Contains("kill_count", vars);
        Assert.Contains("gold", vars);
    }

    [Fact]
    public void GetKnownVariables_ExtractsFromGroups()
    {
        var ctx = new ProjectContext(() => null);
        var groups = new List<TileGroup>
        {
            new()
            {
                Name = "rat",
                DefaultProperties = new Dictionary<string, string>
                {
                    { "on_kill_increment", "rats_killed" }
                }
            }
        };
        var vars = ctx.GetKnownVariables(null, groups);
        Assert.Contains("rats_killed", vars);
    }

    [Fact]
    public void GetKnownFlags_SkipsEmptyValues()
    {
        var ctx = new ProjectContext(() => null);
        var quests = new List<QuestDefinition>
        {
            new() { Id = "q1", StartFlag = "", CompletionFlag = "  " }
        };
        var flags = ctx.GetKnownFlags(quests, null);
        Assert.Empty(flags);
    }
}
