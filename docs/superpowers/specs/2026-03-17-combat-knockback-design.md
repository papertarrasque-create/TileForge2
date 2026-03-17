# Combat Pace Redesign: Knockback System

## Problem

Current bump combat is too fast. With 2 AP per turn, the player can spam-bump an enemy twice per turn with no positioning cost. The existing depth (flanking, poise, terrain defense, status effects) goes underutilized because there's no mechanical friction encouraging deliberate play.

## Solution

Add a knockback mechanic: when an attack lands, the defender is pushed 1 tile away from the attacker. This creates a natural bump-chase rhythm that halves attack frequency against normal enemies and makes positioning a core tactical concern.

## Design

### Core Mechanic

1. Attacker bumps defender -- damage resolves as normal (CombatHelper, flanking, terrain, poise unchanged)
2. Knockback direction: straight line away from attacker (opposite of attack direction, 4-cardinal only -- diagonal knockback is out of scope)
3. Check defender's `weight` property (default 1 if unset)
4. Check how many times the defender has been hit this turn (stagger count)
5. If `hitsThisTurn >= weight`: knockback succeeds, defender moves 1 tile in knockback direction
6. If knockback destination is blocked (wall, entity, unwalkable tile): defender stays put, no extra effect
7. Stagger counts reset at the start of each turn

**Symmetry:** Same rules apply to both player and entity attacks. When an enemy bumps the player, the player is knocked back under identical rules.

**On kill:** If the attack kills the defender, knockback is skipped. Dead entities are removed from the grid; there is no corpse to push.

**Entity-vs-entity:** Knockback only applies to player-vs-entity and entity-vs-player attacks. Entity-on-entity knockback is out of scope.

### Pacing Impact (2 AP base)

| Scenario | Flow | Hits/Turn |
|----------|------|-----------|
| vs Weight-1 (open) | Bump (1 AP) -> enemy pushed -> chase (1 AP) -> turn ends | 1 |
| vs Weight-2 (open) | Bump (1 AP) -> no knockback -> bump (1 AP) -> knockback, enemy pushed | 2 (knockback on 2nd) |
| vs Weight-2 (cornered) | Bump (1 AP) -> no knockback -> bump (1 AP) -> knockback blocked by wall | 2 (no escape) |
| vs Weight-3+ | Effectively immovable with 2 AP | 2 (stand-and-trade) |

### KnockbackResolver -- Static Pure Class

Follows the EntityAI pattern: static class, pure functions, state in / result out. No MonoGame dependencies.

**Signature:**

```
KnockbackResolver.Resolve(
    attackerX, attackerY,
    defenderX, defenderY,
    defenderWeight,
    hitsThisTurn,
    walkabilityCheck    // Func<int, int, bool> -- is tile walkable + unoccupied?
) -> KnockbackResult
```

**KnockbackResult** is a struct:
- `bool KnockedBack` -- did knockback occur?
- `int NewX, NewY` -- defender's new position (same as current if not knocked back)

**Logic:**
1. If `hitsThisTurn < defenderWeight` -> return not knocked back
2. Calculate knockback tile: `(defenderX + dx, defenderY + dy)` where `dx/dy` is direction from attacker to defender
3. If knockback tile is not walkable or is occupied -> return not knocked back
4. Return knocked back with new position

**File location:** `TileForge/Game/KnockbackResolver.cs` alongside CombatHelper.cs and EntityAI.cs.

### Stagger Tracking

**Location:** `PlayState` gets a new field:

```
Dictionary<string, int> HitsThisTurn  // key = entity ID (or "player"), value = hit count
```

**Lifecycle:**
- Cleared at the start of each player turn (`BeginPlayerTurn()`)
- Cleared at the start of each entity's sub-turn (in `ExecuteEntityTurn()` loop, per-entity). This is intentional: each entity's knockback is self-contained. A speed-2 enemy can knock back a weight-2 player (2 hits in its own sub-turn), but two separate speed-1 enemies cannot combine stagger against the same target.
- Incremented after each successful attack lands, before knockback resolution

**Not serialized.** Stagger is transient per-turn state that resets every turn boundary.

### Entity Weight Property

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `weight` | int | 1 | Resistance to knockback. Requires N hits in a single turn to push. |

Authored in GroupEditor like all other combat properties. Sensible range: 1-3.

**Design examples:**

| Entity | Weight | Behavior |
|--------|--------|----------|
| Rat, Skeleton | 1 | Knocked back every hit. One attack per turn. |
| Orc, Knight | 2 | Requires cornering or AP bonus to knock back. |
| Golem, Boss | 3 | Effectively immovable with base AP. |

### Player Weight

- Base value: 1 (on PlayerState)
- Equipment modifier: `equip_weight` -- heavy armor adds weight
- Effective weight: `base + sum(equipped equip_weight)`
- Accessed via `GameStateManager.GetEffectiveWeight()` (additive, same pattern as `GetEffectiveMaxAP()` and `GetEffectiveMaxPoise()`)

### GameplayScreen Integration

**Player attacks (TryBumpAttack):**
1. Damage resolves as normal (unchanged)
2. Increment `play.HitsThisTurn[targetId]`
3. Call `KnockbackResolver.Resolve(...)` with player pos, target pos, weight, hit count, walkability
4. If knocked back: update target entity grid position (X, Y), snap render position (instant, no lerp -- visual polish deferred)
5. Show floating message when knockback occurs

**Entity attacks (ExecuteEntityTurn):**
1. Damage resolves as normal (unchanged)
2. Increment `play.HitsThisTurn["player"]`
3. Call `KnockbackResolver.Resolve(...)` with entity pos, player pos, player effective weight, hit count
4. If knocked back: update player grid position
5. Show floating message for player knockback

**Turn boundary resets:**
- `BeginPlayerTurn()`: `play.HitsThisTurn.Clear()`
- `ExecuteEntityTurn()` per-entity loop start: `play.HitsThisTurn.Clear()`

**No changes to:** CombatHelper damage formula, flanking/positioning, poise, terrain defense, status effects, AP costs, EntityAI decision logic.

### Interactions With Existing Systems

- **Hazard tiles:** Knockback does NOT trigger hazard effects. Knockback is pure repositioning.
- **Flanking/backstab:** Unchanged. Position multipliers calculated at attack time, before knockback.
- **Poise:** Unchanged. Poise absorbs damage as before; knockback is independent of poise.
- **Map boundaries:** Knockback to out-of-bounds treated as blocked (defender stays put).
- **Entity facing:** Knocked-back entities do not change facing direction (they were pushed, not moved voluntarily).

## Testing Strategy

### KnockbackResolver Unit Tests

- Weight-1 defender knocked back on first hit
- Weight-2 defender not knocked back on first hit, knocked back on second
- Weight-3 defender requires 3 hits
- Blocked knockback (wall/unwalkable) -- defender stays put
- Blocked knockback (occupied tile) -- defender stays put
- Knockback direction correct for all 4 cardinal directions
- Default weight (unset property) treated as 1

### Stagger Tracking Tests

- Hit count increments per attack
- Hit count resets on player turn start
- Hit count resets per entity turn start
- Separate tracking per target entity ID
- Stagger does NOT carry over across turns (hit in turn N, no carryover to turn N+1)

### Integration Tests

- Player bumps weight-1 enemy -> enemy position changes
- Player bumps weight-1 enemy against wall -> enemy stays
- Player bumps weight-2 enemy once -> no knockback; twice -> knockback
- Enemy bumps weight-1 player -> player position changes
- Speed-2 enemy hits player twice -> knockback + chase + hit
- Player with equip_weight resists knockback
- Cornering tactic: weight-2 enemy against wall allows double-hit with knockback on second
- Knockback skipped when attack kills the defender
- Two separate speed-1 enemies do not combine stagger against player

## Decisions Made

- Defender gets knocked back (not attacker recoil or mutual separation)
- Blocked knockback has no extra effect (no wall-impact damage)
- Symmetric: same rules for player and entity attacks
- Weight system with per-turn stagger accumulation (not binary immunity)
- Stagger resets each turn (not persistent across turns)
- No hazard tile interaction on knockback
