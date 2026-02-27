using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework.Input;
using Xunit;
using TileForge.Game;
using TileForge.Tests.Helpers;

namespace TileForge.Tests.Infrastructure;

public class GameInputManagerFileTests
{
    private readonly MockFileSystem _fs;
    private readonly GameInputManager _manager;

    public GameInputManagerFileTests()
    {
        _fs = new MockFileSystem();
        _manager = new GameInputManager(_fs);
    }

    [Fact]
    public void SaveBindings_WritesToFileSystem()
    {
        string path = Path.Combine("/mock", "keybindings.json");

        _manager.SaveBindings(path);

        Assert.True(_fs.Exists(path));
    }

    [Fact]
    public void SaveBindings_CreatesDirectory()
    {
        string dir = "/mock/settings";
        string path = Path.Combine(dir, "keybindings.json");

        _manager.SaveBindings(path);

        Assert.True(_fs.DirectoryExists(dir));
    }

    [Fact]
    public void SaveAndLoadBindings_RoundTrips()
    {
        string path = "/mock/keybindings.json";

        _manager.RebindAction(GameAction.MoveUp, Keys.W);
        _manager.SaveBindings(path);

        var manager2 = new GameInputManager(_fs);
        manager2.LoadBindings(path);

        var bindings = manager2.GetBindings();
        Assert.Single(bindings[GameAction.MoveUp]);
        Assert.Equal(Keys.W, bindings[GameAction.MoveUp][0]);
    }

    [Fact]
    public void LoadBindings_MissingFile_KeepsDefaults()
    {
        _manager.LoadBindings("/mock/nonexistent.json");

        var bindings = _manager.GetBindings();
        Assert.Contains(Keys.Up, bindings[GameAction.MoveUp]);
    }

    [Fact]
    public void LoadBindings_CorruptFile_KeepsDefaults()
    {
        string path = "/mock/keybindings.json";
        _fs.AddFile(path, "not valid json {{{");

        _manager.LoadBindings(path);

        var bindings = _manager.GetBindings();
        Assert.Contains(Keys.Up, bindings[GameAction.MoveUp]);
    }
}
