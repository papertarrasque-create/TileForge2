using System.Linq;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class ShortcutsDialogTests
{
    [Fact]
    public void GetShortcuts_ContainsAllCategories()
    {
        var shortcuts = ShortcutsDialog.GetShortcuts();
        var categories = shortcuts.Select(s => s.Category).Distinct().ToList();

        Assert.Contains("File",  categories);
        Assert.Contains("Edit",  categories);
        Assert.Contains("View",  categories);
        Assert.Contains("Tools", categories);
        Assert.Contains("Play",  categories);
    }

    [Fact]
    public void GetShortcuts_ContainsCtrlS()
    {
        var shortcuts = ShortcutsDialog.GetShortcuts();

        var entry = shortcuts.FirstOrDefault(s =>
            s.Shortcut == "Ctrl+S" && s.Description == "Save");

        Assert.NotNull(entry.Shortcut); // default struct has null Shortcut when not found
        Assert.Equal("Ctrl+S", entry.Shortcut);
        Assert.Equal("Save",   entry.Description);
    }

    [Fact]
    public void GetShortcuts_ContainsBrush()
    {
        var shortcuts = ShortcutsDialog.GetShortcuts();

        var entry = shortcuts.FirstOrDefault(s =>
            s.Shortcut == "B" && s.Description == "Brush");

        Assert.NotNull(entry.Shortcut);
        Assert.Equal("B",     entry.Shortcut);
        Assert.Equal("Brush", entry.Description);
    }

    [Fact]
    public void GetShortcuts_NoDuplicateShortcuts()
    {
        var shortcuts = ShortcutsDialog.GetShortcuts();
        var shortcutStrings = shortcuts.Select(s => s.Shortcut).ToList();
        var distinctCount = shortcutStrings.Distinct().Count();

        Assert.Equal(shortcutStrings.Count, distinctCount);
    }
}
