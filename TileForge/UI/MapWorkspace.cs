using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Editor;

namespace TileForge.UI;

/// <summary>
/// Default workspace that delegates to the existing PanelDock + MapCanvas infrastructure.
/// All actual work is done via Action callbacks so this class stays thin.
/// </summary>
public class MapWorkspace : IWorkspace
{
    public delegate void UpdateDelegate(EditorState state, MouseState mouse, MouseState prevMouse,
                                        InputEvent input, SpriteFont font, Rectangle canvasBounds,
                                        GameTime gameTime, int screenW, int screenH);
    public delegate void DrawCanvasDelegate(SpriteBatch spriteBatch, SpriteFont font,
                                            EditorState state, Renderer renderer,
                                            Rectangle canvasBounds);
    public delegate void DrawSidebarDelegate(SpriteBatch spriteBatch, SpriteFont font,
                                             EditorState state, Renderer renderer);

    private readonly UpdateDelegate _update;
    private readonly DrawCanvasDelegate _drawCanvas;
    private readonly DrawSidebarDelegate _drawSidebar;

    public MapWorkspace(UpdateDelegate update, DrawCanvasDelegate drawCanvas,
                        DrawSidebarDelegate drawSidebar)
    {
        _update = update;
        _drawCanvas = drawCanvas;
        _drawSidebar = drawSidebar;
    }

    public void Update(EditorState state, MouseState mouse, MouseState prevMouse,
                       InputEvent input, SpriteFont font, Rectangle canvasBounds,
                       GameTime gameTime, int screenW, int screenH)
    {
        _update(state, mouse, prevMouse, input, font, canvasBounds, gameTime, screenW, screenH);
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                     Renderer renderer, Rectangle canvasBounds)
    {
        _drawCanvas(spriteBatch, font, state, renderer, canvasBounds);
    }

    public void DrawSidebar(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                            Renderer renderer, Rectangle sidebarBounds)
    {
        _drawSidebar(spriteBatch, font, state, renderer);
    }

    public void OnEnter(EditorState state) { }
    public void OnExit(EditorState state) { }
}
