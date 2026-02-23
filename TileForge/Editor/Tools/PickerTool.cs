using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge.Data;

namespace TileForge.Editor.Tools;

public class PickerTool : ITool
{
    private static readonly Color PreviewColor = LayoutConstants.PickerPreviewColor;

    public string Name => "Picker";

    public void OnPress(int gridX, int gridY, EditorState state)
    {
        if (state.Map == null) return;
        if (!state.Map.InBounds(gridX, gridY)) return;

        // First check for entities at this position (entities take priority)
        var entity = FindEntityAt(state, gridX, gridY);
        if (entity != null)
        {
            state.SelectedGroupName = entity.GroupName;
            state.SelectedEntityId = entity.Id;
            state.ActiveTool = new EntityTool();
            return;
        }

        // Then check the active layer for a tile group
        var layer = state.ActiveLayer;
        if (layer != null)
        {
            string groupName = layer.GetCell(gridX, gridY, state.Map.Width);
            if (groupName != null && state.GroupsByName.TryGetValue(groupName, out var group))
            {
                state.SelectedGroupName = groupName;

                if (group.Type == GroupType.Entity)
                    state.ActiveTool = new EntityTool();
                else
                    state.ActiveTool = new BrushTool();

                return;
            }
        }
    }

    public void OnDrag(int gridX, int gridY, EditorState state)
    {
        // No-op
    }

    public void OnRelease(EditorState state)
    {
        // No-op
    }

    public void DrawPreview(SpriteBatch spriteBatch, int gridX, int gridY,
                            EditorState state, Camera camera, Renderer renderer)
    {
        if (state.Sheet == null) return;

        int zoom = camera.Zoom;
        int tileW = state.Sheet.TileWidth;
        int tileH = state.Sheet.TileHeight;
        var screenPos = camera.WorldToScreen(new Vector2(gridX * tileW, gridY * tileH));
        int cellW = tileW * zoom;
        int cellH = tileH * zoom;

        // Draw a crosshair outline to indicate picker mode
        var cellRect = new Rectangle((int)screenPos.X, (int)screenPos.Y, cellW, cellH);
        renderer.DrawRectOutline(spriteBatch, cellRect, PreviewColor, 2);

        // Draw crosshair lines through the center
        int cx = (int)screenPos.X + cellW / 2;
        int cy = (int)screenPos.Y + cellH / 2;
        int armLen = cellW / 4;

        // Horizontal line
        renderer.DrawRect(spriteBatch, new Rectangle(cx - armLen, cy, armLen * 2, 1), PreviewColor);
        // Vertical line
        renderer.DrawRect(spriteBatch, new Rectangle(cx, cy - armLen, 1, armLen * 2), PreviewColor);
    }

    private static Entity FindEntityAt(EditorState state, int gridX, int gridY)
    {
        if (state.Map == null) return null;
        for (int i = state.Map.Entities.Count - 1; i >= 0; i--)
        {
            var e = state.Map.Entities[i];
            if (e.X == gridX && e.Y == gridY)
                return e;
        }
        return null;
    }
}
