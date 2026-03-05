using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.UI;

public static class ConnectionRenderer
{
    /// <summary>
    /// Draws a Bezier connection between two world-space points,
    /// converting to screen space via the camera.
    /// </summary>
    public static void DrawConnection(SpriteBatch spriteBatch, Renderer renderer,
        NodeGraphCamera camera, Vector2 worldStart, Vector2 worldEnd,
        Color color, float thickness = 2f)
    {
        var screenStart = camera.WorldToScreen(worldStart);
        var screenEnd = camera.WorldToScreen(worldEnd);

        // Control points: horizontal offset for S-curve
        float dx = MathHelper.Max(50f, MathHelper.Min(MathF.Abs(screenEnd.X - screenStart.X) * 0.4f, 200f));
        var cp1 = new Vector2(screenStart.X + dx, screenStart.Y);
        var cp2 = new Vector2(screenEnd.X - dx, screenEnd.Y);

        renderer.DrawBezier(spriteBatch, screenStart, cp1, cp2, screenEnd, color, thickness * camera.Zoom);
    }

    /// <summary>
    /// Draws a temporary connection from a world-space port to the current
    /// screen-space mouse position (for drag-to-connect).
    /// </summary>
    public static void DrawDragConnection(SpriteBatch spriteBatch, Renderer renderer,
        NodeGraphCamera camera, Vector2 worldStart, Vector2 screenMousePos,
        Color color, float thickness = 2f)
    {
        var screenStart = camera.WorldToScreen(worldStart);

        float dx = MathHelper.Max(30f, MathHelper.Min(MathF.Abs(screenMousePos.X - screenStart.X) * 0.4f, 150f));
        var cp1 = new Vector2(screenStart.X + dx, screenStart.Y);
        var cp2 = new Vector2(screenMousePos.X - dx, screenMousePos.Y);

        renderer.DrawBezier(spriteBatch, screenStart, cp1, cp2, screenMousePos, color, thickness * camera.Zoom);
    }
}
