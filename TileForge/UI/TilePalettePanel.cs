using System.Collections.Generic;
using System.Linq;
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
    private static readonly Color UngroupedSelectedHighlight = LayoutConstants.TilePaletteUngroupedSelectedHighlight;
    private static readonly Color Background = LayoutConstants.TilePaletteBackground;

    private int _scrollOffsetY;
    private int _scrollOffsetX;
    private float _zoom = LayoutConstants.TilePaletteDefaultZoom;
    private int _hoverCol = -1;
    private int _hoverRow = -1;
    private double _lastClickTime;
    private int _lastClickCol = -1;
    private int _lastClickRow = -1;

    // Zoom button hit rects (computed during Draw, tested during Update)
    private Rectangle _zoomInRect;
    private Rectangle _zoomOutRect;

    // Context menu for grouped sprites
    private readonly ContextMenu _contextMenu = new("Edit", "Delete");
    private string _contextGroupName;

    // Multi-selection for ungrouped sprites
    private readonly Selection _ungroupedSelection = new();

    // Signals
    public string WantsEditGroup { get; private set; }
    public string WantsDeleteGroup { get; private set; }
    public IReadOnlyCollection<(int col, int row)> WantsNewGroupWithSprites { get; private set; }

    // Sprite-to-group lookup, rebuilt each frame
    internal Dictionary<(int col, int row), TileGroup> SpriteGroupIndex { get; } = new();

    // Test accessors
    internal float Zoom => _zoom;
    internal int ScrollOffsetX => _scrollOffsetX;
    internal int ScrollOffsetY => _scrollOffsetY;
    internal Selection UngroupedSelection => _ungroupedSelection;

    internal void SetZoom(float zoom)
    {
        _zoom = MathHelper.Clamp(zoom,
            LayoutConstants.TilePaletteMinZoom,
            LayoutConstants.TilePaletteMaxZoom);
    }

    private Rectangle GetTileArea()
    {
        int barH = LayoutConstants.TilePaletteZoomBarHeight;
        return new Rectangle(ContentBounds.X, ContentBounds.Y + barH,
                             ContentBounds.Width, System.Math.Max(0, ContentBounds.Height - barH));
    }

    public override void UpdateContent(EditorState state, MouseState mouse, MouseState prevMouse,
                                        SpriteFont font, GameTime gameTime, int screenW, int screenH)
    {
        // Delegate to InputEvent-aware overload with a throwaway InputEvent
        UpdateContent(state, mouse, prevMouse, new InputEvent(mouse, prevMouse),
                      font, gameTime, screenW, screenH);
    }

    public override void UpdateContent(EditorState state, MouseState mouse, MouseState prevMouse,
                                        InputEvent input, SpriteFont font, GameTime gameTime,
                                        int screenW, int screenH)
    {
        WantsEditGroup = null;
        WantsDeleteGroup = null;
        WantsNewGroupWithSprites = null;

        // Context menu takes priority (modal overlay)
        if (_contextMenu.IsVisible)
        {
            int clicked = _contextMenu.Update(input);
            if (clicked == 0 && _contextGroupName != null)
                WantsEditGroup = _contextGroupName;
            else if (clicked == 1 && _contextGroupName != null)
                WantsDeleteGroup = _contextGroupName;
            input.ConsumeClick();
            return;
        }

        if (state.Sheet == null) return;

        RebuildGroupIndex(state);

        int tileDisplaySize = GetZoomedTileSize(state.Sheet.Cols);
        var tileArea = GetTileArea();
        int totalWidth = state.Sheet.Cols * tileDisplaySize;
        int totalHeight = state.Sheet.Rows * tileDisplaySize;

        // Zoom button clicks
        bool leftPressed = mouse.LeftButton == ButtonState.Pressed
                           && prevMouse.LeftButton == ButtonState.Released;
        if (leftPressed)
        {
            if (_zoomInRect.Width > 0 && _zoomInRect.Contains(mouse.X, mouse.Y))
            {
                _zoom = MathHelper.Clamp(_zoom + LayoutConstants.TilePaletteZoomStep,
                    LayoutConstants.TilePaletteMinZoom, LayoutConstants.TilePaletteMaxZoom);
                ClampScrollOffsets(state.Sheet.Cols, state.Sheet.Rows);
                input.TryConsumeClick(_zoomInRect);
                return;
            }
            if (_zoomOutRect.Width > 0 && _zoomOutRect.Contains(mouse.X, mouse.Y))
            {
                _zoom = MathHelper.Clamp(_zoom - LayoutConstants.TilePaletteZoomStep,
                    LayoutConstants.TilePaletteMinZoom, LayoutConstants.TilePaletteMaxZoom);
                ClampScrollOffsets(state.Sheet.Cols, state.Sheet.Rows);
                input.TryConsumeClick(_zoomOutRect);
                return;
            }
        }

        // Scroll / zoom within tile area
        if (tileArea.Contains(mouse.X, mouse.Y))
        {
            var kb = Keyboard.GetState();
            bool ctrl = kb.IsKeyDown(Keys.LeftControl) || kb.IsKeyDown(Keys.RightControl);
            bool shift = kb.IsKeyDown(Keys.LeftShift) || kb.IsKeyDown(Keys.RightShift);

            int scrollDelta = prevMouse.ScrollWheelValue - mouse.ScrollWheelValue;

            if (ctrl && scrollDelta != 0)
            {
                // Ctrl+scroll = zoom
                float zoomDelta = scrollDelta > 0
                    ? LayoutConstants.TilePaletteZoomStep
                    : -LayoutConstants.TilePaletteZoomStep;
                _zoom = MathHelper.Clamp(_zoom + zoomDelta,
                    LayoutConstants.TilePaletteMinZoom, LayoutConstants.TilePaletteMaxZoom);
                ClampScrollOffsets(state.Sheet.Cols, state.Sheet.Rows);
                tileDisplaySize = GetZoomedTileSize(state.Sheet.Cols);
            }
            else if (shift)
            {
                // Shift+scroll = horizontal scroll
                _scrollOffsetX = MathHelper.Clamp(_scrollOffsetX + scrollDelta / 4, 0,
                    System.Math.Max(0, totalWidth - tileArea.Width));
            }
            else
            {
                // Plain scroll = vertical scroll
                _scrollOffsetY = MathHelper.Clamp(_scrollOffsetY + scrollDelta / 4, 0,
                    System.Math.Max(0, totalHeight - tileArea.Height));
            }

            // Hit test (accounts for 2D scroll)
            int relX = mouse.X - tileArea.X + _scrollOffsetX;
            int relY = mouse.Y - tileArea.Y + _scrollOffsetY;
            _hoverCol = relX / tileDisplaySize;
            _hoverRow = relY / tileDisplaySize;

            if (_hoverCol >= state.Sheet.Cols || _hoverRow >= state.Sheet.Rows
                || _hoverCol < 0 || _hoverRow < 0)
            {
                _hoverCol = -1;
                _hoverRow = -1;
            }

            // Right-click: context menu for grouped sprites
            bool rightPressed = mouse.RightButton == ButtonState.Pressed
                                && prevMouse.RightButton == ButtonState.Released;
            if (rightPressed && _hoverCol >= 0 && _hoverRow >= 0)
            {
                if (SpriteGroupIndex.TryGetValue((_hoverCol, _hoverRow), out var rGroup))
                {
                    _contextGroupName = rGroup.Name;
                    _contextMenu.Show(mouse.X, mouse.Y, _hoverCol, _hoverRow,
                                      font, screenW, screenH);
                }
            }

            // Left-click handling
            if (leftPressed && _hoverCol >= 0 && _hoverRow >= 0)
            {
                double now = gameTime.TotalGameTime.TotalSeconds;
                bool isGrouped = SpriteGroupIndex.TryGetValue((_hoverCol, _hoverRow), out var group);

                bool isDoubleClick = _lastClickCol == _hoverCol && _lastClickRow == _hoverRow
                    && (now - _lastClickTime) < LayoutConstants.TilePaletteDoubleClickThreshold;

                if (isDoubleClick)
                {
                    if (isGrouped)
                    {
                        // Double-click grouped: edit group
                        WantsEditGroup = group.Name;
                    }
                    else
                    {
                        // Double-click ungrouped: create new group with selection
                        if (!_ungroupedSelection.Contains(_hoverCol, _hoverRow))
                            _ungroupedSelection.AddCell(_hoverCol, _hoverRow);
                        WantsNewGroupWithSprites = _ungroupedSelection.GetSelectedCells()
                            .ToList().AsReadOnly();
                    }
                    _lastClickCol = -1;
                }
                else
                {
                    // Single click
                    if (isGrouped)
                    {
                        state.SelectedGroupName = group.Name;
                        if (group.LayerName != null)
                            state.ActiveLayerName = group.LayerName;
                        _ungroupedSelection.Clear();
                    }
                    else
                    {
                        _ungroupedSelection.Select(_hoverCol, _hoverRow, shift, ctrl);
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

        // Consume clicks within content area for inter-panel consumption
        input.TryConsumeClick(ContentBounds);
    }

    public override void DrawContent(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                                      Renderer renderer)
    {
        renderer.DrawRect(spriteBatch, ContentBounds, Background);

        // Draw zoom toolbar at top of content area
        DrawZoomBar(spriteBatch, font, renderer);

        if (state.Sheet == null)
        {
            spriteBatch.DrawString(font, "No tileset loaded",
                new Vector2(ContentBounds.X + 8, ContentBounds.Y + LayoutConstants.TilePaletteZoomBarHeight + 4),
                LayoutConstants.MapPanelDimLabelColor);
            return;
        }

        int tileDisplaySize = GetZoomedTileSize(state.Sheet.Cols);
        var tileArea = GetTileArea();

        // Scissor clip to tile area so zoomed tiles don't overflow
        var gd = spriteBatch.GraphicsDevice;
        var oldScissor = gd.ScissorRectangle;
        spriteBatch.End();
        var scissorRasterizer = new RasterizerState { ScissorTestEnable = true };
        gd.ScissorRectangle = tileArea;
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                          SamplerState.PointClamp, null, scissorRasterizer);

        for (int row = 0; row < state.Sheet.Rows; row++)
        {
            int drawY = tileArea.Y + row * tileDisplaySize - _scrollOffsetY;

            // Skip rows outside visible area
            if (drawY + tileDisplaySize < tileArea.Y) continue;
            if (drawY > tileArea.Bottom) break;

            for (int col = 0; col < state.Sheet.Cols; col++)
            {
                int drawX = tileArea.X + col * tileDisplaySize - _scrollOffsetX;

                // Skip columns outside visible area
                if (drawX + tileDisplaySize < tileArea.X) continue;
                if (drawX > tileArea.Right) break;

                var destRect = new Rectangle(drawX, drawY, tileDisplaySize, tileDisplaySize);

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
                else if (_ungroupedSelection.Contains(col, row) && !SpriteGroupIndex.ContainsKey((col, row)))
                {
                    // Selected ungrouped sprite: distinct orange-gold highlight
                    renderer.DrawRectOutline(spriteBatch, destRect, UngroupedSelectedHighlight, 2);
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

        // Restore SpriteBatch state
        spriteBatch.End();
        gd.ScissorRectangle = oldScissor;
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                          SamplerState.PointClamp, null, null);

        // Context menu drawn after scissor restore (on top of everything)
        _contextMenu.Draw(spriteBatch, font, renderer);
    }

    private void DrawZoomBar(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer)
    {
        int barH = LayoutConstants.TilePaletteZoomBarHeight;
        var barRect = new Rectangle(ContentBounds.X, ContentBounds.Y,
                                    ContentBounds.Width, barH);
        renderer.DrawRect(spriteBatch, barRect, LayoutConstants.TilePaletteZoomBarBg);

        int btnW = LayoutConstants.TilePaletteZoomButtonWidth;
        int btnH = barH - 4;
        int btnY = ContentBounds.Y + 2;

        // "-" button on right side
        _zoomOutRect = new Rectangle(ContentBounds.Right - btnW * 2 - 6, btnY, btnW, btnH);
        // "+" button on far right
        _zoomInRect = new Rectangle(ContentBounds.Right - btnW - 2, btnY, btnW, btnH);

        var ms = Mouse.GetState();
        bool outHover = _zoomOutRect.Contains(ms.X, ms.Y);
        bool inHover = _zoomInRect.Contains(ms.X, ms.Y);

        renderer.DrawRect(spriteBatch, _zoomOutRect,
            outHover ? LayoutConstants.TilePaletteZoomButtonHoverBg
                     : LayoutConstants.TilePaletteZoomButtonBg);
        renderer.DrawRect(spriteBatch, _zoomInRect,
            inHover ? LayoutConstants.TilePaletteZoomButtonHoverBg
                    : LayoutConstants.TilePaletteZoomButtonBg);

        // "-" label
        var minusSize = font.MeasureString("-");
        spriteBatch.DrawString(font, "-",
            new Vector2(_zoomOutRect.X + (_zoomOutRect.Width - minusSize.X) / 2,
                        _zoomOutRect.Y + (_zoomOutRect.Height - minusSize.Y) / 2),
            LayoutConstants.TilePaletteZoomButtonText);

        // "+" label
        var plusSize = font.MeasureString("+");
        spriteBatch.DrawString(font, "+",
            new Vector2(_zoomInRect.X + (_zoomInRect.Width - plusSize.X) / 2,
                        _zoomInRect.Y + (_zoomInRect.Height - plusSize.Y) / 2),
            LayoutConstants.TilePaletteZoomButtonText);

        // Zoom percentage label
        string zoomText = $"{(int)(_zoom * 100)}%";
        var zoomSize = font.MeasureString(zoomText);
        float textX = _zoomOutRect.X - zoomSize.X - 4;
        float textY = ContentBounds.Y + (barH - zoomSize.Y) / 2;
        spriteBatch.DrawString(font, zoomText, new Vector2(textX, textY),
            LayoutConstants.TilePaletteZoomButtonText);
    }

    private void ClampScrollOffsets(int sheetCols, int sheetRows)
    {
        int tileSize = GetZoomedTileSize(sheetCols);
        var tileArea = GetTileArea();
        int totalWidth = sheetCols * tileSize;
        int totalHeight = sheetRows * tileSize;
        _scrollOffsetX = MathHelper.Clamp(_scrollOffsetX, 0,
            System.Math.Max(0, totalWidth - tileArea.Width));
        _scrollOffsetY = MathHelper.Clamp(_scrollOffsetY, 0,
            System.Math.Max(0, totalHeight - tileArea.Height));
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

    internal int GetZoomedTileSize(int sheetCols)
    {
        int baseSize = CalculateTileDisplaySize(sheetCols);
        return System.Math.Max(LayoutConstants.TilePaletteMinTileDisplaySize,
                               (int)(baseSize * _zoom));
    }
}
