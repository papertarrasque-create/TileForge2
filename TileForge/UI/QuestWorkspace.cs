using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Editor;
using TileForge.Game;

namespace TileForge.UI;

/// <summary>
/// Workspace for editing quests. Shows a QuestListPanel on the left sidebar
/// and hosts a QuestEditor in the canvas area.
/// </summary>
public class QuestWorkspace : IWorkspace
{
    private readonly QuestListPanel _listPanel = new();
    private QuestEditor _editor;
    private KeyboardState _prevKeyboard;
    private GameTime _cachedGameTime;

    // Dependencies
    private readonly Action<List<QuestDefinition>> _saveQuests;
    private readonly Action<string, Action> _showConfirmDelete;

    /// <summary>Whether this workspace has an active modal editor.</summary>
    public bool IsEditorActive => _editor != null;

    /// <summary>The active editor, if any (for text input routing).</summary>
    public QuestEditor ActiveEditor => _editor;

    public QuestWorkspace(
        Action<List<QuestDefinition>> saveQuests,
        Action<string, Action> showConfirmDelete)
    {
        _saveQuests = saveQuests;
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
                canvasBounds, state.Quests, font, screenW, screenH, gameTime);

            if (_editor.IsComplete)
            {
                HandleEditorResult(state);
                _editor = null;
            }
        }

        _prevKeyboard = keyboard;
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
            renderer.DrawRect(spriteBatch, canvasBounds, LayoutConstants.CanvasBackground);
            string hint = "Double-click a quest to edit, or click '+ Add Quest'";
            var hintSize = font.MeasureString(hint);
            spriteBatch.DrawString(font, hint,
                new Vector2(canvasBounds.X + (canvasBounds.Width - hintSize.X) / 2,
                            canvasBounds.Y + (canvasBounds.Height - hintSize.Y) / 2),
                LayoutConstants.DialogueTreeHintColor);
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
        _editor = null;
    }

    private void HandleListPanelSignals(EditorState state)
    {
        if (_listPanel.WantsNew)
        {
            _editor = QuestEditor.ForNewQuest();
        }
        else if (_listPanel.WantsEditIndex >= 0)
        {
            int idx = _listPanel.WantsEditIndex;
            if (idx < state.Quests.Count)
                _editor = QuestEditor.ForExistingQuest(state.Quests[idx]);
        }
        else if (_listPanel.WantsDeleteIndex >= 0)
        {
            int idx = _listPanel.WantsDeleteIndex;
            if (idx < state.Quests.Count)
            {
                string name = state.Quests[idx].Name ?? state.Quests[idx].Id;
                int capturedIdx = idx;
                _showConfirmDelete(name, () =>
                {
                    if (capturedIdx < state.Quests.Count)
                    {
                        state.Quests.RemoveAt(capturedIdx);
                        _saveQuests(state.Quests);
                        state.NotifyQuestsChanged();
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
            state.Quests.Add(result);
        }
        else
        {
            string origId = _editor.OriginalId;
            int idx = state.Quests.FindIndex(q => q.Id == origId);
            if (idx >= 0)
                state.Quests[idx] = result;
            else
                state.Quests.Add(result);
        }

        _saveQuests(state.Quests);
        state.NotifyQuestsChanged();
    }

    private static Rectangle ComputeSidebarBounds(int screenH)
    {
        int topOffset = LayoutConstants.TopChromeHeight;
        return new Rectangle(0, topOffset, LayoutConstants.PanelDockWidth,
                             screenH - topOffset - LayoutConstants.StatusBarHeight);
    }
}
