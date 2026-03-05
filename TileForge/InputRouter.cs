using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Editor.Commands;
using TileForge.Editor.Tools;

namespace TileForge;

public class InputRouter
{
    private readonly EditorState _state;
    private readonly Action _save;
    private readonly Action _open;
    private readonly Action _enterPlayMode;
    private readonly Action _exitPlayMode;
    private readonly Action _exitGame;
    private readonly Action _resizeMap;
    private readonly Action _openRecent;
    private readonly Action _newProject;
    private readonly Action _export;
    private readonly Action _toggleMinimap;

    public InputRouter(EditorState state,
                       Action save, Action open,
                       Action enterPlayMode, Action exitPlayMode,
                       Action exitGame, Action resizeMap,
                       Action openRecent = null, Action newProject = null,
                       Action export = null, Action toggleMinimap = null)
    {
        _state = state;
        _save = save;
        _open = open;
        _enterPlayMode = enterPlayMode;
        _exitPlayMode = exitPlayMode;
        _exitGame = exitGame;
        _resizeMap = resizeMap;
        _openRecent = openRecent;
        _newProject = newProject;
        _export = export;
        _toggleMinimap = toggleMinimap;
    }

    /// <summary>
    /// Processes keyboard shortcuts for one frame. Returns true if a shortcut consumed the input
    /// (caller should skip further update logic for this frame).
    /// </summary>
    public bool Update(KeyboardState keyboard, KeyboardState prevKeyboard, MouseState mouse)
    {
        bool ctrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

        if (ctrl && KeyPressed(keyboard, prevKeyboard, Keys.S))
        {
            _save();
            return true;
        }

        if (ctrl && shift && KeyPressed(keyboard, prevKeyboard, Keys.O))
        {
            _openRecent?.Invoke();
            return true;
        }

        if (ctrl && KeyPressed(keyboard, prevKeyboard, Keys.O))
        {
            _open();
            return true;
        }

        if (ctrl && KeyPressed(keyboard, prevKeyboard, Keys.N))
        {
            _newProject?.Invoke();
            return true;
        }

        if (ctrl && KeyPressed(keyboard, prevKeyboard, Keys.Z))
        {
            _state.UndoStack.Undo();
            return true;
        }

        if (ctrl && KeyPressed(keyboard, prevKeyboard, Keys.Y))
        {
            _state.UndoStack.Redo();
            return true;
        }

        if (ctrl && KeyPressed(keyboard, prevKeyboard, Keys.R))
        {
            _resizeMap();
            return true;
        }

        if (ctrl && KeyPressed(keyboard, prevKeyboard, Keys.E))
        {
            _export?.Invoke();
            return true;
        }

        if (ctrl && KeyPressed(keyboard, prevKeyboard, Keys.M))
        {
            _toggleMinimap?.Invoke();
            return true;
        }

        // F5 toggles play mode
        if (KeyPressed(keyboard, prevKeyboard, Keys.F5))
        {
            if (_state.IsPlayMode)
                _exitPlayMode();
            else
                _enterPlayMode();
            return true;
        }

        if (KeyPressed(keyboard, prevKeyboard, Keys.Escape))
        {
            // In play mode, Escape is handled by the ScreenManager (opens PauseScreen).
            // Only F5 exits play mode from InputRouter.
            if (_state.IsPlayMode)
                return false;

            if (_state.Clipboard != null)
            {
                _state.Clipboard = null;
            }
            else if (_state.TileSelection.HasValue)
            {
                _state.TileSelection = null;
            }
            else if (_state.SelectedEntityId != null)
            {
                _state.SelectedEntityId = null;
            }
            else
            {
                _exitGame();
            }
            return true;
        }

        // Play mode -- skip all editor keybinds
        if (_state.IsPlayMode)
            return false;

        // Ctrl+C: copy tile selection to clipboard
        if (ctrl && KeyPressed(keyboard, prevKeyboard, Keys.C)
            && _state.TileSelection.HasValue && _state.Map != null)
        {
            CopySelection();
            return true;
        }

        // Ctrl+V: paste clipboard at selection origin (or top-left if no selection)
        if (ctrl && KeyPressed(keyboard, prevKeyboard, Keys.V)
            && _state.Clipboard != null && _state.Map != null)
        {
            PasteClipboard();
            return true;
        }

        // Tool keybinds
        if (KeyPressed(keyboard, prevKeyboard, Keys.B))
            _state.ActiveTool = new BrushTool();
        if (KeyPressed(keyboard, prevKeyboard, Keys.E))
            _state.ActiveTool = new EraserTool();
        if (KeyPressed(keyboard, prevKeyboard, Keys.F))
            _state.ActiveTool = new FillTool();
        if (KeyPressed(keyboard, prevKeyboard, Keys.N))
            _state.ActiveTool = new EntityTool();
        if (KeyPressed(keyboard, prevKeyboard, Keys.I))
            _state.ActiveTool = new PickerTool();
        if (KeyPressed(keyboard, prevKeyboard, Keys.M))
            _state.ActiveTool = new SelectionTool();

        // Delete: clear tile selection, or remove selected entity
        if (KeyPressed(keyboard, prevKeyboard, Keys.Delete) && _state.Map != null)
        {
            if (_state.TileSelection.HasValue && _state.ActiveLayerName != null)
            {
                var sel = _state.TileSelection.Value;
                var cmd = new ClearSelectionCommand(_state.Map, _state.ActiveLayerName, sel);
                cmd.Execute();
                _state.UndoStack.Push(cmd);
                _state.TileSelection = null;
            }
            else if (_state.SelectedEntityId != null)
            {
                var entity = _state.Map.Entities.Find(e => e.Id == _state.SelectedEntityId);
                if (entity != null)
                {
                    _state.Map.Entities.Remove(entity);
                    _state.SelectedEntityId = null;
                    _state.UndoStack.Push(new RemoveEntityCommand(_state.Map, entity, _state));
                }
            }
        }

        // Layer switching with Tab
        if (KeyPressed(keyboard, prevKeyboard, Keys.Tab) && _state.Map != null && _state.Map.Layers.Count > 1)
        {
            int currentIdx = _state.Map.Layers.FindIndex(l => l.Name == _state.ActiveLayerName);
            int nextIdx = (currentIdx + 1) % _state.Map.Layers.Count;
            _state.ActiveLayerName = _state.Map.Layers[nextIdx].Name;
        }

        // Layer visibility toggle
        if (KeyPressed(keyboard, prevKeyboard, Keys.V) && _state.Map != null)
        {
            var layer = _state.ActiveLayer;
            if (layer != null)
                layer.Visible = !layer.Visible;
        }

        // Layer reordering with Shift+Up/Down
        if (shift && _state.Map != null && _state.Map.Layers.Count > 1)
        {
            int idx = _state.Map.Layers.FindIndex(l => l.Name == _state.ActiveLayerName);
            if (idx >= 0)
            {
                int target = -1;
                if (KeyPressed(keyboard, prevKeyboard, Keys.Up) && idx < _state.Map.Layers.Count - 1)
                    target = idx + 1;
                else if (KeyPressed(keyboard, prevKeyboard, Keys.Down) && idx > 0)
                    target = idx - 1;

                if (target >= 0)
                {
                    var cmd = new ReorderLayerCommand(_state.Map, idx, target);
                    cmd.Execute();
                    _state.UndoStack.Push(cmd);
                }
            }
        }

        // Auto-switch tool based on selected group type (skip when SelectionTool is active)
        var selected = _state.SelectedGroup;
        if (selected != null && _state.ActiveTool is not SelectionTool)
        {
            if (selected.Type == GroupType.Entity && _state.ActiveTool is not EntityTool)
                _state.ActiveTool = new EntityTool();
            else if (selected.Type == GroupType.Tile && _state.ActiveTool is EntityTool)
                _state.ActiveTool = new BrushTool();
        }

        return false;
    }

    private void CopySelection()
    {
        if (!_state.TileSelection.HasValue || _state.Map == null) return;

        var sel = _state.TileSelection.Value;
        var layer = _state.ActiveLayer;
        if (layer == null) return;

        var cells = new string[sel.Width * sel.Height];
        for (int cy = 0; cy < sel.Height; cy++)
        {
            for (int cx = 0; cx < sel.Width; cx++)
            {
                int mapX = sel.X + cx;
                int mapY = sel.Y + cy;
                if (_state.Map.InBounds(mapX, mapY))
                    cells[cx + cy * sel.Width] = layer.GetCell(mapX, mapY, _state.Map.Width);
            }
        }

        _state.Clipboard = new TileClipboard(sel.Width, sel.Height, cells);
    }

    private void PasteClipboard()
    {
        if (_state.Clipboard == null || _state.Map == null || _state.ActiveLayerName == null) return;

        // Paste at selection origin if there is one, otherwise at (0, 0)
        int targetX = 0;
        int targetY = 0;
        if (_state.TileSelection.HasValue)
        {
            targetX = _state.TileSelection.Value.X;
            targetY = _state.TileSelection.Value.Y;
        }

        var cmd = new PasteCommand(_state.Map, _state.ActiveLayerName, targetX, targetY, _state.Clipboard);
        cmd.Execute();
        _state.UndoStack.Push(cmd);
    }

    private static bool KeyPressed(KeyboardState current, KeyboardState prev, Keys key)
        => current.IsKeyDown(key) && prev.IsKeyUp(key);
}
