using System;

namespace TileForge.UI;

/// <summary>
/// Maps (menuIndex, itemIndex) pairs from the MenuBar to editor action callbacks.
/// All callbacks are the same ones wired to InputRouter and Toolbar signals.
/// </summary>
public class MenuActionDispatcher
{
    private readonly Action _newProject;
    private readonly Action _open;
    private readonly Action _openRecent;
    private readonly Action _save;
    private readonly Action _saveAs;
    private readonly Action _export;
    private readonly Action _exit;
    private readonly Action _undo;
    private readonly Action _redo;
    private readonly Action _copy;
    private readonly Action _paste;
    private readonly Action _delete;
    private readonly Action _resizeMap;
    private readonly Action _toggleMinimap;
    private readonly Action _cycleGrid;
    private readonly Action _toggleLayerVisibility;
    private readonly Action _nextLayer;
    private readonly Action _selectBrush;
    private readonly Action _selectEraser;
    private readonly Action _selectFill;
    private readonly Action _selectEntity;
    private readonly Action _selectPicker;
    private readonly Action _selectSelection;
    private readonly Action _playToggle;
    private readonly Action _showShortcuts;
    private readonly Action _showAbout;

    public MenuActionDispatcher(
        Action newProject, Action open, Action openRecent, Action save,
        Action saveAs, Action export, Action exit,
        Action undo, Action redo, Action copy, Action paste,
        Action delete, Action resizeMap,
        Action toggleMinimap, Action cycleGrid, Action toggleLayerVisibility,
        Action nextLayer,
        Action selectBrush, Action selectEraser, Action selectFill,
        Action selectEntity, Action selectPicker, Action selectSelection,
        Action playToggle,
        Action showShortcuts, Action showAbout)
    {
        _newProject = newProject;
        _open = open;
        _openRecent = openRecent;
        _save = save;
        _saveAs = saveAs;
        _export = export;
        _exit = exit;
        _undo = undo;
        _redo = redo;
        _copy = copy;
        _paste = paste;
        _delete = delete;
        _resizeMap = resizeMap;
        _toggleMinimap = toggleMinimap;
        _cycleGrid = cycleGrid;
        _toggleLayerVisibility = toggleLayerVisibility;
        _nextLayer = nextLayer;
        _selectBrush = selectBrush;
        _selectEraser = selectEraser;
        _selectFill = selectFill;
        _selectEntity = selectEntity;
        _selectPicker = selectPicker;
        _selectSelection = selectSelection;
        _playToggle = playToggle;
        _showShortcuts = showShortcuts;
        _showAbout = showAbout;
    }

    /// <summary>
    /// Dispatches a menu click to the appropriate action callback.
    /// If the (menuIndex, itemIndex) pair doesn't map to anything, does nothing.
    /// </summary>
    public void Dispatch(int menuIndex, int itemIndex)
    {
        switch (menuIndex)
        {
            case EditorMenus.FileMenu:
                switch (itemIndex)
                {
                    case EditorMenus.File_New: _newProject?.Invoke(); break;
                    case EditorMenus.File_Open: _open?.Invoke(); break;
                    case EditorMenus.File_OpenRecent: _openRecent?.Invoke(); break;
                    case EditorMenus.File_Save: _save?.Invoke(); break;
                    case EditorMenus.File_SaveAs: _saveAs?.Invoke(); break;
                    case EditorMenus.File_Export: _export?.Invoke(); break;
                    case EditorMenus.File_Exit: _exit?.Invoke(); break;
                }
                break;
            case EditorMenus.EditMenu:
                switch (itemIndex)
                {
                    case EditorMenus.Edit_Undo: _undo?.Invoke(); break;
                    case EditorMenus.Edit_Redo: _redo?.Invoke(); break;
                    case EditorMenus.Edit_Copy: _copy?.Invoke(); break;
                    case EditorMenus.Edit_Paste: _paste?.Invoke(); break;
                    case EditorMenus.Edit_Delete: _delete?.Invoke(); break;
                    case EditorMenus.Edit_ResizeMap: _resizeMap?.Invoke(); break;
                }
                break;
            case EditorMenus.ViewMenu:
                switch (itemIndex)
                {
                    case EditorMenus.View_ToggleMinimap: _toggleMinimap?.Invoke(); break;
                    case EditorMenus.View_CycleGrid: _cycleGrid?.Invoke(); break;
                    case EditorMenus.View_ToggleLayerVisibility: _toggleLayerVisibility?.Invoke(); break;
                    case EditorMenus.View_NextLayer: _nextLayer?.Invoke(); break;
                }
                break;
            case EditorMenus.ToolsMenu:
                switch (itemIndex)
                {
                    case EditorMenus.Tools_Brush: _selectBrush?.Invoke(); break;
                    case EditorMenus.Tools_Eraser: _selectEraser?.Invoke(); break;
                    case EditorMenus.Tools_Fill: _selectFill?.Invoke(); break;
                    case EditorMenus.Tools_Entity: _selectEntity?.Invoke(); break;
                    case EditorMenus.Tools_Picker: _selectPicker?.Invoke(); break;
                    case EditorMenus.Tools_Selection: _selectSelection?.Invoke(); break;
                }
                break;
            case EditorMenus.PlayMenu:
                if (itemIndex == EditorMenus.Play_PlayStop) _playToggle?.Invoke();
                break;
            case EditorMenus.HelpMenu:
                switch (itemIndex)
                {
                    case EditorMenus.Help_Shortcuts: _showShortcuts?.Invoke(); break;
                    case EditorMenus.Help_About: _showAbout?.Invoke(); break;
                }
                break;
        }
    }
}
