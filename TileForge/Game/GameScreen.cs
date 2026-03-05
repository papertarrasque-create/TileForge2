using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Game;

public abstract class GameScreen
{
    public ScreenManager ScreenManager { get; internal set; }

    public abstract void Update(GameTime gameTime, GameInputManager input);
    public abstract void Draw(SpriteBatch spriteBatch, SpriteFont font,
                              Renderer renderer, Rectangle canvasBounds);
    public virtual void OnEnter() { }
    public virtual void OnExit() { }
    public virtual bool IsOverlay => false;
}
