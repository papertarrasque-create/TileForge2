using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge2.Editor;

namespace TileForge2.UI;

public class StatusBar
{
    public const int Height = 22;

    private static readonly Color BackgroundColor = new(35, 35, 35);
    private static readonly Color TextColor = new(160, 160, 160);
    private static readonly Color SeparatorColor = new(60, 60, 60);

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                     Renderer renderer, MapCanvas canvas, int screenWidth, int screenHeight)
    {
        int y = screenHeight - Height;
        var barRect = new Rectangle(0, y, screenWidth, Height);
        renderer.DrawRect(spriteBatch, barRect, BackgroundColor);
        renderer.DrawRect(spriteBatch, new Rectangle(0, y, screenWidth, 1), SeparatorColor);

        float textY = y + (Height - font.LineSpacing) / 2;

        // Play mode display
        if (state.IsPlayMode && state.PlayState != null)
        {
            var play = state.PlayState;
            string playLeft = $"({play.PlayerEntity.X}, {play.PlayerEntity.Y})";
            if (play.StatusMessage != null)
                playLeft += $"  {play.StatusMessage}";

            spriteBatch.DrawString(font, playLeft, new Vector2(10, textY), TextColor);

            string playRight = "Arrow keys: move  F5: exit play mode";
            var playRightSize = font.MeasureString(playRight);
            spriteBatch.DrawString(font, playRight,
                new Vector2(screenWidth - playRightSize.X - 10, textY), TextColor);
            return;
        }

        // Left side: coordinates and group name
        string left = "";
        if (canvas.HoverX >= 0 && canvas.HoverY >= 0)
        {
            left = $"({canvas.HoverX}, {canvas.HoverY})";

            // Show what's in the cell at hover position
            var layer = state.ActiveLayer;
            if (layer != null && state.Map != null)
            {
                string cellGroup = layer.GetCell(canvas.HoverX, canvas.HoverY, state.Map.Width);
                if (cellGroup != null)
                    left += $"  {cellGroup}";
            }
        }

        left += $"  Layer: {state.ActiveLayerName ?? "-"}";
        spriteBatch.DrawString(font, left, new Vector2(10, textY), TextColor);

        // Right side: hints (only draw if they won't overlap the left text)
        string right = "[G]rid  [V]isibility  [Tab] layer  Shift+Up/Down reorder  Ctrl+Z/Y undo/redo  Ctrl+S save";
        var leftSize = font.MeasureString(left);
        var rightSize = font.MeasureString(right);
        if (leftSize.X + 20 + rightSize.X + 20 <= screenWidth)
        {
            spriteBatch.DrawString(font, right,
                new Vector2(screenWidth - rightSize.X - 10, textY), TextColor);
        }
    }
}
