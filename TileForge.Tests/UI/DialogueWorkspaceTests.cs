using TileForge.Editor;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class DialogueWorkspaceTests
{
    private static DialogueWorkspace CreateWorkspace()
    {
        return new DialogueWorkspace(
            () => null,
            () => null,
            _ => { },
            _ => { },
            _ => { });
    }

    [Fact]
    public void DialogueWorkspace_ImplementsIWorkspace()
    {
        IWorkspace ws = CreateWorkspace();
        Assert.NotNull(ws);
    }

    [Fact]
    public void DialogueWorkspace_IsEditorActive_FalseByDefault()
    {
        var ws = CreateWorkspace();
        Assert.False(ws.IsEditorActive);
    }

    [Fact]
    public void DialogueWorkspace_OnExit_ClearsEditor()
    {
        var ws = CreateWorkspace();
        var state = new EditorState();
        ws.OnExit(state);
        Assert.False(ws.IsEditorActive);
    }

    [Fact]
    public void DialogueWorkspace_OnEnter_DoesNotThrow()
    {
        var ws = CreateWorkspace();
        var state = new EditorState();
        ws.OnEnter(state);
    }
}
