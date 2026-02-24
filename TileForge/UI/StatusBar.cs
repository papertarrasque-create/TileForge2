using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge.Editor;

namespace TileForge.UI;

public class StatusBar
{
    public const int Height = LayoutConstants.StatusBarHeight;

    private static readonly Color BackgroundColor = LayoutConstants.StatusBarBackground;
    private static readonly Color TextColor = LayoutConstants.StatusBarTextColor;
    private static readonly Color SeparatorColor = LayoutConstants.StatusBarSeparatorColor;

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
        string gridLabel = state.Grid.Mode switch
        {
            Editor.GridMode.Normal => "Grid",
            Editor.GridMode.Fine => "Grid:Fine",
            _ => "Grid:Off",
        };
        left += $"  [{gridLabel}]";
        spriteBatch.DrawString(font, left, new Vector2(10, textY), TextColor);

        // Right side: contextual tool hint
        string right = GetToolHint(state);
        var leftSize = font.MeasureString(left);
        var rightSize = font.MeasureString(right);
        if (leftSize.X + 20 + rightSize.X + 20 <= screenWidth)
        {
            spriteBatch.DrawString(font, right,
                new Vector2(screenWidth - rightSize.X - 10, textY), TextColor);
        }
    }

    private static string GetToolHint(EditorState state)
    {
        if (state.Clipboard != null)
            return "Click to stamp | Esc clear clipboard";

        string toolName = state.ActiveTool?.Name;
        return toolName switch
        {
            "Brush" => "Click to paint | Shift+Click line | Ctrl+Z undo",
            "Eraser" => "Click to erase | Ctrl+Z undo",
            "Fill" => "Click to fill connected area | Ctrl+Z undo",
            "Entity" => "Click to place entity | Right-click to interact",
            "Picker" => "Click to pick tile or entity from canvas",
            "Selection" => "Click+Drag to select | Ctrl+C copy | Ctrl+V paste | Del delete",
            _ => "Select a tool from the toolbar or Tools menu",
        };
    }
}
