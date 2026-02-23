using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Editor;

namespace TileForge.UI;

public class PanelDock
{
    public const int Width = LayoutConstants.PanelDockWidth;

    private static readonly Color BackgroundColor = LayoutConstants.PanelDockBackground;
    private static readonly Color DragIndicatorColor = LayoutConstants.PanelDockDragIndicator;

    private readonly List<Panel> _panels = new();
    private Rectangle _bounds;

    // Header drag-to-reorder state
    private int _dragIndex = -1;
    private int _dragMouseStartY;
    private int _dragStartBoundsY;
    private bool _isDragging;
    private int _mouseDownHeaderIndex = -1;
    private int _mouseDownY;

    public List<Panel> Panels => _panels;

    public void Update(EditorState state, MouseState mouse, MouseState prevMouse,
                       SpriteFont font, Rectangle bounds, GameTime gameTime,
                       int screenW, int screenH)
    {
        _bounds = bounds;
        DistributeHeight(bounds);

        // Track mouse down on headers
        bool leftPressed = mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released;
        bool leftHeld = mouse.LeftButton == ButtonState.Pressed;
        bool leftReleased = mouse.LeftButton == ButtonState.Released && prevMouse.LeftButton == ButtonState.Pressed;

        // Header hover
        for (int i = 0; i < _panels.Count; i++)
            _panels[i].HeaderHovered = _panels[i].HeaderBounds.Contains(mouse.X, mouse.Y);

        // Handle header mouse down
        if (leftPressed)
        {
            _mouseDownHeaderIndex = -1;
            for (int i = 0; i < _panels.Count; i++)
            {
                if (_panels[i].HeaderBounds.Contains(mouse.X, mouse.Y))
                {
                    _mouseDownHeaderIndex = i;
                    _mouseDownY = mouse.Y;
                    break;
                }
            }
        }

        // Detect drag start
        if (leftHeld && _mouseDownHeaderIndex >= 0 && !_isDragging)
        {
            if (Math.Abs(mouse.Y - _mouseDownY) > 4)
            {
                _isDragging = true;
                _dragIndex = _mouseDownHeaderIndex;
                _dragMouseStartY = _mouseDownY;
                _dragStartBoundsY = _panels[_dragIndex].HeaderBounds.Y;
            }
        }

        // Handle release
        if (leftReleased)
        {
            if (_isDragging && _dragIndex >= 0)
            {
                // Determine insertion point
                int insertIdx = GetDragInsertIndex(mouse.Y);
                if (insertIdx != _dragIndex && insertIdx != _dragIndex + 1)
                {
                    var panel = _panels[_dragIndex];
                    _panels.RemoveAt(_dragIndex);
                    int adjustedIdx = insertIdx > _dragIndex ? insertIdx - 1 : insertIdx;
                    _panels.Insert(adjustedIdx, panel);
                }
            }
            else if (_mouseDownHeaderIndex >= 0 && _mouseDownHeaderIndex < _panels.Count)
            {
                // Click (not drag) — toggle collapse
                var panel = _panels[_mouseDownHeaderIndex];
                if (panel.HeaderBounds.Contains(mouse.X, mouse.Y))
                    panel.IsCollapsed = !panel.IsCollapsed;
            }

            _isDragging = false;
            _dragIndex = -1;
            _mouseDownHeaderIndex = -1;
        }

        // Update panel content (skip dragged panel, skip collapsed)
        for (int i = 0; i < _panels.Count; i++)
        {
            if (_panels[i].IsCollapsed) continue;
            if (_isDragging && i == _dragIndex) continue;

            _panels[i].UpdateContent(state, mouse, prevMouse, font, gameTime, screenW, screenH);
        }
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, EditorState state, Renderer renderer)
    {
        renderer.DrawRect(spriteBatch, _bounds, BackgroundColor);

        for (int i = 0; i < _panels.Count; i++)
        {
            if (_isDragging && i == _dragIndex) continue;

            _panels[i].DrawHeader(spriteBatch, font, renderer);
            if (!_panels[i].IsCollapsed)
                _panels[i].DrawContent(spriteBatch, font, state, renderer);
        }

        // Draw drag ghost and indicator on top
        if (_isDragging && _dragIndex >= 0 && _dragIndex < _panels.Count)
        {
            // Insertion indicator
            int insertIdx = GetDragInsertIndex(Mouse.GetState().Y);
            int indicatorY = GetInsertIndicatorY(insertIdx);
            renderer.DrawRect(spriteBatch,
                new Rectangle(_bounds.X + 4, indicatorY - 1, _bounds.Width - 8, 2),
                DragIndicatorColor);

            // Ghost header
            var dragPanel = _panels[_dragIndex];
            int ghostY = Mouse.GetState().Y - (_dragMouseStartY - _dragStartBoundsY);
            var savedBounds = dragPanel.HeaderBounds;
            dragPanel.HeaderBounds = new Rectangle(savedBounds.X, ghostY, savedBounds.Width, Panel.HeaderHeight);
            dragPanel.DrawHeader(spriteBatch, font, renderer);
            dragPanel.HeaderBounds = savedBounds;
        }
    }

    private void DistributeHeight(Rectangle bounds)
    {
        int availableHeight = bounds.Height;
        int totalHeaderHeight = _panels.Count * Panel.HeaderHeight;
        int contentSpace = availableHeight - totalHeaderHeight;

        // Calculate space needed by expanded fixed panels
        int fixedSpace = 0;
        int flexibleCount = 0;
        int flexibleMinTotal = 0;

        for (int i = 0; i < _panels.Count; i++)
        {
            if (_panels[i].IsCollapsed) continue;

            if (_panels[i].SizeMode == PanelSizeMode.Fixed)
                fixedSpace += _panels[i].PreferredHeight;
            else
            {
                flexibleCount++;
                flexibleMinTotal += _panels[i].PreferredHeight;
            }
        }

        int remainingForFlex = Math.Max(0, contentSpace - fixedSpace);
        int flexShare = flexibleCount > 0 ? remainingForFlex / flexibleCount : 0;

        // Assign bounds
        int y = bounds.Y;
        for (int i = 0; i < _panels.Count; i++)
        {
            var panel = _panels[i];
            panel.HeaderBounds = new Rectangle(bounds.X, y, bounds.Width, Panel.HeaderHeight);

            if (panel.IsCollapsed)
            {
                panel.ContentBounds = Rectangle.Empty;
                panel.Bounds = new Rectangle(bounds.X, y, bounds.Width, Panel.HeaderHeight);
                y += Panel.HeaderHeight;
            }
            else
            {
                int contentHeight;
                if (panel.SizeMode == PanelSizeMode.Fixed)
                    contentHeight = Math.Min(panel.PreferredHeight, contentSpace);
                else
                    contentHeight = Math.Max(panel.PreferredHeight, flexShare);

                panel.ContentBounds = new Rectangle(bounds.X, y + Panel.HeaderHeight, bounds.Width, contentHeight);
                panel.Bounds = new Rectangle(bounds.X, y, bounds.Width, Panel.HeaderHeight + contentHeight);
                y += Panel.HeaderHeight + contentHeight;
            }
        }
    }

    private int GetDragInsertIndex(int mouseY)
    {
        for (int i = 0; i < _panels.Count; i++)
        {
            if (i == _dragIndex) continue;
            int midY = _panels[i].HeaderBounds.Y + Panel.HeaderHeight / 2;
            if (mouseY < midY) return i;
        }
        return _panels.Count;
    }

    private int GetInsertIndicatorY(int insertIdx)
    {
        if (insertIdx <= 0) return _bounds.Y;
        if (insertIdx >= _panels.Count) return _panels[_panels.Count - 1].Bounds.Bottom;

        return _panels[insertIdx].HeaderBounds.Y;
    }

    // --- State persistence helpers ---

    public List<string> GetPanelOrder()
    {
        var order = new List<string>();
        foreach (var p in _panels) order.Add(p.Title);
        return order;
    }

    public List<string> GetCollapsedPanels()
    {
        var collapsed = new List<string>();
        foreach (var p in _panels)
            if (p.IsCollapsed) collapsed.Add(p.Title);
        return collapsed;
    }

    public void RestoreState(List<string> order, List<string> collapsed)
    {
        if (order != null && order.Count == _panels.Count)
        {
            var byTitle = new Dictionary<string, Panel>();
            foreach (var p in _panels) byTitle[p.Title] = p;

            var reordered = new List<Panel>();
            foreach (var title in order)
            {
                if (byTitle.TryGetValue(title, out var panel))
                    reordered.Add(panel);
            }

            if (reordered.Count == _panels.Count)
            {
                _panels.Clear();
                _panels.AddRange(reordered);
            }
        }

        if (collapsed != null)
        {
            var set = new HashSet<string>(collapsed);
            foreach (var p in _panels)
                p.IsCollapsed = set.Contains(p.Title);
        }
    }
}
