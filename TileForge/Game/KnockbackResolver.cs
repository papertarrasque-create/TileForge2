using System;

namespace TileForge.Game;

public readonly struct KnockbackResult
{
    public bool KnockedBack { get; }
    public int NewX { get; }
    public int NewY { get; }

    public KnockbackResult(bool knockedBack, int newX, int newY)
    {
        KnockedBack = knockedBack;
        NewX = newX;
        NewY = newY;
    }

    public static KnockbackResult None(int currentX, int currentY) =>
        new(false, currentX, currentY);
}

public static class KnockbackResolver
{
    /// <summary>
    /// Determines if a defender should be knocked back after being hit.
    /// Pure function: no side effects, no MonoGame dependencies.
    /// </summary>
    public static KnockbackResult Resolve(
        int attackerX, int attackerY,
        int defenderX, int defenderY,
        int weight,
        int hitsThisTurn,
        Func<int, int, bool> isWalkable)
    {
        if (hitsThisTurn < weight)
            return KnockbackResult.None(defenderX, defenderY);

        int dx = defenderX - attackerX;
        int dy = defenderY - attackerY;

        int targetX = defenderX + dx;
        int targetY = defenderY + dy;

        if (!isWalkable(targetX, targetY))
            return KnockbackResult.None(defenderX, defenderY);

        return new KnockbackResult(true, targetX, targetY);
    }
}
