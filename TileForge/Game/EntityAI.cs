using System;

namespace TileForge.Game;

public static class EntityAI
{
    public static EntityAction DecideAction(
        EntityInstance entity,
        GameState state,
        IPathfinder pathfinder,
        bool isHostile = true)
    {
        var behavior = PropertyAccess.GetString(entity.Properties, PropertyKeys.Behavior);
        if (string.IsNullOrEmpty(behavior))
            return EntityAction.Idle();

        if (!isHostile)
        {
            // Non-hostile: chase becomes idle, chase_patrol becomes patrol-only
            return behavior switch
            {
                "patrol" => DecidePatrol(entity, state, pathfinder),
                "chase_patrol" => DecidePatrol(entity, state, pathfinder),
                _ => EntityAction.Idle(),
            };
        }

        return behavior switch
        {
            "idle" => EntityAction.Idle(),
            "chase" => DecideChase(entity, state, pathfinder),
            "patrol" => DecidePatrol(entity, state, pathfinder),
            "chase_patrol" => DecideChasePatrol(entity, state, pathfinder),
            _ => EntityAction.Idle(),
        };
    }

    private static EntityAction DecideChase(EntityInstance entity, GameState state, IPathfinder pathfinder)
    {
        int aggroRange = PropertyAccess.GetInt(entity.Properties, PropertyKeys.AggroRange, 5);

        // Alert doubles effective aggro range
        if (PropertyAccess.GetInt(entity.Properties, PropertyKeys.AlertTurns, 0) > 0)
            aggroRange *= 2;

        int dx = state.Player.X - entity.X;
        int dy = state.Player.Y - entity.Y;
        int distance = Math.Abs(dx) + Math.Abs(dy);  // Manhattan distance

        if (distance > aggroRange)
            return EntityAction.Idle();

        if (distance == 1)
            return EntityAction.MeleeAttack();

        var step = pathfinder.GetNextStep(entity.X, entity.Y, state.Player.X, state.Player.Y);
        if (step != null)
            return EntityAction.MoveTo(step.Value.x, step.Value.y);

        return EntityAction.Idle();
    }

    private static EntityAction DecidePatrol(EntityInstance entity, GameState state, IPathfinder pathfinder)
    {
        // Read patrol config from properties
        var axis = PropertyAccess.GetString(entity.Properties, PropertyKeys.PatrolAxis);
        bool isXAxis = axis != "y";  // default patrol along X

        int patrolRange = PropertyAccess.GetInt(entity.Properties, PropertyKeys.PatrolRange, 3);

        // Read or initialize patrol origin (set once on first decision)
        int origin;
        if (entity.Properties.TryGetValue(PropertyKeys.PatrolOrigin, out var originStr) && int.TryParse(originStr, out var o))
        {
            origin = o;
        }
        else
        {
            origin = isXAxis ? entity.X : entity.Y;
            PropertyAccess.SetInt(entity.Properties, PropertyKeys.PatrolOrigin, origin);
        }

        // Read or initialize patrol direction
        int dir = PropertyAccess.GetInt(entity.Properties, PropertyKeys.PatrolDir, 0);
        if (dir == 0)
        {
            dir = 1;
            PropertyAccess.SetInt(entity.Properties, PropertyKeys.PatrolDir, dir);
        }

        // Calculate next position
        int nextX = entity.X;
        int nextY = entity.Y;
        if (isXAxis) nextX += dir;
        else nextY += dir;

        int currentOnAxis = isXAxis ? nextX : nextY;

        // Check range bounds or blocked
        bool outOfRange = Math.Abs(currentOnAxis - origin) > patrolRange;
        bool blocked = outOfRange || !IsStepWalkable(nextX, nextY, pathfinder, entity);

        if (blocked)
        {
            // Reverse direction
            dir = -dir;
            PropertyAccess.SetInt(entity.Properties, PropertyKeys.PatrolDir, dir);

            nextX = entity.X;
            nextY = entity.Y;
            if (isXAxis) nextX += dir;
            else nextY += dir;

            currentOnAxis = isXAxis ? nextX : nextY;
            outOfRange = Math.Abs(currentOnAxis - origin) > patrolRange;
            blocked = outOfRange || !IsStepWalkable(nextX, nextY, pathfinder, entity);

            if (blocked)
                return EntityAction.Idle();
        }

        return EntityAction.MoveTo(nextX, nextY);
    }

    private static EntityAction DecideChasePatrol(EntityInstance entity, GameState state, IPathfinder pathfinder)
    {
        int aggroRange = PropertyAccess.GetInt(entity.Properties, PropertyKeys.AggroRange, 5);

        // Alert doubles effective aggro range
        if (PropertyAccess.GetInt(entity.Properties, PropertyKeys.AlertTurns, 0) > 0)
            aggroRange *= 2;

        int dx = state.Player.X - entity.X;
        int dy = state.Player.Y - entity.Y;
        int distance = Math.Abs(dx) + Math.Abs(dy);

        if (distance <= aggroRange)
        {
            // In aggro range → chase behavior
            return DecideChase(entity, state, pathfinder);
        }
        else
        {
            // Outside aggro range → patrol behavior
            return DecidePatrol(entity, state, pathfinder);
        }
    }

    private static bool IsStepWalkable(int x, int y, IPathfinder pathfinder, EntityInstance entity)
    {
        var step = pathfinder.GetNextStep(entity.X, entity.Y, x, y);
        return step != null && step.Value.x == x && step.Value.y == y;
    }
}
