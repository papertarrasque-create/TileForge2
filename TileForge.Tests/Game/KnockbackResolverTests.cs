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
}
