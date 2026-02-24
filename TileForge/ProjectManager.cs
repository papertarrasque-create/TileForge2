using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Game;
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

    private string _projectPath;
    private readonly RecentFilesManager _recentFiles;

    public string ProjectPath => _projectPath;
    public RecentFilesManager RecentFiles => _recentFiles;

    public ProjectManager(EditorState state, GraphicsDevice graphicsDevice, GameWindow window,
                          MapCanvas canvas, PanelDock panelDock, MapPanel mapPanel,
                          Func<Rectangle> getCanvasBounds,
                          Action<IDialog, Action<IDialog>> showDialog)
    {
        _state = state;
        _graphicsDevice = graphicsDevice;
        _window = window;
        _canvas = canvas;
        _panelDock = panelDock;
        _mapPanel = mapPanel;
        _getCanvasBounds = getCanvasBounds;
        _showDialog = showDialog;

        _recentFiles = new RecentFilesManager();

        _state.MapDirtied += OnMapDirtied;
    }

    private void OnMapDirtied()
    {
        if (_projectPath != null)
            _window.Title = $"*TileForge — {Path.GetFileName(_projectPath)}";
        else if (_state.SheetPath != null)
            _window.Title = $"*TileForge — {Path.GetFileName(_state.SheetPath)}";
        else
            _window.Title = "*TileForge";
    }

    public void NewProject()
    {
        var dialog = new NewProjectDialog(browseCallback =>
        {
            // Open a file browser for the spritesheet, then pass the result back to the dialog
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
                _state.Map = new MapData(mapW, mapH);
                _state.Groups = new System.Collections.Generic.List<TileGroup>();

                // Seed a default Player entity group and place it at center
                var playerGroup = new TileGroup
                {
                    Name = "Player",
                    Type = GroupType.Entity,
                    IsPlayer = true,
                    Sprites = { new SpriteRef { Col = 0, Row = 0 } },
                };
                _state.Groups.Add(playerGroup);
                _state.Map.Entities.Add(new Entity
                {
                    Id = "player",
                    GroupName = "Player",
                    X = mapW / 2,
                    Y = mapH / 2,
                });

                _state.RebuildGroupIndex();
                _state.UndoStack.Clear();
                _state.SelectedEntityId = null;
                _state.SelectedGroupName = playerGroup.Name;
                _state.Quests = new List<QuestDefinition>();
                _state.Dialogues = new List<DialogueData>();
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
            var data = ProjectFile.Load(path);

            // Load spritesheet
            _state.Sheet = new SpriteSheet(_graphicsDevice, data.Spritesheet.Path,
                                           data.Spritesheet.TileWidth, data.Spritesheet.TileHeight,
                                           data.Spritesheet.Padding);
            _state.SheetPath = data.Spritesheet.Path;

            // Restore groups
            _state.Groups = ProjectFile.RestoreGroups(data);
            _state.RebuildGroupIndex();

            // Restore map
            _state.Map = ProjectFile.RestoreMap(data);

            // Migrate groups with no layer assignment (old project files)
            string firstLayerName = _state.Map.Layers.Count > 0 ? _state.Map.Layers[0].Name : "Ground";
            foreach (var group in _state.Groups)
            {
                if (string.IsNullOrEmpty(group.LayerName))
                    group.LayerName = firstLayerName;
            }

            // Restore editor state
            if (data.EditorState != null)
            {
                _state.ActiveLayerName = data.EditorState.ActiveLayer ?? "Ground";
                _canvas.Camera.Offset = new Vector2(data.EditorState.CameraX, data.EditorState.CameraY);

                _panelDock.RestoreState(data.EditorState.PanelOrder, data.EditorState.CollapsedPanels);

                if (data.EditorState.CollapsedLayers != null)
                    _mapPanel.RestoreCollapsedLayers(data.EditorState.CollapsedLayers);
            }

            // Load quest definitions from quests.json
            string projectDir = Path.GetDirectoryName(Path.GetFullPath(path));
            _state.Quests = QuestFileManager.Load(projectDir);

            // Load dialogue definitions from dialogues/*.json
            _state.Dialogues = DialogueFileManager.LoadAll(projectDir);

            // Select first group if available
            if (_state.Groups.Count > 0 && _state.SelectedGroupName == null)
                _state.SelectedGroupName = _state.Groups[0].Name;

            _state.UndoStack.Clear();
            _state.SelectedEntityId = null;
            _state.ClearDirty();

            _projectPath = path;
            _window.Title = $"TileForge — {Path.GetFileName(path)}";
            _recentFiles.AddRecent(path);
        }
        catch
        {
            // Silently fail on corrupt project files
        }
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

            // Create a default map if none exists
            if (_state.Map == null)
            {
                _state.Map = new MapData(LayoutConstants.DefaultMapWidth, LayoutConstants.DefaultMapHeight);
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
        _window.Title = $"TileForge — {Path.GetFileName(path)}";

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
        var editorState = new ProjectFile.EditorStateData
        {
            ActiveLayer = _state.ActiveLayerName,
            CameraX = _canvas.Camera.Offset.X,
            CameraY = _canvas.Camera.Offset.Y,
            PanelOrder = _panelDock.GetPanelOrder(),
            CollapsedPanels = _panelDock.GetCollapsedPanels(),
            CollapsedLayers = _mapPanel.GetCollapsedLayers(),
        };

        ProjectFile.Save(path, _state.SheetPath, _state.Sheet, _state.Groups, _state.Map, editorState);
    }
}
