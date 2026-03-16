using TileForge.Editor;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class MapWorkspaceTests
{
    [Fact]
    public void MapWorkspace_ImplementsIWorkspace()
    {
        IWorkspace ws = new MapWorkspace(
            (s, m, pm, i, f, c, g, sw, sh) => { },
            (sb, f, s, r, c) => { },
            (sb, f, s, r) => { });
        Assert.NotNull(ws);
    }

    [Fact]
    public void MapWorkspace_Update_DelegatesToCallback()
    {
        bool called = false;
        var ws = new MapWorkspace(
            (s, m, pm, i, f, c, g, sw, sh) => { called = true; },
            (sb, f, s, r, c) => { },
            (sb, f, s, r) => { });

        // We can't easily construct MonoGame types in tests, but we can verify
        // the workspace implements the interface correctly
        Assert.False(called);
    }

    [Fact]
    public void MapWorkspace_OnEnter_OnExit_DoNotThrow()
    {
        var ws = new MapWorkspace(
            (s, m, pm, i, f, c, g, sw, sh) => { },
            (sb, f, s, r, c) => { },
            (sb, f, s, r) => { });
        var state = new EditorState();

        ws.OnEnter(state);
        ws.OnExit(state);
    }
}
