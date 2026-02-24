using TileForge.Data;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class TileGroupGameplayTests
{
    [Fact]
    public void NewTileGroup_HasSafeDefaults()
    {
        var group = new TileGroup();

        Assert.True(group.IsPassable);
        Assert.False(group.IsHazardous);
        Assert.Equal(1.0f, group.MovementCost);
        Assert.Null(group.DamageType);
        Assert.Equal(0, group.DamagePerTick);
        Assert.Equal(EntityType.Interactable, group.EntityType);
    }

    [Fact]
    public void EntityType_AllValuesExist()
    {
        // Verify all 5 enum values exist
        Assert.Equal(0, (int)EntityType.NPC);
        Assert.Equal(1, (int)EntityType.Item);
        Assert.Equal(2, (int)EntityType.Trap);
        Assert.Equal(3, (int)EntityType.Trigger);
        Assert.Equal(4, (int)EntityType.Interactable);
    }

    [Fact]
    public void EntityType_SerializationRoundtrip()
    {
        foreach (EntityType et in System.Enum.GetValues<EntityType>())
        {
            string str = et.ToString();
            Assert.True(System.Enum.TryParse<EntityType>(str, out var parsed));
            Assert.Equal(et, parsed);
        }
    }
}
