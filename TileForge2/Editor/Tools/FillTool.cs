using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge2.Data;
using TileForge2.Editor.Commands;

namespace TileForge2.Editor.Tools;

public class FillTool : ITool
{
    private static readonly Color PreviewColor = new(180, 255, 180, 80);

    public string Name => "Fill";

    public void OnPress(int gridX, int gridY, EditorState state)
    {
        if (state.Map == null || state.SelectedGroup == null) return;
        if (!state.Map.InBounds(gridX, gridY)) return;
        if (state.SelectedGroup.Type != GroupType.Tile) return;

        var layer = state.ActiveLayer;
        if (layer == null) return;

        string target = layer.GetCell(gridX, gridY, state.Map.Width);
        string fill = state.SelectedGroup.Name;

        if (target == fill) return;

        var changes = new List<CellChange>();
        var visited = new bool[state.Map.Width, state.Map.Height];
        var queue = new Queue<(int X, int Y)>();

        queue.Enqueue((gridX, gridY));
        visited[gridX, gridY] = true;

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            string current = layer.GetCell(cx, cy, state.Map.Width);

            if (current != target) continue;

            changes.Add(new CellChange(cx, cy, state.ActiveLayerName, current, fill));
            layer.SetCell(cx, cy, state.Map.Width, fill);

            TryEnqueue(state.Map, cx - 1, cy, visited, queue);
            TryEnqueue(state.Map, cx + 1, cy, visited, queue);
            TryEnqueue(state.Map, cx, cy - 1, visited, queue);
            TryEnqueue(state.Map, cx, cy + 1, visited, queue);
        }

        if (changes.Count > 0)
        {
            state.UndoStack.Push(new CellStrokeCommand(state.Map, changes));
        }
    }

    public void OnDrag(int gridX, int gridY, EditorState state)
    {
    }

    public void OnRelease(EditorState state)
    {
    }

    public void DrawPreview(SpriteBatch spriteBatch, int gridX, int gridY,
                            EditorState state, Camera camera, Renderer renderer)
    {
        var group = state.SelectedGroup;
        if (group == null || state.Sheet == null || group.Sprites.Count == 0) return;
        if (group.Type != GroupType.Tile) return;

        var sprite = group.Sprites[0];
        var srcRect = state.Sheet.GetTileRect(sprite.Col, sprite.Row);
        int zoom = camera.Zoom;
        var screenPos = camera.WorldToScreen(
            new Vector2(gridX * state.Sheet.TileWidth, gridY * state.Sheet.TileHeight));
        var destRect = new Rectangle((int)screenPos.X, (int)screenPos.Y,
                                     state.Sheet.TileWidth * zoom, state.Sheet.TileHeight * zoom);
        spriteBatch.Draw(state.Sheet.Texture, destRect, srcRect, PreviewColor);
    }

    private static void TryEnqueue(MapData map, int x, int y, bool[,] visited, Queue<(int, int)> queue)
    {
        if (!map.InBounds(x, y)) return;
        if (visited[x, y]) return;
        visited[x, y] = true;
        queue.Enqueue((x, y));
    }
}
