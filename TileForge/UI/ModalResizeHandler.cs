using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;

namespace TileForge.UI;

/// <summary>
/// Reusable resize logic for modal editor overlays (DialogueEditor, QuestEditor).
/// Handles edge-drag resizing, centered panel layout, and resize grip drawing.
/// Value type — embed as a field in each editor.
/// </summary>
public struct ModalResizeHandler
{
    private enum ResizeEdge { None, Right, Bottom, BottomRight }

    private int? _userWidth;
    private int? _userHeight;
    private bool _resizing;
    private ResizeEdge _resizeEdge;
    private Point _resizeDragStart;
    private int _resizeDragStartW;
    private int _resizeDragStartH;

    /// <summary>
    /// True while the user is actively dragging a resize edge.
    /// </summary>
    public readonly bool IsResizing => _resizing;

    /// <summary>
    /// The most recently computed panel rectangle. Updated by <see cref="ComputePanelRect"/>.
    /// </summary>
    public Rectangle PanelRect { get; private set; }

    /// <summary>
    /// Computes the centered panel rectangle given default max dimensions and the available bounds.
    /// Call once per frame in Update (and again in Draw if needed).
    /// </summary>
    public Rectangle ComputePanelRect(int defaultMaxWidth, int defaultMaxHeight, Rectangle bounds)
    {
        int maxW = _userWidth ?? defaultMaxWidth;
        int maxH = _userHeight ?? defaultMaxHeight;
        int panelW = Math.Min(maxW, bounds.Width - 40);
        int panelH = Math.Min(maxH, bounds.Height - 40);
        int px = bounds.X + (bounds.Width - panelW) / 2;
        int py = bounds.Y + (bounds.Height - panelH) / 2;
        PanelRect = new Rectangle(px, py, panelW, panelH);
        return PanelRect;
    }

    /// <summary>
    /// Handles mouse-driven edge resize. Call after <see cref="ComputePanelRect"/>.
    /// </summary>
    public void HandleResize(MouseState mouse, MouseState prevMouse, Rectangle bounds)
    {
        bool leftDown = mouse.LeftButton == ButtonState.Pressed;
        bool leftClick = leftDown && prevMouse.LeftButton == ButtonState.Released;
        int grab = LayoutConstants.ModalEdgeGrabSize;

        if (_resizing)
        {
            if (!leftDown)
            {
                _resizing = false;
                return;
            }

            int dx = mouse.X - _resizeDragStart.X;
            int dy = mouse.Y - _resizeDragStart.Y;

            if (_resizeEdge == ResizeEdge.Right || _resizeEdge == ResizeEdge.BottomRight)
                _userWidth = Math.Clamp(_resizeDragStartW + dx * 2, LayoutConstants.ModalMinWidth, bounds.Width - 40);
            if (_resizeEdge == ResizeEdge.Bottom || _resizeEdge == ResizeEdge.BottomRight)
                _userHeight = Math.Clamp(_resizeDragStartH + dy * 2, LayoutConstants.ModalMinHeight, bounds.Height - 40);
            return;
        }

        if (leftClick)
        {
            var edge = DetectResizeEdge(mouse.X, mouse.Y, grab);
            if (edge != ResizeEdge.None)
            {
                _resizing = true;
                _resizeEdge = edge;
                _resizeDragStart = new Point(mouse.X, mouse.Y);
                _resizeDragStartW = PanelRect.Width;
                _resizeDragStartH = PanelRect.Height;
            }
        }
    }

    /// <summary>
    /// Draws the diagonal resize grip in the bottom-right corner of the panel.
    /// </summary>
    public readonly void DrawResizeGrip(SpriteBatch spriteBatch, Renderer renderer)
    {
        int gx = PanelRect.Right - 12;
        int gy = PanelRect.Bottom - 12;
        var gripColor = new Color(80, 80, 80);
        renderer.DrawRect(spriteBatch, new Rectangle(gx + 8, gy + 8, 2, 2), gripColor);
        renderer.DrawRect(spriteBatch, new Rectangle(gx + 4, gy + 8, 2, 2), gripColor);
        renderer.DrawRect(spriteBatch, new Rectangle(gx + 8, gy + 4, 2, 2), gripColor);
        renderer.DrawRect(spriteBatch, new Rectangle(gx, gy + 8, 2, 2), gripColor);
        renderer.DrawRect(spriteBatch, new Rectangle(gx + 4, gy + 4, 2, 2), gripColor);
        renderer.DrawRect(spriteBatch, new Rectangle(gx + 8, gy, 2, 2), gripColor);
    }

    private readonly ResizeEdge DetectResizeEdge(int mx, int my, int grab)
    {
        bool nearRight = mx >= PanelRect.Right - grab && mx <= PanelRect.Right + grab &&
                         my >= PanelRect.Y && my <= PanelRect.Bottom;
        bool nearBottom = my >= PanelRect.Bottom - grab && my <= PanelRect.Bottom + grab &&
                          mx >= PanelRect.X && mx <= PanelRect.Right;

        if (nearRight && nearBottom) return ResizeEdge.BottomRight;
        if (nearRight) return ResizeEdge.Right;
        if (nearBottom) return ResizeEdge.Bottom;
        return ResizeEdge.None;
    }
}
