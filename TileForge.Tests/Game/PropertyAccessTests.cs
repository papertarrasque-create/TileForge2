using System.Collections.Generic;
using Xunit;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class PropertyAccessTests
{
    [Fact]
    public void GetInt_ValidValue_ReturnsInt()
    {
        var props = new Dictionary<string, string> { { "health", "42" } };
        Assert.Equal(42, PropertyAccess.GetInt(props, "health"));
    }

    [Fact]
    public void GetInt_MissingKey_ReturnsDefault()
    {
        var props = new Dictionary<string, string>();
        Assert.Equal(10, PropertyAccess.GetInt(props, "health", 10));
    }

    [Fact]
    public void GetInt_NonNumeric_ReturnsDefault()
    {
        var props = new Dictionary<string, string> { { "health", "abc" } };
        Assert.Equal(0, PropertyAccess.GetInt(props, "health"));
    }

    [Fact]
    public void GetBool_True_ReturnsTrue()
    {
        var props = new Dictionary<string, string> { { "hostile", "true" } };
        Assert.True(PropertyAccess.GetBool(props, "hostile"));
    }

    [Fact]
    public void GetBool_False_ReturnsFalse()
    {
        var props = new Dictionary<string, string> { { "hostile", "false" } };
        Assert.False(PropertyAccess.GetBool(props, "hostile"));
    }

    [Fact]
    public void GetBool_FalseCaseInsensitive()
    {
        var props = new Dictionary<string, string> { { "hostile", "FALSE" } };
        Assert.False(PropertyAccess.GetBool(props, "hostile"));
    }

    [Fact]
    public void GetBool_MissingKey_ReturnsDefault()
    {
        var props = new Dictionary<string, string>();
        Assert.False(PropertyAccess.GetBool(props, "hostile"));
        Assert.True(PropertyAccess.GetBool(props, "hostile", true));
    }

    [Fact]
    public void GetBool_NonBoolValue_ReturnsTrue()
    {
        // Matches existing IsEntityHostile semantics: anything that isn't "false" is true
        var props = new Dictionary<string, string> { { "hostile", "banana" } };
        Assert.True(PropertyAccess.GetBool(props, "hostile"));
    }

    [Fact]
    public void GetString_ValidValue_ReturnsValue()
    {
        var props = new Dictionary<string, string> { { "dialogue_id", "npc_hello" } };
        Assert.Equal("npc_hello", PropertyAccess.GetString(props, "dialogue_id"));
    }

    [Fact]
    public void GetString_MissingKey_ReturnsDefault()
    {
        var props = new Dictionary<string, string>();
        Assert.Equal("", PropertyAccess.GetString(props, "dialogue_id"));
        Assert.Equal("fallback", PropertyAccess.GetString(props, "dialogue_id", "fallback"));
    }

    [Fact]
    public void GetString_EmptyValue_ReturnsDefault()
    {
        var props = new Dictionary<string, string> { { "dialogue_id", "" } };
        Assert.Equal("fallback", PropertyAccess.GetString(props, "dialogue_id", "fallback"));
    }

    [Fact]
    public void SetInt_SetsStringValue()
    {
        var props = new Dictionary<string, string>();
        PropertyAccess.SetInt(props, "health", 42);
        Assert.Equal("42", props["health"]);
    }

    [Fact]
    public void SetInt_OverwritesExisting()
    {
        var props = new Dictionary<string, string> { { "health", "100" } };
        PropertyAccess.SetInt(props, "health", 50);
        Assert.Equal("50", props["health"]);
    }
}
