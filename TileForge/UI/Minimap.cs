using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge.Editor;

namespace TileForge.UI;

public class Minimap
{
    private bool _isVisible = true;

    public bool IsVisible
    {
        get => _isVisible;
        set => _isVisible = value;
    }

    public void Toggle() => _isVisible = !_isVisible;

    public void Draw(SpriteBatch spriteBatch, EditorState state, Renderer renderer,
                     Camera camera, Rectangle canvasBounds)
    {
        if (!_isVisible) return;
        if (state.Map == null || state.Sheet == null) return;

        int mapW = state.Map.Width;
        int mapH = state.Map.Height;
        var mmRect = GetMinimapRect(mapW, mapH, canvasBounds);

        // Background
        renderer.DrawRect(spriteBatch, mmRect, LayoutConstants.MinimapBackgroundColor);

        float cellW = (float)mmRect.Width / mapW;
        float cellH = (float)mmRect.Height / mapH;

        // Draw tiles as colored pixels
        for (int layerIdx = 0; layerIdx < state.Map.Layers.Count; layerIdx++)
        {
            var layer = state.Map.Layers[layerIdx];
            if (!layer.Visible) continue;

            for (int y = 0; y < mapH; y++)
            {
                for (int x = 0; x < mapW; x++)
                {
                    string groupName = layer.GetCell(x, y, mapW);
                    if (groupName == null) continue;

                    Color pixelColor = GetGroupColor(groupName);
                    var pixelRect = new Rectangle(
                        mmRect.X + (int)(x * cellW),
                        mmRect.Y + (int)(y * cellH),
                        Math.Max(1, (int)Math.Ceiling(cellW)),
                        Math.Max(1, (int)Math.Ceiling(cellH)));

                    renderer.DrawRect(spriteBatch, pixelRect, pixelColor);
                }
            }
        }

        // Entity dots
        foreach (var entity in state.Map.Entities)
        {
            var entityRect = new Rectangle(
                mmRect.X + (int)(entity.X * cellW),
                mmRect.Y + (int)(entity.Y * cellH),
                Math.Max(2, (int)Math.Ceiling(cellW)),
                Math.Max(2, (int)Math.Ceiling(cellH)));
            renderer.DrawRect(spriteBatch, entityRect, LayoutConstants.MinimapEntityColor);
        }

        // Camera viewport rectangle
        DrawViewportRect(spriteBatch, renderer, state, camera, canvasBounds, mmRect, mapW, mapH);

        // Player position dot (play mode)
        if (state.IsPlayMode && state.PlayState != null)
        {
            var play = state.PlayState;
            var playerRect = new Rectangle(
                mmRect.X + (int)(play.RenderPos.X * cellW),
                mmRect.Y + (int)(play.RenderPos.Y * cellH),
                Math.Max(3, (int)Math.Ceiling(cellW * 2)),
                Math.Max(3, (int)Math.Ceiling(cellH * 2)));
            renderer.DrawRect(spriteBatch, playerRect, LayoutConstants.MinimapPlayerColor);
        }

        // Border
        renderer.DrawRectOutline(spriteBatch, mmRect, LayoutConstants.MinimapBorderColor, 1);
    }

    public bool HandleClick(int mouseX, int mouseY, EditorState state, Camera camera,
                            Rectangle canvasBounds)
    {
        if (!_isVisible || state.Map == null || state.Sheet == null) return false;

        var mmRect = GetMinimapRect(state.Map.Width, state.Map.Height, canvasBounds);
        if (!mmRect.Contains(mouseX, mouseY)) return false;

        float relX = (float)(mouseX - mmRect.X) / mmRect.Width;
        float relY = (float)(mouseY - mmRect.Y) / mmRect.Height;

        float worldX = relX * state.Map.Width * state.Sheet.TileWidth;
        float worldY = relY * state.Map.Height * state.Sheet.TileHeight;

        // Center camera on this world position
        camera.Offset = new Vector2(
            canvasBounds.X + canvasBounds.Width / 2f - worldX * camera.Zoom,
            canvasBounds.Y + canvasBounds.Height / 2f - worldY * camera.Zoom);

        return true;
    }

    internal static Rectangle GetMinimapRect(int mapW, int mapH, Rectangle canvasBounds)
    {
        float aspectRatio = (float)mapW / mapH;
        int maxSize = LayoutConstants.MinimapMaxSize;
        int margin = LayoutConstants.MinimapMargin;
        int mmW, mmH;

        if (aspectRatio > 1f)
        {
            mmW = maxSize;
            mmH = Math.Max(1, (int)(maxSize / aspectRatio));
        }
        else
        {
            mmH = maxSize;
            mmW = Math.Max(1, (int)(maxSize * aspectRatio));
        }

        return new Rectangle(
            canvasBounds.Right - mmW - margin,
            canvasBounds.Bottom - mmH - margin,
            mmW, mmH);
    }

    internal static Color GetGroupColor(string groupName)
    {
        int hash = groupName.GetHashCode();
        int r = 80 + ((hash & 0xFF) % 140);
        int g = 80 + (((hash >> 8) & 0xFF) % 140);
        int b = 80 + (((hash >> 16) & 0xFF) % 140);
        return new Color(r, g, b, LayoutConstants.MinimapTileAlpha);
    }

    private void DrawViewportRect(SpriteBatch spriteBatch, Renderer renderer,
                                   EditorState state, Camera camera, Rectangle canvasBounds,
                                   Rectangle mmRect, int mapW, int mapH)
    {
        var topLeft = camera.ScreenToWorld(new Vector2(canvasBounds.X, canvasBounds.Y));
        var bottomRight = camera.ScreenToWorld(new Vector2(canvasBounds.Right, canvasBounds.Bottom));

        int tileW = state.Sheet.TileWidth;
        int tileH = state.Sheet.TileHeight;

        float cellW = (float)mmRect.Width / mapW;
        float cellH = (float)mmRect.Height / mapH;

        int vpX = mmRect.X + (int)(topLeft.X / tileW * cellW);
        int vpY = mmRect.Y + (int)(topLeft.Y / tileH * cellH);
        int vpW = (int)((bottomRight.X - topLeft.X) / tileW * cellW);
        int vpH = (int)((bottomRight.Y - topLeft.Y) / tileH * cellH);

        var vpRect = Rectangle.Intersect(new Rectangle(vpX, vpY, vpW, vpH), mmRect);

        if (vpRect.Width > 0 && vpRect.Height > 0)
            renderer.DrawRectOutline(spriteBatch, vpRect, LayoutConstants.MinimapViewportColor, 1);
    }
}
