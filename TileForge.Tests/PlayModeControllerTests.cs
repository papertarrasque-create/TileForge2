using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Game;
using TileForge.Play;
using TileForge.Tests.Helpers;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests;

/// <summary>
/// Tests for PlayModeController.
///
/// With the ISpriteSheet interface extracted in Phase R4, we can now inject a
/// MockSpriteSheet into EditorState.Sheet. This unlocks testing of:
///   - Enter() success path (sets PlayState, IsPlayMode, RenderPos)
///   - Update() movement via KeyboardState + GameTime (both constructable)
///   - CanMoveTo collision logic (solid tiles, solid entities, map boundaries)
///   - Entity interaction (status messages on bump/walk-into)
///   - Exit() restores camera state after a real Enter()
///
/// MapCanvas can be constructed without MonoGame runtime (it just creates a Camera).
/// KeyboardState is constructable via new KeyboardState(params Keys[]).
/// GameTime is constructable via new GameTime(total, elapsed).
/// </summary>
public class PlayModeControllerTests
{
    private static readonly Func<Rectangle> DefaultCanvasBounds = () => new Rectangle(0, 0, 800, 600);

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Creates an EditorState with a 10x10 map, a mock 16x16 spritesheet,
    /// a player entity group, and a player entity at the given position.
    /// Optionally adds extra groups and entities before building the group index.
    /// </summary>
    private static (EditorState state, MapCanvas canvas, PlayModeController controller)
        CreatePlaySetup(
            int playerX = 5, int playerY = 5,
            int mapWidth = 10, int mapHeight = 10,
            Action<EditorState> customize = null)
    {
        var state = new EditorState
        {
            Map = new MapData(mapWidth, mapHeight),
            Sheet = new MockSpriteSheet(16, 16),
        };

        // Add default player group
        state.AddGroup(new TileGroup
        {
            Name = "player",
            Type = GroupType.Entity,
            IsPlayer = true,
            Sprites = { new SpriteRef { Col = 0, Row = 0 } },
        });

        // Add player entity
        state.Map.Entities.Add(new Entity
        {
            Id = "player01",
            GroupName = "player",
            X = playerX,
            Y = playerY,
        });

        // Allow test-specific customization before building index
        customize?.Invoke(state);

        state.RebuildGroupIndex();

        var canvas = new MapCanvas();
        var controller = new PlayModeController(state, canvas, DefaultCanvasBounds);
        return (state, canvas, controller);
    }

    /// <summary>
    /// Simulates a single key-press Update cycle (key down this frame).
    /// GameInputManager tracks its own previous state internally.
    /// </summary>
    private static void SimulateKeyPress(PlayModeController controller, Keys key)
    {
        var current = new KeyboardState(key);
        var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.01));
        controller.Update(gameTime, current);
    }

    /// <summary>
    /// Starts a move and then completes it by calling Update with enough elapsed time.
    /// After this returns, IsMoving will be false and the player entity position will be updated.
    /// Includes a release frame so the key registers as JustPressed even if the same key
    /// was down from a previous SimulateKeyPress call.
    /// </summary>
    private static void SimulateFullMove(PlayModeController controller, Keys key)
    {
        // Release frame: ensure key is up so next press registers as JustPressed
        var releaseTime = new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.001));
        controller.Update(releaseTime, new KeyboardState());

        // Frame 1: key press initiates the move
        var current = new KeyboardState(key);
        var startTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.01));
        controller.Update(startTime, current);

        // Frame 2: enough time passes to complete the move (elapsed > MoveDuration)
        var finishTime = new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(PlayState.MoveDuration + 0.01f));
        var noKeys = new KeyboardState();
        controller.Update(finishTime, noKeys);
    }

    // =========================================================================
    // Enter() failure paths
    // =========================================================================

    [Fact]
    public void Enter_NullMap_ReturnsFalse()
    {
        var state = new EditorState { Map = null };
        var canvas = new MapCanvas();
        var controller = new PlayModeController(state, canvas, DefaultCanvasBounds);

        bool result = controller.Enter();

        Assert.False(result);
    }

    [Fact]
    public void Enter_NullSheet_ReturnsFalse()
    {
        var state = new EditorState
        {
            Map = new MapData(10, 10),
            Sheet = null,
        };
        var canvas = new MapCanvas();
        var controller = new PlayModeController(state, canvas, DefaultCanvasBounds);

        bool result = controller.Enter();

        Assert.False(result);
    }

    [Fact]
    public void Enter_NullMap_DoesNotSetPlayMode()
    {
        var state = new EditorState { Map = null };
        var canvas = new MapCanvas();
        var controller = new PlayModeController(state, canvas, DefaultCanvasBounds);

        controller.Enter();

        Assert.False(state.IsPlayMode);
        Assert.Null(state.PlayState);
    }

    [Fact]
    public void Enter_NoPlayerEntity_ReturnsFalse()
    {
        // Map and Sheet are non-null, but no entity has a group with IsPlayer = true
        var state = new EditorState
        {
            Map = new MapData(10, 10),
            Sheet = new MockSpriteSheet(16, 16),
        };
        state.AddGroup(new TileGroup
        {
            Name = "grass",
            Type = GroupType.Tile,
            IsPlayer = false,
            Sprites = { new SpriteRef { Col = 0, Row = 0 } },
        });
        state.Map.Entities.Add(new Entity { GroupName = "grass", X = 1, Y = 1 });
        state.RebuildGroupIndex();

        var canvas = new MapCanvas();
        var controller = new PlayModeController(state, canvas, DefaultCanvasBounds);

        bool result = controller.Enter();

        Assert.False(result);
        Assert.False(state.IsPlayMode);
        Assert.Null(state.PlayState);
    }

    [Fact]
    public void Enter_NoEntitiesAtAll_ReturnsFalse()
    {
        var state = new EditorState
        {
            Map = new MapData(10, 10),
            Sheet = new MockSpriteSheet(16, 16),
        };
        state.RebuildGroupIndex();

        var canvas = new MapCanvas();
        var controller = new PlayModeController(state, canvas, DefaultCanvasBounds);

        bool result = controller.Enter();

        Assert.False(result);
    }

    [Fact]
    public void Enter_PlayerGroupExistsButNoEntityOfThatGroup_ReturnsFalse()
    {
        var state = new EditorState
        {
            Map = new MapData(10, 10),
            Sheet = new MockSpriteSheet(16, 16),
        };
        state.AddGroup(new TileGroup
        {
            Name = "player",
            Type = GroupType.Entity,
            IsPlayer = true,
            Sprites = { new SpriteRef { Col = 0, Row = 0 } },
        });
        // No entity placed that references the "player" group
        state.RebuildGroupIndex();

        var canvas = new MapCanvas();
        var controller = new PlayModeController(state, canvas, DefaultCanvasBounds);

        bool result = controller.Enter();

        Assert.False(result);
    }

    // =========================================================================
    // Enter() success path
    // =========================================================================

    [Fact]
    public void Enter_WithValidSetup_ReturnsTrue()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 3, playerY: 4);

        bool result = controller.Enter();

        Assert.True(result);
    }

    [Fact]
    public void Enter_SetsIsPlayModeTrue()
    {
        var (state, canvas, controller) = CreatePlaySetup();

        controller.Enter();

        Assert.True(state.IsPlayMode);
    }

    [Fact]
    public void Enter_CreatesPlayStateWithCorrectPlayerEntity()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 3, playerY: 7);

        controller.Enter();

        Assert.NotNull(state.PlayState);
        Assert.NotNull(state.PlayState.PlayerEntity);
        Assert.Equal("player01", state.PlayState.PlayerEntity.Id);
        Assert.Equal("player", state.PlayState.PlayerEntity.GroupName);
        Assert.Equal(3, state.PlayState.PlayerEntity.X);
        Assert.Equal(7, state.PlayState.PlayerEntity.Y);
    }

    [Fact]
    public void Enter_SetsRenderPosToPlayerEntityPosition()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 2, playerY: 6);

        controller.Enter();

        Assert.Equal(new Vector2(2, 6), state.PlayState.RenderPos);
    }

    [Fact]
    public void Enter_SelectsFirstEntityWhoseGroupIsPlayer()
    {
        var state = new EditorState
        {
            Map = new MapData(10, 10),
            Sheet = new MockSpriteSheet(16, 16),
        };
        state.AddGroup(new TileGroup
        {
            Name = "npc",
            Type = GroupType.Entity,
            IsPlayer = false,
            Sprites = { new SpriteRef { Col = 1, Row = 1 } },
        });
        state.AddGroup(new TileGroup
        {
            Name = "hero",
            Type = GroupType.Entity,
            IsPlayer = true,
            Sprites = { new SpriteRef { Col = 2, Row = 2 } },
        });
        state.Map.Entities.Add(new Entity { Id = "npc01", GroupName = "npc", X = 1, Y = 1 });
        state.Map.Entities.Add(new Entity { Id = "hero01", GroupName = "hero", X = 4, Y = 4 });
        state.RebuildGroupIndex();

        var canvas = new MapCanvas();
        var controller = new PlayModeController(state, canvas, DefaultCanvasBounds);

        controller.Enter();

        Assert.Equal("hero01", state.PlayState.PlayerEntity.Id);
    }

    [Fact]
    public void Enter_CentersCameraOnPlayer()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5);

        controller.Enter();

        // CenterCameraOnPlayer sets the camera offset based on player world position.
        // Player world center: (5 + 0.5) * 16 = 88, (5 + 0.5) * 16 = 88
        // Screen center: 400, 300  (800/2, 600/2)
        // Zoom: Camera default is index 1 → zoom = 2
        // Expected offset: (400 - 88*2, 300 - 88*2) = (224, 124)
        Assert.Equal(new Vector2(224, 124), canvas.Camera.Offset);
    }

    // =========================================================================
    // Exit()
    // =========================================================================

    [Fact]
    public void Exit_ClearsIsPlayMode()
    {
        var (state, canvas, controller) = CreatePlaySetup();
        controller.Enter();

        controller.Exit();

        Assert.False(state.IsPlayMode);
    }

    [Fact]
    public void Exit_ClearsPlayState()
    {
        var (state, canvas, controller) = CreatePlaySetup();
        controller.Enter();

        controller.Exit();

        Assert.Null(state.PlayState);
    }

    [Fact]
    public void Exit_RestoresCameraOffset()
    {
        var (state, canvas, controller) = CreatePlaySetup();

        // Set editor camera state before entering play mode
        canvas.Camera.Offset = new Vector2(100, 200);
        canvas.Camera.ZoomIndex = 3;

        controller.Enter();

        // Verify camera was changed by Enter() (CenterCameraOnPlayer)
        Assert.NotEqual(new Vector2(100, 200), canvas.Camera.Offset);

        controller.Exit();

        // Exit should restore the saved editor camera state
        Assert.Equal(new Vector2(100, 200), canvas.Camera.Offset);
        Assert.Equal(3, canvas.Camera.ZoomIndex);
    }

    [Fact]
    public void Exit_AfterEnterAndMovement_FullCleanup()
    {
        var (state, canvas, controller) = CreatePlaySetup();
        controller.Enter();

        // Simulate some movement
        SimulateFullMove(controller, Keys.Right);

        controller.Exit();

        Assert.False(state.IsPlayMode);
        Assert.Null(state.PlayState);
    }

    // =========================================================================
    // Constructor
    // =========================================================================

    [Fact]
    public void Constructor_DoesNotModifyState()
    {
        var state = new EditorState();
        var canvas = new MapCanvas();

        var controller = new PlayModeController(state, canvas, DefaultCanvasBounds);

        Assert.False(state.IsPlayMode);
        Assert.Null(state.PlayState);
        Assert.Null(state.Map);
    }

    // =========================================================================
    // Update() with null PlayState (no-op)
    // =========================================================================

    [Fact]
    public void Update_NullPlayState_DoesNotThrow()
    {
        var state = new EditorState();
        var canvas = new MapCanvas();
        var controller = new PlayModeController(state, canvas, DefaultCanvasBounds);
        var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(16));
        var keyboard = new KeyboardState();

        var exception = Record.Exception(() => controller.Update(gameTime, keyboard));

        Assert.Null(exception);
    }

    // =========================================================================
    // Movement: basic directions
    // =========================================================================

    [Fact]
    public void Update_RightArrow_InitiatesMove()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5);
        controller.Enter();

        SimulateKeyPress(controller, Keys.Right);

        Assert.True(state.PlayState.IsMoving);
        Assert.Equal(new Vector2(5, 5), state.PlayState.MoveFrom);
        Assert.Equal(new Vector2(6, 5), state.PlayState.MoveTo);
    }

    [Fact]
    public void Update_LeftArrow_InitiatesMove()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5);
        controller.Enter();

        SimulateKeyPress(controller, Keys.Left);

        Assert.True(state.PlayState.IsMoving);
        Assert.Equal(new Vector2(4, 5), state.PlayState.MoveTo);
    }

    [Fact]
    public void Update_UpArrow_InitiatesMove()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5);
        controller.Enter();

        SimulateKeyPress(controller, Keys.Up);

        Assert.True(state.PlayState.IsMoving);
        Assert.Equal(new Vector2(5, 4), state.PlayState.MoveTo);
    }

    [Fact]
    public void Update_DownArrow_InitiatesMove()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5);
        controller.Enter();

        SimulateKeyPress(controller, Keys.Down);

        Assert.True(state.PlayState.IsMoving);
        Assert.Equal(new Vector2(5, 6), state.PlayState.MoveTo);
    }

    [Fact]
    public void Update_CompletedMove_UpdatesEntityPosition()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5);
        controller.Enter();

        SimulateFullMove(controller, Keys.Right);

        Assert.Equal(6, state.PlayState.PlayerEntity.X);
        Assert.Equal(5, state.PlayState.PlayerEntity.Y);
        Assert.False(state.PlayState.IsMoving);
    }

    [Fact]
    public void Update_CompletedMove_UpdatesRenderPos()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5);
        controller.Enter();

        SimulateFullMove(controller, Keys.Down);

        Assert.Equal(new Vector2(5, 6), state.PlayState.RenderPos);
    }

    [Fact]
    public void Update_MultipleMovesSequentially()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5);
        controller.Enter();

        SimulateFullMove(controller, Keys.Right);
        SimulateFullMove(controller, Keys.Down);

        Assert.Equal(6, state.PlayState.PlayerEntity.X);
        Assert.Equal(6, state.PlayState.PlayerEntity.Y);
    }

    [Fact]
    public void Update_NoKeyPress_DoesNotMove()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5);
        controller.Enter();

        // Update with no keys pressed
        var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.1));
        controller.Update(gameTime, new KeyboardState());

        Assert.False(state.PlayState.IsMoving);
        Assert.Equal(5, state.PlayState.PlayerEntity.X);
        Assert.Equal(5, state.PlayState.PlayerEntity.Y);
    }

    [Fact]
    public void Update_KeyHeldDown_DoesNotRepeatMove()
    {
        // GameInputManager tracks its own previous state. A key held across
        // frames should only trigger JustPressed on the first frame.
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5);
        controller.Enter();

        var rightDown = new KeyboardState(Keys.Right);

        // Frame 1: key pressed — initiates move
        var startTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.01));
        controller.Update(startTime, rightDown);
        Assert.True(state.PlayState.IsMoving);

        // Complete the move
        var finishTime = new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(PlayState.MoveDuration + 0.01f));
        controller.Update(finishTime, rightDown);
        Assert.False(state.PlayState.IsMoving);
        Assert.Equal(6, state.PlayState.PlayerEntity.X);

        // Frame 3: key still held — should NOT start a new move
        var holdTime = new GameTime(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(0.01));
        controller.Update(holdTime, rightDown);
        Assert.False(state.PlayState.IsMoving);
        Assert.Equal(6, state.PlayState.PlayerEntity.X);
    }

    // =========================================================================
    // Movement: blocked by map boundary
    // =========================================================================

    [Fact]
    public void Update_BlockedByLeftBoundary()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 0, playerY: 5);
        controller.Enter();

        SimulateKeyPress(controller, Keys.Left);

        Assert.False(state.PlayState.IsMoving);
        Assert.Equal(0, state.PlayState.PlayerEntity.X);
    }

    [Fact]
    public void Update_BlockedByTopBoundary()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 0);
        controller.Enter();

        SimulateKeyPress(controller, Keys.Up);

        Assert.False(state.PlayState.IsMoving);
        Assert.Equal(0, state.PlayState.PlayerEntity.Y);
    }

    [Fact]
    public void Update_BlockedByRightBoundary()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 9, playerY: 5, mapWidth: 10, mapHeight: 10);
        controller.Enter();

        SimulateKeyPress(controller, Keys.Right);

        Assert.False(state.PlayState.IsMoving);
        Assert.Equal(9, state.PlayState.PlayerEntity.X);
    }

    [Fact]
    public void Update_BlockedByBottomBoundary()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 9, mapWidth: 10, mapHeight: 10);
        controller.Enter();

        SimulateKeyPress(controller, Keys.Down);

        Assert.False(state.PlayState.IsMoving);
        Assert.Equal(9, state.PlayState.PlayerEntity.Y);
    }

    // =========================================================================
    // Movement: blocked by solid tile
    // =========================================================================

    [Fact]
    public void Update_BlockedBySolidTile()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "wall",
                Type = GroupType.Tile,
                IsSolid = true,
                Sprites = { new SpriteRef { Col = 1, Row = 1 } },
            });
            // Place a wall tile at (6, 5) on the Ground layer
            s.Map.GetLayer("Ground").SetCell(6, 5, s.Map.Width, "wall");
        });
        controller.Enter();

        SimulateKeyPress(controller, Keys.Right);

        Assert.False(state.PlayState.IsMoving);
        Assert.Equal(5, state.PlayState.PlayerEntity.X);
    }

    [Fact]
    public void Update_NotBlockedByNonSolidTile()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "grass",
                Type = GroupType.Tile,
                IsSolid = false,
                Sprites = { new SpriteRef { Col = 2, Row = 2 } },
            });
            // Place a grass tile at (6, 5) — non-solid, player can walk on it
            s.Map.GetLayer("Ground").SetCell(6, 5, s.Map.Width, "grass");
        });
        controller.Enter();

        SimulateKeyPress(controller, Keys.Right);

        Assert.True(state.PlayState.IsMoving);
        Assert.Equal(new Vector2(6, 5), state.PlayState.MoveTo);
    }

    [Fact]
    public void Update_BlockedBySolidTileOnAnyLayer()
    {
        // CanMoveTo checks ALL layers for solid groups
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "wall",
                Type = GroupType.Tile,
                IsSolid = true,
                Sprites = { new SpriteRef { Col = 1, Row = 1 } },
            });
            // Place the wall on the Objects layer (not Ground)
            s.Map.GetLayer("Objects").SetCell(6, 5, s.Map.Width, "wall");
        });
        controller.Enter();

        SimulateKeyPress(controller, Keys.Right);

        Assert.False(state.PlayState.IsMoving);
    }

    [Fact]
    public void Update_PlayerMovesToEmptyCell()
    {
        // No tiles or entities at the target cell — player moves freely
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5);
        controller.Enter();

        SimulateKeyPress(controller, Keys.Right);

        Assert.True(state.PlayState.IsMoving);
    }

    // =========================================================================
    // Movement: blocked by solid entity
    // =========================================================================

    [Fact]
    public void Update_BlockedBySolidEntity()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "rock",
                Type = GroupType.Entity,
                IsSolid = true,
                Sprites = { new SpriteRef { Col = 3, Row = 3 } },
            });
            s.Map.Entities.Add(new Entity
            {
                Id = "rock01",
                GroupName = "rock",
                X = 6,
                Y = 5,
            });
        });
        controller.Enter();

        SimulateKeyPress(controller, Keys.Right);

        Assert.False(state.PlayState.IsMoving);
        Assert.Equal(5, state.PlayState.PlayerEntity.X);
    }

    [Fact]
    public void Update_NotBlockedByNonSolidEntity()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "coin",
                Type = GroupType.Entity,
                IsSolid = false,
                Sprites = { new SpriteRef { Col = 4, Row = 4 } },
            });
            s.Map.Entities.Add(new Entity
            {
                Id = "coin01",
                GroupName = "coin",
                X = 6,
                Y = 5,
            });
        });
        controller.Enter();

        SimulateKeyPress(controller, Keys.Right);

        Assert.True(state.PlayState.IsMoving);
        Assert.Equal(new Vector2(6, 5), state.PlayState.MoveTo);
    }

    // =========================================================================
    // Entity interaction
    // =========================================================================

    [Fact]
    public void Update_WalkIntoNonSolidEntity_SetsFloatingMessage()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "chest",
                Type = GroupType.Entity,
                IsSolid = false,
                Sprites = { new SpriteRef { Col = 5, Row = 5 } },
            });
            s.Map.Entities.Add(new Entity
            {
                Id = "chest01",
                GroupName = "chest",
                X = 6,
                Y = 5,
            });
        });
        controller.Enter();

        // Walk onto the chest cell — player moves through and triggers interaction
        SimulateFullMove(controller, Keys.Right);

        Assert.Contains(state.PlayState.FloatingMessages, m => m.Text == "Interacted with chest");
    }

    [Fact]
    public void Update_BumpIntoSolidEntity_SetsFloatingMessage()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "door",
                Type = GroupType.Entity,
                IsSolid = true,
                Sprites = { new SpriteRef { Col = 6, Row = 6 } },
            });
            s.Map.Entities.Add(new Entity
            {
                Id = "door01",
                GroupName = "door",
                X = 6,
                Y = 5,
            });
        });
        controller.Enter();

        // Bump into the door — player is blocked but interaction still fires
        SimulateKeyPress(controller, Keys.Right);

        Assert.Contains(state.PlayState.FloatingMessages, m => m.Text == "Interacted with door");
    }

    [Fact]
    public void Update_FloatingMessageTimerCountsDown()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "chest",
                Type = GroupType.Entity,
                IsSolid = false,
                Sprites = { new SpriteRef { Col = 5, Row = 5 } },
            });
            s.Map.Entities.Add(new Entity
            {
                Id = "chest01",
                GroupName = "chest",
                X = 6,
                Y = 5,
            });
        });
        controller.Enter();
        SimulateFullMove(controller, Keys.Right);

        Assert.NotEmpty(state.PlayState.FloatingMessages);
        float initialTimer = state.PlayState.FloatingMessages[0].Timer;

        // Run an Update with some elapsed time (no key press)
        var gameTime = new GameTime(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(0.5));
        controller.Update(gameTime, new KeyboardState());

        Assert.True(state.PlayState.FloatingMessages[0].Timer < initialTimer);
    }

    [Fact]
    public void Update_FloatingMessageClearsAfterDuration()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "chest",
                Type = GroupType.Entity,
                IsSolid = false,
                Sprites = { new SpriteRef { Col = 5, Row = 5 } },
            });
            s.Map.Entities.Add(new Entity
            {
                Id = "chest01",
                GroupName = "chest",
                X = 6,
                Y = 5,
            });
        });
        controller.Enter();
        SimulateFullMove(controller, Keys.Right);

        Assert.NotEmpty(state.PlayState.FloatingMessages);

        // Tick past the full floating message duration
        var gameTime = new GameTime(TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(TileForge.Game.FloatingMessage.Duration + 0.1f));
        controller.Update(gameTime, new KeyboardState());

        Assert.Empty(state.PlayState.FloatingMessages);
    }

    [Fact]
    public void Update_BumpIntoEmptyInBoundsCell_NoFloatingMessage()
    {
        // Bumping into a boundary sets no message — CanMoveTo returns false but the
        // "bump interaction" code only runs when target is in bounds.
        // Here we bump into a wall tile (in bounds) with no entity — no interaction message.
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "wall",
                Type = GroupType.Tile,
                IsSolid = true,
                Sprites = { new SpriteRef { Col = 1, Row = 1 } },
            });
            s.Map.GetLayer("Ground").SetCell(6, 5, s.Map.Width, "wall");
        });
        controller.Enter();

        SimulateKeyPress(controller, Keys.Right);

        // Blocked by wall, target is in bounds but no entity there — no floating message
        Assert.Empty(state.PlayState.FloatingMessages);
    }

    // =========================================================================
    // Movement lerp
    // =========================================================================

    [Fact]
    public void Update_MidLerp_RenderPosIsBetweenFromAndTo()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5);
        controller.Enter();

        // Initiate move
        var current = new KeyboardState(Keys.Right);
        var startTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.01));
        controller.Update(startTime, current);

        Assert.True(state.PlayState.IsMoving);

        // Partial progress: half of MoveDuration
        var halfTime = new GameTime(TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(PlayState.MoveDuration * 0.5));
        controller.Update(halfTime, new KeyboardState());

        // RenderPos should be between MoveFrom (5,5) and MoveTo (6,5)
        Assert.True(state.PlayState.RenderPos.X > 5f);
        Assert.True(state.PlayState.RenderPos.X < 6f);
        Assert.Equal(5f, state.PlayState.RenderPos.Y);
    }

    [Fact]
    public void Update_CannotMoveWhileAlreadyMoving()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5);
        controller.Enter();

        // Start a move to the right
        var rightKey = new KeyboardState(Keys.Right);
        var startTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.01));
        controller.Update(startTime, rightKey);

        Assert.True(state.PlayState.IsMoving);
        Assert.Equal(new Vector2(6, 5), state.PlayState.MoveTo);

        // While still moving, press Down — should be ignored
        var downKey = new KeyboardState(Keys.Down);
        var partialTime = new GameTime(TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(PlayState.MoveDuration * 0.3));
        controller.Update(partialTime, downKey);

        // Still moving to the original target
        Assert.True(state.PlayState.IsMoving);
        Assert.Equal(new Vector2(6, 5), state.PlayState.MoveTo);
    }

    // =========================================================================
    // Edge cases
    // =========================================================================

    [Fact]
    public void Enter_TwiceWithoutExit_OverwritesPlayState()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5);
        controller.Enter();
        var firstPlayState = state.PlayState;

        // Enter again (without exiting)
        controller.Enter();

        // PlayState should be a new instance
        Assert.NotSame(firstPlayState, state.PlayState);
        Assert.True(state.IsPlayMode);
    }

    [Theory]
    [InlineData(16, 16)]
    [InlineData(32, 32)]
    [InlineData(24, 24)]
    [InlineData(8, 8)]
    public void Enter_DifferentTileSizes_CameraPositionScalesCorrectly(int tileW, int tileH)
    {
        var state = new EditorState
        {
            Map = new MapData(10, 10),
            Sheet = new MockSpriteSheet(tileW, tileH),
        };
        state.AddGroup(new TileGroup
        {
            Name = "player",
            Type = GroupType.Entity,
            IsPlayer = true,
            Sprites = { new SpriteRef { Col = 0, Row = 0 } },
        });
        state.Map.Entities.Add(new Entity
        {
            Id = "p1",
            GroupName = "player",
            X = 5,
            Y = 5,
        });
        state.RebuildGroupIndex();
        var canvas = new MapCanvas();
        var controller = new PlayModeController(state, canvas, DefaultCanvasBounds);

        controller.Enter();

        // Camera centering uses TileWidth and TileHeight from the sheet.
        // Player world center = (5 + 0.5) * tileW, (5 + 0.5) * tileH
        // Screen center = 400, 300
        // Zoom (default index 1) = 2
        float worldX = (5 + 0.5f) * tileW;
        float worldY = (5 + 0.5f) * tileH;
        float expectedOffsetX = 400f - worldX * 2;
        float expectedOffsetY = 300f - worldY * 2;

        Assert.Equal(expectedOffsetX, canvas.Camera.Offset.X);
        Assert.Equal(expectedOffsetY, canvas.Camera.Offset.Y);
    }

    [Fact]
    public void Update_PlayerMovesIntoCorner_CannotMoveInBlockedDirections()
    {
        // Player at (0,0) cannot move left or up
        var (state, canvas, controller) = CreatePlaySetup(playerX: 0, playerY: 0);
        controller.Enter();

        SimulateKeyPress(controller, Keys.Left);
        Assert.False(state.PlayState.IsMoving);

        SimulateKeyPress(controller, Keys.Up);
        Assert.False(state.PlayState.IsMoving);

        // But can move right and down
        SimulateKeyPress(controller, Keys.Right);
        Assert.True(state.PlayState.IsMoving);
    }

    // =========================================================================
    // G2: GameStateManager integration
    // =========================================================================

    [Fact]
    public void Enter_CreatesGameStateManager()
    {
        var (state, canvas, controller) = CreatePlaySetup();
        controller.Enter();

        Assert.NotNull(controller.GameStateManager);
        Assert.NotNull(controller.GameStateManager.State);
    }

    [Fact]
    public void Enter_GameStateManagerHasPlayerPosition()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 3, playerY: 7);
        controller.Enter();

        Assert.Equal(3, controller.GameStateManager.State.Player.X);
        Assert.Equal(7, controller.GameStateManager.State.Player.Y);
    }

    [Fact]
    public void Enter_GameStateManagerExcludesPlayerFromActiveEntities()
    {
        var (state, canvas, controller) = CreatePlaySetup(customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "npc",
                Type = GroupType.Entity,
                Sprites = { new SpriteRef { Col = 1, Row = 1 } },
            });
            s.Map.Entities.Add(new Entity { Id = "npc01", GroupName = "npc", X = 8, Y = 8 });
        });
        controller.Enter();

        Assert.Single(controller.GameStateManager.State.ActiveEntities);
        Assert.Equal("npc01", controller.GameStateManager.State.ActiveEntities[0].Id);
    }

    [Fact]
    public void Exit_ClearsGameStateManager()
    {
        var (state, canvas, controller) = CreatePlaySetup();
        controller.Enter();
        controller.Exit();

        Assert.Null(controller.GameStateManager);
    }

    // =========================================================================
    // G2: Hazard tile damage
    // =========================================================================

    [Fact]
    public void Update_HazardTile_DamagesPlayer()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "lava",
                Type = GroupType.Tile,
                IsHazardous = true,
                DamagePerTick = 10,
                DamageType = "fire",
                Sprites = { new SpriteRef { Col = 3, Row = 3 } },
            });
            s.Map.GetLayer("Ground").SetCell(6, 5, s.Map.Width, "lava");
        });
        controller.Enter();

        SimulateFullMove(controller, Keys.Right);

        // 10 instant fire damage + 1 burn effect damage on same step = 89
        Assert.Equal(89, controller.GameStateManager.State.Player.Health);
    }

    [Fact]
    public void Update_HazardTile_ShowsDamageMessage()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "lava",
                Type = GroupType.Tile,
                IsHazardous = true,
                DamagePerTick = 10,
                DamageType = "fire",
                Sprites = { new SpriteRef { Col = 3, Row = 3 } },
            });
            s.Map.GetLayer("Ground").SetCell(6, 5, s.Map.Width, "lava");
        });
        controller.Enter();

        SimulateFullMove(controller, Keys.Right);

        Assert.Contains(state.PlayState.FloatingMessages, m => m.Text == "Took 10 fire damage!");
    }

    [Fact]
    public void Update_NonHazardousTile_NoDamage()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "grass",
                Type = GroupType.Tile,
                IsHazardous = false,
                Sprites = { new SpriteRef { Col = 2, Row = 2 } },
            });
            s.Map.GetLayer("Ground").SetCell(6, 5, s.Map.Width, "grass");
        });
        controller.Enter();

        SimulateFullMove(controller, Keys.Right);

        Assert.Equal(100, controller.GameStateManager.State.Player.Health);
    }

    [Fact]
    public void Update_HazardKillsPlayer_ShowsDeathMessage()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "deathpit",
                Type = GroupType.Tile,
                IsHazardous = true,
                DamagePerTick = 200,
                DamageType = "spikes",
                Sprites = { new SpriteRef { Col = 3, Row = 3 } },
            });
            s.Map.GetLayer("Ground").SetCell(6, 5, s.Map.Width, "deathpit");
        });
        controller.Enter();

        SimulateFullMove(controller, Keys.Right);

        Assert.Equal(0, controller.GameStateManager.State.Player.Health);
        Assert.False(controller.GameStateManager.IsPlayerAlive());
        Assert.True(controller.ScreenManager.HasScreens);
    }

    [Fact]
    public void Update_DeadPlayer_CannotMove()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "deathpit",
                Type = GroupType.Tile,
                IsHazardous = true,
                DamagePerTick = 200,
                Sprites = { new SpriteRef { Col = 3, Row = 3 } },
            });
            s.Map.GetLayer("Ground").SetCell(6, 5, s.Map.Width, "deathpit");
        });
        controller.Enter();

        // Move onto lethal tile — player dies
        SimulateFullMove(controller, Keys.Right);
        Assert.False(controller.GameStateManager.IsPlayerAlive());

        // Try to move again — should not work
        int xBefore = state.PlayState.PlayerEntity.X;
        SimulateKeyPress(controller, Keys.Down);
        Assert.False(state.PlayState.IsMoving);
        Assert.Equal(xBefore, state.PlayState.PlayerEntity.X);
    }

    // =========================================================================
    // G2: Movement cost
    // =========================================================================

    [Fact]
    public void Update_HighMovementCost_IncreasesMoveDuration()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "swamp",
                Type = GroupType.Tile,
                MovementCost = 2.0f,
                Sprites = { new SpriteRef { Col = 4, Row = 4 } },
            });
            s.Map.GetLayer("Ground").SetCell(6, 5, s.Map.Width, "swamp");
        });
        controller.Enter();

        // Initiate move onto swamp tile
        SimulateKeyPress(controller, Keys.Right);

        // CurrentMoveDuration should be 0.15 * 2.0 = 0.30
        Assert.Equal(PlayState.MoveDuration * 2.0f, state.PlayState.CurrentMoveDuration, 3);
    }

    [Fact]
    public void Update_DefaultMovementCost_UsesBaseDuration()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5);
        controller.Enter();

        SimulateKeyPress(controller, Keys.Right);

        Assert.Equal(PlayState.MoveDuration, state.PlayState.CurrentMoveDuration, 3);
    }

    [Fact]
    public void Update_HighMovementCost_TakesLongerToComplete()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "swamp",
                Type = GroupType.Tile,
                MovementCost = 3.0f,
                Sprites = { new SpriteRef { Col = 4, Row = 4 } },
            });
            s.Map.GetLayer("Ground").SetCell(6, 5, s.Map.Width, "swamp");
        });
        controller.Enter();

        // Start move
        var current = new KeyboardState(Keys.Right);
        var startTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.01));
        controller.Update(startTime, current);
        Assert.True(state.PlayState.IsMoving);

        // After base MoveDuration (0.15s), move should NOT be complete (cost=3x → needs 0.45s)
        var midTime = new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(PlayState.MoveDuration));
        controller.Update(midTime, new KeyboardState());
        Assert.True(state.PlayState.IsMoving);

        // After total 0.45s+, move should be complete
        var endTime = new GameTime(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(PlayState.MoveDuration * 2.0f + 0.01f));
        controller.Update(endTime, new KeyboardState());
        Assert.False(state.PlayState.IsMoving);
    }

    // =========================================================================
    // G2: Entity interaction by EntityType
    // =========================================================================

    [Fact]
    public void Update_NPCInteraction_ShowsTalkedMessage()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "elder",
                Type = GroupType.Entity,
                IsSolid = true,
                EntityType = EntityType.NPC,
                Sprites = { new SpriteRef { Col = 5, Row = 5 } },
            });
            s.Map.Entities.Add(new Entity { Id = "elder01", GroupName = "elder", X = 6, Y = 5 });
        });
        controller.Enter();

        // Bump into the NPC (solid)
        SimulateKeyPress(controller, Keys.Right);

        Assert.Contains(state.PlayState.FloatingMessages, m => m.Text == "Talked to elder");
    }

    [Fact]
    public void Update_ItemInteraction_CollectsAndDeactivates()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "health_potion",
                Type = GroupType.Entity,
                IsSolid = false,
                EntityType = EntityType.Item,
                Sprites = { new SpriteRef { Col = 6, Row = 6 } },
            });
            s.Map.Entities.Add(new Entity { Id = "hp01", GroupName = "health_potion", X = 6, Y = 5 });
        });
        controller.Enter();

        SimulateFullMove(controller, Keys.Right);

        Assert.Contains(state.PlayState.FloatingMessages, m => m.Text == "Collected health_potion");
        Assert.True(controller.GameStateManager.HasItem("health_potion"));

        // Entity should be deactivated
        var instance = controller.GameStateManager.State.ActiveEntities[0];
        Assert.False(instance.IsActive);
    }

    [Fact]
    public void Update_CollectedItem_NoLongerBlocks()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "key",
                Type = GroupType.Entity,
                IsSolid = true,
                EntityType = EntityType.Item,
                Sprites = { new SpriteRef { Col = 6, Row = 6 } },
            });
            s.Map.Entities.Add(new Entity { Id = "key01", GroupName = "key", X = 6, Y = 5 });
        });
        controller.Enter();

        // Bump into the key (solid + item = collect on bump)
        SimulateKeyPress(controller, Keys.Right);
        Assert.Contains(state.PlayState.FloatingMessages, m => m.Text == "Collected key");

        // Now the deactivated entity should no longer block
        SimulateFullMove(controller, Keys.Right);
        Assert.Equal(6, state.PlayState.PlayerEntity.X);
    }

    [Fact]
    public void Update_TrapInteraction_DealsDamage()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "spike_trap",
                Type = GroupType.Entity,
                IsSolid = false,
                EntityType = EntityType.Trap,
                Sprites = { new SpriteRef { Col = 7, Row = 7 } },
            });
            s.Map.Entities.Add(new Entity
            {
                Id = "trap01",
                GroupName = "spike_trap",
                X = 6,
                Y = 5,
                Properties = new() { ["damage"] = "15" },
            });
        });
        controller.Enter();

        SimulateFullMove(controller, Keys.Right);

        Assert.Equal(85, controller.GameStateManager.State.Player.Health);
        Assert.Contains(state.PlayState.FloatingMessages, m => m.Text == "spike_trap dealt 15 damage!");
    }

    [Fact]
    public void Update_TriggerInteraction_ShowsTriggeredMessage()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "door",
                Type = GroupType.Entity,
                IsSolid = false,
                EntityType = EntityType.Trigger,
                Sprites = { new SpriteRef { Col = 8, Row = 8 } },
            });
            s.Map.Entities.Add(new Entity
            {
                Id = "door01",
                GroupName = "door",
                X = 6,
                Y = 5,
                Properties = new() { ["message"] = "A mysterious door" },
            });
        });
        controller.Enter();

        SimulateFullMove(controller, Keys.Right);

        Assert.Contains(state.PlayState.FloatingMessages, m => m.Text == "Triggered door");
    }

    [Fact]
    public void Update_InteractableInteraction_ShowsDefaultMessage()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "sign",
                Type = GroupType.Entity,
                IsSolid = true,
                EntityType = EntityType.Interactable,
                Sprites = { new SpriteRef { Col = 9, Row = 9 } },
            });
            s.Map.Entities.Add(new Entity { Id = "sign01", GroupName = "sign", X = 6, Y = 5 });
        });
        controller.Enter();

        // Bump into the sign (solid interactable)
        SimulateKeyPress(controller, Keys.Right);

        Assert.Contains(state.PlayState.FloatingMessages, m => m.Text == "Interacted with sign");
    }

    [Fact]
    public void Update_InactiveEntity_NotInteractedWith()
    {
        var (state, canvas, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "coin",
                Type = GroupType.Entity,
                IsSolid = false,
                EntityType = EntityType.Item,
                Sprites = { new SpriteRef { Col = 6, Row = 6 } },
            });
            s.Map.Entities.Add(new Entity { Id = "coin01", GroupName = "coin", X = 6, Y = 5 });
        });
        controller.Enter();

        // Collect the coin
        SimulateFullMove(controller, Keys.Right);
        Assert.Contains(state.PlayState.FloatingMessages, m => m.Text == "Collected coin");

        // Move away
        SimulateFullMove(controller, Keys.Right);

        // Clear floating messages so we can detect if a new one is added
        state.PlayState.FloatingMessages.Clear();

        // Move back to coin position — coin is inactive, should not trigger
        SimulateFullMove(controller, Keys.Left);

        Assert.Empty(state.PlayState.FloatingMessages);
    }
}
