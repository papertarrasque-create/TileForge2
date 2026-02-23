using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TileForge.Data;
using TileForge.Editor;
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
        return true;
    }

    public void Exit()
    {
        _canvas.Camera.Offset = _savedCameraOffset;
        _canvas.Camera.ZoomIndex = _savedZoomIndex;

        _state.IsPlayMode = false;
        _state.PlayState = null;
    }

    public void Update(GameTime gameTime, KeyboardState keyboard, KeyboardState prevKeyboard)
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

            if (KeyPressed(keyboard, prevKeyboard, Keys.Up)) dy = -1;
            else if (KeyPressed(keyboard, prevKeyboard, Keys.Down)) dy = 1;
            else if (KeyPressed(keyboard, prevKeyboard, Keys.Left)) dx = -1;
            else if (KeyPressed(keyboard, prevKeyboard, Keys.Right)) dx = 1;

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
                    // Blocked -- check for bump interaction with entity
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
        var canvasBounds = _getCanvasBounds();

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

    private static bool KeyPressed(KeyboardState current, KeyboardState prev, Keys key)
        => current.IsKeyDown(key) && prev.IsKeyUp(key);
}
