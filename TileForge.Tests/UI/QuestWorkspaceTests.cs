using TileForge.Editor;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class QuestWorkspaceTests
{
    private static QuestWorkspace CreateWorkspace()
    {
        return new QuestWorkspace(
            _ => { },
            (_, __) => { });
    }

    [Fact]
    public void QuestWorkspace_ImplementsIWorkspace()
    {
        IWorkspace ws = CreateWorkspace();
        Assert.NotNull(ws);
    }

    [Fact]
    public void QuestWorkspace_IsEditorActive_FalseByDefault()
    {
        var ws = CreateWorkspace();
        Assert.False(ws.IsEditorActive);
    }

    [Fact]
    public void QuestWorkspace_OnExit_ClearsEditor()
    {
        var ws = CreateWorkspace();
        var state = new EditorState();
        ws.OnExit(state);
        Assert.False(ws.IsEditorActive);
    }

    [Fact]
    public void QuestWorkspace_OnEnter_DoesNotThrow()
    {
        var ws = CreateWorkspace();
        var state = new EditorState();
        ws.OnEnter(state);
    }
}
