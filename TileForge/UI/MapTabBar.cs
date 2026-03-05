using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Editor;

namespace TileForge.UI;

public class MapTabBar
{
    public const int Height = LayoutConstants.MapTabBarHeight;

    // Signals (cleared each frame)
    public int WantsSelectTab { get; private set; } = -1;
    public bool WantsNewMap { get; private set; }
    public int WantsCloseTab { get; private set; } = -1;
    public int WantsRenameTab { get; private set; } = -1;
    public int WantsDuplicateTab { get; private set; } = -1;

    private struct TabRect
    {
        public Rectangle Tab;
        public Rectangle Close;
    }

    private readonly List<TabRect> _tabRects = new();
    private Rectangle _addButtonRect;
    private int _hoverTab = -1;
    private bool _hoverClose;
    private bool _hoverAdd;

    // Context menu
    private bool _contextMenuVisible;
    private int _contextMenuTabIndex = -1;
    private Rectangle _contextMenuBounds;
    private int _contextMenuHover = -1;
    private static readonly string[] ContextMenuItems = { "Rename", "Duplicate", "Delete" };
    private const int ContextMenuItemHeight = 22;
    private const int ContextMenuWidth = 100;

    // Double-click rename
    private double _lastClickTime;
    private int _lastClickTab = -1;
    private const double DoubleClickThreshold = 0.4;

    public void Update(EditorState state, MouseState mouse, MouseState prevMouse,
                       int screenWidth, SpriteFont font, GameTime gameTime)
    {
        WantsSelectTab = -1;
        WantsNewMap = false;
        WantsCloseTab = -1;
        WantsRenameTab = -1;
        WantsDuplicateTab = -1;

        if (state.IsPlayMode) return;

        var docs = state.MapDocuments;
        int activeIdx = state.ActiveMapIndex;
        int y = MenuBar.Height + ToolbarRibbon.Height;

        // Compute tab rectangles
        _tabRects.Clear();
        int x = 0;
        foreach (var doc in docs)
        {
            int textWidth = (int)font.MeasureString(doc.Name).X;
            int tabWidth = Math.Clamp(textWidth + 28, LayoutConstants.MapTabMinWidth, LayoutConstants.MapTabMaxWidth);
            var tabRect = new Rectangle(x, y, tabWidth, Height);
            var closeRect = new Rectangle(
                x + tabWidth - LayoutConstants.MapTabCloseSize - 4,
                y + (Height - LayoutConstants.MapTabCloseSize) / 2,
                LayoutConstants.MapTabCloseSize, LayoutConstants.MapTabCloseSize);
            _tabRects.Add(new TabRect { Tab = tabRect, Close = closeRect });
            x += tabWidth + 1; // 1px gap between tabs
        }

        _addButtonRect = new Rectangle(x + 4, y + 2, LayoutConstants.MapTabAddWidth, Height - 4);

        var mousePos = new Point(mouse.X, mouse.Y);
        bool leftPressed = mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released;
        bool rightPressed = mouse.RightButton == ButtonState.Pressed && prevMouse.RightButton == ButtonState.Released;

        // Context menu handling
        if (_contextMenuVisible)
        {
            _contextMenuHover = -1;
            if (_contextMenuBounds.Contains(mousePos))
            {
                _contextMenuHover = (mousePos.Y - _contextMenuBounds.Y) / ContextMenuItemHeight;
                if (_contextMenuHover >= ContextMenuItems.Length) _contextMenuHover = -1;
            }

            if (leftPressed)
            {
                if (_contextMenuHover >= 0 && _contextMenuTabIndex >= 0)
                {
                    switch (_contextMenuHover)
                    {
                        case 0: WantsRenameTab = _contextMenuTabIndex; break;
                        case 1: WantsDuplicateTab = _contextMenuTabIndex; break;
                        case 2: WantsCloseTab = _contextMenuTabIndex; break;
                    }
                }
                _contextMenuVisible = false;
                return;
            }

            if (rightPressed)
            {
                _contextMenuVisible = false;
                return;
            }
            return;
        }

        // Hover detection
        _hoverTab = -1;
        _hoverClose = false;
        _hoverAdd = false;

        for (int i = 0; i < _tabRects.Count; i++)
        {
            if (_tabRects[i].Tab.Contains(mousePos))
            {
                _hoverTab = i;
                if (docs.Count > 1 && _tabRects[i].Close.Contains(mousePos))
                    _hoverClose = true;
                break;
            }
        }

        if (_addButtonRect.Contains(mousePos))
            _hoverAdd = true;

        // Click handling
        if (leftPressed)
        {
            if (_hoverTab >= 0)
            {
                if (_hoverClose && docs.Count > 1)
                {
                    WantsCloseTab = _hoverTab;
                }
                else
                {
                    // Check double-click for rename
                    double now = gameTime.TotalGameTime.TotalSeconds;
                    if (_hoverTab == _lastClickTab && (now - _lastClickTime) < DoubleClickThreshold)
                    {
                        WantsRenameTab = _hoverTab;
                        _lastClickTab = -1;
                    }
                    else
                    {
                        WantsSelectTab = _hoverTab;
                        _lastClickTab = _hoverTab;
                        _lastClickTime = now;
                    }
                }
            }
            else if (_hoverAdd)
            {
                WantsNewMap = true;
            }
        }

        if (rightPressed && _hoverTab >= 0)
        {
            _contextMenuTabIndex = _hoverTab;
            _contextMenuVisible = true;
            _contextMenuBounds = new Rectangle(
                mousePos.X, mousePos.Y,
                ContextMenuWidth,
                ContextMenuItems.Length * ContextMenuItemHeight);
        }
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                     EditorState state, int screenWidth)
    {
        if (state.IsPlayMode) return;

        int y = MenuBar.Height + ToolbarRibbon.Height;
        var barRect = new Rectangle(0, y, screenWidth, Height);
        renderer.DrawRect(spriteBatch, barRect, LayoutConstants.MapTabBarBackground);

        var docs = state.MapDocuments;
        int activeIdx = state.ActiveMapIndex;

        // Draw tabs
        for (int i = 0; i < _tabRects.Count && i < docs.Count; i++)
        {
            var tr = _tabRects[i];
            bool isActive = i == activeIdx;
            bool isHover = i == _hoverTab && !isActive;

            // Tab background
            Color bg = isActive ? LayoutConstants.MapTabActive
                     : isHover ? LayoutConstants.MapTabHover
                     : LayoutConstants.MapTabInactive;
            renderer.DrawRect(spriteBatch, tr.Tab, bg);

            // Active tab indicator (bottom line)
            if (isActive)
            {
                var indicator = new Rectangle(tr.Tab.X, tr.Tab.Y + tr.Tab.Height - 2, tr.Tab.Width, 2);
                renderer.DrawRect(spriteBatch, indicator, new Color(100, 160, 255));
            }

            // Tab text
            Color textColor = isActive ? Color.White : LayoutConstants.MapTabDimTextColor;
            string name = docs[i].Name;
            var textSize = font.MeasureString(name);
            int maxTextWidth = tr.Tab.Width - 24; // leave room for close button
            if (textSize.X > maxTextWidth)
            {
                // Truncate with ellipsis
                while (name.Length > 1 && font.MeasureString(name + "...").X > maxTextWidth)
                    name = name[..^1];
                name += "...";
            }
            spriteBatch.DrawString(font, name,
                new Vector2(tr.Tab.X + 6, tr.Tab.Y + (Height - textSize.Y) / 2f),
                textColor);

            // Close button (only if multiple maps)
            if (docs.Count > 1 && (i == _hoverTab || isActive))
            {
                bool closeHover = i == _hoverTab && _hoverClose;
                Color closeColor = closeHover
                    ? LayoutConstants.MapTabCloseHoverColor
                    : LayoutConstants.MapTabCloseColor;
                // Draw X
                int cx = tr.Close.X + 2;
                int cy = tr.Close.Y + 2;
                int cs = LayoutConstants.MapTabCloseSize - 4;
                renderer.DrawLine(spriteBatch,
                    new Vector2(cx, cy), new Vector2(cx + cs, cy + cs), closeColor);
                renderer.DrawLine(spriteBatch,
                    new Vector2(cx + cs, cy), new Vector2(cx, cy + cs), closeColor);
            }
        }

        // Add button
        Color addBg = _hoverAdd ? LayoutConstants.MapTabAddHoverColor : LayoutConstants.MapTabAddColor;
        renderer.DrawRect(spriteBatch, _addButtonRect, new Color(addBg.R, addBg.G, addBg.B, (byte)40));
        // Draw + symbol
        string plus = "+";
        var plusSize = font.MeasureString(plus);
        spriteBatch.DrawString(font, plus,
            new Vector2(
                _addButtonRect.X + (_addButtonRect.Width - plusSize.X) / 2f,
                _addButtonRect.Y + (_addButtonRect.Height - plusSize.Y) / 2f),
            LayoutConstants.MapTabAddColor);

        // Context menu
        if (_contextMenuVisible)
        {
            DrawContextMenu(spriteBatch, font, renderer);
        }
    }

    private void DrawContextMenu(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer)
    {
        // Shadow
        var shadow = new Rectangle(_contextMenuBounds.X + 2, _contextMenuBounds.Y + 2,
                                    _contextMenuBounds.Width, _contextMenuBounds.Height);
        renderer.DrawRect(spriteBatch, shadow, new Color(0, 0, 0, 100));

        // Background
        renderer.DrawRect(spriteBatch, _contextMenuBounds, new Color(45, 45, 45));
        renderer.DrawRectOutline(spriteBatch, _contextMenuBounds, new Color(70, 70, 70), 1);

        for (int i = 0; i < ContextMenuItems.Length; i++)
        {
            var itemRect = new Rectangle(
                _contextMenuBounds.X, _contextMenuBounds.Y + i * ContextMenuItemHeight,
                _contextMenuBounds.Width, ContextMenuItemHeight);

            if (i == _contextMenuHover)
                renderer.DrawRect(spriteBatch, itemRect, new Color(60, 80, 120));

            Color textColor = (i == 2 && _contextMenuTabIndex >= 0)
                ? new Color(180, 80, 80) // Delete in red-ish
                : new Color(200, 200, 200);

            spriteBatch.DrawString(font, ContextMenuItems[i],
                new Vector2(itemRect.X + 8, itemRect.Y + 3), textColor);
        }
    }
}
