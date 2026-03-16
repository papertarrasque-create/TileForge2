using TileForge.Editor;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class WorkspaceTests
{
    [Fact]
    public void ActiveWorkspace_DefaultsToMap()
    {
        var state = new EditorState();
        Assert.Equal(WorkspaceMode.Map, state.ActiveWorkspace);
    }

    [Fact]
    public void ActiveWorkspace_RaisesEvent_OnChange()
    {
        var state = new EditorState();
        WorkspaceMode? received = null;
        state.WorkspaceChanged += mode => received = mode;

        state.ActiveWorkspace = WorkspaceMode.Dialogues;

        Assert.Equal(WorkspaceMode.Dialogues, received);
    }

    [Fact]
    public void ActiveWorkspace_DoesNotRaiseEvent_WhenSameValue()
    {
        var state = new EditorState();
        int callCount = 0;
        state.WorkspaceChanged += _ => callCount++;

        state.ActiveWorkspace = WorkspaceMode.Map; // same as default

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void ActiveWorkspace_SwitchToQuests_ThenBack()
    {
        var state = new EditorState();
        state.ActiveWorkspace = WorkspaceMode.Quests;
        Assert.Equal(WorkspaceMode.Quests, state.ActiveWorkspace);

        state.ActiveWorkspace = WorkspaceMode.Map;
        Assert.Equal(WorkspaceMode.Map, state.ActiveWorkspace);
    }
}
