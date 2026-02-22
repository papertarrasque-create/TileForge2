using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge2.Data;
using TileForge2.Editor;

namespace TileForge2.UI;

public class MapCanvas
{
    private static readonly Color BackgroundColor = new(25, 25, 25);
    private static readonly Color GridColor = new(255, 255, 255, 30);
    private static readonly Color GridBorderColor = new(255, 255, 255, 60);

    public Camera Camera { get; } = new();

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

        // Left-click painting
        if (mouse.LeftButton == ButtonState.Pressed && inBounds && !_isPanning)
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

        // Grid toggle
        if (keyboard.IsKeyDown(Keys.G) && prevKeyboard.IsKeyUp(Keys.G))
            _showGrid = !_showGrid;
    }

    private bool _showGrid = true;

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
        if (_showGrid && !state.IsPlayMode)
        {
            // Map border
            var borderStart = Camera.WorldToScreen(Vector2.Zero);
            var mapRect = new Rectangle((int)borderStart.X, (int)borderStart.Y,
                                        mapW * cellW, mapH * cellH);
            renderer.DrawRectOutline(spriteBatch, mapRect, GridBorderColor, 1);

            // Grid lines
            for (int x = startCol; x <= endCol + 1; x++)
            {
                var screenPos = Camera.WorldToScreen(new Vector2(x * tileW, startRow * tileH));
                int lineHeight = (endRow - startRow + 1) * cellH;
                renderer.DrawRect(spriteBatch, new Rectangle((int)screenPos.X, (int)screenPos.Y, 1, lineHeight), GridColor);
            }
            for (int y = startRow; y <= endRow + 1; y++)
            {
                var screenPos = Camera.WorldToScreen(new Vector2(startCol * tileW, y * tileH));
                int lineWidth = (endCol - startCol + 1) * cellW;
                renderer.DrawRect(spriteBatch, new Rectangle((int)screenPos.X, (int)screenPos.Y, lineWidth, 1), GridColor);
            }
        }

        // Tool preview (editor mode only)
        if (!state.IsPlayMode && HoverX >= 0 && HoverY >= 0 && state.ActiveTool != null)
        {
            state.ActiveTool.DrawPreview(spriteBatch, HoverX, HoverY, state, Camera, renderer);
        }
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
                renderer.DrawRectOutline(spriteBatch, selRect, new Color(100, 200, 255, 200), 2);
                break;
            }
        }
    }
}
