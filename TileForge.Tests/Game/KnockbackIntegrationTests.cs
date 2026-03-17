using System;
using System.Collections.Generic;
using TileForge.Game;
using TileForge.Play;
using Xunit;

namespace TileForge.Tests.Game;

public class KnockbackIntegrationTests
{
    [Fact]
    public void PlayerState_Weight_DefaultsTo1()
    {
        var ps = new PlayerState();
        Assert.Equal(1, ps.Weight);
    }
}
