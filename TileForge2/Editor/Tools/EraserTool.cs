using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge2.Editor.Commands;

namespace TileForge2.Editor.Tools;

public class EraserTool : ITool
{
    private static readonly Color PreviewColor = new(255, 60, 60, 120);

    private List<CellChange> _strokeChanges;

    public string Name => "Eraser";

    public void OnPress(int gridX, int gridY, EditorState state)
    {
        _strokeChanges = new List<CellChange>();
        Erase(gridX, gridY, state);
    }

    public void OnDrag(int gridX, int gridY, EditorState state)
    {
        Erase(gridX, gridY, state);
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

        int zoom = camera.Zoom;
        var screenPos = camera.WorldToScreen(new Vector2(gridX * state.Sheet.TileWidth, gridY * state.Sheet.TileHeight));
        var destRect = new Rectangle((int)screenPos.X, (int)screenPos.Y,
                                     state.Sheet.TileWidth * zoom, state.Sheet.TileHeight * zoom);
        renderer.DrawRect(spriteBatch, destRect, PreviewColor);
    }

    private void Erase(int gridX, int gridY, EditorState state)
    {
        if (state.Map == null) return;
        if (!state.Map.InBounds(gridX, gridY)) return;

        var layer = state.ActiveLayer;
        if (layer == null) return;

        string oldValue = layer.GetCell(gridX, gridY, state.Map.Width);
        if (oldValue == null) return;

        _strokeChanges?.Add(new CellChange(gridX, gridY, state.ActiveLayerName, oldValue, null));
        layer.SetCell(gridX, gridY, state.Map.Width, null);
    }
}
