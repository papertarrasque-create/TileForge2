using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DojoUI;

/// <summary>
/// Manages scrollable content within a clipping region.
/// Provides scissor clipping, mouse wheel scrolling, and a visual scroll bar.
///
/// Usage pattern (immediate-mode):
///   1. Call BeginScroll() before drawing content — sets up scissor clip
///   2. Draw content at (original Y - ScrollOffset)
///   3. Call EndScroll(totalContentHeight) — restores scissor, draws scroll bar
///   4. Call UpdateScroll() in Update to handle mouse wheel input
/// </summary>
public class ScrollPanel
{
    private static readonly Color TrackColor = new(40, 40, 40);
    private static readonly Color ThumbColor = new(100, 100, 100);
    private static readonly Color ThumbHoverColor = new(130, 130, 130);
    private static readonly RasterizerState ScissorRasterizer = new() { ScissorTestEnable = true };

    public const int ScrollBarWidth = 6;
    public const int ScrollStep = 20;

    /// <summary>Current scroll offset in pixels.</summary>
    public int ScrollOffset { get; set; }

    private Rectangle _viewport;
    private Rectangle _savedScissor;
    private int _totalContentHeight;
    private bool _thumbHovered;

    /// <summary>Whether content overflows the viewport.</summary>
    public bool ContentOverflows => _totalContentHeight > _viewport.Height;

    /// <summary>
    /// Sets up scissor clipping for the content area.
    /// Returns the adjusted Y start position (viewport.Y - ScrollOffset).
    /// </summary>
    public int BeginScroll(SpriteBatch spriteBatch, Rectangle viewport)
    {
        _viewport = viewport;

        _savedScissor = spriteBatch.GraphicsDevice.ScissorRectangle;
        spriteBatch.End();
        spriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: ScissorRasterizer);
        spriteBatch.GraphicsDevice.ScissorRectangle = viewport;

        return viewport.Y - ScrollOffset;
    }

    /// <summary>
    /// Restores scissor clipping and draws scroll bar if content overflows.
    /// </summary>
    public void EndScroll(SpriteBatch spriteBatch, Renderer renderer, int totalContentHeight)
    {
        _totalContentHeight = totalContentHeight;

        // Clamp scroll offset
        int maxScroll = Math.Max(0, totalContentHeight - _viewport.Height);
        ScrollOffset = Math.Clamp(ScrollOffset, 0, maxScroll);

        // Restore scissor state
        spriteBatch.End();
        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        spriteBatch.GraphicsDevice.ScissorRectangle = _savedScissor;

        // Draw scroll bar if content overflows
        if (totalContentHeight > _viewport.Height)
        {
            DrawScrollBar(spriteBatch, renderer, maxScroll);
        }
    }

    /// <summary>
    /// Handle mouse wheel scrolling. Call in Update().
    /// </summary>
    public void UpdateScroll(MouseState mouse, MouseState prevMouse, Rectangle viewport)
    {
        _viewport = viewport;

        // Track thumb hover for visual feedback
        if (ContentOverflows)
        {
            int barX = viewport.Right - ScrollBarWidth;
            var trackRect = new Rectangle(barX, viewport.Y, ScrollBarWidth, viewport.Height);
            _thumbHovered = trackRect.Contains(mouse.X, mouse.Y);
        }
        else
        {
            _thumbHovered = false;
        }

        // Mouse wheel scrolling (only when cursor is over viewport)
        if (!viewport.Contains(mouse.X, mouse.Y)) return;

        int scrollDelta = mouse.ScrollWheelValue - prevMouse.ScrollWheelValue;
        if (scrollDelta != 0)
        {
            ScrollOffset -= scrollDelta > 0 ? ScrollStep : -ScrollStep;
            ClampScroll();
        }
    }

    /// <summary>
    /// Clamp scroll offset to valid range.
    /// </summary>
    public void ClampScroll()
    {
        int maxScroll = Math.Max(0, _totalContentHeight - _viewport.Height);
        ScrollOffset = Math.Clamp(ScrollOffset, 0, maxScroll);
    }

    private void DrawScrollBar(SpriteBatch spriteBatch, Renderer renderer, int maxScroll)
    {
        int barX = _viewport.Right - ScrollBarWidth;
        int barH = _viewport.Height;

        // Track
        renderer.DrawRect(spriteBatch, new Rectangle(barX, _viewport.Y, ScrollBarWidth, barH), TrackColor);

        // Thumb
        float thumbRatio = (float)_viewport.Height / _totalContentHeight;
        int thumbH = Math.Max(20, (int)(barH * thumbRatio));
        float scrollRatio = maxScroll <= 0 ? 0 : (float)ScrollOffset / maxScroll;
        int thumbY = _viewport.Y + (int)((barH - thumbH) * scrollRatio);

        Color thumbColor = _thumbHovered ? ThumbHoverColor : ThumbColor;
        renderer.DrawRect(spriteBatch, new Rectangle(barX, thumbY, ScrollBarWidth, thumbH), thumbColor);
    }
}
