using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge2.Editor.Commands;

namespace TileForge2.Editor.Tools;

public class BrushTool : ITool
{
    private static readonly Color PreviewColor = new(255, 255, 255, 100);

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
        var group = state.SelectedGroup;
        if (group == null || state.Sheet == null || group.Sprites.Count == 0)
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
        if (state.Map == null || state.SelectedGroup == null) return;
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
}
