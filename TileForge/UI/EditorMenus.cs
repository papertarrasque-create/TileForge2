using DojoUI;

namespace TileForge.UI;

/// <summary>
/// Defines the menu bar structure for the TileForge editor.
/// Centralizes all menu definitions so they can be tested and maintained in one place.
/// </summary>
public static class EditorMenus
{
    // Menu indices (for MenuActionDispatcher)
    public const int FileMenu = 0;
    public const int EditMenu = 1;
    public const int ViewMenu = 2;
    public const int ToolsMenu = 3;
    public const int PlayMenu = 4;
    public const int HelpMenu = 5;

    // File menu item indices
    public const int File_New = 0;
    public const int File_Open = 1;
    public const int File_OpenRecent = 2;
    // 3 = separator
    public const int File_Save = 4;
    public const int File_SaveAs = 5;
    // 6 = separator
    public const int File_Export = 7;
    // 8 = separator
    public const int File_Exit = 9;

    // Edit menu item indices
    public const int Edit_Undo = 0;
    public const int Edit_Redo = 1;
    // 2 = separator
    public const int Edit_Copy = 3;
    public const int Edit_Paste = 4;
    public const int Edit_Delete = 5;
    // 6 = separator
    public const int Edit_ResizeMap = 7;

    // View menu item indices
    public const int View_ToggleMinimap = 0;
    public const int View_CycleGrid = 1;
    public const int View_ToggleLayerVisibility = 2;
    // 3 = separator
    public const int View_NextLayer = 4;
    // 5 = separator
    public const int View_WorldMap = 6;

    // Tools menu item indices
    public const int Tools_Brush = 0;
    public const int Tools_Eraser = 1;
    public const int Tools_Fill = 2;
    public const int Tools_Entity = 3;
    public const int Tools_Picker = 4;
    public const int Tools_Selection = 5;

    // Play menu item indices
    public const int Play_PlayStop = 0;

    // Help menu item indices
    public const int Help_Shortcuts = 0;
    public const int Help_About = 1;

    public static MenuDef[] CreateMenus() => new[]
    {
        new MenuDef("File", new[]
        {
            new MenuItemDef("New Project", "Ctrl+N"),
            new MenuItemDef("Open...", "Ctrl+O"),
            new MenuItemDef("Open Recent", "Ctrl+Shift+O"),
            MenuItemDef.Separator,
            new MenuItemDef("Save", "Ctrl+S"),
            new MenuItemDef("Save As..."),
            MenuItemDef.Separator,
            new MenuItemDef("Export...", "Ctrl+E"),
            MenuItemDef.Separator,
            new MenuItemDef("Exit"),
        }),
        new MenuDef("Edit", new[]
        {
            new MenuItemDef("Undo", "Ctrl+Z"),
            new MenuItemDef("Redo", "Ctrl+Y"),
            MenuItemDef.Separator,
            new MenuItemDef("Copy", "Ctrl+C"),
            new MenuItemDef("Paste", "Ctrl+V"),
            new MenuItemDef("Delete", "Del"),
            MenuItemDef.Separator,
            new MenuItemDef("Resize Map...", "Ctrl+R"),
        }),
        new MenuDef("View", new[]
        {
            new MenuItemDef("Toggle Minimap", "Ctrl+M"),
            new MenuItemDef("Cycle Grid", "G"),
            new MenuItemDef("Toggle Layer Visibility", "V"),
            MenuItemDef.Separator,
            new MenuItemDef("Next Layer", "Tab"),
            MenuItemDef.Separator,
            new MenuItemDef("World Map...", "Ctrl+W"),
        }),
        new MenuDef("Tools", new[]
        {
            new MenuItemDef("Brush", "B"),
            new MenuItemDef("Eraser", "E"),
            new MenuItemDef("Fill Bucket", "F"),
            new MenuItemDef("Entity Placer", "N"),
            new MenuItemDef("Tile Picker", "I"),
            new MenuItemDef("Selection", "M"),
        }),
        new MenuDef("Play", new[]
        {
            new MenuItemDef("Play / Stop", "F5"),
        }),
        new MenuDef("Help", new[]
        {
            new MenuItemDef("Keyboard Shortcuts"),
            new MenuItemDef("About TileForge"),
        }),
    };
}
