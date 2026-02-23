using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;

namespace TileForge;

public class DialogManager
{
    private IDialog _activeDialog;
    private Action<IDialog> _onDialogComplete;

    // Stack for nested dialogs (e.g., file browser opened from new project dialog)
    private readonly Stack<(IDialog Dialog, Action<IDialog> OnComplete)> _dialogStack = new();

    public bool IsActive => _activeDialog != null;

    public void Show(IDialog dialog, Action<IDialog> onComplete)
    {
        // If a dialog is already active, push it onto the stack so it resumes when the new one closes
        if (_activeDialog != null)
            _dialogStack.Push((_activeDialog, _onDialogComplete));

        _activeDialog = dialog;
        _onDialogComplete = onComplete;
    }

    /// <summary>
    /// Updates the active dialog. Returns true if a dialog is active (caller should skip other input).
    /// </summary>
    public bool Update(KeyboardState keyboard, KeyboardState prevKeyboard, GameTime gameTime)
    {
        if (_activeDialog == null) return false;

        _activeDialog.Update(keyboard, prevKeyboard, gameTime);
        if (_activeDialog.IsComplete)
        {
            var dialog = _activeDialog;
            var callback = _onDialogComplete;

            // Restore parent dialog from stack, or clear
            if (_dialogStack.Count > 0)
            {
                var parent = _dialogStack.Pop();
                _activeDialog = parent.Dialog;
                _onDialogComplete = parent.OnComplete;
            }
            else
            {
                _activeDialog = null;
                _onDialogComplete = null;
            }

            callback?.Invoke(dialog);
        }

        return true;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                     int screenWidth, int screenHeight, GameTime gameTime)
    {
        if (_activeDialog != null)
            _activeDialog.Draw(spriteBatch, font, renderer, screenWidth, screenHeight, gameTime);
    }

    public void OnTextInput(char character)
    {
        _activeDialog?.OnTextInput(character);
    }
}
