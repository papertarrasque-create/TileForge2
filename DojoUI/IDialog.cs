using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DojoUI;

public interface IDialog
{
    bool IsComplete { get; }
    bool WasCancelled { get; }
    void Update(KeyboardState keyboard, KeyboardState prevKeyboard, GameTime gameTime);
    void OnTextInput(char character);
    void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer, int screenWidth, int screenHeight, GameTime gameTime);
}
