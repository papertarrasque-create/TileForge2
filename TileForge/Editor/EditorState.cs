using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TileForge.Data;
using TileForge.Editor.Tools;
using TileForge.Game;
using TileForge.Play;
using DojoUI;

namespace TileForge.Editor;

public class EditorState
{
    // --- Multimap support ---

    public List<MapDocumentState> MapDocuments { get; set; } = new();
    private int _activeMapIndex = -1;
    private UndoStack _wiredUndoStack;
    private readonly UndoStack _fallbackUndoStack = new();

    public int ActiveMapIndex
    {
        get => _activeMapIndex;
        set
        {
            if (value < -1) value = -1;
            if (value >= MapDocuments.Count) value = MapDocuments.Count - 1;
            if (_activeMapIndex == value) return;

            _activeMapIndex = value;
            WireUndoStack();
            ActiveMapChanged?.Invoke(ActiveMapDocument);
        }
    }

    public MapDocumentState ActiveMapDocument =>
        _activeMapIndex >= 0 && _activeMapIndex < MapDocuments.Count
            ? MapDocuments[_activeMapIndex] : null;

    public event Action<MapDocumentState> ActiveMapChanged;

    // --- Map facade (delegates to active document) ---

    public MapData Map
    {
        get => ActiveMapDocument?.Map;
        set
        {
            if (ActiveMapDocument != null)
            {
                ActiveMapDocument.Map = value;
            }
            else if (value != null)
            {
                // Auto-create a map document for backward compatibility
                var doc = new MapDocumentState { Name = "main", Map = value };
                MapDocuments.Add(doc);
                ActiveMapIndex = 0;
            }
        }
    }

    // --- UndoStack facade ---

    public UndoStack UndoStack => ActiveMapDocument?.UndoStack ?? _fallbackUndoStack;

    // --- Shared project-level state ---

    public List<TileGroup> Groups { get; set; } = new();
    public Dictionary<string, TileGroup> GroupsByName { get; private set; } = new();

    public ISpriteSheet Sheet { get; set; }
    public string SheetPath { get; set; }

    // --- Constructor ---

    public EditorState()
    {
        _fallbackUndoStack.StateChanged += OnUndoStackStateChanged;
        _wiredUndoStack = _fallbackUndoStack;
    }

    private void WireUndoStack()
    {
        var target = ActiveMapDocument?.UndoStack ?? _fallbackUndoStack;
        if (target == _wiredUndoStack) return;

        if (_wiredUndoStack != null)
            _wiredUndoStack.StateChanged -= OnUndoStackStateChanged;

        _wiredUndoStack = target;
        _wiredUndoStack.StateChanged += OnUndoStackStateChanged;
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
    public event Action QuestsChanged;

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

    private string _fallbackActiveLayerName = "Ground";
    public string ActiveLayerName
    {
        get => ActiveMapDocument?.ActiveLayerName ?? _fallbackActiveLayerName;
        set
        {
            var doc = ActiveMapDocument;
            string current = doc?.ActiveLayerName ?? _fallbackActiveLayerName;
            if (current != value)
            {
                if (doc != null)
                    doc.ActiveLayerName = value;
                else
                    _fallbackActiveLayerName = value;
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

    private string _fallbackSelectedEntityId;
    public string SelectedEntityId
    {
        get => ActiveMapDocument?.SelectedEntityId ?? _fallbackSelectedEntityId;
        set
        {
            var doc = ActiveMapDocument;
            string current = doc?.SelectedEntityId ?? _fallbackSelectedEntityId;
            if (current != value)
            {
                if (doc != null)
                    doc.SelectedEntityId = value;
                else
                    _fallbackSelectedEntityId = value;
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

    // --- Quest data (loaded from quests.json) ---

    public List<QuestDefinition> Quests { get; set; } = new();

    public void NotifyQuestsChanged()
    {
        QuestsChanged?.Invoke();
        MarkDirty();
    }

    // --- World layout (grid-based map relationships) ---

    public WorldLayout WorldLayout { get; set; }

    // --- Dialogue data (loaded from dialogues/*.json) ---

    public List<DialogueData> Dialogues { get; set; } = new();

    public event Action DialoguesChanged;

    public void NotifyDialoguesChanged()
    {
        DialoguesChanged?.Invoke();
        MarkDirty();
    }

    // --- Selection & Clipboard ---

    private Rectangle? _fallbackTileSelection;
    /// <summary>Selection rectangle in grid coordinates, or null if no selection.</summary>
    public Rectangle? TileSelection
    {
        get => ActiveMapDocument?.TileSelection ?? _fallbackTileSelection;
        set
        {
            if (ActiveMapDocument != null)
                ActiveMapDocument.TileSelection = value;
            else
                _fallbackTileSelection = value;
        }
    }

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

        // Clear all map cells referencing this group across ALL maps
        foreach (var doc in MapDocuments)
        {
            if (doc.Map == null) continue;
            foreach (var layer in doc.Map.Layers)
            {
                for (int i = 0; i < layer.Cells.Length; i++)
                {
                    if (layer.Cells[i] == name)
                        layer.Cells[i] = null;
                }
            }
            doc.Map.Entities.RemoveAll(e => e.GroupName == name);
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

        // Update references across ALL maps
        foreach (var doc in MapDocuments)
        {
            if (doc.Map == null) continue;
            foreach (var layer in doc.Map.Layers)
            {
                for (int i = 0; i < layer.Cells.Length; i++)
                {
                    if (layer.Cells[i] == oldName)
                        layer.Cells[i] = newName;
                }
            }
            foreach (var entity in doc.Map.Entities)
            {
                if (entity.GroupName == oldName)
                    entity.GroupName = newName;
            }
        }

        if (SelectedGroupName == oldName)
            SelectedGroupName = newName;
    }
}
