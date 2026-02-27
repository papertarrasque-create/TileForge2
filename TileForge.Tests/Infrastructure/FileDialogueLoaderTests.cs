using System.IO;
using System.Text.Json;
using Xunit;
using TileForge.Game;
using TileForge.Infrastructure;
using TileForge.Tests.Helpers;

namespace TileForge.Tests.Infrastructure;

public class FileDialogueLoaderTests
{
    private readonly MockFileSystem _fs;

    public FileDialogueLoaderTests()
    {
        _fs = new MockFileSystem();
    }

    [Fact]
    public void LoadDialogue_DirectPath_ReturnsData()
    {
        var dialogue = new DialogueData { Id = "greeting" };
        string json = JsonSerializer.Serialize(dialogue);
        string path = Path.Combine("/project", "greeting.json");
        _fs.AddFile(path, json);

        var loader = new FileDialogueLoader("/project", _fs);
        var result = loader.LoadDialogue("greeting");

        Assert.NotNull(result);
        Assert.Equal("greeting", result.Id);
    }

    [Fact]
    public void LoadDialogue_DialoguesSubdir_ReturnsData()
    {
        var dialogue = new DialogueData { Id = "elder" };
        string json = JsonSerializer.Serialize(dialogue);
        string path = Path.Combine("/project", "dialogues", "elder.json");
        _fs.AddFile(path, json);

        var loader = new FileDialogueLoader("/project", _fs);
        var result = loader.LoadDialogue("elder");

        Assert.NotNull(result);
        Assert.Equal("elder", result.Id);
    }

    [Fact]
    public void LoadDialogue_NotFound_ReturnsNull()
    {
        var loader = new FileDialogueLoader("/project", _fs);
        var result = loader.LoadDialogue("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public void LoadDialogue_InvalidJson_ReturnsNull()
    {
        string path = Path.Combine("/project", "bad.json");
        _fs.AddFile(path, "not valid json {{{");

        var loader = new FileDialogueLoader("/project", _fs);
        var result = loader.LoadDialogue("bad");

        Assert.Null(result);
    }

    [Fact]
    public void LoadDialogue_NullBasePath_ReturnsNull()
    {
        var loader = new FileDialogueLoader(null, _fs);
        var result = loader.LoadDialogue("anything");

        Assert.Null(result);
    }

    [Fact]
    public void LoadDialogue_EmptyBasePath_ReturnsNull()
    {
        var loader = new FileDialogueLoader("", _fs);
        var result = loader.LoadDialogue("anything");

        Assert.Null(result);
    }

    [Fact]
    public void LoadDialogue_PrefersDirectPathOverSubdir()
    {
        var directDialogue = new DialogueData { Id = "direct_version" };
        var subdirDialogue = new DialogueData { Id = "subdir_version" };
        _fs.AddFile(Path.Combine("/project", "test.json"),
            JsonSerializer.Serialize(directDialogue));
        _fs.AddFile(Path.Combine("/project", "dialogues", "test.json"),
            JsonSerializer.Serialize(subdirDialogue));

        var loader = new FileDialogueLoader("/project", _fs);
        var result = loader.LoadDialogue("test");

        Assert.Equal("direct_version", result.Id);
    }
}

public class MockDialogueLoaderTests
{
    [Fact]
    public void ReturnsConfiguredDialogue()
    {
        var mock = new MockDialogueLoader();
        mock.Dialogues["greet"] = new DialogueData { Id = "greet" };

        var result = mock.LoadDialogue("greet");

        Assert.NotNull(result);
        Assert.Equal("greet", result.Id);
    }

    [Fact]
    public void UnknownRef_ReturnsNull()
    {
        var mock = new MockDialogueLoader();

        Assert.Null(mock.LoadDialogue("unknown"));
    }
}
