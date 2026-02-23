using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Data;
using TileForge.Editor;

namespace TileForge.UI;

public class TilePalettePanel : Panel
{
    public override string Title => "Tileset";
    public override PanelSizeMode SizeMode => PanelSizeMode.Flexible;
    public override int PreferredHeight => LayoutConstants.TilePalettePanelPreferredHeight;

    private static readonly Color SelectedHighlight = LayoutConstants.TilePaletteSelectedHighlight;
    private static readonly Color HoverOutline = LayoutConstants.TilePaletteHoverOutline;
    private static readonly Color UngroupedHint = LayoutConstants.TilePaletteUngroupedHint;
    private static readonly Color Background = LayoutConstants.TilePaletteBackground;

    private int _scrollOffset;
    private int _hoverCol = -1;
    private int _hoverRow = -1;
    private double _lastClickTime;
    private int _lastClickCol = -1;
    private int _lastClickRow = -1;

    // Signal: set when double-click requests GroupEditor for a group
    public string WantsEditGroup { get; private set; }

    // Sprite-to-group lookup, rebuilt each frame
    internal Dictionary<(int col, int row), TileGroup> SpriteGroupIndex { get; } = new();

    public override void UpdateContent(EditorState state, MouseState mouse, MouseState prevMouse,
                                        SpriteFont font, GameTime gameTime, int screenW, int screenH)
    {
        WantsEditGroup = null;
        if (state.Sheet == null) return;

        RebuildGroupIndex(state);

        int tileDisplaySize = CalculateTileDisplaySize(state.Sheet.Cols);
        int totalHeight = state.Sheet.Rows * tileDisplaySize;
        int visibleHeight = ContentBounds.Height;

        // Scroll with mouse wheel
        if (ContentBounds.Contains(mouse.X, mouse.Y))
        {
            int scrollDelta = prevMouse.ScrollWheelValue - mouse.ScrollWheelValue;
            _scrollOffset = MathHelper.Clamp(_scrollOffset + scrollDelta / 4, 0,
                System.Math.Max(0, totalHeight - visibleHeight));

            // Hit test
            int relX = mouse.X - ContentBounds.X;
            int relY = mouse.Y - ContentBounds.Y + _scrollOffset;
            _hoverCol = relX / tileDisplaySize;
            _hoverRow = relY / tileDisplaySize;

            if (_hoverCol >= state.Sheet.Cols || _hoverRow >= state.Sheet.Rows
                || _hoverCol < 0 || _hoverRow < 0)
            {
                _hoverCol = -1;
                _hoverRow = -1;
            }

            // Click handling
            bool leftPressed = mouse.LeftButton == ButtonState.Pressed
                               && prevMouse.LeftButton == ButtonState.Released;
            if (leftPressed && _hoverCol >= 0 && _hoverRow >= 0)
            {
                double now = gameTime.TotalGameTime.TotalSeconds;

                if (_lastClickCol == _hoverCol && _lastClickRow == _hoverRow
                    && (now - _lastClickTime) < LayoutConstants.TilePaletteDoubleClickThreshold)
                {
                    // Double-click: open GroupEditor
                    if (SpriteGroupIndex.TryGetValue((_hoverCol, _hoverRow), out var group))
                        WantsEditGroup = group.Name;
                    _lastClickCol = -1;
                }
                else
                {
                    // Single click: select group containing this sprite
                    if (SpriteGroupIndex.TryGetValue((_hoverCol, _hoverRow), out var group))
                    {
                        state.SelectedGroupName = group.Name;
                        if (group.LayerName != null)
                            state.ActiveLayerName = group.LayerName;
                    }
                    _lastClickTime = now;
                    _lastClickCol = _hoverCol;
                    _lastClickRow = _hoverRow;
                }
            }
        }
        else
        {
            _hoverCol = -1;
            _hoverRow = -1;
        }
    }

    public override void DrawContent(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                                      Renderer renderer)
    {
        renderer.DrawRect(spriteBatch, ContentBounds, Background);

        if (state.Sheet == null)
        {
            spriteBatch.DrawString(font, "No tileset loaded",
                new Vector2(ContentBounds.X + 8, ContentBounds.Y + 8),
                LayoutConstants.MapPanelDimLabelColor);
            return;
        }

        int tileDisplaySize = CalculateTileDisplaySize(state.Sheet.Cols);

        for (int row = 0; row < state.Sheet.Rows; row++)
        {
            int drawY = ContentBounds.Y + row * tileDisplaySize - _scrollOffset;

            // Skip rows outside visible area
            if (drawY + tileDisplaySize < ContentBounds.Y) continue;
            if (drawY > ContentBounds.Bottom) break;

            for (int col = 0; col < state.Sheet.Cols; col++)
            {
                int drawX = ContentBounds.X + col * tileDisplaySize;
                var destRect = new Rectangle(drawX, drawY, tileDisplaySize, tileDisplaySize);

                // Skip if fully above or below content area
                if (destRect.Bottom < ContentBounds.Y || destRect.Y > ContentBounds.Bottom)
                    continue;

                // Draw the tile sprite
                var srcRect = state.Sheet.GetTileRect(col, row);
                spriteBatch.Draw(state.Sheet.Texture, destRect, srcRect, Color.White);

                // Highlight: belongs to selected group
                bool belongsToSelected = false;
                var selectedGroup = state.SelectedGroup;
                if (selectedGroup != null)
                {
                    foreach (var s in selectedGroup.Sprites)
                    {
                        if (s.Col == col && s.Row == row) { belongsToSelected = true; break; }
                    }
                }

                if (belongsToSelected)
                {
                    renderer.DrawRectOutline(spriteBatch, destRect, SelectedHighlight, 2);
                }
                else if (!SpriteGroupIndex.ContainsKey((col, row)))
                {
                    // Subtle dot for ungrouped sprites
                    renderer.DrawRect(spriteBatch,
                        new Rectangle(drawX + tileDisplaySize - 4, drawY + tileDisplaySize - 4, 3, 3),
                        UngroupedHint);
                }

                // Hover highlight
                if (col == _hoverCol && row == _hoverRow)
                {
                    renderer.DrawRectOutline(spriteBatch, destRect, HoverOutline, 1);
                }
            }
        }
    }

    internal void RebuildGroupIndex(EditorState state)
    {
        SpriteGroupIndex.Clear();
        foreach (var group in state.Groups)
        {
            foreach (var sprite in group.Sprites)
            {
                SpriteGroupIndex.TryAdd((sprite.Col, sprite.Row), group);
            }
        }
    }

    internal int CalculateTileDisplaySize(int sheetCols)
    {
        if (sheetCols <= 0) return LayoutConstants.TilePaletteMinTileDisplaySize;
        int size = ContentBounds.Width / sheetCols;
        return System.Math.Max(size, LayoutConstants.TilePaletteMinTileDisplaySize);
    }
}
