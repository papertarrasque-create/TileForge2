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
            };
        }

        // Build active entities (excluding player)
        foreach (var entity in map.Entities)
        {
            if (groupsByName.TryGetValue(entity.GroupName, out var group) && group.IsPlayer)
                continue;

            // Merge group DefaultProperties as base, then overlay with instance overrides
            var props = new Dictionary<string, string>();
            if (group?.DefaultProperties != null)
            {
                foreach (var kvp in group.DefaultProperties)
                    props[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in entity.Properties)
                props[kvp.Key] = kvp.Value;

            State.ActiveEntities.Add(new EntityInstance
            {
                Id = entity.Id,
                DefinitionName = entity.GroupName,
                X = entity.X,
                Y = entity.Y,
                Properties = props,
                IsActive = true,
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

            // Merge group DefaultProperties as base, then overlay with instance overrides
            var props = new Dictionary<string, string>();
            if (group?.DefaultProperties != null)
            {
                foreach (var kvp in group.DefaultProperties)
                    props[kvp.Key] = kvp.Value;
            }
            foreach (var kvp in entity.Properties)
                props[kvp.Key] = kvp.Value;

            State.ActiveEntities.Add(new EntityInstance
            {
                Id = entity.Id,
                DefinitionName = entity.DefinitionName,
                X = entity.X,
                Y = entity.Y,
                Properties = props,
                IsActive = !State.Flags.Contains(EntityInactivePrefix + entity.Id),
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

    // Flag operations
    public void SetFlag(string flag) => State.Flags.Add(flag);
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
    public void DamagePlayer(int amount)
    {
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
        if (entity.Properties.TryGetValue("on_collect_set_flag", out var collectFlag)
            && !string.IsNullOrEmpty(collectFlag))
            SetFlag(collectFlag);
        if (entity.Properties.TryGetValue("on_collect_increment", out var collectVar)
            && !string.IsNullOrEmpty(collectVar))
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
            && props.TryGetValue("equip_slot", out var slotStr)
            && Enum.TryParse<EquipmentSlot>(slotStr, ignoreCase: true, out var slot))
            return slot;
        return null;
    }

    /// <summary>
    /// Returns effective attack: base player attack + sum of equip_attack bonuses from all equipped items.
    /// </summary>
    public int GetEffectiveAttack()
    {
        return State.Player.Attack + GetEquipmentBonus("equip_attack");
    }

    /// <summary>
    /// Returns effective defense: base player defense + sum of equip_defense bonuses from all equipped items.
    /// </summary>
    public int GetEffectiveDefense()
    {
        return State.Player.Defense + GetEquipmentBonus("equip_defense");
    }

    /// <summary>
    /// Returns effective max AP: base MaxAP + sum of equip_ap bonuses from all equipped items.
    /// </summary>
    public int GetEffectiveMaxAP()
    {
        return State.Player.MaxAP + GetEquipmentBonus("equip_ap");
    }

    private int GetEquipmentBonus(string propertyKey)
    {
        int total = 0;
        foreach (var kvp in State.Player.Equipment)
        {
            if (State.ItemPropertyCache.TryGetValue(kvp.Value, out var props)
                && props.TryGetValue(propertyKey, out var val)
                && int.TryParse(val, out var bonus))
                total += bonus;
        }
        return total;
    }

    // Entity property helpers
    public int GetEntityIntProperty(EntityInstance entity, string key, int defaultValue = 0)
    {
        if (entity.Properties.TryGetValue(key, out var value) && int.TryParse(value, out var result))
            return result;
        return defaultValue;
    }

    public void SetEntityIntProperty(EntityInstance entity, string key, int value)
    {
        entity.Properties[key] = value.ToString();
    }

    /// <summary>
    /// Returns true if the entity is currently hostile. Checks flag overrides first
    /// (friendly_flag makes non-hostile, hostile_flag makes hostile), then falls back
    /// to the entity's "hostile" property. Default is hostile (backward compatible).
    /// </summary>
    public bool IsEntityHostile(EntityInstance entity)
    {
        if (entity.Properties.TryGetValue("friendly_flag", out var ff)
            && !string.IsNullOrEmpty(ff) && State.Flags.Contains(ff))
            return false;
        if (entity.Properties.TryGetValue("hostile_flag", out var hf)
            && !string.IsNullOrEmpty(hf) && State.Flags.Contains(hf))
            return true;
        if (entity.Properties.TryGetValue("hostile", out var h))
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

        int health = GetEntityIntProperty(entity, "health", 0);
        if (health <= 0) return false;

        if (!groupsByName.TryGetValue(entity.DefinitionName, out var group))
            return false;

        return group.EntityType == EntityType.NPC || group.EntityType == EntityType.Trap;
    }

    /// <summary>
    /// Attacks an entity: calculates damage, reduces health, deactivates if killed.
    /// Returns an AttackResult with the outcome.
    /// </summary>
    public AttackResult AttackEntity(EntityInstance entity, int attackerAttack)
    {
        int defense = GetEntityIntProperty(entity, "defense", 0);
        int damage = CombatHelper.CalculateDamage(attackerAttack, defense);

        int currentHealth = GetEntityIntProperty(entity, "health", 0);
        int newHealth = Math.Max(0, currentHealth - damage);
        SetEntityIntProperty(entity, "health", newHealth);

        bool killed = newHealth <= 0;
        if (killed)
        {
            DeactivateEntity(entity);

            // Process entity kill event hooks for quest tracking
            if (entity.Properties.TryGetValue("on_kill_set_flag", out var killFlag)
                && !string.IsNullOrEmpty(killFlag))
                SetFlag(killFlag);
            if (entity.Properties.TryGetValue("on_kill_increment", out var killVar)
                && !string.IsNullOrEmpty(killVar))
                IncrementVariable(killVar);
        }

        int maxHealth = GetEntityIntProperty(entity, "max_health", currentHealth);
        string xpStr = "";
        if (killed)
        {
            int xp = GetEntityIntProperty(entity, "xp", 0);
            xpStr = xp > 0 ? $" (+{xp} XP)" : "";
        }

        string message = killed
            ? $"{entity.DefinitionName} defeated!{xpStr}"
            : $"Hit {entity.DefinitionName} for {damage}! ({newHealth}/{maxHealth} HP)";

        return new AttackResult
        {
            DamageDealt = damage,
            RemainingHealth = newHealth,
            Killed = killed,
            TargetName = entity.DefinitionName,
            Message = message,
        };
    }
}
