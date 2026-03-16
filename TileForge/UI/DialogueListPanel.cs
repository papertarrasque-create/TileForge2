using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Editor;
using TileForge.Game;

namespace TileForge.UI;

/// <summary>
/// Sidebar list panel for dialogues in the Dialogue workspace.
/// Displays a scrollable list with selection, double-click to edit,
/// right-click context menu, and an Add button.
/// </summary>
public class DialogueListPanel
{
    // Colors (reuse dialogue panel constants)
    private static readonly Color Background = LayoutConstants.PanelDockBackground;
    private static readonly Color ItemBg = LayoutConstants.DialoguePanelItemBg;
    private static readonly Color SelectedBg = LayoutConstants.DialoguePanelSelectedBg;
    private static readonly Color HoverBg = LayoutConstants.DialoguePanelHoverBg;
    private static readonly Color LabelColor = LayoutConstants.DialoguePanelLabelColor;
    private static readonly Color AddButtonBg = LayoutConstants.DialoguePanelAddButtonBg;
    private static readonly Color AddButtonHoverBg = LayoutConstants.DialoguePanelAddButtonHoverBg;
    private static readonly Color HeaderBg = LayoutConstants.PanelHeaderColor;
    private static readonly Color HeaderTextColor = LayoutConstants.PanelHeaderTextColor;

    private const int ItemHeight = LayoutConstants.DialoguePanelItemHeight;
    private const int ItemPadding = LayoutConstants.DialoguePanelItemPadding;
    private const int AddButtonHeight = LayoutConstants.DialoguePanelAddButtonHeight;
    private const int HeaderHeight = LayoutConstants.PanelHeaderHeight;
    private const double DoubleClickThreshold = LayoutConstants.DialoguePanelDoubleClickThreshold;

    // State
    private int _selectedIndex = -1;
    private int _hoverIndex = -1;
    private int _scrollOffset;
    private double _lastClickTime;
    private int _lastClickIndex = -1;
    private Rectangle _bounds;

    // Context menu
    private readonly ContextMenu _contextMenu = new("Edit", "Delete");
    private int _contextIndex = -1;

    // Signals (cleared each frame)
    public bool WantsNew { get; private set; }
    public int WantsEditIndex { get; private set; } = -1;
    public int WantsDeleteIndex { get; private set; } = -1;
    public int SelectedIndex => _selectedIndex;

    // Layout entries
    private enum EntryType { Row, AddButton }
    private struct LayoutEntry
    {
        public EntryType Type;
        public Rectangle Rect;
        public int Index;
    }
    private readonly List<LayoutEntry> _entries = new();

    public void Update(EditorState state, MouseState mouse, MouseState prevMouse,
                       SpriteFont font, Rectangle bounds, GameTime gameTime,
                       int screenW, int screenH)
    {
        _bounds = bounds;
        WantsNew = false;
        WantsEditIndex = -1;
        WantsDeleteIndex = -1;

        var contentBounds = new Rectangle(bounds.X, bounds.Y + HeaderHeight,
                                           bounds.Width, bounds.Height - HeaderHeight);
        ComputeLayout(state.Dialogues, contentBounds);

        // Context menu priority
        if (_contextMenu.IsVisible)
        {
            int clicked = _contextMenu.Update(mouse, prevMouse);
            if (clicked == 0) WantsEditIndex = _contextIndex;
            else if (clicked == 1) WantsDeleteIndex = _contextIndex;
            return;
        }

        // Hover
        _hoverIndex = -1;
        if (contentBounds.Contains(mouse.X, mouse.Y))
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Rect.Contains(mouse.X, mouse.Y))
                {
                    _hoverIndex = i;
                    break;
                }
            }
        }

        // Scroll
        if (contentBounds.Contains(mouse.X, mouse.Y))
        {
            int scrollDelta = mouse.ScrollWheelValue - prevMouse.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                _scrollOffset -= scrollDelta > 0 ? 1 : -1;
                int totalH = GetTotalContentHeight(contentBounds);
                int maxScroll = Math.Max(0, totalH - contentBounds.Height);
                _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
                ComputeLayout(state.Dialogues, contentBounds);
            }
        }

        // Clicks
        bool leftClick = mouse.LeftButton == ButtonState.Pressed
                      && prevMouse.LeftButton == ButtonState.Released;
        bool rightClick = mouse.RightButton == ButtonState.Pressed
                       && prevMouse.RightButton == ButtonState.Released;

        if (_hoverIndex >= 0 && _hoverIndex < _entries.Count)
        {
            var entry = _entries[_hoverIndex];
            if (leftClick)
            {
                if (entry.Type == EntryType.Row)
                {
                    double now = gameTime.TotalGameTime.TotalSeconds;
                    if (entry.Index == _lastClickIndex
                        && now - _lastClickTime < DoubleClickThreshold)
                    {
                        WantsEditIndex = entry.Index;
                        _lastClickIndex = -1;
                    }
                    else
                    {
                        _selectedIndex = entry.Index;
                        _lastClickIndex = entry.Index;
                        _lastClickTime = now;
                    }
                }
                else if (entry.Type == EntryType.AddButton)
                {
                    WantsNew = true;
                }
            }
            else if (rightClick && entry.Type == EntryType.Row)
            {
                _contextIndex = entry.Index;
                _contextMenu.Show(mouse.X, mouse.Y, 0, 0, font, screenW, screenH);
            }
        }

        // Clamp
        if (_selectedIndex >= state.Dialogues.Count)
            _selectedIndex = state.Dialogues.Count - 1;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                     Renderer renderer, Rectangle bounds)
    {
        // Background
        renderer.DrawRect(spriteBatch, bounds, Background);

        // Header
        var headerRect = new Rectangle(bounds.X, bounds.Y, bounds.Width, HeaderHeight);
        renderer.DrawRect(spriteBatch, headerRect, HeaderBg);
        spriteBatch.DrawString(font, "Dialogues",
            new Vector2(bounds.X + 8, bounds.Y + (HeaderHeight - font.LineSpacing) / 2),
            HeaderTextColor);

        // Content
        var contentBounds = new Rectangle(bounds.X, bounds.Y + HeaderHeight,
                                           bounds.Width, bounds.Height - HeaderHeight);

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry.Rect.Bottom < contentBounds.Y || entry.Rect.Y > contentBounds.Bottom)
                continue;

            bool isHovered = _hoverIndex == i;
            if (entry.Type == EntryType.Row)
                DrawRow(spriteBatch, font, renderer, state.Dialogues, entry, isHovered);
            else
                DrawAddButton(spriteBatch, font, renderer, entry.Rect, "+ Add Dialogue", isHovered);
        }

        _contextMenu.Draw(spriteBatch, font, renderer);
    }

    private void DrawRow(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                         List<DialogueData> dialogues, LayoutEntry entry, bool isHovered)
    {
        bool isSelected = entry.Index == _selectedIndex;
        var bgColor = isSelected ? SelectedBg : isHovered ? HoverBg : ItemBg;
        renderer.DrawRect(spriteBatch, entry.Rect, bgColor);

        if (entry.Index < dialogues.Count)
        {
            string label = dialogues[entry.Index].Id ?? "(unnamed)";
            int maxW = entry.Rect.Width - 12;
            label = TextUtils.TruncateToFit(font, label, maxW);
            int textY = entry.Rect.Y + (entry.Rect.Height - font.LineSpacing) / 2;
            spriteBatch.DrawString(font, label, new Vector2(entry.Rect.X + 6, textY), LabelColor);
        }
    }

    private static void DrawAddButton(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                                       Rectangle rect, string label, bool isHovered)
    {
        renderer.DrawRect(spriteBatch, rect, isHovered ? AddButtonHoverBg : AddButtonBg);
        var size = font.MeasureString(label);
        spriteBatch.DrawString(font, label,
            new Vector2(rect.X + (rect.Width - size.X) / 2, rect.Y + (rect.Height - size.Y) / 2),
            Color.White);
    }

    private void ComputeLayout(List<DialogueData> dialogues, Rectangle contentBounds)
    {
        _entries.Clear();
        int y = contentBounds.Y - _scrollOffset;

        for (int i = 0; i < dialogues.Count; i++)
        {
            _entries.Add(new LayoutEntry
            {
                Type = EntryType.Row,
                Rect = new Rectangle(contentBounds.X + 2, y, contentBounds.Width - 4, ItemHeight),
                Index = i,
            });
            y += ItemHeight + ItemPadding;
        }

        y += 2;
        _entries.Add(new LayoutEntry
        {
            Type = EntryType.AddButton,
            Rect = new Rectangle(contentBounds.X + 2, y, contentBounds.Width - 4, AddButtonHeight),
            Index = -1,
        });
    }

    private int GetTotalContentHeight(Rectangle contentBounds)
    {
        if (_entries.Count == 0) return 0;
        var last = _entries[^1];
        return (last.Rect.Y + last.Rect.Height + _scrollOffset) - contentBounds.Y + ItemPadding;
    }
}
