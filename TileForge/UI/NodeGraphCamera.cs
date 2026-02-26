using Microsoft.Xna.Framework;

namespace TileForge.UI;

public class NodeGraphCamera
{
    public Vector2 Offset { get; set; }
    public float Zoom { get; private set; } = 1.0f;

    public const float MinZoom = 0.25f;
    public const float MaxZoom = 3.0f;

    public Vector2 WorldToScreen(Vector2 worldPos)
    {
        return worldPos * Zoom + Offset;
    }

    public Vector2 ScreenToWorld(Vector2 screenPos)
    {
        return (screenPos - Offset) / Zoom;
    }

    public void AdjustZoom(float delta, Vector2 screenCenter)
    {
        // Keep the point under the cursor stable
        var worldBefore = ScreenToWorld(screenCenter);
        Zoom = MathHelper.Clamp(Zoom + delta, MinZoom, MaxZoom);
        var worldAfter = ScreenToWorld(screenCenter);
        Offset += (worldAfter - worldBefore) * Zoom;
    }

    public void CenterOn(float worldX, float worldY, int viewportWidth, int viewportHeight)
    {
        Offset = new Vector2(
            viewportWidth / 2f - worldX * Zoom,
            viewportHeight / 2f - worldY * Zoom);
    }
}
