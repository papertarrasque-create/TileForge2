using System;
using System.Collections.Generic;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class MenuActionDispatcherTests
{
    [Fact]
    public void Dispatch_FileSave_InvokesCallback()
    {
        bool called = false;
        var dispatcher = new MenuActionDispatcher(new Dictionary<(int, int), Action>
        {
            { (EditorMenus.FileMenu, EditorMenus.File_Save), () => called = true },
        });
        dispatcher.Dispatch(EditorMenus.FileMenu, EditorMenus.File_Save);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_FileNew_InvokesCallback()
    {
        bool called = false;
        var dispatcher = new MenuActionDispatcher(new Dictionary<(int, int), Action>
        {
            { (EditorMenus.FileMenu, EditorMenus.File_New), () => called = true },
        });
        dispatcher.Dispatch(EditorMenus.FileMenu, EditorMenus.File_New);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_EditUndo_InvokesCallback()
    {
        bool called = false;
        var dispatcher = new MenuActionDispatcher(new Dictionary<(int, int), Action>
        {
            { (EditorMenus.EditMenu, EditorMenus.Edit_Undo), () => called = true },
        });
        dispatcher.Dispatch(EditorMenus.EditMenu, EditorMenus.Edit_Undo);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_EditRedo_InvokesCallback()
    {
        bool called = false;
        var dispatcher = new MenuActionDispatcher(new Dictionary<(int, int), Action>
        {
            { (EditorMenus.EditMenu, EditorMenus.Edit_Redo), () => called = true },
        });
        dispatcher.Dispatch(EditorMenus.EditMenu, EditorMenus.Edit_Redo);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_ToolsBrush_InvokesCallback()
    {
        bool called = false;
        var dispatcher = new MenuActionDispatcher(new Dictionary<(int, int), Action>
        {
            { (EditorMenus.ToolsMenu, EditorMenus.Tools_Brush), () => called = true },
        });
        dispatcher.Dispatch(EditorMenus.ToolsMenu, EditorMenus.Tools_Brush);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_PlayToggle_InvokesCallback()
    {
        bool called = false;
        var dispatcher = new MenuActionDispatcher(new Dictionary<(int, int), Action>
        {
            { (EditorMenus.PlayMenu, EditorMenus.Play_PlayStop), () => called = true },
        });
        dispatcher.Dispatch(EditorMenus.PlayMenu, EditorMenus.Play_PlayStop);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_InvalidMenu_DoesNotThrow()
    {
        var dispatcher = new MenuActionDispatcher(new Dictionary<(int, int), Action>());
        dispatcher.Dispatch(99, 0);
    }

    [Fact]
    public void Dispatch_InvalidItem_DoesNotThrow()
    {
        var dispatcher = new MenuActionDispatcher(new Dictionary<(int, int), Action>());
        dispatcher.Dispatch(EditorMenus.FileMenu, 99);
    }

    [Fact]
    public void Dispatch_NullDictionary_DoesNotThrow()
    {
        var dispatcher = new MenuActionDispatcher(null);
        dispatcher.Dispatch(EditorMenus.FileMenu, EditorMenus.File_Save);
    }

    [Fact]
    public void Dispatch_ViewToggleMinimap_InvokesCallback()
    {
        bool called = false;
        var dispatcher = new MenuActionDispatcher(new Dictionary<(int, int), Action>
        {
            { (EditorMenus.ViewMenu, EditorMenus.View_ToggleMinimap), () => called = true },
        });
        dispatcher.Dispatch(EditorMenus.ViewMenu, EditorMenus.View_ToggleMinimap);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_HelpAbout_InvokesCallback()
    {
        bool called = false;
        var dispatcher = new MenuActionDispatcher(new Dictionary<(int, int), Action>
        {
            { (EditorMenus.HelpMenu, EditorMenus.Help_About), () => called = true },
        });
        dispatcher.Dispatch(EditorMenus.HelpMenu, EditorMenus.Help_About);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_FileExport_InvokesCallback()
    {
        bool called = false;
        var dispatcher = new MenuActionDispatcher(new Dictionary<(int, int), Action>
        {
            { (EditorMenus.FileMenu, EditorMenus.File_Export), () => called = true },
        });
        dispatcher.Dispatch(EditorMenus.FileMenu, EditorMenus.File_Export);
        Assert.True(called);
    }

    [Fact]
    public void Dispatch_ViewWorldMap_InvokesCallback()
    {
        bool called = false;
        var dispatcher = new MenuActionDispatcher(new Dictionary<(int, int), Action>
        {
            { (EditorMenus.ViewMenu, EditorMenus.View_WorldMap), () => called = true },
        });
        dispatcher.Dispatch(EditorMenus.ViewMenu, EditorMenus.View_WorldMap);
        Assert.True(called);
    }
}
