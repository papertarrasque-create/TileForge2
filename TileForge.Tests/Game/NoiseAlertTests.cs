using System;
using System.Collections.Generic;
using TileForge.Data;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class NoiseAlertTests
{
    private class StubPathfinder : IPathfinder
    {
        public (int x, int y)? GetNextStep(int fromX, int fromY, int toX, int toY)
        {
            int dx = Math.Sign(toX - fromX);
            int dy = Math.Sign(toY - fromY);
            if (dx != 0) return (fromX + dx, fromY);
            if (dy != 0) return (fromX, fromY + dy);
            return null;
        }

        public bool HasLineOfSight(int fromX, int fromY, int toX, int toY) => true;
    }

    [Fact]
    public void EntityAI_AlertDoublesAggroRange_InDecideChase()
    {
        var entity = new EntityInstance
        {
            Id = "e1", DefinitionName = "guard", X = 0, Y = 0, IsActive = true,
            Properties = new Dictionary<string, string>
            {
                ["behavior"] = "chase", ["aggro_range"] = "3", ["alert_turns"] = "2"
            }
        };
        var state = new GameState
        {
            Player = new PlayerState { X = 5, Y = 0 } // distance = 5, > 3 normal, <= 6 alerted
        };

        var action = EntityAI.DecideAction(entity, state, new StubPathfinder());
        Assert.Equal(EntityActionType.Move, action.Type);
    }

    [Fact]
    public void EntityAI_NoAlert_OutOfRange_ReturnsIdle()
    {
        var entity = new EntityInstance
        {
            Id = "e1", DefinitionName = "guard", X = 0, Y = 0, IsActive = true,
            Properties = new Dictionary<string, string>
            {
                ["behavior"] = "chase", ["aggro_range"] = "3"
            }
        };
        var state = new GameState
        {
            Player = new PlayerState { X = 5, Y = 0 } // distance = 5, > 3
        };

        var action = EntityAI.DecideAction(entity, state, new StubPathfinder());
        Assert.Equal(EntityActionType.Idle, action.Type);
    }

    [Fact]
    public void EntityAI_AlertDoublesAggroRange_InDecideChasePatrol()
    {
        var entity = new EntityInstance
        {
            Id = "e1", DefinitionName = "guard", X = 0, Y = 0, IsActive = true,
            Properties = new Dictionary<string, string>
            {
                ["behavior"] = "chase_patrol", ["aggro_range"] = "3", ["alert_turns"] = "1"
            }
        };
        var state = new GameState
        {
            Player = new PlayerState { X = 5, Y = 0 }
        };

        var action = EntityAI.DecideAction(entity, state, new StubPathfinder());
        // Alerted (range 6) — should chase, not patrol
        Assert.Equal(EntityActionType.Move, action.Type);
    }

    [Fact]
    public void EntityAI_AlertZero_NotDoubled()
    {
        var entity = new EntityInstance
        {
            Id = "e1", DefinitionName = "guard", X = 0, Y = 0, IsActive = true,
            Properties = new Dictionary<string, string>
            {
                ["behavior"] = "chase", ["aggro_range"] = "3", ["alert_turns"] = "0"
            }
        };
        var state = new GameState
        {
            Player = new PlayerState { X = 5, Y = 0 }
        };

        var action = EntityAI.DecideAction(entity, state, new StubPathfinder());
        Assert.Equal(EntityActionType.Idle, action.Type);
    }

    [Fact]
    public void AlertTurns_Decremented_BySetEntityIntProperty()
    {
        var mgr = new GameStateManager();
        var entity = new EntityInstance
        {
            Id = "e1", DefinitionName = "guard", X = 0, Y = 0, IsActive = true,
            Properties = new Dictionary<string, string> { ["alert_turns"] = "3" }
        };

        int alertTurns = mgr.GetEntityIntProperty(entity, "alert_turns", 0);
        Assert.Equal(3, alertTurns);

        mgr.SetEntityIntProperty(entity, "alert_turns", alertTurns - 1);
        Assert.Equal(2, mgr.GetEntityIntProperty(entity, "alert_turns", 0));
    }

    [Fact]
    public void AlertTurns_Expires_AtZero()
    {
        var entity = new EntityInstance
        {
            Id = "e1", DefinitionName = "guard", X = 0, Y = 0, IsActive = true,
            Properties = new Dictionary<string, string>
            {
                ["behavior"] = "chase", ["aggro_range"] = "3", ["alert_turns"] = "0"
            }
        };
        var state = new GameState
        {
            Player = new PlayerState { X = 5, Y = 0 }
        };

        // alert_turns = 0, so no double — out of range
        var action = EntityAI.DecideAction(entity, state, new StubPathfinder());
        Assert.Equal(EntityActionType.Idle, action.Type);
    }

    [Fact]
    public void TileGroup_NoiseLevel_DefaultOne_BackwardCompatible()
    {
        var group = new TileGroup { Name = "stone" };
        Assert.Equal(1, group.NoiseLevel);
    }

    [Fact]
    public void TileGroup_NoiseLevel_Zero_IsSilent()
    {
        var group = new TileGroup { Name = "carpet", NoiseLevel = 0 };
        Assert.Equal(0, group.NoiseLevel);
    }

    [Fact]
    public void TileGroup_NoiseLevel_Two_IsLoud()
    {
        var group = new TileGroup { Name = "gravel", NoiseLevel = 2 };
        Assert.Equal(2, group.NoiseLevel);
    }

    [Fact]
    public void EntityAI_PatrolBehavior_UnaffectedByAlert()
    {
        // Patrol-only entities should still patrol (alert only affects chase behaviors)
        var entity = new EntityInstance
        {
            Id = "e1", DefinitionName = "guard", X = 5, Y = 5, IsActive = true,
            Properties = new Dictionary<string, string>
            {
                ["behavior"] = "patrol", ["patrol_range"] = "3", ["alert_turns"] = "3"
            }
        };
        var state = new GameState
        {
            Player = new PlayerState { X = 20, Y = 20 }
        };

        var action = EntityAI.DecideAction(entity, state, new StubPathfinder());
        // Should patrol, not chase (patrol behavior doesn't use aggro_range)
        Assert.Equal(EntityActionType.Move, action.Type);
    }

    [Fact]
    public void EntityAI_NonHostile_ChasePatrol_UnaffectedByAlert()
    {
        var entity = new EntityInstance
        {
            Id = "e1", DefinitionName = "guard", X = 0, Y = 0, IsActive = true,
            Properties = new Dictionary<string, string>
            {
                ["behavior"] = "chase_patrol", ["aggro_range"] = "3",
                ["alert_turns"] = "3", ["hostile"] = "false"
            }
        };
        var state = new GameState
        {
            Player = new PlayerState { X = 5, Y = 0 }
        };

        // Non-hostile chase_patrol becomes patrol-only — not affected by alert for chasing
        var action = EntityAI.DecideAction(entity, state, new StubPathfinder(), isHostile: false);
        Assert.Equal(EntityActionType.Move, action.Type); // patrol
    }
}
