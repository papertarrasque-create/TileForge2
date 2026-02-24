using System;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class MenuActionDispatcherTests
{
    private MenuActionDispatcher CreateDispatcher(
        Action newProject = null, Action open = null, Action openRecent = null,
        Action save = null, Action saveAs = null, Action export = null, Action exit = null,
        Action undo = null, Action redo = null, Action copy = null, Action paste = null,
        Action delete = null, Action resizeMap = null,
        Action toggleMinimap = null, Action cycleGrid = null, Action toggleLayerVisibility = null,
        Action nextLayer = null,
        Action selectBrush = null, Action selectEraser = null, Action selectFill = null,
        Action selectEntity = null, Action selectPicker = null, Action selectSelection = null,
        Action playToggle = null,
        Action showShortcuts = null, Action showAbout = null)
    {
        return new MenuActionDispatcher(
            newProject, open, openRecent, save, saveAs, export, exit,
            undo, redo, copy, paste, delete, resizeMap,
            toggleMinimap, cycleGrid, toggleLayerVisibility, nextLayer,
            selectBrush, selectEraser, selectFill, selectEntity, selectPicker, selectSelection,
            playToggle, showShortcuts, showAbout);
    }

    [Fact]
    public void Dispatch_FileSave_InvokesCallback()
    {
        bool called = false;
        var dispatcher = CreateDispatcher(save: () => called = true);
        dispatcher.Dispatch(EditorMenus.FileMenu, EditorMenus.File_Save);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_FileNew_InvokesCallback()
    {
        bool called = false;
        var dispatcher = CreateDispatcher(newProject: () => called = true);
        dispatcher.Dispatch(EditorMenus.FileMenu, EditorMenus.File_New);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_EditUndo_InvokesCallback()
    {
        bool called = false;
        var dispatcher = CreateDispatcher(undo: () => called = true);
        dispatcher.Dispatch(EditorMenus.EditMenu, EditorMenus.Edit_Undo);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_EditRedo_InvokesCallback()
    {
        bool called = false;
        var dispatcher = CreateDispatcher(redo: () => called = true);
        dispatcher.Dispatch(EditorMenus.EditMenu, EditorMenus.Edit_Redo);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_ToolsBrush_InvokesCallback()
    {
        bool called = false;
        var dispatcher = CreateDispatcher(selectBrush: () => called = true);
        dispatcher.Dispatch(EditorMenus.ToolsMenu, EditorMenus.Tools_Brush);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_PlayToggle_InvokesCallback()
    {
        bool called = false;
        var dispatcher = CreateDispatcher(playToggle: () => called = true);
        dispatcher.Dispatch(EditorMenus.PlayMenu, EditorMenus.Play_PlayStop);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_InvalidMenu_DoesNotThrow()
    {
        var dispatcher = CreateDispatcher();
        dispatcher.Dispatch(99, 0);  // Should not throw
    }

    [Fact]
    public void Dispatch_InvalidItem_DoesNotThrow()
    {
        var dispatcher = CreateDispatcher();
        dispatcher.Dispatch(EditorMenus.FileMenu, 99);  // Should not throw
    }

    [Fact]
    public void Dispatch_NullCallback_DoesNotThrow()
    {
        var dispatcher = CreateDispatcher();  // all null
        dispatcher.Dispatch(EditorMenus.FileMenu, EditorMenus.File_Save);  // null?.Invoke() is safe
    }

    [Fact]
    public void Dispatch_ViewToggleMinimap_InvokesCallback()
    {
        bool called = false;
        var dispatcher = CreateDispatcher(toggleMinimap: () => called = true);
        dispatcher.Dispatch(EditorMenus.ViewMenu, EditorMenus.View_ToggleMinimap);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_HelpAbout_InvokesCallback()
    {
        bool called = false;
        var dispatcher = CreateDispatcher(showAbout: () => called = true);
        dispatcher.Dispatch(EditorMenus.HelpMenu, EditorMenus.Help_About);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_FileExport_InvokesCallback()
    {
        bool called = false;
        var dispatcher = CreateDispatcher(export: () => called = true);
        dispatcher.Dispatch(EditorMenus.FileMenu, EditorMenus.File_Export);
        Assert.True(called);
    }
}
