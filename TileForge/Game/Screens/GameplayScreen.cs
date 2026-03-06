using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge.Data;
using TileForge.Editor;
using TileForge.Infrastructure;
using TileForge.Play;
using TileForge.UI;

namespace TileForge.Game.Screens;

/// <summary>
/// Main gameplay screen. Handles player movement, collision, hazard damage,
/// entity interaction, camera follow, and HUD rendering. Extracted from
/// PlayModeController to fit the ScreenManager architecture.
/// </summary>
public class GameplayScreen : GameScreen
{
    private readonly EditorState _state;
    private readonly MapCanvas _canvas;
    private readonly GameStateManager _gameStateManager;
    private readonly SaveManager _saveManager;
    private readonly GameInputManager _inputManager;
    private readonly string _bindingsPath;
    private readonly QuestManager _questManager;
    private readonly Func<Rectangle> _getCanvasBounds;
    private readonly EdgeTransitionResolver _edgeResolver;
    private readonly IDialogueLoader _dialogueLoader;
    private readonly GameLog _gameLog;
    private IPathfinder _pathfinder;

    // --- Cached fields to avoid per-frame allocations and redundant computation ---

    // Fix #1: Reusable dictionary for SyncEntityRenderState (avoids allocation every frame)
    private readonly Dictionary<string, EntityInstance> _activeById = new();

    // Fix #2: Cached AP text (only rebuilt when currentAP or maxAP changes)
    private string _cachedAPText = "";
    private int _cachedCurrentAP = -1;
    private int _cachedMaxAP = -1;

    // Fix #3: Cached stats text and its measured size (only rebuilt when ATK/DEF changes)
    private string _cachedStatsText = "";
    private Vector2 _cachedStatsSize;
    private int _cachedATK = -1;
    private int _cachedDEF = -1;

    // Fix #5: Cached hostile-nearby flag (computed in Update, read in Draw)
    private bool _hostileNearby;

    // Fix #6: Cached cover bonus (recomputed when player position changes)
    private int _cachedCover;
    private int _cachedCoverX = int.MinValue;
    private int _cachedCoverY = int.MinValue;

    // Fix #7: Cached player position and canvas bounds for camera centering
    private float _lastCameraRenderX = float.NaN;
    private float _lastCameraRenderY = float.NaN;
    private Rectangle _lastCanvasBounds;

    public GameplayScreen(EditorState state, MapCanvas canvas, GamePlayContext context,
        GameLog gameLog = null)
    {
        _state = state;
        _canvas = canvas;
        _gameStateManager = context.StateManager;
        _saveManager = context.SaveManager;
        _inputManager = context.InputManager;
        _bindingsPath = context.BindingsPath;
        _questManager = context.QuestManager;
        _getCanvasBounds = context.GetCanvasBounds;
        _edgeResolver = context.EdgeResolver;
        _dialogueLoader = context.DialogueLoader;
        _gameLog = gameLog;
    }

    public override void OnEnter()
    {
        _pathfinder = CreatePathfinder();
        CenterCameraOnPlayer();
    }

    internal void TriggerDamageFlash()
    {
        var play = _state.PlayState;
        if (play != null)
            play.PlayerFlashTimer = PlayState.FlashDuration;
    }

    internal void TriggerEntityFlash(string entityId)
    {
        var play = _state.PlayState;
        if (play != null)
        {
            play.EntityFlashTimer = PlayState.FlashDuration;
            play.FlashedEntityId = entityId;
        }
    }

    public override void Update(GameTime gameTime, GameInputManager input)
    {
        var play = _state.PlayState;
        if (play == null) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Tick floating messages
        for (int i = play.FloatingMessages.Count - 1; i >= 0; i--)
        {
            var fm = play.FloatingMessages[i];
            fm.Timer -= dt;
            fm.VerticalOffset += FloatingMessage.DriftPixels * dt / FloatingMessage.Duration;
            if (fm.Timer <= 0)
                play.FloatingMessages.RemoveAt(i);
        }

        if (play.PlayerFlashTimer > 0)
            play.PlayerFlashTimer -= dt;
        if (play.EntityFlashTimer > 0)
            play.EntityFlashTimer -= dt;

        if (play.IsMoving)
        {
            // Continue lerp — use per-move duration (affected by movement cost)
            play.MoveProgress += dt / play.CurrentMoveDuration;
            if (play.MoveProgress >= 1.0f)
            {
                play.MoveProgress = 1.0f;
                play.RenderPos = play.MoveTo;
                play.IsMoving = false;

                // Update entity grid position
                play.PlayerEntity.X = (int)play.MoveTo.X;
                play.PlayerEntity.Y = (int)play.MoveTo.Y;

                // Sync to game state so EntityAI/pathfinder see current position
                _gameStateManager.State.Player.X = play.PlayerEntity.X;
                _gameStateManager.State.Player.Y = play.PlayerEntity.Y;

                // Apply hazard damage at destination
                CheckHazardAtPosition(play, play.PlayerEntity.X, play.PlayerEntity.Y);

                // Propagate noise to nearby dormant entities
                PropagateNoise(play, play.PlayerEntity.X, play.PlayerEntity.Y);

                // Check for entity interaction at destination
                CheckEntityInteractionAt(play, play.PlayerEntity.X, play.PlayerEntity.Y);

                // Process lingering status effects after each step
                if (_gameStateManager.IsPlayerAlive())
                {
                    var effectMessages = _gameStateManager.ProcessStatusEffects();
                    foreach (var effectMsg in effectMessages)
                        LogAndFloat(play,effectMsg, Color.Red, play.PlayerEntity.X, play.PlayerEntity.Y);
                    if (effectMessages.Exists(m => m.Contains("damage")))
                        TriggerDamageFlash();

                    if (!_gameStateManager.IsPlayerAlive())
                    {
                        ScreenManager.Push(new GameOverScreen(_gameStateManager));
                    }
                }

                // Deduct 1 AP for completed move and process turn if needed
                if (_gameStateManager.IsPlayerAlive())
                {
                    play.PlayerAP--;
                    AfterPlayerAction(play);
                }
            }
            else
            {
                play.RenderPos = Vector2.Lerp(play.MoveFrom, play.MoveTo, play.MoveProgress);
            }
        }

        if (!play.IsMoving && _gameStateManager.IsPlayerAlive())
        {
            // Overlay screens (0 AP cost — always available)
            if (input.IsActionJustPressed(GameAction.Pause))
            {
                ScreenManager.Push(new PauseScreen(_saveManager, _gameStateManager, _inputManager, _bindingsPath));
                return;
            }
            if (input.IsActionJustPressed(GameAction.OpenInventory))
            {
                ScreenManager.Push(new InventoryScreen(_gameStateManager));
                return;
            }
            if (input.IsActionJustPressed(GameAction.OpenQuestLog) && _questManager != null)
            {
                ScreenManager.Push(new QuestLogScreen(_questManager, _gameStateManager));
                return;
            }

            // AP-gated actions require AP > 0 and player turn
            if (!play.IsPlayerTurn || play.PlayerAP <= 0)
                return;

            // End Turn — forfeit remaining AP, entities act
            if (input.IsActionJustPressed(GameAction.EndTurn))
            {
                EndPlayerTurn(play);
                return;
            }

            // Directional attack / interact (Interact key when not moving)
            if (input.IsActionJustPressed(GameAction.Interact))
            {
                var (fx, fy) = play.GetFacingTile();
                if (_state.Map.InBounds(fx, fy) && TryBumpAttack(play, fx, fy))
                {
                    play.PlayerAP--;
                    AfterPlayerAction(play);
                }
                else if (_state.Map.InBounds(fx, fy))
                {
                    // Friendly interaction — 0 AP cost
                    CheckEntityInteractionAt(play, fx, fy);
                }
                return;
            }

            // Accept movement input
            int dx = 0, dy = 0;

            if (input.IsActionJustPressed(GameAction.MoveUp)) dy = -1;
            else if (input.IsActionJustPressed(GameAction.MoveDown)) dy = 1;
            else if (input.IsActionJustPressed(GameAction.MoveLeft)) dx = -1;
            else if (input.IsActionJustPressed(GameAction.MoveRight)) dx = 1;

            if (dx != 0 || dy != 0)
            {
                // Update facing direction for sprite flip and directional attacks
                if (dx < 0) { play.PlayerFacing = Direction.Left; _gameStateManager.State.Player.Facing = Direction.Left; }
                else if (dx > 0) { play.PlayerFacing = Direction.Right; _gameStateManager.State.Player.Facing = Direction.Right; }
                else if (dy < 0) { play.PlayerFacing = Direction.Up; _gameStateManager.State.Player.Facing = Direction.Up; }
                else if (dy > 0) { play.PlayerFacing = Direction.Down; _gameStateManager.State.Player.Facing = Direction.Down; }

                int targetX = play.PlayerEntity.X + dx;
                int targetY = play.PlayerEntity.Y + dy;

                // Check custom exit point transitions first (portal-style)
                if (_edgeResolver != null)
                {
                    var exitReq = _edgeResolver.ResolveExitPoint(
                        _gameStateManager.State.CurrentMapId, targetX, targetY);
                    if (exitReq != null)
                    {
                        _gameStateManager.PendingTransition = exitReq;
                        LogAndFloat(play,$"Transitioning to {exitReq.TargetMap}...", Color.White, play.PlayerEntity.X, play.PlayerEntity.Y);
                        dx = 0; dy = 0; // Skip normal movement
                    }
                }

                if ((dx != 0 || dy != 0) && CanMoveTo(targetX, targetY))
                {
                    play.MoveFrom = new Vector2(play.PlayerEntity.X, play.PlayerEntity.Y);
                    play.MoveTo = new Vector2(targetX, targetY);
                    play.MoveProgress = 0f;
                    var (moveCost, slowGroupName) = GetMovementCostWithSource(targetX, targetY);
                    play.CurrentMoveDuration = PlayState.MoveDuration * moveCost * _gameStateManager.GetEffectiveMovementMultiplier();
                    play.IsMoving = true;

                    if (moveCost > 1.0f && slowGroupName != null)
                        _gameLog?.Add($"Slowed by {slowGroupName} ({moveCost:0.#}x)", Color.Gray);
                }
                else if (_state.Map.InBounds(targetX, targetY))
                {
                    // Blocked -- try bump attack first, then normal interaction
                    if (TryBumpAttack(play, targetX, targetY))
                    {
                        play.PlayerAP--;
                        if (_gameStateManager.IsPlayerAlive())
                            AfterPlayerAction(play);
                    }
                    else
                    {
                        CheckEntityInteractionAt(play, targetX, targetY);
                    }
                }
                else if (_edgeResolver != null)
                {
                    // Out of bounds — check for edge-based map transition via WorldLayout
                    var edgeRequest = _edgeResolver.Resolve(
                        _gameStateManager.State.CurrentMapId,
                        targetX, targetY,
                        play.PlayerEntity.X, play.PlayerEntity.Y,
                        _state.Map.Width, _state.Map.Height);
                    if (edgeRequest != null)
                    {
                        _gameStateManager.PendingTransition = edgeRequest;
                        LogAndFloat(play,$"Transitioning to {edgeRequest.TargetMap}...", Color.White, play.PlayerEntity.X, play.PlayerEntity.Y);
                    }
                }
            }
        }

        SyncEntityRenderState();

        // Fix #7: Only center camera when player position or canvas bounds actually change
        float renderX = play.RenderPos.X;
        float renderY = play.RenderPos.Y;
        var currentBounds = _getCanvasBounds();
        if (renderX != _lastCameraRenderX || renderY != _lastCameraRenderY
            || currentBounds != _lastCanvasBounds)
        {
            _lastCameraRenderX = renderX;
            _lastCameraRenderY = renderY;
            _lastCanvasBounds = currentBounds;
            CenterCameraOnPlayer();
        }

        // Fix #5: Cache hostile-nearby result for Draw
        _hostileNearby = AnyHostileNearby();

        // Fix #6: Cache cover bonus — recompute only when player position changes
        if (play.PlayerEntity != null)
        {
            int px = play.PlayerEntity.X;
            int py = play.PlayerEntity.Y;
            if (px != _cachedCoverX || py != _cachedCoverY)
            {
                _cachedCoverX = px;
                _cachedCoverY = py;
                _cachedCover = GetDefenseBonusAt(px, py);
            }
        }

        // Fix #3: Cache stats text — recompute only when ATK/DEF changes
        int currentATK = _gameStateManager.GetEffectiveAttack();
        int currentDEF = _gameStateManager.GetEffectiveDefense();
        if (currentATK != _cachedATK || currentDEF != _cachedDEF)
        {
            _cachedATK = currentATK;
            _cachedDEF = currentDEF;
            _cachedStatsText = $"ATK:{currentATK} DEF:{currentDEF}";
            _cachedStatsSize = default; // Reset; will be measured once in Draw when font is available
        }

        // Fix #2: Cache AP text — recompute only when currentAP or maxAP changes
        int maxAP = _gameStateManager.GetEffectiveMaxAP();
        int currentAP = play.PlayerAP;
        if (currentAP != _cachedCurrentAP || maxAP != _cachedMaxAP)
        {
            _cachedCurrentAP = currentAP;
            _cachedMaxAP = maxAP;
            var sb = new System.Text.StringBuilder("AP:", 3 + maxAP);
            for (int i = 0; i < maxAP; i++)
                sb.Append(i < currentAP ? '*' : '.');
            _cachedAPText = sb.ToString();
        }
    }

    /// <summary>
    /// Syncs runtime entity positions and visibility to the editor entity list
    /// so MapCanvas renders entities at their current AI-driven positions.
    /// Deactivated entities (killed, collected) are removed from the render list.
    /// Safe because editor state is restored from _savedMap on play mode exit.
    /// </summary>
    private void SyncEntityRenderState()
    {
        var play = _state.PlayState;
        _activeById.Clear();
        foreach (var instance in _gameStateManager.State.ActiveEntities)
            _activeById[instance.Id] = instance;

        for (int i = _state.Map.Entities.Count - 1; i >= 0; i--)
        {
            var editorEntity = _state.Map.Entities[i];
            if (editorEntity == play?.PlayerEntity) continue;

            if (_activeById.TryGetValue(editorEntity.Id, out var instance))
            {
                if (instance.IsActive)
                {
                    editorEntity.X = instance.X;
                    editorEntity.Y = instance.Y;
                }
                else
                {
                    _state.Map.Entities.RemoveAt(i);
                }
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch, SpriteFont font,
        Renderer renderer, Rectangle canvasBounds)
    {
        _canvas.Draw(spriteBatch, _state, renderer, canvasBounds);

        // Floating messages (world-space, drift upward + fade)
        var play = _state.PlayState;
        if (play?.FloatingMessages?.Count > 0)
        {
            var sheet = _state.Sheet;
            int tileW = sheet?.TileWidth ?? 16;
            int tileH = sheet?.TileHeight ?? 16;

            foreach (var fm in play.FloatingMessages)
            {
                float alpha = Math.Clamp(fm.Timer / FloatingMessage.Duration, 0f, 1f);
                if (alpha <= 0) continue;

                float worldX = (fm.TileX + 0.5f) * tileW;
                float worldY = fm.TileY * tileH - fm.VerticalOffset;
                var screenPos = _canvas.Camera.WorldToScreen(new Vector2(worldX, worldY));

                var textSize = font.MeasureString(fm.Text);
                var drawPos = new Vector2(
                    screenPos.X - textSize.X / 2f,
                    screenPos.Y - textSize.Y);

                spriteBatch.DrawString(font, fm.Text, drawPos, fm.Color * alpha);
            }
        }

        // HUD stats + status effects are now shown in the SidebarHUD
    }

    private bool CanMoveTo(int x, int y)
    {
        var map = _state.Map;
        if (!map.InBounds(x, y)) return false;

        // Check all layers for solid groups
        foreach (var layer in map.Layers)
        {
            string groupName = layer.GetCell(x, y, map.Width);
            if (groupName != null
                && _state.GroupsByName.TryGetValue(groupName, out var group)
                && group.IsSolid)
            {
                return false;
            }
        }

        // Check active entities for solid groups
        foreach (var instance in _gameStateManager.State.ActiveEntities)
        {
            if (!instance.IsActive) continue;
            if (instance.X == x && instance.Y == y
                && _state.GroupsByName.TryGetValue(instance.DefinitionName, out var group)
                && group.IsSolid)
            {
                return false;
            }
        }

        return true;
    }

    private void CheckEntityInteractionAt(PlayState play, int x, int y)
    {
        foreach (var instance in _gameStateManager.State.ActiveEntities)
        {
            if (!instance.IsActive) continue;
            if (instance.X != x || instance.Y != y) continue;

            if (!_state.GroupsByName.TryGetValue(instance.DefinitionName, out var group))
                continue;

            switch (group.EntityType)
            {
                case EntityType.NPC:
                    if (TryShowDialogue(instance, play)) return;
                    LogAndFloat(play,$"Talked to {instance.DefinitionName}", Color.White, instance.X, instance.Y);
                    break;

                case EntityType.Item:
                    _gameStateManager.CollectItem(instance);
                    LogAndFloat(play,$"Collected {instance.DefinitionName}", Color.LimeGreen, instance.X, instance.Y);
                    TryShowPickupDialogue(instance, play);
                    break;

                case EntityType.Trap:
                    int damage = 0;
                    if (instance.Properties.TryGetValue("damage", out var dmgStr))
                        int.TryParse(dmgStr, out damage);
                    if (damage > 0)
                    {
                        _gameStateManager.DamagePlayer(damage);
                        TriggerDamageFlash();
                        LogAndFloat(play,$"{instance.DefinitionName} dealt {damage} damage!", Color.Red, play.PlayerEntity.X, play.PlayerEntity.Y);
                    }
                    else
                    {
                        LogAndFloat(play,$"Triggered {instance.DefinitionName}", Color.White, instance.X, instance.Y);
                    }
                    if (!_gameStateManager.IsPlayerAlive())
                    {
                        ScreenManager.Push(new GameOverScreen(_gameStateManager));
                        return;
                    }
                    break;

                case EntityType.Trigger:
                    if (instance.Properties.TryGetValue("target_map", out var targetMap)
                        && !string.IsNullOrEmpty(targetMap))
                    {
                        instance.Properties.TryGetValue("target_x", out var txStr);
                        instance.Properties.TryGetValue("target_y", out var tyStr);
                        int.TryParse(txStr ?? "0", out var tx);
                        int.TryParse(tyStr ?? "0", out var ty);
                        _gameStateManager.PendingTransition = new MapTransitionRequest
                        {
                            TargetMap = targetMap,
                            TargetX = tx,
                            TargetY = ty,
                        };
                        LogAndFloat(play,$"Transitioning to {targetMap}...", Color.White, instance.X, instance.Y);
                    }
                    else
                    {
                        LogAndFloat(play,$"Triggered {instance.DefinitionName}", Color.White, instance.X, instance.Y);
                    }
                    break;

                case EntityType.Interactable:
                    if (TryShowDialogue(instance, play)) return;
                    LogAndFloat(play,$"Interacted with {instance.DefinitionName}", Color.White, instance.X, instance.Y);
                    break;
                default:
                    LogAndFloat(play,$"Interacted with {instance.DefinitionName}", Color.White, instance.X, instance.Y);
                    break;
            }

            return;
        }
    }

    private void CheckHazardAtPosition(PlayState play, int x, int y)
    {
        foreach (var layer in _state.Map.Layers)
        {
            string groupName = layer.GetCell(x, y, _state.Map.Width);
            if (groupName != null
                && _state.GroupsByName.TryGetValue(groupName, out var group)
                && group.IsHazardous)
            {
                // Apply instant damage
                if (group.DamagePerTick > 0)
                {
                    _gameStateManager.DamagePlayer(group.DamagePerTick);
                    TriggerDamageFlash();
                    string dmgType = group.DamageType ?? "damage";
                    LogAndFloat(play,$"Took {group.DamagePerTick} {dmgType} damage!", Color.Red, play.PlayerEntity.X, play.PlayerEntity.Y);
                }

                // Apply lingering status effect based on DamageType
                switch (group.DamageType)
                {
                    case "fire":
                        _gameStateManager.ApplyStatusEffect("fire", 3, 1, 1.0f);
                        break;
                    case "poison":
                        _gameStateManager.ApplyStatusEffect("poison", 6, 1, 1.0f);
                        break;
                    case "ice":
                        _gameStateManager.ApplyStatusEffect("ice", 3, 0, 2.0f);
                        break;
                    // "spikes" and null: instant damage only, no lingering effect
                }

                if (!_gameStateManager.IsPlayerAlive())
                {
                    ScreenManager.Push(new GameOverScreen(_gameStateManager));
                }
                return;
            }
        }
    }

    private float GetMovementCostAt(int x, int y)
    {
        float maxCost = 1.0f;
        foreach (var layer in _state.Map.Layers)
        {
            string groupName = layer.GetCell(x, y, _state.Map.Width);
            if (groupName != null
                && _state.GroupsByName.TryGetValue(groupName, out var group))
            {
                if (group.MovementCost > maxCost)
                    maxCost = group.MovementCost;
            }
        }
        return maxCost;
    }

    internal (float cost, string groupName) GetMovementCostWithSource(int x, int y)
    {
        float maxCost = 1.0f;
        string sourceName = null;
        foreach (var layer in _state.Map.Layers)
        {
            string groupName = layer.GetCell(x, y, _state.Map.Width);
            if (groupName != null
                && _state.GroupsByName.TryGetValue(groupName, out var group))
            {
                if (group.MovementCost > maxCost)
                {
                    maxCost = group.MovementCost;
                    sourceName = groupName;
                }
            }
        }
        return (maxCost, sourceName);
    }

    internal int GetDefenseBonusAt(int x, int y)
    {
        int maxBonus = 0;
        foreach (var layer in _state.Map.Layers)
        {
            string groupName = layer.GetCell(x, y, _state.Map.Width);
            if (groupName != null
                && _state.GroupsByName.TryGetValue(groupName, out var group))
            {
                if (group.DefenseBonus > maxBonus)
                    maxBonus = group.DefenseBonus;
            }
        }
        return maxBonus;
    }

    internal int GetNoiseLevelAt(int x, int y)
    {
        int maxNoise = 0;
        foreach (var layer in _state.Map.Layers)
        {
            string groupName = layer.GetCell(x, y, _state.Map.Width);
            if (groupName != null
                && _state.GroupsByName.TryGetValue(groupName, out var group))
            {
                if (group.NoiseLevel > maxNoise)
                    maxNoise = group.NoiseLevel;
            }
        }
        return maxNoise;
    }

    private void PropagateNoise(PlayState play, int x, int y)
    {
        int noiseLevel = GetNoiseLevelAt(x, y);
        if (noiseLevel <= 0) return;

        int noiseRadius = 3 * noiseLevel;
        var player = _gameStateManager.State.Player;

        foreach (var entity in _gameStateManager.State.ActiveEntities)
        {
            if (!entity.IsActive) continue;
            if (!entity.Properties.ContainsKey("behavior")) continue;
            if (!_gameStateManager.IsEntityHostile(entity)) continue;

            int aggroRange = _gameStateManager.GetEntityIntProperty(entity, "aggro_range", 5);
            int distance = Math.Abs(entity.X - player.X) + Math.Abs(entity.Y - player.Y);

            // Only alert entities outside their normal aggro range but within noise radius
            if (distance <= aggroRange || distance > noiseRadius) continue;

            // Already alerted entities don't get re-alerted
            int existingAlert = _gameStateManager.GetEntityIntProperty(entity, "alert_turns", 0);
            if (existingAlert > 0) continue;

            _gameStateManager.SetEntityIntProperty(entity, "alert_turns", 3);
            LogAndFloat(play,"!", Color.Yellow, entity.X, entity.Y);
        }
    }

    private void CenterCameraOnPlayer()
    {
        var play = _state.PlayState;
        var sheet = _state.Sheet;
        var canvasBounds = _getCanvasBounds();

        // Player world pixel center
        float worldX = (play.RenderPos.X + 0.5f) * sheet.TileWidth;
        float worldY = (play.RenderPos.Y + 0.5f) * sheet.TileHeight;

        // Center on screen
        float screenCenterX = canvasBounds.X + canvasBounds.Width / 2f;
        float screenCenterY = canvasBounds.Y + canvasBounds.Height / 2f;
        int zoom = _canvas.Camera.Zoom;

        _canvas.Camera.Offset = new Vector2(
            screenCenterX - worldX * zoom,
            screenCenterY - worldY * zoom);
    }

    private void ProcessQuestUpdates(PlayState play)
    {
        if (_questManager == null) return;

        var events = _questManager.CheckForUpdates(_gameStateManager);
        foreach (var evt in events)
        {
            string msg = evt.Type switch
            {
                QuestEventType.QuestStarted => $"Quest started: {evt.QuestName}",
                QuestEventType.ObjectiveCompleted => $"Objective complete: {evt.ObjectiveDescription}",
                QuestEventType.QuestCompleted => $"Quest complete: {evt.QuestName}!",
                _ => null,
            };

            if (msg != null)
            {
                LogAndFloat(play,msg, Color.Cyan, play.PlayerEntity.X, play.PlayerEntity.Y);
            }
        }
    }

    private bool TryBumpAttack(PlayState play, int x, int y)
    {
        foreach (var instance in _gameStateManager.State.ActiveEntities)
        {
            if (!instance.IsActive) continue;
            if (instance.X != x || instance.Y != y) continue;

            if (_gameStateManager.IsAttackable(instance, _state.GroupsByName))
            {
                int terrainBonus = GetDefenseBonusAt(instance.X, instance.Y);

                // Flanking: check entity facing
                Direction entityFacing = play.EntityFacings.TryGetValue(instance.Id, out var ef) ? ef : Direction.Down;
                var attackPos = CombatHelper.GetAttackPosition(
                    play.PlayerEntity.X, play.PlayerEntity.Y,
                    instance.X, instance.Y, entityFacing);
                float posMult = CombatHelper.GetPositionMultiplier(attackPos);

                var result = _gameStateManager.AttackEntity(instance, _gameStateManager.GetEffectiveAttack(), terrainBonus, posMult);

                // Show position-based floating message
                if (attackPos == AttackPosition.Backstab)
                    LogAndFloat(play,"BACKSTAB!", Color.OrangeRed, instance.X, instance.Y);
                else if (attackPos == AttackPosition.Flank)
                    LogAndFloat(play,"Flanked!", Color.Orange, instance.X, instance.Y);

                LogAndFloat(play,result.Message, Color.Gold, instance.X, instance.Y);
                TriggerEntityFlash(instance.Id);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns true if any hostile entity with a behavior is within aggro range of the player.
    /// Used for auto-end-turn: exploration mode ends turn immediately when no threats are near.
    /// </summary>
    internal bool AnyHostileNearby()
    {
        var player = _gameStateManager.State.Player;
        foreach (var entity in _gameStateManager.State.ActiveEntities)
        {
            if (!entity.IsActive) continue;
            if (!entity.Properties.ContainsKey("behavior")) continue;
            if (!_gameStateManager.IsEntityHostile(entity)) continue;

            int aggroRange = _gameStateManager.GetEntityIntProperty(entity, "aggro_range", 5);
            int alertTurns = _gameStateManager.GetEntityIntProperty(entity, "alert_turns", 0);
            if (alertTurns > 0)
                aggroRange *= 2;

            int distance = Math.Abs(entity.X - player.X) + Math.Abs(entity.Y - player.Y);
            if (distance <= aggroRange)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Begins a new player turn: refill AP, set turn flag.
    /// </summary>
    private void BeginPlayerTurn(PlayState play)
    {
        play.PlayerAP = _gameStateManager.GetEffectiveMaxAP();
        play.IsPlayerTurn = true;

        // Poise regeneration when no hostiles nearby
        if (!AnyHostileNearby())
        {
            int regenAmount = _gameStateManager.RegeneratePoise();
            if (regenAmount > 0)
                LogAndFloat(play,$"+{regenAmount} Poise", Color.CornflowerBlue, play.PlayerEntity.X, play.PlayerEntity.Y);
        }
    }

    /// <summary>
    /// Ends the player turn: execute entity turn, then begin next player turn.
    /// </summary>
    private void EndPlayerTurn(PlayState play)
    {
        play.PlayerAP = 0;
        play.IsPlayerTurn = false;

        if (_gameStateManager.IsPlayerAlive())
            ExecuteEntityTurn(play);

        if (!_gameStateManager.IsPlayerAlive())
            return;

        ProcessQuestUpdates(play);
        BeginPlayerTurn(play);
    }

    /// <summary>
    /// Called after each AP-spending action. If no hostiles nearby, auto-ends the turn.
    /// </summary>
    private void AfterPlayerAction(PlayState play)
    {
        if (!_gameStateManager.IsPlayerAlive())
            return;

        if (play.PlayerAP <= 0)
        {
            EndPlayerTurn(play);
            return;
        }

        // Auto-end exploration mode: if no hostiles nearby, end turn immediately
        if (!AnyHostileNearby())
        {
            EndPlayerTurn(play);
        }
    }

    private void ExecuteEntityTurn(PlayState play)
    {
        foreach (var entity in _gameStateManager.State.ActiveEntities)
        {
            if (!entity.IsActive) continue;
            if (!entity.Properties.ContainsKey("behavior")) continue;

            int entityAP = Math.Clamp(_gameStateManager.GetEntityIntProperty(entity, "speed", 1), 1, 3);
            bool hostile = _gameStateManager.IsEntityHostile(entity);

            while (entityAP > 0)
            {
                var action = EntityAI.DecideAction(entity, _gameStateManager.State, _pathfinder, hostile);

                if (action.Type == EntityActionType.Idle)
                    break;

                switch (action.Type)
                {
                    case EntityActionType.Move:
                        int moveDx = action.TargetX - entity.X;
                        int moveDy = action.TargetY - entity.Y;
                        // 4-directional facing: horizontal takes priority for sprite flip
                        if (moveDx < 0) play.EntityFacings[entity.Id] = Direction.Left;
                        else if (moveDx > 0) play.EntityFacings[entity.Id] = Direction.Right;
                        else if (moveDy < 0) play.EntityFacings[entity.Id] = Direction.Up;
                        else if (moveDy > 0) play.EntityFacings[entity.Id] = Direction.Down;
                        entity.X = action.TargetX;
                        entity.Y = action.TargetY;
                        break;

                    case EntityActionType.Attack:
                        if (action.AttackTargetX == null)
                        {
                            var atk = _gameStateManager.GetEntityIntProperty(entity, "attack", 3);
                            int terrainBonus = GetDefenseBonusAt(play.PlayerEntity.X, play.PlayerEntity.Y);

                            // Flanking: entity attacks player — check player facing
                            Direction entityFacing = play.EntityFacings.TryGetValue(entity.Id, out var ef2) ? ef2 : Direction.Down;
                            var attackPos = CombatHelper.GetAttackPosition(
                                entity.X, entity.Y,
                                play.PlayerEntity.X, play.PlayerEntity.Y,
                                play.PlayerFacing);
                            float posMult = CombatHelper.GetPositionMultiplier(attackPos);

                            var damage = CombatHelper.CalculateDamage(atk, _gameStateManager.GetEffectiveDefense(), terrainBonus, posMult);
                            _gameStateManager.DamagePlayer(damage);
                            TriggerDamageFlash();

                            string posLabel = attackPos == AttackPosition.Backstab ? " (Backstab!)"
                                            : attackPos == AttackPosition.Flank ? " (Flanked!)"
                                            : "";
                            LogAndFloat(play,$"{entity.DefinitionName} hit you for {damage}{posLabel}!", Color.Red, play.PlayerEntity.X, play.PlayerEntity.Y);

                            if (_gameStateManager.LastDamageBrokePoise)
                                LogAndFloat(play,"POISE BROKEN!", Color.OrangeRed, play.PlayerEntity.X, play.PlayerEntity.Y);
                        }
                        break;
                }

                entityAP--;

                if (!_gameStateManager.IsPlayerAlive())
                {
                    ScreenManager.Push(new GameOverScreen(_gameStateManager));
                    return;
                }
            }

            // Alert tick-down after each entity's turn
            int alertTurns = _gameStateManager.GetEntityIntProperty(entity, "alert_turns", 0);
            if (alertTurns > 0)
                _gameStateManager.SetEntityIntProperty(entity, "alert_turns", alertTurns - 1);
        }

        // Check player death after all entities have acted
        if (!_gameStateManager.IsPlayerAlive())
        {
            ScreenManager.Push(new GameOverScreen(_gameStateManager));
        }
    }

    private IPathfinder CreatePathfinder()
    {
        var map = _state.Map;
        var loadedMap = new LoadedMap
        {
            Id = "editor",
            Width = map.Width,
            Height = map.Height,
        };
        foreach (var layer in map.Layers)
        {
            loadedMap.Layers.Add(new LoadedMapLayer
            {
                Name = layer.Name,
                Cells = layer.Cells,
            });
        }
        return new SimplePathfinder(
            loadedMap,
            _state.GroupsByName,
            _gameStateManager.State.ActiveEntities,
            _gameStateManager.State.Player);
    }

    private void TryShowPickupDialogue(EntityInstance instance, PlayState play)
    {
        instance.Properties.TryGetValue("on_pickup_dialogue", out var pickupDialogue);
        if (string.IsNullOrEmpty(pickupDialogue)) return;

        string flag = $"pickup_dialogue_shown:{instance.DefinitionName}";
        if (_gameStateManager.HasFlag(flag)) return;

        _gameStateManager.SetFlag(flag);

        var dialogue = LoadDialogue(pickupDialogue);
        dialogue ??= CreateInlineDialogue(instance.DefinitionName, pickupDialogue);
        ScreenManager.Push(new DialogueScreen(dialogue, _gameStateManager, _gameLog));
        play.FloatingMessages.Clear();
    }

    private bool TryShowDialogue(EntityInstance instance, PlayState play)
    {
        // Check if this entity's dialogue has concluded — show reminder instead
        instance.Properties.TryGetValue("concluded_flag", out var concludedFlag);
        if (!string.IsNullOrEmpty(concludedFlag) && _gameStateManager.HasFlag(concludedFlag))
        {
            instance.Properties.TryGetValue("concluded_dialogue", out var concludedValue);
            if (!string.IsNullOrEmpty(concludedValue))
            {
                var concluded = LoadDialogue(concludedValue);
                concluded ??= CreateInlineDialogue(instance.DefinitionName, concludedValue);
                ScreenManager.Push(new DialogueScreen(concluded, _gameStateManager, _gameLog));
                play.FloatingMessages.Clear();
                return true;
            }
        }

        instance.Properties.TryGetValue("dialogue", out var dialogueValue);
        if (string.IsNullOrEmpty(dialogueValue))
            instance.Properties.TryGetValue("dialogue_id", out dialogueValue);
        if (string.IsNullOrEmpty(dialogueValue))
            return false;

        // Try file-based dialogue first, fall back to inline text
        var dialogue = LoadDialogue(dialogueValue);
        dialogue ??= CreateInlineDialogue(instance.DefinitionName, dialogueValue);

        ScreenManager.Push(new DialogueScreen(dialogue, _gameStateManager, _gameLog));
        play.FloatingMessages.Clear();
        return true;
    }

    internal static DialogueData CreateInlineDialogue(string entityName, string text)
    {
        var dialogue = new DialogueData { Id = $"inline_{entityName}" };
        var pages = text.Split('|');
        for (int i = 0; i < pages.Length; i++)
        {
            dialogue.Nodes.Add(new DialogueNode
            {
                Id = $"page_{i}",
                Speaker = entityName,
                Text = pages[i].Trim(),
                NextNodeId = i < pages.Length - 1 ? $"page_{i + 1}" : null,
            });
        }
        return dialogue;
    }

    private DialogueData LoadDialogue(string dialogueRef)
    {
        return _dialogueLoader?.LoadDialogue(dialogueRef);
    }

    /// <summary>
    /// Adds a floating message AND logs it to the persistent game log.
    /// </summary>
    private void LogAndFloat(PlayState play, string text, Color color, int tileX, int tileY)
    {
        play.AddFloatingMessage(text, color, tileX, tileY);
        _gameLog?.Add(text, color);
    }
}
