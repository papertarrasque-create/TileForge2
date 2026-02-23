using Microsoft.Xna.Framework;

namespace TileForge;

/// <summary>
/// Centralized layout, sizing, and color constants for the TileForge editor UI.
/// All UI magic numbers live here so they can be tuned from a single location.
/// </summary>
public static class LayoutConstants
{
    // ---------------------------------------------------------------
    //  Window defaults
    // ---------------------------------------------------------------
    public const int DefaultWindowWidth = 1440;
    public const int DefaultWindowHeight = 900;

    // ---------------------------------------------------------------
    //  Default map size (when creating a new map without a project)
    // ---------------------------------------------------------------
    public const int DefaultMapWidth = 40;
    public const int DefaultMapHeight = 30;

    // ---------------------------------------------------------------
    //  Toolbar
    // ---------------------------------------------------------------
    public const int ToolbarHeight = 28;
    public const int ToolbarButtonSize = 24;
    public const int ToolbarButtonPadding = 4;

    public static readonly Color ToolbarBackground = new(40, 40, 40);
    public static readonly Color ToolbarButtonColor = new(55, 55, 55);
    public static readonly Color ToolbarButtonActiveColor = new(70, 90, 130);
    public static readonly Color ToolbarButtonHoverColor = new(65, 65, 65);
    public static readonly Color ToolbarButtonDisabledColor = new(45, 45, 45);
    public static readonly Color ToolbarIconColor = new(200, 200, 200);
    public static readonly Color ToolbarIconDimColor = new(80, 80, 80);
    public static readonly Color ToolbarDimTextColor = new(120, 120, 120);
    public static readonly Color ToolbarSeparatorColor = new(60, 60, 60);
    public static readonly Color ToolbarPlayModeTextColor = new(255, 200, 100);

    // ---------------------------------------------------------------
    //  Panel dock (left sidebar)
    // ---------------------------------------------------------------
    public const int PanelDockWidth = 200;

    public static readonly Color PanelDockBackground = new(35, 35, 35);
    public static readonly Color PanelDockDragIndicator = new(100, 160, 255);

    // ---------------------------------------------------------------
    //  Panel header (shared across all panels)
    // ---------------------------------------------------------------
    public const int PanelHeaderHeight = 24;

    public static readonly Color PanelHeaderColor = new(45, 45, 45);
    public static readonly Color PanelHeaderHoverColor = new(55, 55, 55);
    public static readonly Color PanelHeaderTextColor = new(180, 180, 180);
    public static readonly Color PanelArrowColor = new(140, 140, 140);
    public static readonly Color PanelSeparatorColor = new(60, 60, 60);

    // ---------------------------------------------------------------
    //  Tool panel (2x2 tool button grid)
    // ---------------------------------------------------------------
    public const int ToolButtonSize = 36;
    public const int ToolButtonPadding = 6;

    public static readonly Color ToolButtonColor = new(55, 55, 55);
    public static readonly Color ToolButtonActiveColor = new(70, 90, 130);
    public static readonly Color ToolButtonHoverColor = new(65, 65, 65);
    public static readonly Color ToolIconColor = new(200, 200, 200);
    public static readonly Color ToolIconDimColor = new(140, 140, 140);

    // ---------------------------------------------------------------
    //  Map panel (layers + groups)
    // ---------------------------------------------------------------
    public const int MapPanelLayerHeaderHeight = 28;
    public const int MapPanelGroupItemHeight = 36;
    public const int MapPanelItemPadding = 2;
    public const int MapPanelGroupIndent = 16;
    public const int MapPanelPreviewSize = 24;
    public const int MapPanelVisibilitySize = 12;
    public const int MapPanelArrowAreaWidth = 20;
    public const int MapPanelVisibilityPadding = 8;
    public const int MapPanelAddGroupButtonHeight = 22;
    public const int MapPanelAddLayerButtonHeight = 24;
    public const double MapPanelDoubleClickThreshold = 0.4;
    public const int MapPanelPreferredHeight = 200;

    public static readonly Color MapPanelLayerHeaderBg = new(50, 50, 50);
    public static readonly Color MapPanelLayerHeaderActiveBg = new(55, 70, 95);
    public static readonly Color MapPanelLayerHeaderHoverBg = new(58, 58, 58);
    public static readonly Color MapPanelGroupItemBg = new(42, 42, 42);
    public static readonly Color MapPanelGroupSelectedBg = new(60, 80, 120);
    public static readonly Color MapPanelGroupHoverBg = new(50, 55, 65);
    public static readonly Color MapPanelLabelColor = new(200, 200, 200);
    public static readonly Color MapPanelDimLabelColor = new(120, 120, 120);
    public static readonly Color MapPanelVisibleColor = new(180, 200, 180);
    public static readonly Color MapPanelHiddenColor = new(100, 70, 70);
    public static readonly Color MapPanelArrowColor = new(140, 140, 140);
    public static readonly Color MapPanelBadgeColor = new(100, 100, 100);
    public static readonly Color MapPanelAddButtonBg = new(50, 60, 50);
    public static readonly Color MapPanelAddButtonHoverBg = new(60, 72, 60);
    public static readonly Color MapPanelHeaderTextColor = new(180, 180, 180);
    public static readonly Color MapPanelDragIndicatorColor = new(100, 160, 255);

    // ---------------------------------------------------------------
    //  Status bar
    // ---------------------------------------------------------------
    public const int StatusBarHeight = 22;

    public static readonly Color StatusBarBackground = new(35, 35, 35);
    public static readonly Color StatusBarTextColor = new(160, 160, 160);
    public static readonly Color StatusBarSeparatorColor = new(60, 60, 60);

    // ---------------------------------------------------------------
    //  Map canvas
    // ---------------------------------------------------------------
    public static readonly Color CanvasBackground = new(25, 25, 25);
    public static readonly Color CanvasGridColor = new(255, 255, 255, 30);
    public static readonly Color CanvasGridBorderColor = new(255, 255, 255, 60);
    public static readonly Color CanvasEntitySelectionColor = new(100, 200, 255, 200);
    public const int CanvasEntitySelectionThickness = 2;

    // ---------------------------------------------------------------
    //  Group editor (modal overlay)
    // ---------------------------------------------------------------
    public const int GroupEditorHeaderBaseHeight = 32;
    public const int GroupEditorTypeButtonWidth = 60;
    public const int GroupEditorNameFieldX = 8;
    public const int GroupEditorNameFieldWidth = 200;
    public const int GroupEditorNameFieldHeight = 24;
    public const int GroupEditorTypeButtonsX = 220;

    public static readonly Color GroupEditorBackground = new(20, 20, 20);
    public static readonly Color GroupEditorHeaderColor = new(40, 40, 40);
    public static readonly Color GroupEditorGridColor = new(255, 255, 255, 40);
    public static readonly Color GroupEditorSelectionFill = new(100, 160, 255, 80);
    public static readonly Color GroupEditorSelectionBorder = new(100, 160, 255, 200);
    public static readonly Color GroupEditorHintColor = new(120, 120, 120);
    public static readonly Color GroupEditorTypeActiveColor = new(70, 90, 130);
    public static readonly Color GroupEditorTypeInactiveColor = new(55, 55, 55);
    public static readonly Color GroupEditorSpriteCountColor = new(180, 180, 180);

    // ---------------------------------------------------------------
    //  Main window background (GraphicsDevice.Clear)
    // ---------------------------------------------------------------
    public static readonly Color WindowClearColor = new(30, 30, 30);

    // ---------------------------------------------------------------
    //  Tool preview colors
    // ---------------------------------------------------------------
    public static readonly Color BrushPreviewColor = new(255, 255, 255, 100);
    public static readonly Color EraserPreviewColor = new(255, 60, 60, 120);
    public static readonly Color FillPreviewColor = new(180, 255, 180, 80);
    public static readonly Color EntityPreviewColor = new(100, 200, 255, 100);
    public static readonly Color PickerPreviewColor = new(255, 255, 100, 200);
    public static readonly Color SelectionPreviewColor = new(0, 220, 220, 180);

    // ---------------------------------------------------------------
    //  Selection overlay
    // ---------------------------------------------------------------
    public static readonly Color SelectionOutlineColor = new(0, 220, 220, 200);
    public const int SelectionOutlineThickness = 2;

    // ---------------------------------------------------------------
    //  Grid subdivisions (Fine mode)
    // ---------------------------------------------------------------
    public static readonly Color GridSubdivisionColor = new(255, 255, 255, 15);

    // ---------------------------------------------------------------
    //  Stamp brush preview
    // ---------------------------------------------------------------
    public static readonly Color StampPreviewColor = new(255, 255, 255, 80);
    public static readonly Color StampOutlineColor = new(100, 200, 255, 180);

    // ---------------------------------------------------------------
    //  Tile Palette Panel
    // ---------------------------------------------------------------
    public const int TilePalettePanelPreferredHeight = 200;
    public const int TilePaletteMinTileDisplaySize = 8;
    public const double TilePaletteDoubleClickThreshold = 0.4;

    public static readonly Color TilePaletteSelectedHighlight = new(100, 160, 255, 200);
    public static readonly Color TilePaletteHoverOutline = new(200, 200, 200, 150);
    public static readonly Color TilePaletteUngroupedHint = new(80, 80, 80, 120);
    public static readonly Color TilePaletteBackground = new(30, 30, 30);

    // ---------------------------------------------------------------
    //  Minimap
    // ---------------------------------------------------------------
    public const int MinimapMaxSize = 160;
    public const int MinimapMargin = 10;
    public const int MinimapTileAlpha = 180;

    public static readonly Color MinimapBackgroundColor = new(20, 20, 20, 200);
    public static readonly Color MinimapBorderColor = new(80, 80, 80, 200);
    public static readonly Color MinimapViewportColor = new(255, 255, 255, 200);
    public static readonly Color MinimapEntityColor = new(255, 200, 100, 220);
    public static readonly Color MinimapPlayerColor = new(100, 255, 100, 255);
}
