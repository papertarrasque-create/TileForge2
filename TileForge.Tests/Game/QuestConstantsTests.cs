using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class QuestConstantsTests
{
    [Fact]
    public void StartedFlag_FormatsCorrectly()
    {
        Assert.Equal("quest_started:cave_quest", QuestConstants.StartedFlag("cave_quest"));
    }

    [Fact]
    public void CompleteFlag_FormatsCorrectly()
    {
        Assert.Equal("quest_complete:cave_quest", QuestConstants.CompleteFlag("cave_quest"));
    }

    [Fact]
    public void ObjectiveFlag_FormatsCorrectly()
    {
        Assert.Equal("objective_complete:find_sword", QuestConstants.ObjectiveFlag("find_sword"));
    }

    [Fact]
    public void Prefixes_MatchExpectedValues()
    {
        Assert.Equal("quest_started:", QuestConstants.StartedPrefix);
        Assert.Equal("quest_complete:", QuestConstants.CompletePrefix);
        Assert.Equal("objective_complete:", QuestConstants.ObjectivePrefix);
    }
}
