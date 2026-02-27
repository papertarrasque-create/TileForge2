using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Game;
using TileForge.Infrastructure;
using TileForge.UI;

namespace TileForge;

public class ProjectManager
{
    private readonly EditorState _state;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly GameWindow _window;
    private readonly MapCanvas _canvas;
    private readonly PanelDock _panelDock;
    private readonly MapPanel _mapPanel;
    private readonly Func<Rectangle> _getCanvasBounds;
    private readonly Action<IDialog, Action<IDialog>> _showDialog;
    private readonly IProjectFileService _projectFileService;

    private string _projectPath;
    private readonly RecentFilesManager _recentFiles;

    public string ProjectPath => _projectPath;
    public RecentFilesManager RecentFiles => _recentFiles;

    public ProjectManager(EditorState state, GraphicsDevice graphicsDevice, GameWindow window,
                          MapCanvas canvas, PanelDock panelDock, MapPanel mapPanel,
                          Func<Rectangle> getCanvasBounds,
                          Action<IDialog, Action<IDialog>> showDialog,
                          IProjectFileService projectFileService = null)
    {
        _state = state;
        _graphicsDevice = graphicsDevice;
        _window = window;
        _canvas = canvas;
        _panelDock = panelDock;
        _mapPanel = mapPanel;
        _getCanvasBounds = getCanvasBounds;
        _showDialog = showDialog;
        _projectFileService = projectFileService ?? new DefaultProjectFileService();

        _recentFiles = new RecentFilesManager();

        _state.MapDirtied += OnMapDirtied;
    }

    private void OnMapDirtied()
    {
        string mapSuffix = _state.ActiveMapDocument != null
            ? $" [{_state.ActiveMapDocument.Name}]" : "";

        if (_projectPath != null)
            _window.Title = $"*TileForge — {Path.GetFileName(_projectPath)}{mapSuffix}";
        else if (_state.SheetPath != null)
            _window.Title = $"*TileForge — {Path.GetFileName(_state.SheetPath)}{mapSuffix}";
        else
            _window.Title = "*TileForge";
    }

    public void NewProject()
    {
        var dialog = new NewProjectDialog(browseCallback =>
        {
            string startDir = _state.SheetPath != null
                ? Path.GetDirectoryName(_state.SheetPath)
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var browser = new FileBrowserDialog("Select Spritesheet", startDir,
                new[] { ".png", ".jpg" }, FileBrowserMode.Open);

            _showDialog(browser, fb =>
            {
                var fileBrowser = (FileBrowserDialog)fb;
                if (!fileBrowser.WasCancelled)
                    browseCallback(fileBrowser.ResultPath);
            });
        });

        _showDialog(dialog, d =>
        {
            var npd = (NewProjectDialog)d;
            if (npd.WasCancelled) return;

            if (ParseTileSize(npd.TileSizeText, out int tw, out int th, out int padding))
            {
                // Parse map size
                string mapText = npd.MapSizeText.Trim();
                int xIdx = mapText.IndexOf('x', StringComparison.OrdinalIgnoreCase);
                int mapW = LayoutConstants.DefaultMapWidth;
                int mapH = LayoutConstants.DefaultMapHeight;
                if (xIdx > 0)
                {
                    int.TryParse(mapText.AsSpan(0, xIdx), out mapW);
                    int.TryParse(mapText.AsSpan(xIdx + 1), out mapH);
                }
                if (mapW < 1) mapW = 1;
                if (mapH < 1) mapH = 1;

                // Reset state for new project
                _state.Groups = new List<TileGroup>();

                // Seed a default Player entity group
                var playerGroup = new TileGroup
                {
                    Name = "Player",
                    Type = GroupType.Entity,
                    IsPlayer = true,
                    Sprites = { new SpriteRef { Col = 0, Row = 0 } },
                };
                _state.Groups.Add(playerGroup);
                _state.RebuildGroupIndex();

                // Create the initial map document
                var mapData = new MapData(mapW, mapH);
                mapData.Entities.Add(new Entity
                {
                    Id = "player",
                    GroupName = "Player",
                    X = mapW / 2,
                    Y = mapH / 2,
                });

                _state.MapDocuments.Clear();
                _state.MapDocuments.Add(new MapDocumentState
                {
                    Name = "main",
                    Map = mapData,
                });
                _state.ActiveMapIndex = 0;

                _state.UndoStack.Clear();
                _state.SelectedEntityId = null;
                _state.SelectedGroupName = playerGroup.Name;
                _state.Quests = new List<QuestDefinition>();
                _state.Dialogues = new List<DialogueData>();
                _state.WorldLayout = null;
                _state.ClearDirty();
                _projectPath = null;

                // Load the spritesheet
                LoadSpritesheet(npd.SpritesheetPath, tw, th, padding);
            }
        });
    }

    public void Save()
    {
        System.Diagnostics.Debug.WriteLine($"[SAVE] SaveProject called. Sheet={_state.Sheet != null}, ProjectPath={_projectPath}");
        if (_state.Sheet == null) return;

        if (_projectPath != null)
        {
            DoSave(_projectPath);
        }
        else
        {
            PromptSavePath();
        }
    }

    public void OpenRecent()
    {
        _recentFiles.PruneNonExistent();
        if (_recentFiles.RecentFiles.Count == 0) return;

        var dialog = new RecentFilesDialog(_recentFiles.RecentFiles);
        _showDialog(dialog, d =>
        {
            var rfd = (RecentFilesDialog)d;
            if (rfd.WasCancelled || rfd.SelectedPath == null) return;
            Load(rfd.SelectedPath);
        });
    }

    public void Open()
    {
        string startDir = _projectPath != null
            ? Path.GetDirectoryName(_projectPath)
            : (_state.SheetPath != null
                ? Path.GetDirectoryName(_state.SheetPath)
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        var browser = new FileBrowserDialog("Open File", startDir,
            new[] { ".png", ".jpg", ".tileforge", ".tileforge2" }, FileBrowserMode.Open);

        _showDialog(browser, dialog =>
        {
            var fb = (FileBrowserDialog)dialog;
            if (fb.WasCancelled) return;

            string path = fb.ResultPath;
            if (path.EndsWith(".tileforge", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".tileforge2", StringComparison.OrdinalIgnoreCase))
                Load(path);
            else
                PromptTileSize(path);
        });
    }

    public void Load(string path)
    {
        try
        {
            var data = _projectFileService.Load(path);

            // Load spritesheet
            _state.Sheet = new SpriteSheet(_graphicsDevice, data.Spritesheet.Path,
                                           data.Spritesheet.TileWidth, data.Spritesheet.TileHeight,
                                           data.Spritesheet.Padding);
            _state.SheetPath = data.Spritesheet.Path;

            // Restore groups (shared)
            _state.Groups = ProjectFile.RestoreGroups(data);
            _state.RebuildGroupIndex();

            // Restore maps
            _state.MapDocuments.Clear();

            if (data.Maps != null && data.Maps.Count > 0)
            {
                // V2: multiple maps
                foreach (var mapDoc in data.Maps)
                {
                    var doc = new MapDocumentState
                    {
                        Name = mapDoc.Name,
                        Map = ProjectFile.RestoreMapDocument(mapDoc),
                    };
                    _state.MapDocuments.Add(doc);
                }
            }
            else
            {
                // V1: single map migration
                var doc = new MapDocumentState
                {
                    Name = Path.GetFileNameWithoutExtension(path),
                    Map = ProjectFile.RestoreMap(data),
                };
                _state.MapDocuments.Add(doc);
            }

            // Restore per-map editor states
            if (data.EditorState?.MapStates != null)
            {
                foreach (var ms in data.EditorState.MapStates)
                {
                    var doc = _state.MapDocuments.FirstOrDefault(d => d.Name == ms.MapName);
                    if (doc == null) continue;
                    doc.CameraX = ms.CameraX;
                    doc.CameraY = ms.CameraY;
                    doc.ZoomIndex = ms.ZoomIndex;
                    doc.ActiveLayerName = ms.ActiveLayer ?? "Ground";
                    if (ms.CollapsedLayers != null)
                        doc.CollapsedLayers = new HashSet<string>(ms.CollapsedLayers);
                }
            }

            // Set active map
            string activeMapName = data.EditorState?.ActiveMapName;
            int activeIdx = activeMapName != null
                ? _state.MapDocuments.FindIndex(d => d.Name == activeMapName) : 0;
            _state.ActiveMapIndex = Math.Max(0, activeIdx);

            // Migrate groups with no layer assignment (old project files)
            string firstLayerName = _state.Map?.Layers.Count > 0 ? _state.Map.Layers[0].Name : "Ground";
            foreach (var group in _state.Groups)
            {
                if (string.IsNullOrEmpty(group.LayerName))
                    group.LayerName = firstLayerName;
            }

            // Restore V1 editor state (camera/layer for single-map files)
            if (data.EditorState != null && (data.Maps == null || data.Maps.Count == 0))
            {
                _state.ActiveLayerName = data.EditorState.ActiveLayer ?? "Ground";
                _canvas.Camera.Offset = new Vector2(data.EditorState.CameraX, data.EditorState.CameraY);

                if (data.EditorState.CollapsedLayers != null)
                    _mapPanel.RestoreCollapsedLayers(data.EditorState.CollapsedLayers);
            }
            else if (_state.ActiveMapDocument != null)
            {
                // V2: restore camera from active doc
                var doc = _state.ActiveMapDocument;
                _canvas.Camera.Offset = new Vector2(doc.CameraX, doc.CameraY);
                _canvas.Camera.ZoomIndex = doc.ZoomIndex;
                if (doc.CollapsedLayers.Count > 0)
                    _mapPanel.RestoreCollapsedLayers(doc.CollapsedLayers.ToList());
            }

            _panelDock.RestoreState(data.EditorState?.PanelOrder, data.EditorState?.CollapsedPanels);

            // Load quest definitions from quests.json
            string projectDir = Path.GetDirectoryName(Path.GetFullPath(path));
            _state.Quests = QuestFileManager.Load(projectDir);

            // Load dialogue definitions from dialogues/*.json
            _state.Dialogues = DialogueFileManager.LoadAll(projectDir);

            // Restore world layout (null if not configured)
            _state.WorldLayout = data.WorldLayout;

            // Select first group if available
            if (_state.Groups.Count > 0 && _state.SelectedGroupName == null)
                _state.SelectedGroupName = _state.Groups[0].Name;

            _state.UndoStack.Clear();
            _state.SelectedEntityId = null;
            _state.ClearDirty();

            _projectPath = path;
            UpdateWindowTitle(false);
            _recentFiles.AddRecent(path);
        }
        catch
        {
            // Silently fail on corrupt project files
        }
    }

    // --- Map CRUD ---

    public void CreateNewMap(string name, int width, int height)
    {
        string uniqueName = EnsureUniqueName(name);
        var map = new MapData(width, height);

        // Copy layer structure from the active map so custom layers carry over
        var sourceMap = _state.Map;
        if (sourceMap != null && sourceMap.Layers.Count > 0)
        {
            map.Layers.Clear();
            foreach (var srcLayer in sourceMap.Layers)
                map.Layers.Add(new Data.MapLayer(srcLayer.Name, width, height));
            map.EntityRenderOrder = System.Math.Min(sourceMap.EntityRenderOrder, map.Layers.Count - 1);
        }

        var doc = new MapDocumentState
        {
            Name = uniqueName,
            Map = map,
        };
        _state.MapDocuments.Add(doc);
        _state.ActiveMapIndex = _state.MapDocuments.Count - 1;

        // Sync camera to new map
        _canvas.Camera.Offset = new Vector2(doc.CameraX, doc.CameraY);
        _canvas.Camera.ZoomIndex = doc.ZoomIndex;

        _state.MarkDirty();
    }

    public void DeleteMap(int index)
    {
        if (_state.MapDocuments.Count <= 1) return;
        if (index < 0 || index >= _state.MapDocuments.Count) return;

        string deletedName = _state.MapDocuments[index].Name;
        _state.MapDocuments.RemoveAt(index);

        // Remove from WorldLayout
        _state.WorldLayout?.Maps?.Remove(deletedName);
        if (_state.ActiveMapIndex >= _state.MapDocuments.Count)
            _state.ActiveMapIndex = _state.MapDocuments.Count - 1;
        else if (_state.ActiveMapIndex == index)
        {
            // Force re-wire by triggering setter
            int newIdx = Math.Min(index, _state.MapDocuments.Count - 1);
            // Reset to -1 first to force change event
            _state.ActiveMapIndex = -1;
            _state.ActiveMapIndex = newIdx;
        }

        // Sync camera to active map
        var activeDoc = _state.ActiveMapDocument;
        if (activeDoc != null)
        {
            _canvas.Camera.Offset = new Vector2(activeDoc.CameraX, activeDoc.CameraY);
            _canvas.Camera.ZoomIndex = activeDoc.ZoomIndex;
        }

        _state.MarkDirty();
    }

    public void RenameMap(int index, string newName)
    {
        if (index < 0 || index >= _state.MapDocuments.Count) return;

        string oldName = _state.MapDocuments[index].Name;
        string uniqueName = EnsureUniqueName(newName, oldName);
        _state.MapDocuments[index].Name = uniqueName;

        // Update target_map references across all maps
        foreach (var doc in _state.MapDocuments)
        {
            if (doc.Map == null) continue;
            foreach (var entity in doc.Map.Entities)
            {
                if (entity.Properties.TryGetValue("target_map", out var targetMap)
                    && targetMap == oldName)
                {
                    entity.Properties["target_map"] = uniqueName;
                }
            }
        }

        // Update WorldLayout key
        if (_state.WorldLayout?.Maps != null
            && _state.WorldLayout.Maps.Remove(oldName, out var placement))
        {
            _state.WorldLayout.Maps[uniqueName] = placement;
        }

        _state.MarkDirty();
    }

    public void DuplicateMap(int index)
    {
        if (index < 0 || index >= _state.MapDocuments.Count) return;

        var source = _state.MapDocuments[index];
        string newName = EnsureUniqueName(source.Name + "_copy");

        var newMap = DeepCloneMapData(source.Map);
        var doc = new MapDocumentState
        {
            Name = newName,
            Map = newMap,
            CameraX = source.CameraX,
            CameraY = source.CameraY,
            ZoomIndex = source.ZoomIndex,
            ActiveLayerName = source.ActiveLayerName,
        };
        _state.MapDocuments.Insert(index + 1, doc);
        _state.ActiveMapIndex = index + 1;

        _canvas.Camera.Offset = new Vector2(doc.CameraX, doc.CameraY);
        _canvas.Camera.ZoomIndex = doc.ZoomIndex;

        _state.MarkDirty();
    }

    /// <summary>
    /// Saves the current camera state to the active MapDocumentState.
    /// Call before switching tabs or saving.
    /// </summary>
    public void SyncCameraToActiveDocument()
    {
        var doc = _state.ActiveMapDocument;
        if (doc == null) return;
        doc.CameraX = _canvas.Camera.Offset.X;
        doc.CameraY = _canvas.Camera.Offset.Y;
        doc.ZoomIndex = _canvas.Camera.ZoomIndex;
        doc.CollapsedLayers = new HashSet<string>(_mapPanel.GetCollapsedLayers());
    }

    /// <summary>
    /// Restores camera state from the active MapDocumentState.
    /// Call after switching tabs.
    /// </summary>
    public void RestoreCameraFromActiveDocument()
    {
        var doc = _state.ActiveMapDocument;
        if (doc == null) return;
        _canvas.Camera.Offset = new Vector2(doc.CameraX, doc.CameraY);
        _canvas.Camera.ZoomIndex = doc.ZoomIndex;
        if (doc.CollapsedLayers.Count > 0)
            _mapPanel.RestoreCollapsedLayers(doc.CollapsedLayers.ToList());
        else
            _mapPanel.RestoreCollapsedLayers(new List<string>());
    }

    // --- Helpers ---

    private string EnsureUniqueName(string name, string skipName = null)
    {
        var existing = new HashSet<string>(
            _state.MapDocuments.Select(d => d.Name),
            StringComparer.OrdinalIgnoreCase);

        if (skipName != null)
            existing.Remove(skipName);

        if (!existing.Contains(name))
            return name;

        for (int i = 2; i < 1000; i++)
        {
            string candidate = $"{name}_{i}";
            if (!existing.Contains(candidate))
                return candidate;
        }
        return name + "_" + Guid.NewGuid().ToString("N")[..6];
    }

    private static MapData DeepCloneMapData(MapData source)
    {
        if (source == null) return null;

        var clone = new MapData(source.Width, source.Height);
        clone.EntityRenderOrder = source.EntityRenderOrder;
        clone.Layers.Clear();

        foreach (var layer in source.Layers)
        {
            var clonedLayer = new MapLayer(layer.Name, source.Width, source.Height)
            {
                Visible = layer.Visible,
            };
            Array.Copy(layer.Cells, clonedLayer.Cells, layer.Cells.Length);
            clone.Layers.Add(clonedLayer);
        }

        foreach (var entity in source.Entities)
        {
            clone.Entities.Add(new Entity
            {
                Id = Guid.NewGuid().ToString("N")[..8],
                GroupName = entity.GroupName,
                X = entity.X,
                Y = entity.Y,
                Properties = new Dictionary<string, string>(entity.Properties),
            });
        }

        return clone;
    }

    private void UpdateWindowTitle(bool dirty)
    {
        string prefix = dirty ? "*" : "";
        string mapSuffix = _state.ActiveMapDocument != null
            ? $" [{_state.ActiveMapDocument.Name}]" : "";

        if (_projectPath != null)
            _window.Title = $"{prefix}TileForge — {Path.GetFileName(_projectPath)}{mapSuffix}";
        else if (_state.SheetPath != null)
            _window.Title = $"{prefix}TileForge — {Path.GetFileName(_state.SheetPath)}{mapSuffix}";
        else
            _window.Title = $"{prefix}TileForge";
    }

    public void PromptTileSize(string imagePath)
    {
        _showDialog(new InputDialog("Tile size (e.g. 16, 16x24, 16+1):", "16"), dialog =>
        {
            var input = (InputDialog)dialog;
            if (input.WasCancelled) return;

            if (ParseTileSize(input.ResultText, out int tw, out int th, out int padding))
            {
                LoadSpritesheet(imagePath, tw, th, padding);
            }
        });
    }

    public void LoadSpritesheet(string path, int tileWidth, int tileHeight, int padding)
    {
        try
        {
            _state.Sheet = new SpriteSheet(_graphicsDevice, path, tileWidth, tileHeight, padding);
            _state.SheetPath = path;

            // Create a default map document if none exists
            if (_state.MapDocuments.Count == 0)
            {
                _state.MapDocuments.Add(new MapDocumentState
                {
                    Name = "main",
                    Map = new MapData(LayoutConstants.DefaultMapWidth, LayoutConstants.DefaultMapHeight),
                });
                _state.ActiveMapIndex = 0;
                _state.UndoStack.Clear();
                _state.SelectedEntityId = null;
            }

            // Center camera on the map
            var canvasBounds = _getCanvasBounds();
            _canvas.CenterOnMap(_state, canvasBounds);

            _window.Title = $"TileForge — {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load spritesheet: {ex.Message}");
        }
    }

    internal static bool ParseTileSize(string input, out int width, out int height, out int padding)
    {
        width = 0; height = 0; padding = 0;
        input = input.Trim();

        // Handle padding: "16+1"
        int plusIdx = input.IndexOf('+');
        if (plusIdx > 0)
        {
            if (!int.TryParse(input[(plusIdx + 1)..], out padding))
                return false;
            input = input[..plusIdx];
        }

        // Handle rectangular: "16x24"
        int xIdx = input.IndexOf('x');
        if (xIdx > 0)
        {
            if (!int.TryParse(input[..xIdx], out width)) return false;
            if (!int.TryParse(input[(xIdx + 1)..], out height)) return false;
            return width > 0 && height > 0;
        }

        // Square: "16"
        if (!int.TryParse(input, out width)) return false;
        height = width;
        return width > 0;
    }

    private void PromptSavePath()
    {
        string defaultName = Path.GetFileNameWithoutExtension(_state.SheetPath ?? "project") + ".tileforge";
        string startDir = Path.GetDirectoryName(_state.SheetPath ?? Environment.CurrentDirectory);

        var browser = new FileBrowserDialog("Save Project", startDir,
            new[] { ".tileforge", ".tileforge2" }, FileBrowserMode.Save, defaultName);

        _showDialog(browser, dialog =>
        {
            var fb = (FileBrowserDialog)dialog;
            if (fb.WasCancelled) return;
            DoSave(fb.ResultPath);
        });
    }

    private void DoSave(string path)
    {
        SaveToPath(path);

        _projectPath = path;
        _state.ClearDirty();
        UpdateWindowTitle(false);

        _recentFiles.AddRecent(path);

        // Clean up autosave sidecar on successful manual save
        AutoSaveManager.CleanupAutoSave(path);
    }

    /// <summary>
    /// Saves the project to the given path without changing dirty state or project path.
    /// Used by AutoSaveManager for sidecar saves.
    /// </summary>
    public void SaveToPath(string path)
    {
        // Sync current camera to active document before saving
        SyncCameraToActiveDocument();

        var editorState = new ProjectFile.EditorStateData
        {
            ActiveMapName = _state.ActiveMapDocument?.Name,
            PanelOrder = _panelDock.GetPanelOrder(),
            CollapsedPanels = _panelDock.GetCollapsedPanels(),
            MapStates = BuildMapEditorStates(),
            // V1 compat fields (used by old readers, filled from active map)
            ActiveLayer = _state.ActiveLayerName,
            CameraX = _canvas.Camera.Offset.X,
            CameraY = _canvas.Camera.Offset.Y,
            CollapsedLayers = _mapPanel.GetCollapsedLayers(),
        };

        _projectFileService.Save(path, _state.SheetPath, _state.Sheet,
                         _state.Groups, _state.MapDocuments, editorState, _state.WorldLayout);
    }

    private List<ProjectFile.MapEditorStateData> BuildMapEditorStates()
    {
        var result = new List<ProjectFile.MapEditorStateData>();
        foreach (var doc in _state.MapDocuments)
        {
            result.Add(new ProjectFile.MapEditorStateData
            {
                MapName = doc.Name,
                CameraX = doc.CameraX,
                CameraY = doc.CameraY,
                ZoomIndex = doc.ZoomIndex,
                ActiveLayer = doc.ActiveLayerName,
                CollapsedLayers = doc.CollapsedLayers.Count > 0
                    ? doc.CollapsedLayers.ToList() : null,
            });
        }
        return result;
    }
}
