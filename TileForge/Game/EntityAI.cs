using System;

namespace TileForge.Game;

public static class EntityAI
{
    public static EntityAction DecideAction(
        EntityInstance entity,
        GameState state,
        IPathfinder pathfinder)
    {
        if (!entity.Properties.TryGetValue("behavior", out var behavior))
            return EntityAction.Idle();

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
        int aggroRange = 5;
        if (entity.Properties.TryGetValue("aggro_range", out var rangeStr) && int.TryParse(rangeStr, out var r))
            aggroRange = r;

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
        bool isXAxis = true;  // default patrol along X
        if (entity.Properties.TryGetValue("patrol_axis", out var axis))
            isXAxis = axis != "y";

        int patrolRange = 3;
        if (entity.Properties.TryGetValue("patrol_range", out var prStr) && int.TryParse(prStr, out var pr))
            patrolRange = pr;

        // Read or initialize patrol origin (set once on first decision)
        int origin;
        if (entity.Properties.TryGetValue("patrol_origin", out var originStr) && int.TryParse(originStr, out var o))
        {
            origin = o;
        }
        else
        {
            origin = isXAxis ? entity.X : entity.Y;
            entity.Properties["patrol_origin"] = origin.ToString();
        }

        // Read or initialize patrol direction
        int dir = 1;
        if (entity.Properties.TryGetValue("patrol_dir", out var dirStr) && int.TryParse(dirStr, out var d))
            dir = d;
        else
            entity.Properties["patrol_dir"] = "1";

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
            entity.Properties["patrol_dir"] = dir.ToString();

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
        int aggroRange = 5;
        if (entity.Properties.TryGetValue("aggro_range", out var rangeStr) && int.TryParse(rangeStr, out var r))
            aggroRange = r;

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
