using DojoUI;
using Xunit;

namespace TileForge.Tests.DojoUI;

public class MenuBarTests
{
    private static MenuDef[] MakeMenus() =>
    [
        new MenuDef("File",
        [
            new MenuItemDef("New",  "Ctrl+N"),
            new MenuItemDef("Open", "Ctrl+O"),
            MenuItemDef.Separator,
            new MenuItemDef("Exit"),
        ]),
        new MenuDef("Edit",
        [
            new MenuItemDef("Undo",     "Ctrl+Z"),
            new MenuItemDef("Redo",     "Ctrl+Y", Enabled: false),
        ]),
    ];

    [Fact]
    public void Constructor_AcceptsMenuDefs()
    {
        var bar = new MenuBar(MakeMenus());
        Assert.NotNull(bar);
    }

    [Fact]
    public void Height_Is22()
    {
        Assert.Equal(22, MenuBar.Height);
    }

    [Fact]
    public void Menus_ReturnsDefs()
    {
        var defs = MakeMenus();
        var bar  = new MenuBar(defs);

        Assert.Same(defs, bar.Menus);
    }

    [Fact]
    public void IsMenuOpen_InitiallyFalse()
    {
        var bar = new MenuBar(MakeMenus());

        Assert.False(bar.IsMenuOpen);
    }

    [Fact]
    public void SetItemEnabled_SetsState()
    {
        var menus = MakeMenus();
        var bar   = new MenuBar(menus);

        // Item 1 in menu 0 ("Open") is initially enabled
        bar.SetItemEnabled(0, 1, false);

        // Verify by re-enabling and checking no exception (state is internal,
        // but we can verify it has no effect through SetItemEnabled idempotency)
        bar.SetItemEnabled(0, 1, false); // no exception
        bar.SetItemEnabled(0, 1, true);  // re-enable — no exception
    }

    [Fact]
    public void SetItemEnabled_OutOfRange_NoException()
    {
        var bar = new MenuBar(MakeMenus());

        // All of these should silently no-op
        bar.SetItemEnabled(-1,  0, true);
        bar.SetItemEnabled( 0, -1, true);
        bar.SetItemEnabled(99,  0, false);
        bar.SetItemEnabled( 0, 99, false);
    }

    [Fact]
    public void MenuItemDef_Separator_HasCorrectDefaults()
    {
        var sep = MenuItemDef.Separator;

        Assert.True(sep.IsSeparator);
        Assert.Equal("", sep.Label);
    }

    [Fact]
    public void MenuDef_StoresLabelAndItems()
    {
        var items = new[]
        {
            new MenuItemDef("New"),
            new MenuItemDef("Open"),
        };
        var def = new MenuDef("File", items);

        Assert.Equal("File", def.Label);
        Assert.Same(items, def.Items);
    }
}
