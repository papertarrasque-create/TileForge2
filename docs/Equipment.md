---
updated: 2026-03-06
status: current
---

# Equipment System

Equipment provides stat bonuses via three slots. Stats are **computed, not stored** -- effective values are calculated from base stats plus equipment bonuses.

## Equipment Slots

`EquipmentSlot` enum:

| Slot | Purpose |
|------|---------|
| **Weapon** | Primary attack modifier |
| **Armor** | Primary defense modifier |
| **Accessory** | Flexible bonus (any stat) |

## Item Properties

Items become equippable by setting these properties in the [[Group Editor]]:

| Property | Type | Effect |
|----------|------|--------|
| `equip_slot` | string | "Weapon", "Armor", or "Accessory" |
| `equip_attack` | int | Bonus to attack stat |
| `equip_defense` | int | Bonus to defense stat |
| `equip_ap` | int | Bonus to max AP per turn |
| `equip_poise` | int | Bonus to max poise |

All bonuses are **additive** -- multiple items in different slots stack.

## Effective Stat Calculation

`GameStateManager` provides computed stats:

```
GetEffectiveAttack()  = Player.Attack  + sum(equip_attack from all equipped items)
GetEffectiveDefense() = Player.Defense + sum(equip_defense from all equipped items)
GetEffectiveMaxAP()   = Player.MaxAP   + sum(equip_ap from all equipped items)
GetEffectiveMaxPoise()= Player.MaxPoise+ sum(equip_poise from all equipped items)
```

`GetEquipmentBonus(propertyKey)` iterates all equipped items, looks up each in the `ItemPropertyCache`, parses the property value as int, and sums them.

## Item Property Cache

When an item is collected (`CollectItem()`), its entity properties are cached in `GameState.ItemPropertyCache`:

```
ItemPropertyCache[itemName] = copy of entity properties dict
```

This cache:
- Survives map transitions (cached at collection time)
- Enables equipment stat lookups without access to the original entity
- Persists in save files
- Is the **only** way equipment bonuses are resolved

## Equip/Unequip

Managed via `GameStateManager`:

**Equip:**
1. Remove any existing item in target slot (returns to inventory)
2. Remove item from inventory
3. Add to `Equipment[slot.ToString()]`

**Unequip:**
1. Remove item from `Equipment[slot]`
2. Add back to inventory

**Query:**
- `GetEquippedItem(slot)` -- returns item name or null
- `IsEquipped(itemName)` -- true if in any slot

## Inventory Screen

The `InventoryScreen` handles equip/unequip UI:
- Items with `equip_slot` show equip option
- Currently equipped items show unequip option
- Grouped display with counts for non-equippable items

## HUD Display

The [[Sidebar HUD]] shows:
- **ATK/DEF stats** -- Effective values (base + equipment)
- **Equipment section** -- Weapon, Armor, Accessory names (or "-" if empty)
- **Inventory section** -- All held items with counts

## Save Compatibility

`GameState.Version` was bumped to 2 when equipment was added. V1 saves get an empty Equipment dict on load.

## Related

- [[Combat]] -- How effective stats feed into damage calculation
- [[Entities]] -- Item entity type and collection
- [[Property Reference]] -- Equipment property keys
- [[Sidebar HUD]] -- Equipment display
- [[Group Editor]] -- Where equipment properties are authored
