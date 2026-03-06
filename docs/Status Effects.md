---
updated: 2026-03-06
status: current
---

# Status Effects

Status effects are **step-based** (tick per player movement, not real-time). They are deterministic and save-friendly.

## StatusEffect Structure

```
StatusEffect
  Type: string                    -- "fire", "poison", "ice", etc.
  RemainingSteps: int             -- Decrements each player move
  DamagePerStep: int              -- Applied per step (through poise)
  MovementMultiplier: float       -- Movement cost multiplier (default 1.0)
```

## Built-in Effects

Triggered by hazard tiles (`TileGroup.IsHazardous = true`, `DamageType` property):

| Effect | Duration | Damage/Step | Movement | Source |
|--------|----------|-------------|----------|--------|
| **Fire** | 3 steps | 1 | 1.0x (normal) | `DamageType = "fire"` |
| **Poison** | 6 steps | 1 | 1.0x (normal) | `DamageType = "poison"` |
| **Ice** | 3 steps | 0 | 2.0x (slowed) | `DamageType = "ice"` |
| **Spikes** | -- | Instant only | -- | `DamageType = "spikes"` |

Spikes deal instant damage (`DamagePerTick`) but apply no lingering effect.

## Application

`GameStateManager.ApplyStatusEffect()`:
- If an effect of the same type already exists, **replaces** it (resets duration)
- Otherwise adds a new effect to `ActiveEffects`
- Multiple different effects can be active simultaneously (e.g., fire + ice)

## Processing

`GameStateManager.ProcessStatusEffects()` -- called after each player movement:

1. Iterate all active effects
2. Apply damage if `DamagePerStep > 0` (via `DamagePlayer`, which goes through [[Combat|poise]])
3. Decrement `RemainingSteps`
4. Remove when `RemainingSteps <= 0`
5. Return list of message strings for display

## Movement Modifier

`GameStateManager.GetEffectiveMovementMultiplier()`:
- Multiplies all active effect multipliers together
- 1.0 if no effects
- Applied to movement duration: `duration = baseDuration * tileCost * multiplier`

Example: Ice effect (2.0x) + normal tile (1.0x cost) = movement takes twice as long.

## HUD Display

Active effects shown in [[Sidebar HUD]] as formatted text: `[BURNING 3]`, `[POISONED 2]`, `[FROZEN 1]`.

## Serialization

Status effects are part of `PlayerState.ActiveEffects` and persist through [[Save System|save/load]].

## Related

- [[Combat]] -- Damage from effects goes through poise
- [[Maps]] -- Hazard tile properties that trigger effects
- [[Property Reference]] -- Tile damage type properties
- [[Group Editor]] -- Setting DamageType and DamagePerTick
