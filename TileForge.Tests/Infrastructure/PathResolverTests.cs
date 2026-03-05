using System;
using System.IO;
using Xunit;
using TileForge.Infrastructure;

namespace TileForge.Tests.Infrastructure;

public class PathResolverTests
{
    [Fact]
    public void DefaultPathResolver_SettingsDirectory_EndsWith_Tileforge()
    {
        var resolver = new DefaultPathResolver();
        Assert.EndsWith(".tileforge", resolver.SettingsDirectory);
    }

    [Fact]
    public void DefaultPathResolver_SavesDirectory_IsUnderSettings()
    {
        var resolver = new DefaultPathResolver();
        Assert.StartsWith(resolver.SettingsDirectory, resolver.SavesDirectory);
        Assert.EndsWith("saves", resolver.SavesDirectory);
    }

    [Fact]
    public void DefaultPathResolver_KeybindingsPath_IsUnderSettings()
    {
        var resolver = new DefaultPathResolver();
        Assert.StartsWith(resolver.SettingsDirectory, resolver.KeybindingsPath);
        Assert.EndsWith("keybindings.json", resolver.KeybindingsPath);
    }

    [Fact]
    public void DefaultPathResolver_RecentFilesPath_IsUnderSettings()
    {
        var resolver = new DefaultPathResolver();
        Assert.StartsWith(resolver.SettingsDirectory, resolver.RecentFilesPath);
        Assert.EndsWith("recent.json", resolver.RecentFilesPath);
    }
}
