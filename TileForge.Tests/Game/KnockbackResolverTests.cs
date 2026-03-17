using System;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class KnockbackResolverTests
{
    private static readonly Func<int, int, bool> AlwaysWalkable = (x, y) => true;
    private static readonly Func<int, int, bool> NeverWalkable = (x, y) => false;

    [Fact]
    public void Weight1_KnockedBack_OnFirstHit()
    {
        // Attacker at (5,5), defender at (5,4) -- attacker is below, so knockback pushes up
        var result = KnockbackResolver.Resolve(5, 5, 5, 4, weight: 1, hitsThisTurn: 1, AlwaysWalkable);
        Assert.True(result.KnockedBack);
        Assert.Equal(5, result.NewX);
        Assert.Equal(3, result.NewY);
    }

    // Weight tests
    [Fact]
    public void Weight2_NotKnockedBack_OnFirstHit()
    {
        var result = KnockbackResolver.Resolve(5, 5, 5, 4, weight: 2, hitsThisTurn: 1, AlwaysWalkable);
        Assert.False(result.KnockedBack);
        Assert.Equal(5, result.NewX);
        Assert.Equal(4, result.NewY);
    }

    [Fact]
    public void Weight2_KnockedBack_OnSecondHit()
    {
        var result = KnockbackResolver.Resolve(5, 5, 5, 4, weight: 2, hitsThisTurn: 2, AlwaysWalkable);
        Assert.True(result.KnockedBack);
        Assert.Equal(5, result.NewX);
        Assert.Equal(3, result.NewY);
    }

    [Fact]
    public void Weight3_RequiresThreeHits()
    {
        var r1 = KnockbackResolver.Resolve(5, 5, 5, 4, weight: 3, hitsThisTurn: 1, AlwaysWalkable);
        Assert.False(r1.KnockedBack);
        var r2 = KnockbackResolver.Resolve(5, 5, 5, 4, weight: 3, hitsThisTurn: 2, AlwaysWalkable);
        Assert.False(r2.KnockedBack);
        var r3 = KnockbackResolver.Resolve(5, 5, 5, 4, weight: 3, hitsThisTurn: 3, AlwaysWalkable);
        Assert.True(r3.KnockedBack);
    }

    // Direction tests -- all 4 cardinal directions
    [Fact]
    public void KnockbackDirection_AttackerBelow_PushesUp()
    {
        var result = KnockbackResolver.Resolve(5, 6, 5, 5, 1, 1, AlwaysWalkable);
        Assert.True(result.KnockedBack);
        Assert.Equal(5, result.NewX);
        Assert.Equal(4, result.NewY);
    }

    [Fact]
    public void KnockbackDirection_AttackerAbove_PushesDown()
    {
        var result = KnockbackResolver.Resolve(5, 4, 5, 5, 1, 1, AlwaysWalkable);
        Assert.True(result.KnockedBack);
        Assert.Equal(5, result.NewX);
        Assert.Equal(6, result.NewY);
    }

    [Fact]
    public void KnockbackDirection_AttackerLeft_PushesRight()
    {
        var result = KnockbackResolver.Resolve(4, 5, 5, 5, 1, 1, AlwaysWalkable);
        Assert.True(result.KnockedBack);
        Assert.Equal(6, result.NewX);
        Assert.Equal(5, result.NewY);
    }

    [Fact]
    public void KnockbackDirection_AttackerRight_PushesLeft()
    {
        var result = KnockbackResolver.Resolve(6, 5, 5, 5, 1, 1, AlwaysWalkable);
        Assert.True(result.KnockedBack);
        Assert.Equal(4, result.NewX);
        Assert.Equal(5, result.NewY);
    }

    // Blocking tests
    [Fact]
    public void BlockedByWall_NoKnockback()
    {
        var result = KnockbackResolver.Resolve(5, 5, 5, 4, 1, 1, NeverWalkable);
        Assert.False(result.KnockedBack);
        Assert.Equal(5, result.NewX);
        Assert.Equal(4, result.NewY);
    }

    [Fact]
    public void BlockedBySpecificTile_NoKnockback()
    {
        Func<int, int, bool> walkable = (x, y) => !(x == 5 && y == 3);
        var result = KnockbackResolver.Resolve(5, 5, 5, 4, 1, 1, walkable);
        Assert.False(result.KnockedBack);
    }

    // Occupied tile blocking
    [Fact]
    public void BlockedByOccupiedTile_NoKnockback()
    {
        Func<int, int, bool> occupiedAt5_3 = (x, y) => !(x == 5 && y == 3);
        var result = KnockbackResolver.Resolve(5, 5, 5, 4, 1, 1, occupiedAt5_3);
        Assert.False(result.KnockedBack);
        Assert.Equal(5, result.NewX);
        Assert.Equal(4, result.NewY);
    }

    // Default weight
    [Fact]
    public void DefaultWeight1_KnockedBackOnFirstHit()
    {
        var result = KnockbackResolver.Resolve(5, 5, 5, 4, 1, 1, AlwaysWalkable);
        Assert.True(result.KnockedBack);
    }
}
