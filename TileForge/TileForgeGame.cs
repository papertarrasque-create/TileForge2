using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Editor.Commands;
using TileForge.Editor.Tools;
using TileForge.Export;
using TileForge.UI;

namespace TileForge;

public class TileForgeGame : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;
    private Renderer _renderer;
    private RasterizerState _scissorRasterizer;

    // UI regions
    private MenuBar _menuBar;
    private ToolbarRibbon _toolbarRibbon;
    private MenuActionDispatcher _menuDispatcher;
    private PanelDock _panelDock;
    private MapPanel _mapPanel;
    private TilePalettePanel _tilePalettePanel;
    private MapCanvas _canvas;
    private StatusBar _statusBar;
    private GroupEditor _groupEditor;
    private QuestPanel _questPanel;
    private QuestEditor _questEditor;
    private DialoguePanel _dialoguePanel;
    private DialogueTreeEditor _dialogueEditor;
    private WorldMapEditor _worldMapEditor;
    private MapTabBar _mapTabBar;

    // Central state
    private EditorState _state;
    private IProjectContext _projectContext;

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
    private bool _firstUpdateLogged;
    private bool _firstDrawLogged;

    public TileForgeGame()
    {
        DebugLog.Log("Constructor: start");
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        DebugLog.Log("Constructor: done");
    }

    protected override void Initialize()
    {
        DebugLog.Log("Initialize: start");
        try
        {
            _graphics.PreferredBackBufferWidth = LayoutConstants.DefaultWindowWidth;
            _graphics.PreferredBackBufferHeight = LayoutConstants.DefaultWindowHeight;
            _graphics.ApplyChanges();
            DebugLog.Log("Initialize: graphics applied");
            Window.Title = "TileForge";
            Window.AllowUserResizing = true;
            Window.TextInput += OnTextInput;
            Window.FileDrop += OnFileDrop;
            Window.ClientSizeChanged += OnResize;
            DebugLog.Log("Initialize: calling base.Initialize()");
            base.Initialize();
            DebugLog.Log("Initialize: done");
        }
        catch (Exception ex)
        {
            DebugLog.Error("Initialize failed", ex);
            throw;
        }
    }

    protected override void LoadContent()
    {
        DebugLog.Log("LoadContent: start");
        try
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("Font");
            _renderer = new Renderer(GraphicsDevice);
            _scissorRasterizer = new RasterizerState { ScissorTestEnable = true };
            DebugLog.Log("LoadContent: renderer + font loaded");

            _menuBar = new MenuBar(EditorMenus.CreateMenus());
            _toolbarRibbon = new ToolbarRibbon();
            _mapTabBar = new MapTabBar();
            _canvas = new MapCanvas();
            _statusBar = new StatusBar();
            _mapPanel = new MapPanel();
            _tilePalettePanel = new TilePalettePanel();
            _questPanel = new QuestPanel();
            _dialoguePanel = new DialoguePanel();
            _panelDock = new PanelDock();
            _panelDock.Panels.Add(_mapPanel);
            _panelDock.Panels.Add(_questPanel);
            _panelDock.Panels.Add(_dialoguePanel);
            _panelDock.Panels.Add(_tilePalettePanel);
            DebugLog.Log("LoadContent: UI panels created");

            _state = new EditorState { ActiveTool = new BrushTool() };
            DebugLog.Log("LoadContent: EditorState created");

            _dialogManager = new DialogManager();
            _projectManager = new ProjectManager(_state, GraphicsDevice, Window,
                _canvas, _panelDock, _mapPanel, GetCanvasBounds, _dialogManager.Show);
            _playMode = new PlayModeController(_state, _canvas, GetCanvasBounds);
            _inputRouter = new InputRouter(_state,
                _projectManager.Save, _projectManager.Open,
                EnterPlayMode, ExitPlayMode, Exit, ResizeMap,
                _projectManager.OpenRecent, NewProject, ShowExportDialog,
                () => _canvas.Minimap.Toggle());
            _menuDispatcher = new MenuActionDispatcher(new Dictionary<(int, int), Action>
            {
                { (EditorMenus.FileMenu, EditorMenus.File_New), NewProject },
                { (EditorMenus.FileMenu, EditorMenus.File_Open), _projectManager.Open },
                { (EditorMenus.FileMenu, EditorMenus.File_OpenRecent), _projectManager.OpenRecent },
                { (EditorMenus.FileMenu, EditorMenus.File_Save), _projectManager.Save },
                { (EditorMenus.FileMenu, EditorMenus.File_SaveAs), _projectManager.Save },
                { (EditorMenus.FileMenu, EditorMenus.File_Export), ShowExportDialog },
                { (EditorMenus.FileMenu, EditorMenus.File_Exit), Exit },
                { (EditorMenus.EditMenu, EditorMenus.Edit_Undo), _state.UndoStack.Undo },
                { (EditorMenus.EditMenu, EditorMenus.Edit_Redo), _state.UndoStack.Redo },
                { (EditorMenus.EditMenu, EditorMenus.Edit_Copy), () => { /* handled by InputRouter */ } },
                { (EditorMenus.EditMenu, EditorMenus.Edit_Paste), () => { /* handled by InputRouter */ } },
                { (EditorMenus.EditMenu, EditorMenus.Edit_Delete), () => { /* handled by InputRouter */ } },
                { (EditorMenus.EditMenu, EditorMenus.Edit_ResizeMap), ResizeMap },
                { (EditorMenus.ViewMenu, EditorMenus.View_ToggleMinimap), () => _canvas.Minimap.Toggle() },
                { (EditorMenus.ViewMenu, EditorMenus.View_CycleGrid), () => _state.Grid.CycleMode() },
                { (EditorMenus.ViewMenu, EditorMenus.View_ToggleLayerVisibility), () => { var l = _state.ActiveLayer; if (l != null) l.Visible = !l.Visible; } },
                { (EditorMenus.ViewMenu, EditorMenus.View_NextLayer), () => { /* handled by InputRouter */ } },
                { (EditorMenus.ViewMenu, EditorMenus.View_WorldMap), OpenWorldMap },
                { (EditorMenus.ToolsMenu, EditorMenus.Tools_Brush), () => _state.ActiveTool = new BrushTool() },
                { (EditorMenus.ToolsMenu, EditorMenus.Tools_Eraser), () => _state.ActiveTool = new EraserTool() },
                { (EditorMenus.ToolsMenu, EditorMenus.Tools_Fill), () => _state.ActiveTool = new FillTool() },
                { (EditorMenus.ToolsMenu, EditorMenus.Tools_Entity), () => _state.ActiveTool = new EntityTool() },
                { (EditorMenus.ToolsMenu, EditorMenus.Tools_Picker), () => _state.ActiveTool = new PickerTool() },
                { (EditorMenus.ToolsMenu, EditorMenus.Tools_Selection), () => _state.ActiveTool = new SelectionTool() },
                { (EditorMenus.PlayMenu, EditorMenus.Play_PlayStop), () => { if (_state.IsPlayMode) ExitPlayMode(); else EnterPlayMode(); } },
                { (EditorMenus.HelpMenu, EditorMenus.Help_Shortcuts), () => _dialogManager.Show(new ShortcutsDialog(), _ => { }) },
                { (EditorMenus.HelpMenu, EditorMenus.Help_About), () => _dialogManager.Show(new AboutDialog(), _ => { }) },
            });
            _projectContext = new ProjectContext(
                () => _projectManager.ProjectPath,
                () => _state.MapDocuments.Select(d => d.Name).ToList());
            _autoSave = new AutoSaveManager(_state,
                () => _projectManager.ProjectPath, _projectManager.SaveToPath);
            DebugLog.Log("LoadContent: all managers created");

            string defaultProject = Path.GetFullPath(Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "TutorialProject", "TestCavernDungeon.tileforge"));
            DebugLog.Log($"LoadContent: default project path = {defaultProject}, exists = {File.Exists(defaultProject)}");
            if (File.Exists(defaultProject))
            {
                DebugLog.Log("LoadContent: loading default project");
                LoadWithRecoveryCheck(defaultProject);
                DebugLog.Log("LoadContent: default project loaded");
            }

            DebugLog.Log("LoadContent: done");
        }
        catch (Exception ex)
        {
            DebugLog.Error("LoadContent failed", ex);
            throw;
        }
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
        if (_questEditor != null) { _questEditor.OnTextInput(e.Character); return; }
        if (_dialogueEditor != null) { _dialogueEditor.OnTextInput(e.Character); return; }
        if (_worldMapEditor != null) { _worldMapEditor.OnTextInput(e.Character); return; }
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
        // Set base directory for map transitions and dialogue loading
        if (_projectManager.ProjectPath != null)
            _playMode.MapBaseDirectory = Path.GetDirectoryName(_projectManager.ProjectPath);

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

    private void OpenWorldMap()
    {
        if (_state.MapDocuments.Count == 0) return;
        _worldMapEditor = WorldMapEditor.Open(_state.WorldLayout, _state.MapDocuments);
    }

    private void HandleWorldMapEditorResult()
    {
        if (_worldMapEditor == null) return;
        if (!_worldMapEditor.WasCancelled && _worldMapEditor.Result != null)
        {
            _state.WorldLayout = _worldMapEditor.Result;
            _state.MarkDirty();
        }
    }

    private Rectangle GetCanvasBounds()
    {
        int screenW = _graphics.PreferredBackBufferWidth;
        int screenH = _graphics.PreferredBackBufferHeight;
        int leftOffset = _state.IsPlayMode ? 0 : PanelDock.Width;
        int topOffset = _state.IsPlayMode
            ? LayoutConstants.PlayTopChromeHeight
            : LayoutConstants.TopChromeHeight;
        return new Rectangle(leftOffset, topOffset,
                             screenW - leftOffset,
                             screenH - topOffset - StatusBar.Height);
    }

    // --- Update ---

    protected override void Update(GameTime gameTime)
    {
        if (!_firstUpdateLogged) { DebugLog.Log("Update: first frame"); _firstUpdateLogged = true; }
        try
        {
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        int screenH = _graphics.PreferredBackBufferHeight;

        // Dialog priority
        if (_dialogManager.Update(keyboard, _prevKeyboard, gameTime))
        { FinishUpdate(keyboard, mouse, gameTime); return; }

        // QuestEditor priority (modal overlay)
        if (_questEditor != null)
        {
            int qScreenW = _graphics.PreferredBackBufferWidth;
            _questEditor.Update(mouse, _prevMouse, keyboard, _prevKeyboard,
                GetCanvasBounds(), _state.Quests, _font, qScreenW, screenH);
            if (_questEditor.IsComplete) { HandleQuestEditorResult(); _questEditor = null; }
            FinishUpdate(keyboard, mouse, gameTime); return;
        }

        // DialogueEditor priority (modal overlay)
        if (_dialogueEditor != null)
        {
            int dScreenW = _graphics.PreferredBackBufferWidth;
            _dialogueEditor.Update(mouse, _prevMouse, keyboard, _prevKeyboard,
                GetCanvasBounds(), _state.Dialogues, _font, dScreenW, screenH);
            if (_dialogueEditor.IsComplete) { HandleDialogueEditorResult(); _dialogueEditor = null; }
            FinishUpdate(keyboard, mouse, gameTime); return;
        }

        // WorldMapEditor priority (modal overlay)
        if (_worldMapEditor != null)
        {
            int wScreenW = _graphics.PreferredBackBufferWidth;
            _worldMapEditor.Update(mouse, _prevMouse, keyboard, _prevKeyboard,
                GetCanvasBounds(), _state.MapDocuments, _font, wScreenW, screenH);
            if (_worldMapEditor.IsComplete) { HandleWorldMapEditorResult(); _worldMapEditor = null; }
            FinishUpdate(keyboard, mouse, gameTime); return;
        }

        // GroupEditor priority
        if (_groupEditor != null)
        {
            int gScreenW = _graphics.PreferredBackBufferWidth;
            _groupEditor.Update(_state, mouse, _prevMouse, keyboard, _prevKeyboard,
                GetCanvasBounds(), _font, gScreenW, screenH);
            if (_groupEditor.IsComplete) { HandleGroupEditorResult(); _groupEditor = null; }
            else { HandleGroupEditorCreateSignals(); }
            FinishUpdate(keyboard, mouse, gameTime); return;
        }

        // Global keybinds
        if (_inputRouter.Update(keyboard, _prevKeyboard, mouse))
        {
            if (_state.IsPlayMode) _playMode.Update(gameTime, keyboard);
            FinishUpdate(keyboard, mouse, gameTime); return;
        }

        // Play mode -- skip editor UI (but update ribbon for play/stop button)
        if (_state.IsPlayMode)
        {
            _toolbarRibbon.Update(_state, mouse, _prevMouse,
                _graphics.PreferredBackBufferWidth, _font, gameTime);
            HandleRibbonActions();
            _playMode.Update(gameTime, keyboard);
            FinishUpdate(keyboard, mouse, gameTime); return;
        }

        // Menu bar (highest priority after modals)
        int screenW = _graphics.PreferredBackBufferWidth;
        UpdateMenuState();
        var menuResult = _menuBar.Update(mouse, _prevMouse, screenW, _font);
        if (menuResult.Menu >= 0)
            _menuDispatcher.Dispatch(menuResult.Menu, menuResult.Item);
        if (_menuBar.IsMenuOpen)
        { FinishUpdate(keyboard, mouse, gameTime); return; }

        // InputEvent for cross-component click consumption
        var input = new InputEvent(mouse, _prevMouse);

        // Toolbar ribbon (consumes before panels/canvas)
        _toolbarRibbon.Update(_state, input, screenW, _font, gameTime);

        // Map tab bar
        _mapTabBar.Update(_state, mouse, _prevMouse, screenW, _font, gameTime);
        HandleMapTabBarActions();

        // Editor panels (consumes before canvas)
        int topOffset = LayoutConstants.TopChromeHeight;
        var dockBounds = new Rectangle(0, topOffset, PanelDock.Width,
                                        screenH - topOffset - StatusBar.Height);
        _panelDock.Update(_state, mouse, _prevMouse, input, _font, dockBounds, gameTime, screenW, screenH);
        _canvas.Update(_state, input, keyboard, _prevKeyboard, GetCanvasBounds());

        HandleMapPanelActions();
        HandleQuestPanelActions();
        HandleDialoguePanelActions();
        HandleTilePaletteActions();
        HandleRibbonActions();

        FinishUpdate(keyboard, mouse, gameTime);
        }
        catch (Exception ex)
        {
            DebugLog.Error("Update failed", ex);
            throw;
        }
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
            _groupEditor = GroupEditor.ForNewGroup(_projectContext);
            _groupEditor.CenterOnSheet(_state.Sheet, GetCanvasBounds());
        }
        else if (_mapPanel.WantsEditGroup != null)
        {
            if (_state.GroupsByName.TryGetValue(_mapPanel.WantsEditGroup, out var group))
            {
                _groupEditor = GroupEditor.ForExistingGroup(group, _projectContext);
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
                _groupEditor = GroupEditor.ForExistingGroup(group, _projectContext);
                _groupEditor.CenterOnSheet(_state.Sheet, GetCanvasBounds());
            }
        }
    }

    private void HandleRibbonActions()
    {
        if (_toolbarRibbon.WantsNew) NewProject();
        if (_toolbarRibbon.WantsOpen) _projectManager.Open();
        if (_toolbarRibbon.WantsSave) _projectManager.Save();
        if (_toolbarRibbon.WantsUndo) _state.UndoStack.Undo();
        if (_toolbarRibbon.WantsRedo) _state.UndoStack.Redo();
        if (_toolbarRibbon.WantsExport) ShowExportDialog();
        if (_toolbarRibbon.WantsPlayToggle)
        { if (_state.IsPlayMode) ExitPlayMode(); else EnterPlayMode(); }

        int toolIdx = _toolbarRibbon.WantsToolIndex;
        if (toolIdx >= 0)
        {
            _state.ActiveTool = toolIdx switch
            {
                0 => new BrushTool(),
                1 => new EraserTool(),
                2 => new FillTool(),
                3 => new EntityTool(),
                4 => new PickerTool(),
                5 => new SelectionTool(),
                _ => _state.ActiveTool,
            };
        }
    }

    private void HandleMapTabBarActions()
    {
        if (_mapTabBar.WantsSelectTab >= 0 && _mapTabBar.WantsSelectTab != _state.ActiveMapIndex)
        {
            _projectManager.SyncCameraToActiveDocument();
            _state.ActiveMapIndex = _mapTabBar.WantsSelectTab;
            _projectManager.RestoreCameraFromActiveDocument();
        }

        if (_mapTabBar.WantsNewMap)
        {
            _dialogManager.Show(new InputDialog("New map name (e.g. dungeon 20x15):", ""), dialog =>
            {
                var input = (InputDialog)dialog;
                if (input.WasCancelled) return;
                string text = input.ResultText.Trim();
                if (string.IsNullOrEmpty(text)) return;

                // Parse "name WxH" or just "name"
                string name = text;
                int width = LayoutConstants.DefaultMapWidth;
                int height = LayoutConstants.DefaultMapHeight;
                int spaceIdx = text.LastIndexOf(' ');
                if (spaceIdx > 0)
                {
                    string sizePart = text[(spaceIdx + 1)..];
                    int xIdx = sizePart.IndexOf('x', StringComparison.OrdinalIgnoreCase);
                    if (xIdx > 0
                        && int.TryParse(sizePart[..xIdx], out int w)
                        && int.TryParse(sizePart[(xIdx + 1)..], out int h)
                        && w > 0 && h > 0)
                    {
                        name = text[..spaceIdx].Trim();
                        width = w;
                        height = h;
                    }
                }

                _projectManager.SyncCameraToActiveDocument();
                _projectManager.CreateNewMap(name, width, height);
                _projectManager.RestoreCameraFromActiveDocument();
            });
        }

        if (_mapTabBar.WantsCloseTab >= 0)
        {
            int idx = _mapTabBar.WantsCloseTab;
            if (idx < _state.MapDocuments.Count && _state.MapDocuments.Count > 1)
            {
                string mapName = _state.MapDocuments[idx].Name;
                _dialogManager.Show(new ConfirmDialog($"Delete map \"{mapName}\"?"), dialog =>
                {
                    if (!dialog.WasCancelled)
                    {
                        _projectManager.SyncCameraToActiveDocument();
                        _projectManager.DeleteMap(idx);
                        _projectManager.RestoreCameraFromActiveDocument();
                    }
                });
            }
        }

        if (_mapTabBar.WantsRenameTab >= 0)
        {
            int idx = _mapTabBar.WantsRenameTab;
            if (idx < _state.MapDocuments.Count)
            {
                string oldName = _state.MapDocuments[idx].Name;
                _dialogManager.Show(new InputDialog("Rename map:", oldName), dialog =>
                {
                    var input = (InputDialog)dialog;
                    if (input.WasCancelled) return;
                    string newName = input.ResultText.Trim();
                    if (!string.IsNullOrEmpty(newName) && newName != oldName)
                        _projectManager.RenameMap(idx, newName);
                });
            }
        }

        if (_mapTabBar.WantsDuplicateTab >= 0)
        {
            int idx = _mapTabBar.WantsDuplicateTab;
            if (idx < _state.MapDocuments.Count)
            {
                _projectManager.SyncCameraToActiveDocument();
                _projectManager.DuplicateMap(idx);
                _projectManager.RestoreCameraFromActiveDocument();
            }
        }
    }

    private void HandleGroupEditorResult()
    {
        if (_groupEditor.WasCancelled || _groupEditor.Result == null)
            return;

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
                if (_state.GroupsByName.ContainsKey(result.Name))
                    result.Name = oldName;
                else
                    _state.RenameGroup(oldName, result.Name);
            }
            if (_state.GroupsByName.TryGetValue(result.Name, out var existing))
            {
                existing.Type = result.Type;
                existing.Sprites = result.Sprites;
                existing.IsSolid = result.IsSolid;
                existing.IsPlayer = result.IsPlayer;
                existing.IsPassable = result.IsPassable;
                existing.IsHazardous = result.IsHazardous;
                existing.MovementCost = result.MovementCost;
                existing.DamageType = result.DamageType;
                existing.DamagePerTick = result.DamagePerTick;
                existing.EntityType = result.EntityType;
                existing.DefaultProperties = result.DefaultProperties;
            }
        }
    }

    private void UpdateMenuState()
    {
        _menuBar.SetItemEnabled(EditorMenus.EditMenu, EditorMenus.Edit_Undo, _state.UndoStack.CanUndo);
        _menuBar.SetItemEnabled(EditorMenus.EditMenu, EditorMenus.Edit_Redo, _state.UndoStack.CanRedo);
        _menuBar.SetItemEnabled(EditorMenus.FileMenu, EditorMenus.File_Export, _state.Sheet != null);
        _menuBar.SetItemEnabled(EditorMenus.EditMenu, EditorMenus.Edit_ResizeMap, _state.Map != null);
    }

    private void HandleGroupEditorCreateSignals()
    {
        if (_groupEditor == null) return;

        if (_groupEditor.WantsCreateMap)
        {
            _dialogManager.Show(new InputDialog("New map name:", ""), dialog =>
            {
                var input = (InputDialog)dialog;
                if (input.WasCancelled || _groupEditor == null) return;
                string name = input.ResultText.Trim();
                if (string.IsNullOrEmpty(name)) return;
                // Strip .tileforge extension if user typed it
                if (name.EndsWith(".tileforge", StringComparison.OrdinalIgnoreCase))
                    name = name[..^10];

                // Create a new map document within the project
                _projectManager.CreateNewMap(name,
                    LayoutConstants.DefaultMapWidth, LayoutConstants.DefaultMapHeight);
                // Switch back to the previous tab so the user stays in the GroupEditor
                if (_state.MapDocuments.Count > 1)
                    _state.ActiveMapIndex = _state.MapDocuments.Count - 2;
                _groupEditor.RefreshBrowseField("target_map", name);
            });
        }

        if (_groupEditor.WantsCreateDialogue)
        {
            _dialogManager.Show(new InputDialog("New dialogue name:", ""), dialog =>
            {
                var input = (InputDialog)dialog;
                if (input.WasCancelled || _groupEditor == null) return;
                string name = input.ResultText.Trim();
                if (string.IsNullOrEmpty(name)) return;
                if (name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    name = name[..^5];
                string dir = _projectContext.ProjectDirectory;
                if (dir != null)
                {
                    var emptyDialogue = new Game.DialogueData { Id = name, Nodes = new() };
                    DialogueFileManager.SaveOne(dir, emptyDialogue);
                    _state.Dialogues.Add(emptyDialogue);
                    _state.NotifyDialoguesChanged();
                    foreach (var k in new[] { "dialogue", "dialogue_id" })
                        _groupEditor.RefreshBrowseField(k, name);
                }
            });
        }
    }

    private void HandleQuestPanelActions()
    {
        if (_questPanel.WantsNewQuest)
        {
            _questEditor = QuestEditor.ForNewQuest();
        }
        else if (_questPanel.WantsEditQuestIndex >= 0)
        {
            int idx = _questPanel.WantsEditQuestIndex;
            if (idx < _state.Quests.Count)
                _questEditor = QuestEditor.ForExistingQuest(_state.Quests[idx]);
        }
        else if (_questPanel.WantsDeleteQuestIndex >= 0)
        {
            int idx = _questPanel.WantsDeleteQuestIndex;
            if (idx < _state.Quests.Count)
            {
                string name = _state.Quests[idx].Name ?? _state.Quests[idx].Id;
                int capturedIdx = idx;
                _dialogManager.Show(new ConfirmDialog($"Delete quest \"{name}\"?"), dialog =>
                {
                    if (!dialog.WasCancelled && capturedIdx < _state.Quests.Count)
                    {
                        _state.Quests.RemoveAt(capturedIdx);
                        SaveQuests();
                        _state.NotifyQuestsChanged();
                    }
                });
            }
        }
    }

    private void HandleQuestEditorResult()
    {
        if (_questEditor.WasCancelled || _questEditor.Result == null) return;
        var result = _questEditor.Result;

        if (_questEditor.IsNew)
        {
            _state.Quests.Add(result);
        }
        else
        {
            // Replace existing quest by original id
            string origId = _questEditor.OriginalId;
            int idx = _state.Quests.FindIndex(q => q.Id == origId);
            if (idx >= 0)
                _state.Quests[idx] = result;
            else
                _state.Quests.Add(result);
        }

        SaveQuests();
        _state.NotifyQuestsChanged();
    }

    private void SaveQuests()
    {
        if (_projectManager.ProjectPath == null) return;
        string projectDir = Path.GetDirectoryName(_projectManager.ProjectPath);
        QuestFileManager.Save(projectDir, _state.Quests);
    }

    private void HandleDialoguePanelActions()
    {
        if (_dialoguePanel.WantsNewDialogue)
        {
            _dialogueEditor = DialogueTreeEditor.ForNewDialogue();
        }
        else if (_dialoguePanel.WantsEditDialogueIndex >= 0)
        {
            int idx = _dialoguePanel.WantsEditDialogueIndex;
            if (idx < _state.Dialogues.Count)
                _dialogueEditor = DialogueTreeEditor.ForExistingDialogue(_state.Dialogues[idx]);
        }
        else if (_dialoguePanel.WantsDeleteDialogueIndex >= 0)
        {
            int idx = _dialoguePanel.WantsDeleteDialogueIndex;
            if (idx < _state.Dialogues.Count)
            {
                string name = _state.Dialogues[idx].Id;
                int capturedIdx = idx;
                _dialogManager.Show(new ConfirmDialog($"Delete dialogue \"{name}\"?"), dialog =>
                {
                    if (!dialog.WasCancelled && capturedIdx < _state.Dialogues.Count)
                    {
                        string deletedId = _state.Dialogues[capturedIdx].Id;
                        _state.Dialogues.RemoveAt(capturedIdx);
                        if (_projectManager.ProjectPath != null)
                        {
                            string projectDir = Path.GetDirectoryName(_projectManager.ProjectPath);
                            DialogueFileManager.DeleteOne(projectDir, deletedId);
                        }
                        _state.NotifyDialoguesChanged();
                    }
                });
            }
        }
    }

    private void HandleDialogueEditorResult()
    {
        if (_dialogueEditor.WasCancelled || _dialogueEditor.Result == null) return;
        var result = _dialogueEditor.Result;

        if (_dialogueEditor.IsNew)
        {
            _state.Dialogues.Add(result);
        }
        else
        {
            string origId = _dialogueEditor.OriginalId;
            int idx = _state.Dialogues.FindIndex(d => d.Id == origId);
            if (idx >= 0)
            {
                // If id changed, delete old file
                if (result.Id != origId && _projectManager.ProjectPath != null)
                {
                    string projectDir = Path.GetDirectoryName(_projectManager.ProjectPath);
                    DialogueFileManager.DeleteOne(projectDir, origId);
                }
                _state.Dialogues[idx] = result;
            }
            else
            {
                _state.Dialogues.Add(result);
            }
        }

        SaveDialogue(result);
        _state.NotifyDialoguesChanged();
    }

    private void SaveDialogue(Game.DialogueData dialogue)
    {
        if (_projectManager.ProjectPath == null) return;
        string projectDir = Path.GetDirectoryName(_projectManager.ProjectPath);
        DialogueFileManager.SaveOne(projectDir, dialogue);
    }

    // --- Draw ---

    protected override void Draw(GameTime gameTime)
    {
        if (!_firstDrawLogged) { DebugLog.Log("Draw: first frame"); _firstDrawLogged = true; }
        try
        {
            GraphicsDevice.Clear(LayoutConstants.WindowClearColor);
            int screenW = _graphics.PreferredBackBufferWidth;
            int screenH = _graphics.PreferredBackBufferHeight;
            var canvasBounds = GetCanvasBounds();

            // Pass 1: Scissor-clipped content
            GraphicsDevice.ScissorRectangle = canvasBounds;
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: _scissorRasterizer);
            if (_state.IsPlayMode)
            {
                _playMode.Draw(_spriteBatch, _font, _renderer, canvasBounds);
            }
            else
            {
                _canvas.Draw(_spriteBatch, _state, _renderer, canvasBounds);
                if (_groupEditor != null)
                    _groupEditor.Draw(_spriteBatch, _font, _state, _renderer, canvasBounds, gameTime);
                if (_questEditor != null)
                    _questEditor.Draw(_spriteBatch, _font, _renderer, canvasBounds, gameTime);
                if (_dialogueEditor != null)
                    _dialogueEditor.Draw(_spriteBatch, _font, _renderer, canvasBounds, gameTime);
                if (_worldMapEditor != null)
                    _worldMapEditor.Draw(_spriteBatch, _font, _renderer, canvasBounds, gameTime);
            }
            _spriteBatch.End();

            // Pass 2: UI chrome -- no clipping
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            if (!_state.IsPlayMode)
            {
                _panelDock.Draw(_spriteBatch, _font, _state, _renderer);
                _menuBar.Draw(_spriteBatch, _font, _renderer, screenW);
                _mapTabBar.Draw(_spriteBatch, _font, _renderer, _state, screenW);
            }
            _toolbarRibbon.Draw(_spriteBatch, _font, _state, _renderer, screenW);
            _statusBar.Draw(_spriteBatch, _font, _state, _renderer, _canvas, screenW, screenH);
            // Menu submenus drawn last (on top of everything)
            if (!_state.IsPlayMode)
                _menuBar.DrawSubmenu(_spriteBatch, _font, _renderer);
            _dialogManager.Draw(_spriteBatch, _font, _renderer, screenW, screenH, gameTime);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
        catch (Exception ex)
        {
            DebugLog.Error("Draw failed", ex);
            throw;
        }
    }
}
