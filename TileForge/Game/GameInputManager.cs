using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework.Input;

namespace TileForge.Game;

/// <summary>
/// Translates raw keyboard state into abstract GameActions.
/// Call Update() once per frame with the current KeyboardState.
/// IsActionPressed returns true while any mapped key is held down.
/// IsActionJustPressed returns true only on the frame the key was first pressed (edge detection).
/// Supports runtime rebinding via RebindAction and persistence via SaveBindings/LoadBindings.
/// </summary>
public class GameInputManager
{
    private Dictionary<GameAction, Keys[]> _bindings;
    private KeyboardState _current;
    private KeyboardState _previous;
    private bool _hasBeenUpdated;

    public GameInputManager()
    {
        _bindings = GetDefaultBindings();
    }

    /// <summary>
    /// Advances the frame: stores the previous keyboard state and records the new one.
    /// Must be called once per frame before querying actions.
    /// </summary>
    public void Update(KeyboardState current)
    {
        _previous = _current;
        _current = current;
        _hasBeenUpdated = true;
    }

    /// <summary>
    /// Returns true if ANY mapped key for the action is currently held down.
    /// </summary>
    public bool IsActionPressed(GameAction action)
    {
        if (!_bindings.TryGetValue(action, out var keys))
            return false;

        foreach (var key in keys)
        {
            if (_current.IsKeyDown(key))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns true if ANY mapped key for the action went from up (previous frame) to down (current frame).
    /// Returns false before the first Update call.
    /// </summary>
    public bool IsActionJustPressed(GameAction action)
    {
        if (!_hasBeenUpdated)
            return false;

        if (!_bindings.TryGetValue(action, out var keys))
            return false;

        foreach (var key in keys)
        {
            if (_current.IsKeyDown(key) && _previous.IsKeyUp(key))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Replaces all keys for an action with a single key.
    /// </summary>
    public void RebindAction(GameAction action, Keys key)
    {
        _bindings[action] = new[] { key };
    }

    /// <summary>
    /// Resets all bindings to defaults.
    /// </summary>
    public void ResetDefaults()
    {
        _bindings.Clear();
        foreach (var kvp in GetDefaultBindings())
            _bindings[kvp.Key] = kvp.Value;
    }

    /// <summary>
    /// Returns a copy of the current bindings.
    /// Modifying the returned dictionary does not affect the manager.
    /// </summary>
    public Dictionary<GameAction, Keys[]> GetBindings()
    {
        var copy = new Dictionary<GameAction, Keys[]>();
        foreach (var kvp in _bindings)
            copy[kvp.Key] = (Keys[])kvp.Value.Clone();
        return copy;
    }

    /// <summary>
    /// Saves current bindings to a JSON file.
    /// Creates the directory if it does not exist.
    /// </summary>
    public void SaveBindings(string path)
    {
        var serializable = new Dictionary<string, string[]>();
        foreach (var kvp in _bindings)
            serializable[kvp.Key.ToString()] = Array.ConvertAll(kvp.Value, k => k.ToString());

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(serializable, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Loads bindings from a JSON file.
    /// If the file is missing, the current bindings are unchanged.
    /// If the file is corrupt or unreadable, the current bindings are unchanged.
    /// </summary>
    public void LoadBindings(string path)
    {
        if (!File.Exists(path))
            return;

        try
        {
            var json = File.ReadAllText(path);
            var serializable = JsonSerializer.Deserialize<Dictionary<string, string[]>>(json);
            if (serializable == null) return;

            foreach (var kvp in serializable)
            {
                if (Enum.TryParse<GameAction>(kvp.Key, out var action))
                {
                    var keys = new List<Keys>();
                    foreach (var keyName in kvp.Value)
                    {
                        if (Enum.TryParse<Keys>(keyName, out var key))
                            keys.Add(key);
                    }
                    if (keys.Count > 0)
                        _bindings[action] = keys.ToArray();
                }
            }
        }
        catch
        {
            // Corrupted file — keep current bindings
        }
    }

    private static Dictionary<GameAction, Keys[]> GetDefaultBindings()
    {
        return new Dictionary<GameAction, Keys[]>
        {
            { GameAction.MoveUp,        new[] { Keys.Up } },
            { GameAction.MoveDown,      new[] { Keys.Down } },
            { GameAction.MoveLeft,      new[] { Keys.Left } },
            { GameAction.MoveRight,     new[] { Keys.Right } },
            { GameAction.Interact,      new[] { Keys.Z, Keys.Enter } },
            { GameAction.Cancel,        new[] { Keys.X, Keys.Escape } },
            { GameAction.Pause,         new[] { Keys.Escape } },
            { GameAction.OpenInventory, new[] { Keys.I } },
            { GameAction.OpenQuestLog, new[] { Keys.Q } },
        };
    }
}
