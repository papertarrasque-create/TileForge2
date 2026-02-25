using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;
using TileForge.Data;
using TileForge.Editor;
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
    private readonly string _dialogueBasePath;
    private readonly QuestManager _questManager;
    private readonly Func<Rectangle> _getCanvasBounds;
    private IPathfinder _pathfinder;

    public GameplayScreen(EditorState state, MapCanvas canvas,
        GameStateManager gameStateManager, SaveManager saveManager,
        GameInputManager inputManager, string bindingsPath,
        string dialogueBasePath, QuestManager questManager,
        Func<Rectangle> getCanvasBounds)
    {
        _state = state;
        _canvas = canvas;
        _gameStateManager = gameStateManager;
        _saveManager = saveManager;
        _inputManager = inputManager;
        _bindingsPath = bindingsPath;
        _dialogueBasePath = dialogueBasePath;
        _questManager = questManager;
        _getCanvasBounds = getCanvasBounds;
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

        // Tick status message timer
        if (play.StatusMessageTimer > 0)
        {
            play.StatusMessageTimer -= dt;
            if (play.StatusMessageTimer <= 0)
                play.StatusMessage = null;
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

                // Check for entity interaction at destination
                CheckEntityInteractionAt(play, play.PlayerEntity.X, play.PlayerEntity.Y);

                // Process lingering status effects after each step
                if (_gameStateManager.IsPlayerAlive())
                {
                    var effectMessages = _gameStateManager.ProcessStatusEffects();
                    if (effectMessages.Count > 0 && play.StatusMessage == null)
                    {
                        play.StatusMessage = effectMessages[0];
                        play.StatusMessageTimer = PlayState.StatusMessageDuration;
                    }
                    if (effectMessages.Exists(m => m.Contains("damage")))
                        TriggerDamageFlash();

                    if (!_gameStateManager.IsPlayerAlive())
                    {
                        play.StatusMessage = null;
                        ScreenManager.Push(new GameOverScreen(_gameStateManager));
                    }
                }

                // Entity turn: all entities with AI act after the player's move
                if (_gameStateManager.IsPlayerAlive())
                    ExecuteEntityTurn(play);

                // Check quest progress after move + interactions + entity turn
                ProcessQuestUpdates(play);
            }
            else
            {
                play.RenderPos = Vector2.Lerp(play.MoveFrom, play.MoveTo, play.MoveProgress);
            }
        }

        if (!play.IsMoving && _gameStateManager.IsPlayerAlive())
        {
            // Check for pause
            if (input.IsActionJustPressed(GameAction.Pause))
            {
                ScreenManager.Push(new PauseScreen(_saveManager, _gameStateManager, _inputManager, _bindingsPath));
                return;
            }

            // Check for inventory
            if (input.IsActionJustPressed(GameAction.OpenInventory))
            {
                ScreenManager.Push(new InventoryScreen(_gameStateManager));
                return;
            }

            // Check for quest log
            if (input.IsActionJustPressed(GameAction.OpenQuestLog) && _questManager != null)
            {
                ScreenManager.Push(new QuestLogScreen(_questManager, _gameStateManager));
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
                int targetX = play.PlayerEntity.X + dx;
                int targetY = play.PlayerEntity.Y + dy;

                if (CanMoveTo(targetX, targetY))
                {
                    play.MoveFrom = new Vector2(play.PlayerEntity.X, play.PlayerEntity.Y);
                    play.MoveTo = new Vector2(targetX, targetY);
                    play.MoveProgress = 0f;
                    play.CurrentMoveDuration = PlayState.MoveDuration * GetMovementCostAt(targetX, targetY) * _gameStateManager.GetEffectiveMovementMultiplier();
                    play.IsMoving = true;
                }
                else if (_state.Map.InBounds(targetX, targetY))
                {
                    // Blocked -- try bump attack first, then normal interaction
                    if (TryBumpAttack(play, targetX, targetY))
                    {
                        // Attack counts as player action — entities get their turn
                        if (_gameStateManager.IsPlayerAlive())
                            ExecuteEntityTurn(play);
                        ProcessQuestUpdates(play);
                    }
                    else
                    {
                        CheckEntityInteractionAt(play, targetX, targetY);
                    }
                }
            }
        }

        SyncEntityRenderState();
        CenterCameraOnPlayer();
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
        for (int i = _state.Map.Entities.Count - 1; i >= 0; i--)
        {
            var editorEntity = _state.Map.Entities[i];
            if (editorEntity == play?.PlayerEntity) continue;

            foreach (var instance in _gameStateManager.State.ActiveEntities)
            {
                if (instance.Id != editorEntity.Id) continue;

                if (instance.IsActive)
                {
                    editorEntity.X = instance.X;
                    editorEntity.Y = instance.Y;
                }
                else
                {
                    _state.Map.Entities.RemoveAt(i);
                }
                break;
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch, SpriteFont font,
        Renderer renderer, Rectangle canvasBounds)
    {
        _canvas.Draw(spriteBatch, _state, renderer, canvasBounds);

        // Health bar (top-left of canvas)
        if (_gameStateManager?.State?.Player != null)
        {
            var player = _gameStateManager.State.Player;
            int barX = canvasBounds.X + 8;
            int barY = canvasBounds.Y + 8;
            int barWidth = 100;
            int barHeight = 12;

            // Background (dark)
            renderer.DrawRect(spriteBatch, new Rectangle(barX, barY, barWidth, barHeight), new Color(40, 40, 40));

            // Fill (red → green based on health %)
            float healthPct = player.MaxHealth > 0 ? (float)player.Health / player.MaxHealth : 0;
            int fillWidth = (int)(barWidth * healthPct);
            if (fillWidth > 0)
            {
                Color healthColor = healthPct > 0.5f ? Color.Green : healthPct > 0.25f ? Color.Yellow : Color.Red;
                renderer.DrawRect(spriteBatch, new Rectangle(barX, barY, fillWidth, barHeight), healthColor);
            }

            // ATK/DEF stats readout
            string statsText = $"ATK:{_gameStateManager.GetEffectiveAttack()} DEF:{_gameStateManager.GetEffectiveDefense()}";
            var statsPos = new Vector2(barX, barY + barHeight + 2);
            spriteBatch.DrawString(font, statsText, statsPos, Color.White);
        }

        // Status message text below health bar
        if (!string.IsNullOrEmpty(_state.PlayState?.StatusMessage) && _state.PlayState.StatusMessageTimer > 0)
        {
            var msg = _state.PlayState.StatusMessage;
            Color msgColor;
            if (msg.Contains("damage") || msg.Contains("died") || msg.Contains("dealt") || msg.Contains("hit you for"))
                msgColor = Color.Red;
            else if (msg.Contains("Hit ") || msg.Contains("defeated"))
                msgColor = Color.Gold;
            else if (msg.Contains("Collected") || msg.Contains("healed") || msg.Contains("Healed"))
                msgColor = Color.LimeGreen;
            else if (msg.Contains("Quest") || msg.Contains("Objective"))
                msgColor = Color.Cyan;
            else
                msgColor = Color.White;

            var msgSize = font.MeasureString(msg);
            var msgPos = new Vector2(
                canvasBounds.X + (canvasBounds.Width - msgSize.X) / 2f,
                canvasBounds.Y + canvasBounds.Height - msgSize.Y - 16f);
            spriteBatch.DrawString(font, msg, msgPos, msgColor);
        }

        // Active status effect indicators
        if (_gameStateManager?.State?.Player?.ActiveEffects != null)
        {
            float effectX = canvasBounds.X + 8 + 100 + 8; // after health bar
            float effectY = canvasBounds.Y + 8;

            foreach (var effect in _gameStateManager.State.Player.ActiveEffects)
            {
                string label = effect.Type?.ToUpperInvariant() switch
                {
                    "FIRE" => $"[BURN {effect.RemainingSteps}]",
                    "POISON" => $"[PSN {effect.RemainingSteps}]",
                    "ICE" => $"[SLOW {effect.RemainingSteps}]",
                    _ => $"[{effect.Type?.ToUpperInvariant()} {effect.RemainingSteps}]",
                };

                Color effectColor = effect.Type switch
                {
                    "fire" => Color.OrangeRed,
                    "poison" => new Color(180, 50, 220),
                    "ice" => Color.CornflowerBlue,
                    _ => Color.White,
                };

                spriteBatch.DrawString(font, label, new Vector2(effectX, effectY), effectColor);
                effectX += font.MeasureString(label).X + 6;
            }
        }

        // Per-sprite damage flash is now rendered in MapCanvas.DrawEntities()
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
                    play.StatusMessage = $"Talked to {instance.DefinitionName}";
                    break;

                case EntityType.Item:
                    _gameStateManager.CollectItem(instance);
                    play.StatusMessage = $"Collected {instance.DefinitionName}";
                    break;

                case EntityType.Trap:
                    int damage = 0;
                    if (instance.Properties.TryGetValue("damage", out var dmgStr))
                        int.TryParse(dmgStr, out damage);
                    if (damage > 0)
                    {
                        _gameStateManager.DamagePlayer(damage);
                        TriggerDamageFlash();
                    }
                    play.StatusMessage = damage > 0
                        ? $"{instance.DefinitionName} dealt {damage} damage!"
                        : $"Triggered {instance.DefinitionName}";
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
                        play.StatusMessage = $"Transitioning to {targetMap}...";
                    }
                    else
                    {
                        play.StatusMessage = $"Triggered {instance.DefinitionName}";
                    }
                    break;

                case EntityType.Interactable:
                    if (TryShowDialogue(instance, play)) return;
                    play.StatusMessage = $"Interacted with {instance.DefinitionName}";
                    break;
                default:
                    play.StatusMessage = $"Interacted with {instance.DefinitionName}";
                    break;
            }

            play.StatusMessageTimer = PlayState.StatusMessageDuration;
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
                    play.StatusMessage = $"Took {group.DamagePerTick} {dmgType} damage!";
                    play.StatusMessageTimer = PlayState.StatusMessageDuration;
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
                play.StatusMessage = msg;
                play.StatusMessageTimer = PlayState.StatusMessageDuration;
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
                var result = _gameStateManager.AttackEntity(instance, _gameStateManager.GetEffectiveAttack());
                play.StatusMessage = result.Message;
                play.StatusMessageTimer = PlayState.StatusMessageDuration;
                TriggerEntityFlash(instance.Id);
                return true;
            }
        }
        return false;
    }

    private void ExecuteEntityTurn(PlayState play)
    {
        foreach (var entity in _gameStateManager.State.ActiveEntities)
        {
            if (!entity.IsActive) continue;
            if (!entity.Properties.ContainsKey("behavior")) continue;

            var action = EntityAI.DecideAction(entity, _gameStateManager.State, _pathfinder);

            switch (action.Type)
            {
                case EntityActionType.Move:
                    entity.X = action.TargetX;
                    entity.Y = action.TargetY;
                    break;

                case EntityActionType.Attack:
                    if (action.AttackTargetX == null)
                    {
                        // Melee: entity is adjacent to player
                        var atk = _gameStateManager.GetEntityIntProperty(entity, "attack", 3);
                        var damage = CombatHelper.CalculateDamage(atk, _gameStateManager.GetEffectiveDefense());
                        _gameStateManager.DamagePlayer(damage);
                        TriggerDamageFlash();
                        play.StatusMessage = $"{entity.DefinitionName} hit you for {damage} damage!";
                        play.StatusMessageTimer = PlayState.StatusMessageDuration;
                    }
                    break;
            }
        }

        // Check player death after all entities have acted
        if (!_gameStateManager.IsPlayerAlive())
        {
            play.StatusMessage = null;
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

    private bool TryShowDialogue(EntityInstance instance, PlayState play)
    {
        instance.Properties.TryGetValue("dialogue", out var dialogueValue);
        if (string.IsNullOrEmpty(dialogueValue))
            instance.Properties.TryGetValue("dialogue_id", out dialogueValue);
        if (string.IsNullOrEmpty(dialogueValue))
            return false;

        // Try file-based dialogue first, fall back to inline text
        var dialogue = LoadDialogue(dialogueValue);
        dialogue ??= CreateInlineDialogue(instance.DefinitionName, dialogueValue);

        ScreenManager.Push(new DialogueScreen(dialogue, _gameStateManager));
        play.StatusMessageTimer = 0;
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
        if (string.IsNullOrEmpty(_dialogueBasePath))
            return null;

        string path = Path.Combine(_dialogueBasePath, dialogueRef + ".json");
        if (!File.Exists(path))
            path = Path.Combine(_dialogueBasePath, "dialogues", dialogueRef + ".json");

        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<DialogueData>(json, options);
        }
        catch
        {
            return null;
        }
    }
}
