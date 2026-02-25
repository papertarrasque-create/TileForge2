using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Editor.Tools;
using TileForge.Play;

namespace TileForge.UI;

public class MapCanvas
{
    private static readonly Color BackgroundColor = LayoutConstants.CanvasBackground;
    private static readonly Color GridColor = LayoutConstants.CanvasGridColor;
    private static readonly Color GridBorderColor = LayoutConstants.CanvasGridBorderColor;

    public Camera Camera { get; } = new();
    public Minimap Minimap { get; } = new();

    private bool _isPanning;
    private Point _panStart;
    private Vector2 _panOffsetStart;

    private bool _isPainting;
    private int _lastPaintX = -1;
    private int _lastPaintY = -1;

    // Hover position in grid coords (-1 if outside)
    public int HoverX { get; private set; } = -1;
    public int HoverY { get; private set; } = -1;

    public void Update(EditorState state, MouseState mouse, MouseState prevMouse,
                       KeyboardState keyboard, KeyboardState prevKeyboard,
                       Rectangle bounds)
    {
        if (state.IsPlayMode) return;
        if (state.Sheet == null || state.Map == null) return;

        int mx = mouse.X;
        int my = mouse.Y;
        bool inBounds = bounds.Contains(mx, my);

        // Update hover position
        if (inBounds)
        {
            var worldPos = Camera.ScreenToWorld(new Vector2(mx, my));
            int gx = (int)Math.Floor(worldPos.X / state.Sheet.TileWidth);
            int gy = (int)Math.Floor(worldPos.Y / state.Sheet.TileHeight);
            if (state.Map.InBounds(gx, gy))
            {
                HoverX = gx;
                HoverY = gy;
            }
            else
            {
                HoverX = -1;
                HoverY = -1;
            }
        }
        else
        {
            HoverX = -1;
            HoverY = -1;
        }

        // Zoom
        int scrollDelta = mouse.ScrollWheelValue - prevMouse.ScrollWheelValue;
        if (scrollDelta != 0 && inBounds)
        {
            Camera.AdjustZoom(scrollDelta > 0 ? 1 : -1, bounds.Width, bounds.Height);
        }

        // Middle-mouse pan
        if (mouse.MiddleButton == ButtonState.Pressed)
        {
            if (!_isPanning)
            {
                _isPanning = true;
                _panStart = new Point(mx, my);
                _panOffsetStart = Camera.Offset;
            }
            else
            {
                Camera.Offset = _panOffsetStart + new Vector2(mx - _panStart.X, my - _panStart.Y);
            }
        }
        else
        {
            _isPanning = false;
        }

        // Alt + left-click: quick-pick (eyedropper) without changing tool
        bool altDown = keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt);
        if (altDown && mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released
            && inBounds && HoverX >= 0 && HoverY >= 0 && state.Map != null)
        {
            // Check for entities first
            Entity hitEntity = null;
            for (int i = state.Map.Entities.Count - 1; i >= 0; i--)
            {
                var e = state.Map.Entities[i];
                if (e.X == HoverX && e.Y == HoverY)
                {
                    hitEntity = e;
                    break;
                }
            }

            if (hitEntity != null)
            {
                state.SelectedGroupName = hitEntity.GroupName;
                state.SelectedEntityId = hitEntity.Id;
            }
            else
            {
                var layer = state.ActiveLayer;
                if (layer != null)
                {
                    string groupName = layer.GetCell(HoverX, HoverY, state.Map.Width);
                    if (groupName != null)
                        state.SelectedGroupName = groupName;
                }
            }
        }

        // Minimap click intercept (before tool dispatch)
        if (mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released
            && inBounds && !altDown
            && Minimap.HandleClick(mx, my, state, Camera, bounds))
        {
            return; // Minimap consumed the click
        }

        // Left-click painting (skip when Alt is held — Alt-click is reserved for picking)
        if (mouse.LeftButton == ButtonState.Pressed && inBounds && !_isPanning && !altDown)
        {
            if (HoverX >= 0 && HoverY >= 0 && state.ActiveTool != null)
            {
                if (!_isPainting)
                {
                    _isPainting = true;
                    _lastPaintX = -1;
                    _lastPaintY = -1;
                    state.ActiveTool.OnPress(HoverX, HoverY, state);
                    _lastPaintX = HoverX;
                    _lastPaintY = HoverY;
                }
                else if (HoverX != _lastPaintX || HoverY != _lastPaintY)
                {
                    state.ActiveTool.OnDrag(HoverX, HoverY, state);
                    _lastPaintX = HoverX;
                    _lastPaintY = HoverY;
                }
            }
        }
        else if (_isPainting)
        {
            _isPainting = false;
            state.ActiveTool?.OnRelease(state);
        }

        // Grid toggle (cycles Normal → Fine → Off)
        if (keyboard.IsKeyDown(Keys.G) && prevKeyboard.IsKeyUp(Keys.G))
            state.Grid.CycleMode();
    }

    /// <summary>
    /// InputEvent-aware update. Consumes canvas clicks so they don't
    /// propagate back to toolbar or panels.
    /// </summary>
    public void Update(EditorState state, InputEvent input,
                       KeyboardState keyboard, KeyboardState prevKeyboard,
                       Rectangle bounds)
    {
        Update(state, input.Mouse, input.PrevMouse, keyboard, prevKeyboard, bounds);

        // Consume clicks within canvas bounds for cross-component consumption
        input.TryConsumeClick(bounds);
    }

    public void Draw(SpriteBatch spriteBatch, EditorState state, Renderer renderer, Rectangle bounds)
    {
        // Background
        renderer.DrawRect(spriteBatch, bounds, BackgroundColor);

        if (state.Sheet == null || state.Map == null) return;

        int zoom = Camera.Zoom;
        int tileW = state.Sheet.TileWidth;
        int tileH = state.Sheet.TileHeight;
        int cellW = tileW * zoom;
        int cellH = tileH * zoom;

        int mapW = state.Map.Width;
        int mapH = state.Map.Height;

        // Determine visible cell range for culling
        var topLeft = Camera.ScreenToWorld(new Vector2(bounds.X, bounds.Y));
        var bottomRight = Camera.ScreenToWorld(new Vector2(bounds.Right, bounds.Bottom));
        int startCol = Math.Max(0, (int)Math.Floor(topLeft.X / tileW));
        int startRow = Math.Max(0, (int)Math.Floor(topLeft.Y / tileH));
        int endCol = Math.Min(mapW - 1, (int)Math.Floor(bottomRight.X / tileW));
        int endRow = Math.Min(mapH - 1, (int)Math.Floor(bottomRight.Y / tileH));

        // Draw layers bottom to top, with entities inserted at EntityRenderOrder
        bool entitiesDrawn = false;
        for (int layerIdx = 0; layerIdx < state.Map.Layers.Count; layerIdx++)
        {
            var layer = state.Map.Layers[layerIdx];
            if (layer.Visible)
            {
                for (int y = startRow; y <= endRow; y++)
                {
                    for (int x = startCol; x <= endCol; x++)
                    {
                        string groupName = layer.GetCell(x, y, mapW);
                        if (groupName == null) continue;

                        if (!state.GroupsByName.TryGetValue(groupName, out var group)) continue;
                        if (group.Sprites.Count == 0) continue;

                        // Position-seeded variation
                        int spriteIdx = group.Sprites.Count == 1
                            ? 0
                            : ((x * 31 + y * 37) % group.Sprites.Count + group.Sprites.Count) % group.Sprites.Count;

                        var sprite = group.Sprites[spriteIdx];
                        var srcRect = state.Sheet.GetTileRect(sprite.Col, sprite.Row);
                        var screenPos = Camera.WorldToScreen(new Vector2(x * tileW, y * tileH));
                        var destRect = new Rectangle((int)screenPos.X, (int)screenPos.Y, cellW, cellH);

                        spriteBatch.Draw(state.Sheet.Texture, destRect, srcRect, Color.White);
                    }
                }
            }

            // Draw entities after the designated layer
            if (!entitiesDrawn && layerIdx == state.Map.EntityRenderOrder)
            {
                DrawEntities(spriteBatch, state, renderer, tileW, tileH, cellW, cellH,
                             startCol, startRow, endCol, endRow);
                entitiesDrawn = true;
            }
        }

        // Fallback: if EntityRenderOrder >= layer count, draw entities after all layers
        if (!entitiesDrawn)
        {
            DrawEntities(spriteBatch, state, renderer, tileW, tileH, cellW, cellH,
                         startCol, startRow, endCol, endRow);
        }

        // Grid overlay (editor mode only)
        if (state.Grid.Mode != Editor.GridMode.Off && !state.IsPlayMode)
        {
            // Map border
            var borderStart = Camera.WorldToScreen(Vector2.Zero);
            var mapRect = new Rectangle((int)borderStart.X, (int)borderStart.Y,
                                        mapW * cellW, mapH * cellH);
            renderer.DrawRectOutline(spriteBatch, mapRect, state.Grid.BorderColor, 1);

            // Full-tile grid lines
            for (int x = startCol; x <= endCol + 1; x++)
            {
                var screenPos = Camera.WorldToScreen(new Vector2(x * tileW, startRow * tileH));
                int lineHeight = (endRow - startRow + 1) * cellH;
                renderer.DrawRect(spriteBatch, new Rectangle((int)screenPos.X, (int)screenPos.Y, 1, lineHeight), state.Grid.LineColor);
            }
            for (int y = startRow; y <= endRow + 1; y++)
            {
                var screenPos = Camera.WorldToScreen(new Vector2(startCol * tileW, y * tileH));
                int lineWidth = (endCol - startCol + 1) * cellW;
                renderer.DrawRect(spriteBatch, new Rectangle((int)screenPos.X, (int)screenPos.Y, lineWidth, 1), state.Grid.LineColor);
            }

            // Half-tile subdivision lines (Fine mode only)
            if (state.Grid.Mode == Editor.GridMode.Fine)
            {
                int halfW = cellW / 2;
                int halfH = cellH / 2;
                if (halfW >= 4 && halfH >= 4) // Only draw when cells are large enough
                {
                    for (int x = startCol; x <= endCol; x++)
                    {
                        var screenPos = Camera.WorldToScreen(new Vector2(x * tileW + tileW / 2f, startRow * tileH));
                        int lineHeight = (endRow - startRow + 1) * cellH;
                        renderer.DrawRect(spriteBatch, new Rectangle((int)screenPos.X, (int)screenPos.Y, 1, lineHeight), state.Grid.SubdivisionColor);
                    }
                    for (int y = startRow; y <= endRow; y++)
                    {
                        var screenPos = Camera.WorldToScreen(new Vector2(startCol * tileW, y * tileH + tileH / 2f));
                        int lineWidth = (endCol - startCol + 1) * cellW;
                        renderer.DrawRect(spriteBatch, new Rectangle((int)screenPos.X, (int)screenPos.Y, lineWidth, 1), state.Grid.SubdivisionColor);
                    }
                }
            }
        }

        // Selection rectangle overlay (editor mode only)
        if (!state.IsPlayMode && state.TileSelection.HasValue)
        {
            SelectionTool.DrawSelectionOutline(spriteBatch, state, Camera, renderer);
        }

        // Tool preview (editor mode only)
        if (!state.IsPlayMode && HoverX >= 0 && HoverY >= 0 && state.ActiveTool != null)
        {
            state.ActiveTool.DrawPreview(spriteBatch, HoverX, HoverY, state, Camera, renderer);
        }

        // Minimap overlay
        Minimap.Draw(spriteBatch, state, renderer, Camera, bounds);
    }

    public void CenterOnMap(EditorState state, Rectangle bounds)
    {
        if (state.Sheet == null || state.Map == null) return;

        int mapPixelW = state.Map.Width * state.Sheet.TileWidth;
        int mapPixelH = state.Map.Height * state.Sheet.TileHeight;
        Camera.CenterOn(mapPixelW, mapPixelH, bounds.Width, bounds.Height);
    }

    private void DrawEntities(SpriteBatch spriteBatch, EditorState state, Renderer renderer,
                              int tileW, int tileH, int cellW, int cellH,
                              int startCol, int startRow, int endCol, int endRow)
    {
        foreach (var entity in state.Map.Entities)
        {
            if (!state.GroupsByName.TryGetValue(entity.GroupName, out var group)) continue;
            if (group.Sprites.Count == 0) continue;

            // In play mode, render player entity at lerp position
            float drawX, drawY;
            if (state.IsPlayMode && state.PlayState != null && entity == state.PlayState.PlayerEntity)
            {
                drawX = state.PlayState.RenderPos.X;
                drawY = state.PlayState.RenderPos.Y;
            }
            else
            {
                drawX = entity.X;
                drawY = entity.Y;

                // Cull non-player entities outside visible range
                if (entity.X < startCol || entity.X > endCol || entity.Y < startRow || entity.Y > endRow)
                    continue;
            }

            var sprite = group.Sprites[0];
            var srcRect = state.Sheet.GetTileRect(sprite.Col, sprite.Row);
            var screenPos = Camera.WorldToScreen(new Vector2(drawX * tileW, drawY * tileH));
            var destRect = new Rectangle((int)screenPos.X, (int)screenPos.Y, cellW, cellH);

            spriteBatch.Draw(state.Sheet.Texture, destRect, srcRect, Color.White);

            // Per-sprite damage flash overlays (play mode only)
            if (state.IsPlayMode && state.PlayState != null)
            {
                if (entity == state.PlayState.PlayerEntity && state.PlayState.PlayerFlashTimer > 0)
                {
                    float intensity = state.PlayState.PlayerFlashTimer / PlayState.FlashDuration;
                    int alpha = (int)(intensity * 150);
                    renderer.DrawRect(spriteBatch, destRect, new Color(255, 0, 0, alpha));
                }
                else if (entity.Id == state.PlayState.FlashedEntityId && state.PlayState.EntityFlashTimer > 0)
                {
                    float intensity = state.PlayState.EntityFlashTimer / PlayState.FlashDuration;
                    int alpha = (int)(intensity * 200);
                    renderer.DrawRect(spriteBatch, destRect, new Color(255, 255, 255, alpha));
                }
            }
        }

        // Selection highlight (editor mode only)
        if (!state.IsPlayMode && state.SelectedEntityId != null)
        {
            foreach (var entity in state.Map.Entities)
            {
                if (entity.Id != state.SelectedEntityId) continue;
                if (entity.X < startCol || entity.X > endCol || entity.Y < startRow || entity.Y > endRow)
                    break;

                var selPos = Camera.WorldToScreen(new Vector2(entity.X * tileW, entity.Y * tileH));
                var selRect = new Rectangle((int)selPos.X, (int)selPos.Y, cellW, cellH);
                renderer.DrawRectOutline(spriteBatch, selRect, LayoutConstants.CanvasEntitySelectionColor, LayoutConstants.CanvasEntitySelectionThickness);
                break;
            }
        }
    }
}
