# Combat Rebalance Design Spec

**Date:** 2026-03-29
**Goal:** Rebalance player and enemy stats so combat feels tactical with a progressive difficulty curve across two enemy tiers, using only existing property levers.

## Design Philosophy

- **Approach B: Raise the floor, widen the spread** -- Player keeps 100 HP (forgiving) but reduced poise (10) so damage sticks between encounters. Defense bumped to 3 to create meaningful spread between enemy tiers.
- **Two tiers, each with a fast/weak and slow/strong archetype.** Tier 1 (early areas): Rat + Goblin/Skeleton. Tier 2 (mid areas): Ghost + Zombie.
- **Danger comes from sustained fights and groups**, not single enemies one-shotting the player. Tier 2 enemies threaten through attrition and force target prioritization.
- **No new mechanics.** All tuning uses existing levers: HP, attack, defense, speed, weight, poise, aggro_range, behavior, XP.

## Player Rebalance

| Stat | Old | New | Rationale |
|------|-----|-----|-----------|
| HP | 100 | 100 | Forgiving health pool for new players |
| Max Poise | 20 | 10 | Breaks in real fights; regen (2/turn) is meaningful but not instant |
| Attack | 5 | 5 | Equipment is the scaling lever for offense |
| Defense | 2 | 3 | Rats deal minimum damage; creates headroom against Tier 2 |
| MaxAP | 2 | 2 | Unchanged |
| Weight | 1 | 1 | Unchanged |

Poise regen: `max(1, 10/4) = 2` per out-of-combat turn. Full recharge in 5 idle turns.

## Tier 1 Enemies (Early Areas)

### Rat -- Fast, fragile swarm fodder

| Stat | Value | Notes |
|------|-------|-------|
| health | 8 | Dies in 2 player hits |
| attack | 2 | Deals 1 dmg to player (minimum) |
| defense | 0 | Paper thin |
| speed | 2 | 2 actions/turn -- close and bite, or bite twice |
| weight | 1 | Easily knocked back |
| poise | 0 | -- |
| aggro_range | 6 | Alert critters, notice from further |
| behavior | chase | Simple rush-down |
| xp | 5 | Minimal reward |

**Role:** Nuisance solo, dangerous in packs of 4-5. Teaches swarm management and knockback tactics.

### Goblin -- Standard mook, punchy baseline

| Stat | Old | New | Notes |
|------|-----|-----|-------|
| health | 20 | 15 | Dies in 4 player hits (5 ATK - 1 DEF = 4 dmg) |
| attack | 3 | 5 | Deals 2 dmg to player. Actually hurts. |
| defense | 0 | 1 | Slightly tougher than rats |
| speed | 1 | 1 | One action per turn |
| weight | 1 | 1 | Knocked back on first hit |
| poise | 0 | 0 | -- |
| aggro_range | 10 | 8 | Slightly reduced |
| behavior | chase | chase | Unchanged |
| xp | -- | 15 | Worth fighting |

**Role:** The baseline "real enemy." One is manageable, two pressure poise, three demand tactical play.

### Skeleton -- Durable sentinel, goblin-class

| Stat | Value | Notes |
|------|-------|-------|
| health | 18 | Slightly tougher than goblin |
| attack | 4 | Deals 1 dmg to player (4 - 3 DEF) |
| defense | 2 | Player deals 3 per hit -- 6 hits to kill |
| speed | 1 | Same tempo as goblin |
| weight | 1 | Knocked back on first hit |
| poise | 0 | -- |
| aggro_range | 7 | Slightly shorter awareness |
| behavior | chase_patrol | Patrols an area, chases when alerted |
| xp | 15 | Same tier reward as goblin |

**Role:** Armored counterpart to goblins. Same threat level, different texture -- goblins punish engagement, skeletons punish not finishing them quickly. Chase_patrol makes them feel like sentinels.

## Tier 2 Enemies (Mid Areas)

### Zombie -- Slow, tanky bruiser

| Stat | Value | Notes |
|------|-------|-------|
| health | 40 | 8 player hits to kill. A wall. |
| attack | 8 | Deals 5 dmg to player. Breaks poise in 2 hits. |
| defense | 0 | Hits land clean, but lots of HP to chew through |
| speed | 1 | Slow, plodding |
| weight | 3 | Needs 3 hits/turn to knock back -- player can't with base 2 AP |
| poise | 0 | -- |
| aggro_range | 5 | Short awareness, shambles toward you late |
| behavior | chase | Relentless once engaged |
| xp | 30 | Meaningful reward |

**Role:** War of attrition. Slow and telegraphed, but hits hard and won't go down. Weight 3 negates knockback kiting without AP-boosting equipment. Single zombie costs ~15-20 HP. Two in a corridor is a resource decision.

### Ghost -- Fast glass cannon

| Stat | Value | Notes |
|------|-------|-------|
| health | 12 | Dies in 3 player hits (5 - 1 DEF = 4 dmg) |
| attack | 7 | Deals 4 dmg to player per action |
| defense | 1 | Slight resistance |
| speed | 2 | 2 actions/turn -- 8 dmg/round |
| weight | 1 | Easily knocked back |
| poise | 0 | -- |
| aggro_range | 10 | High awareness |
| behavior | chase | Aggressive rush-down |
| xp | 25 | Good reward for quick dangerous fight |

**Role:** Tier 2 mirror of rats. Where rats are nuisance, ghosts are lethal. Shreds poise in one round. Pairs with zombies to create target prioritization puzzles -- kill the ghost first (it's killing you fastest) while the zombie soaks your hits.

## Difficulty Curve Summary

| Enemy | Dmg/round | Rounds to break poise (10) | Player hits to kill | Actions/turn | Feel |
|-------|-----------|---------------------------|---------------------|-------------|------|
| Rat | 2 | 5 | 2 | 2 | Swarm fodder |
| Goblin | 2 | 5 | 4 | 1 | Punchy baseline |
| Skeleton | 1 | 10 | 6 | 1 | Durable sentinel |
| Ghost | 8 | 2 | 3 | 2 | Lethal but fragile |
| Zombie | 5 | 2 | 8 | 1 | Grinding wall |

**Tier 1** nibbles. Individual enemies are manageable; groups start to pressure. The skill test is efficiency.
**Tier 2** bites. Individual enemies demand attention; mixed groups demand prioritization and positioning. The skill test is tactical decision-making.

## Encounter Design Notes

These are guidelines for adventure designers placing enemies, not implementation scope:

- **Tier 1 introductions:** Solo rat, then 2-3 rats, then a goblin, then goblin + rats. Skeleton appears as a guarding variant.
- **Tier 2 introductions:** Solo zombie in a wide room (teaches the attrition fight), then a ghost in an open area (teaches speed threat), then zombie + ghost together (the real test).
- **Mixed tier:** Goblins + a zombie, or skeletons + a ghost. Tier mixing creates interesting tactical layers.

## Implementation Scope

### Code changes (direct)
1. **Modify PlayerState defaults** -- poise 20 -> 10, defense 2 -> 3
2. **Update tests** -- any tests asserting old player defaults or goblin stats

### Entity data (via adventure-designer skill)
3. **Update existing Goblin entity group** -- new stat values
4. **Update existing Rat entity group** (if it exists) or create one -- stat values as specified
5. **Create Skeleton entity group** -- new entity with chase_patrol behavior
6. **Create Zombie entity group** -- new entity with weight 3
7. **Create Ghost entity group** -- new entity with speed 2
