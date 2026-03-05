using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge.Data;
using TileForge.Editor;

namespace TileForge.UI;

public class Minimap
{
    private bool _isVisible = true;

    // --- Tile cache fields ---
    private readonly Dictionary<string, Color> _colorCache = new();
    private Color[] _pixelCache;
    private int _cachedMapW;
    private int _cachedMapH;
    private bool _dirty = true;
    private MapData _cachedMapRef;
    private int _cachedLayerCount;
    private bool[] _cachedLayerVisibility;

    public bool IsVisible
    {
        get => _isVisible;
        set => _isVisible = value;
    }

    public void Toggle() => _isVisible = !_isVisible;

    /// <summary>
    /// Marks the minimap tile cache as stale. Call this when map data changes
    /// (tile paint/erase, fill, undo/redo, entity add/remove, etc.).
    /// </summary>
    public void MarkDirty() => _dirty = true;

    public void Draw(SpriteBatch spriteBatch, EditorState state, Renderer renderer,
                     Camera camera, Rectangle canvasBounds)
    {
        if (!_isVisible) return;
        if (state.Map == null || state.Sheet == null) return;

        int mapW = state.Map.Width;
        int mapH = state.Map.Height;
        var mmRect = GetMinimapRect(mapW, mapH, canvasBounds);

        // Background
        renderer.DrawRect(spriteBatch, mmRect, LayoutConstants.MinimapBackgroundColor);

        float cellW = (float)mmRect.Width / mapW;
        float cellH = (float)mmRect.Height / mapH;

        // Auto-detect staleness: map reference changed, dimensions changed, or layer state changed
        if (IsCacheStale(state.Map, mapW, mapH))
            _dirty = true;

        // Rebuild pixel cache when dirty
        if (_dirty)
            RebuildPixelCache(state.Map, mapW, mapH);

        // Draw tiles from cache
        int pixelW = Math.Max(1, (int)Math.Ceiling(cellW));
        int pixelH = Math.Max(1, (int)Math.Ceiling(cellH));

        for (int y = 0; y < mapH; y++)
        {
            for (int x = 0; x < mapW; x++)
            {
                Color pixelColor = _pixelCache[y * mapW + x];
                if (pixelColor.A == 0) continue;

                var pixelRect = new Rectangle(
                    mmRect.X + (int)(x * cellW),
                    mmRect.Y + (int)(y * cellH),
                    pixelW, pixelH);

                renderer.DrawRect(spriteBatch, pixelRect, pixelColor);
            }
        }

        // Entity dots (not cached — entities move in play mode)
        foreach (var entity in state.Map.Entities)
        {
            var entityRect = new Rectangle(
                mmRect.X + (int)(entity.X * cellW),
                mmRect.Y + (int)(entity.Y * cellH),
                Math.Max(2, (int)Math.Ceiling(cellW)),
                Math.Max(2, (int)Math.Ceiling(cellH)));
            renderer.DrawRect(spriteBatch, entityRect, LayoutConstants.MinimapEntityColor);
        }

        // Camera viewport rectangle
        DrawViewportRect(spriteBatch, renderer, state, camera, canvasBounds, mmRect, mapW, mapH);

        // Player position dot (play mode)
        if (state.IsPlayMode && state.PlayState != null)
        {
            var play = state.PlayState;
            var playerRect = new Rectangle(
                mmRect.X + (int)(play.RenderPos.X * cellW),
                mmRect.Y + (int)(play.RenderPos.Y * cellH),
                Math.Max(3, (int)Math.Ceiling(cellW * 2)),
                Math.Max(3, (int)Math.Ceiling(cellH * 2)));
            renderer.DrawRect(spriteBatch, playerRect, LayoutConstants.MinimapPlayerColor);
        }

        // Border
        renderer.DrawRectOutline(spriteBatch, mmRect, LayoutConstants.MinimapBorderColor, 1);
    }

    private bool IsCacheStale(MapData map, int mapW, int mapH)
    {
        // Map reference or dimensions changed
        if (map != _cachedMapRef || mapW != _cachedMapW || mapH != _cachedMapH)
            return true;

        // Layer count changed
        if (map.Layers.Count != _cachedLayerCount)
            return true;

        // Layer visibility changed
        if (_cachedLayerVisibility == null || _cachedLayerVisibility.Length != map.Layers.Count)
            return true;

        for (int i = 0; i < map.Layers.Count; i++)
        {
            if (map.Layers[i].Visible != _cachedLayerVisibility[i])
                return true;
        }

        return false;
    }

    private void RebuildPixelCache(MapData map, int mapW, int mapH)
    {
        int totalCells = mapW * mapH;

        // Reallocate if dimensions changed
        if (_pixelCache == null || _pixelCache.Length != totalCells)
            _pixelCache = new Color[totalCells];

        // Clear cache to transparent
        Array.Clear(_pixelCache, 0, totalCells);

        // Scan all visible layers (bottom to top, later layers overwrite)
        for (int layerIdx = 0; layerIdx < map.Layers.Count; layerIdx++)
        {
            var layer = map.Layers[layerIdx];
            if (!layer.Visible) continue;

            for (int y = 0; y < mapH; y++)
            {
                for (int x = 0; x < mapW; x++)
                {
                    string groupName = layer.GetCell(x, y, mapW);
                    if (groupName == null) continue;

                    _pixelCache[y * mapW + x] = GetGroupColorCached(groupName);
                }
            }
        }

        // Snapshot tracking state
        _cachedMapRef = map;
        _cachedMapW = mapW;
        _cachedMapH = mapH;
        _cachedLayerCount = map.Layers.Count;

        if (_cachedLayerVisibility == null || _cachedLayerVisibility.Length != map.Layers.Count)
            _cachedLayerVisibility = new bool[map.Layers.Count];

        for (int i = 0; i < map.Layers.Count; i++)
            _cachedLayerVisibility[i] = map.Layers[i].Visible;

        _dirty = false;
    }

    private Color GetGroupColorCached(string groupName)
    {
        if (!_colorCache.TryGetValue(groupName, out var color))
        {
            color = GetGroupColor(groupName);
            _colorCache[groupName] = color;
        }
        return color;
    }

    public bool HandleClick(int mouseX, int mouseY, EditorState state, Camera camera,
                            Rectangle canvasBounds)
    {
        if (!_isVisible || state.Map == null || state.Sheet == null) return false;

        var mmRect = GetMinimapRect(state.Map.Width, state.Map.Height, canvasBounds);
        if (!mmRect.Contains(mouseX, mouseY)) return false;

        float relX = (float)(mouseX - mmRect.X) / mmRect.Width;
        float relY = (float)(mouseY - mmRect.Y) / mmRect.Height;

        float worldX = relX * state.Map.Width * state.Sheet.TileWidth;
        float worldY = relY * state.Map.Height * state.Sheet.TileHeight;

        // Center camera on this world position
        camera.Offset = new Vector2(
            canvasBounds.X + canvasBounds.Width / 2f - worldX * camera.Zoom,
            canvasBounds.Y + canvasBounds.Height / 2f - worldY * camera.Zoom);

        return true;
    }

    internal static Rectangle GetMinimapRect(int mapW, int mapH, Rectangle canvasBounds)
    {
        float aspectRatio = (float)mapW / mapH;
        int maxSize = LayoutConstants.MinimapMaxSize;
        int margin = LayoutConstants.MinimapMargin;
        int mmW, mmH;

        if (aspectRatio > 1f)
        {
            mmW = maxSize;
            mmH = Math.Max(1, (int)(maxSize / aspectRatio));
        }
        else
        {
            mmH = maxSize;
            mmW = Math.Max(1, (int)(maxSize * aspectRatio));
        }

        return new Rectangle(
            canvasBounds.Right - mmW - margin,
            canvasBounds.Bottom - mmH - margin,
            mmW, mmH);
    }

    internal static Color GetGroupColor(string groupName)
    {
        int hash = groupName.GetHashCode();
        int r = 80 + ((hash & 0xFF) % 140);
        int g = 80 + (((hash >> 8) & 0xFF) % 140);
        int b = 80 + (((hash >> 16) & 0xFF) % 140);
        return new Color(r, g, b, LayoutConstants.MinimapTileAlpha);
    }

    private void DrawViewportRect(SpriteBatch spriteBatch, Renderer renderer,
                                   EditorState state, Camera camera, Rectangle canvasBounds,
                                   Rectangle mmRect, int mapW, int mapH)
    {
        var topLeft = camera.ScreenToWorld(new Vector2(canvasBounds.X, canvasBounds.Y));
        var bottomRight = camera.ScreenToWorld(new Vector2(canvasBounds.Right, canvasBounds.Bottom));

        int tileW = state.Sheet.TileWidth;
        int tileH = state.Sheet.TileHeight;

        float cellW = (float)mmRect.Width / mapW;
        float cellH = (float)mmRect.Height / mapH;

        int vpX = mmRect.X + (int)(topLeft.X / tileW * cellW);
        int vpY = mmRect.Y + (int)(topLeft.Y / tileH * cellH);
        int vpW = (int)((bottomRight.X - topLeft.X) / tileW * cellW);
        int vpH = (int)((bottomRight.Y - topLeft.Y) / tileH * cellH);

        var vpRect = Rectangle.Intersect(new Rectangle(vpX, vpY, vpW, vpH), mmRect);

        if (vpRect.Width > 0 && vpRect.Height > 0)
            renderer.DrawRectOutline(spriteBatch, vpRect, LayoutConstants.MinimapViewportColor, 1);
    }
}
