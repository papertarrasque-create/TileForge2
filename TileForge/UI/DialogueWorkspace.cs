using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Game;

namespace TileForge.UI;

/// <summary>
/// Workspace for editing dialogues. Shows a DialogueListPanel on the left sidebar
/// and hosts a DialogueTreeEditor in the canvas area.
/// </summary>
public class DialogueWorkspace : IWorkspace
{
    private readonly DialogueListPanel _listPanel = new();
    private DialogueTreeEditor _editor;
    private KeyboardState _prevKeyboard;
    private GameTime _cachedGameTime;
    private string _lastSaveWarning;
    private float _warningTimer;

    // Dependencies injected from TileForgeGame
    private readonly Func<IProjectContext> _getProjectContext;
    private readonly Func<string> _getProjectDir;
    private readonly Action<DialogueData> _saveDialogue;
    private readonly Action<string> _deleteDialogueFile;
    private readonly Action<Action<string>> _showConfirmDelete;

    /// <summary>Whether this workspace has an active modal editor that should capture input.</summary>
    public bool IsEditorActive => _editor != null;

    /// <summary>The active editor, if any (for text input routing).</summary>
    public DialogueTreeEditor ActiveEditor => _editor;

    public DialogueWorkspace(
        Func<IProjectContext> getProjectContext,
        Func<string> getProjectDir,
        Action<DialogueData> saveDialogue,
        Action<string> deleteDialogueFile,
        Action<Action<string>> showConfirmDelete)
    {
        _getProjectContext = getProjectContext;
        _getProjectDir = getProjectDir;
        _saveDialogue = saveDialogue;
        _deleteDialogueFile = deleteDialogueFile;
        _showConfirmDelete = showConfirmDelete;
    }

    public void OnTextInput(char character)
    {
        _editor?.OnTextInput(character);
    }

    public void Update(EditorState state, MouseState mouse, MouseState prevMouse,
                       InputEvent input, SpriteFont font, Rectangle canvasBounds,
                       GameTime gameTime, int screenW, int screenH)
    {
        _cachedGameTime = gameTime;
        var keyboard = Keyboard.GetState();

        // Update list panel
        var sidebarBounds = ComputeSidebarBounds(screenH);
        _listPanel.Update(state, mouse, prevMouse, font, sidebarBounds, gameTime, screenW, screenH);

        // Handle list panel signals
        HandleListPanelSignals(state);

        // Update active editor
        if (_editor != null)
        {
            _editor.Update(mouse, prevMouse, keyboard, _prevKeyboard,
                canvasBounds, state.Dialogues, font, screenW, screenH, gameTime,
                _getProjectContext(), state.Quests, state.Groups);

            if (_editor.IsComplete)
            {
                HandleEditorResult(state);
                _editor = null;
            }
        }

        _prevKeyboard = keyboard;

        if (_warningTimer > 0)
        {
            _warningTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_warningTimer <= 0)
                _lastSaveWarning = null;
        }
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                     Renderer renderer, Rectangle canvasBounds)
    {
        if (_editor != null)
        {
            _editor.Draw(spriteBatch, font, renderer, canvasBounds, _cachedGameTime);
        }
        else
        {
            // Draw hint text when no editor is open
            renderer.DrawRect(spriteBatch, canvasBounds, LayoutConstants.CanvasBackground);
            string hint = "Double-click a dialogue to edit, or click '+ Add Dialogue'";
            var hintSize = font.MeasureString(hint);
            spriteBatch.DrawString(font, hint,
                new Vector2(canvasBounds.X + (canvasBounds.Width - hintSize.X) / 2,
                            canvasBounds.Y + (canvasBounds.Height - hintSize.Y) / 2),
                LayoutConstants.DialogueTreeHintColor);
        }

        if (!string.IsNullOrEmpty(_lastSaveWarning))
        {
            spriteBatch.DrawString(font, _lastSaveWarning,
                new Vector2(canvasBounds.X + 8, canvasBounds.Bottom - 24),
                Color.Orange);
        }
    }

    public void DrawSidebar(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                            Renderer renderer, Rectangle sidebarBounds)
    {
        _listPanel.Draw(spriteBatch, font, state, renderer, sidebarBounds);
    }

    public void OnEnter(EditorState state) { }

    public void OnExit(EditorState state)
    {
        // Close any open editor without saving
        _editor = null;
    }

    private void HandleListPanelSignals(EditorState state)
    {
        if (_listPanel.WantsNew)
        {
            _editor = DialogueTreeEditor.ForNewDialogue();
        }
        else if (_listPanel.WantsEditIndex >= 0)
        {
            int idx = _listPanel.WantsEditIndex;
            if (idx < state.Dialogues.Count)
                _editor = DialogueTreeEditor.ForExistingDialogue(state.Dialogues[idx]);
        }
        else if (_listPanel.WantsDeleteIndex >= 0)
        {
            int idx = _listPanel.WantsDeleteIndex;
            if (idx < state.Dialogues.Count)
            {
                string name = state.Dialogues[idx].Id;
                int capturedIdx = idx;
                _showConfirmDelete(confirmedName =>
                {
                    if (capturedIdx < state.Dialogues.Count)
                    {
                        string deletedId = state.Dialogues[capturedIdx].Id;
                        state.Dialogues.RemoveAt(capturedIdx);
                        _deleteDialogueFile(deletedId);
                        state.NotifyDialoguesChanged();
                    }
                });
            }
        }
    }

    private void HandleEditorResult(EditorState state)
    {
        if (_editor.WasCancelled || _editor.Result == null) return;
        var result = _editor.Result;

        if (_editor.IsNew)
        {
            state.Dialogues.Add(result);
        }
        else
        {
            string origId = _editor.OriginalId;
            int idx = state.Dialogues.FindIndex(d => d.Id == origId);
            if (idx >= 0)
            {
                if (result.Id != origId)
                    _deleteDialogueFile(origId);
                state.Dialogues[idx] = result;
            }
            else
            {
                state.Dialogues.Add(result);
            }
        }

        _saveDialogue(result);
        state.NotifyDialoguesChanged();

        // Validate cross-references after save
        var brokenRefs = CrossReferenceValidator.FindBrokenQuestReferences(
            new List<DialogueData> { result }, state.Quests);
        if (brokenRefs.Count > 0)
        {
            _lastSaveWarning = $"Warning: {brokenRefs.Count} broken quest reference(s)";
            _warningTimer = 5f;
        }
        else
        {
            _lastSaveWarning = null;
        }
    }

    private static Rectangle ComputeSidebarBounds(int screenH)
    {
        int topOffset = LayoutConstants.TopChromeHeight;
        return new Rectangle(0, topOffset, LayoutConstants.PanelDockWidth,
                             screenH - topOffset - LayoutConstants.StatusBarHeight);
    }
}
