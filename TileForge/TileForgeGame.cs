using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Editor;
using TileForge.Editor.Commands;
using TileForge.Editor.Tools;
using TileForge.Export;
using TileForge.UI;

namespace TileForge;

public class TileForgeGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    private Renderer _renderer;
    private RasterizerState _scissorRasterizer;

    // UI regions
    private Toolbar _toolbar;
    private PanelDock _panelDock;
    private ToolPanel _toolPanel;
    private MapPanel _mapPanel;
    private TilePalettePanel _tilePalettePanel;
    private MapCanvas _canvas;
    private StatusBar _statusBar;
    private GroupEditor _groupEditor;

    // Central state
    private EditorState _state;

    // Input tracking
    private KeyboardState _prevKeyboard;
    private MouseState _prevMouse;

    // Managers
    private DialogManager _dialogManager;
    private ProjectManager _projectManager;
    private PlayModeController _playMode;
    private InputRouter _inputRouter;
    private AutoSaveManager _autoSave;

    // Pending new group layer context
    private string _pendingNewGroupLayer;

    public TileForgeGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = LayoutConstants.DefaultWindowWidth;
        _graphics.PreferredBackBufferHeight = LayoutConstants.DefaultWindowHeight;
        _graphics.ApplyChanges();
        Window.Title = "TileForge";
        Window.AllowUserResizing = true;
        Window.TextInput += OnTextInput;
        Window.FileDrop += OnFileDrop;
        Window.ClientSizeChanged += OnResize;
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("Font");
        _renderer = new Renderer(GraphicsDevice);
        _scissorRasterizer = new RasterizerState { ScissorTestEnable = true };

        _toolbar = new Toolbar();
        _canvas = new MapCanvas();
        _statusBar = new StatusBar();
        _toolPanel = new ToolPanel();
        _mapPanel = new MapPanel();
        _tilePalettePanel = new TilePalettePanel();
        _panelDock = new PanelDock();
        _panelDock.Panels.Add(_toolPanel);
        _panelDock.Panels.Add(_mapPanel);
        _panelDock.Panels.Add(_tilePalettePanel);

        _state = new EditorState { ActiveTool = new BrushTool() };

        _dialogManager = new DialogManager();
        _projectManager = new ProjectManager(_state, GraphicsDevice, Window,
            _canvas, _panelDock, _mapPanel, GetCanvasBounds, _dialogManager.Show);
        _playMode = new PlayModeController(_state, _canvas, GetCanvasBounds);
        _inputRouter = new InputRouter(_state,
            _projectManager.Save, _projectManager.Open,
            EnterPlayMode, ExitPlayMode, Exit, ResizeMap,
            _projectManager.OpenRecent, NewProject, ShowExportDialog,
            () => _canvas.Minimap.Toggle());
        _autoSave = new AutoSaveManager(_state,
            () => _projectManager.ProjectPath, _projectManager.SaveToPath);

        string defaultProject = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "TutorialProject", "TestCavernDungeon.tileforge"));
        if (File.Exists(defaultProject))
            LoadWithRecoveryCheck(defaultProject);
    }

    private void OnResize(object sender, EventArgs e)
    {
        _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
        _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
        _graphics.ApplyChanges();
    }

    private void OnTextInput(object sender, TextInputEventArgs e)
    {
        if (_dialogManager.IsActive) { _dialogManager.OnTextInput(e.Character); return; }
        _groupEditor?.OnTextInput(e.Character);
    }

    private void OnFileDrop(object sender, FileDropEventArgs e)
    {
        foreach (var file in e.Files)
        {
            if (file.EndsWith(".tileforge", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".tileforge2", StringComparison.OrdinalIgnoreCase))
            { LoadWithRecoveryCheck(file); return; }
            if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            { _projectManager.PromptTileSize(file); return; }
        }
    }

    private void LoadWithRecoveryCheck(string path)
    {
        string recoveryPath = AutoSaveManager.CheckForRecovery(path);
        if (recoveryPath != null)
        {
            _dialogManager.Show(new ConfirmDialog("An autosave was found. Recover unsaved changes?"), dialog =>
            {
                if (!dialog.WasCancelled)
                {
                    _projectManager.Load(recoveryPath);
                    // Mark dirty so user knows to save properly
                    _state.MarkDirty();
                }
                else
                {
                    _projectManager.Load(path);
                    AutoSaveManager.CleanupAutoSave(path);
                }
            });
        }
        else
        {
            _projectManager.Load(path);
        }
    }

    private void EnterPlayMode()
    {
        if (!_playMode.Enter())
            Window.Title = "TileForge — No player entity found (mark a group as Player in GroupEditor)";
    }

    private void ExitPlayMode() => _playMode.Exit();

    private void NewProject()
    {
        _projectManager.NewProject();
    }

    private void ResizeMap()
    {
        if (_state.Map == null) return;

        string defaultValue = $"{_state.Map.Width}x{_state.Map.Height}";
        _dialogManager.Show(new InputDialog("New size (e.g. 60x40):", defaultValue), dialog =>
        {
            var input = (InputDialog)dialog;
            if (input.WasCancelled) return;

            string text = input.ResultText.Trim();
            int xIdx = text.IndexOf('x', StringComparison.OrdinalIgnoreCase);
            if (xIdx < 0) return;

            if (!int.TryParse(text.AsSpan(0, xIdx), out int newWidth)) return;
            if (!int.TryParse(text.AsSpan(xIdx + 1), out int newHeight)) return;

            if (newWidth < 1 || newHeight < 1) return;
            if (newWidth == _state.Map.Width && newHeight == _state.Map.Height) return;

            var cmd = new ResizeMapCommand(_state.Map, newWidth, newHeight);
            cmd.Execute();
            _state.UndoStack.Push(cmd);
        });
    }

    private void ShowExportDialog()
    {
        if (_state.Map == null || _state.Sheet == null) return;

        string defaultPath = _projectManager.ProjectPath != null
            ? Path.ChangeExtension(_projectManager.ProjectPath, ".json")
            : "export.json";

        _dialogManager.Show(new ExportDialog(defaultPath), dialog =>
        {
            var exportDialog = (ExportDialog)dialog;
            if (exportDialog.WasCancelled) return;

            string path = exportDialog.OutputPath;
            if (exportDialog.SelectedFormat == ExportDialog.ExportFormat.Json)
            {
                string json = MapExporter.ExportJson(_state.Map, _state.Groups);
                File.WriteAllText(path, json);
            }
            else
            {
                PngExporter.Export(GraphicsDevice, _spriteBatch, _state.Map, _state.Groups,
                                  _state.GroupsByName, _state.Sheet, path);
            }
        });
    }

    private Rectangle GetCanvasBounds()
    {
        int screenW = _graphics.PreferredBackBufferWidth;
        int screenH = _graphics.PreferredBackBufferHeight;
        int leftOffset = _state.IsPlayMode ? 0 : PanelDock.Width;
        return new Rectangle(leftOffset, Toolbar.Height,
                             screenW - leftOffset,
                             screenH - Toolbar.Height - StatusBar.Height);
    }

    // --- Update ---

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        int screenH = _graphics.PreferredBackBufferHeight;

        // Dialog priority
        if (_dialogManager.Update(keyboard, _prevKeyboard, gameTime))
        { FinishUpdate(keyboard, mouse, gameTime); return; }

        // GroupEditor priority
        if (_groupEditor != null)
        {
            _groupEditor.Update(_state, mouse, _prevMouse, keyboard, _prevKeyboard, GetCanvasBounds());
            if (_groupEditor.IsComplete) { HandleGroupEditorResult(); _groupEditor = null; }
            FinishUpdate(keyboard, mouse, gameTime); return;
        }

        // Global keybinds
        if (_inputRouter.Update(keyboard, _prevKeyboard, mouse))
        {
            if (_state.IsPlayMode) _playMode.Update(gameTime, keyboard, _prevKeyboard);
            FinishUpdate(keyboard, mouse, gameTime); return;
        }

        // Play mode -- skip editor UI
        if (_state.IsPlayMode)
        {
            _playMode.Update(gameTime, keyboard, _prevKeyboard);
            FinishUpdate(keyboard, mouse, gameTime); return;
        }

        // Editor UI
        int screenW = _graphics.PreferredBackBufferWidth;
        var dockBounds = new Rectangle(0, Toolbar.Height, PanelDock.Width,
                                        screenH - Toolbar.Height - StatusBar.Height);
        _toolbar.Update(_state, mouse, _prevMouse, screenW, _font);
        _panelDock.Update(_state, mouse, _prevMouse, _font, dockBounds, gameTime, screenW, screenH);
        _canvas.Update(_state, mouse, _prevMouse, keyboard, _prevKeyboard, GetCanvasBounds());

        HandleMapPanelActions();
        HandleTilePaletteActions();
        HandleToolbarActions();

        FinishUpdate(keyboard, mouse, gameTime);
    }

    private void FinishUpdate(KeyboardState keyboard, MouseState mouse, GameTime gameTime)
    {
        _prevKeyboard = keyboard;
        _prevMouse = mouse;
        _autoSave.Update(gameTime.ElapsedGameTime.TotalSeconds);
        base.Update(gameTime);
    }

    private void HandleMapPanelActions()
    {
        if (_mapPanel.WantsNewGroupForLayer.Requested && _state.Sheet != null)
        {
            _pendingNewGroupLayer = _mapPanel.WantsNewGroupForLayer.LayerName;
            _groupEditor = GroupEditor.ForNewGroup();
            _groupEditor.CenterOnSheet(_state.Sheet, GetCanvasBounds());
        }
        else if (_mapPanel.WantsEditGroup != null)
        {
            if (_state.GroupsByName.TryGetValue(_mapPanel.WantsEditGroup, out var group))
            {
                _groupEditor = GroupEditor.ForExistingGroup(group);
                _groupEditor.CenterOnSheet(_state.Sheet, GetCanvasBounds());
            }
        }
        else if (_mapPanel.WantsDeleteGroup != null)
        {
            string name = _mapPanel.WantsDeleteGroup;
            _dialogManager.Show(new ConfirmDialog($"Delete group \"{name}\"?"), dialog =>
            { if (!dialog.WasCancelled) _state.RemoveGroup(name); });
        }

        if (_mapPanel.WantsNewLayer && _state.Map != null)
        {
            _dialogManager.Show(new InputDialog("Layer name:", ""), dialog =>
            {
                var input = (InputDialog)dialog;
                if (input.WasCancelled) return;
                string name = input.ResultText.Trim();
                if (string.IsNullOrEmpty(name) || _state.Map.HasLayer(name)) return;
                _state.Map.AddLayer(name);
                _state.ActiveLayerName = name;
            });
        }

        if (_mapPanel.PendingLayerReorder.HasValue)
        {
            var (fromIdx, toIdx) = _mapPanel.PendingLayerReorder.Value;
            var cmd = new ReorderLayerCommand(_state.Map, fromIdx, toIdx);
            cmd.Execute();
            _state.UndoStack.Push(cmd);
        }
    }

    private void HandleTilePaletteActions()
    {
        if (_tilePalettePanel.WantsEditGroup != null && _state.Sheet != null)
        {
            if (_state.GroupsByName.TryGetValue(_tilePalettePanel.WantsEditGroup, out var group))
            {
                _groupEditor = GroupEditor.ForExistingGroup(group);
                _groupEditor.CenterOnSheet(_state.Sheet, GetCanvasBounds());
            }
        }
    }

    private void HandleToolbarActions()
    {
        if (_toolbar.WantsUndo) _state.UndoStack.Undo();
        if (_toolbar.WantsRedo) _state.UndoStack.Redo();
        if (_toolbar.WantsSave) _projectManager.Save();
        if (_toolbar.WantsPlayToggle)
        { if (_state.IsPlayMode) ExitPlayMode(); else EnterPlayMode(); }
    }

    private void HandleGroupEditorResult()
    {
        if (_groupEditor.WasCancelled || _groupEditor.Result == null) return;
        var result = _groupEditor.Result;

        if (_groupEditor.IsNew)
        {
            if (_state.GroupsByName.ContainsKey(result.Name))
            {
                int suffix = 2;
                string baseName = result.Name;
                while (_state.GroupsByName.ContainsKey(result.Name))
                    result.Name = $"{baseName}{suffix++}";
            }
            result.LayerName = _pendingNewGroupLayer ?? _state.ActiveLayerName;
            _state.AddGroup(result);
            _state.SelectedGroupName = result.Name;
            _state.ActiveLayerName = result.LayerName;
            _pendingNewGroupLayer = null;
        }
        else
        {
            string oldName = _groupEditor.OriginalName;
            if (result.Name != oldName)
            {
                if (_state.GroupsByName.ContainsKey(result.Name)) result.Name = oldName;
                else _state.RenameGroup(oldName, result.Name);
            }
            if (_state.GroupsByName.TryGetValue(result.Name, out var existing))
            {
                existing.Type = result.Type;
                existing.Sprites = result.Sprites;
                existing.IsSolid = result.IsSolid;
                existing.IsPlayer = result.IsPlayer;
            }
        }
    }

    // --- Draw ---

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(LayoutConstants.WindowClearColor);
        int screenW = _graphics.PreferredBackBufferWidth;
        int screenH = _graphics.PreferredBackBufferHeight;
        var canvasBounds = GetCanvasBounds();

        // Pass 1: Canvas and GroupEditor, scissor-clipped
        GraphicsDevice.ScissorRectangle = canvasBounds;
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: _scissorRasterizer);
        _canvas.Draw(_spriteBatch, _state, _renderer, canvasBounds);
        if (!_state.IsPlayMode && _groupEditor != null)
            _groupEditor.Draw(_spriteBatch, _font, _state, _renderer, canvasBounds, gameTime);
        _spriteBatch.End();

        // Pass 2: UI chrome -- no clipping
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        if (!_state.IsPlayMode)
            _panelDock.Draw(_spriteBatch, _font, _state, _renderer);
        _toolbar.Draw(_spriteBatch, _font, _state, _renderer, screenW);
        _statusBar.Draw(_spriteBatch, _font, _state, _renderer, _canvas, screenW, screenH);
        _dialogManager.Draw(_spriteBatch, _font, _renderer, screenW, screenH, gameTime);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
