namespace TileForge.Game;

public enum EntityActionType
{
    Idle,
    Move,
    Attack
}

public class EntityAction
{
    public EntityActionType Type { get; set; }
    public int TargetX { get; set; }
    public int TargetY { get; set; }
    public int? AttackTargetX { get; set; }  // null = melee (adjacent), non-null = ranged target
    public int? AttackTargetY { get; set; }

    public static EntityAction Idle() => new() { Type = EntityActionType.Idle };

    public static EntityAction MoveTo(int x, int y) => new()
    {
        Type = EntityActionType.Move,
        TargetX = x,
        TargetY = y,
    };

    public static EntityAction MeleeAttack() => new()
    {
        Type = EntityActionType.Attack,
        AttackTargetX = null,
        AttackTargetY = null,
    };
}
