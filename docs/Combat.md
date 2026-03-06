---
updated: 2026-03-06
status: current
---

# Combat System

TileForge uses an **Action Point (AP) based tactical combat system**. Combat is deterministic -- no RNG. Damage depends only on attack, defense, terrain, and positioning.

## Action Points

Each turn, the player gets AP to spend on actions:

| Action | AP Cost |
|--------|---------|
| Move (one tile) | 1 AP |
| Bump attack (walk into enemy) | 1 AP |
| Directional attack (Interact key) | 1 AP |
| Friendly interaction (dialogue, NPC talk) | 0 AP |
| End turn (Space) | Forfeits remaining AP |

**Base MaxAP:** 2 (configurable via `PlayerState.MaxAP`)
**Equipment modifier:** `equip_ap` property on items adds to MaxAP via `GetEffectiveMaxAP()`

AP refills at the start of each player turn via `BeginPlayerTurn()`.

### Auto-End Turn

When no hostile entity with a `behavior` property is within its `aggro_range` (Manhattan distance), the turn ends immediately after each action. The player never notices the AP system during exploration -- it only surfaces in combat. Alert-aware: `AnyHostileNearby()` accounts for entities with doubled aggro range from the [[Noise and Alertness]] system.

## Damage Formula

Three overloads in `CombatHelper.CalculateDamage()`:

```
Basic:          damage = max(1, attack - defense)
With terrain:   damage = max(1, attack - (defense + terrainBonus))
With position:  damage = max(1, (attack - (defense + terrainBonus)) * positionMultiplier)
```

**Key rules:**
- Minimum damage is always 1 (no zero-damage hits)
- Terrain defense stacks additively with entity defense
- Position multiplier applies after defense subtraction
- Result is floored back to minimum 1 after multiplication

See [[Equipment]] for how effective attack/defense are calculated.

## Terrain Defense

`TileGroup.DefenseBonus` (integer, default 0) adds to the defender's defense value. Retrieved via `GetDefenseBonusAt(x, y)` which takes the max bonus from all map layers at that tile position.

The [[Sidebar HUD]] shows `COVER:+n` in blue when standing on defensive terrain.

See [[Group Editor]] for how DefenseBonus is set (Def dropdown: 0/1/2/3/5).

## Positional Combat

`AttackPosition` enum determines damage multipliers based on attacker position relative to defender's facing direction:

| Position | Multiplier | Condition | Float Message |
|----------|-----------|-----------|---------------|
| Front | 1.0x | Attacker faces defender head-on | -- |
| Flank | 1.5x | Attacker is perpendicular to facing | "Flanked!" (Orange) |
| Backstab | 2.0x | Attacker is behind defender | "BACKSTAB!" (OrangeRed) |

`CombatHelper.GetAttackPosition()` compares attacker X/Y to defender X/Y relative to the defender's `Direction` facing. Both player and entity attacks use positioning.

### Entity Facing

Tracked in `PlayState.EntityFacings` (Dictionary<string, Direction>). Updated when entities move -- horizontal movement takes priority for sprite flip, but all 4 directions are tracked for combat logic. Default is `Direction.Down` if untracked.

## Poise (Shield System)

Poise is a regenerating damage buffer that absorbs hits before health.

### Player Poise

- `PlayerState.Poise` / `MaxPoise` (default 20)
- Effective max: base + sum of `equip_poise` from [[Equipment]]
- Damage flow: `Poise > 0` -> absorb `min(poise, damage)` -> remainder hits health
- When poise goes from >0 to 0: `LastDamageBrokePoise` flag triggers "POISE BROKEN!" floating message (OrangeRed)

### Poise Regeneration

Called in `BeginPlayerTurn()` when `AnyHostileNearby()` returns false:

```
regen = max(1, effectiveMaxPoise / 4)
poise = min(poise + regen, effectiveMaxPoise)
```

This creates a risk/reward loop (inspired by Moonring): press the attack or retreat to recover.

### Entity Poise

Entities with a `poise` property in their property bag get a poise buffer. Works the same as player poise -- damage absorbed before health. Tracked in `entity.Properties["poise"]` (string, parsed to int). Entity poise does not regenerate.

### Poise Bar

Displayed in [[Sidebar HUD]] below the health bar. Color shifts by percentage:
- Blue: > 50%
- Yellow: > 25%
- Red: <= 25%

## Entity Speed

Entities have a `speed` property (int, clamped 1-3) determining actions per turn:

| Speed | Actions | Use Case |
|-------|---------|----------|
| 1 | 1 move OR 1 attack | Standard enemies |
| 2 | Move + attack, or 2 moves | Fast enemies |
| 3 | 3 actions per turn | Very fast enemies |

Each entity gets `speed` AP per turn. `EntityAI.DecideAction()` is called per AP -- since it's stateless, re-evaluation after each action works naturally.

## Turn Sequence

1. **Player Turn Start** -- `BeginPlayerTurn()`: refill AP, check poise regen (if no hostiles nearby)
2. **Player Actions** -- Each action consumes AP, triggers `AfterPlayerAction()`
3. **Turn End** -- Auto-end (no hostiles) or manual (Space key) or AP exhausted
4. **Entity Turn** -- `ExecuteEntityTurn()`:
   - Iterate all active entities with `behavior` property
   - Each entity gets `speed` AP
   - `EntityAI.DecideAction()` returns Move/Attack/Idle per AP
   - `alert_turns` decrements by 1 after entity acts
5. **Back to Player Turn**

Entity turns also fire after bump attacks (not just movement), ensuring enemies respond to being hit.

## Floating Messages

Combat events display as floating text at the relevant tile position:

| Event | Color | Example |
|-------|-------|---------|
| Damage dealt | Gold | "5 dmg" |
| Damage taken | Red | "-3 HP" |
| Backstab | OrangeRed | "BACKSTAB!" |
| Flanked | Orange | "Flanked!" |
| Poise broken | OrangeRed | "POISE BROKEN!" |
| Poise regen | CornflowerBlue | "+5 PP" |

Floating messages drift 16px upward over 1 second with alpha fade. They are visual-only -- the [[Sidebar HUD]] GameLog is the persistent record.

## Related

- [[Entities]] -- AI behaviors that drive enemy combat actions
- [[Equipment]] -- How effective stats are calculated
- [[Noise and Alertness]] -- How noise affects combat engagement
- [[Property Reference]] -- All combat-related entity properties
- [[Sidebar HUD]] -- Combat message display
