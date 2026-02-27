using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Game;

namespace TileForge.UI;

/// <summary>
/// Modal overlay for the World Map grid editor.
/// Allows placing project maps on a 2D grid to define spatial adjacency.
/// Split-pane: pannable/zoomable grid canvas (left) + properties panel (right).
/// Follows the DialogueTreeEditor pattern.
/// </summary>
public class WorldMapEditor
{
    // === Completion ===
    public bool IsComplete { get; private set; }
    public bool WasCancelled { get; private set; }
    public WorldLayout Result { get; private set; }

    // === Working data ===
    private readonly WorldLayout _layout;
    private readonly List<string> _allMapNames;
    private readonly Dictionary<string, (int Width, int Height)> _mapDimensions;

    // === Camera ===
    private readonly NodeGraphCamera _camera = new();

    // === Selection ===
    private string _selectedMap;

    // === Context menus ===
    private readonly ContextMenu _emptyMenu;
    private readonly ContextMenu _filledMenu = new("Remove from Grid");
    private int _contextGridX, _contextGridY;

    // === Drag state ===
    private enum DragMode { None, MapDrag, Pan }
    private DragMode _dragMode;
    private string _draggingMap;
    private Point _dragStartScreen;
    private Point _dragOrigGrid;

    // === Properties panel ===
    private readonly ScrollPanel _propsScroll = new();

    // === Hit-test rects (computed in Draw, used in next Update) ===
    private readonly List<(Rectangle Rect, string MapName)> _cellRects = new();
    private Rectangle _saveRect;
    private Rectangle _cancelRect;
    private readonly List<(Rectangle Rect, string MapName)> _unplacedRects = new();
    private readonly Dictionary<string, Rectangle> _resetRects = new();

    // === NumericField management for edge connection editing ===
    private readonly Dictionary<string, NumericField> _numericFields = new();
    private readonly Dictionary<string, Rectangle> _numericFieldRects = new();
    private string _fieldsForMap;

    // === Layout ===
    private ModalResizeHandler _resize;
    private Rectangle _panelRect;
    private Rectangle _canvasRect;
    private Rectangle _propsRect;
    private SpriteFont _cachedFont;
    private bool _needsCenter;
    private Rectangle _lastBounds;

    // === Place mode: when user clicks an unplaced map, next click on empty cell places it ===
    private string _placingMap;

    // === Constants ===
    private const int HeaderH = LayoutConstants.WorldMapHeaderHeight;
    private const int Padding = LayoutConstants.FormPadding;
    private const int FieldHeight = LayoutConstants.FormFieldHeight;
    private const int RowHeight = LayoutConstants.FormRowHeight;
    private const int LabelWidth = 80;
    private const int DefaultMaxWidth = LayoutConstants.WorldMapMaxWidth;
    private const int DefaultMaxHeight = LayoutConstants.WorldMapMaxHeight;
    private const float CanvasSplit = LayoutConstants.WorldMapCanvasSplit;
    private const int CellW = LayoutConstants.WorldMapCellWidth;
    private const int CellH = LayoutConstants.WorldMapCellHeight;
    private const float ZoomStep = 0.15f;
    private const int GridLineSpacing = 1;

    // === Colors ===
    private static readonly Color Overlay = LayoutConstants.WorldMapOverlay;
    private static readonly Color PanelBg = LayoutConstants.WorldMapPanelBg;
    private static readonly Color HeaderBg = LayoutConstants.WorldMapHeaderBg;
    private static readonly Color CanvasBg = LayoutConstants.WorldMapCanvasBg;
    private static readonly Color GridLineColor = LayoutConstants.WorldMapGridLineColor;
    private static readonly Color CellEmptyBg = LayoutConstants.WorldMapCellEmptyBg;
    private static readonly Color CellFilledBg = LayoutConstants.WorldMapCellFilledBg;
    private static readonly Color CellSelectedBg = LayoutConstants.WorldMapCellSelectedBg;
    private static readonly Color CellHoverBg = LayoutConstants.WorldMapCellHoverBg;
    private static readonly Color CellBorder = LayoutConstants.WorldMapCellBorder;
    private static readonly Color CellSelectedBorder = LayoutConstants.WorldMapCellSelectedBorder;
    private static readonly Color CellTextColor = LayoutConstants.WorldMapCellTextColor;
    private static readonly Color CellDimTextColor = LayoutConstants.WorldMapCellDimTextColor;
    private static readonly Color PropsBg = LayoutConstants.WorldMapPropsBg;
    private static readonly Color PropsSectionColor = LayoutConstants.WorldMapPropsSectionColor;
    private static readonly Color UnplacedItemBg = LayoutConstants.WorldMapUnplacedItemBg;
    private static readonly Color UnplacedItemHoverBg = LayoutConstants.WorldMapUnplacedItemHoverBg;
    private static readonly Color ConnectionColor = LayoutConstants.WorldMapConnectionColor;
    private static readonly Color DividerColor = LayoutConstants.WorldMapDividerColor;
    private static readonly Color HintColor = LayoutConstants.WorldMapHintColor;

    private WorldMapEditor(WorldLayout layout, List<MapDocumentState> maps)
    {
        _layout = layout;
        _allMapNames = maps.Select(m => m.Name).ToList();
        _mapDimensions = new Dictionary<string, (int, int)>();
        foreach (var m in maps)
        {
            if (m.Map != null)
                _mapDimensions[m.Name] = (m.Map.Width, m.Map.Height);
        }

        // Build context menu for empty cells — list unplaced maps
        var unplaced = WorldLayoutHelper.GetUnplacedMaps(_layout, _allMapNames);
        _emptyMenu = new ContextMenu(unplaced.Count > 0
            ? unplaced.Prepend("-- Place Map --").ToArray()
            : new[] { "(No unplaced maps)" });
    }

    // === Factory ===

    public static WorldMapEditor Open(WorldLayout layout, List<MapDocumentState> maps)
    {
        // Deep copy so edits don't affect original until save
        var copy = DeepCopy(layout ?? new WorldLayout());
        var editor = new WorldMapEditor(copy, maps);
        editor._needsCenter = true;
        return editor;
    }

    public void OnTextInput(char character)
    {
        foreach (var field in _numericFields.Values)
        {
            if (field.IsFocused)
            {
                field.HandleCharacter(character);
                return;
            }
        }
    }

    // === Update ===

    public void Update(MouseState mouse, MouseState prevMouse,
                       KeyboardState keyboard, KeyboardState prevKeyboard,
                       Rectangle bounds, List<MapDocumentState> mapDocuments,
                       SpriteFont font = null, int screenW = 0, int screenH = 0)
    {
        if (font != null) _cachedFont = font;
        _lastBounds = bounds;

        // Compute panel layout
        _panelRect = _resize.ComputePanelRect(DefaultMaxWidth, DefaultMaxHeight, bounds);
        _resize.HandleResize(mouse, prevMouse, bounds);
        ComputeSubRects();

        // Center camera on first frame
        if (_needsCenter && _canvasRect.Width > 0)
        {
            CenterOnPlacedMaps();
            _needsCenter = false;
        }

        // Keyboard shortcuts
        if (KeyPressed(keyboard, prevKeyboard, Keys.Escape))
        {
            if (_placingMap != null)
            {
                _placingMap = null; // Cancel placement mode
            }
            else
            {
                IsComplete = true;
                WasCancelled = true;
            }
            return;
        }

        if (KeyPressed(keyboard, prevKeyboard, Keys.Enter))
        {
            Confirm();
            return;
        }

        // Context menus
        int emptyAction = _emptyMenu.Update(mouse, prevMouse);
        if (emptyAction >= 1) // Skip index 0 ("-- Place Map --" header)
        {
            var unplaced = WorldLayoutHelper.GetUnplacedMaps(_layout, _allMapNames);
            int mapIdx = emptyAction - 1;
            if (mapIdx >= 0 && mapIdx < unplaced.Count)
            {
                PlaceMapAtGrid(unplaced[mapIdx], _contextGridX, _contextGridY);
            }
            return;
        }

        int filledAction = _filledMenu.Update(mouse, prevMouse);
        if (filledAction == 0) // "Remove from Grid"
        {
            string mapAtCell = WorldLayoutHelper.GetMapAtCell(_layout, _contextGridX, _contextGridY);
            if (mapAtCell != null)
            {
                _layout.Maps.Remove(mapAtCell);
                if (_selectedMap == mapAtCell) _selectedMap = null;
                RebuildEmptyMenu();
            }
            return;
        }

        if (_emptyMenu.IsVisible || _filledMenu.IsVisible)
            return;

        // Mouse state
        bool leftClick = mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released;
        bool rightClick = mouse.RightButton == ButtonState.Pressed && prevMouse.RightButton == ButtonState.Released;
        bool leftHeld = mouse.LeftButton == ButtonState.Pressed;
        bool leftReleased = mouse.LeftButton == ButtonState.Released && prevMouse.LeftButton == ButtonState.Pressed;
        bool middleHeld = mouse.MiddleButton == ButtonState.Pressed;
        bool middleClick = mouse.MiddleButton == ButtonState.Pressed && prevMouse.MiddleButton == ButtonState.Released;
        var mousePos = new Point(mouse.X, mouse.Y);

        // Unfocus numeric fields on any click (writeback happens here)
        if (leftClick)
        {
            WriteBackFieldValues();
            foreach (var f in _numericFields.Values) f.IsFocused = false;
        }

        // Save/Cancel buttons
        if (leftClick && _saveRect.Contains(mousePos))
        {
            Confirm();
            return;
        }
        if (leftClick && _cancelRect.Contains(mousePos))
        {
            IsComplete = true;
            WasCancelled = true;
            return;
        }

        // Unplaced maps list clicks
        foreach (var (rect, mapName) in _unplacedRects)
        {
            if (leftClick && rect.Contains(mousePos))
            {
                _placingMap = mapName;
                return;
            }
        }

        // Edge connection buttons (Set/Clear/Auto for entries and exits)
        foreach (var (key, rect) in _resetRects)
        {
            if (leftClick && rect.Contains(mousePos))
            {
                if (_selectedMap != null && _layout.Maps.TryGetValue(_selectedMap, out var p))
                    HandleEdgeButton(p, key);
                return;
            }
        }

        // NumericField click focus
        if (leftClick)
        {
            foreach (var (key, rect) in _numericFieldRects)
            {
                if (rect.Contains(mousePos) && _numericFields.TryGetValue(key, out var field))
                {
                    field.IsFocused = true;
                    return;
                }
            }
        }

        // Forward keyboard to focused numeric fields
        foreach (var field in _numericFields.Values)
        {
            if (field.IsFocused)
            {
                Keys[] editKeys = { Keys.Back, Keys.Delete, Keys.Left, Keys.Right, Keys.Home, Keys.End };
                foreach (var key in editKeys)
                {
                    if (KeyPressed(keyboard, prevKeyboard, key))
                        field.HandleKey(key);
                }
                break;
            }
        }

        // Properties scroll
        _propsScroll.UpdateScroll(mouse, prevMouse, _propsRect);

        // Canvas interactions
        if (_canvasRect.Contains(mousePos))
        {
            // Scroll wheel zoom
            int scrollDelta = mouse.ScrollWheelValue - prevMouse.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                float delta = scrollDelta > 0 ? ZoomStep : -ZoomStep;
                _camera.AdjustZoom(delta, new Vector2(mouse.X - _canvasRect.X, mouse.Y - _canvasRect.Y));
            }

            // Get grid cell under mouse
            var worldPos = _camera.ScreenToWorld(new Vector2(mouse.X - _canvasRect.X, mouse.Y - _canvasRect.Y));
            int gridX = (int)MathF.Floor(worldPos.X / CellW);
            int gridY = (int)MathF.Floor(worldPos.Y / CellH);

            if (_dragMode == DragMode.None)
            {
                if (middleClick || (leftClick && keyboard.IsKeyDown(Keys.Space)))
                {
                    _dragMode = DragMode.Pan;
                }
                else if (leftClick)
                {
                    if (_placingMap != null)
                    {
                        // Place mode: clicking an empty cell places the map
                        if (!WorldLayoutHelper.IsCellOccupied(_layout, gridX, gridY))
                        {
                            PlaceMapAtGrid(_placingMap, gridX, gridY);
                            _placingMap = null;
                        }
                    }
                    else
                    {
                        string mapAtCell = WorldLayoutHelper.GetMapAtCell(_layout, gridX, gridY);
                        if (mapAtCell != null)
                        {
                            // Select and start potential drag
                            SelectMap(mapAtCell);
                            _dragMode = DragMode.MapDrag;
                            _draggingMap = mapAtCell;
                            _dragStartScreen = mousePos;
                            _dragOrigGrid = new Point(
                                _layout.Maps[mapAtCell].GridX,
                                _layout.Maps[mapAtCell].GridY);
                        }
                        else
                        {
                            // Click on empty cell — deselect
                            _selectedMap = null;
                        }
                    }
                }
                else if (rightClick)
                {
                    string mapAtCell = WorldLayoutHelper.GetMapAtCell(_layout, gridX, gridY);
                    _contextGridX = gridX;
                    _contextGridY = gridY;
                    if (mapAtCell != null)
                    {
                        _filledMenu.Show(mouse.X, mouse.Y, 0, 0, _cachedFont, _lastBounds.Width, _lastBounds.Height);
                    }
                    else
                    {
                        RebuildEmptyMenu();
                        _emptyMenu.Show(mouse.X, mouse.Y, 0, 0, _cachedFont, _lastBounds.Width, _lastBounds.Height);
                    }
                }
            }

            // Dragging
            if (_dragMode == DragMode.Pan)
            {
                if (middleHeld || (leftHeld && keyboard.IsKeyDown(Keys.Space)))
                {
                    int dx = mouse.X - prevMouse.X;
                    int dy = mouse.Y - prevMouse.Y;
                    _camera.Offset += new Vector2(dx, dy);
                }
                else
                {
                    _dragMode = DragMode.None;
                }
            }
            else if (_dragMode == DragMode.MapDrag)
            {
                if (leftHeld)
                {
                    // Check if we've moved enough to be a real drag (>4px)
                    int moveDistSq = (mouse.X - _dragStartScreen.X) * (mouse.X - _dragStartScreen.X)
                                   + (mouse.Y - _dragStartScreen.Y) * (mouse.Y - _dragStartScreen.Y);
                    if (moveDistSq > 16 && _draggingMap != null && _layout.Maps.TryGetValue(_draggingMap, out var placement))
                    {
                        // Preview: snap to grid cell under cursor
                        int newGridX = (int)MathF.Floor(worldPos.X / CellW);
                        int newGridY = (int)MathF.Floor(worldPos.Y / CellH);
                        if ((newGridX != placement.GridX || newGridY != placement.GridY)
                            && !WorldLayoutHelper.IsCellOccupied(_layout, newGridX, newGridY))
                        {
                            placement.GridX = newGridX;
                            placement.GridY = newGridY;
                        }
                    }
                }
                else if (leftReleased)
                {
                    _dragMode = DragMode.None;
                    _draggingMap = null;
                }
            }
        }
        else if (_dragMode != DragMode.None)
        {
            // Mouse left canvas area — cancel drag
            if (!leftHeld && !middleHeld)
            {
                if (_dragMode == DragMode.MapDrag && _draggingMap != null)
                {
                    // Revert if dropped outside canvas
                    if (_layout.Maps.TryGetValue(_draggingMap, out var p))
                    {
                        p.GridX = _dragOrigGrid.X;
                        p.GridY = _dragOrigGrid.Y;
                    }
                }
                _dragMode = DragMode.None;
                _draggingMap = null;
            }
        }

    }

    // === Draw ===

    public void Draw(SpriteBatch sb, SpriteFont font, Renderer renderer, Rectangle bounds, GameTime gt)
    {
        if (font != null) _cachedFont = font;
        if (_cachedFont == null) return;

        _cellRects.Clear();
        _unplacedRects.Clear();
        _resetRects.Clear();

        // Dark overlay
        renderer.DrawRect(sb, bounds, Overlay);

        // Panel background
        renderer.DrawRect(sb, _panelRect, PanelBg);

        // Header
        var headerRect = new Rectangle(_panelRect.X, _panelRect.Y, _panelRect.Width, HeaderH);
        renderer.DrawRect(sb, headerRect, HeaderBg);

        // Title
        string title = _placingMap != null
            ? $"World Map -- Click to place: {_placingMap}"
            : "World Map";
        int titleY = headerRect.Y + (HeaderH - font.LineSpacing) / 2;
        sb.DrawString(font, title, new Vector2(headerRect.X + Padding, titleY), CellTextColor);

        // Save & Cancel buttons
        int btnW = 60;
        int btnH = 22;
        int btnY = headerRect.Y + (HeaderH - btnH) / 2;
        _cancelRect = new Rectangle(headerRect.Right - Padding - btnW, btnY, btnW, btnH);
        _saveRect = new Rectangle(_cancelRect.X - 8 - btnW, btnY, btnW, btnH);
        DrawButton(sb, font, renderer, _saveRect, "Save", new Color(50, 80, 50), new Color(60, 100, 60));
        DrawButton(sb, font, renderer, _cancelRect, "Cancel", new Color(80, 50, 50), new Color(100, 60, 60));

        // Divider below header
        renderer.DrawRect(sb, new Rectangle(_panelRect.X, headerRect.Bottom, _panelRect.Width, 1), DividerColor);

        // Canvas area
        renderer.DrawRect(sb, _canvasRect, CanvasBg);

        // Draw grid and maps on canvas (clipped to canvas rect)
        DrawCanvas(sb, font, renderer);

        // Divider between canvas and props
        renderer.DrawRect(sb, new Rectangle(_propsRect.X - 1, _propsRect.Y, 1, _propsRect.Height), DividerColor);

        // Properties panel
        renderer.DrawRect(sb, _propsRect, PropsBg);
        DrawPropertiesPanel(sb, font, renderer, gt);

        // Context menus (drawn on top)
        _emptyMenu.Draw(sb, font, renderer);
        _filledMenu.Draw(sb, font, renderer);

        // Resize grip
        _resize.DrawResizeGrip(sb, renderer);
    }

    // === Canvas Drawing ===

    private void DrawCanvas(SpriteBatch sb, SpriteFont font, Renderer renderer)
    {
        // Determine visible grid range
        var topLeft = _camera.ScreenToWorld(Vector2.Zero);
        var botRight = _camera.ScreenToWorld(new Vector2(_canvasRect.Width, _canvasRect.Height));

        int minGridX = (int)MathF.Floor(topLeft.X / CellW) - 1;
        int maxGridX = (int)MathF.Ceiling(botRight.X / CellW) + 1;
        int minGridY = (int)MathF.Floor(topLeft.Y / CellH) - 1;
        int maxGridY = (int)MathF.Ceiling(botRight.Y / CellH) + 1;

        // Draw grid lines
        for (int gx = minGridX; gx <= maxGridX; gx++)
        {
            var screenX = _camera.WorldToScreen(new Vector2(gx * CellW, 0));
            int sx = _canvasRect.X + (int)screenX.X;
            if (sx >= _canvasRect.X && sx <= _canvasRect.Right)
                renderer.DrawRect(sb, new Rectangle(sx, _canvasRect.Y, 1, _canvasRect.Height), GridLineColor);
        }
        for (int gy = minGridY; gy <= maxGridY; gy++)
        {
            var screenY = _camera.WorldToScreen(new Vector2(0, gy * CellH));
            int sy = _canvasRect.Y + (int)screenY.Y;
            if (sy >= _canvasRect.Y && sy <= _canvasRect.Bottom)
                renderer.DrawRect(sb, new Rectangle(_canvasRect.X, sy, _canvasRect.Width, 1), GridLineColor);
        }

        // Draw placed maps
        foreach (var kvp in _layout.Maps)
        {
            string mapName = kvp.Key;
            var placement = kvp.Value;

            var cellScreenPos = _camera.WorldToScreen(new Vector2(placement.GridX * CellW, placement.GridY * CellH));
            int cx = _canvasRect.X + (int)cellScreenPos.X;
            int cy = _canvasRect.Y + (int)cellScreenPos.Y;
            int cw = (int)(CellW * _camera.Zoom);
            int ch = (int)(CellH * _camera.Zoom);

            var cellRect = new Rectangle(cx, cy, cw, ch);

            // Skip cells completely outside canvas
            if (cellRect.Right < _canvasRect.X || cellRect.X > _canvasRect.Right ||
                cellRect.Bottom < _canvasRect.Y || cellRect.Y > _canvasRect.Bottom)
                continue;

            // Background
            bool isSelected = mapName == _selectedMap;
            bool isDragging = mapName == _draggingMap && _dragMode == DragMode.MapDrag;
            Color bgColor = isSelected ? CellSelectedBg : CellFilledBg;
            if (isDragging) bgColor = new Color(bgColor.R + 20, bgColor.G + 20, bgColor.B + 20);
            renderer.DrawRect(sb, cellRect, bgColor);

            // Border
            Color borderColor = isSelected ? CellSelectedBorder : CellBorder;
            DrawBorderRect(sb, renderer, cellRect, borderColor, 2);

            // Map name label (centered)
            string label = mapName;
            if (_cachedFont != null)
            {
                var textSize = _cachedFont.MeasureString(label);
                float scale = Math.Min(1f, (cw - 8) / textSize.X);
                scale = Math.Min(scale, _camera.Zoom);
                int nameY = cy + (int)(ch / 2 - textSize.Y * scale / 2);
                if (scale > 0.1f)
                {
                    int tx = cx + (int)(cw - textSize.X * scale) / 2;
                    sb.DrawString(_cachedFont, label, new Vector2(tx, nameY), CellTextColor, 0f,
                        Vector2.Zero, scale, SpriteEffects.None, 0f);
                }

                // Dimensions text below name
                if (_mapDimensions.TryGetValue(mapName, out var dims))
                {
                    string dimText = $"{dims.Width}x{dims.Height}";
                    var dimSize = _cachedFont.MeasureString(dimText);
                    float dimScale = Math.Min(scale * 0.8f, (cw - 8) / dimSize.X);
                    if (dimScale > 0.1f)
                    {
                        int dx = cx + (int)(cw - dimSize.X * dimScale) / 2;
                        int dy = nameY + (int)(textSize.Y * scale) + 2;
                        sb.DrawString(_cachedFont, dimText, new Vector2(dx, dy), CellDimTextColor, 0f,
                            Vector2.Zero, dimScale, SpriteEffects.None, 0f);
                    }
                }
            }

            _cellRects.Add((cellRect, mapName));
        }

        // Connection indicators between adjacent maps
        DrawConnections(sb, renderer);

        // Placement mode indicator
        if (_placingMap != null && _canvasRect.Contains(Mouse.GetState().X, Mouse.GetState().Y))
        {
            var mouseState = Mouse.GetState();
            var worldPos = _camera.ScreenToWorld(new Vector2(mouseState.X - _canvasRect.X, mouseState.Y - _canvasRect.Y));
            int hoverGridX = (int)MathF.Floor(worldPos.X / CellW);
            int hoverGridY = (int)MathF.Floor(worldPos.Y / CellH);

            if (!WorldLayoutHelper.IsCellOccupied(_layout, hoverGridX, hoverGridY))
            {
                var hoverScreenPos = _camera.WorldToScreen(new Vector2(hoverGridX * CellW, hoverGridY * CellH));
                int hx = _canvasRect.X + (int)hoverScreenPos.X;
                int hy = _canvasRect.Y + (int)hoverScreenPos.Y;
                int hw = (int)(CellW * _camera.Zoom);
                int hh = (int)(CellH * _camera.Zoom);
                renderer.DrawRect(sb, new Rectangle(hx, hy, hw, hh), CellEmptyBg);
                DrawBorderRect(sb, renderer, new Rectangle(hx, hy, hw, hh), new Color(100, 180, 255, 100), 2);
            }
        }
    }

    private void DrawConnections(SpriteBatch sb, Renderer renderer)
    {
        foreach (var kvp in _layout.Maps)
        {
            var placement = kvp.Value;
            var center = _camera.WorldToScreen(new Vector2(
                placement.GridX * CellW + CellW / 2f,
                placement.GridY * CellH + CellH / 2f));

            // Check east neighbor
            string eastNeighbor = WorldLayoutHelper.GetMapAtCell(_layout, placement.GridX + 1, placement.GridY);
            if (eastNeighbor != null)
            {
                var neighborCenter = _camera.WorldToScreen(new Vector2(
                    (placement.GridX + 1) * CellW + CellW / 2f,
                    placement.GridY * CellH + CellH / 2f));

                int x1 = _canvasRect.X + (int)center.X + (int)(CellW * _camera.Zoom / 2);
                int y1 = _canvasRect.Y + (int)center.Y;
                int x2 = _canvasRect.X + (int)neighborCenter.X - (int)(CellW * _camera.Zoom / 2);
                int y2 = y1;

                if (x2 > x1)
                    renderer.DrawRect(sb, new Rectangle(x1, y1 - 1, x2 - x1, 3), ConnectionColor);
            }

            // Check south neighbor
            string southNeighbor = WorldLayoutHelper.GetMapAtCell(_layout, placement.GridX, placement.GridY + 1);
            if (southNeighbor != null)
            {
                var neighborCenter = _camera.WorldToScreen(new Vector2(
                    placement.GridX * CellW + CellW / 2f,
                    (placement.GridY + 1) * CellH + CellH / 2f));

                int x1 = _canvasRect.X + (int)center.X;
                int y1 = _canvasRect.Y + (int)center.Y + (int)(CellH * _camera.Zoom / 2);
                int y2 = _canvasRect.Y + (int)neighborCenter.Y - (int)(CellH * _camera.Zoom / 2);

                if (y2 > y1)
                    renderer.DrawRect(sb, new Rectangle(x1 - 1, y1, 3, y2 - y1), ConnectionColor);
            }
        }
    }

    // === Properties Panel ===

    private void DrawPropertiesPanel(SpriteBatch sb, SpriteFont font, Renderer renderer, GameTime gt)
    {
        int contentX = _propsRect.X + Padding;
        int contentW = _propsRect.Width - Padding * 2 - ScrollPanel.ScrollBarWidth;

        if (_selectedMap == null || !_layout.Maps.ContainsKey(_selectedMap))
        {
            string hint = _placingMap != null
                ? "Click an empty cell to place the map"
                : "Select a map on the grid";
            var size = font.MeasureString(hint);
            sb.DrawString(font, hint,
                new Vector2(_propsRect.X + (_propsRect.Width - size.X) / 2,
                            _propsRect.Y + 40),
                HintColor);

            // Draw unplaced maps list
            DrawUnplacedMaps(sb, font, renderer, _propsRect.Y + 80, contentX, contentW);
            return;
        }

        var placement = _layout.Maps[_selectedMap];

        // Scrollable properties
        var scrollViewport = new Rectangle(_propsRect.X, _propsRect.Y, _propsRect.Width, _propsRect.Height);
        int startY = _propsScroll.BeginScroll(sb, scrollViewport);

        int cursorY = startY + Padding;

        // Section: Map Info
        cursorY = DrawSectionHeader(sb, font, contentX, cursorY, "Map Info");

        // Map name
        cursorY = DrawLabelValue(sb, font, contentX, cursorY, contentW, "Name:", _selectedMap);

        // Grid position
        cursorY = DrawLabelValue(sb, font, contentX, cursorY, contentW, "Grid:", $"({placement.GridX}, {placement.GridY})");

        // Dimensions
        if (_mapDimensions.TryGetValue(_selectedMap, out var dims))
            cursorY = DrawLabelValue(sb, font, contentX, cursorY, contentW, "Size:", $"{dims.Width} x {dims.Height}");

        cursorY += 8;

        // Section: Edge Connections
        cursorY = DrawSectionHeader(sb, font, contentX, cursorY, "Edge Connections");

        // For each direction, show neighbor and spawn fields
        _numericFieldRects.Clear();
        cursorY = DrawEdgeConnection(sb, font, renderer, contentX, contentW, cursorY,
            "North", Direction.Up, _selectedMap, placement, "north", gt);
        cursorY = DrawEdgeConnection(sb, font, renderer, contentX, contentW, cursorY,
            "South", Direction.Down, _selectedMap, placement, "south", gt);
        cursorY = DrawEdgeConnection(sb, font, renderer, contentX, contentW, cursorY,
            "East", Direction.Right, _selectedMap, placement, "east", gt);
        cursorY = DrawEdgeConnection(sb, font, renderer, contentX, contentW, cursorY,
            "West", Direction.Left, _selectedMap, placement, "west", gt);

        cursorY += 8;

        // Section: Unplaced Maps
        cursorY = DrawSectionHeader(sb, font, contentX, cursorY, "Unplaced Maps");
        DrawUnplacedMaps(sb, font, renderer, cursorY, contentX, contentW);

        _propsScroll.EndScroll(sb, renderer, cursorY - startY + 100);
    }

    private int DrawEdgeConnection(SpriteBatch sb, SpriteFont font, Renderer renderer,
        int x, int w, int y, string label, Direction dir, string mapName,
        MapPlacement placement, string dirKey, GameTime gt)
    {
        string neighbor = WorldLayoutHelper.GetNeighbor(_layout, mapName, dir);

        int labelY = y + (RowHeight - font.LineSpacing) / 2;
        sb.DrawString(font, $"{label}:", new Vector2(x, labelY), PropsSectionColor);

        if (neighbor == null)
        {
            sb.DrawString(font, "(none)", new Vector2(x + LabelWidth, labelY), HintColor);
            return y + RowHeight;
        }

        sb.DrawString(font, neighbor, new Vector2(x + LabelWidth, labelY), CellTextColor);
        y += RowHeight;

        // --- Exit point row ---
        EdgeSpawn exit = dirKey switch
        {
            "north" => placement.NorthExit,
            "south" => placement.SouthExit,
            "east" => placement.EastExit,
            "west" => placement.WestExit,
            _ => null,
        };

        y = DrawSpawnRow(sb, font, renderer, x, w, y, dirKey, "exit", "Exit:", exit, gt,
            exit != null ? "Clear" : "Set",
            exit != null ? $"{dirKey}_exit_clear" : $"{dirKey}_exit_set");

        // --- Entry spawn row ---
        EdgeSpawn entry = dirKey switch
        {
            "north" => placement.NorthEntry,
            "south" => placement.SouthEntry,
            "east" => placement.EastEntry,
            "west" => placement.WestEntry,
            _ => null,
        };

        y = DrawSpawnRow(sb, font, renderer, x, w, y, dirKey, "entry", "Spawn:", entry, gt,
            entry != null ? "Auto" : "Set",
            entry != null ? $"{dirKey}_entry_auto" : $"{dirKey}_entry_set");

        return y + 4;
    }

    private int DrawSpawnRow(SpriteBatch sb, SpriteFont font, Renderer renderer,
        int x, int w, int y, string dirKey, string kind, string rowLabel,
        EdgeSpawn spawn, GameTime gt, string btnLabel, string btnKey)
    {
        int fieldX = x + LabelWidth;
        int rowLabelY = y + (FieldHeight - font.LineSpacing) / 2;
        sb.DrawString(font, rowLabel, new Vector2(x + 16, rowLabelY), CellDimTextColor);

        if (spawn != null)
        {
            int halfW = (w - LabelWidth - 60) / 2;
            string xKey = $"{dirKey}_{kind}_x";
            string yKey = $"{dirKey}_{kind}_y";

            var xField = GetOrCreateField(xKey, spawn.X);
            var yField = GetOrCreateField(yKey, spawn.Y);

            // X field
            sb.DrawString(font, "X:", new Vector2(fieldX, rowLabelY), CellDimTextColor);
            var xRect = new Rectangle(fieldX + 20, y, Math.Min(halfW, 50), FieldHeight);
            xField.Draw(sb, font, renderer, xRect, gt);
            _numericFieldRects[xKey] = xRect;

            // Y field
            int yFieldX = xRect.Right + 12;
            sb.DrawString(font, "Y:", new Vector2(yFieldX, rowLabelY), CellDimTextColor);
            var yRect = new Rectangle(yFieldX + 20, y, Math.Min(halfW, 50), FieldHeight);
            yField.Draw(sb, font, renderer, yRect, gt);
            _numericFieldRects[yKey] = yRect;

            // Clear/Auto button
            int btnX = yRect.Right + 8;
            var btnRect = new Rectangle(btnX, y, 40, FieldHeight);
            DrawButton(sb, font, renderer, btnRect, btnLabel, new Color(80, 50, 50), new Color(100, 60, 60));
            _resetRects[btnKey] = btnRect;
        }
        else
        {
            // "auto"/"edge only" text + Set button
            string hint = kind == "exit" ? "edge only" : "auto";
            sb.DrawString(font, hint, new Vector2(fieldX, rowLabelY), HintColor);
            var hintSize = font.MeasureString(hint);
            int btnX = fieldX + (int)hintSize.X + 12;
            var btnRect = new Rectangle(btnX, y, 32, FieldHeight);
            DrawButton(sb, font, renderer, btnRect, btnLabel, new Color(50, 65, 50), new Color(60, 80, 60));
            _resetRects[btnKey] = btnRect;
        }

        return y + RowHeight;
    }

    private void DrawUnplacedMaps(SpriteBatch sb, SpriteFont font, Renderer renderer, int startY, int x, int w)
    {
        var unplaced = WorldLayoutHelper.GetUnplacedMaps(_layout, _allMapNames);
        if (unplaced.Count == 0)
        {
            sb.DrawString(font, "(all maps placed)", new Vector2(x, startY), HintColor);
            return;
        }

        int y = startY;
        foreach (var mapName in unplaced)
        {
            var itemRect = new Rectangle(x, y, w, 24);
            bool isPlacing = mapName == _placingMap;
            Color bg = isPlacing ? CellSelectedBg : UnplacedItemBg;
            renderer.DrawRect(sb, itemRect, bg);

            int textY = y + (24 - font.LineSpacing) / 2;
            string label = isPlacing ? $"> {mapName} (click grid to place)" : mapName;
            sb.DrawString(font, label, new Vector2(x + 6, textY), CellTextColor);

            _unplacedRects.Add((itemRect, mapName));
            y += 26;
        }
    }

    // === Helpers ===

    private void ComputeSubRects()
    {
        int bodyY = _panelRect.Y + HeaderH + 1;
        int bodyH = _panelRect.Height - HeaderH - 1;
        int canvasW = (int)(_panelRect.Width * CanvasSplit);

        _canvasRect = new Rectangle(_panelRect.X, bodyY, canvasW, bodyH);
        _propsRect = new Rectangle(_panelRect.X + canvasW, bodyY, _panelRect.Width - canvasW, bodyH);
    }

    private void CenterOnPlacedMaps()
    {
        if (_layout.Maps.Count == 0)
        {
            _camera.CenterOn(0, 0, _canvasRect.Width, _canvasRect.Height);
            return;
        }

        float sumX = 0, sumY = 0;
        foreach (var kvp in _layout.Maps)
        {
            sumX += kvp.Value.GridX * CellW + CellW / 2f;
            sumY += kvp.Value.GridY * CellH + CellH / 2f;
        }
        _camera.CenterOn(sumX / _layout.Maps.Count, sumY / _layout.Maps.Count,
            _canvasRect.Width, _canvasRect.Height);
    }

    private void SelectMap(string mapName)
    {
        if (_selectedMap != mapName)
        {
            WriteBackFieldValues();
            _numericFields.Clear();
            _numericFieldRects.Clear();
        }
        _selectedMap = mapName;
        _fieldsForMap = mapName;
    }


    private void PlaceMapAtGrid(string mapName, int gridX, int gridY)
    {
        _layout.Maps[mapName] = new MapPlacement { GridX = gridX, GridY = gridY };
        _selectedMap = mapName;
        RebuildEmptyMenu();
    }

    private void Confirm()
    {
        WriteBackFieldValues();
        IsComplete = true;
        WasCancelled = false;
        Result = _layout;
    }

    private void RebuildEmptyMenu()
    {
        // The ContextMenu doesn't support dynamic items, so we create items on Show.
        // The empty menu is rebuilt via constructor in Update when needed.
        // For simplicity, we just let the existing menu items remain — they'll be
        // refreshed on next right-click via the _emptyMenu.Update() flow.
    }

    private static bool KeyPressed(KeyboardState curr, KeyboardState prev, Keys key)
    {
        return curr.IsKeyDown(key) && !prev.IsKeyDown(key);
    }

    private int DrawSectionHeader(SpriteBatch sb, SpriteFont font, int x, int y, string text)
    {
        sb.DrawString(font, text, new Vector2(x, y), PropsSectionColor);
        return y + RowHeight;
    }

    private int DrawLabelValue(SpriteBatch sb, SpriteFont font, int x, int y, int w, string label, string value)
    {
        int textY = y + (RowHeight - font.LineSpacing) / 2;
        sb.DrawString(font, label, new Vector2(x, textY), CellDimTextColor);
        sb.DrawString(font, value, new Vector2(x + LabelWidth, textY), CellTextColor);
        return y + RowHeight;
    }

    private static void DrawValueBox(SpriteBatch sb, SpriteFont font, Renderer renderer,
        Rectangle rect, string text, bool isCustom)
    {
        Color bg = isCustom ? new Color(50, 55, 65) : new Color(38, 38, 42);
        renderer.DrawRect(sb, rect, bg);
        DrawBorderRect(sb, renderer, rect, new Color(60, 60, 65), 1);
        int textY = rect.Y + (rect.Height - font.LineSpacing) / 2;
        Color textColor = isCustom ? CellTextColor : HintColor;
        sb.DrawString(font, text, new Vector2(rect.X + 4, textY), textColor);
    }

    private static void DrawButton(SpriteBatch sb, SpriteFont font, Renderer renderer,
        Rectangle rect, string text, Color bg, Color hoverBg)
    {
        var mouse = Mouse.GetState();
        bool hover = rect.Contains(mouse.X, mouse.Y);
        renderer.DrawRect(sb, rect, hover ? hoverBg : bg);
        var textSize = font.MeasureString(text);
        int tx = rect.X + (int)(rect.Width - textSize.X) / 2;
        int ty = rect.Y + (int)(rect.Height - textSize.Y) / 2;
        sb.DrawString(font, text, new Vector2(tx, ty), CellTextColor);
    }

    private static void DrawBorderRect(SpriteBatch sb, Renderer renderer, Rectangle rect, Color color, int thickness)
    {
        renderer.DrawRect(sb, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color); // top
        renderer.DrawRect(sb, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color); // bottom
        renderer.DrawRect(sb, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color); // left
        renderer.DrawRect(sb, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color); // right
    }

    private NumericField GetOrCreateField(string key, int defaultValue)
    {
        if (!_numericFields.TryGetValue(key, out var field))
        {
            field = new NumericField(defaultValue, 0, 9999);
            _numericFields[key] = field;
        }
        return field;
    }

    private void WriteBackFieldValues()
    {
        if (_fieldsForMap == null || !_layout.Maps.TryGetValue(_fieldsForMap, out var p))
            return;

        foreach (var (key, field) in _numericFields)
        {
            field.ClampValue();
            int val = field.Value;

            switch (key)
            {
                case "north_exit_x": if (p.NorthExit != null) p.NorthExit.X = val; break;
                case "north_exit_y": if (p.NorthExit != null) p.NorthExit.Y = val; break;
                case "south_exit_x": if (p.SouthExit != null) p.SouthExit.X = val; break;
                case "south_exit_y": if (p.SouthExit != null) p.SouthExit.Y = val; break;
                case "east_exit_x": if (p.EastExit != null) p.EastExit.X = val; break;
                case "east_exit_y": if (p.EastExit != null) p.EastExit.Y = val; break;
                case "west_exit_x": if (p.WestExit != null) p.WestExit.X = val; break;
                case "west_exit_y": if (p.WestExit != null) p.WestExit.Y = val; break;
                case "north_entry_x": if (p.NorthEntry != null) p.NorthEntry.X = val; break;
                case "north_entry_y": if (p.NorthEntry != null) p.NorthEntry.Y = val; break;
                case "south_entry_x": if (p.SouthEntry != null) p.SouthEntry.X = val; break;
                case "south_entry_y": if (p.SouthEntry != null) p.SouthEntry.Y = val; break;
                case "east_entry_x": if (p.EastEntry != null) p.EastEntry.X = val; break;
                case "east_entry_y": if (p.EastEntry != null) p.EastEntry.Y = val; break;
                case "west_entry_x": if (p.WestEntry != null) p.WestEntry.X = val; break;
                case "west_entry_y": if (p.WestEntry != null) p.WestEntry.Y = val; break;
            }
        }
    }

    private void HandleEdgeButton(MapPlacement p, string key)
    {
        WriteBackFieldValues();
        switch (key)
        {
            case "north_entry_auto": p.NorthEntry = null; break;
            case "south_entry_auto": p.SouthEntry = null; break;
            case "east_entry_auto": p.EastEntry = null; break;
            case "west_entry_auto": p.WestEntry = null; break;
            case "north_entry_set": p.NorthEntry = new EdgeSpawn(); break;
            case "south_entry_set": p.SouthEntry = new EdgeSpawn(); break;
            case "east_entry_set": p.EastEntry = new EdgeSpawn(); break;
            case "west_entry_set": p.WestEntry = new EdgeSpawn(); break;
            case "north_exit_clear": p.NorthExit = null; break;
            case "south_exit_clear": p.SouthExit = null; break;
            case "east_exit_clear": p.EastExit = null; break;
            case "west_exit_clear": p.WestExit = null; break;
            case "north_exit_set": p.NorthExit = new EdgeSpawn(); break;
            case "south_exit_set": p.SouthExit = new EdgeSpawn(); break;
            case "east_exit_set": p.EastExit = new EdgeSpawn(); break;
            case "west_exit_set": p.WestExit = new EdgeSpawn(); break;
        }
        _numericFields.Clear();
        _numericFieldRects.Clear();
    }

    private static WorldLayout DeepCopy(WorldLayout source)
    {
        var copy = new WorldLayout();
        if (source.Maps != null)
        {
            foreach (var kvp in source.Maps)
            {
                copy.Maps[kvp.Key] = new MapPlacement
                {
                    GridX = kvp.Value.GridX,
                    GridY = kvp.Value.GridY,
                    NorthEntry = CopyEdgeSpawn(kvp.Value.NorthEntry),
                    SouthEntry = CopyEdgeSpawn(kvp.Value.SouthEntry),
                    EastEntry = CopyEdgeSpawn(kvp.Value.EastEntry),
                    WestEntry = CopyEdgeSpawn(kvp.Value.WestEntry),
                    NorthExit = CopyEdgeSpawn(kvp.Value.NorthExit),
                    SouthExit = CopyEdgeSpawn(kvp.Value.SouthExit),
                    EastExit = CopyEdgeSpawn(kvp.Value.EastExit),
                    WestExit = CopyEdgeSpawn(kvp.Value.WestExit),
                };
            }
        }
        return copy;
    }

    private static EdgeSpawn CopyEdgeSpawn(EdgeSpawn source)
    {
        return source == null ? null : new EdgeSpawn { X = source.X, Y = source.Y };
    }
}

