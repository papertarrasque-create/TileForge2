using System;
using System.Collections.Generic;
using Xunit;
using TileForge.Game;
using TileForge.Data;

namespace TileForge.Tests.Game;

/// <summary>
/// Integration tests for the entity turn execution loop.
/// Tests EntityAI + SimplePathfinder + GameStateManager working together,
/// simulating what GameplayScreen.ExecuteEntityTurn does.
/// </summary>
public class EntityTurnTests
{
    private LoadedMap CreateMap(int width, int height, bool[,] solidMask = null)
    {
        var cells = new string[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                cells[x + y * width] = solidMask != null && solidMask[x, y] ? "wall" : "floor";

        return new LoadedMap
        {
            Id = "test",
            Width = width,
            Height = height,
            Layers = new() { new LoadedMapLayer { Name = "base", Cells = cells } },
        };
    }

    private static readonly Dictionary<string, TileGroup> Groups = new()
    {
        ["floor"] = new TileGroup { Name = "floor", IsSolid = false },
        ["wall"] = new TileGroup { Name = "wall", IsSolid = true },
    };

    /// <summary>
    /// Simulates one entity turn, exactly as GameplayScreen.ExecuteEntityTurn does.
    /// Entities get speed-based AP (default 1). Each AP iteration calls DecideAction.
    /// </summary>
    private List<string> SimulateEntityTurn(
        GameStateManager gsm, IPathfinder pathfinder)
    {
        var messages = new List<string>();
        foreach (var entity in gsm.State.ActiveEntities)
        {
            if (!entity.IsActive) continue;
            if (!entity.Properties.ContainsKey("behavior")) continue;

            int entityAP = Math.Clamp(gsm.GetEntityIntProperty(entity, "speed", 1), 1, 3);

            while (entityAP > 0)
            {
                var action = EntityAI.DecideAction(entity, gsm.State, pathfinder);

                if (action.Type == EntityActionType.Idle)
                    break;

                switch (action.Type)
                {
                    case EntityActionType.Move:
                        entity.X = action.TargetX;
                        entity.Y = action.TargetY;
                        break;

                    case EntityActionType.Attack:
                        if (action.AttackTargetX == null)
                        {
                            var atk = gsm.GetEntityIntProperty(entity, "attack", 3);
                            var damage = CombatHelper.CalculateDamage(atk, gsm.State.Player.Defense);
                            gsm.DamagePlayer(damage);
                            messages.Add($"{entity.DefinitionName} hit you for {damage} damage!");
                        }
                        break;
                }

                entityAP--;
                if (!gsm.IsPlayerAlive()) return messages;
            }
        }
        return messages;
    }

    [Fact]
    public void ChaseEntity_MovesTowardPlayer_OverMultipleTurns()
    {
        var map = CreateMap(10, 10);
        var state = new GameState
        {
            Player = new PlayerState { X = 5, Y = 5 },
        };
        var enemy = new EntityInstance
        {
            Id = "e1", DefinitionName = "Goblin", X = 2, Y = 5,
            Properties = new() { ["behavior"] = "chase", ["health"] = "10", ["attack"] = "3" },
        };
        state.ActiveEntities.Add(enemy);

        var gsm = new GameStateManager();
        gsm.LoadState(state);
        var pathfinder = new SimplePathfinder(map, Groups, state.ActiveEntities, state.Player);

        // Turn 1: enemy at (2,5), player at (5,5) — distance 3, in range, moves toward
        SimulateEntityTurn(gsm, pathfinder);
        Assert.Equal(3, enemy.X);
        Assert.Equal(5, enemy.Y);

        // Turn 2: enemy at (3,5) — distance 2
        SimulateEntityTurn(gsm, pathfinder);
        Assert.Equal(4, enemy.X);
        Assert.Equal(5, enemy.Y);

        // Turn 3: enemy at (4,5) — distance 1, attacks instead of moving
        var messages = SimulateEntityTurn(gsm, pathfinder);
        Assert.Equal(4, enemy.X); // didn't move
        Assert.Single(messages);
        Assert.Contains("hit you for", messages[0]);
    }

    [Fact]
    public void ChaseEntity_AttacksAdjacentPlayer()
    {
        var map = CreateMap(10, 10);
        var state = new GameState
        {
            Player = new PlayerState { X = 3, Y = 3, Health = 100, MaxHealth = 100, Defense = 2, Poise = 0 },
        };
        var enemy = new EntityInstance
        {
            Id = "e1", DefinitionName = "Goblin", X = 4, Y = 3,
            Properties = new() { ["behavior"] = "chase", ["health"] = "10", ["attack"] = "6" },
        };
        state.ActiveEntities.Add(enemy);

        var gsm = new GameStateManager();
        gsm.LoadState(state);
        var pathfinder = new SimplePathfinder(map, Groups, state.ActiveEntities, state.Player);

        var messages = SimulateEntityTurn(gsm, pathfinder);

        // Damage = max(1, 6 - 2) = 4
        Assert.Equal(96, state.Player.Health);
        Assert.Single(messages);
        Assert.Equal("Goblin hit you for 4 damage!", messages[0]);
    }

    [Fact]
    public void ChaseEntity_OutOfAggroRange_DoesNothing()
    {
        var map = CreateMap(20, 20);
        var state = new GameState
        {
            Player = new PlayerState { X = 15, Y = 15 },
        };
        var enemy = new EntityInstance
        {
            Id = "e1", DefinitionName = "Goblin", X = 1, Y = 1,
            Properties = new() { ["behavior"] = "chase", ["aggro_range"] = "5", ["health"] = "10" },
        };
        state.ActiveEntities.Add(enemy);

        var gsm = new GameStateManager();
        gsm.LoadState(state);
        var pathfinder = new SimplePathfinder(map, Groups, state.ActiveEntities, state.Player);

        SimulateEntityTurn(gsm, pathfinder);

        // Enemy didn't move — player is too far
        Assert.Equal(1, enemy.X);
        Assert.Equal(1, enemy.Y);
    }

    [Fact]
    public void PatrolEntity_MovesBackAndForth()
    {
        var map = CreateMap(10, 10);
        var state = new GameState
        {
            Player = new PlayerState { X = 9, Y = 9 }, // far away
        };
        var guard = new EntityInstance
        {
            Id = "g1", DefinitionName = "Guard", X = 3, Y = 5,
            Properties = new()
            {
                ["behavior"] = "patrol",
                ["patrol_axis"] = "x",
                ["patrol_range"] = "2",
            },
        };
        state.ActiveEntities.Add(guard);

        var gsm = new GameStateManager();
        gsm.LoadState(state);
        var pathfinder = new SimplePathfinder(map, Groups, state.ActiveEntities, state.Player);

        // Turn 1: moves +1 along X (patrol_dir starts at 1)
        SimulateEntityTurn(gsm, pathfinder);
        Assert.Equal(4, guard.X);

        // Turn 2: moves +1 along X
        SimulateEntityTurn(gsm, pathfinder);
        Assert.Equal(5, guard.X);

        // Turn 3: at range limit (origin 3, range 2, pos 5) → reverses
        SimulateEntityTurn(gsm, pathfinder);
        Assert.Equal(4, guard.X);

        // Turn 4: continues backward
        SimulateEntityTurn(gsm, pathfinder);
        Assert.Equal(3, guard.X);
    }

    [Fact]
    public void EntityBlockedByWall_CannotMove()
    {
        var solid = new bool[5, 5];
        solid[3, 2] = true; // wall blocking path

        var map = CreateMap(5, 5, solid);
        var state = new GameState
        {
            Player = new PlayerState { X = 4, Y = 2 },
        };
        var enemy = new EntityInstance
        {
            Id = "e1", DefinitionName = "Goblin", X = 2, Y = 2,
            Properties = new() { ["behavior"] = "chase", ["health"] = "10", ["attack"] = "3" },
        };
        state.ActiveEntities.Add(enemy);

        var gsm = new GameStateManager();
        gsm.LoadState(state);
        var pathfinder = new SimplePathfinder(map, Groups, state.ActiveEntities, state.Player);

        // Enemy tries to chase but wall at (3,2) blocks primary X axis
        // Secondary axis Y: distance is 0, so no Y step
        // Pathfinder should try Y axis fallback
        SimulateEntityTurn(gsm, pathfinder);

        // Enemy should try alternate path or stay put
        // With SimplePathfinder: primary is X (blocked), secondary Y has signY=0 → null → idle
        Assert.Equal(2, enemy.X);
        Assert.Equal(2, enemy.Y);
    }

    [Fact]
    public void EntityAttack_KillsPlayer()
    {
        var map = CreateMap(10, 10);
        var state = new GameState
        {
            Player = new PlayerState { X = 3, Y = 3, Health = 3, MaxHealth = 100, Defense = 0, Poise = 0 },
        };
        var enemy = new EntityInstance
        {
            Id = "e1", DefinitionName = "Dragon", X = 4, Y = 3,
            Properties = new() { ["behavior"] = "chase", ["health"] = "50", ["attack"] = "10" },
        };
        state.ActiveEntities.Add(enemy);

        var gsm = new GameStateManager();
        gsm.LoadState(state);
        var pathfinder = new SimplePathfinder(map, Groups, state.ActiveEntities, state.Player);

        SimulateEntityTurn(gsm, pathfinder);

        Assert.Equal(0, state.Player.Health);
        Assert.False(gsm.IsPlayerAlive());
    }

    [Fact]
    public void MultipleEntities_AllActInOneTurn()
    {
        var map = CreateMap(10, 10);
        var state = new GameState
        {
            Player = new PlayerState { X = 5, Y = 5 },
        };
        var enemy1 = new EntityInstance
        {
            Id = "e1", DefinitionName = "Goblin", X = 3, Y = 5,
            Properties = new() { ["behavior"] = "chase", ["health"] = "10" },
        };
        var enemy2 = new EntityInstance
        {
            Id = "e2", DefinitionName = "Orc", X = 5, Y = 2,
            Properties = new() { ["behavior"] = "chase", ["health"] = "15" },
        };
        state.ActiveEntities.Add(enemy1);
        state.ActiveEntities.Add(enemy2);

        var gsm = new GameStateManager();
        gsm.LoadState(state);
        var pathfinder = new SimplePathfinder(map, Groups, state.ActiveEntities, state.Player);

        SimulateEntityTurn(gsm, pathfinder);

        // Both enemies moved toward player
        Assert.Equal(4, enemy1.X); // was 3, moved +1 X
        Assert.Equal(3, enemy2.Y); // was 2, moved +1 Y
    }

    [Fact]
    public void InactiveEntity_IsSkipped()
    {
        var map = CreateMap(10, 10);
        var state = new GameState
        {
            Player = new PlayerState { X = 5, Y = 5 },
        };
        var enemy = new EntityInstance
        {
            Id = "e1", DefinitionName = "Goblin", X = 4, Y = 5,
            Properties = new() { ["behavior"] = "chase", ["health"] = "10", ["attack"] = "5" },
            IsActive = false,
        };
        state.ActiveEntities.Add(enemy);

        var gsm = new GameStateManager();
        gsm.LoadState(state);
        var pathfinder = new SimplePathfinder(map, Groups, state.ActiveEntities, state.Player);

        var messages = SimulateEntityTurn(gsm, pathfinder);

        // Inactive entity doesn't act — no damage, no movement
        Assert.Equal(4, enemy.X);
        Assert.Equal(100, state.Player.Health);
        Assert.Empty(messages);
    }

    [Fact]
    public void EntityWithoutBehavior_IsSkipped()
    {
        var map = CreateMap(10, 10);
        var state = new GameState
        {
            Player = new PlayerState { X = 5, Y = 5 },
        };
        var npc = new EntityInstance
        {
            Id = "n1", DefinitionName = "Villager", X = 4, Y = 5,
            Properties = new() { ["dialogue"] = "Hello!" },
        };
        state.ActiveEntities.Add(npc);

        var gsm = new GameStateManager();
        gsm.LoadState(state);
        var pathfinder = new SimplePathfinder(map, Groups, state.ActiveEntities, state.Player);

        SimulateEntityTurn(gsm, pathfinder);

        // NPC without behavior doesn't move
        Assert.Equal(4, npc.X);
    }

    [Fact]
    public void ChasePatrol_SwitchesModes()
    {
        var map = CreateMap(20, 5);
        var state = new GameState
        {
            Player = new PlayerState { X = 15, Y = 2 }, // far away
        };
        var enemy = new EntityInstance
        {
            Id = "e1", DefinitionName = "Sentry", X = 5, Y = 2,
            Properties = new()
            {
                ["behavior"] = "chase_patrol",
                ["aggro_range"] = "4",
                ["patrol_axis"] = "x",
                ["patrol_range"] = "2",
                ["health"] = "10",
                ["attack"] = "3",
            },
        };
        state.ActiveEntities.Add(enemy);

        var gsm = new GameStateManager();
        gsm.LoadState(state);
        var pathfinder = new SimplePathfinder(map, Groups, state.ActiveEntities, state.Player);

        // Player out of range — entity patrols
        SimulateEntityTurn(gsm, pathfinder);
        Assert.Equal(6, enemy.X); // patrol +1

        // Move player into aggro range
        state.Player.X = 9;

        SimulateEntityTurn(gsm, pathfinder);
        Assert.Equal(7, enemy.X); // chasing toward player at 9
    }

    [Fact]
    public void BumpAttack_DamagesEntity()
    {
        var gsm = new GameStateManager();
        var state = new GameState
        {
            Player = new PlayerState { X = 3, Y = 3, Attack = 5, Defense = 2 },
        };
        var enemy = new EntityInstance
        {
            Id = "e1", DefinitionName = "Goblin", X = 4, Y = 3,
            Properties = new()
            {
                ["health"] = "20",
                ["max_health"] = "20",
                ["defense"] = "1",
                ["attack"] = "3",
                ["behavior"] = "chase",
            },
        };
        state.ActiveEntities.Add(enemy);
        gsm.LoadState(state);

        var groups = new Dictionary<string, TileGroup>
        {
            ["Goblin"] = new TileGroup { Name = "Goblin", EntityType = EntityType.NPC },
        };

        // Player bumps into enemy
        Assert.True(gsm.IsAttackable(enemy, groups));
        var result = gsm.AttackEntity(enemy, state.Player.Attack);

        // damage = max(1, 5 - 1) = 4
        Assert.Equal(4, result.DamageDealt);
        Assert.Equal(16, result.RemainingHealth);
        Assert.False(result.Killed);
        Assert.Contains("Hit Goblin for 4!", result.Message);
    }

    [Fact]
    public void BumpAttack_KillsEntity()
    {
        var gsm = new GameStateManager();
        var state = new GameState
        {
            Player = new PlayerState { X = 3, Y = 3, Attack = 10 },
        };
        var enemy = new EntityInstance
        {
            Id = "e1", DefinitionName = "Rat", X = 4, Y = 3,
            Properties = new()
            {
                ["health"] = "3",
                ["max_health"] = "3",
                ["defense"] = "0",
                ["xp"] = "5",
            },
        };
        state.ActiveEntities.Add(enemy);
        gsm.LoadState(state);

        var groups = new Dictionary<string, TileGroup>
        {
            ["Rat"] = new TileGroup { Name = "Rat", EntityType = EntityType.NPC },
        };

        var result = gsm.AttackEntity(enemy, state.Player.Attack);

        Assert.True(result.Killed);
        Assert.False(enemy.IsActive);
        Assert.Equal(0, result.RemainingHealth);
        Assert.Contains("defeated", result.Message);
        Assert.Contains("+5 XP", result.Message);
    }

    [Fact]
    public void EntityEntityCollision_EntitiesDoNotOverlap()
    {
        var map = CreateMap(10, 10);
        var state = new GameState
        {
            Player = new PlayerState { X = 5, Y = 5 },
        };
        // Two enemies chasing player from the same direction
        var enemy1 = new EntityInstance
        {
            Id = "e1", DefinitionName = "Goblin", X = 2, Y = 5,
            Properties = new() { ["behavior"] = "chase", ["health"] = "10" },
        };
        var enemy2 = new EntityInstance
        {
            Id = "e2", DefinitionName = "Orc", X = 1, Y = 5,
            Properties = new() { ["behavior"] = "chase", ["health"] = "15" },
        };
        state.ActiveEntities.Add(enemy1);
        state.ActiveEntities.Add(enemy2);

        var gsm = new GameStateManager();
        gsm.LoadState(state);
        var pathfinder = new SimplePathfinder(map, Groups, state.ActiveEntities, state.Player);

        // Turn 1: enemy1 moves first to (3,5), then enemy2 tries to move to (2,5)
        SimulateEntityTurn(gsm, pathfinder);

        // They should not occupy the same tile
        Assert.False(enemy1.X == enemy2.X && enemy1.Y == enemy2.Y,
            "Entities should not overlap after moving");
    }

    [Fact]
    public void DamageFormula_FloorOfOne()
    {
        // Entity with very high defense still takes at least 1 damage
        var gsm = new GameStateManager();
        var state = new GameState
        {
            Player = new PlayerState { X = 3, Y = 3, Attack = 1 },
        };
        var enemy = new EntityInstance
        {
            Id = "e1", DefinitionName = "Golem", X = 4, Y = 3,
            Properties = new()
            {
                ["health"] = "100",
                ["max_health"] = "100",
                ["defense"] = "50",
            },
        };
        state.ActiveEntities.Add(enemy);
        gsm.LoadState(state);

        var result = gsm.AttackEntity(enemy, state.Player.Attack);

        // damage = max(1, 1 - 50) = 1
        Assert.Equal(1, result.DamageDealt);
        Assert.Equal(99, result.RemainingHealth);
    }
}
