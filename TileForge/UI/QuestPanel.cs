using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Editor;

namespace TileForge.UI;

public class QuestPanel : Panel
{
    public override string Title => "Quests";
    public override PanelSizeMode SizeMode => PanelSizeMode.Fixed;
    public override int PreferredHeight => LayoutConstants.QuestPanelPreferredHeight;

    // Colors
    private static readonly Color ItemBg = LayoutConstants.QuestPanelItemBg;
    private static readonly Color SelectedBg = LayoutConstants.QuestPanelSelectedBg;
    private static readonly Color HoverBg = LayoutConstants.QuestPanelHoverBg;
    private static readonly Color LabelColor = LayoutConstants.QuestPanelLabelColor;
    private static readonly Color AddButtonBg = LayoutConstants.QuestPanelAddButtonBg;
    private static readonly Color AddButtonHoverBg = LayoutConstants.QuestPanelAddButtonHoverBg;

    // Layout constants
    private const int ItemHeight = LayoutConstants.QuestPanelItemHeight;
    private const int ItemPadding = LayoutConstants.QuestPanelItemPadding;
    private const int AddButtonHeight = LayoutConstants.QuestPanelAddButtonHeight;
    private const double DoubleClickThreshold = LayoutConstants.QuestPanelDoubleClickThreshold;

    // Layout cache
    private enum EntryType { QuestRow, AddQuestButton }

    private struct LayoutEntry
    {
        public EntryType Type;
        public Rectangle Rect;
        public int QuestIndex;
    }

    private readonly List<LayoutEntry> _entries = new();

    // Scroll
    private int _scrollOffset;

    // Hover
    private int _hoverEntryIndex = -1;

    // Double-click tracking
    private double _lastClickTime;
    private int _lastClickQuestIndex = -1;

    // Context menu
    private readonly ContextMenu _contextMenu = new("Edit", "Delete");
    private int _contextQuestIndex = -1;

    // Selected quest index
    private int _selectedIndex = -1;

    // Signals (cleared each frame)
    public bool WantsNewQuest { get; private set; }
    public int WantsEditQuestIndex { get; private set; } = -1;
    public int WantsDeleteQuestIndex { get; private set; } = -1;

    public override void UpdateContent(EditorState state, MouseState mouse, MouseState prevMouse,
                                        SpriteFont font, GameTime gameTime, int screenW, int screenH)
    {
        // Clear signals
        WantsNewQuest = false;
        WantsEditQuestIndex = -1;
        WantsDeleteQuestIndex = -1;

        ComputeLayout(state);

        // Context menu takes priority
        if (_contextMenu.IsVisible)
        {
            int clicked = _contextMenu.Update(mouse, prevMouse);
            if (clicked == 0) // Edit
                WantsEditQuestIndex = _contextQuestIndex;
            else if (clicked == 1) // Delete
                WantsDeleteQuestIndex = _contextQuestIndex;
            return;
        }

        // Hit test for hover
        _hoverEntryIndex = -1;
        if (ContentBounds.Contains(mouse.X, mouse.Y))
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Rect.Contains(mouse.X, mouse.Y))
                {
                    _hoverEntryIndex = i;
                    break;
                }
            }
        }

        // Scroll
        if (ContentBounds.Contains(mouse.X, mouse.Y))
        {
            int scrollDelta = mouse.ScrollWheelValue - prevMouse.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                _scrollOffset -= scrollDelta > 0 ? 1 : -1;
                int totalHeight = GetTotalContentHeight();
                int maxScroll = Math.Max(0, totalHeight - ContentBounds.Height);
                _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
                ComputeLayout(state);
            }
        }

        // Click handling
        bool leftClick = mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released;
        bool rightClick = mouse.RightButton == ButtonState.Pressed && prevMouse.RightButton == ButtonState.Released;

        if (_hoverEntryIndex >= 0)
        {
            var entry = _entries[_hoverEntryIndex];

            if (leftClick)
            {
                switch (entry.Type)
                {
                    case EntryType.QuestRow:
                        // Double-click detection
                        double now = gameTime.TotalGameTime.TotalSeconds;
                        if (entry.QuestIndex == _lastClickQuestIndex &&
                            now - _lastClickTime < DoubleClickThreshold)
                        {
                            WantsEditQuestIndex = entry.QuestIndex;
                            _lastClickQuestIndex = -1;
                        }
                        else
                        {
                            _selectedIndex = entry.QuestIndex;
                            _lastClickQuestIndex = entry.QuestIndex;
                            _lastClickTime = now;
                        }
                        break;

                    case EntryType.AddQuestButton:
                        WantsNewQuest = true;
                        break;
                }
            }
            else if (rightClick && entry.Type == EntryType.QuestRow)
            {
                _contextQuestIndex = entry.QuestIndex;
                _contextMenu.Show(mouse.X, mouse.Y, 0, 0, font, screenW, screenH);
            }
        }

        // Clamp selected index
        if (_selectedIndex >= state.Quests.Count)
            _selectedIndex = state.Quests.Count - 1;
    }

    public override void DrawContent(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                                      Renderer renderer)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];

            // Skip if outside visible area
            if (entry.Rect.Bottom < ContentBounds.Y || entry.Rect.Y > ContentBounds.Bottom)
                continue;

            bool isHovered = _hoverEntryIndex == i;

            switch (entry.Type)
            {
                case EntryType.QuestRow:
                    DrawQuestRow(spriteBatch, font, renderer, state, entry, isHovered);
                    break;
                case EntryType.AddQuestButton:
                    DrawAddButton(spriteBatch, font, renderer, entry.Rect, "+ Add Quest", isHovered);
                    break;
            }
        }

        _contextMenu.Draw(spriteBatch, font, renderer);
    }

    private void DrawQuestRow(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                               EditorState state, LayoutEntry entry, bool isHovered)
    {
        bool isSelected = entry.QuestIndex == _selectedIndex;
        var bgColor = isSelected ? SelectedBg : isHovered ? HoverBg : ItemBg;
        renderer.DrawRect(spriteBatch, entry.Rect, bgColor);

        if (entry.QuestIndex < state.Quests.Count)
        {
            var quest = state.Quests[entry.QuestIndex];
            string label = quest.Name ?? quest.Id ?? "(unnamed)";

            // Truncate label to fit panel width
            int maxWidth = entry.Rect.Width - 12;
            while (label.Length > 3 && font.MeasureString(label).X > maxWidth)
                label = label[..^4] + "...";

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

    private void ComputeLayout(EditorState state)
    {
        _entries.Clear();
        int y = ContentBounds.Y - _scrollOffset;

        for (int i = 0; i < state.Quests.Count; i++)
        {
            _entries.Add(new LayoutEntry
            {
                Type = EntryType.QuestRow,
                Rect = new Rectangle(ContentBounds.X + 2, y, ContentBounds.Width - 4, ItemHeight),
                QuestIndex = i,
            });
            y += ItemHeight + ItemPadding;
        }

        // Add button at bottom
        y += 2;
        _entries.Add(new LayoutEntry
        {
            Type = EntryType.AddQuestButton,
            Rect = new Rectangle(ContentBounds.X + 2, y, ContentBounds.Width - 4, AddButtonHeight),
            QuestIndex = -1,
        });
    }

    private int GetTotalContentHeight()
    {
        if (_entries.Count == 0) return 0;
        var last = _entries[^1];
        return (last.Rect.Y + last.Rect.Height + _scrollOffset) - ContentBounds.Y + ItemPadding;
    }
}
