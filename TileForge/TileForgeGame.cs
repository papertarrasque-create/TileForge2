using System;
using System.IO;
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
    private DialogueEditor _dialogueEditor;

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

        _menuBar = new MenuBar(EditorMenus.CreateMenus());
        _toolbarRibbon = new ToolbarRibbon();
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
        _menuDispatcher = new MenuActionDispatcher(
            NewProject, _projectManager.Open, _projectManager.OpenRecent,
            _projectManager.Save, _projectManager.Save, ShowExportDialog, Exit,
            _state.UndoStack.Undo, _state.UndoStack.Redo,
            () => { /* copy: handled by InputRouter */ },
            () => { /* paste: handled by InputRouter */ },
            () => { /* delete: handled by InputRouter */ },
            ResizeMap,
            () => _canvas.Minimap.Toggle(),
            () => _state.Grid.CycleMode(),
            () => { var l = _state.ActiveLayer; if (l != null) l.Visible = !l.Visible; },
            () => { /* next layer: handled by InputRouter */ },
            () => _state.ActiveTool = new BrushTool(),
            () => _state.ActiveTool = new EraserTool(),
            () => _state.ActiveTool = new FillTool(),
            () => _state.ActiveTool = new EntityTool(),
            () => _state.ActiveTool = new PickerTool(),
            () => _state.ActiveTool = new SelectionTool(),
            () => { if (_state.IsPlayMode) ExitPlayMode(); else EnterPlayMode(); },
            () => _dialogManager.Show(new ShortcutsDialog(), _ => { }),
            () => _dialogManager.Show(new AboutDialog(), _ => { }));
        _projectContext = new ProjectContext(() => _projectManager.ProjectPath);
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
        if (_questEditor != null) { _questEditor.OnTextInput(e.Character); return; }
        if (_dialogueEditor != null) { _dialogueEditor.OnTextInput(e.Character); return; }
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
        var menuResult = _menuBar.Update(mouse, _prevMouse, screenW);
        if (menuResult.Menu >= 0)
            _menuDispatcher.Dispatch(menuResult.Menu, menuResult.Item);
        if (_menuBar.IsMenuOpen)
        { FinishUpdate(keyboard, mouse, gameTime); return; }

        // Toolbar ribbon
        _toolbarRibbon.Update(_state, mouse, _prevMouse, screenW, _font, gameTime);

        // Editor panels
        int topOffset = LayoutConstants.TopChromeHeight;
        var dockBounds = new Rectangle(0, topOffset, PanelDock.Width,
                                        screenH - topOffset - StatusBar.Height);
        _panelDock.Update(_state, mouse, _prevMouse, _font, dockBounds, gameTime, screenW, screenH);
        _canvas.Update(_state, mouse, _prevMouse, keyboard, _prevKeyboard, GetCanvasBounds());

        HandleMapPanelActions();
        HandleQuestPanelActions();
        HandleDialoguePanelActions();
        HandleTilePaletteActions();
        HandleRibbonActions();

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
                if (!name.EndsWith(".tileforge", StringComparison.OrdinalIgnoreCase))
                    name += ".tileforge";
                string dir = _projectContext.ProjectDirectory;
                if (dir != null)
                {
                    string path = Path.Combine(dir, name);
                    if (!File.Exists(path))
                        File.WriteAllText(path, "{}");
                    _groupEditor.RefreshBrowseField("target_map",
                        Path.GetFileNameWithoutExtension(name));
                }
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
            _dialogueEditor = DialogueEditor.ForNewDialogue();
        }
        else if (_dialoguePanel.WantsEditDialogueIndex >= 0)
        {
            int idx = _dialoguePanel.WantsEditDialogueIndex;
            if (idx < _state.Dialogues.Count)
                _dialogueEditor = DialogueEditor.ForExistingDialogue(_state.Dialogues[idx]);
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
        }
        _spriteBatch.End();

        // Pass 2: UI chrome -- no clipping
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        if (!_state.IsPlayMode)
        {
            _panelDock.Draw(_spriteBatch, _font, _state, _renderer);
            _menuBar.Draw(_spriteBatch, _font, _renderer, screenW);
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
}
