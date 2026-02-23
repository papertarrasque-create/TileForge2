using TileForge.Editor;
using Xunit;

namespace TileForge.Tests.Editor;

/// <summary>
/// A simple testable ICommand that tracks Execute/Undo call counts.
/// </summary>
public class MockCommand : ICommand
{
    public int ExecuteCount { get; private set; }
    public int UndoCount { get; private set; }

    public void Execute() => ExecuteCount++;
    public void Undo() => UndoCount++;
}

public class UndoStackTests
{
    [Fact]
    public void Push_AddsCommand_CanUndoIsTrue()
    {
        var stack = new UndoStack();
        var cmd = new MockCommand();

        stack.Push(cmd);

        Assert.True(stack.CanUndo);
    }

    [Fact]
    public void Undo_ReversesLastCommand()
    {
        var stack = new UndoStack();
        var cmd = new MockCommand();
        stack.Push(cmd);

        stack.Undo();

        Assert.Equal(1, cmd.UndoCount);
    }

    [Fact]
    public void Undo_MultipleCommands_ReversesInOrder()
    {
        var stack = new UndoStack();
        var cmd1 = new MockCommand();
        var cmd2 = new MockCommand();
        stack.Push(cmd1);
        stack.Push(cmd2);

        stack.Undo(); // Should undo cmd2
        Assert.Equal(1, cmd2.UndoCount);
        Assert.Equal(0, cmd1.UndoCount);

        stack.Undo(); // Should undo cmd1
        Assert.Equal(1, cmd1.UndoCount);
    }

    [Fact]
    public void Redo_ReExecutesUndoneCommand()
    {
        var stack = new UndoStack();
        var cmd = new MockCommand();
        stack.Push(cmd);

        stack.Undo();
        stack.Redo();

        Assert.Equal(1, cmd.ExecuteCount);
    }

    [Fact]
    public void Redo_AfterMultipleUndos_ReExecutesInOrder()
    {
        var stack = new UndoStack();
        var cmd1 = new MockCommand();
        var cmd2 = new MockCommand();
        stack.Push(cmd1);
        stack.Push(cmd2);

        stack.Undo(); // undo cmd2
        stack.Undo(); // undo cmd1

        stack.Redo(); // redo cmd1
        Assert.Equal(1, cmd1.ExecuteCount);
        Assert.Equal(0, cmd2.ExecuteCount);

        stack.Redo(); // redo cmd2
        Assert.Equal(1, cmd2.ExecuteCount);
    }

    [Fact]
    public void Push_ClearsRedoStack()
    {
        var stack = new UndoStack();
        var cmd1 = new MockCommand();
        var cmd2 = new MockCommand();
        stack.Push(cmd1);

        stack.Undo(); // Move cmd1 to redo stack
        Assert.True(stack.CanRedo);

        stack.Push(cmd2); // Should clear redo stack

        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void Undo_OnEmptyStack_DoesNothing()
    {
        var stack = new UndoStack();

        var exception = Record.Exception(() => stack.Undo());

        Assert.Null(exception);
    }

    [Fact]
    public void Redo_OnEmptyStack_DoesNothing()
    {
        var stack = new UndoStack();

        var exception = Record.Exception(() => stack.Redo());

        Assert.Null(exception);
    }

    [Fact]
    public void CanUndo_EmptyStack_ReturnsFalse()
    {
        var stack = new UndoStack();

        Assert.False(stack.CanUndo);
    }

    [Fact]
    public void CanRedo_EmptyStack_ReturnsFalse()
    {
        var stack = new UndoStack();

        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void CanUndo_AfterPush_ReturnsTrue()
    {
        var stack = new UndoStack();
        stack.Push(new MockCommand());

        Assert.True(stack.CanUndo);
    }

    [Fact]
    public void CanRedo_AfterUndo_ReturnsTrue()
    {
        var stack = new UndoStack();
        stack.Push(new MockCommand());
        stack.Undo();

        Assert.True(stack.CanRedo);
    }

    [Fact]
    public void CanUndo_AfterUndoAll_ReturnsFalse()
    {
        var stack = new UndoStack();
        stack.Push(new MockCommand());
        stack.Undo();

        Assert.False(stack.CanUndo);
    }

    [Fact]
    public void CanRedo_AfterRedoAll_ReturnsFalse()
    {
        var stack = new UndoStack();
        stack.Push(new MockCommand());
        stack.Undo();
        stack.Redo();

        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void Clear_ResetsUndoAndRedo()
    {
        var stack = new UndoStack();
        stack.Push(new MockCommand());
        stack.Push(new MockCommand());
        stack.Undo();

        stack.Clear();

        Assert.False(stack.CanUndo);
        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void UndoRedo_FullCycle_CommandCallCountsCorrect()
    {
        var stack = new UndoStack();
        var cmd = new MockCommand();

        stack.Push(cmd);       // push (no execute call from Push itself)
        stack.Undo();          // undo count = 1
        stack.Redo();          // execute count = 1
        stack.Undo();          // undo count = 2
        stack.Redo();          // execute count = 2

        Assert.Equal(2, cmd.ExecuteCount);
        Assert.Equal(2, cmd.UndoCount);
    }

    [Fact]
    public void Undo_MovesCommandToRedoStack()
    {
        var stack = new UndoStack();
        var cmd = new MockCommand();
        stack.Push(cmd);

        stack.Undo();

        Assert.False(stack.CanUndo);
        Assert.True(stack.CanRedo);
    }

    [Fact]
    public void Redo_MovesCommandBackToUndoStack()
    {
        var stack = new UndoStack();
        var cmd = new MockCommand();
        stack.Push(cmd);
        stack.Undo();

        stack.Redo();

        Assert.True(stack.CanUndo);
        Assert.False(stack.CanRedo);
    }
}
