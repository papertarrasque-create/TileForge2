using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TileForge.Data;
using TileForge.Editor.Tools;
using TileForge.Play;
using DojoUI;

namespace TileForge.Editor;

public class EditorState
{
    public MapData Map { get; set; }
    public List<TileGroup> Groups { get; set; } = new();
    public Dictionary<string, TileGroup> GroupsByName { get; private set; } = new();

    public ISpriteSheet Sheet { get; set; }
    public string SheetPath { get; set; }

    public UndoStack UndoStack { get; }

    public EditorState()
    {
        UndoStack = new UndoStack();
        UndoStack.StateChanged += OnUndoStackStateChanged;
    }

    private void OnUndoStackStateChanged()
    {
        MarkDirty();
        NotifyUndoRedoStateChanged();
    }

    // --- Events ---

    public event Action<string> ActiveLayerChanged;
    public event Action<string> SelectedGroupChanged;
    public event Action<ITool> ActiveToolChanged;
    public event Action<string> SelectedEntityChanged;
    public event Action<bool> PlayModeChanged;
    public event Action MapDirtied;
    public event Action UndoRedoStateChanged;

    // --- Event-raising properties ---

    private ITool _activeTool;
    public ITool ActiveTool
    {
        get => _activeTool;
        set
        {
            if (_activeTool != value)
            {
                // Clear tile selection when switching away from SelectionTool
                if (_activeTool is Tools.SelectionTool && value is not Tools.SelectionTool)
                    TileSelection = null;

                _activeTool = value;
                ActiveToolChanged?.Invoke(value);
            }
        }
    }

    private string _activeLayerName = "Ground";
    public string ActiveLayerName
    {
        get => _activeLayerName;
        set
        {
            if (_activeLayerName != value)
            {
                _activeLayerName = value;
                ActiveLayerChanged?.Invoke(value);
            }
        }
    }

    private string _selectedGroupName;
    public string SelectedGroupName
    {
        get => _selectedGroupName;
        set
        {
            if (_selectedGroupName != value)
            {
                _selectedGroupName = value;
                SelectedGroupChanged?.Invoke(value);
            }
        }
    }

    private string _selectedEntityId;
    public string SelectedEntityId
    {
        get => _selectedEntityId;
        set
        {
            if (_selectedEntityId != value)
            {
                _selectedEntityId = value;
                SelectedEntityChanged?.Invoke(value);
            }
        }
    }

    private bool _isPlayMode;
    public bool IsPlayMode
    {
        get => _isPlayMode;
        set
        {
            if (_isPlayMode != value)
            {
                _isPlayMode = value;
                PlayModeChanged?.Invoke(value);
            }
        }
    }

    public PlayState PlayState { get; set; }

    // --- Selection & Clipboard ---

    /// <summary>Selection rectangle in grid coordinates, or null if no selection.</summary>
    public Rectangle? TileSelection { get; set; }

    /// <summary>Clipboard holding copied tile data.</summary>
    public TileClipboard Clipboard { get; set; }

    /// <summary>Grid overlay configuration.</summary>
    public GridConfig Grid { get; } = new();

    // --- Dirty state tracking ---

    public bool IsDirty { get; private set; }

    public void MarkDirty()
    {
        IsDirty = true;
        MapDirtied?.Invoke();
    }

    public void ClearDirty()
    {
        IsDirty = false;
    }

    /// <summary>
    /// Raises the UndoRedoStateChanged event. Called by UndoStack when its state changes.
    /// </summary>
    public void NotifyUndoRedoStateChanged()
    {
        UndoRedoStateChanged?.Invoke();
    }

    public MapLayer ActiveLayer
    {
        get => Map?.GetLayer(ActiveLayerName);
    }

    public TileGroup SelectedGroup
    {
        get
        {
            if (SelectedGroupName != null && GroupsByName.TryGetValue(SelectedGroupName, out var g))
                return g;
            return null;
        }
    }

    public void RebuildGroupIndex()
    {
        GroupsByName = new Dictionary<string, TileGroup>();
        foreach (var group in Groups)
            GroupsByName[group.Name] = group;
    }

    public void AddGroup(TileGroup group)
    {
        Groups.Add(group);
        GroupsByName[group.Name] = group;
    }

    public void RemoveGroup(string name)
    {
        if (!GroupsByName.TryGetValue(name, out var group)) return;

        Groups.Remove(group);
        GroupsByName.Remove(name);

        // Clear all map cells referencing this group
        if (Map != null)
        {
            foreach (var layer in Map.Layers)
            {
                for (int i = 0; i < layer.Cells.Length; i++)
                {
                    if (layer.Cells[i] == name)
                        layer.Cells[i] = null;
                }
            }

            Map.Entities.RemoveAll(e => e.GroupName == name);
        }

        if (SelectedGroupName == name)
            SelectedGroupName = Groups.Count > 0 ? Groups[0].Name : null;
    }

    public void RenameGroup(string oldName, string newName)
    {
        if (!GroupsByName.TryGetValue(oldName, out var group)) return;
        if (GroupsByName.ContainsKey(newName)) return;

        group.Name = newName;
        GroupsByName.Remove(oldName);
        GroupsByName[newName] = group;

        if (Map != null)
        {
            foreach (var layer in Map.Layers)
            {
                for (int i = 0; i < layer.Cells.Length; i++)
                {
                    if (layer.Cells[i] == oldName)
                        layer.Cells[i] = newName;
                }
            }

            foreach (var entity in Map.Entities)
            {
                if (entity.GroupName == oldName)
                    entity.GroupName = newName;
            }
        }

        if (SelectedGroupName == oldName)
            SelectedGroupName = newName;
    }
}
