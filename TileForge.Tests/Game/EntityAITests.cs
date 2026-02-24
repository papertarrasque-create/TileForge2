using System;
using System.Collections.Generic;
using Xunit;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class EntityAITests
{
    // -------------------------------------------------------------------------
    // MockPathfinder
    // -------------------------------------------------------------------------

    private class MockPathfinder : IPathfinder
    {
        public Dictionary<(int, int, int, int), (int, int)?> Steps { get; } = new();
        public bool DefaultHasLOS { get; set; } = true;

        public (int x, int y)? GetNextStep(int fromX, int fromY, int toX, int toY)
        {
            if (Steps.TryGetValue((fromX, fromY, toX, toY), out var result))
                return result;
            // Default: step one tile toward target on primary axis
            int dx = Math.Sign(toX - fromX);
            int dy = Math.Sign(toY - fromY);
            if (Math.Abs(toX - fromX) >= Math.Abs(toY - fromY) && dx != 0)
                return (fromX + dx, fromY);
            if (dy != 0)
                return (fromX, fromY + dy);
            return null;
        }

        public bool HasLineOfSight(int fromX, int fromY, int toX, int toY) => DefaultHasLOS;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private GameState CreateState(int playerX = 0, int playerY = 0)
    {
        return new GameState
        {
            Player = new PlayerState { X = playerX, Y = playerY },
        };
    }

    private EntityInstance CreateEntity(int x = 5, int y = 5, Dictionary<string, string> props = null)
    {
        return new EntityInstance
        {
            Id = "entity1",
            DefinitionName = "Enemy",
            X = x,
            Y = y,
            Properties = props ?? new Dictionary<string, string>(),
        };
    }

    // -------------------------------------------------------------------------
    // No behavior
    // -------------------------------------------------------------------------

    [Fact]
    public void NoBehaviorProperty_ReturnsIdle()
    {
        var entity = CreateEntity();
        var state = CreateState();
        var pathfinder = new MockPathfinder();

        var action = EntityAI.DecideAction(entity, state, pathfinder);

        Assert.Equal(EntityActionType.Idle, action.Type);
    }

    // -------------------------------------------------------------------------
    // Idle behavior
    // -------------------------------------------------------------------------

    [Fact]
    public void BehaviorIdle_ReturnsIdle()
    {
        var entity = CreateEntity(props: new Dictionary<string, string> { ["behavior"] = "idle" });
        var state = CreateState();
        var pathfinder = new MockPathfinder();

        var action = EntityAI.DecideAction(entity, state, pathfinder);

        Assert.Equal(EntityActionType.Idle, action.Type);
    }

    // -------------------------------------------------------------------------
    // Chase behavior
    // -------------------------------------------------------------------------

    [Fact]
    public void Chase_PlayerInRange_DistanceGreaterThanOne_MovestowardPlayer()
    {
        // Entity at (5,5), player at (5,8) — distance 3, within default aggro range 5
        var entity = CreateEntity(5, 5, new Dictionary<string, string> { ["behavior"] = "chase" });
        var state = CreateState(5, 8);
        var pathfinder = new MockPathfinder();

        var action = EntityAI.DecideAction(entity, state, pathfinder);

        Assert.Equal(EntityActionType.Move, action.Type);
        Assert.Equal(5, action.TargetX);
        Assert.Equal(6, action.TargetY);
    }

    [Fact]
    public void Chase_PlayerAdjacent_DistanceOne_MeleeAttack()
    {
        // Entity at (5,5), player at (5,6) — distance 1
        var entity = CreateEntity(5, 5, new Dictionary<string, string> { ["behavior"] = "chase" });
        var state = CreateState(5, 6);
        var pathfinder = new MockPathfinder();

        var action = EntityAI.DecideAction(entity, state, pathfinder);

        Assert.Equal(EntityActionType.Attack, action.Type);
        Assert.Null(action.AttackTargetX);
        Assert.Null(action.AttackTargetY);
    }

    [Fact]
    public void Chase_PlayerOutOfAggroRange_ReturnsIdle()
    {
        // Entity at (5,5), player at (0,0) — distance 10, outside default aggro range 5
        var entity = CreateEntity(5, 5, new Dictionary<string, string> { ["behavior"] = "chase" });
        var state = CreateState(0, 0);
        var pathfinder = new MockPathfinder();

        var action = EntityAI.DecideAction(entity, state, pathfinder);

        Assert.Equal(EntityActionType.Idle, action.Type);
    }

    [Fact]
    public void Chase_CustomAggroRange_RespectsProperty()
    {
        // Entity at (5,5), player at (5,12) — distance 7, within custom aggro_range=10
        var entity = CreateEntity(5, 5, new Dictionary<string, string>
        {
            ["behavior"] = "chase",
            ["aggro_range"] = "10",
        });
        var state = CreateState(5, 12);
        var pathfinder = new MockPathfinder();

        var action = EntityAI.DecideAction(entity, state, pathfinder);

        Assert.Equal(EntityActionType.Move, action.Type);
    }

    [Fact]
    public void Chase_CustomAggroRange_PlayerOutsideCustomRange_ReturnsIdle()
    {
        // Entity at (5,5), player at (5,8) — distance 3, outside custom aggro_range=2
        var entity = CreateEntity(5, 5, new Dictionary<string, string>
        {
            ["behavior"] = "chase",
            ["aggro_range"] = "2",
        });
        var state = CreateState(5, 8);
        var pathfinder = new MockPathfinder();

        var action = EntityAI.DecideAction(entity, state, pathfinder);

        Assert.Equal(EntityActionType.Idle, action.Type);
    }

    [Fact]
    public void Chase_PathfinderReturnsNull_ReturnsIdle()
    {
        // Entity at (5,5), player at (5,7) — in range, distance 2, but pathfinder blocked
        var entity = CreateEntity(5, 5, new Dictionary<string, string> { ["behavior"] = "chase" });
        var state = CreateState(5, 7);
        var pathfinder = new MockPathfinder();
        pathfinder.Steps[(5, 5, 5, 7)] = null;

        var action = EntityAI.DecideAction(entity, state, pathfinder);

        Assert.Equal(EntityActionType.Idle, action.Type);
    }

    // -------------------------------------------------------------------------
    // Patrol behavior
    // -------------------------------------------------------------------------

    [Fact]
    public void Patrol_DefaultXAxis_MovesInXDirection()
    {
        var entity = CreateEntity(5, 5, new Dictionary<string, string> { ["behavior"] = "patrol" });
        var state = CreateState();
        var pathfinder = new MockPathfinder();

        var action = EntityAI.DecideAction(entity, state, pathfinder);

        Assert.Equal(EntityActionType.Move, action.Type);
        // Should move along X axis
        Assert.Equal(5, action.TargetY);
        Assert.NotEqual(5, action.TargetX);
    }

    [Fact]
    public void Patrol_YAxis_MovesInYDirection()
    {
        var entity = CreateEntity(5, 5, new Dictionary<string, string>
        {
            ["behavior"] = "patrol",
            ["patrol_axis"] = "y",
        });
        var state = CreateState();
        var pathfinder = new MockPathfinder();

        var action = EntityAI.DecideAction(entity, state, pathfinder);

        Assert.Equal(EntityActionType.Move, action.Type);
        // Should move along Y axis
        Assert.Equal(5, action.TargetX);
        Assert.NotEqual(5, action.TargetY);
    }

    [Fact]
    public void Patrol_ReachesRange_ReversesDirection()
    {
        // Entity at (8,5) with patrol_origin=5, patrol_range=3, dir=1
        // That means next step would be (9,5) — 4 away from origin, out of range → reverse
        var entity = CreateEntity(8, 5, new Dictionary<string, string>
        {
            ["behavior"] = "patrol",
            ["patrol_origin"] = "5",
            ["patrol_range"] = "3",
            ["patrol_dir"] = "1",
        });
        var state = CreateState();
        var pathfinder = new MockPathfinder();

        var action = EntityAI.DecideAction(entity, state, pathfinder);

        Assert.Equal(EntityActionType.Move, action.Type);
        // Direction should reverse, so entity moves to (7,5)
        Assert.Equal(7, action.TargetX);
        Assert.Equal(5, action.TargetY);
        Assert.Equal("-1", entity.Properties["patrol_dir"]);
    }

    [Fact]
    public void Patrol_BlockedByWall_ReversesDirection()
    {
        // Entity at (5,5), patrol along X, dir=1 (toward 6,5), but that is blocked
        var entity = CreateEntity(5, 5, new Dictionary<string, string>
        {
            ["behavior"] = "patrol",
            ["patrol_origin"] = "5",
            ["patrol_range"] = "3",
            ["patrol_dir"] = "1",
        });
        var state = CreateState();
        var pathfinder = new MockPathfinder();
        // Block the step toward (6,5)
        pathfinder.Steps[(5, 5, 6, 5)] = null;

        var action = EntityAI.DecideAction(entity, state, pathfinder);

        Assert.Equal(EntityActionType.Move, action.Type);
        // Direction reversed, moves to (4,5)
        Assert.Equal(4, action.TargetX);
        Assert.Equal(5, action.TargetY);
        Assert.Equal("-1", entity.Properties["patrol_dir"]);
    }

    [Fact]
    public void Patrol_BothDirectionsBlocked_ReturnsIdle()
    {
        // Entity at (5,5), both (6,5) and (4,5) blocked
        var entity = CreateEntity(5, 5, new Dictionary<string, string>
        {
            ["behavior"] = "patrol",
            ["patrol_origin"] = "5",
            ["patrol_range"] = "3",
            ["patrol_dir"] = "1",
        });
        var state = CreateState();
        var pathfinder = new MockPathfinder();
        pathfinder.Steps[(5, 5, 6, 5)] = null;
        pathfinder.Steps[(5, 5, 4, 5)] = null;

        var action = EntityAI.DecideAction(entity, state, pathfinder);

        Assert.Equal(EntityActionType.Idle, action.Type);
    }

    [Fact]
    public void Patrol_PatrolOriginSetOnFirstDecision()
    {
        // Entity at (5,5) with no patrol_origin set yet
        var entity = CreateEntity(5, 5, new Dictionary<string, string> { ["behavior"] = "patrol" });
        var state = CreateState();
        var pathfinder = new MockPathfinder();

        EntityAI.DecideAction(entity, state, pathfinder);

        Assert.True(entity.Properties.ContainsKey("patrol_origin"));
        Assert.Equal("5", entity.Properties["patrol_origin"]);
    }

    [Fact]
    public void Patrol_PatrolOriginSetOnFirstDecision_YAxis()
    {
        // Entity at (3,7) with patrol_axis=y, no patrol_origin set yet
        var entity = CreateEntity(3, 7, new Dictionary<string, string>
        {
            ["behavior"] = "patrol",
            ["patrol_axis"] = "y",
        });
        var state = CreateState();
        var pathfinder = new MockPathfinder();

        EntityAI.DecideAction(entity, state, pathfinder);

        Assert.True(entity.Properties.ContainsKey("patrol_origin"));
        Assert.Equal("7", entity.Properties["patrol_origin"]);
    }

    [Fact]
    public void Patrol_PatrolDirPersistsAcrossCalls()
    {
        // Entity starts with dir=1, moves to (6,5), then on next call dir should still be 1
        var entity = CreateEntity(5, 5, new Dictionary<string, string>
        {
            ["behavior"] = "patrol",
            ["patrol_origin"] = "5",
            ["patrol_range"] = "3",
            ["patrol_dir"] = "1",
        });
        var state = CreateState();
        var pathfinder = new MockPathfinder();

        var action1 = EntityAI.DecideAction(entity, state, pathfinder);

        Assert.Equal(EntityActionType.Move, action1.Type);
        Assert.Equal("1", entity.Properties["patrol_dir"]);
    }

    [Fact]
    public void Patrol_PatrolDirDefaultIsOne()
    {
        // Entity with no patrol_dir set — should default to 1
        var entity = CreateEntity(5, 5, new Dictionary<string, string> { ["behavior"] = "patrol" });
        var state = CreateState();
        var pathfinder = new MockPathfinder();

        EntityAI.DecideAction(entity, state, pathfinder);

        Assert.True(entity.Properties.ContainsKey("patrol_dir"));
        Assert.Equal("1", entity.Properties["patrol_dir"]);
    }

    // -------------------------------------------------------------------------
    // Chase_patrol behavior
    // -------------------------------------------------------------------------

    [Fact]
    public void ChasePatrol_PlayerInRange_ActsLikeChase()
    {
        // Entity at (5,5), player at (5,7) — distance 2, within default aggro_range=5
        var entity = CreateEntity(5, 5, new Dictionary<string, string>
        {
            ["behavior"] = "chase_patrol",
            ["patrol_origin"] = "5",
            ["patrol_range"] = "3",
            ["patrol_dir"] = "1",
        });
        var state = CreateState(5, 7);
        var pathfinder = new MockPathfinder();

        var action = EntityAI.DecideAction(entity, state, pathfinder);

        // Chase behavior: moves toward player along Y
        Assert.Equal(EntityActionType.Move, action.Type);
        Assert.Equal(5, action.TargetX);
        Assert.Equal(6, action.TargetY);
    }

    [Fact]
    public void ChasePatrol_PlayerOutOfRange_ActsLikePatrol()
    {
        // Entity at (5,5), player at (5,20) — distance 15, outside default aggro_range=5
        var entity = CreateEntity(5, 5, new Dictionary<string, string>
        {
            ["behavior"] = "chase_patrol",
            ["patrol_origin"] = "5",
            ["patrol_range"] = "3",
            ["patrol_dir"] = "1",
        });
        var state = CreateState(5, 20);
        var pathfinder = new MockPathfinder();

        var action = EntityAI.DecideAction(entity, state, pathfinder);

        // Patrol behavior: moves in patrol direction
        Assert.Equal(EntityActionType.Move, action.Type);
        Assert.Equal(6, action.TargetX);
        Assert.Equal(5, action.TargetY);
    }

    [Fact]
    public void ChasePatrol_PlayerAdjacentInRange_MeleeAttack()
    {
        // Entity at (5,5), player at (5,6) — distance 1, in range
        var entity = CreateEntity(5, 5, new Dictionary<string, string>
        {
            ["behavior"] = "chase_patrol",
        });
        var state = CreateState(5, 6);
        var pathfinder = new MockPathfinder();

        var action = EntityAI.DecideAction(entity, state, pathfinder);

        Assert.Equal(EntityActionType.Attack, action.Type);
        Assert.Null(action.AttackTargetX);
        Assert.Null(action.AttackTargetY);
    }

    [Fact]
    public void ChasePatrol_PlayerEntersThenLeavesRange_SwitchesModes()
    {
        var entity = CreateEntity(5, 5, new Dictionary<string, string>
        {
            ["behavior"] = "chase_patrol",
            ["patrol_origin"] = "5",
            ["patrol_range"] = "3",
            ["patrol_dir"] = "1",
        });
        var pathfinder = new MockPathfinder();

        // Player in range
        var stateInRange = CreateState(5, 7);
        var actionChase = EntityAI.DecideAction(entity, state: stateInRange, pathfinder);
        Assert.Equal(EntityActionType.Move, actionChase.Type);
        Assert.Equal(5, actionChase.TargetX);
        Assert.Equal(6, actionChase.TargetY);

        // Player out of range
        var stateOutOfRange = CreateState(5, 20);
        var actionPatrol = EntityAI.DecideAction(entity, state: stateOutOfRange, pathfinder);
        Assert.Equal(EntityActionType.Move, actionPatrol.Type);
        // Patrol continues along X
        Assert.Equal(5, actionPatrol.TargetY);
    }

    // -------------------------------------------------------------------------
    // Unknown behavior
    // -------------------------------------------------------------------------

    [Fact]
    public void UnknownBehavior_ReturnsIdle()
    {
        var entity = CreateEntity(props: new Dictionary<string, string> { ["behavior"] = "unknown_value" });
        var state = CreateState();
        var pathfinder = new MockPathfinder();

        var action = EntityAI.DecideAction(entity, state, pathfinder);

        Assert.Equal(EntityActionType.Idle, action.Type);
    }
}
