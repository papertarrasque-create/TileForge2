---
updated: 2026-03-06
status: current
---

# Noise and Alertness

A tile-based stealth system where player movement propagates noise, alerting dormant enemies.

## Tile Noise Levels

`TileGroup.NoiseLevel` (integer, default 1):

| Value | Label | Effect |
|-------|-------|--------|
| 0 | Silent | No noise propagation (carpet, grass) |
| 1 | Normal | Standard step noise |
| 2 | Loud | Extended noise radius (gravel, metal) |

Set in the [[Group Editor]] via the Noise dropdown.

## Noise Propagation

After player completes a movement step:

1. Get noise level of destination tile
2. Calculate radius: `3 * noiseLevel`
3. Find all dormant entities within radius (Manhattan distance)
4. Set `alert_turns = 3` on each

| Noise Level | Radius | Range |
|-------------|--------|-------|
| 0 (Silent) | 0 | No propagation |
| 1 (Normal) | 3 tiles | Nearby enemies |
| 2 (Loud) | 6 tiles | Wide area alert |

"Dormant" means entities outside their normal aggro range that would otherwise ignore the player.

## Alert System

When an entity receives `alert_turns`:

- **Aggro range doubled** in chase/chase_patrol behavior
- **AnyHostileNearby()** accounts for alerted entities -- prevents auto-end-turn
- Floating "!" message (Yellow) when entity first alerted
- `alert_turns` decrements by 1 after each entity turn
- After 3 turns without re-alerting: entity returns to dormant state

### Effect on Combat

Alerted enemies with doubled aggro range can detect the player from much further away:

| Base Aggro | Normal Range | Alerted Range |
|-----------|-------------|--------------|
| 5 (default) | 5 tiles | 10 tiles |
| 3 | 3 tiles | 6 tiles |
| 8 | 8 tiles | 16 tiles |

This creates a stealth dynamic: stepping on loud tiles can alert enemies across large areas, while silent tiles allow sneaking past.

### Effect on Turn System

The auto-end-turn feature (see [[Combat]]) checks `AnyHostileNearby()`. Alerted enemies count as "nearby" even if they're beyond normal aggro range, keeping the player in combat mode.

## Gameplay Design

The noise/alertness system creates Moonring-inspired gameplay loops:
- **Loud approach:** Fast movement on normal/loud tiles, but enemies detect early
- **Stealth approach:** Careful pathing on silent tiles, bypass dormant enemies
- **Risk/reward:** Some optimal paths may cross loud terrain

## Related

- [[Combat]] -- Auto-end-turn and aggro range mechanics
- [[Entities]] -- AI behaviors affected by alert state
- [[Maps]] -- Tile noise level properties
- [[Property Reference]] -- `alert_turns`, `aggro_range` properties
