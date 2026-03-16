using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Editor;

namespace TileForge.UI;

public interface IWorkspace
{
    void Update(EditorState state, MouseState mouse, MouseState prevMouse,
                InputEvent input, SpriteFont font, Rectangle canvasBounds,
                GameTime gameTime, int screenW, int screenH);
    void Draw(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
              Renderer renderer, Rectangle canvasBounds);
    void DrawSidebar(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                     Renderer renderer, Rectangle sidebarBounds);
    void OnEnter(EditorState state);
    void OnExit(EditorState state);
}
