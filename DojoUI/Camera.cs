using System;
using Microsoft.Xna.Framework;

namespace DojoUI;

public class Camera
{
    private static readonly int[] ZoomLevels = { 1, 2, 3, 4, 6, 8, 12, 16 };
    private int _zoomIndex = 1; // start at 2x

    public Vector2 Offset;

    public int Zoom => ZoomLevels[_zoomIndex];

    public int ZoomIndex
    {
        get => _zoomIndex;
        set => _zoomIndex = Math.Clamp(value, 0, ZoomLevels.Length - 1);
    }

    public Vector2 WorldToScreen(Vector2 worldPos)
    {
        return worldPos * Zoom + Offset;
    }

    public Vector2 ScreenToWorld(Vector2 screenPos)
    {
        return (screenPos - Offset) / Zoom;
    }

    public void CenterOn(int imageWidth, int imageHeight, int screenWidth, int screenHeight)
    {
        Offset = new Vector2(
            (screenWidth - imageWidth * Zoom) / 2f,
            (screenHeight - imageHeight * Zoom) / 2f);
    }

    public void AdjustZoom(int direction, int screenWidth, int screenHeight)
    {
        int newIndex = Math.Clamp(_zoomIndex + direction, 0, ZoomLevels.Length - 1);
        if (newIndex == _zoomIndex) return;

        // Keep screen center stable
        float centerX = (screenWidth / 2f - Offset.X) / Zoom;
        float centerY = (screenHeight / 2f - Offset.Y) / Zoom;

        _zoomIndex = newIndex;

        Offset.X = screenWidth / 2f - centerX * Zoom;
        Offset.Y = screenHeight / 2f - centerY * Zoom;
    }
}
