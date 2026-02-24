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
using TileForge.Game;
using TileForge.Game.Screens;
using TileForge.Play;
using TileForge.UI;

namespace TileForge;

public class PlayModeController
{
    private readonly EditorState _state;
    private readonly MapCanvas _canvas;
    private readonly Func<Rectangle> _getCanvasBounds;

    private Vector2 _savedCameraOffset;
    private int _savedZoomIndex;
    private MapData _savedMap;
    private List<TileGroup> _savedGroups;
    private GameStateManager _gameStateManager;
    private GameInputManager _inputManager;
    private ScreenManager _screenManager;
    private SaveManager _saveManager;
    private QuestManager _questManager;
    private string _bindingsPath;

    public GameStateManager GameStateManager => _gameStateManager;
    public ScreenManager ScreenManager => _screenManager;

    /// <summary>
    /// Optional base directory for resolving relative map paths in transitions.
    /// If null, target_map paths are used as-is.
    /// </summary>
    public string MapBaseDirectory { get; set; }

    public PlayModeController(EditorState state, MapCanvas canvas, Func<Rectangle> getCanvasBounds)
    {
        _state = state;
        _canvas = canvas;
        _getCanvasBounds = getCanvasBounds;
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

        // Save editor state for restoration on exit
        _savedCameraOffset = _canvas.Camera.Offset;
        _savedZoomIndex = _canvas.Camera.ZoomIndex;
        _savedMap = _state.Map;
        _savedGroups = new List<TileGroup>(_state.Groups);

        // Initialize game state
        _gameStateManager = new GameStateManager();
        _gameStateManager.Initialize(_state.Map, _state.GroupsByName);

        // Initialize input, screen management, and save manager
        _inputManager = new GameInputManager();
        _bindingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".tileforge", "keybindings.json");
        _inputManager.LoadBindings(_bindingsPath);
        _screenManager = new ScreenManager();
        _saveManager = new SaveManager();
        _questManager = LoadQuests();

        // Create play state (rendering/lerp)
        _state.PlayState = new PlayState
        {
            PlayerEntity = playerEntity,
            RenderPos = new Vector2(playerEntity.X, playerEntity.Y),
        };
        _state.IsPlayMode = true;

        // Push the gameplay screen
        _screenManager.Push(new GameplayScreen(_state, _canvas, _gameStateManager, _saveManager, _inputManager, _bindingsPath, MapBaseDirectory, _questManager, _getCanvasBounds));

        return true;
    }

    public void Exit()
    {
        _screenManager?.Clear();

        _canvas.Camera.Offset = _savedCameraOffset;
        _canvas.Camera.ZoomIndex = _savedZoomIndex;

        // Restore editor state
        if (_savedMap != null)
        {
            _state.Map = _savedMap;
            _state.Groups = new List<TileGroup>(_savedGroups);
            _state.RebuildGroupIndex();
            _savedMap = null;
            _savedGroups = null;
        }

        _state.IsPlayMode = false;
        _state.PlayState = null;
        _gameStateManager = null;
        _inputManager = null;
        _screenManager = null;
        _saveManager = null;
        _questManager = null;
        _bindingsPath = null;
    }

    public void Update(GameTime gameTime, KeyboardState keyboard)
    {
        if (_state.PlayState == null) return;

        _inputManager.Update(keyboard);
        _screenManager.Update(gameTime, _inputManager);

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

    private void ExecuteMapTransition(MapTransitionRequest request)
    {
        // Resolve map path
        string mapPath = request.TargetMap;
        if (MapBaseDirectory != null && !Path.IsPathRooted(mapPath))
            mapPath = Path.Combine(MapBaseDirectory, mapPath);

        if (!File.Exists(mapPath))
        {
            _state.PlayState.StatusMessage = $"Map not found: {request.TargetMap}";
            _state.PlayState.StatusMessageTimer = PlayState.StatusMessageDuration;
            return;
        }

        // Load the target map
        string json = File.ReadAllText(mapPath);
        var loader = new MapLoader();
        var loadedMap = loader.Load(json, Path.GetFileNameWithoutExtension(mapPath));

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

        // Reset play state for new position
        _state.PlayState = new PlayState
        {
            PlayerEntity = playerEntity,
            RenderPos = new Vector2(request.TargetX, request.TargetY),
        };

        // Pop the old gameplay screen and push a new one (re-centers camera)
        _screenManager.Clear();
        _screenManager.Push(new GameplayScreen(_state, _canvas, _gameStateManager, _saveManager, _inputManager, _bindingsPath, MapBaseDirectory, _questManager, _getCanvasBounds));
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
