using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Game;
using TileForge.Play;
using TileForge.Tests.Helpers;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.Game;

/// <summary>
/// Tests for the AP (Action Point) combat system:
/// - Player gets 2 AP per turn (MaxAP)
/// - Move = 1 AP, Bump attack = 1 AP
/// - Entities get speed-based AP (1-3)
/// - Auto-end turn when no hostiles nearby
/// </summary>
public class APCombatTests
{
    private static readonly Func<Rectangle> DefaultCanvasBounds = () => new Rectangle(0, 0, 800, 600);

    // =========================================================================
    // Data model tests
    // =========================================================================

    [Fact]
    public void PlayerState_MaxAP_DefaultsTo2()
    {
        var ps = new PlayerState();
        Assert.Equal(2, ps.MaxAP);
    }

    [Fact]
    public void PlayState_AP_DefaultValues()
    {
        var play = new PlayState();
        Assert.Equal(0, play.PlayerAP);
        Assert.True(play.IsPlayerTurn);
    }

    [Fact]
    public void GetFacingTile_Right()
    {
        var play = new PlayState
        {
            PlayerEntity = new Entity { X = 5, Y = 5 },
            PlayerFacing = Direction.Right,
        };
        var (x, y) = play.GetFacingTile();
        Assert.Equal(6, x);
        Assert.Equal(5, y);
    }

    [Fact]
    public void GetFacingTile_Left()
    {
        var play = new PlayState
        {
            PlayerEntity = new Entity { X = 5, Y = 5 },
            PlayerFacing = Direction.Left,
        };
        var (x, y) = play.GetFacingTile();
        Assert.Equal(4, x);
        Assert.Equal(5, y);
    }

    [Fact]
    public void GetFacingTile_Up()
    {
        var play = new PlayState
        {
            PlayerEntity = new Entity { X = 5, Y = 5 },
            PlayerFacing = Direction.Up,
        };
        var (x, y) = play.GetFacingTile();
        Assert.Equal(5, x);
        Assert.Equal(4, y);
    }

    [Fact]
    public void GetFacingTile_Down()
    {
        var play = new PlayState
        {
            PlayerEntity = new Entity { X = 5, Y = 5 },
            PlayerFacing = Direction.Down,
        };
        var (x, y) = play.GetFacingTile();
        Assert.Equal(5, x);
        Assert.Equal(6, y);
    }

    [Fact]
    public void GetEffectiveMaxAP_ReturnsBaseWithoutEquipment()
    {
        var gsm = new GameStateManager();
        gsm.LoadState(new GameState { Player = new PlayerState { MaxAP = 2 } });
        Assert.Equal(2, gsm.GetEffectiveMaxAP());
    }

    [Fact]
    public void GetEffectiveMaxAP_IncludesEquipmentBonus()
    {
        var gsm = new GameStateManager();
        var state = new GameState
        {
            Player = new PlayerState
            {
                MaxAP = 2,
                Equipment = new() { ["Accessory"] = "boots_of_haste" },
            },
            ItemPropertyCache = new()
            {
                ["boots_of_haste"] = new() { ["equip_ap"] = "1" },
            },
        };
        gsm.LoadState(state);
        Assert.Equal(3, gsm.GetEffectiveMaxAP());
    }

    [Fact]
    public void LoadState_FixesZeroMaxAP()
    {
        var gsm = new GameStateManager();
        gsm.LoadState(new GameState { Player = new PlayerState { MaxAP = 0 } });
        Assert.Equal(2, gsm.State.Player.MaxAP);
    }

    [Fact]
    public void EndTurn_ExistsInGameActionEnum()
    {
        Assert.True(Enum.IsDefined(typeof(GameAction), GameAction.EndTurn));
    }

    [Fact]
    public void DefaultBindings_ContainEndTurn()
    {
        var input = new GameInputManager();
        var bindings = input.GetBindings();
        Assert.True(bindings.ContainsKey(GameAction.EndTurn));
    }

    // =========================================================================
    // Integration tests via PlayModeController
    // =========================================================================

    private static (EditorState state, MapCanvas canvas, PlayModeController controller)
        CreatePlaySetup(
            int playerX = 5, int playerY = 5,
            int mapWidth = 10, int mapHeight = 10,
            Action<EditorState> customize = null)
    {
        var state = new EditorState
        {
            Map = new MapData(mapWidth, mapHeight),
            Sheet = new MockSpriteSheet(16, 16),
        };

        state.AddGroup(new TileGroup
        {
            Name = "player",
            Type = GroupType.Entity,
            IsPlayer = true,
            Sprites = { new SpriteRef { Col = 0, Row = 0 } },
        });

        state.Map.Entities.Add(new Entity
        {
            Id = "player01",
            GroupName = "player",
            X = playerX,
            Y = playerY,
        });

        customize?.Invoke(state);
        state.RebuildGroupIndex();

        var canvas = new MapCanvas();
        var controller = new PlayModeController(state, canvas, DefaultCanvasBounds);
        return (state, canvas, controller);
    }

    private static void SimulateFullMove(PlayModeController controller, Keys key)
    {
        var releaseTime = new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.001));
        controller.Update(releaseTime, new KeyboardState());

        var current = new KeyboardState(key);
        var startTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.01));
        controller.Update(startTime, current);

        var finishTime = new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(PlayState.MoveDuration + 0.01f));
        controller.Update(finishTime, new KeyboardState());
    }

    private static void SimulateKeyPress(PlayModeController controller, Keys key)
    {
        var releaseTime = new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.001));
        controller.Update(releaseTime, new KeyboardState());

        var current = new KeyboardState(key);
        var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.01));
        controller.Update(gameTime, current);
    }

    [Fact]
    public void PlayModeEntry_InitializesAP()
    {
        var (state, _, controller) = CreatePlaySetup();
        controller.Enter();

        Assert.Equal(2, state.PlayState.PlayerAP);
        Assert.True(state.PlayState.IsPlayerTurn);
    }

    [Fact]
    public void Move_NoHostiles_AutoEndsTurn_APRefills()
    {
        // Move with no hostiles nearby: AP should auto-refill after each move
        var (state, _, controller) = CreatePlaySetup(playerX: 5, playerY: 5);
        controller.Enter();

        Assert.Equal(2, state.PlayState.PlayerAP);

        // Move right — no hostiles, auto-end-turn triggers, AP refills
        SimulateFullMove(controller, Keys.Right);

        Assert.Equal(6, state.PlayState.PlayerEntity.X);
        Assert.Equal(2, state.PlayState.PlayerAP); // Refilled because no hostiles
        Assert.True(state.PlayState.IsPlayerTurn);
    }

    [Fact]
    public void Move_HostileNearby_APDeducted_PlayerRetainsRemainingAP()
    {
        var (state, _, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "Goblin",
                Type = GroupType.Entity,
                EntityType = EntityType.NPC,
                IsSolid = true,
                Sprites = { new SpriteRef { Col = 1, Row = 0 } },
                DefaultProperties = new()
                {
                    ["health"] = "20",
                    ["attack"] = "3",
                    ["defense"] = "0",
                    ["behavior"] = "chase",
                    ["hostile"] = "true",
                    ["aggro_range"] = "10",
                },
            });
            s.Map.Entities.Add(new Entity
            {
                Id = "goblin01",
                GroupName = "Goblin",
                X = 8,
                Y = 5,
            });
        });
        controller.Enter();

        Assert.Equal(2, state.PlayState.PlayerAP);

        // Move right — hostile is within aggro range (distance 3 after move = within 10)
        SimulateFullMove(controller, Keys.Right);

        Assert.Equal(6, state.PlayState.PlayerEntity.X);
        Assert.Equal(1, state.PlayState.PlayerAP); // 1 AP remaining
        Assert.True(state.PlayState.IsPlayerTurn);
    }

    [Fact]
    public void BumpAttack_DeductsOneAP()
    {
        var (state, _, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "Goblin",
                Type = GroupType.Entity,
                EntityType = EntityType.NPC,
                IsSolid = true,
                Sprites = { new SpriteRef { Col = 1, Row = 0 } },
                DefaultProperties = new()
                {
                    ["health"] = "20",
                    ["attack"] = "3",
                    ["defense"] = "0",
                    ["behavior"] = "chase",
                    ["hostile"] = "true",
                    ["aggro_range"] = "10",
                },
            });
            s.Map.Entities.Add(new Entity
            {
                Id = "goblin01",
                GroupName = "Goblin",
                X = 6,
                Y = 5,
            });
        });
        controller.Enter();

        Assert.Equal(2, state.PlayState.PlayerAP);

        // Bump right into goblin — attack (1 AP), player stays at (5,5)
        SimulateKeyPress(controller, Keys.Right);

        Assert.Equal(5, state.PlayState.PlayerEntity.X); // Didn't move
        Assert.Equal(1, state.PlayState.PlayerAP); // 1 AP spent on bump attack
    }

    [Fact]
    public void TwoActions_ExhaustsAP_EntitiesAct()
    {
        // Player at (3,5), goblin at (8,5). Move right twice (2 AP), entities should act.
        var (state, _, controller) = CreatePlaySetup(playerX: 3, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "Goblin",
                Type = GroupType.Entity,
                EntityType = EntityType.NPC,
                IsSolid = true,
                Sprites = { new SpriteRef { Col = 1, Row = 0 } },
                DefaultProperties = new()
                {
                    ["health"] = "20",
                    ["attack"] = "3",
                    ["defense"] = "0",
                    ["behavior"] = "chase",
                    ["hostile"] = "true",
                    ["aggro_range"] = "10",
                },
            });
            s.Map.Entities.Add(new Entity
            {
                Id = "goblin01",
                GroupName = "Goblin",
                X = 8,
                Y = 5,
            });
        });
        controller.Enter();

        // Move 1: right to (4,5) — 1 AP left, hostile nearby
        SimulateFullMove(controller, Keys.Right);
        Assert.Equal(4, state.PlayState.PlayerEntity.X);
        Assert.Equal(1, state.PlayState.PlayerAP);

        var goblinEntity = FindActiveEntity(controller, "goblin01");
        int goblinXBefore = goblinEntity.X;

        // Move 2: right to (5,5) — 0 AP, triggers entity turn
        SimulateFullMove(controller, Keys.Right);
        Assert.Equal(5, state.PlayState.PlayerEntity.X);
        Assert.Equal(2, state.PlayState.PlayerAP); // Refilled after entity turn

        // Goblin should have moved toward player (from 8 toward 5)
        Assert.True(goblinEntity.X < goblinXBefore,
            "Entity should have acted after player exhausted AP");
    }

    [Fact]
    public void EndTurnKey_ForfeitsRemainingAP_EntitiesAct()
    {
        var (state, _, controller) = CreatePlaySetup(playerX: 5, playerY: 5, customize: s =>
        {
            s.AddGroup(new TileGroup
            {
                Name = "Goblin",
                Type = GroupType.Entity,
                EntityType = EntityType.NPC,
                IsSolid = true,
                Sprites = { new SpriteRef { Col = 1, Row = 0 } },
                DefaultProperties = new()
                {
                    ["health"] = "20",
                    ["attack"] = "3",
                    ["defense"] = "0",
                    ["behavior"] = "chase",
                    ["hostile"] = "true",
                    ["aggro_range"] = "10",
                },
            });
            s.Map.Entities.Add(new Entity
            {
                Id = "goblin01",
                GroupName = "Goblin",
                X = 8,
                Y = 5,
            });
        });
        controller.Enter();

        var goblinEntity = FindActiveEntity(controller, "goblin01");
        int goblinXBefore = goblinEntity.X;

        // Press Space to end turn — forfeit 2 AP, entities act
        SimulateKeyPress(controller, Keys.Space);

        Assert.Equal(2, state.PlayState.PlayerAP); // Refilled after entity turn
        Assert.True(state.PlayState.IsPlayerTurn);
        // Goblin should have chased toward player
        Assert.True(goblinEntity.X < goblinXBefore,
            "Entity should have acted after EndTurn");
    }

    // =========================================================================
    // Entity speed tests (standalone, no PlayModeController)
    // =========================================================================

    private static readonly Dictionary<string, TileGroup> FloorGroups = new()
    {
        ["floor"] = new TileGroup { Name = "floor", IsSolid = false },
    };

    private LoadedMap CreateMap(int width, int height)
    {
        var cells = new string[width * height];
        for (int i = 0; i < cells.Length; i++)
            cells[i] = "floor";

        return new LoadedMap
        {
            Id = "test",
            Width = width,
            Height = height,
            Layers = new() { new LoadedMapLayer { Name = "base", Cells = cells } },
        };
    }

    private List<string> SimulateEntityTurn(GameStateManager gsm, IPathfinder pathfinder)
    {
        var messages = new List<string>();
        foreach (var entity in gsm.State.ActiveEntities)
        {
            if (!entity.IsActive) continue;
            if (!entity.Properties.ContainsKey("behavior")) continue;

            int entityAP = Math.Clamp(gsm.GetEntityIntProperty(entity, "speed", 1), 1, 3);
            bool hostile = gsm.IsEntityHostile(entity);

            while (entityAP > 0)
            {
                var action = EntityAI.DecideAction(entity, gsm.State, pathfinder, hostile);
                if (action.Type == EntityActionType.Idle) break;

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
                            var damage = CombatHelper.CalculateDamage(atk, gsm.GetEffectiveDefense());
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
    public void EntitySpeed_DefaultSpeed1_SingleAction()
    {
        var map = CreateMap(10, 10);
        var state = new GameState
        {
            Player = new PlayerState { X = 5, Y = 5 },
        };
        var enemy = new EntityInstance
        {
            Id = "e1", DefinitionName = "Goblin", X = 2, Y = 5,
            Properties = new() { ["behavior"] = "chase", ["health"] = "10" },
            // No "speed" property → defaults to 1
        };
        state.ActiveEntities.Add(enemy);

        var gsm = new GameStateManager();
        gsm.LoadState(state);
        var pathfinder = new SimplePathfinder(map, FloorGroups, state.ActiveEntities, state.Player);

        SimulateEntityTurn(gsm, pathfinder);

        // Moved 1 tile (speed=1, so 1 action)
        Assert.Equal(3, enemy.X);
    }

    [Fact]
    public void EntitySpeed_Speed2_TwoActions()
    {
        var map = CreateMap(10, 10);
        var state = new GameState
        {
            Player = new PlayerState { X = 8, Y = 5 },
        };
        var enemy = new EntityInstance
        {
            Id = "e1", DefinitionName = "FastGoblin", X = 5, Y = 5,
            Properties = new()
            {
                ["behavior"] = "chase", ["health"] = "10", ["speed"] = "2",
                ["aggro_range"] = "10",
            },
        };
        state.ActiveEntities.Add(enemy);

        var gsm = new GameStateManager();
        gsm.LoadState(state);
        var pathfinder = new SimplePathfinder(map, FloorGroups, state.ActiveEntities, state.Player);

        SimulateEntityTurn(gsm, pathfinder);

        // Moved 2 tiles (speed=2, so 2 actions): 5 → 6 → 7
        Assert.Equal(7, enemy.X);
    }

    [Fact]
    public void EntitySpeed_Speed2_MoveAndAttack()
    {
        // Fast enemy at distance 2 from player: first action = move, second = attack
        var map = CreateMap(10, 10);
        var state = new GameState
        {
            Player = new PlayerState { X = 5, Y = 5, Health = 100, MaxHealth = 100, Defense = 0 },
        };
        var enemy = new EntityInstance
        {
            Id = "e1", DefinitionName = "FastGoblin", X = 3, Y = 5,
            Properties = new()
            {
                ["behavior"] = "chase", ["health"] = "10", ["attack"] = "5", ["speed"] = "2",
            },
        };
        state.ActiveEntities.Add(enemy);

        var gsm = new GameStateManager();
        gsm.LoadState(state);
        var pathfinder = new SimplePathfinder(map, FloorGroups, state.ActiveEntities, state.Player);

        var messages = SimulateEntityTurn(gsm, pathfinder);

        // First action: moved from (3,5) to (4,5)
        Assert.Equal(4, enemy.X);
        // Second action: adjacent to player at (5,5), attacks
        Assert.Single(messages);
        Assert.Contains("hit you for", messages[0]);
        Assert.True(state.Player.Health < 100);
    }

    [Fact]
    public void EntitySpeed_IdleBehavior_BreaksLoop()
    {
        var map = CreateMap(10, 10);
        var state = new GameState
        {
            Player = new PlayerState { X = 5, Y = 5 },
        };
        var enemy = new EntityInstance
        {
            Id = "e1", DefinitionName = "IdleGuard", X = 2, Y = 2,
            Properties = new()
            {
                ["behavior"] = "idle", ["health"] = "10", ["speed"] = "3",
            },
        };
        state.ActiveEntities.Add(enemy);

        var gsm = new GameStateManager();
        gsm.LoadState(state);
        var pathfinder = new SimplePathfinder(map, FloorGroups, state.ActiveEntities, state.Player);

        SimulateEntityTurn(gsm, pathfinder);

        // Idle behavior breaks the loop immediately — no movement despite speed=3
        Assert.Equal(2, enemy.X);
        Assert.Equal(2, enemy.Y);
    }

    [Fact]
    public void EntitySpeed_ClampedTo3Maximum()
    {
        var map = CreateMap(20, 10);
        var state = new GameState
        {
            Player = new PlayerState { X = 15, Y = 5 },
        };
        var enemy = new EntityInstance
        {
            Id = "e1", DefinitionName = "SuperFast", X = 10, Y = 5,
            Properties = new()
            {
                ["behavior"] = "chase", ["health"] = "10", ["speed"] = "99",
                ["aggro_range"] = "20",
            },
        };
        state.ActiveEntities.Add(enemy);

        var gsm = new GameStateManager();
        gsm.LoadState(state);
        var pathfinder = new SimplePathfinder(map, FloorGroups, state.ActiveEntities, state.Player);

        SimulateEntityTurn(gsm, pathfinder);

        // Speed clamped to 3, so should move exactly 3 tiles: 10 → 11 → 12 → 13
        Assert.Equal(13, enemy.X);
    }

    // =========================================================================
    // Helper
    // =========================================================================

    private static EntityInstance FindActiveEntity(PlayModeController controller, string entityId)
    {
        // Access via the GameStateManager exposed through the controller's EditorState
        // We use reflection-free approach: the controller's Enter() populates GameState
        var field = typeof(PlayModeController).GetField("_gameStateManager",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var gsm = (GameStateManager)field.GetValue(controller);
        foreach (var entity in gsm.State.ActiveEntities)
        {
            if (entity.Id == entityId)
                return entity;
        }
        return null;
    }
}
