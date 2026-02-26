using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Game;

namespace TileForge.UI;

public class DialogueTreeEditor
{
    // === Completion (same API as DialogueEditor) ===
    public bool IsComplete { get; private set; }
    public bool WasCancelled { get; private set; }
    public DialogueData Result { get; private set; }
    public bool IsNew { get; private set; }
    public string OriginalId { get; private set; }

    // === Working data ===
    private readonly DialogueData _data;
    private readonly TextInputField _idField;

    // === Camera ===
    private readonly NodeGraphCamera _camera = new();

    // === Widgets (rebuilt from _data each frame) ===
    private readonly List<DialogueNodeWidget> _widgets = new();
    private readonly Dictionary<string, DialogueNodeWidget> _widgetById = new();

    // === Selection ===
    private int _selectedNodeIndex = -1;

    // === Context menus ===
    private readonly ContextMenu _canvasMenu = new("Add Node");
    private readonly ContextMenu _nodeMenu = new("Delete Node", "Disconnect All");
    private int _contextNodeIndex = -1;
    private Vector2 _contextWorldPos;

    // === Drag state ===
    private enum DragMode { None, Node, Connection, Pan }
    private DragMode _dragMode;
    private Vector2 _dragOffset;
    private int _connectFromNode = -1;
    private int _connectFromPort = -1;

    // === Properties panel: node fields ===
    private readonly TextInputField _nodeIdField = new("", maxLength: 64);
    private readonly TextInputField _nodeSpeakerField = new("", maxLength: 128);
    private readonly TextInputField _nodeTextField = new("", maxLength: 512);
    private readonly TextInputField _nodeNextField = new("", maxLength: 64);
    private readonly TextInputField _nodeReqFlagField = new("", maxLength: 128);
    private readonly TextInputField _nodeSetFlagField = new("", maxLength: 128);
    private readonly TextInputField _nodeSetVarField = new("", maxLength: 128);

    // === Properties panel: choice fields ===
    private readonly List<ChoiceFields> _choiceFields = new();

    // === Properties panel: focus + scroll ===
    private TextInputField _activeField;
    private readonly ScrollPanel _propsScroll = new();
    private readonly TooltipManager _tooltipManager = new(delaySeconds: 0.4);

    // === Hit-test rects (computed in Draw, used in next Update) ===
    private readonly List<Rectangle> _propsFieldRects = new();
    private readonly List<Rectangle> _choiceRemoveRects = new();
    private Rectangle _addChoiceRect;
    private Rectangle _autoLayoutRect;
    private readonly List<(Rectangle Rect, TextInputField Field)> _tooltipFields = new();

    // === Layout ===
    private ModalResizeHandler _resize;
    private Rectangle _panelRect;
    private Rectangle _canvasRect;
    private Rectangle _propsRect;
    private Vector2 _canvasOrigin;
    private SpriteFont _cachedFont;
    private bool _needsCenterOnNodes;

    // === Hover tracking for ports ===
    private int _hoverNodeIndex = -1;
    private int _hoverPortIndex = -1;
    private bool _hoverInputPort;

    // === Constants ===
    private const int HeaderH = LayoutConstants.DialogueTreeHeaderHeight;
    private const int HintH = LayoutConstants.DialogueTreeHintHeight;
    private const int Padding = LayoutConstants.FormPadding;
    private const int FieldHeight = LayoutConstants.FormFieldHeight;
    private const int RowHeight = LayoutConstants.FormRowHeight;
    private const int LabelWidth = LayoutConstants.DialogueEditorLabelWidth;
    private const int DefaultMaxWidth = LayoutConstants.DialogueTreeMaxWidth;
    private const int DefaultMaxHeight = LayoutConstants.DialogueTreeMaxHeight;
    private const float CanvasSplit = LayoutConstants.DialogueTreeCanvasSplit;
    private const float ZoomStep = 0.15f;
    private const int GridDotSpacing = 40;

    // === Colors ===
    private static readonly Color Overlay = LayoutConstants.DialogueTreeOverlay;
    private static readonly Color PanelBg = LayoutConstants.DialogueTreePanelBg;
    private static readonly Color HeaderBg = LayoutConstants.DialogueTreeHeaderBg;
    private static readonly Color CanvasBg = LayoutConstants.DialogueTreeCanvasBg;
    private static readonly Color GridDotColor = LayoutConstants.DialogueTreeGridDotColor;
    private static readonly Color DividerColor = LayoutConstants.DialogueTreeDividerColor;
    private static readonly Color HintColor = LayoutConstants.DialogueTreeHintColor;
    private static readonly Color PropsBg = LayoutConstants.DialogueTreePropsBg;
    private static readonly Color PropsSectionColor = LayoutConstants.DialogueTreePropsSectionColor;
    private static readonly Color LabelColor = LayoutConstants.DialogueEditorLabelColor;
    private static readonly Color NodeBg = LayoutConstants.DialogueNodeBg;
    private static readonly Color NodeHeaderBg = LayoutConstants.DialogueNodeHeaderBg;
    private static readonly Color NodeSelectedHeaderBg = LayoutConstants.DialogueNodeSelectedHeaderBg;
    private static readonly Color NodeBorder = LayoutConstants.DialogueNodeBorder;
    private static readonly Color NodeSelectedBorder = LayoutConstants.DialogueNodeSelectedBorder;
    private static readonly Color NodeTextColor = LayoutConstants.DialogueNodeTextColor;
    private static readonly Color NodeDimTextColor = LayoutConstants.DialogueNodeDimTextColor;
    private static readonly Color PortColor = LayoutConstants.DialogueNodePortColor;
    private static readonly Color PortHoverColor = LayoutConstants.DialogueNodePortHoverColor;
    private static readonly Color ConnectionColor = LayoutConstants.DialogueConnectionColor;
    private static readonly Color ConnectionActiveColor = LayoutConstants.DialogueConnectionActiveColor;
    private static readonly Color ConnectionDragColor = LayoutConstants.DialogueConnectionDragColor;
    private static readonly Color AddBtnBg = LayoutConstants.DialogueEditorAddButtonBg;
    private static readonly Color AddBtnHoverBg = LayoutConstants.DialogueEditorAddButtonHoverBg;
    private static readonly Color RemoveColor = LayoutConstants.DialogueEditorRemoveColor;
    private static readonly Color RemoveHoverColor = LayoutConstants.DialogueEditorRemoveHoverColor;

    private DialogueTreeEditor(DialogueData data)
    {
        _data = data;
        _idField = new TextInputField(data.Id ?? "", maxLength: 64);
    }

    // === Factory Methods ===

    public static DialogueTreeEditor ForNewDialogue()
    {
        var data = new DialogueData { Id = "", Nodes = new List<DialogueNode>() };
        var editor = new DialogueTreeEditor(data) { IsNew = true };
        editor._needsCenterOnNodes = true;
        editor.FocusField(editor._idField);
        return editor;
    }

    public static DialogueTreeEditor ForExistingDialogue(DialogueData dialogue)
    {
        // Deep copy the dialogue so edits don't affect original until save
        var data = DeepCopy(dialogue);
        var editor = new DialogueTreeEditor(data)
        {
            IsNew = false,
            OriginalId = dialogue.Id,
        };

        // Auto-layout nodes without positions
        bool needsLayout = data.Nodes.Any(n => n.EditorX == null || n.EditorY == null);
        if (needsLayout)
            DialogueAutoLayout.ApplyLayout(data);

        editor._needsCenterOnNodes = true;
        editor.FocusField(null);
        return editor;
    }

    public void OnTextInput(char character)
    {
        _activeField?.HandleCharacter(character);
    }

    // === Update ===

    public void Update(MouseState mouse, MouseState prevMouse,
                       KeyboardState keyboard, KeyboardState prevKeyboard,
                       Rectangle bounds, List<DialogueData> existingDialogues,
                       SpriteFont font = null, int screenW = 0, int screenH = 0)
    {
        if (font != null) _cachedFont = font;

        // Flush properties panel → data every frame for live canvas updates
        FlushSelectedNode();
        RebuildWidgets();

        // Compute panel layout
        _panelRect = _resize.ComputePanelRect(DefaultMaxWidth, DefaultMaxHeight, bounds);
        _resize.HandleResize(mouse, prevMouse, bounds);
        ComputeSubRects();

        // Center camera on first frame that has layout info
        if (_needsCenterOnNodes && _canvasRect.Width > 0)
        {
            CenterOnNodes();
            _needsCenterOnNodes = false;
        }

        // Keyboard shortcuts
        if (KeyPressed(keyboard, prevKeyboard, Keys.Escape))
        {
            IsComplete = true;
            WasCancelled = true;
            return;
        }

        if (KeyPressed(keyboard, prevKeyboard, Keys.Enter) && _activeField == null)
        {
            TryConfirm(existingDialogues);
            return;
        }

        if (KeyPressed(keyboard, prevKeyboard, Keys.Tab))
            CycleFocus();

        // Route keys to active field
        if (_activeField != null)
        {
            foreach (var key in new[] { Keys.Back, Keys.Delete, Keys.Left, Keys.Right, Keys.Home, Keys.End })
            {
                if (KeyPressed(keyboard, prevKeyboard, key))
                    _activeField.HandleKey(key);
            }
        }

        // Context menus take priority (modal overlay)
        int canvasAction = _canvasMenu.Update(mouse, prevMouse);
        if (canvasAction == 0) // "Add Node"
        {
            AddNodeAtWorldPos(_contextWorldPos);
            return;
        }

        int nodeAction = _nodeMenu.Update(mouse, prevMouse);
        if (nodeAction >= 0)
        {
            if (nodeAction == 0) // "Delete Node"
                DeleteNode(_contextNodeIndex);
            else if (nodeAction == 1) // "Disconnect All"
                DisconnectNode(_contextNodeIndex);
            return;
        }

        if (_canvasMenu.IsVisible || _nodeMenu.IsVisible)
            return; // menu is showing, consume frame

        // Tooltip
        bool mouseMoved = mouse.X != prevMouse.X || mouse.Y != prevMouse.Y;
        _tooltipManager.Update(mouseMoved ? 0 : 1.0 / 60.0);

        // Properties panel scroll
        if (_selectedNodeIndex >= 0)
            _propsScroll.UpdateScroll(mouse, prevMouse, _propsRect);

        // Handle button clicks
        bool leftClick = mouse.LeftButton == ButtonState.Pressed && prevMouse.LeftButton == ButtonState.Released;
        bool rightClick = mouse.RightButton == ButtonState.Pressed && prevMouse.RightButton == ButtonState.Released;
        bool leftHeld = mouse.LeftButton == ButtonState.Pressed;
        bool leftReleased = mouse.LeftButton == ButtonState.Released && prevMouse.LeftButton == ButtonState.Pressed;
        bool middleHeld = mouse.MiddleButton == ButtonState.Pressed;
        bool middleClick = mouse.MiddleButton == ButtonState.Pressed && prevMouse.MiddleButton == ButtonState.Released;

        // Auto-layout button
        if (leftClick && _autoLayoutRect.Contains(mouse.X, mouse.Y))
        {
            FlushSelectedNode();
            DialogueAutoLayout.ApplyLayout(_data);
            RebuildWidgets();
            CenterOnNodes();
            return;
        }

        // Properties panel interaction
        if (leftClick && _propsRect.Contains(mouse.X, mouse.Y) && _selectedNodeIndex >= 0 && !_resize.IsResizing)
        {
            HandlePropertiesPanelClick(mouse);
            return;
        }

        // Canvas interaction
        if (_canvasRect.Contains(mouse.X, mouse.Y) || _dragMode != DragMode.None)
        {
            UpdateCanvasInteraction(mouse, prevMouse, leftClick, rightClick, leftHeld, leftReleased,
                                    middleHeld, middleClick, screenW, screenH);
        }

        // Scroll zoom on canvas
        int scrollDelta = mouse.ScrollWheelValue - prevMouse.ScrollWheelValue;
        if (scrollDelta != 0 && _canvasRect.Contains(mouse.X, mouse.Y))
        {
            float zoomDelta = scrollDelta > 0 ? ZoomStep : -ZoomStep;
            var localMouse = new Vector2(mouse.X - _canvasRect.X, mouse.Y - _canvasRect.Y);
            _camera.AdjustZoom(zoomDelta, localMouse);
        }

        // Update port hover state
        UpdatePortHover(mouse);
    }

    private void UpdateCanvasInteraction(MouseState mouse, MouseState prevMouse,
        bool leftClick, bool rightClick, bool leftHeld, bool leftReleased,
        bool middleHeld, bool middleClick, int screenW, int screenH)
    {
        var worldMouse = CanvasScreenToWorld(new Vector2(mouse.X, mouse.Y));
        int wmx = (int)worldMouse.X, wmy = (int)worldMouse.Y;

        // Handle active drags
        if (_dragMode == DragMode.Pan && middleHeld)
        {
            float dx = mouse.X - prevMouse.X;
            float dy = mouse.Y - prevMouse.Y;
            _camera.Offset += new Vector2(dx, dy);
            return;
        }
        if (_dragMode == DragMode.Pan && !middleHeld)
        {
            _dragMode = DragMode.None;
            return;
        }

        if (_dragMode == DragMode.Node && leftHeld)
        {
            var nodeIdx = (int)_dragOffset.X; // stored node index
            float offsetX = _dragOffset.Y; // not used — we store world offset differently
            if (nodeIdx >= 0 && nodeIdx < _data.Nodes.Count)
            {
                _data.Nodes[nodeIdx].EditorX = (int)(worldMouse.X - _nodeDragWorldOffset.X);
                _data.Nodes[nodeIdx].EditorY = (int)(worldMouse.Y - _nodeDragWorldOffset.Y);
            }
            return;
        }
        if (_dragMode == DragMode.Node && !leftHeld)
        {
            _dragMode = DragMode.None;
            return;
        }

        if (_dragMode == DragMode.Connection && leftHeld)
        {
            // Connection drag in progress — visual only (drawn in Draw)
            return;
        }
        if (_dragMode == DragMode.Connection && leftReleased)
        {
            // Check if released on an input port
            for (int i = 0; i < _widgets.Count; i++)
            {
                if (i == _connectFromNode) continue;
                if (_widgets[i].HitTestInputPort(wmx, wmy))
                {
                    ConnectNodes(_connectFromNode, _connectFromPort, i);
                    break;
                }
            }
            _dragMode = DragMode.None;
            _connectFromNode = -1;
            _connectFromPort = -1;
            return;
        }

        // Start middle-mouse pan
        if (middleClick)
        {
            _dragMode = DragMode.Pan;
            return;
        }

        // Left-click on canvas
        if (leftClick && _canvasRect.Contains(mouse.X, mouse.Y))
        {
            // Check nodes in reverse order (top-drawn first)
            for (int i = _widgets.Count - 1; i >= 0; i--)
            {
                var w = _widgets[i];

                // Output port click → start connection drag
                int portIdx = w.HitTestOutputPort(wmx, wmy);
                if (portIdx >= 0)
                {
                    _dragMode = DragMode.Connection;
                    _connectFromNode = w.NodeIndex;
                    _connectFromPort = portIdx;
                    return;
                }

                // Body click → select + start drag
                if (w.HitTestBody(wmx, wmy))
                {
                    SelectNode(w.NodeIndex);
                    _dragMode = DragMode.Node;
                    var nodePos = new Vector2(_data.Nodes[w.NodeIndex].EditorX ?? 0,
                                              _data.Nodes[w.NodeIndex].EditorY ?? 0);
                    _nodeDragWorldOffset = worldMouse - nodePos;
                    _dragOffset = new Vector2(w.NodeIndex, 0);
                    return;
                }
            }

            // Click on empty canvas → deselect
            SelectNode(-1);
            return;
        }

        // Right-click → context menu
        if (rightClick && _canvasRect.Contains(mouse.X, mouse.Y) && _cachedFont != null)
        {
            int sw = screenW > 0 ? screenW : _panelRect.Right;
            int sh = screenH > 0 ? screenH : _panelRect.Bottom;

            for (int i = _widgets.Count - 1; i >= 0; i--)
            {
                if (_widgets[i].HitTestBody(wmx, wmy))
                {
                    _contextNodeIndex = _widgets[i].NodeIndex;
                    _nodeMenu.Show(mouse.X, mouse.Y, _contextNodeIndex, 0, _cachedFont, sw, sh);
                    return;
                }
            }

            // Right-click on empty canvas
            _contextWorldPos = worldMouse;
            _canvasMenu.Show(mouse.X, mouse.Y, -1, 0, _cachedFont, sw, sh);
        }
    }

    // Store world-space offset from mouse to node origin during drag
    private Vector2 _nodeDragWorldOffset;

    private void UpdatePortHover(MouseState mouse)
    {
        _hoverNodeIndex = -1;
        _hoverPortIndex = -1;
        _hoverInputPort = false;

        if (!_canvasRect.Contains(mouse.X, mouse.Y)) return;

        var worldMouse = CanvasScreenToWorld(new Vector2(mouse.X, mouse.Y));
        int wmx = (int)worldMouse.X, wmy = (int)worldMouse.Y;

        for (int i = _widgets.Count - 1; i >= 0; i--)
        {
            int port = _widgets[i].HitTestOutputPort(wmx, wmy);
            if (port >= 0)
            {
                _hoverNodeIndex = i;
                _hoverPortIndex = port;
                return;
            }
            if (_widgets[i].HitTestInputPort(wmx, wmy))
            {
                _hoverNodeIndex = i;
                _hoverInputPort = true;
                return;
            }
        }
    }

    // === Draw ===

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
                     Rectangle bounds, GameTime gameTime)
    {
        _cachedFont = font;

        // Recompute layout for Draw
        _panelRect = _resize.ComputePanelRect(DefaultMaxWidth, DefaultMaxHeight, bounds);
        ComputeSubRects();

        // Clear hit-test state
        _propsFieldRects.Clear();
        _choiceRemoveRects.Clear();
        _tooltipFields.Clear();
        _addChoiceRect = Rectangle.Empty;
        _autoLayoutRect = Rectangle.Empty;

        // Dim background
        renderer.DrawRect(spriteBatch, bounds, Overlay);

        // Panel background
        renderer.DrawRect(spriteBatch, _panelRect, PanelBg);

        // Header
        DrawHeader(spriteBatch, font, renderer, gameTime);

        // Canvas area
        DrawCanvas(spriteBatch, font, renderer);

        // Divider
        int divX = _canvasRect.Right;
        renderer.DrawRect(spriteBatch, new Rectangle(divX, _canvasRect.Y, 1, _canvasRect.Height), DividerColor);

        // Properties panel
        DrawPropertiesPanel(spriteBatch, font, renderer, gameTime);

        // Hints bar
        string hints = _activeField != null
            ? "[Enter] Save    [Esc] Cancel    [Tab] Next Field"
            : "[Enter] Save    [Esc] Cancel    [Middle-drag] Pan    [Scroll] Zoom    [Right-click] Menu";
        var hintSize = font.MeasureString(hints);
        int hintY = _panelRect.Bottom - HintH;
        renderer.DrawRect(spriteBatch, new Rectangle(_panelRect.X, hintY, _panelRect.Width, HintH), HeaderBg);
        spriteBatch.DrawString(font, hints,
            new Vector2(_panelRect.Right - hintSize.X - Padding, hintY + (HintH - font.LineSpacing) / 2), HintColor);

        // Context menus (on top of everything)
        _canvasMenu.Draw(spriteBatch, font, renderer);
        _nodeMenu.Draw(spriteBatch, font, renderer);

        // Tooltips
        UpdateTooltipHover(font);
        _tooltipManager.Draw(spriteBatch, font, renderer, bounds.Width);

        // Panel border + resize grip
        renderer.DrawRectOutline(spriteBatch, _panelRect, new Color(80, 80, 80), 1);
        _resize.DrawResizeGrip(spriteBatch, renderer);
    }

    // === Drawing Helpers ===

    private void DrawHeader(SpriteBatch sb, SpriteFont font, Renderer renderer, GameTime gt)
    {
        var headerRect = new Rectangle(_panelRect.X, _panelRect.Y, _panelRect.Width, HeaderH);
        renderer.DrawRect(sb, headerRect, HeaderBg);

        string title = IsNew ? "New Dialogue" : "Edit Dialogue";
        sb.DrawString(font, title, new Vector2(_panelRect.X + Padding, _panelRect.Y + (HeaderH - font.LineSpacing) / 2), Color.White);

        // Id field (after title)
        int titleW = (int)font.MeasureString(title).X + 12;
        int idLabelX = _panelRect.X + Padding + titleW;
        sb.DrawString(font, "Id:", new Vector2(idLabelX, _panelRect.Y + (HeaderH - font.LineSpacing) / 2), LabelColor);
        int idFieldX = idLabelX + (int)font.MeasureString("Id:").X + 6;
        int idFieldW = 180;
        var idRect = new Rectangle(idFieldX, _panelRect.Y + (HeaderH - FieldHeight) / 2, idFieldW, FieldHeight);
        _idField.Draw(sb, font, renderer, idRect, gt);
        _propsFieldRects.Add(idRect);
        _tooltipFields.Add((idRect, _idField));

        // Auto-layout button
        int btnX = idFieldX + idFieldW + 16;
        int btnW = 85;
        _autoLayoutRect = new Rectangle(btnX, _panelRect.Y + (HeaderH - 20) / 2, btnW, 20);
        DrawButton(sb, font, renderer, _autoLayoutRect, "Auto-Layout", AddBtnBg, AddBtnHoverBg);
    }

    private void DrawCanvas(SpriteBatch sb, SpriteFont font, Renderer renderer)
    {
        // Canvas background
        renderer.DrawRect(sb, _canvasRect, CanvasBg);

        // Save scissor state and clip to canvas
        var gd = sb.GraphicsDevice;
        var oldScissor = gd.ScissorRectangle;
        var oldRasterizer = sb.GraphicsDevice.RasterizerState;
        sb.End();
        var rs = new RasterizerState { ScissorTestEnable = true };
        gd.ScissorRectangle = _canvasRect;
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, rs);

        // Grid dots
        DrawGridDots(sb, renderer);

        // Build lookup for connection drawing
        _widgetById.Clear();
        foreach (var w in _widgets)
            _widgetById[w.NodeId] = w;

        // Draw connections
        foreach (var w in _widgets)
        {
            for (int p = 0; p < w.OutputPorts.Count; p++)
            {
                string targetId = w.OutputTargets[p];
                if (targetId != null && _widgetById.TryGetValue(targetId, out var target))
                {
                    var startWorld = w.GetOutputPortCenter(p);
                    var endWorld = target.GetInputPortCenter();
                    bool isActive = w.NodeIndex == _selectedNodeIndex || target.NodeIndex == _selectedNodeIndex;
                    DrawConnectionBezier(sb, renderer, startWorld, endWorld, isActive ? ConnectionActiveColor : ConnectionColor);
                }
            }
        }

        // Draw drag connection
        if (_dragMode == DragMode.Connection && _connectFromNode >= 0 && _connectFromNode < _widgets.Count)
        {
            var fromWidget = _widgets.FirstOrDefault(w => w.NodeIndex == _connectFromNode);
            if (fromWidget != null && _connectFromPort >= 0 && _connectFromPort < fromWidget.OutputPorts.Count)
            {
                var startWorld = fromWidget.GetOutputPortCenter(_connectFromPort);
                var startScreen = CanvasWorldToScreen(startWorld);
                var ms = Mouse.GetState();
                DrawConnectionBezierScreenEnd(sb, renderer, startWorld, new Vector2(ms.X, ms.Y), ConnectionDragColor);
            }
        }

        // Draw nodes
        foreach (var w in _widgets)
            DrawNode(sb, font, renderer, w);

        // Restore scissor
        sb.End();
        gd.ScissorRectangle = oldScissor;
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, oldRasterizer);
    }

    private void DrawGridDots(SpriteBatch sb, Renderer renderer)
    {
        float z = _camera.Zoom;
        float spacing = GridDotSpacing * z;
        if (spacing < 8) return; // too small to show

        float startWorldX = ((_canvasRect.X - _camera.Offset.X - _canvasRect.X) / z);
        float startWorldY = ((_canvasRect.Y - _camera.Offset.Y - _canvasRect.Y) / z);
        float endWorldX = startWorldX + _canvasRect.Width / z;
        float endWorldY = startWorldY + _canvasRect.Height / z;

        int startCol = (int)MathF.Floor(startWorldX / GridDotSpacing);
        int endCol = (int)MathF.Ceiling(endWorldX / GridDotSpacing);
        int startRow = (int)MathF.Floor(startWorldY / GridDotSpacing);
        int endRow = (int)MathF.Ceiling(endWorldY / GridDotSpacing);

        int dotSize = Math.Max(1, (int)(z));
        for (int col = startCol; col <= endCol; col++)
        {
            for (int row = startRow; row <= endRow; row++)
            {
                var screenPos = CanvasWorldToScreen(new Vector2(col * GridDotSpacing, row * GridDotSpacing));
                renderer.DrawRect(sb, new Rectangle((int)screenPos.X, (int)screenPos.Y, dotSize, dotSize), GridDotColor);
            }
        }
    }

    private void DrawNode(SpriteBatch sb, SpriteFont font, Renderer renderer, DialogueNodeWidget w)
    {
        float z = _camera.Zoom;
        bool isSelected = w.NodeIndex == _selectedNodeIndex;
        w.IsSelected = isSelected;

        // Shadow
        var shadowRect = WorldRectToScreen(new Rectangle(w.Bounds.X + 3, w.Bounds.Y + 3, w.Bounds.Width, w.Bounds.Height));
        renderer.DrawRect(sb, shadowRect, new Color(0, 0, 0, 60));

        // Body
        var bodyRect = WorldRectToScreen(w.Bounds);
        renderer.DrawRect(sb, bodyRect, NodeBg);

        // Header
        var headerRect = WorldRectToScreen(w.HeaderBounds);
        renderer.DrawRect(sb, headerRect, isSelected ? NodeSelectedHeaderBg : NodeHeaderBg);

        // Border
        renderer.DrawRectOutline(sb, bodyRect, isSelected ? NodeSelectedBorder : NodeBorder, 1);

        // Don't draw text at very small zoom
        if (z < 0.35f) return;

        float textScale = Math.Min(z, 1.2f);

        // Speaker label in header
        var headerTextPos = CanvasWorldToScreen(new Vector2(w.HeaderBounds.X + 6, w.HeaderBounds.Y + 4));
        string headerText = $"[{w.NodeId}] {w.SpeakerLabel}";
        sb.DrawString(font, headerText, headerTextPos, Color.White, 0f, Vector2.Zero, textScale, SpriteEffects.None, 0f);

        // Text preview
        if (w.TextPreview != null)
        {
            var textPos = CanvasWorldToScreen(new Vector2(
                w.Bounds.X + 6,
                w.Bounds.Y + DialogueNodeWidget.HeaderHeight + 4));
            sb.DrawString(font, w.TextPreview, textPos, NodeDimTextColor, 0f, Vector2.Zero, textScale * 0.9f, SpriteEffects.None, 0f);
        }

        // Output port labels and port indicators
        for (int p = 0; p < w.OutputPorts.Count; p++)
        {
            var port = w.OutputPorts[p];
            var screenPort = WorldRectToScreen(port);

            // Port indicator
            bool portHovered = _hoverNodeIndex >= 0 && _widgets[_hoverNodeIndex].NodeIndex == w.NodeIndex
                               && !_hoverInputPort && _hoverPortIndex == p;
            renderer.DrawRect(sb, screenPort, portHovered ? PortHoverColor : PortColor);

            // Port label
            if (z >= 0.5f && w.OutputLabels[p] != "→")
            {
                var labelPos = CanvasWorldToScreen(new Vector2(
                    port.X - 8 - (w.OutputLabels[p].Length * 6),
                    port.Y + 1));
                sb.DrawString(font, w.OutputLabels[p], labelPos, NodeDimTextColor, 0f, Vector2.Zero, textScale * 0.8f, SpriteEffects.None, 0f);
            }
        }

        // Input port indicator
        var inputScreen = WorldRectToScreen(w.InputPort);
        bool inputHovered = _hoverNodeIndex >= 0 && _widgets[_hoverNodeIndex].NodeIndex == w.NodeIndex && _hoverInputPort;
        renderer.DrawRect(sb, inputScreen, inputHovered ? PortHoverColor : PortColor);
    }

    private void DrawPropertiesPanel(SpriteBatch sb, SpriteFont font, Renderer renderer, GameTime gt)
    {
        renderer.DrawRect(sb, _propsRect, PropsBg);

        if (_selectedNodeIndex < 0 || _selectedNodeIndex >= _data.Nodes.Count)
        {
            // No node selected — show hint
            string hint = "Select a node to edit its properties";
            var size = font.MeasureString(hint);
            sb.DrawString(font, hint,
                new Vector2(_propsRect.X + (_propsRect.Width - size.X) / 2,
                            _propsRect.Y + (_propsRect.Height - size.Y) / 2),
                HintColor);
            return;
        }

        // Scrollable properties area
        var scrollViewport = new Rectangle(_propsRect.X, _propsRect.Y, _propsRect.Width, _propsRect.Height);
        int startY = _propsScroll.BeginScroll(sb, scrollViewport);

        int contentX = _propsRect.X + Padding;
        int contentW = _propsRect.Width - Padding * 2 - ScrollPanel.ScrollBarWidth;
        var layout = new FormLayout(contentX, contentW, startY + Padding,
            labelWidth: LabelWidth, fieldHeight: FieldHeight, rowHeight: RowHeight);

        // Section: Node Properties
        layout.DrawSectionHeader(sb, font, "Node Properties", PropsSectionColor);

        var idRect = layout.DrawLabeledField(sb, font, renderer, "Id:", _nodeIdField, gt, LabelColor);
        _propsFieldRects.Add(idRect);
        _tooltipFields.Add((idRect, _nodeIdField));

        var speakerRect = layout.DrawLabeledField(sb, font, renderer, "Speaker:", _nodeSpeakerField, gt, LabelColor);
        _propsFieldRects.Add(speakerRect);
        _tooltipFields.Add((speakerRect, _nodeSpeakerField));

        var textRect = layout.DrawLabeledField(sb, font, renderer, "Text:", _nodeTextField, gt, LabelColor);
        _propsFieldRects.Add(textRect);
        _tooltipFields.Add((textRect, _nodeTextField));

        var nextRect = layout.DrawLabeledField(sb, font, renderer, "Next:", _nodeNextField, gt, LabelColor);
        _propsFieldRects.Add(nextRect);
        _tooltipFields.Add((nextRect, _nodeNextField));

        var (reqRect, flagRect) = layout.DrawTwoFieldRow(sb, font, renderer,
            "Requires:", _nodeReqFlagField, "Sets Flag:", _nodeSetFlagField,
            gt, LabelColor, LayoutConstants.FormTwoFieldGap);
        _propsFieldRects.Add(reqRect);
        _propsFieldRects.Add(flagRect);
        _tooltipFields.Add((reqRect, _nodeReqFlagField));
        _tooltipFields.Add((flagRect, _nodeSetFlagField));

        var varRect = layout.DrawLabeledField(sb, font, renderer, "Sets Var:", _nodeSetVarField, gt, LabelColor);
        _propsFieldRects.Add(varRect);
        _tooltipFields.Add((varRect, _nodeSetVarField));

        layout.Space(8);

        // Section: Choices
        int choiceLabelY = FormLayout.CenterTextY(layout.CursorY, FieldHeight, font.LineSpacing);
        sb.DrawString(font, "Choices", new Vector2(contentX, choiceLabelY), PropsSectionColor);

        int addBtnW = 50;
        _addChoiceRect = new Rectangle(contentX + 70, layout.CursorY, addBtnW, 18);
        DrawButton(sb, font, renderer, _addChoiceRect, "+ Add", AddBtnBg, AddBtnHoverBg);
        layout.CursorY += RowHeight;

        for (int c = 0; c < _choiceFields.Count; c++)
        {
            var cf = _choiceFields[c];
            char letter = (char)('a' + (c < 26 ? c : 25));

            // Choice text
            int cLabelY = FormLayout.CenterTextY(layout.CursorY, FieldHeight, font.LineSpacing);
            sb.DrawString(font, $"{letter})", new Vector2(contentX, cLabelY), LabelColor);
            int cFieldX = contentX + 20;
            int cFieldW = contentW - 20;
            var cTextRect = new Rectangle(cFieldX, layout.CursorY, cFieldW, FieldHeight);
            cf.TextField.Draw(sb, font, renderer, cTextRect, gt);
            _propsFieldRects.Add(cTextRect);
            _tooltipFields.Add((cTextRect, cf.TextField));
            layout.CursorY += RowHeight;

            // Next + Requires
            var choiceLayout = new FormLayout(cFieldX, cFieldW, layout.CursorY,
                labelWidth: LabelWidth, fieldHeight: FieldHeight, rowHeight: RowHeight);
            var (cNextRect, cReqRect) = choiceLayout.DrawTwoFieldRow(sb, font, renderer,
                "Next:", cf.NextField, "Requires:", cf.RequiresField,
                gt, LabelColor, LayoutConstants.FormTwoFieldGap);
            _propsFieldRects.Add(cNextRect);
            _propsFieldRects.Add(cReqRect);
            _tooltipFields.Add((cNextRect, cf.NextField));
            _tooltipFields.Add((cReqRect, cf.RequiresField));
            layout.CursorY = choiceLayout.CursorY;

            // Sets Flag + Del
            int sfLblY = FormLayout.CenterTextY(layout.CursorY, FieldHeight, font.LineSpacing);
            sb.DrawString(font, "Sets Flag:", new Vector2(cFieldX, sfLblY), LabelColor);
            int sfLblW = (int)font.MeasureString("Sets Flag:").X + 6;
            int delW = 36;
            int sfFieldW = cFieldW - sfLblW - delW - 8;
            var cFlagRect = new Rectangle(cFieldX + sfLblW, layout.CursorY, sfFieldW, FieldHeight);
            cf.SetsField.Draw(sb, font, renderer, cFlagRect, gt);
            _propsFieldRects.Add(cFlagRect);
            _tooltipFields.Add((cFlagRect, cf.SetsField));

            var delRect = new Rectangle(cFieldX + cFieldW - delW, layout.CursorY, delW, FieldHeight);
            _choiceRemoveRects.Add(delRect);
            DrawButton(sb, font, renderer, delRect, "Del", RemoveColor, RemoveHoverColor);

            layout.CursorY += RowHeight + 4;
        }

        int totalH = layout.CursorY - (startY + Padding) + Padding;
        _propsScroll.EndScroll(sb, renderer, totalH);
    }

    // === Coordinate Helpers ===

    private Vector2 CanvasWorldToScreen(Vector2 worldPos)
    {
        return _camera.WorldToScreen(worldPos) + _canvasOrigin;
    }

    private Vector2 CanvasScreenToWorld(Vector2 screenPos)
    {
        return _camera.ScreenToWorld(screenPos - _canvasOrigin);
    }

    private Rectangle WorldRectToScreen(Rectangle worldRect)
    {
        var topLeft = CanvasWorldToScreen(new Vector2(worldRect.X, worldRect.Y));
        return new Rectangle(
            (int)topLeft.X, (int)topLeft.Y,
            Math.Max(1, (int)(worldRect.Width * _camera.Zoom)),
            Math.Max(1, (int)(worldRect.Height * _camera.Zoom)));
    }

    // === Connection Drawing ===

    private void DrawConnectionBezier(SpriteBatch sb, Renderer renderer,
        Vector2 worldStart, Vector2 worldEnd, Color color)
    {
        var s = CanvasWorldToScreen(worldStart);
        var e = CanvasWorldToScreen(worldEnd);
        float dx = MathHelper.Max(50f, MathHelper.Min(MathF.Abs(e.X - s.X) * 0.4f, 200f));
        var cp1 = new Vector2(s.X + dx, s.Y);
        var cp2 = new Vector2(e.X - dx, e.Y);
        renderer.DrawBezier(sb, s, cp1, cp2, e, color, 2f * _camera.Zoom);
    }

    private void DrawConnectionBezierScreenEnd(SpriteBatch sb, Renderer renderer,
        Vector2 worldStart, Vector2 screenEnd, Color color)
    {
        var s = CanvasWorldToScreen(worldStart);
        float dx = MathHelper.Max(30f, MathHelper.Min(MathF.Abs(screenEnd.X - s.X) * 0.4f, 150f));
        var cp1 = new Vector2(s.X + dx, s.Y);
        var cp2 = new Vector2(screenEnd.X - dx, screenEnd.Y);
        renderer.DrawBezier(sb, s, cp1, cp2, screenEnd, color, 2f * _camera.Zoom);
    }

    // === Layout ===

    private void ComputeSubRects()
    {
        int contentTop = _panelRect.Y + HeaderH;
        int contentBottom = _panelRect.Bottom - HintH;
        int contentH = contentBottom - contentTop;

        int canvasW = (int)(_panelRect.Width * CanvasSplit);
        _canvasRect = new Rectangle(_panelRect.X, contentTop, canvasW, contentH);
        _propsRect = new Rectangle(_panelRect.X + canvasW + 1, contentTop, _panelRect.Width - canvasW - 1, contentH);
        _canvasOrigin = new Vector2(_canvasRect.X, _canvasRect.Y);
    }

    // === Data Operations ===

    private void RebuildWidgets()
    {
        _widgets.Clear();
        for (int i = 0; i < _data.Nodes.Count; i++)
            _widgets.Add(DialogueNodeWidget.FromNode(_data.Nodes[i], i));
    }

    private void SelectNode(int index)
    {
        FlushSelectedNode();
        _selectedNodeIndex = index;
        _propsScroll.ScrollOffset = 0;

        if (index >= 0 && index < _data.Nodes.Count)
            PopulatePropertiesPanel(index);
        else
            FocusField(null);
    }

    private void PopulatePropertiesPanel(int index)
    {
        var node = _data.Nodes[index];
        _nodeIdField.SetText(node.Id ?? "");
        _nodeSpeakerField.SetText(node.Speaker ?? "");
        _nodeTextField.SetText(node.Text ?? "");
        _nodeNextField.SetText(node.NextNodeId ?? "");
        _nodeReqFlagField.SetText(node.RequiresFlag ?? "");
        _nodeSetFlagField.SetText(node.SetsFlag ?? "");
        _nodeSetVarField.SetText(node.SetsVariable ?? "");

        _choiceFields.Clear();
        if (node.Choices != null)
        {
            foreach (var choice in node.Choices)
            {
                _choiceFields.Add(new ChoiceFields
                {
                    TextField = new TextInputField(choice.Text ?? "", maxLength: 256),
                    NextField = new TextInputField(choice.NextNodeId ?? "", maxLength: 64),
                    RequiresField = new TextInputField(choice.RequiresFlag ?? "", maxLength: 128),
                    SetsField = new TextInputField(choice.SetsFlag ?? "", maxLength: 128),
                });
            }
        }
    }

    private void FlushSelectedNode()
    {
        if (_selectedNodeIndex < 0 || _selectedNodeIndex >= _data.Nodes.Count) return;

        var node = _data.Nodes[_selectedNodeIndex];
        node.Id = NullIfEmpty(_nodeIdField.Text);
        node.Speaker = NullIfEmpty(_nodeSpeakerField.Text);
        node.Text = NullIfEmpty(_nodeTextField.Text);
        node.NextNodeId = NullIfEmpty(_nodeNextField.Text);
        node.RequiresFlag = NullIfEmpty(_nodeReqFlagField.Text);
        node.SetsFlag = NullIfEmpty(_nodeSetFlagField.Text);
        node.SetsVariable = NullIfEmpty(_nodeSetVarField.Text);

        if (_choiceFields.Count > 0)
        {
            node.Choices ??= new List<DialogueChoice>();
            node.Choices.Clear();
            foreach (var cf in _choiceFields)
            {
                node.Choices.Add(new DialogueChoice
                {
                    Text = NullIfEmpty(cf.TextField.Text),
                    NextNodeId = NullIfEmpty(cf.NextField.Text),
                    RequiresFlag = NullIfEmpty(cf.RequiresField.Text),
                    SetsFlag = NullIfEmpty(cf.SetsField.Text),
                });
            }
        }
        else
        {
            node.Choices = null;
        }
    }

    private void AddNodeAtWorldPos(Vector2 worldPos)
    {
        string newId = $"node_{_data.Nodes.Count}";
        _data.Nodes.Add(new DialogueNode
        {
            Id = newId,
            EditorX = (int)worldPos.X,
            EditorY = (int)worldPos.Y,
        });
        RebuildWidgets();
        SelectNode(_data.Nodes.Count - 1);
    }

    private void DeleteNode(int index)
    {
        if (index < 0 || index >= _data.Nodes.Count) return;

        string deletedId = _data.Nodes[index].Id;

        // Clear references to this node from other nodes
        foreach (var node in _data.Nodes)
        {
            if (node.NextNodeId == deletedId)
                node.NextNodeId = null;
            if (node.Choices != null)
            {
                foreach (var choice in node.Choices)
                {
                    if (choice.NextNodeId == deletedId)
                        choice.NextNodeId = null;
                }
            }
        }

        if (_selectedNodeIndex == index)
            SelectNode(-1);
        else if (_selectedNodeIndex > index)
            _selectedNodeIndex--;

        _data.Nodes.RemoveAt(index);
        RebuildWidgets();
    }

    private void DisconnectNode(int index)
    {
        if (index < 0 || index >= _data.Nodes.Count) return;

        var node = _data.Nodes[index];
        string nodeId = node.Id;

        // Clear this node's outgoing connections
        node.NextNodeId = null;
        if (node.Choices != null)
        {
            foreach (var choice in node.Choices)
                choice.NextNodeId = null;
        }

        // Clear incoming connections to this node
        foreach (var other in _data.Nodes)
        {
            if (other.NextNodeId == nodeId)
                other.NextNodeId = null;
            if (other.Choices != null)
            {
                foreach (var choice in other.Choices)
                {
                    if (choice.NextNodeId == nodeId)
                        choice.NextNodeId = null;
                }
            }
        }

        // Refresh properties panel if this node is selected
        if (_selectedNodeIndex == index)
            PopulatePropertiesPanel(index);

        RebuildWidgets();
    }

    private void ConnectNodes(int fromNodeIndex, int fromPortIndex, int toNodeIndex)
    {
        if (fromNodeIndex < 0 || fromNodeIndex >= _data.Nodes.Count) return;
        if (toNodeIndex < 0 || toNodeIndex >= _data.Nodes.Count) return;

        var fromNode = _data.Nodes[fromNodeIndex];
        string targetId = _data.Nodes[toNodeIndex].Id;

        bool hasChoices = fromNode.Choices != null && fromNode.Choices.Count > 0;
        if (hasChoices && fromPortIndex >= 0 && fromPortIndex < fromNode.Choices.Count)
        {
            fromNode.Choices[fromPortIndex].NextNodeId = targetId;
        }
        else if (!hasChoices)
        {
            fromNode.NextNodeId = targetId;
        }

        // Refresh properties panel if the source node is selected
        if (_selectedNodeIndex == fromNodeIndex)
            PopulatePropertiesPanel(fromNodeIndex);

        RebuildWidgets();
    }

    private void CenterOnNodes()
    {
        if (_widgets.Count == 0)
        {
            _camera.CenterOn(0, 0, _canvasRect.Width, _canvasRect.Height);
            return;
        }

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        foreach (var w in _widgets)
        {
            minX = Math.Min(minX, w.Bounds.X);
            minY = Math.Min(minY, w.Bounds.Y);
            maxX = Math.Max(maxX, w.Bounds.Right);
            maxY = Math.Max(maxY, w.Bounds.Bottom);
        }

        float centerX = (minX + maxX) / 2f;
        float centerY = (minY + maxY) / 2f;
        _camera.CenterOn(centerX, centerY, _canvasRect.Width, _canvasRect.Height);
    }

    // === Properties Panel Click Handling ===

    private void HandlePropertiesPanelClick(MouseState mouse)
    {
        // Check choice remove buttons
        for (int c = 0; c < _choiceRemoveRects.Count && c < _choiceFields.Count; c++)
        {
            if (_choiceRemoveRects[c].Contains(mouse.X, mouse.Y))
            {
                RemoveChoice(c);
                return;
            }
        }

        // Check add choice button
        if (_addChoiceRect.Contains(mouse.X, mouse.Y))
        {
            AddChoice();
            return;
        }

        // Check field clicks
        var allFields = GetPropertiesFields();
        for (int i = 0; i < _propsFieldRects.Count && i < allFields.Count; i++)
        {
            if (_propsFieldRects[i].Contains(mouse.X, mouse.Y))
            {
                FocusField(allFields[i]);
                return;
            }
        }

        // Click on empty properties area → unfocus
        FocusField(null);
    }

    private void AddChoice()
    {
        _choiceFields.Add(new ChoiceFields
        {
            TextField = new TextInputField("", maxLength: 256),
            NextField = new TextInputField("", maxLength: 64),
            RequiresField = new TextInputField("", maxLength: 128),
            SetsField = new TextInputField("", maxLength: 128),
        });
    }

    private void RemoveChoice(int index)
    {
        if (index >= 0 && index < _choiceFields.Count)
        {
            var cf = _choiceFields[index];
            if (_activeField == cf.TextField || _activeField == cf.NextField ||
                _activeField == cf.RequiresField || _activeField == cf.SetsField)
                FocusField(null);
            _choiceFields.RemoveAt(index);
        }
    }

    // === Confirmation ===

    private void TryConfirm(List<DialogueData> existingDialogues)
    {
        string id = _idField.Text.Trim();
        if (string.IsNullOrEmpty(id)) return;

        if (existingDialogues != null)
        {
            foreach (var existing in existingDialogues)
            {
                if (existing.Id == id && (IsNew || id != OriginalId))
                    return;
            }
        }

        FlushSelectedNode();
        _data.Id = id;
        Result = _data;
        IsComplete = true;
        WasCancelled = false;
    }

    // === Focus Management ===

    private List<TextInputField> GetPropertiesFields()
    {
        var fields = new List<TextInputField>
        {
            _idField, _nodeIdField, _nodeSpeakerField, _nodeTextField,
            _nodeNextField, _nodeReqFlagField, _nodeSetFlagField, _nodeSetVarField,
        };

        foreach (var cf in _choiceFields)
        {
            fields.Add(cf.TextField);
            fields.Add(cf.NextField);
            fields.Add(cf.RequiresField);
            fields.Add(cf.SetsField);
        }

        return fields;
    }

    private void CycleFocus()
    {
        var allFields = GetPropertiesFields();
        if (_activeField == null)
        {
            FocusField(allFields.Count > 0 ? allFields[0] : null);
            return;
        }
        int idx = allFields.IndexOf(_activeField);
        if (idx < 0 || idx >= allFields.Count - 1)
            FocusField(null);
        else
            FocusField(allFields[idx + 1]);
    }

    private void FocusField(TextInputField field)
    {
        if (_activeField != null) _activeField.IsFocused = false;
        _activeField = field;
        if (_activeField != null) _activeField.IsFocused = true;
    }

    // === Tooltip ===

    private void UpdateTooltipHover(SpriteFont font)
    {
        var ms = Mouse.GetState();
        bool foundOverflow = false;
        foreach (var (rect, field) in _tooltipFields)
        {
            if (rect.Contains(ms.X, ms.Y) && field.IsTextOverflowing(font, rect))
            {
                _tooltipManager.SetHover(field.Text, ms.X, ms.Y);
                foundOverflow = true;
                break;
            }
        }
        if (!foundOverflow)
            _tooltipManager.ClearHover();
    }

    // === UI Helpers ===

    private static void DrawButton(SpriteBatch sb, SpriteFont font, Renderer renderer,
        Rectangle rect, string label, Color bg, Color hoverBg)
    {
        var ms = Mouse.GetState();
        bool hovered = rect.Contains(ms.X, ms.Y);
        renderer.DrawRect(sb, rect, hovered ? hoverBg : bg);
        var size = font.MeasureString(label);
        sb.DrawString(font, label,
            new Vector2(rect.X + (rect.Width - size.X) / 2, rect.Y + (rect.Height - size.Y) / 2), Color.White);
    }

    private static string NullIfEmpty(string text)
    {
        string trimmed = text?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static bool KeyPressed(KeyboardState current, KeyboardState prev, Keys key) =>
        current.IsKeyDown(key) && prev.IsKeyUp(key);

    // === Deep Copy ===

    private static DialogueData DeepCopy(DialogueData src)
    {
        var copy = new DialogueData { Id = src.Id, Nodes = new List<DialogueNode>() };
        foreach (var node in src.Nodes)
        {
            var nodeCopy = new DialogueNode
            {
                Id = node.Id,
                Speaker = node.Speaker,
                Text = node.Text,
                NextNodeId = node.NextNodeId,
                RequiresFlag = node.RequiresFlag,
                SetsFlag = node.SetsFlag,
                SetsVariable = node.SetsVariable,
                EditorX = node.EditorX,
                EditorY = node.EditorY,
            };

            if (node.Choices != null)
            {
                nodeCopy.Choices = new List<DialogueChoice>();
                foreach (var choice in node.Choices)
                {
                    nodeCopy.Choices.Add(new DialogueChoice
                    {
                        Text = choice.Text,
                        NextNodeId = choice.NextNodeId,
                        RequiresFlag = choice.RequiresFlag,
                        SetsFlag = choice.SetsFlag,
                    });
                }
            }

            copy.Nodes.Add(nodeCopy);
        }
        return copy;
    }

    // === Inner Classes ===

    private class ChoiceFields
    {
        public TextInputField TextField = new("", maxLength: 256);
        public TextInputField NextField = new("", maxLength: 64);
        public TextInputField RequiresField = new("", maxLength: 128);
        public TextInputField SetsField = new("", maxLength: 128);
    }
}
