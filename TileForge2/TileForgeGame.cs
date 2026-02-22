using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge2.Data;
using TileForge2.Editor;
using TileForge2.Editor.Commands;
using TileForge2.Editor.Tools;
using TileForge2.Play;
using TileForge2.UI;

namespace TileForge2;

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
    private MapCanvas _canvas;
    private StatusBar _statusBar;

    // Modal overlay
    private GroupEditor _groupEditor;

    // Central state
    private EditorState _state;

    // Input tracking
    private KeyboardState _prevKeyboard;
    private MouseState _prevMouse;

    // Project
    private string _projectPath;
    private IDialog _activeDialog;
    private Action<IDialog> _onDialogComplete;

    // Play mode: saved editor camera state
    private Vector2 _savedCameraOffset;
    private int _savedZoomIndex;

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
        _graphics.PreferredBackBufferWidth = 1440;
        _graphics.PreferredBackBufferHeight = 900;
        _graphics.ApplyChanges();
        Window.Title = "TileForge2";
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

        // Panel dock with two panels
        _toolPanel = new ToolPanel();
        _mapPanel = new MapPanel();

        _panelDock = new PanelDock();
        _panelDock.Panels.Add(_toolPanel);
        _panelDock.Panels.Add(_mapPanel);

        _state = new EditorState
        {
            ActiveTool = new BrushTool(),
        };

        // Auto-load tutorial project if available
        string defaultProject = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "TutorialProject", "TestCavernDungeon.tileforge2");
        defaultProject = Path.GetFullPath(defaultProject);
        if (File.Exists(defaultProject))
            LoadProject(defaultProject);
    }

    // --- Window resize ---

    private void OnResize(object sender, EventArgs e)
    {
        _graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
        _graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
        _graphics.ApplyChanges();
    }

    // --- Input events ---

    private void OnTextInput(object sender, TextInputEventArgs e)
    {
        if (_activeDialog != null)
        {
            _activeDialog.OnTextInput(e.Character);
            return;
        }

        if (_groupEditor != null)
        {
            _groupEditor.OnTextInput(e.Character);
            return;
        }
    }

    private void OnFileDrop(object sender, FileDropEventArgs e)
    {
        foreach (var file in e.Files)
        {
            if (file.EndsWith(".tileforge2", StringComparison.OrdinalIgnoreCase))
            {
                LoadProject(file);
                return;
            }

            if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            {
                PromptTileSize(file);
                return;
            }
        }
    }

    // --- Spritesheet loading ---

    private void PromptTileSize(string imagePath)
    {
        ShowDialog(new InputDialog("Tile size (e.g. 16, 16x24, 16+1):", "16"), dialog =>
        {
            var input = (InputDialog)dialog;
            if (input.WasCancelled) return;

            if (ParseTileSize(input.ResultText, out int tw, out int th, out int padding))
            {
                LoadSpritesheet(imagePath, tw, th, padding);
            }
        });
    }

    private void LoadSpritesheet(string path, int tileWidth, int tileHeight, int padding)
    {
        try
        {
            _state.Sheet = new SpriteSheet(GraphicsDevice, path, tileWidth, tileHeight, padding);
            _state.SheetPath = path;

            // Create a default map if none exists
            if (_state.Map == null)
            {
                _state.Map = new MapData(40, 30);
                _state.UndoStack.Clear();
                _state.SelectedEntityId = null;
            }

            // Center camera on the map
            var canvasBounds = GetCanvasBounds();
            _canvas.CenterOnMap(_state, canvasBounds);

            Window.Title = $"TileForge2 — {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load spritesheet: {ex.Message}");
        }
    }

    private static bool ParseTileSize(string input, out int width, out int height, out int padding)
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

    // --- Project save/load ---

    private void SaveProject()
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

    private void PromptSavePath()
    {
        string defaultName = Path.GetFileNameWithoutExtension(_state.SheetPath ?? "project") + ".tileforge2";
        string startDir = Path.GetDirectoryName(_state.SheetPath ?? Environment.CurrentDirectory);

        var browser = new FileBrowserDialog("Save Project", startDir,
            new[] { ".tileforge2" }, FileBrowserMode.Save, defaultName);

        ShowDialog(browser, dialog =>
        {
            var fb = (FileBrowserDialog)dialog;
            if (fb.WasCancelled) return;
            DoSave(fb.ResultPath);
        });
    }

    private void DoSave(string path)
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

        _projectPath = path;
        Window.Title = $"TileForge2 — {Path.GetFileName(path)}";
    }

    private void LoadProject(string path)
    {
        try
        {
            var data = ProjectFile.Load(path);

            // Load spritesheet
            _state.Sheet = new SpriteSheet(GraphicsDevice, data.Spritesheet.Path,
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

            // Select first group if available
            if (_state.Groups.Count > 0 && _state.SelectedGroupName == null)
                _state.SelectedGroupName = _state.Groups[0].Name;

            _state.UndoStack.Clear();
            _state.SelectedEntityId = null;

            _projectPath = path;
            Window.Title = $"TileForge2 — {Path.GetFileName(path)}";
        }
        catch
        {
            // Silently fail on corrupt project files
        }
    }

    // --- Dialog system ---

    private void ShowDialog(IDialog dialog, Action<IDialog> onComplete)
    {
        _activeDialog = dialog;
        _onDialogComplete = onComplete;
    }

    // --- Open file ---

    private void OpenFile()
    {
        string startDir = _projectPath != null
            ? Path.GetDirectoryName(_projectPath)
            : (_state.SheetPath != null
                ? Path.GetDirectoryName(_state.SheetPath)
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

        var browser = new FileBrowserDialog("Open File", startDir,
            new[] { ".png", ".jpg", ".tileforge2" }, FileBrowserMode.Open);

        ShowDialog(browser, dialog =>
        {
            var fb = (FileBrowserDialog)dialog;
            if (fb.WasCancelled) return;

            string path = fb.ResultPath;
            if (path.EndsWith(".tileforge2", StringComparison.OrdinalIgnoreCase))
                LoadProject(path);
            else
                PromptTileSize(path);
        });
    }

    // --- GroupEditor result handling ---

    private void HandleGroupEditorResult()
    {
        if (_groupEditor.WasCancelled || _groupEditor.Result == null) return;

        var result = _groupEditor.Result;

        if (_groupEditor.IsNew)
        {
            // Guard against name collision — append suffix
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
            // Editing existing group
            string oldName = _groupEditor.OriginalName;

            // Handle rename
            if (result.Name != oldName)
            {
                if (_state.GroupsByName.ContainsKey(result.Name))
                    result.Name = oldName; // collision — keep old name
                else
                    _state.RenameGroup(oldName, result.Name);
            }

            // Update sprites, type, and properties on the existing group
            if (_state.GroupsByName.TryGetValue(result.Name, out var existing))
            {
                existing.Type = result.Type;
                existing.Sprites = result.Sprites;
                existing.IsSolid = result.IsSolid;
                existing.IsPlayer = result.IsPlayer;
            }
        }
    }

    // --- Layout helpers ---

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

        int screenW = _graphics.PreferredBackBufferWidth;
        int screenH = _graphics.PreferredBackBufferHeight;

        // Dialog priority
        if (_activeDialog != null)
        {
            _activeDialog.Update(keyboard, _prevKeyboard, gameTime);
            if (_activeDialog.IsComplete)
            {
                var dialog = _activeDialog;
                var callback = _onDialogComplete;
                _activeDialog = null;
                _onDialogComplete = null;
                callback?.Invoke(dialog);
            }
            _prevKeyboard = keyboard;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        // GroupEditor priority
        if (_groupEditor != null)
        {
            _groupEditor.Update(_state, mouse, _prevMouse, keyboard, _prevKeyboard, GetCanvasBounds());

            if (_groupEditor.IsComplete)
            {
                HandleGroupEditorResult();
                _groupEditor = null;
            }

            _prevKeyboard = keyboard;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        // Global keybinds
        bool ctrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

        if (ctrl && KeyPressed(keyboard, Keys.S))
        {
            SaveProject();
            _prevKeyboard = keyboard;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        if (ctrl && KeyPressed(keyboard, Keys.O))
        {
            OpenFile();
            _prevKeyboard = keyboard;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        if (ctrl && KeyPressed(keyboard, Keys.Z))
        {
            _state.UndoStack.Undo();
            _prevKeyboard = keyboard;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        if (ctrl && KeyPressed(keyboard, Keys.Y))
        {
            _state.UndoStack.Redo();
            _prevKeyboard = keyboard;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        // F5 toggles play mode
        if (KeyPressed(keyboard, Keys.F5))
        {
            if (_state.IsPlayMode)
                ExitPlayMode();
            else
                EnterPlayMode();
            _prevKeyboard = keyboard;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        if (KeyPressed(keyboard, Keys.Escape))
        {
            if (_state.IsPlayMode)
            {
                ExitPlayMode();
            }
            else if (_state.SelectedEntityId != null)
            {
                _state.SelectedEntityId = null;
            }
            else
            {
                Exit();
            }
            _prevKeyboard = keyboard;
            _prevMouse = mouse;
            return;
        }

        // Play mode update — skip all editor logic
        if (_state.IsPlayMode)
        {
            UpdatePlayMode(gameTime, keyboard);
            _prevKeyboard = keyboard;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        // Tool keybinds
        if (KeyPressed(keyboard, Keys.B))
            _state.ActiveTool = new BrushTool();
        if (KeyPressed(keyboard, Keys.E))
            _state.ActiveTool = new EraserTool();
        if (KeyPressed(keyboard, Keys.F))
            _state.ActiveTool = new FillTool();
        if (KeyPressed(keyboard, Keys.N))
            _state.ActiveTool = new EntityTool();

        // Delete selected entity
        if (KeyPressed(keyboard, Keys.Delete) && _state.SelectedEntityId != null && _state.Map != null)
        {
            var entity = _state.Map.Entities.Find(e => e.Id == _state.SelectedEntityId);
            if (entity != null)
            {
                _state.Map.Entities.Remove(entity);
                _state.SelectedEntityId = null;
                _state.UndoStack.Push(new RemoveEntityCommand(_state.Map, entity, _state));
            }
        }

        // Layer switching with Tab
        if (KeyPressed(keyboard, Keys.Tab) && _state.Map != null && _state.Map.Layers.Count > 1)
        {
            int currentIdx = _state.Map.Layers.FindIndex(l => l.Name == _state.ActiveLayerName);
            int nextIdx = (currentIdx + 1) % _state.Map.Layers.Count;
            _state.ActiveLayerName = _state.Map.Layers[nextIdx].Name;
        }

        // Layer visibility toggle
        if (KeyPressed(keyboard, Keys.V) && _state.Map != null)
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
                if (KeyPressed(keyboard, Keys.Up) && idx < _state.Map.Layers.Count - 1)
                    target = idx + 1;
                else if (KeyPressed(keyboard, Keys.Down) && idx > 0)
                    target = idx - 1;

                if (target >= 0)
                {
                    var cmd = new ReorderLayerCommand(_state.Map, idx, target);
                    cmd.Execute();
                    _state.UndoStack.Push(cmd);
                }
            }
        }

        // UI regions
        var dockBounds = new Rectangle(0, Toolbar.Height, PanelDock.Width,
                                        screenH - Toolbar.Height - StatusBar.Height);
        var canvasBounds = GetCanvasBounds();

        _toolbar.Update(_state, mouse, _prevMouse, screenW, _font);
        _panelDock.Update(_state, mouse, _prevMouse, _font, dockBounds, gameTime, screenW, screenH);
        _canvas.Update(_state, mouse, _prevMouse, keyboard, _prevKeyboard, canvasBounds);

        // Auto-switch tool based on selected group type
        var selected = _state.SelectedGroup;
        if (selected != null)
        {
            if (selected.Type == GroupType.Entity && _state.ActiveTool is not EntityTool)
                _state.ActiveTool = new EntityTool();
            else if (selected.Type == GroupType.Tile && _state.ActiveTool is EntityTool)
                _state.ActiveTool = new BrushTool();
        }

        // Handle map panel actions
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
            ShowDialog(new ConfirmDialog($"Delete group \"{name}\"?"), dialog =>
            {
                if (!dialog.WasCancelled)
                    _state.RemoveGroup(name);
            });
        }

        if (_mapPanel.WantsNewLayer && _state.Map != null)
        {
            ShowDialog(new InputDialog("Layer name:", ""), dialog =>
            {
                var input = (InputDialog)dialog;
                if (input.WasCancelled) return;

                string name = input.ResultText.Trim();
                if (string.IsNullOrEmpty(name)) return;
                if (_state.Map.HasLayer(name)) return;

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

        // Handle toolbar actions
        if (_toolbar.WantsUndo) _state.UndoStack.Undo();
        if (_toolbar.WantsRedo) _state.UndoStack.Redo();
        if (_toolbar.WantsSave) SaveProject();
        if (_toolbar.WantsPlayToggle)
        {
            if (_state.IsPlayMode) ExitPlayMode();
            else EnterPlayMode();
        }

        _prevKeyboard = keyboard;
        _prevMouse = mouse;
        base.Update(gameTime);
    }

    // --- Draw ---

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(30, 30, 30));

        int screenW = _graphics.PreferredBackBufferWidth;
        int screenH = _graphics.PreferredBackBufferHeight;

        var canvasBounds = GetCanvasBounds();

        // Pass 1: Canvas and GroupEditor, scissor-clipped to canvas bounds
        GraphicsDevice.ScissorRectangle = canvasBounds;
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: _scissorRasterizer);

        _canvas.Draw(_spriteBatch, _state, _renderer, canvasBounds);

        if (!_state.IsPlayMode && _groupEditor != null)
            _groupEditor.Draw(_spriteBatch, _font, _state, _renderer, canvasBounds, gameTime);

        _spriteBatch.End();

        // Pass 2: UI chrome (toolbar, panels, status bar, dialogs) — no clipping
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        if (!_state.IsPlayMode)
            _panelDock.Draw(_spriteBatch, _font, _state, _renderer);

        _toolbar.Draw(_spriteBatch, _font, _state, _renderer, screenW);
        _statusBar.Draw(_spriteBatch, _font, _state, _renderer, _canvas, screenW, screenH);

        // Dialog overlay
        if (_activeDialog != null)
            _activeDialog.Draw(_spriteBatch, _font, _renderer, screenW, screenH, gameTime);

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    // --- Play mode ---

    private void EnterPlayMode()
    {
        if (_state.Map == null || _state.Sheet == null) return;

        // Find the player entity: first entity whose group has IsPlayer
        Entity playerEntity = null;
        foreach (var entity in _state.Map.Entities)
        {
            if (_state.GroupsByName.TryGetValue(entity.GroupName, out var group) && group.IsPlayer)
            {
                playerEntity = entity;
                break;
            }
        }

        if (playerEntity == null)
        {
            Window.Title = "TileForge2 — No player entity found (mark a group as Player in GroupEditor)";
            return;
        }

        // Save editor camera state
        _savedCameraOffset = _canvas.Camera.Offset;
        _savedZoomIndex = _canvas.Camera.ZoomIndex;

        // Create play state
        _state.PlayState = new PlayState
        {
            PlayerEntity = playerEntity,
            RenderPos = new Vector2(playerEntity.X, playerEntity.Y),
        };
        _state.IsPlayMode = true;

        CenterCameraOnPlayer();
    }

    private void ExitPlayMode()
    {
        _canvas.Camera.Offset = _savedCameraOffset;
        _canvas.Camera.ZoomIndex = _savedZoomIndex;

        _state.IsPlayMode = false;
        _state.PlayState = null;
    }

    private void UpdatePlayMode(GameTime gameTime, KeyboardState keyboard)
    {
        var play = _state.PlayState;
        if (play == null) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Tick status message timer
        if (play.StatusMessageTimer > 0)
        {
            play.StatusMessageTimer -= dt;
            if (play.StatusMessageTimer <= 0)
                play.StatusMessage = null;
        }

        if (play.IsMoving)
        {
            // Continue lerp
            play.MoveProgress += dt / PlayState.MoveDuration;
            if (play.MoveProgress >= 1.0f)
            {
                play.MoveProgress = 1.0f;
                play.RenderPos = play.MoveTo;
                play.IsMoving = false;

                // Update entity grid position
                play.PlayerEntity.X = (int)play.MoveTo.X;
                play.PlayerEntity.Y = (int)play.MoveTo.Y;

                // Check for entity interaction at destination
                CheckEntityInteractionAt(play, play.PlayerEntity.X, play.PlayerEntity.Y);
            }
            else
            {
                play.RenderPos = Vector2.Lerp(play.MoveFrom, play.MoveTo, play.MoveProgress);
            }
        }

        if (!play.IsMoving)
        {
            // Accept movement input
            int dx = 0, dy = 0;

            if (KeyPressed(keyboard, Keys.Up)) dy = -1;
            else if (KeyPressed(keyboard, Keys.Down)) dy = 1;
            else if (KeyPressed(keyboard, Keys.Left)) dx = -1;
            else if (KeyPressed(keyboard, Keys.Right)) dx = 1;

            if (dx != 0 || dy != 0)
            {
                int targetX = play.PlayerEntity.X + dx;
                int targetY = play.PlayerEntity.Y + dy;

                if (CanMoveTo(targetX, targetY))
                {
                    play.MoveFrom = new Vector2(play.PlayerEntity.X, play.PlayerEntity.Y);
                    play.MoveTo = new Vector2(targetX, targetY);
                    play.MoveProgress = 0f;
                    play.IsMoving = true;
                }
                else if (_state.Map.InBounds(targetX, targetY))
                {
                    // Blocked — check for bump interaction with entity
                    CheckEntityInteractionAt(play, targetX, targetY);
                }
            }
        }

        CenterCameraOnPlayer();
    }

    private bool CanMoveTo(int x, int y)
    {
        var map = _state.Map;
        if (!map.InBounds(x, y)) return false;

        // Check all layers for solid groups
        foreach (var layer in map.Layers)
        {
            string groupName = layer.GetCell(x, y, map.Width);
            if (groupName != null
                && _state.GroupsByName.TryGetValue(groupName, out var group)
                && group.IsSolid)
            {
                return false;
            }
        }

        // Check entities (excluding player) for solid groups
        foreach (var entity in map.Entities)
        {
            if (entity == _state.PlayState.PlayerEntity) continue;
            if (entity.X == x && entity.Y == y
                && _state.GroupsByName.TryGetValue(entity.GroupName, out var group)
                && group.IsSolid)
            {
                return false;
            }
        }

        return true;
    }

    private void CheckEntityInteractionAt(PlayState play, int x, int y)
    {
        foreach (var entity in _state.Map.Entities)
        {
            if (entity == play.PlayerEntity) continue;
            if (entity.X == x && entity.Y == y)
            {
                play.StatusMessage = $"Interacted with {entity.GroupName}";
                play.StatusMessageTimer = PlayState.StatusMessageDuration;
                return;
            }
        }
    }

    private void CenterCameraOnPlayer()
    {
        var play = _state.PlayState;
        var sheet = _state.Sheet;
        var canvasBounds = GetCanvasBounds();

        // Player world pixel center
        float worldX = (play.RenderPos.X + 0.5f) * sheet.TileWidth;
        float worldY = (play.RenderPos.Y + 0.5f) * sheet.TileHeight;

        // Center on screen
        float screenCenterX = canvasBounds.X + canvasBounds.Width / 2f;
        float screenCenterY = canvasBounds.Y + canvasBounds.Height / 2f;
        int zoom = _canvas.Camera.Zoom;

        _canvas.Camera.Offset = new Vector2(
            screenCenterX - worldX * zoom,
            screenCenterY - worldY * zoom);
    }

    private bool KeyPressed(KeyboardState current, Keys key)
        => current.IsKeyDown(key) && _prevKeyboard.IsKeyUp(key);
}
