using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Editor.Tools;

public class SelectionTool : ITool
{
    private static readonly Color PreviewColor = LayoutConstants.SelectionPreviewColor;
    private static readonly Color OutlineColor = LayoutConstants.SelectionOutlineColor;
    private const int OutlineThickness = LayoutConstants.SelectionOutlineThickness;

    // Drag anchors (grid coords of the initial press)
    private int _anchorX;
    private int _anchorY;
    private bool _isDragging;

    public string Name => "Selection";

    public void OnPress(int gridX, int gridY, EditorState state)
    {
        // If we already have a selection and click outside it, clear the selection
        if (state.TileSelection.HasValue)
        {
            var sel = state.TileSelection.Value;
            if (!sel.Contains(gridX, gridY))
            {
                state.TileSelection = null;
            }
        }

        // Start a new selection drag
        _anchorX = gridX;
        _anchorY = gridY;
        _isDragging = true;

        // Set a 1x1 selection at the press point
        state.TileSelection = new Rectangle(gridX, gridY, 1, 1);
    }

    public void OnDrag(int gridX, int gridY, EditorState state)
    {
        if (!_isDragging) return;

        int minX = Math.Min(_anchorX, gridX);
        int minY = Math.Min(_anchorY, gridY);
        int maxX = Math.Max(_anchorX, gridX);
        int maxY = Math.Max(_anchorY, gridY);

        state.TileSelection = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    public void OnRelease(EditorState state)
    {
        _isDragging = false;
    }

    public void DrawPreview(SpriteBatch spriteBatch, int gridX, int gridY,
                            EditorState state, Camera camera, Renderer renderer)
    {
        if (state.Sheet == null) return;

        int zoom = camera.Zoom;
        int tileW = state.Sheet.TileWidth;
        int tileH = state.Sheet.TileHeight;

        // Draw hover cell outline when no selection yet
        if (!state.TileSelection.HasValue)
        {
            var screenPos = camera.WorldToScreen(new Vector2(gridX * tileW, gridY * tileH));
            var cellRect = new Rectangle((int)screenPos.X, (int)screenPos.Y,
                                         tileW * zoom, tileH * zoom);
            renderer.DrawRectOutline(spriteBatch, cellRect, PreviewColor, 1);
        }

        // Draw selection rectangle
        DrawSelectionOutline(spriteBatch, state, camera, renderer);
    }

    /// <summary>
    /// Draws the selection rectangle outline. Called both from DrawPreview and from MapCanvas
    /// so the selection remains visible even when the cursor is outside the map.
    /// </summary>
    public static void DrawSelectionOutline(SpriteBatch spriteBatch, EditorState state,
                                            Camera camera, Renderer renderer)
    {
        if (!state.TileSelection.HasValue || state.Sheet == null) return;

        var sel = state.TileSelection.Value;
        int zoom = camera.Zoom;
        int tileW = state.Sheet.TileWidth;
        int tileH = state.Sheet.TileHeight;

        var topLeft = camera.WorldToScreen(new Vector2(sel.X * tileW, sel.Y * tileH));
        var selRect = new Rectangle((int)topLeft.X, (int)topLeft.Y,
                                    sel.Width * tileW * zoom, sel.Height * tileH * zoom);

        renderer.DrawRectOutline(spriteBatch, selRect, OutlineColor, OutlineThickness);
    }
}
