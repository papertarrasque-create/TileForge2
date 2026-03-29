using System;
using System.Collections.Generic;
using System.Linq;
using TileForge.Data;

namespace TileForge.Game;

public class GameStateManager
{
    /// <summary>
    /// Persistence flag prefix. When an entity is deactivated (item collected, etc.),
    /// a flag with this prefix + entity ID is set so the entity stays inactive on map re-entry.
    /// </summary>
    public const string EntityInactivePrefix = "entity_inactive:";

    public GameState State { get; private set; } = new();

    private readonly List<string> _statusEffectMessages = new();

    /// <summary>
    /// Set by GameplayScreen when a Trigger entity is stepped on.
    /// Consumed by PlayModeController to execute the transition.
    /// </summary>
    public MapTransitionRequest PendingTransition { get; set; }

    /// <summary>
    /// Set by GameOverScreen when the player chooses "Restart".
    /// Consumed by PlayModeController to exit and re-enter play mode.
    /// </summary>
    public bool RestartRequested { get; set; }

    /// <summary>
    /// Replaces the current game state with a loaded state (for save/load).
    /// </summary>
    public void LoadState(GameState state)
    {
        State = state;

        // Backward-compat: old saves may not have MaxAP
        if (State.Player != null && State.Player.MaxAP <= 0)
            State.Player.MaxAP = 2;

        // Backward-compat: old saves may not have MaxPoise
        if (State.Player != null && State.Player.MaxPoise <= 0)
        {
            State.Player.MaxPoise = 10;
            State.Player.Poise = 10;
        }
    }

    /// Initialize from editor map data and groups.
    /// Finds the player entity, builds ActiveEntities list.
    public void Initialize(MapData map, IReadOnlyDictionary<string, TileGroup> groupsByName)
    {
        State = new GameState();

        // Find player entity
        Entity playerEntity = null;
        foreach (var entity in map.Entities)
        {
            if (groupsByName.TryGetValue(entity.GroupName, out var group) && group.IsPlayer)
            {
                playerEntity = entity;
                break;
            }
        }

        if (playerEntity != null)
        {
            State.Player = new PlayerState
            {
                X = playerEntity.X,
                Y = playerEntity.Y,
                Facing = Direction.Down,
                Health = 100,
                MaxHealth = 100,
                MaxAP = 2,
                Poise = 10,
                MaxPoise = 10,
            };
        }

        // Build active entities (excluding player)
        foreach (var entity in map.Entities)
        {
            if (groupsByName.TryGetValue(entity.GroupName, out var group) && group.IsPlayer)
                continue;

            var merged = MergeProperties(group, entity.Properties);
            bool spawnGated = !PassesSpawnConditions(merged, State);

            State.ActiveEntities.Add(new EntityInstance
            {
                Id = entity.Id,
                DefinitionName = entity.GroupName,
                X = entity.X,
                Y = entity.Y,
                Properties = merged,
                IsActive = !spawnGated,
            });
        }
    }

    /// <summary>
    /// Switch to a new map. Preserves player health, inventory, flags, and variables.
    /// Rebuilds ActiveEntities from the new map, applying entity persistence via flags.
    /// </summary>
    public void SwitchMap(LoadedMap loadedMap, int targetX, int targetY)
    {
        var groupsByName = loadedMap.Groups.ToDictionary(g => g.Name);

        // Update player position (health, inventory, facing are preserved)
        State.Player.X = targetX;
        State.Player.Y = targetY;

        // Update map ID
        State.CurrentMapId = loadedMap.Id;
        SetFlag($"visited_map:{loadedMap.Id}");

        // Rebuild active entities from new map, checking persistence flags
        State.ActiveEntities.Clear();
        foreach (var entity in loadedMap.Entities)
        {
            // Skip player entities
            if (groupsByName.TryGetValue(entity.DefinitionName, out var group) && group.IsPlayer)
                continue;

            var merged = MergeProperties(group, entity.Properties);

            // Persistence check: killed/collected entities stay in list as inactive
            bool persistedInactive = State.Flags.Contains(EntityInactivePrefix + entity.Id);

            // Spawn condition check: gated entities are added as inactive so
            // ReEvaluateSpawnConditions can reactivate them if conditions change
            bool spawnGated = !persistedInactive && !PassesSpawnConditions(merged, State);

            State.ActiveEntities.Add(new EntityInstance
            {
                Id = entity.Id,
                DefinitionName = entity.DefinitionName,
                X = entity.X,
                Y = entity.Y,
                Properties = merged,
                IsActive = !persistedInactive && !spawnGated,
            });
        }

        // Flags and Variables are preserved across map transitions
    }

    /// <summary>
    /// Marks an entity as persistently inactive by setting a flag.
    /// The entity will remain inactive when the map is revisited.
    /// </summary>
    public void DeactivateEntity(EntityInstance entity)
    {
        entity.IsActive = false;
        SetFlag(EntityInactivePrefix + entity.Id);
    }

    /// <summary>
    /// Re-checks spawn conditions for all entities. Deactivates those that no longer
    /// pass and reactivates those that now pass (unless permanently killed/collected).
    /// Spawn-condition deactivation is reversible — it does not set the persistent
    /// entity_inactive flag used by kills and collects.
    /// </summary>
    public void ReEvaluateSpawnConditions()
    {
        foreach (var entity in State.ActiveEntities)
        {
            bool passes = PassesSpawnConditions(entity.Properties, State);
            if (entity.IsActive && !passes)
            {
                // Reversible deactivation — no persistent flag
                entity.IsActive = false;
            }
            else if (!entity.IsActive && passes && !HasFlag(EntityInactivePrefix + entity.Id))
            {
                // Reactivate: spawn conditions pass and entity was not permanently killed/collected
                entity.IsActive = true;
            }
        }
    }

    private static bool PassesSpawnConditions(Dictionary<string, string> properties, GameState state)
    {
        var requires = PropertyAccess.GetString(properties, PropertyKeys.SpawnRequiresFlag);
        if (!string.IsNullOrEmpty(requires) && !state.Flags.Contains(requires))
            return false;

        var forbids = PropertyAccess.GetString(properties, PropertyKeys.SpawnForbidsFlag);
        if (!string.IsNullOrEmpty(forbids) && state.Flags.Contains(forbids))
            return false;

        return true;
    }

    private static Dictionary<string, string> MergeProperties(TileGroup group, Dictionary<string, string> overrides)
    {
        var props = new Dictionary<string, string>();
        if (group?.DefaultProperties != null)
        {
            foreach (var kvp in group.DefaultProperties)
                props[kvp.Key] = kvp.Value;
        }
        foreach (var kvp in overrides)
            props[kvp.Key] = kvp.Value;
        return props;
    }

    // Flag operations
    public void SetFlag(string flag) => State.Flags.Add(flag);
    public void ClearFlag(string flag) => State.Flags.Remove(flag);
    public bool HasFlag(string flag) => State.Flags.Contains(flag);

    // Variable operations
    public void SetVariable(string key, string value) => State.Variables[key] = value;
    public string GetVariable(string key) => State.Variables.TryGetValue(key, out var value) ? value : null;

    /// <summary>
    /// Increments an integer variable by 1. Creates the variable if missing (starting from 0).
    /// Used by entity event hooks (on_kill_increment, on_collect_increment).
    /// </summary>
    public void IncrementVariable(string key)
    {
        int current = 0;
        if (State.Variables.TryGetValue(key, out var val))
            int.TryParse(val, out current);
        State.Variables[key] = (current + 1).ToString();
    }

    // Health operations

    /// <summary>
    /// True if the most recent DamagePlayer call reduced poise from above 0 to 0.
    /// Checked by GameplayScreen to show "POISE BROKEN!" floating message.
    /// </summary>
    public bool LastDamageBrokePoise { get; private set; }

    public void DamagePlayer(int amount)
    {
        LastDamageBrokePoise = false;
        if (State.Player.Poise > 0)
        {
            int absorbed = Math.Min(State.Player.Poise, amount);
            bool hadPoise = State.Player.Poise > 0;
            State.Player.Poise -= absorbed;
            amount -= absorbed;
            if (hadPoise && State.Player.Poise <= 0)
                LastDamageBrokePoise = true;
        }
        if (amount > 0)
            State.Player.Health = Math.Max(0, State.Player.Health - amount);
    }

    public void HealPlayer(int amount)
    {
        State.Player.Health = Math.Min(State.Player.MaxHealth, State.Player.Health + amount);
    }

    public bool IsPlayerAlive() => State.Player.Health > 0;

    // Status effect operations

    /// <summary>
    /// Applies a status effect. If an effect of the same type already exists, it is replaced.
    /// </summary>
    public void ApplyStatusEffect(string type, int remainingSteps, int damagePerStep, float movementMultiplier)
    {
        var effects = State.Player.ActiveEffects;
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            if (effects[i].Type == type)
            {
                effects.RemoveAt(i);
                break;
            }
        }

        effects.Add(new StatusEffect
        {
            Type = type,
            RemainingSteps = remainingSteps,
            DamagePerStep = damagePerStep,
            MovementMultiplier = movementMultiplier,
        });
    }

    /// <summary>
    /// Processes all active status effects: applies damage, decrements steps, removes expired.
    /// Returns a list of status messages for the HUD.
    /// </summary>
    public List<string> ProcessStatusEffects()
    {
        _statusEffectMessages.Clear();
        var messages = _statusEffectMessages;
        var effects = State.Player.ActiveEffects;

        for (int i = effects.Count - 1; i >= 0; i--)
        {
            var effect = effects[i];
            if (effect.DamagePerStep > 0)
            {
                DamagePlayer(effect.DamagePerStep);
                messages.Add($"{effect.Type} dealt {effect.DamagePerStep} damage!");
            }

            effect.RemainingSteps--;
            if (effect.RemainingSteps <= 0)
            {
                effects.RemoveAt(i);
                messages.Add($"{effect.Type} effect wore off.");
            }
        }

        return messages;
    }

    /// <summary>
    /// Returns the combined movement multiplier from all active effects (multiplicative).
    /// 1.0 if no effects are active.
    /// </summary>
    public float GetEffectiveMovementMultiplier()
    {
        float multiplier = 1.0f;
        foreach (var effect in State.Player.ActiveEffects)
            multiplier *= effect.MovementMultiplier;
        return multiplier;
    }

    // Inventory operations
    public void AddToInventory(string itemId) => State.Player.Inventory.Add(itemId);
    public bool HasItem(string itemId) => State.Player.Inventory.Contains(itemId);
    public bool RemoveFromInventory(string itemId) => State.Player.Inventory.Remove(itemId);

    /// <summary>
    /// Collects an item entity: adds to inventory, caches its properties, and deactivates it.
    /// The property cache survives map transitions so InventoryScreen can resolve item properties.
    /// </summary>
    public void CollectItem(EntityInstance entity)
    {
        AddToInventory(entity.DefinitionName);
        if (entity.Properties.Count > 0 && !State.ItemPropertyCache.ContainsKey(entity.DefinitionName))
            State.ItemPropertyCache[entity.DefinitionName] = new Dictionary<string, string>(entity.Properties);
        DeactivateEntity(entity);

        // Process entity collect event hooks for quest tracking
        var collectFlag = PropertyAccess.GetString(entity.Properties, PropertyKeys.OnCollectSetFlag);
        if (!string.IsNullOrEmpty(collectFlag))
            SetFlag(collectFlag);
        var collectVar = PropertyAccess.GetString(entity.Properties, PropertyKeys.OnCollectIncrement);
        if (!string.IsNullOrEmpty(collectVar))
            IncrementVariable(collectVar);
    }

    // Equipment operations

    /// <summary>
    /// Equips an item from inventory into the specified slot.
    /// If the slot already has an item, that item is returned to inventory first.
    /// Removes one instance of the item from inventory.
    /// </summary>
    public void EquipItem(string itemName, EquipmentSlot slot)
    {
        string slotKey = slot.ToString();

        // Unequip existing item in this slot (return to inventory)
        if (State.Player.Equipment.TryGetValue(slotKey, out var existing))
        {
            AddToInventory(existing);
            State.Player.Equipment.Remove(slotKey);
        }

        // Move from inventory to equipment slot
        RemoveFromInventory(itemName);
        State.Player.Equipment[slotKey] = itemName;
    }

    /// <summary>
    /// Unequips an item from the specified slot, returning it to inventory.
    /// No-op if the slot is empty.
    /// </summary>
    public void UnequipItem(EquipmentSlot slot)
    {
        string slotKey = slot.ToString();
        if (State.Player.Equipment.TryGetValue(slotKey, out var itemName))
        {
            State.Player.Equipment.Remove(slotKey);
            AddToInventory(itemName);
        }
    }

    /// <summary>
    /// Returns true if the named item is currently equipped in any slot.
    /// </summary>
    public bool IsEquipped(string itemName) => State.Player.Equipment.ContainsValue(itemName);

    /// <summary>
    /// Returns the item name equipped in the given slot, or null if empty.
    /// </summary>
    public string GetEquippedItem(EquipmentSlot slot)
    {
        return State.Player.Equipment.TryGetValue(slot.ToString(), out var item) ? item : null;
    }

    /// <summary>
    /// Returns the EquipmentSlot for an item based on its cached equip_slot property,
    /// or null if the item is not equippable.
    /// </summary>
    public EquipmentSlot? GetItemEquipSlot(string itemName)
    {
        if (State.ItemPropertyCache.TryGetValue(itemName, out var props)
            && props.TryGetValue(PropertyKeys.EquipSlot, out var slotStr)
            && Enum.TryParse<EquipmentSlot>(slotStr, ignoreCase: true, out var slot))
            return slot;
        return null;
    }

    /// <summary>
    /// Returns effective attack: base player attack + sum of equip_attack bonuses from all equipped items.
    /// </summary>
    public int GetEffectiveAttack()
    {
        return State.Player.Attack + GetEquipmentBonus(PropertyKeys.EquipAttack);
    }

    /// <summary>
    /// Returns effective defense: base player defense + sum of equip_defense bonuses from all equipped items.
    /// </summary>
    public int GetEffectiveDefense()
    {
        return State.Player.Defense + GetEquipmentBonus(PropertyKeys.EquipDefense);
    }

    /// <summary>
    /// Returns effective max AP: base MaxAP + sum of equip_ap bonuses from all equipped items.
    /// </summary>
    public int GetEffectiveMaxAP()
    {
        return State.Player.MaxAP + GetEquipmentBonus(PropertyKeys.EquipAp);
    }

    /// <summary>
    /// Returns effective max poise: base MaxPoise + sum of equip_poise bonuses from all equipped items.
    /// </summary>
    public int GetEffectiveMaxPoise()
    {
        return State.Player.MaxPoise + GetEquipmentBonus(PropertyKeys.EquipPoise);
    }

    /// <summary>
    /// Returns effective weight: base player weight + sum of equip_weight bonuses from all equipped items.
    /// </summary>
    public int GetEffectiveWeight()
    {
        return State.Player.Weight + GetEquipmentBonus(PropertyKeys.EquipWeight);
    }

    /// <summary>
    /// Regenerates poise by max(1, effectiveMaxPoise / 4), capped at effective max.
    /// Returns the amount regenerated (0 if already full).
    /// </summary>
    public int RegeneratePoise()
    {
        int maxPoise = GetEffectiveMaxPoise();
        if (State.Player.Poise >= maxPoise)
            return 0;
        int amount = Math.Max(1, maxPoise / 4);
        int before = State.Player.Poise;
        State.Player.Poise = Math.Min(maxPoise, State.Player.Poise + amount);
        return State.Player.Poise - before;
    }

    private int GetEquipmentBonus(string propertyKey)
    {
        int total = 0;
        foreach (var kvp in State.Player.Equipment)
        {
            if (State.ItemPropertyCache.TryGetValue(kvp.Value, out var props))
                total += PropertyAccess.GetInt(props, propertyKey);
        }
        return total;
    }

    /// <summary>
    /// Returns true if the entity is currently hostile. Checks flag overrides first
    /// (friendly_flag makes non-hostile, hostile_flag makes hostile), then falls back
    /// to the entity's "hostile" property. Default is hostile (backward compatible).
    /// </summary>
    public bool IsEntityHostile(EntityInstance entity)
    {
        var ff = PropertyAccess.GetString(entity.Properties, PropertyKeys.FriendlyFlag);
        if (!string.IsNullOrEmpty(ff) && State.Flags.Contains(ff))
            return false;
        var hf = PropertyAccess.GetString(entity.Properties, PropertyKeys.HostileFlag);
        if (!string.IsNullOrEmpty(hf) && State.Flags.Contains(hf))
            return true;
        if (entity.Properties.TryGetValue(PropertyKeys.Hostile, out var h))
            return !string.Equals(h, "false", StringComparison.OrdinalIgnoreCase);
        return true;
    }

    /// <summary>
    /// Returns true if the entity can be attacked: is active, hostile, has health > 0,
    /// and its EntityType is NPC or Trap (Items and Triggers are never attackable).
    /// </summary>
    public bool IsAttackable(EntityInstance entity, IReadOnlyDictionary<string, TileGroup> groupsByName)
    {
        if (!entity.IsActive) return false;
        if (!IsEntityHostile(entity)) return false;

        int health = PropertyAccess.GetInt(entity.Properties, PropertyKeys.Health);
        if (health <= 0) return false;

        if (!groupsByName.TryGetValue(entity.DefinitionName, out var group))
            return false;

        return group.EntityType == EntityType.NPC || group.EntityType == EntityType.Trap;
    }

    /// <summary>
    /// Attacks an entity with terrain and positional modifiers.
    /// </summary>
    public AttackResult AttackEntity(EntityInstance entity, int attackerAttack, int terrainBonus, float positionMultiplier)
    {
        int defense = PropertyAccess.GetInt(entity.Properties, PropertyKeys.Defense);

        // Entity poise: absorb damage through poise first
        int entityPoise = PropertyAccess.GetInt(entity.Properties, PropertyKeys.Poise);
        int rawDamage = CombatHelper.CalculateDamage(attackerAttack, defense, terrainBonus, positionMultiplier);

        int damage = rawDamage;
        if (entityPoise > 0)
        {
            int absorbed = Math.Min(entityPoise, damage);
            entityPoise -= absorbed;
            damage -= absorbed;
            PropertyAccess.SetInt(entity.Properties, PropertyKeys.Poise, entityPoise);
        }

        int currentHealth = PropertyAccess.GetInt(entity.Properties, PropertyKeys.Health);
        int newHealth = Math.Max(0, currentHealth - damage);
        PropertyAccess.SetInt(entity.Properties, PropertyKeys.Health, newHealth);

        bool killed = newHealth <= 0;
        if (killed)
        {
            DeactivateEntity(entity);

            var killFlag = PropertyAccess.GetString(entity.Properties, PropertyKeys.OnKillSetFlag);
            if (!string.IsNullOrEmpty(killFlag))
                SetFlag(killFlag);
            var killVar = PropertyAccess.GetString(entity.Properties, PropertyKeys.OnKillIncrement);
            if (!string.IsNullOrEmpty(killVar))
                IncrementVariable(killVar);
        }

        int maxHealth = PropertyAccess.GetInt(entity.Properties, PropertyKeys.MaxHealth, currentHealth);
        string xpStr = "";
        if (killed)
        {
            int xp = PropertyAccess.GetInt(entity.Properties, PropertyKeys.Xp);
            xpStr = xp > 0 ? $" (+{xp} XP)" : "";
        }

        string message = killed
            ? $"{entity.DefinitionName} defeated!{xpStr}"
            : $"Hit {entity.DefinitionName} for {rawDamage}! ({newHealth}/{maxHealth} HP)";

        return new AttackResult
        {
            DamageDealt = rawDamage,
            RemainingHealth = newHealth,
            Killed = killed,
            TargetName = entity.DefinitionName,
            Message = message,
        };
    }

    public AttackResult AttackEntity(EntityInstance entity, int attackerAttack)
        => AttackEntity(entity, attackerAttack, 0, 1.0f);
}
