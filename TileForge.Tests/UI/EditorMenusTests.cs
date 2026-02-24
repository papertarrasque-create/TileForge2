using DojoUI;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class EditorMenusTests
{
    [Fact]
    public void CreateMenus_Returns6Menus()
    {
        var menus = EditorMenus.CreateMenus();
        Assert.Equal(6, menus.Length);
    }

    [Fact]
    public void CreateMenus_FileMenu_HasCorrectLabel()
    {
        var menus = EditorMenus.CreateMenus();
        Assert.Equal("File", menus[EditorMenus.FileMenu].Label);
    }

    [Fact]
    public void CreateMenus_EditMenu_HasCorrectLabel()
    {
        var menus = EditorMenus.CreateMenus();
        Assert.Equal("Edit", menus[EditorMenus.EditMenu].Label);
    }

    [Fact]
    public void CreateMenus_ViewMenu_HasCorrectLabel()
    {
        var menus = EditorMenus.CreateMenus();
        Assert.Equal("View", menus[EditorMenus.ViewMenu].Label);
    }

    [Fact]
    public void CreateMenus_ToolsMenu_Has6Items()
    {
        var menus = EditorMenus.CreateMenus();
        Assert.Equal(6, menus[EditorMenus.ToolsMenu].Items.Length);
    }

    [Fact]
    public void CreateMenus_FileMenu_SaveHasHotkey()
    {
        var menus = EditorMenus.CreateMenus();
        var saveItem = menus[EditorMenus.FileMenu].Items[EditorMenus.File_Save];
        Assert.Equal("Save", saveItem.Label);
        Assert.Equal("Ctrl+S", saveItem.Hotkey);
    }

    [Fact]
    public void CreateMenus_FileMenu_HasSeparators()
    {
        var menus = EditorMenus.CreateMenus();
        var items = menus[EditorMenus.FileMenu].Items;
        Assert.True(items[3].IsSeparator);
        Assert.True(items[6].IsSeparator);
        Assert.True(items[8].IsSeparator);
    }

    [Fact]
    public void CreateMenus_IndicesAreConsistent()
    {
        var menus = EditorMenus.CreateMenus();
        // Verify key index constants match actual items
        Assert.Equal("Undo", menus[EditorMenus.EditMenu].Items[EditorMenus.Edit_Undo].Label);
        Assert.Equal("Redo", menus[EditorMenus.EditMenu].Items[EditorMenus.Edit_Redo].Label);
        Assert.Equal("Play / Stop", menus[EditorMenus.PlayMenu].Items[EditorMenus.Play_PlayStop].Label);
        Assert.Equal("Brush", menus[EditorMenus.ToolsMenu].Items[EditorMenus.Tools_Brush].Label);
    }

    [Fact]
    public void CreateMenus_HelpMenu_HasAbout()
    {
        var menus = EditorMenus.CreateMenus();
        Assert.Equal("About TileForge", menus[EditorMenus.HelpMenu].Items[EditorMenus.Help_About].Label);
    }

    [Fact]
    public void CreateMenus_AllItemsHaveNonNullLabel()
    {
        var menus = EditorMenus.CreateMenus();
        foreach (var menu in menus)
        {
            Assert.NotNull(menu.Label);
            foreach (var item in menu.Items)
                Assert.NotNull(item.Label);
        }
    }
}
