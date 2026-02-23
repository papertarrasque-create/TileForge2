using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge.Editor;

namespace TileForge.Editor.Tools;

public interface ITool
{
    string Name { get; }
    void OnPress(int gridX, int gridY, EditorState state);
    void OnDrag(int gridX, int gridY, EditorState state);
    void OnRelease(EditorState state);
    void DrawPreview(SpriteBatch spriteBatch, int gridX, int gridY,
                     EditorState state, Camera camera, Renderer renderer);
}
