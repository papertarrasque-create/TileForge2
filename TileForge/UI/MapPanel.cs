using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Data;
using TileForge.Editor;

namespace TileForge.UI;

public class MapPanel : Panel
{
    public override string Title => "Map";
    public override PanelSizeMode SizeMode => PanelSizeMode.Flexible;
    public override int PreferredHeight => LayoutConstants.MapPanelPreferredHeight;

    // Colors
    private static readonly Color LayerHeaderBg = LayoutConstants.MapPanelLayerHeaderBg;
    private static readonly Color LayerHeaderActiveBg = LayoutConstants.MapPanelLayerHeaderActiveBg;
    private static readonly Color LayerHeaderHoverBg = LayoutConstants.MapPanelLayerHeaderHoverBg;
    private static readonly Color GroupItemBg = LayoutConstants.MapPanelGroupItemBg;
    private static readonly Color GroupSelectedBg = LayoutConstants.MapPanelGroupSelectedBg;
    private static readonly Color GroupHoverBg = LayoutConstants.MapPanelGroupHoverBg;
    private static readonly Color LabelColor = LayoutConstants.MapPanelLabelColor;
    private static readonly Color DimLabelColor = LayoutConstants.MapPanelDimLabelColor;
    private static readonly Color VisibleColor = LayoutConstants.MapPanelVisibleColor;
    private static readonly Color HiddenColor = LayoutConstants.MapPanelHiddenColor;
    private static readonly Color ArrowColor = LayoutConstants.MapPanelArrowColor;
    private static readonly Color BadgeColor = LayoutConstants.MapPanelBadgeColor;
    private static readonly Color AddButtonBg = LayoutConstants.MapPanelAddButtonBg;
    private static readonly Color AddButtonHoverBg = LayoutConstants.MapPanelAddButtonHoverBg;
    private static readonly Color HeaderTextColor = LayoutConstants.MapPanelHeaderTextColor;
    private static readonly Color DragIndicatorColor = LayoutConstants.MapPanelDragIndicatorColor;

    // Layout constants
    private const int LayerHeaderHeight = LayoutConstants.MapPanelLayerHeaderHeight;
    private const int GroupItemHeight = LayoutConstants.MapPanelGroupItemHeight;
    private const int ItemPadding = LayoutConstants.MapPanelItemPadding;
    private const int GroupIndent = LayoutConstants.MapPanelGroupIndent;
    private const int PreviewSize = LayoutConstants.MapPanelPreviewSize;
    private const int VisibilitySize = LayoutConstants.MapPanelVisibilitySize;
    private const int ArrowAreaWidth = LayoutConstants.MapPanelArrowAreaWidth;
    private const int VisibilityPadding = LayoutConstants.MapPanelVisibilityPadding;
    private const int AddGroupButtonHeight = LayoutConstants.MapPanelAddGroupButtonHeight;
    private const int AddLayerButtonHeight = LayoutConstants.MapPanelAddLayerButtonHeight;
    private const double DoubleClickThreshold = LayoutConstants.MapPanelDoubleClickThreshold;

    // Layout cache
    private enum EntryType { LayerHeader, GroupRow, AddGroupButton, AddLayerButton }

    private struct LayoutEntry
    {
        public EntryType Type;
        public Rectangle Rect;
        public int DataLayerIndex;
        public string LayerName;
        public TileGroup Group;
    }

    private readonly List<LayoutEntry> _entries = new();

    // Scroll
    private int _scrollOffset;

    // Hover
    private int _hoverEntryIndex = -1;

    // Layer section collapse state
    private readonly HashSet<string> _collapsedLayers = new();

    // Double-click tracking for group edit
    private double _lastClickTime;
    private string _lastClickGroupName;

    // Context menu
    private readonly ContextMenu _contextMenu = new("Edit", "Delete");
    private string _contextGroupName;

    // Layer drag-to-reorder
    private int _dragLayerDataIndex = -1;
    private int _dragMouseStartY;
    private bool _isDraggingLayer;
    private int _mouseDownLayerDataIndex = -1;
    private int _mouseDownY;

    // Group drag-to-move between layers
    private string _mouseDownGroupName;
    private int _mouseDownGroupY;
    private bool _isDraggingGroup;
    private string _dragGroupName;

    // Signals (cleared each frame)
    public (bool Requested, string LayerName) WantsNewGroupForLayer { get; private set; }
    public string WantsEditGroup { get; private set; }
    public string WantsDeleteGroup { get; private set; }
    public bool WantsNewLayer { get; private set; }
    public (int fromIndex, int toIndex)? PendingLayerReorder { get; private set; }

    // --- Collapse state persistence ---

    public List<string> GetCollapsedLayers() => _collapsedLayers.ToList();

    public void RestoreCollapsedLayers(List<string> collapsed)
    {
        _collapsedLayers.Clear();
        if (collapsed != null)
            foreach (var name in collapsed)
                _collapsedLayers.Add(name);
    }

    // --- Layout ---

    private void ComputeLayout(EditorState state)
    {
        _entries.Clear();
        if (state.Map == null) return;

        int y = ContentBounds.Y - _scrollOffset;
        int layerCount = state.Map.Layers.Count;

        // Iterate layers in reverse data order (highest index at top)
        for (int di = layerCount - 1; di >= 0; di--)
        {
            var layer = state.Map.Layers[di];

            // Layer header
            _entries.Add(new LayoutEntry
            {
                Type = EntryType.LayerHeader,
                Rect = new Rectangle(ContentBounds.X + 4, y, ContentBounds.Width - 8, LayerHeaderHeight),
                DataLayerIndex = di,
                LayerName = layer.Name,
            });
            y += LayerHeaderHeight + ItemPadding;

            // Groups under this layer (if expanded)
            if (!_collapsedLayers.Contains(layer.Name))
            {
                foreach (var group in state.Groups)
                {
                    if (group.LayerName != layer.Name) continue;

                    _entries.Add(new LayoutEntry
                    {
                        Type = EntryType.GroupRow,
                        Rect = new Rectangle(ContentBounds.X + 4 + GroupIndent, y,
                                             ContentBounds.Width - 8 - GroupIndent, GroupItemHeight),
                        DataLayerIndex = di,
                        LayerName = layer.Name,
                        Group = group,
                    });
                    y += GroupItemHeight + ItemPadding;
                }

                // "+ Add Group" button
                if (state.Sheet != null)
                {
                    _entries.Add(new LayoutEntry
                    {
                        Type = EntryType.AddGroupButton,
                        Rect = new Rectangle(ContentBounds.X + 4 + GroupIndent, y,
                                             ContentBounds.Width - 8 - GroupIndent, AddGroupButtonHeight),
                        DataLayerIndex = di,
                        LayerName = layer.Name,
                    });
                    y += AddGroupButtonHeight + ItemPadding;
                }
            }
        }

        // "+ Add Layer" button at the bottom
        _entries.Add(new LayoutEntry
        {
            Type = EntryType.AddLayerButton,
            Rect = new Rectangle(ContentBounds.X + 4, y, ContentBounds.Width - 8, AddLayerButtonHeight),
        });
    }

    // --- Update ---

    public override void UpdateContent(EditorState state, MouseState mouse, MouseState prevMouse,
                                        SpriteFont font, GameTime gameTime, int screenW, int screenH)
    {
        WantsNewGroupForLayer = default;
        WantsEditGroup = null;
        WantsDeleteGroup = null;
        WantsNewLayer = false;
        PendingLayerReorder = null;

        ComputeLayout(state);

        // Context menu priority
        if (_contextMenu.IsVisible)
        {
            int clicked = _contextMenu.Update(mouse, prevMouse);
            if (clicked == 0 && _contextGroupName != null)
                WantsEditGroup = _contextGroupName;
            else if (clicked == 1 && _contextGroupName != null)
                WantsDeleteGroup = _contextGroupName;
            return;
        }

        if (state.Map == null) return;

        bool leftPressed = mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released;
        bool leftHeld = mouse.LeftButton == ButtonState.Pressed;
        bool leftReleased = mouse.LeftButton == ButtonState.Released && prevMouse.LeftButton == ButtonState.Pressed;
        bool rightClick = mouse.RightButton == ButtonState.Pressed && prevMouse.RightButton == ButtonState.Released;

        // Handle ongoing layer drag
        if (_isDraggingLayer)
        {
            if (leftReleased)
            {
                int insertDataIndex = GetLayerDragInsertIndex(mouse.Y, state.Map.Layers.Count);
                if (insertDataIndex != _dragLayerDataIndex)
                    PendingLayerReorder = (_dragLayerDataIndex, insertDataIndex);

                _isDraggingLayer = false;
                _dragLayerDataIndex = -1;
                _mouseDownLayerDataIndex = -1;
            }
            return;
        }

        // Handle ongoing group drag
        if (_isDraggingGroup)
        {
            if (leftReleased)
            {
                string targetLayer = GetGroupDragTargetLayer(mouse.Y);
                if (targetLayer != null && _dragGroupName != null
                    && state.GroupsByName.TryGetValue(_dragGroupName, out var group)
                    && group.LayerName != targetLayer)
                {
                    group.LayerName = targetLayer;
                    state.ActiveLayerName = targetLayer;
                }
                _isDraggingGroup = false;
                _dragGroupName = null;
                _mouseDownGroupName = null;
            }
            return;
        }

        // Detect layer drag start
        if (leftHeld && _mouseDownLayerDataIndex >= 0)
        {
            if (Math.Abs(mouse.Y - _mouseDownY) > 4)
            {
                _isDraggingLayer = true;
                _dragLayerDataIndex = _mouseDownLayerDataIndex;
                _dragMouseStartY = _mouseDownY;
                return;
            }
        }

        // Detect group drag start
        if (leftHeld && _mouseDownGroupName != null)
        {
            if (Math.Abs(mouse.Y - _mouseDownGroupY) > 4)
            {
                _isDraggingGroup = true;
                _dragGroupName = _mouseDownGroupName;
                return;
            }
        }

        if (!ContentBounds.Contains(mouse.X, mouse.Y))
        {
            _hoverEntryIndex = -1;
            _mouseDownLayerDataIndex = -1;
            _mouseDownGroupName = null;
            return;
        }

        // Scroll
        int scrollDelta = mouse.ScrollWheelValue - prevMouse.ScrollWheelValue;
        if (scrollDelta != 0)
        {
            _scrollOffset -= scrollDelta > 0 ? 1 : -1;
            int totalHeight = GetTotalContentHeight();
            int maxScroll = Math.Max(0, totalHeight - ContentBounds.Height);
            _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
            ComputeLayout(state);
        }

        // Hit test
        _hoverEntryIndex = -1;
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (!entry.Rect.Contains(mouse.X, mouse.Y)) continue;
            if (entry.Rect.Bottom < ContentBounds.Y || entry.Rect.Y > ContentBounds.Bottom) continue;

            _hoverEntryIndex = i;

            switch (entry.Type)
            {
                case EntryType.LayerHeader:
                    HandleLayerHeaderInput(state, entry, mouse, leftPressed, rightClick, i);
                    break;

                case EntryType.GroupRow:
                    HandleGroupRowInput(state, entry, mouse, leftPressed, rightClick, font, gameTime, screenW, screenH);
                    break;

                case EntryType.AddGroupButton:
                    if (leftPressed)
                        WantsNewGroupForLayer = (true, entry.LayerName);
                    break;

                case EntryType.AddLayerButton:
                    if (leftPressed)
                        WantsNewLayer = true;
                    break;
            }

            break;
        }

        // Clear mouse down if released without drag
        if (leftReleased)
        {
            _mouseDownLayerDataIndex = -1;
            _mouseDownGroupName = null;
        }
    }

    public override void UpdateContent(EditorState state, MouseState mouse, MouseState prevMouse,
                                        InputEvent input, SpriteFont font, GameTime gameTime,
                                        int screenW, int screenH)
    {
        // Run the existing update logic
        UpdateContent(state, mouse, prevMouse, font, gameTime, screenW, screenH);

        // Context menu visible → consume all clicks (modal overlay)
        if (_contextMenu.IsVisible)
        {
            input.ConsumeClick();
            return;
        }

        // Consume clicks within content area for inter-panel consumption
        input.TryConsumeClick(ContentBounds);
    }

    private void HandleLayerHeaderInput(EditorState state, LayoutEntry entry,
                                         MouseState mouse, bool leftPressed, bool rightClick, int entryIndex)
    {
        if (!leftPressed) return;

        int relX = mouse.X - entry.Rect.X;

        if (relX < ArrowAreaWidth)
        {
            // Click on collapse arrow
            if (_collapsedLayers.Contains(entry.LayerName))
                _collapsedLayers.Remove(entry.LayerName);
            else
                _collapsedLayers.Add(entry.LayerName);
        }
        else
        {
            int visX = ArrowAreaWidth + VisibilityPadding;
            if (relX >= visX && relX < visX + VisibilitySize + 4)
            {
                // Click on visibility toggle
                state.Map.Layers[entry.DataLayerIndex].Visible = !state.Map.Layers[entry.DataLayerIndex].Visible;
            }
            else
            {
                // Click on name area: select layer + start potential drag
                state.ActiveLayerName = entry.LayerName;
                _mouseDownLayerDataIndex = entry.DataLayerIndex;
                _mouseDownY = mouse.Y;
            }
        }
    }

    private void HandleGroupRowInput(EditorState state, LayoutEntry entry,
                                      MouseState mouse, bool leftPressed, bool rightClick,
                                      SpriteFont font, GameTime gameTime, int screenW, int screenH)
    {
        if (leftPressed && entry.Group != null)
        {
            double now = gameTime.TotalGameTime.TotalSeconds;

            if (_lastClickGroupName == entry.Group.Name && (now - _lastClickTime) < DoubleClickThreshold)
            {
                WantsEditGroup = entry.Group.Name;
                _lastClickGroupName = null;
            }
            else
            {
                state.SelectedGroupName = entry.Group.Name;
                state.ActiveLayerName = entry.LayerName;
                _lastClickTime = now;
                _lastClickGroupName = entry.Group.Name;

                // Record for potential drag-to-move
                _mouseDownGroupName = entry.Group.Name;
                _mouseDownGroupY = mouse.Y;
            }
        }

        if (rightClick && entry.Group != null)
        {
            _contextGroupName = entry.Group.Name;
            _contextMenu.Show(mouse.X, mouse.Y, 0, 0, font, screenW, screenH);
        }
    }

    // --- Layer drag helpers ---

    private int GetLayerDragInsertIndex(int mouseY, int layerCount)
    {
        // Find the layer header entries and determine insertion based on mouse Y
        int fromDisplay = -1;

        var layerHeaders = new List<(int dataIndex, int centerY)>();
        foreach (var entry in _entries)
        {
            if (entry.Type == EntryType.LayerHeader)
                layerHeaders.Add((entry.DataLayerIndex, entry.Rect.Y + LayerHeaderHeight / 2));
        }

        // Determine display insert position
        int insertDisplay = layerHeaders.Count;
        for (int i = 0; i < layerHeaders.Count; i++)
        {
            if (mouseY < layerHeaders[i].centerY)
            {
                insertDisplay = i;
                break;
            }
        }

        // Convert display insertion to data index
        // Display order is reverse: display[0] = highest data index, display[last] = data index 0
        // Display insertion point i means "before display item i" = "after display item i-1"
        if (layerHeaders.Count == 0) return 0;

        // Find the from position in display order
        fromDisplay = -1;
        for (int i = 0; i < layerHeaders.Count; i++)
        {
            if (layerHeaders[i].dataIndex == _dragLayerDataIndex)
            {
                fromDisplay = i;
                break;
            }
        }

        int toDisplay = insertDisplay <= fromDisplay ? insertDisplay : insertDisplay - 1;
        toDisplay = Math.Clamp(toDisplay, 0, layerCount - 1);
        int toData = layerHeaders.Count > 0 && toDisplay < layerHeaders.Count
            ? layerHeaders[toDisplay].dataIndex
            : 0;

        return toData;
    }

    private string GetGroupDragTargetLayer(int mouseY)
    {
        // Find which layer section the mouse is in.
        // Entries are top-to-bottom; the last layer header at or above mouseY owns that region.
        string result = null;
        foreach (var entry in _entries)
        {
            if (entry.Type == EntryType.LayerHeader && mouseY >= entry.Rect.Y)
                result = entry.LayerName;
        }
        return result;
    }

    private int GetTotalContentHeight()
    {
        if (_entries.Count == 0) return 0;
        var last = _entries[_entries.Count - 1];
        return (last.Rect.Y + last.Rect.Height + _scrollOffset) - ContentBounds.Y + ItemPadding;
    }

    // --- Draw ---

    public override void DrawContent(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                                      Renderer renderer)
    {
        // Recompute layout to guard against stale entries (e.g. project loaded mid-frame
        // via dialog callback, before UpdateContent had a chance to run)
        ComputeLayout(state);

        if (state.Map == null)
        {
            string msg = "No map loaded";
            var msgSize = font.MeasureString(msg);
            spriteBatch.DrawString(font, msg,
                new Vector2(ContentBounds.X + (ContentBounds.Width - msgSize.X) / 2, ContentBounds.Y + 10),
                DimLabelColor);
            return;
        }

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];

            // Skip if outside visible area
            if (entry.Rect.Bottom < ContentBounds.Y || entry.Rect.Y > ContentBounds.Bottom)
                continue;

            // Skip dragged layer header + its children
            if (_isDraggingLayer && entry.DataLayerIndex == _dragLayerDataIndex)
                continue;

            // Skip dragged group row
            if (_isDraggingGroup && entry.Type == EntryType.GroupRow
                && entry.Group != null && entry.Group.Name == _dragGroupName)
                continue;

            bool isHovered = _hoverEntryIndex == i && !_isDraggingLayer && !_isDraggingGroup;

            switch (entry.Type)
            {
                case EntryType.LayerHeader:
                    DrawLayerHeader(spriteBatch, font, renderer, state, entry, isHovered);
                    break;

                case EntryType.GroupRow:
                    DrawGroupRow(spriteBatch, font, renderer, state, entry, isHovered);
                    break;

                case EntryType.AddGroupButton:
                    DrawAddButton(spriteBatch, font, renderer, entry.Rect, "+ Add Group", isHovered);
                    break;

                case EntryType.AddLayerButton:
                    DrawAddButton(spriteBatch, font, renderer, entry.Rect, "+ Add Layer", isHovered);
                    break;
            }
        }

        // Layer drag ghost + indicator
        if (_isDraggingLayer && _dragLayerDataIndex >= 0)
        {
            var mouseState = Mouse.GetState();

            // Find the dragged layer header entry for ghost
            foreach (var entry in _entries)
            {
                if (entry.Type == EntryType.LayerHeader && entry.DataLayerIndex == _dragLayerDataIndex)
                {
                    int ghostY = mouseState.Y - (LayerHeaderHeight / 2);
                    var ghostRect = new Rectangle(entry.Rect.X, ghostY, entry.Rect.Width, LayerHeaderHeight);
                    DrawLayerHeader(spriteBatch, font, renderer, state,
                        new LayoutEntry
                        {
                            Type = EntryType.LayerHeader,
                            Rect = ghostRect,
                            DataLayerIndex = entry.DataLayerIndex,
                            LayerName = entry.LayerName,
                        }, false);
                    break;
                }
            }

            // Insertion indicator
            int insertIdx = GetLayerDragInsertIndex(mouseState.Y, state.Map.Layers.Count);
            int indicatorY = GetDragIndicatorY(insertIdx);
            if (indicatorY >= 0)
            {
                renderer.DrawRect(spriteBatch,
                    new Rectangle(ContentBounds.X + 8, indicatorY - 1, ContentBounds.Width - 16, 2),
                    DragIndicatorColor);
            }
        }

        // Group drag ghost + target layer highlight
        if (_isDraggingGroup && _dragGroupName != null)
        {
            var mouseState = Mouse.GetState();

            // Highlight target layer header
            string targetLayer = GetGroupDragTargetLayer(mouseState.Y);
            if (targetLayer != null)
            {
                foreach (var entry in _entries)
                {
                    if (entry.Type == EntryType.LayerHeader && entry.LayerName == targetLayer)
                    {
                        renderer.DrawRectOutline(spriteBatch, entry.Rect, DragIndicatorColor, 2);
                        break;
                    }
                }
            }

            // Ghost of dragged group at mouse position
            if (state.GroupsByName.TryGetValue(_dragGroupName, out var dragGroup))
            {
                int ghostY = mouseState.Y - GroupItemHeight / 2;
                var ghostRect = new Rectangle(ContentBounds.X + 4 + GroupIndent, ghostY,
                                              ContentBounds.Width - 8 - GroupIndent, GroupItemHeight);
                DrawGroupRow(spriteBatch, font, renderer, state,
                    new LayoutEntry
                    {
                        Type = EntryType.GroupRow,
                        Rect = ghostRect,
                        Group = dragGroup,
                    }, false);
            }
        }

        // Context menu on top
        _contextMenu.Draw(spriteBatch, font, renderer);
    }

    private int GetDragIndicatorY(int targetDataIndex)
    {
        // Find the layer header for this data index and put indicator above it
        foreach (var entry in _entries)
        {
            if (entry.Type == EntryType.LayerHeader && entry.DataLayerIndex == targetDataIndex)
                return entry.Rect.Y;
        }
        // Fallback: below last entry
        if (_entries.Count > 0)
            return _entries[_entries.Count - 1].Rect.Bottom;
        return ContentBounds.Y;
    }

    private void DrawLayerHeader(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                                  EditorState state, LayoutEntry entry, bool isHovered)
    {
        var layer = state.Map.Layers[entry.DataLayerIndex];
        bool isActive = state.ActiveLayerName == layer.Name;
        bool isCollapsed = _collapsedLayers.Contains(layer.Name);

        var bgColor = isActive ? LayerHeaderActiveBg : (isHovered ? LayerHeaderHoverBg : LayerHeaderBg);
        renderer.DrawRect(spriteBatch, entry.Rect, bgColor);

        // Collapse arrow
        int arrowX = entry.Rect.X + 6;
        int arrowY = entry.Rect.Y + (LayerHeaderHeight - 8) / 2;
        DrawCollapseArrow(spriteBatch, renderer, arrowX, arrowY, isCollapsed);

        // Visibility indicator
        int visX = entry.Rect.X + ArrowAreaWidth + VisibilityPadding;
        int visY = entry.Rect.Y + (LayerHeaderHeight - VisibilitySize) / 2;

        if (layer.Visible)
            renderer.DrawRect(spriteBatch, new Rectangle(visX + 2, visY + 2, VisibilitySize - 4, VisibilitySize - 4), VisibleColor);
        else
            renderer.DrawRectOutline(spriteBatch, new Rectangle(visX + 2, visY + 2, VisibilitySize - 4, VisibilitySize - 4), HiddenColor, 1);

        // Layer name
        var nameColor = layer.Visible ? LabelColor : DimLabelColor;
        int nameX = visX + VisibilitySize + 8;
        spriteBatch.DrawString(font, layer.Name,
            new Vector2(nameX, entry.Rect.Y + (LayerHeaderHeight - font.LineSpacing) / 2),
            nameColor);
    }

    private void DrawGroupRow(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                               EditorState state, LayoutEntry entry, bool isHovered)
    {
        var group = entry.Group;
        bool isSelected = state.SelectedGroupName == group.Name;

        var bgColor = isSelected ? GroupSelectedBg : (isHovered ? GroupHoverBg : GroupItemBg);
        renderer.DrawRect(spriteBatch, entry.Rect, bgColor);

        // Sprite preview
        if (state.Sheet != null && group.Sprites.Count > 0)
        {
            var sprite = group.Sprites[0];
            var srcRect = state.Sheet.GetTileRect(sprite.Col, sprite.Row);
            var previewRect = new Rectangle(
                entry.Rect.X + 6,
                entry.Rect.Y + (GroupItemHeight - PreviewSize) / 2,
                PreviewSize, PreviewSize);
            spriteBatch.Draw(state.Sheet.Texture, previewRect, srcRect, Color.White);
        }

        // Group name
        spriteBatch.DrawString(font, group.Name,
            new Vector2(entry.Rect.X + 6 + PreviewSize + 6, entry.Rect.Y + 4),
            LabelColor);

        // Badges (S for solid, P for player — no T/E)
        string badge = "";
        if (group.IsSolid) badge += "S";
        if (group.IsPlayer) badge += "P";
        if (badge.Length > 0)
        {
            var badgePos = new Vector2(
                entry.Rect.Right - font.MeasureString(badge).X - 8,
                entry.Rect.Y + (GroupItemHeight - font.LineSpacing) / 2);
            spriteBatch.DrawString(font, badge, badgePos, BadgeColor);
        }
    }

    private void DrawAddButton(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                                Rectangle rect, string label, bool isHovered)
    {
        var bgColor = isHovered ? AddButtonHoverBg : AddButtonBg;
        renderer.DrawRect(spriteBatch, rect, bgColor);
        var textSize = font.MeasureString(label);
        spriteBatch.DrawString(font, label,
            new Vector2(rect.X + (rect.Width - textSize.X) / 2,
                         rect.Y + (rect.Height - textSize.Y) / 2),
            HeaderTextColor);
    }

    private static void DrawCollapseArrow(SpriteBatch spriteBatch, Renderer renderer,
                                           int x, int y, bool collapsed)
    {
        if (collapsed)
        {
            // Right-pointing arrow
            renderer.DrawRect(spriteBatch, new Rectangle(x, y, 2, 8), ArrowColor);
            renderer.DrawRect(spriteBatch, new Rectangle(x + 2, y + 1, 2, 6), ArrowColor);
            renderer.DrawRect(spriteBatch, new Rectangle(x + 4, y + 2, 2, 4), ArrowColor);
            renderer.DrawRect(spriteBatch, new Rectangle(x + 6, y + 3, 2, 2), ArrowColor);
        }
        else
        {
            // Down-pointing arrow
            renderer.DrawRect(spriteBatch, new Rectangle(x, y, 8, 2), ArrowColor);
            renderer.DrawRect(spriteBatch, new Rectangle(x + 1, y + 2, 6, 2), ArrowColor);
            renderer.DrawRect(spriteBatch, new Rectangle(x + 2, y + 4, 4, 2), ArrowColor);
            renderer.DrawRect(spriteBatch, new Rectangle(x + 3, y + 6, 2, 2), ArrowColor);
        }
    }
}
