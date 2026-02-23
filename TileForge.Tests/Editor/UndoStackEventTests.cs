using TileForge.Editor;
using Xunit;

namespace TileForge.Tests.Editor;

public class UndoStackEventTests
{
    [Fact]
    public void StateChanged_Fires_OnPush()
    {
        var stack = new UndoStack();
        bool fired = false;
        stack.StateChanged += () => fired = true;

        stack.Push(new MockCommand());

        Assert.True(fired);
    }

    [Fact]
    public void StateChanged_Fires_OnUndo()
    {
        var stack = new UndoStack();
        stack.Push(new MockCommand());

        bool fired = false;
        stack.StateChanged += () => fired = true;

        stack.Undo();

        Assert.True(fired);
    }

    [Fact]
    public void StateChanged_Fires_OnRedo()
    {
        var stack = new UndoStack();
        stack.Push(new MockCommand());
        stack.Undo();

        bool fired = false;
        stack.StateChanged += () => fired = true;

        stack.Redo();

        Assert.True(fired);
    }

    [Fact]
    public void StateChanged_Fires_OnClear()
    {
        var stack = new UndoStack();
        stack.Push(new MockCommand());

        bool fired = false;
        stack.StateChanged += () => fired = true;

        stack.Clear();

        Assert.True(fired);
    }

    [Fact]
    public void StateChanged_DoesNotFire_OnUndo_WhenStackEmpty()
    {
        var stack = new UndoStack();
        bool fired = false;
        stack.StateChanged += () => fired = true;

        stack.Undo();

        Assert.False(fired);
    }

    [Fact]
    public void StateChanged_DoesNotFire_OnRedo_WhenStackEmpty()
    {
        var stack = new UndoStack();
        bool fired = false;
        stack.StateChanged += () => fired = true;

        stack.Redo();

        Assert.False(fired);
    }

    [Fact]
    public void StateChanged_FiresAfterOperation_CanUndoReflectsNewState()
    {
        var stack = new UndoStack();
        bool canUndoInHandler = false;

        stack.StateChanged += () => canUndoInHandler = stack.CanUndo;

        stack.Push(new MockCommand());

        // After Push, CanUndo should be true when the handler runs
        Assert.True(canUndoInHandler);
    }
}
