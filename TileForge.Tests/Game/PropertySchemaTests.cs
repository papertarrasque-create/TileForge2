using System.Linq;
using Xunit;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class PropertySchemaTests
{
    [Fact]
    public void All_NoDuplicateKeys()
    {
        var keys = PropertySchema.All.Select(d => d.Key).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    [Fact]
    public void Get_KnownKey_ReturnsDef()
    {
        var def = PropertySchema.Get("health");
        Assert.NotNull(def);
        Assert.Equal(PropType.Int, def.Type);
    }

    [Fact]
    public void Get_UnknownKey_ReturnsNull()
    {
        Assert.Null(PropertySchema.Get("nonexistent"));
    }

    [Fact]
    public void IsKnown_KnownKey_ReturnsTrue()
    {
        Assert.True(PropertySchema.IsKnown("behavior"));
    }

    [Fact]
    public void IsKnown_UnknownKey_ReturnsFalse()
    {
        Assert.False(PropertySchema.IsKnown("nonexistent"));
    }

    [Fact]
    public void ForEntityType_NPC_IncludesHealth()
    {
        var keys = PropertySchema.ForEntityType(EntityType.NPC).Select(d => d.Key);
        Assert.Contains("health", keys);
    }

    [Fact]
    public void ForEntityType_Item_ExcludesHealth()
    {
        var keys = PropertySchema.ForEntityType(EntityType.Item).Select(d => d.Key);
        Assert.DoesNotContain("health", keys);
    }

    [Fact]
    public void ForEntityType_Item_IncludesHeal()
    {
        var keys = PropertySchema.ForEntityType(EntityType.Item).Select(d => d.Key);
        Assert.Contains("heal", keys);
    }

    [Fact]
    public void ForEntityType_Trigger_IncludesTargetMap()
    {
        var keys = PropertySchema.ForEntityType(EntityType.Trigger).Select(d => d.Key);
        Assert.Contains("target_map", keys);
    }

    [Fact]
    public void DialogueId_AppliesToAllEntityTypes()
    {
        var def = PropertySchema.Get("dialogue_id");
        Assert.NotNull(def);
        Assert.Contains(EntityType.NPC, def.AppliesTo);
        Assert.Contains(EntityType.Item, def.AppliesTo);
        Assert.Contains(EntityType.Trap, def.AppliesTo);
        Assert.Contains(EntityType.Trigger, def.AppliesTo);
        Assert.Contains(EntityType.Interactable, def.AppliesTo);
    }

    [Fact]
    public void AllPropertyKeys_HaveMatchingSchemaEntry()
    {
        var fields = typeof(PropertyKeys).GetFields(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        foreach (var field in fields)
        {
            var key = (string)field.GetValue(null);
            Assert.True(PropertySchema.IsKnown(key),
                $"PropertyKeys.{field.Name} = \"{key}\" has no entry in PropertySchema");
        }
    }
}
