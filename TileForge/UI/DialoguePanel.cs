using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Editor;
using TileForge.Game;

namespace TileForge.UI;

public class DialoguePanel : Panel
{
    public override string Title => "Dialogues";
    public override PanelSizeMode SizeMode => PanelSizeMode.Fixed;
    public override int PreferredHeight => LayoutConstants.DialoguePanelPreferredHeight;

    // Colors
    private static readonly Color ItemBg = LayoutConstants.DialoguePanelItemBg;
    private static readonly Color SelectedBg = LayoutConstants.DialoguePanelSelectedBg;
    private static readonly Color HoverBg = LayoutConstants.DialoguePanelHoverBg;
    private static readonly Color LabelColor = LayoutConstants.DialoguePanelLabelColor;
    private static readonly Color AddButtonBg = LayoutConstants.DialoguePanelAddButtonBg;
    private static readonly Color AddButtonHoverBg = LayoutConstants.DialoguePanelAddButtonHoverBg;

    // Layout constants
    private const int ItemHeight = LayoutConstants.DialoguePanelItemHeight;
    private const int ItemPadding = LayoutConstants.DialoguePanelItemPadding;
    private const int AddButtonHeight = LayoutConstants.DialoguePanelAddButtonHeight;
    private const double DoubleClickThreshold = LayoutConstants.DialoguePanelDoubleClickThreshold;

    // Layout cache
    private enum EntryType { DialogueRow, AddDialogueButton }

    private struct LayoutEntry
    {
        public EntryType Type;
        public Rectangle Rect;
        public int DialogueIndex;
    }

    private readonly List<LayoutEntry> _entries = new();

    // Scroll
    private int _scrollOffset;

    // Hover
    private int _hoverEntryIndex = -1;

    // Double-click tracking
    private double _lastClickTime;
    private int _lastClickDialogueIndex = -1;

    // Context menu
    private readonly ContextMenu _contextMenu = new("Edit", "Delete");
    private int _contextDialogueIndex = -1;

    // Selected dialogue index
    private int _selectedIndex = -1;

    // Signals (cleared each frame)
    public bool WantsNewDialogue { get; private set; }
    public int WantsEditDialogueIndex { get; private set; } = -1;
    public int WantsDeleteDialogueIndex { get; private set; } = -1;

    public override void UpdateContent(EditorState state, MouseState mouse, MouseState prevMouse,
                                        SpriteFont font, GameTime gameTime, int screenW, int screenH)
    {
        // Clear signals
        WantsNewDialogue = false;
        WantsEditDialogueIndex = -1;
        WantsDeleteDialogueIndex = -1;

        ComputeLayout(state);

        // Context menu takes priority
        if (_contextMenu.IsVisible)
        {
            int clicked = _contextMenu.Update(mouse, prevMouse);
            if (clicked == 0) // Edit
                WantsEditDialogueIndex = _contextDialogueIndex;
            else if (clicked == 1) // Delete
                WantsDeleteDialogueIndex = _contextDialogueIndex;
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
                    case EntryType.DialogueRow:
                        // Double-click detection
                        double now = gameTime.TotalGameTime.TotalSeconds;
                        if (entry.DialogueIndex == _lastClickDialogueIndex &&
                            now - _lastClickTime < DoubleClickThreshold)
                        {
                            WantsEditDialogueIndex = entry.DialogueIndex;
                            _lastClickDialogueIndex = -1;
                        }
                        else
                        {
                            _selectedIndex = entry.DialogueIndex;
                            _lastClickDialogueIndex = entry.DialogueIndex;
                            _lastClickTime = now;
                        }
                        break;

                    case EntryType.AddDialogueButton:
                        WantsNewDialogue = true;
                        break;
                }
            }
            else if (rightClick && entry.Type == EntryType.DialogueRow)
            {
                _contextDialogueIndex = entry.DialogueIndex;
                _contextMenu.Show(mouse.X, mouse.Y, 0, 0, font, screenW, screenH);
            }
        }

        // Clamp selected index
        if (_selectedIndex >= state.Dialogues.Count)
            _selectedIndex = state.Dialogues.Count - 1;
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
                case EntryType.DialogueRow:
                    DrawDialogueRow(spriteBatch, font, renderer, state, entry, isHovered);
                    break;
                case EntryType.AddDialogueButton:
                    DrawAddButton(spriteBatch, font, renderer, entry.Rect, "+ Add Dialogue", isHovered);
                    break;
            }
        }

        _contextMenu.Draw(spriteBatch, font, renderer);
    }

    private void DrawDialogueRow(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                               EditorState state, LayoutEntry entry, bool isHovered)
    {
        bool isSelected = entry.DialogueIndex == _selectedIndex;
        var bgColor = isSelected ? SelectedBg : isHovered ? HoverBg : ItemBg;
        renderer.DrawRect(spriteBatch, entry.Rect, bgColor);

        if (entry.DialogueIndex < state.Dialogues.Count)
        {
            var dialogue = state.Dialogues[entry.DialogueIndex];
            string label = dialogue.Id ?? "(unnamed)";

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

        for (int i = 0; i < state.Dialogues.Count; i++)
        {
            _entries.Add(new LayoutEntry
            {
                Type = EntryType.DialogueRow,
                Rect = new Rectangle(ContentBounds.X + 2, y, ContentBounds.Width - 4, ItemHeight),
                DialogueIndex = i,
            });
            y += ItemHeight + ItemPadding;
        }

        // Add button at bottom
        y += 2;
        _entries.Add(new LayoutEntry
        {
            Type = EntryType.AddDialogueButton,
            Rect = new Rectangle(ContentBounds.X + 2, y, ContentBounds.Width - 4, AddButtonHeight),
            DialogueIndex = -1,
        });
    }

    private int GetTotalContentHeight()
    {
        if (_entries.Count == 0) return 0;
        var last = _entries[^1];
        return (last.Rect.Y + last.Rect.Height + _scrollOffset) - ContentBounds.Y + ItemPadding;
    }
}
