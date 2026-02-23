using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using Xunit;

namespace TileForge.Tests;

/// <summary>
/// A minimal mock IDialog implementation for testing DialogManager lifecycle.
/// Does not depend on MonoGame rendering (Draw is a no-op).
/// </summary>
public class MockDialog : IDialog
{
    public bool IsComplete { get; set; }
    public bool WasCancelled { get; set; }

    public int UpdateCallCount { get; private set; }
    public List<char> TextInputReceived { get; } = new();

    /// <summary>
    /// If set, the dialog will mark itself complete after this many Update calls.
    /// </summary>
    public int? CompleteAfterUpdates { get; set; }

    public void Update(KeyboardState keyboard, KeyboardState prevKeyboard, GameTime gameTime)
    {
        UpdateCallCount++;
        if (CompleteAfterUpdates.HasValue && UpdateCallCount >= CompleteAfterUpdates.Value)
        {
            IsComplete = true;
        }
    }

    public void OnTextInput(char character)
    {
        TextInputReceived.Add(character);
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                     int screenWidth, int screenHeight, GameTime gameTime)
    {
        // No-op: drawing is not tested without a graphics device
    }
}

public class DialogManagerTests
{
    private static readonly KeyboardState EmptyKeyboard = new KeyboardState();
    private static readonly GameTime DummyGameTime = new GameTime();

    [Fact]
    public void IsActive_NoDialogShown_ReturnsFalse()
    {
        var manager = new DialogManager();

        Assert.False(manager.IsActive);
    }

    [Fact]
    public void Show_SetsIsActiveToTrue()
    {
        var manager = new DialogManager();
        var dialog = new MockDialog();

        manager.Show(dialog, _ => { });

        Assert.True(manager.IsActive);
    }

    [Fact]
    public void Update_WhenNoDialog_ReturnsFalse()
    {
        var manager = new DialogManager();

        bool result = manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime);

        Assert.False(result);
    }

    [Fact]
    public void Update_WhenDialogActive_ReturnsTrue()
    {
        var manager = new DialogManager();
        var dialog = new MockDialog();
        manager.Show(dialog, _ => { });

        bool result = manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime);

        Assert.True(result);
    }

    [Fact]
    public void Update_DelegatesToDialogUpdate()
    {
        var manager = new DialogManager();
        var dialog = new MockDialog();
        manager.Show(dialog, _ => { });

        manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime);

        Assert.Equal(1, dialog.UpdateCallCount);
    }

    [Fact]
    public void Update_WhenDialogCompletes_CallsCallback()
    {
        var manager = new DialogManager();
        var dialog = new MockDialog { CompleteAfterUpdates = 1 };
        IDialog callbackDialog = null;
        manager.Show(dialog, d => callbackDialog = d);

        manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime);

        Assert.Same(dialog, callbackDialog);
    }

    [Fact]
    public void Update_WhenDialogCompletes_ClearsActiveDialog()
    {
        var manager = new DialogManager();
        var dialog = new MockDialog { CompleteAfterUpdates = 1 };
        manager.Show(dialog, _ => { });

        manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime);

        Assert.False(manager.IsActive);
    }

    [Fact]
    public void Update_WhenDialogCompletes_NextUpdateReturnsFalse()
    {
        var manager = new DialogManager();
        var dialog = new MockDialog { CompleteAfterUpdates = 1 };
        manager.Show(dialog, _ => { });

        manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime); // completes dialog
        bool result = manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime); // no active dialog

        Assert.False(result);
    }

    [Fact]
    public void Update_DialogNotYetComplete_DoesNotCallCallback()
    {
        var manager = new DialogManager();
        var dialog = new MockDialog { CompleteAfterUpdates = 3 };
        bool callbackCalled = false;
        manager.Show(dialog, _ => callbackCalled = true);

        manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime); // update 1 of 3
        manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime); // update 2 of 3

        Assert.False(callbackCalled);
        Assert.True(manager.IsActive);
    }

    [Fact]
    public void Update_DialogCompletesOnThirdUpdate_CallsCallbackOnThird()
    {
        var manager = new DialogManager();
        var dialog = new MockDialog { CompleteAfterUpdates = 3 };
        bool callbackCalled = false;
        manager.Show(dialog, _ => callbackCalled = true);

        manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime);
        manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime);
        Assert.False(callbackCalled);

        manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime); // 3rd update completes
        Assert.True(callbackCalled);
    }

    [Fact]
    public void OnTextInput_WhenDialogActive_RoutesToDialog()
    {
        var manager = new DialogManager();
        var dialog = new MockDialog();
        manager.Show(dialog, _ => { });

        manager.OnTextInput('a');
        manager.OnTextInput('b');

        Assert.Equal(new List<char> { 'a', 'b' }, dialog.TextInputReceived);
    }

    [Fact]
    public void OnTextInput_WhenNoDialog_DoesNotThrow()
    {
        var manager = new DialogManager();

        var exception = Record.Exception(() => manager.OnTextInput('x'));

        Assert.Null(exception);
    }

    [Fact]
    public void Show_WhileDialogActive_PushesAndShowsNew()
    {
        var manager = new DialogManager();
        var dialog1 = new MockDialog();
        var dialog2 = new MockDialog();
        manager.Show(dialog1, _ => { });

        manager.Show(dialog2, _ => { });

        Assert.True(manager.IsActive);
        // Verify that updates now go to dialog2, not dialog1
        manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime);
        Assert.Equal(0, dialog1.UpdateCallCount);
        Assert.Equal(1, dialog2.UpdateCallCount);
    }

    [Fact]
    public void Show_NestedDialogCompletes_RestoresParent()
    {
        var manager = new DialogManager();
        var parentDialog = new MockDialog();
        var childDialog = new MockDialog { CompleteAfterUpdates = 1 };
        manager.Show(parentDialog, _ => { });

        // Nest a child dialog
        manager.Show(childDialog, _ => { });

        // Child completes — parent should be restored
        manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime);
        Assert.True(manager.IsActive); // Parent is still there

        // Verify parent receives updates
        manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime);
        Assert.Equal(1, parentDialog.UpdateCallCount);
    }

    [Fact]
    public void Show_NestedDialogCompletes_CallsChildCallbackOnly()
    {
        var manager = new DialogManager();
        var parentDialog = new MockDialog();
        var childDialog = new MockDialog { CompleteAfterUpdates = 1 };
        bool parentCallbackCalled = false;
        bool childCallbackCalled = false;
        manager.Show(parentDialog, _ => parentCallbackCalled = true);
        manager.Show(childDialog, _ => childCallbackCalled = true);

        // Child completes
        manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime);

        Assert.True(childCallbackCalled);
        Assert.False(parentCallbackCalled); // Parent callback NOT called
    }

    [Fact]
    public void Show_NestedDialogCompletes_ParentCompletesLater()
    {
        var manager = new DialogManager();
        var parentDialog = new MockDialog { CompleteAfterUpdates = 1 };
        var childDialog = new MockDialog { CompleteAfterUpdates = 1 };
        bool parentCallbackCalled = false;
        bool childCallbackCalled = false;
        manager.Show(parentDialog, _ => parentCallbackCalled = true);
        manager.Show(childDialog, _ => childCallbackCalled = true);

        // Child completes, parent restored
        manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime);
        Assert.True(childCallbackCalled);
        Assert.False(parentCallbackCalled);

        // Parent completes
        manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime);
        Assert.True(parentCallbackCalled);
        Assert.False(manager.IsActive);
    }

    [Fact]
    public void SequentialDialogs_FirstCompleteThenShowSecond_Works()
    {
        var manager = new DialogManager();
        var dialog1 = new MockDialog { CompleteAfterUpdates = 1 };
        var dialog2 = new MockDialog();
        IDialog completedDialog = null;

        // Show first dialog
        manager.Show(dialog1, d =>
        {
            completedDialog = d;
            // In the callback, show the next dialog
            manager.Show(dialog2, _ => { });
        });

        // First dialog completes, callback fires and shows second
        manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime);
        Assert.Same(dialog1, completedDialog);
        Assert.True(manager.IsActive); // dialog2 is now active

        // Verify dialog2 receives updates
        manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime);
        Assert.Equal(1, dialog2.UpdateCallCount);
    }

    [Fact]
    public void Update_NullCallback_DoesNotThrowWhenDialogCompletes()
    {
        var manager = new DialogManager();
        var dialog = new MockDialog { CompleteAfterUpdates = 1 };
        manager.Show(dialog, null!);

        var exception = Record.Exception(() => manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime));

        Assert.Null(exception);
        Assert.False(manager.IsActive);
    }

    [Fact]
    public void Update_ReturnsTrueEvenOnCompletionFrame()
    {
        // The Update method should return true on the frame the dialog completes,
        // because it was active when processing started.
        var manager = new DialogManager();
        var dialog = new MockDialog { CompleteAfterUpdates = 1 };
        manager.Show(dialog, _ => { });

        bool result = manager.Update(EmptyKeyboard, EmptyKeyboard, DummyGameTime);

        Assert.True(result); // Still returns true for the completion frame
        Assert.False(manager.IsActive); // But dialog is now cleared
    }
}
