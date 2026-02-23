using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge.Data;
using TileForge.Editor.Commands;

namespace TileForge.Editor.Tools;

public class EntityTool : ITool
{
    private static readonly Color PreviewColor = LayoutConstants.EntityPreviewColor;

    private enum Mode { None, Placed, Dragging }

    private Mode _mode;
    private Entity _dragEntity;
    private int _dragStartX, _dragStartY;

    public string Name => "Entity";

    public void OnPress(int gridX, int gridY, EditorState state)
    {
        if (state.Map == null) return;

        // Check if there's an entity at this position
        var hit = FindEntityAt(state, gridX, gridY);

        if (hit != null)
        {
            // Select and prepare for drag
            state.SelectedEntityId = hit.Id;
            _mode = Mode.Dragging;
            _dragEntity = hit;
            _dragStartX = hit.X;
            _dragStartY = hit.Y;
        }
        else
        {
            // Deselect
            state.SelectedEntityId = null;

            // Place new entity if an Entity-type group is selected
            var group = state.SelectedGroup;
            if (group == null || group.Type != GroupType.Entity) return;
            if (!state.Map.InBounds(gridX, gridY)) return;

            var entity = new Entity
            {
                GroupName = group.Name,
                X = gridX,
                Y = gridY,
            };

            state.Map.Entities.Add(entity);
            state.SelectedEntityId = entity.Id;
            state.UndoStack.Push(new PlaceEntityCommand(state.Map, entity));
            _mode = Mode.Placed;
        }
    }

    public void OnDrag(int gridX, int gridY, EditorState state)
    {
        if (_mode != Mode.Dragging || _dragEntity == null) return;
        if (state.Map == null || !state.Map.InBounds(gridX, gridY)) return;

        _dragEntity.X = gridX;
        _dragEntity.Y = gridY;
    }

    public void OnRelease(EditorState state)
    {
        if (_mode == Mode.Dragging && _dragEntity != null)
        {
            var cmd = new MoveEntityCommand(_dragEntity, _dragStartX, _dragStartY,
                                            _dragEntity.X, _dragEntity.Y);
            if (!cmd.IsNoOp)
                state.UndoStack.Push(cmd);
        }

        _mode = Mode.None;
        _dragEntity = null;
    }

    public void DrawPreview(SpriteBatch spriteBatch, int gridX, int gridY,
                            EditorState state, Camera camera, Renderer renderer)
    {
        if (state.SelectedGroup?.Type != GroupType.Entity) return;
        if (state.SelectedGroup.Sprites.Count == 0 || state.Sheet == null) return;
        if (FindEntityAt(state, gridX, gridY) != null) return;

        var sprite = state.SelectedGroup.Sprites[0];
        var srcRect = state.Sheet.GetTileRect(sprite.Col, sprite.Row);
        int zoom = camera.Zoom;
        var screenPos = camera.WorldToScreen(
            new Vector2(gridX * state.Sheet.TileWidth, gridY * state.Sheet.TileHeight));
        var destRect = new Rectangle((int)screenPos.X, (int)screenPos.Y,
                                     state.Sheet.TileWidth * zoom, state.Sheet.TileHeight * zoom);
        spriteBatch.Draw(state.Sheet.Texture, destRect, srcRect, PreviewColor);
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
