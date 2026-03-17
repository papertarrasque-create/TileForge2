using Xunit;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class PropertyDefTests
{
    [Fact]
    public void Validate_Int_ValidValue_ReturnsTrue()
    {
        var def = new PropertyDef("health", PropType.Int, new IntRange(1, 9999), EntityType.NPC);
        Assert.True(def.Validate("100", out var reason));
        Assert.Null(reason);
    }

    [Fact]
    public void Validate_Int_NonNumeric_ReturnsFalse()
    {
        var def = new PropertyDef("health", PropType.Int, new IntRange(1, 9999), EntityType.NPC);
        Assert.False(def.Validate("abc", out var reason));
        Assert.Contains("Expected integer", reason);
    }

    [Fact]
    public void Validate_Int_OutOfRange_ReturnsFalse()
    {
        var def = new PropertyDef("health", PropType.Int, new IntRange(1, 9999), EntityType.NPC);
        Assert.False(def.Validate("0", out var reason));
        Assert.Contains("outside range", reason);
    }

    [Fact]
    public void Validate_Int_NoRange_AnyIntValid()
    {
        var def = new PropertyDef("patrol_origin", PropType.Int, EntityType.NPC);
        Assert.True(def.Validate("-5", out _));
        Assert.True(def.Validate("99999", out _));
    }

    [Fact]
    public void Validate_Bool_ValidValues()
    {
        var def = new PropertyDef("hostile", PropType.Bool, EntityType.NPC);
        Assert.True(def.Validate("true", out _));
        Assert.True(def.Validate("false", out _));
        Assert.True(def.Validate("TRUE", out _));
    }

    [Fact]
    public void Validate_Bool_InvalidValue_ReturnsFalse()
    {
        var def = new PropertyDef("hostile", PropType.Bool, EntityType.NPC);
        Assert.False(def.Validate("yes", out var reason));
        Assert.Contains("Expected true/false", reason);
    }

    [Fact]
    public void Validate_Enum_ValidValue()
    {
        var def = new PropertyDef("behavior", PropType.Enum,
            new[] { "idle", "chase", "patrol" }, EntityType.NPC);
        Assert.True(def.Validate("chase", out _));
    }

    [Fact]
    public void Validate_Enum_InvalidValue_ReturnsFalse()
    {
        var def = new PropertyDef("behavior", PropType.Enum,
            new[] { "idle", "chase", "patrol" }, EntityType.NPC);
        Assert.False(def.Validate("flee", out var reason));
        Assert.Contains("not in", reason);
    }

    [Fact]
    public void Validate_EmptyString_AlwaysValid()
    {
        var def = new PropertyDef("health", PropType.Int, new IntRange(1, 9999), EntityType.NPC);
        Assert.True(def.Validate("", out _));
        Assert.True(def.Validate(null, out _));
    }

    [Fact]
    public void Validate_String_AlwaysValid()
    {
        var def = new PropertyDef("hostile_flag", PropType.String, EntityType.NPC);
        Assert.True(def.Validate("anything", out _));
    }
}
