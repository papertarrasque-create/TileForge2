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
using TileForge.Export;
using TileForge.Game;
using TileForge.Game.Screens;
using TileForge.Infrastructure;
using TileForge.Play;
using TileForge.UI;

namespace TileForge;

public class PlayModeController
{
    private readonly EditorState _state;
    private readonly MapCanvas _canvas;
    private readonly Func<Rectangle> _getCanvasBounds;
    private readonly IPathResolver _pathResolver;
    private readonly Minimap _minimap;

    private Vector2 _savedCameraOffset;
    private int _savedZoomIndex;
    private int _savedActiveMapIndex;
    private List<MapDocumentState> _savedMapDocuments;
    private List<TileGroup> _savedGroups;
    private GameStateManager _gameStateManager;
    private GameInputManager _inputManager;
    private ScreenManager _screenManager;
    private SaveManager _saveManager;
    private QuestManager _questManager;
    private string _bindingsPath;
    private GamePlayContext _context;

    // Pre-exported project maps for in-project transitions
    private Dictionary<string, LoadedMap> _projectMaps;
    private EdgeTransitionResolver _edgeResolver;

    // Sidebar HUD (retro CRPG style)
    private GameLog _gameLog;
    private SidebarHUD _sidebarHUD;

    public GameStateManager GameStateManager => _gameStateManager;
    public ScreenManager ScreenManager => _screenManager;
    public GameLog GameLog => _gameLog;
    public SidebarHUD SidebarHUD => _sidebarHUD;

    /// <summary>
    /// Optional base directory for resolving relative map paths in transitions.
    /// If null, target_map paths are used as-is.
    /// </summary>
    public string MapBaseDirectory { get; set; }

    public PlayModeController(EditorState state, MapCanvas canvas, Func<Rectangle> getCanvasBounds,
        IPathResolver pathResolver = null, Minimap minimap = null)
    {
        _state = state;
        _canvas = canvas;
        _getCanvasBounds = getCanvasBounds;
        _pathResolver = pathResolver ?? new DefaultPathResolver();
        _minimap = minimap;
    }

    /// <summary>
    /// Attempts to enter play mode. Returns false if no player entity was found.
    /// </summary>
    public bool Enter()
    {
        if (_state.Map == null || _state.Sheet == null) return false;

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
            return false;

        // Save editor state for restoration on exit (deep copy so play mode mutations don't corrupt the snapshot)
        _savedCameraOffset = _canvas.Camera.Offset;
        _savedZoomIndex = _canvas.Camera.ZoomIndex;
        _savedActiveMapIndex = _state.ActiveMapIndex;
        _savedMapDocuments = _state.MapDocuments.ConvertAll(d => d.DeepCopy());
        _savedGroups = _state.Groups.ConvertAll(g => g.DeepCopy());

        // Pre-export all project maps for in-project transitions
        _projectMaps = new Dictionary<string, LoadedMap>(StringComparer.OrdinalIgnoreCase);
        foreach (var doc in _state.MapDocuments)
        {
            if (doc.Map == null) continue;
            try
            {
                string json = MapExporter.ExportJson(doc.Map, _state.Groups);
                var loader = new MapLoader();
                var loadedMap = loader.Load(json, doc.Name);
                _projectMaps[doc.Name] = loadedMap;
            }
            catch
            {
                // Skip maps that fail to export
            }
        }

        // Build edge transition resolver from WorldLayout (if configured)
        if (_state.WorldLayout != null)
            _edgeResolver = new EdgeTransitionResolver(_state.WorldLayout, _projectMaps);
        else
            _edgeResolver = null;

        // Initialize game state
        _gameStateManager = new GameStateManager();
        _gameStateManager.Initialize(_state.Map, _state.GroupsByName);

        // Set initial map identity for edge transitions and save/load
        _gameStateManager.State.CurrentMapId = _state.ActiveMapDocument?.Name;

        // Initialize input, screen management, and save manager
        _inputManager = new GameInputManager();
        _bindingsPath = _pathResolver.KeybindingsPath;
        _inputManager.LoadBindings(_bindingsPath);
        _screenManager = new ScreenManager();
        _saveManager = new SaveManager();
        _questManager = LoadQuests();

        // Build dialogue loader and shared context for gameplay screens
        IDialogueLoader dialogueLoader = !string.IsNullOrEmpty(MapBaseDirectory)
            ? new FileDialogueLoader(MapBaseDirectory)
            : null;
        _context = new GamePlayContext(
            _gameStateManager, _saveManager, _inputManager,
            _bindingsPath, _questManager,
            _getCanvasBounds, _edgeResolver, dialogueLoader);

        // Create play state (rendering/lerp + AP)
        _state.PlayState = new PlayState
        {
            PlayerEntity = playerEntity,
            RenderPos = new Vector2(playerEntity.X, playerEntity.Y),
            PlayerAP = _gameStateManager.GetEffectiveMaxAP(),
            IsPlayerTurn = true,
        };
        _state.IsPlayMode = true;

        // Initialize sidebar HUD and game log
        _gameLog = new GameLog();
        _sidebarHUD = new SidebarHUD(_gameStateManager, _gameLog, () => _state.PlayState);

        // Push the gameplay screen
        _screenManager.Push(new GameplayScreen(_state, _canvas, _context, _gameLog));

        return true;
    }

    public void Exit(bool revert = true)
    {
        _screenManager?.Clear();

        _canvas.Camera.Offset = _savedCameraOffset;
        _canvas.Camera.ZoomIndex = _savedZoomIndex;

        // Restore editor state from snapshot (multimap-aware)
        if (revert && _savedMapDocuments != null)
        {
            _state.MapDocuments.Clear();
            _state.MapDocuments.AddRange(_savedMapDocuments);
            _state.Groups = new List<TileGroup>(_savedGroups);
            _state.RebuildGroupIndex();
            _state.ActiveMapIndex = _savedActiveMapIndex;
        }
        _savedMapDocuments = null;
        _savedGroups = null;

        _state.IsPlayMode = false;
        _state.PlayState = null;
        _gameStateManager = null;
        _inputManager = null;
        _screenManager = null;
        _saveManager = null;
        _questManager = null;
        _bindingsPath = null;
        _context = null;
        _projectMaps = null;
        _edgeResolver = null;
        _gameLog = null;
        _sidebarHUD = null;
    }

    public void Update(GameTime gameTime, KeyboardState keyboard,
        int mouseScrollDelta = 0, bool mouseOverSidebar = false)
    {
        if (_state.PlayState == null) return;

        _inputManager.Update(keyboard);
        _screenManager.Update(gameTime, _inputManager);
        _sidebarHUD?.Update();

        // Mouse wheel scrolls the sidebar log when hovering over it
        if (mouseScrollDelta != 0 && mouseOverSidebar && _sidebarHUD != null)
        {
            // ScrollWheelValue increases when scrolling up; negative delta = scroll up (older)
            int entries = mouseScrollDelta > 0 ? -3 : 3;
            _sidebarHUD.ScrollLog(entries);
        }

        // Check for pending map transition (set by GameplayScreen on trigger)
        if (_gameStateManager?.PendingTransition != null)
        {
            var transition = _gameStateManager.PendingTransition;
            _gameStateManager.PendingTransition = null;
            ExecuteMapTransition(transition);
        }

        if (_gameStateManager?.RestartRequested == true)
        {
            _gameStateManager.RestartRequested = false;
            Exit();
            Enter();
            return;
        }

        if (_screenManager.ExitRequested)
            Exit();
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font,
        Renderer renderer, Rectangle canvasBounds)
    {
        _screenManager?.Draw(spriteBatch, font, renderer, canvasBounds);
    }

    /// <summary>
    /// Draws the sidebar HUD in its own region (right of the game viewport).
    /// Called outside the game viewport scissor rect.
    /// </summary>
    public void DrawSidebar(SpriteBatch spriteBatch, SpriteFont font,
        Renderer renderer, Rectangle sidebarBounds)
    {
        // Feed map dimensions for adaptive minimap sizing
        if (_sidebarHUD != null && _state.Map != null)
        {
            _sidebarHUD.MapWidth = _state.Map.Width;
            _sidebarHUD.MapHeight = _state.Map.Height;
        }

        _sidebarHUD?.Draw(spriteBatch, font, renderer, sidebarBounds);

        // Draw minimap in the reserved space at the bottom of the sidebar
        if (_minimap != null && _sidebarHUD != null)
        {
            var mmRect = _sidebarHUD.MinimapRect;
            if (mmRect.Width > 0 && mmRect.Height > 0)
                _minimap.DrawInRect(spriteBatch, _state, renderer, _canvas.Camera,
                    mmRect, _getCanvasBounds());
        }
    }

    private void ExecuteMapTransition(MapTransitionRequest request)
    {
        // Try in-project maps first
        if (_projectMaps != null && _projectMaps.TryGetValue(request.TargetMap, out var projectMap))
        {
            ExecuteTransitionWithLoadedMap(projectMap, request);
            return;
        }

        // Fallback to filesystem (for cross-project/external maps)
        string mapPath = request.TargetMap;
        if (MapBaseDirectory != null && !Path.IsPathRooted(mapPath))
            mapPath = Path.Combine(MapBaseDirectory, mapPath);

        if (!File.Exists(mapPath))
        {
            _state.PlayState.AddFloatingMessage($"Map not found: {request.TargetMap}", Microsoft.Xna.Framework.Color.Red, _state.PlayState.PlayerEntity.X, _state.PlayState.PlayerEntity.Y);
            return;
        }

        // Load the target map from file
        string json = File.ReadAllText(mapPath);
        var loader = new MapLoader();
        var loadedMap = loader.Load(json, Path.GetFileNameWithoutExtension(mapPath));

        ExecuteTransitionWithLoadedMap(loadedMap, request);
    }

    private void ExecuteTransitionWithLoadedMap(LoadedMap loadedMap, MapTransitionRequest request)
    {
        // Switch game state (preserves flags, variables, inventory, health)
        _gameStateManager.SwitchMap(loadedMap, request.TargetX, request.TargetY);

        // Update editor state for rendering
        ApplyLoadedMapToEditorState(loadedMap);

        // Find or create the player entity for rendering
        var playerGroupName = _state.Groups.FirstOrDefault(g => g.IsPlayer)?.Name;
        if (playerGroupName == null)
        {
            // Carry forward the player group from the original map
            var originalPlayerGroup = _savedGroups?.FirstOrDefault(g => g.IsPlayer);
            if (originalPlayerGroup != null)
            {
                _state.AddGroup(originalPlayerGroup);
                playerGroupName = originalPlayerGroup.Name;
            }
        }

        var playerEntity = new Entity
        {
            Id = "player",
            GroupName = playerGroupName ?? "player",
            X = request.TargetX,
            Y = request.TargetY,
        };
        _state.Map.Entities.Add(playerEntity);

        // Reset play state for new position (AP refills on map transition)
        _state.PlayState = new PlayState
        {
            PlayerEntity = playerEntity,
            RenderPos = new Vector2(request.TargetX, request.TargetY),
            PlayerAP = _gameStateManager.GetEffectiveMaxAP(),
            IsPlayerTurn = true,
        };

        // Pop the old gameplay screen and push a new one (re-centers camera)
        _screenManager.Clear();
        _screenManager.Push(new GameplayScreen(_state, _canvas, _context, _gameLog));
    }

    private QuestManager LoadQuests()
    {
        if (string.IsNullOrEmpty(MapBaseDirectory))
            return new QuestManager(new List<QuestDefinition>());

        string questPath = Path.Combine(MapBaseDirectory, "quests.json");
        var quests = QuestLoader.Load(questPath);
        return new QuestManager(quests);
    }

    private void ApplyLoadedMapToEditorState(LoadedMap loadedMap)
    {
        // Build MapData from LoadedMap
        var mapData = new MapData(loadedMap.Width, loadedMap.Height);
        mapData.Layers.Clear();

        foreach (var layer in loadedMap.Layers)
        {
            var editorLayer = new MapLayer(layer.Name, loadedMap.Width, loadedMap.Height);
            if (layer.Cells != null)
            {
                int copyLen = Math.Min(layer.Cells.Length, editorLayer.Cells.Length);
                Array.Copy(layer.Cells, editorLayer.Cells, copyLen);
            }
            mapData.Layers.Add(editorLayer);
        }

        mapData.EntityRenderOrder = Math.Max(0, mapData.Layers.Count - 1);

        // Add non-player entities to MapData (for rendering)
        foreach (var ei in loadedMap.Entities)
        {
            var group = loadedMap.Groups.FirstOrDefault(g => g.Name == ei.DefinitionName);
            if (group != null && group.IsPlayer)
                continue;

            mapData.Entities.Add(new Entity
            {
                Id = ei.Id,
                GroupName = ei.DefinitionName,
                X = ei.X,
                Y = ei.Y,
                Properties = new Dictionary<string, string>(ei.Properties),
            });
        }

        _state.Map = mapData;
        _state.Groups = new List<TileGroup>(loadedMap.Groups);
        _state.RebuildGroupIndex();
    }
}
