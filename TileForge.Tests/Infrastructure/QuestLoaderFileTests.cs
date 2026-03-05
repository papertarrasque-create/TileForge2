using Xunit;
using TileForge.Game;
using TileForge.Tests.Helpers;

namespace TileForge.Tests.Infrastructure;

public class QuestLoaderFileTests
{
    private readonly MockFileSystem _fs;

    public QuestLoaderFileTests()
    {
        _fs = new MockFileSystem();
    }

    [Fact]
    public void Load_WithFileSystem_ReadsQuests()
    {
        string path = "/mock/quests.json";
        _fs.AddFile(path, @"{
            ""quests"": [
                {
                    ""id"": ""q1"",
                    ""name"": ""Test Quest"",
                    ""objectives"": []
                }
            ]
        }");

        var quests = QuestLoader.Load(path, _fs);

        Assert.Single(quests);
        Assert.Equal("q1", quests[0].Id);
        Assert.Equal("Test Quest", quests[0].Name);
    }

    [Fact]
    public void Load_WithFileSystem_MissingFile_ReturnsEmpty()
    {
        var quests = QuestLoader.Load("/mock/nonexistent.json", _fs);

        Assert.Empty(quests);
    }

    [Fact]
    public void Load_WithFileSystem_NullPath_ReturnsEmpty()
    {
        var quests = QuestLoader.Load(null, _fs);

        Assert.Empty(quests);
    }

    [Fact]
    public void Load_WithFileSystem_InvalidJson_ReturnsEmpty()
    {
        string path = "/mock/quests.json";
        _fs.AddFile(path, "not json");

        var quests = QuestLoader.Load(path, _fs);

        Assert.Empty(quests);
    }

    [Fact]
    public void Load_WithFileSystem_SnakeCaseKeys_Works()
    {
        string path = "/mock/quests.json";
        _fs.AddFile(path, @"{
            ""quests"": [
                {
                    ""id"": ""q1"",
                    ""name"": ""Find Sword"",
                    ""start_flag"": ""quest_started"",
                    ""completion_flag"": ""quest_done"",
                    ""objectives"": [
                        {
                            ""description"": ""Find the sword"",
                            ""type"": ""flag"",
                            ""flag"": ""sword_found""
                        }
                    ]
                }
            ]
        }");

        var quests = QuestLoader.Load(path, _fs);

        Assert.Single(quests);
        Assert.Equal("quest_started", quests[0].StartFlag);
        Assert.Equal("quest_done", quests[0].CompletionFlag);
        Assert.Single(quests[0].Objectives);
        Assert.Equal("sword_found", quests[0].Objectives[0].Flag);
    }
}
