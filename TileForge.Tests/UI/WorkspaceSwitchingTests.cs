using TileForge.Editor;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class WorkspaceSwitchingTests
{
    [Fact]
    public void SwitchWorkspace_ChangesActiveWorkspace()
    {
        var state = new EditorState();
        Assert.Equal(WorkspaceMode.Map, state.ActiveWorkspace);

        state.ActiveWorkspace = WorkspaceMode.Dialogues;
        Assert.Equal(WorkspaceMode.Dialogues, state.ActiveWorkspace);
    }

    [Fact]
    public void SwitchWorkspace_FiresEvent()
    {
        var state = new EditorState();
        WorkspaceMode? received = null;
        state.WorkspaceChanged += m => received = m;

        state.ActiveWorkspace = WorkspaceMode.Quests;
        Assert.Equal(WorkspaceMode.Quests, received);
    }

    [Fact]
    public void SwitchWorkspace_SameMode_NoEvent()
    {
        var state = new EditorState();
        int count = 0;
        state.WorkspaceChanged += _ => count++;

        state.ActiveWorkspace = WorkspaceMode.Map; // same as default
        Assert.Equal(0, count);
    }

    [Fact]
    public void WorkspaceMode_EnumValues_Correct()
    {
        Assert.Equal(0, (int)WorkspaceMode.Map);
        Assert.Equal(1, (int)WorkspaceMode.Dialogues);
        Assert.Equal(2, (int)WorkspaceMode.Quests);
        Assert.Equal(3, (int)WorkspaceMode.WorldMap);
    }

    [Fact]
    public void DialogueWorkspace_OnExit_Called_ClearsState()
    {
        var ws = new DialogueWorkspace(() => null, () => null, _ => { }, _ => { }, _ => { });
        var state = new EditorState();
        ws.OnEnter(state);
        ws.OnExit(state);
        Assert.False(ws.IsEditorActive);
    }

    [Fact]
    public void QuestWorkspace_OnExit_Called_ClearsState()
    {
        var ws = new QuestWorkspace(_ => { }, (_, __) => { });
        var state = new EditorState();
        ws.OnEnter(state);
        ws.OnExit(state);
        Assert.False(ws.IsEditorActive);
    }
}
