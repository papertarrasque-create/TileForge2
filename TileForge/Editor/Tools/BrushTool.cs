using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge.Editor.Commands;

namespace TileForge.Editor.Tools;

public class BrushTool : ITool
{
    private static readonly Color PreviewColor = LayoutConstants.BrushPreviewColor;
    private static readonly Color StampPreviewColor = LayoutConstants.StampPreviewColor;
    private static readonly Color StampOutlineColor = LayoutConstants.StampOutlineColor;

    private List<CellChange> _strokeChanges;

    public string Name => "Brush";

    public void OnPress(int gridX, int gridY, EditorState state)
    {
        _strokeChanges = new List<CellChange>();
        Paint(gridX, gridY, state);
    }

    public void OnDrag(int gridX, int gridY, EditorState state)
    {
        Paint(gridX, gridY, state);
    }

    public void OnRelease(EditorState state)
    {
        if (_strokeChanges != null && _strokeChanges.Count > 0)
        {
            state.UndoStack.Push(new CellStrokeCommand(state.Map, _strokeChanges));
        }
        _strokeChanges = null;
    }

    public void DrawPreview(SpriteBatch spriteBatch, int gridX, int gridY,
                            EditorState state, Camera camera, Renderer renderer)
    {
        if (state.Sheet == null) return;

        // Stamp mode preview
        if (state.Clipboard != null)
        {
            DrawStampPreview(spriteBatch, gridX, gridY, state, camera, renderer);
            return;
        }

        var group = state.SelectedGroup;
        if (group == null || group.Sprites.Count == 0)
            return;

        var sprite = group.Sprites[0];
        var srcRect = state.Sheet.GetTileRect(sprite.Col, sprite.Row);
        int zoom = camera.Zoom;
        var screenPos = camera.WorldToScreen(new Vector2(gridX * state.Sheet.TileWidth, gridY * state.Sheet.TileHeight));
        var destRect = new Rectangle((int)screenPos.X, (int)screenPos.Y,
                                     state.Sheet.TileWidth * zoom, state.Sheet.TileHeight * zoom);
        spriteBatch.Draw(state.Sheet.Texture, destRect, srcRect, PreviewColor);
    }

    private void Paint(int gridX, int gridY, EditorState state)
    {
        if (state.Map == null) return;

        // Stamp mode: paint the clipboard pattern
        if (state.Clipboard != null)
        {
            PaintStamp(gridX, gridY, state);
            return;
        }

        if (state.SelectedGroup == null) return;
        if (state.SelectedGroup.Type != Data.GroupType.Tile) return;
        if (!state.Map.InBounds(gridX, gridY)) return;

        var layer = state.ActiveLayer;
        if (layer == null) return;

        string oldValue = layer.GetCell(gridX, gridY, state.Map.Width);
        string newValue = state.SelectedGroup.Name;

        if (oldValue == newValue) return;

        _strokeChanges?.Add(new CellChange(gridX, gridY, state.ActiveLayerName, oldValue, newValue));
        layer.SetCell(gridX, gridY, state.Map.Width, newValue);
    }

    internal void PaintStamp(int gridX, int gridY, EditorState state)
    {
        var clipboard = state.Clipboard;
        if (clipboard == null || state.Map == null) return;

        var layer = state.ActiveLayer;
        if (layer == null) return;

        for (int cy = 0; cy < clipboard.Height; cy++)
        {
            for (int cx = 0; cx < clipboard.Width; cx++)
            {
                int mapX = gridX + cx;
                int mapY = gridY + cy;
                if (!state.Map.InBounds(mapX, mapY)) continue;

                string newValue = clipboard.GetCell(cx, cy);
                if (newValue == null) continue;

                string oldValue = layer.GetCell(mapX, mapY, state.Map.Width);
                if (oldValue == newValue) continue;

                _strokeChanges?.Add(new CellChange(mapX, mapY, state.ActiveLayerName, oldValue, newValue));
                layer.SetCell(mapX, mapY, state.Map.Width, newValue);
            }
        }
    }

    private void DrawStampPreview(SpriteBatch spriteBatch, int gridX, int gridY,
                                   EditorState state, Camera camera, Renderer renderer)
    {
        var clipboard = state.Clipboard;
        int zoom = camera.Zoom;
        int tileW = state.Sheet.TileWidth;
        int tileH = state.Sheet.TileHeight;

        // Draw outline of stamp area
        var topLeftScreen = camera.WorldToScreen(new Vector2(gridX * tileW, gridY * tileH));
        var stampRect = new Rectangle((int)topLeftScreen.X, (int)topLeftScreen.Y,
                                      clipboard.Width * tileW * zoom, clipboard.Height * tileH * zoom);
        renderer.DrawRectOutline(spriteBatch, stampRect, StampOutlineColor, 1);

        // Draw faint tile previews for each non-null cell
        for (int cy = 0; cy < clipboard.Height; cy++)
        {
            for (int cx = 0; cx < clipboard.Width; cx++)
            {
                string groupName = clipboard.GetCell(cx, cy);
                if (groupName == null) continue;
                if (!state.GroupsByName.TryGetValue(groupName, out var group)) continue;
                if (group.Sprites.Count == 0) continue;

                var sprite = group.Sprites[0];
                var srcRect = state.Sheet.GetTileRect(sprite.Col, sprite.Row);
                var screenPos = camera.WorldToScreen(new Vector2((gridX + cx) * tileW, (gridY + cy) * tileH));
                var destRect = new Rectangle((int)screenPos.X, (int)screenPos.Y, tileW * zoom, tileH * zoom);
                spriteBatch.Draw(state.Sheet.Texture, destRect, srcRect, StampPreviewColor);
            }
        }
    }
}
