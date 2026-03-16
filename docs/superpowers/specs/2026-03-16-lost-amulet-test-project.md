# The Lost Amulet — Quest & Dialogue Test Project

**Date:** 2026-03-16
**Purpose:** Standalone test project exercising every feature of the dialogue v2 and quest systems for end-to-end validation.

## Overview

A single-map project (`QuestTestMap.tileforge`) using the `The Roguelike 1-15-16.png` tileset. The player completes a short quest — retrieving a stolen amulet from a rat-infested cellar — that touches every dialogue condition, action, route mechanic, and quest objective type. Completable in under a minute.

This project replaces `TestCavernDungeon.tileforge` as the default project loaded on startup (change in `TileForgeGame.cs` line 216).

## Narrative

The player starts in a small village clearing. The **Village Sage** explains that rats stole her protective amulet and dragged it into the old cellar. She asks the player to clear the rats and retrieve it. A **Guard** near the cellar entrance reacts differently depending on quest state. Inside the cellar area, the player fights **2 Rats** and finds the **Amulet** on the ground. A **Warning Sign** at the cellar entrance speaks once. Returning the amulet to the Sage completes the quest and earns a reward.

## Map

Single map named `main`, 15x15 tiles. Ground is grass. Trees form a border and divide the village (west) from the cellar area (east).

```
Legend: . = grass, T = tree (solid), S = Sage, P = Player, G = Guard
        ! = Sign, R = Rat, A = Amulet

Row 0:  T T T T T T T T T T T T T T T
Row 1:  T . . . . . . T . . . . . . T
Row 2:  T . . . . . . T . . . . . . T
Row 3:  T . . S . . . T . . . . . . T
Row 4:  T . . . . . . T . . . R . . T
Row 5:  T . . P . . . . . . . . . . T
Row 6:  T . . . . . . T . . . . . . T
Row 7:  T . . . . G ! . . . . . . . T
Row 8:  T . . . . . . T . . . . . . T
Row 9:  T . . . . . . T . . R . . . T
Row 10: T . . . . . . T . . . . . . T
Row 11: T . . . . . . T . . A . . . T
Row 12: T . . . . . . T . . . . . . T
Row 13: T . . . . . . T . . . . . . T
Row 14: T T T T T T T T T T T T T T T
```

Key layout decisions:
- Tree wall at column 7 separates village (west) from cellar (east), with a gap at rows 5 and 7 for passage
- Sage is in the village, near the player start
- Guard and Sign are at the gap, providing narrative transition
- Rats and Amulet are in the cellar (east side)
- Full tree border prevents walking off-map

### Tile Groups

| Group | Type | Sprite (col,row) | Properties |
|-------|------|-------------------|------------|
| grass | Tile | 4,6 | layer: Ground |
| tree | Tile | 4,4 | layer: Ground, isSolid: true |

### Entity Groups

| Group | Type | EntityType | Sprite (col,row) | Key Properties |
|-------|------|------------|-------------------|----------------|
| Hero | Entity | NPC (isPlayer) | 5,34 | health:20, attack:5, defense:2, behavior:idle |
| Sage | Entity | NPC | 17,34 | dialogue_id:sage_01, hostile:false, behavior:idle, health:1 |
| Guard | Entity | NPC | 8,33 | dialogue_id:guard_01, hostile:false, behavior:idle, health:1 |
| Sign | Entity | Interactable | 41,21 | dialogue_id:cellar_sign |
| Rat | Entity | NPC | 5,31 | hostile:true, behavior:chase, health:5, attack:2, aggro_range:5, on_kill_increment:rats_killed |
| Amulet | Entity | Item | 12,35 | dialogue_id:found_amulet |

### Entity Placements

| Entity | Position (x,y) |
|--------|----------------|
| Hero | 3,5 |
| Sage | 3,3 |
| Guard | 5,7 |
| Sign | 6,7 |
| Rat 1 | 11,4 |
| Rat 2 | 10,9 |
| Amulet | 10,11 |

## Dialogues

### sage_01.json — Village Sage

Four routes evaluated top-to-bottom. First matching route determines entry point.

**Route 1 — Quest complete:**
- Conditions: `quest_complete: lost_amulet`
- Start node: `post_quest`
- Node text: "The village is safe again, thanks to you. You are always welcome here."
- Actions: `log "The Sage smiles warmly."`
- Terminal (no choices, no nextNodeId)

**Route 2 — Has the amulet, quest active:**
- Conditions: `quest_active: lost_amulet` AND `has_item: amulet`
- Start node: `return_amulet`
- Node text: "You found it! I can feel its power from here."
- Choice: "Here, take it back."
  - Next: `amulet_returned`
  - Actions: `remove_item amulet`, `complete_objective return_amulet`, `set_flag amulet_returned`
- Node `amulet_returned`: "Thank you! Let me mend your wounds as thanks."
  - Actions: `heal`, `set_variable reputation 10`, `log "The Sage restored your health."`
  - Terminal

**Route 3 — Quest active, no amulet yet:**
- Conditions: `quest_active: lost_amulet`
- Start node: `in_progress`
- Node text: "Please hurry! The rats grow bolder by the hour. The cellar is through the gap to the east."
- Terminal

**Route 4 — Default (no conditions):**
- Start node: `start`
- Node `start`: "Dark times, traveler. Rats stole my amulet of protection and dragged it into the old cellar to the east."
- Choices:
  - "I'll get it back for you."
    - Next: `quest_accept`
    - Actions: `start_quest lost_amulet`, `set_flag sage_asked_help`
  - "Not my problem."
    - Next: `decline`
- Node `quest_accept`: "Bless you! Speak with the guard by the cellar entrance -- he will let you through. Clear those rats and bring back my amulet."
  - Terminal
- Node `decline`: "I understand. But without the amulet, the village is defenseless..."
  - Terminal

### guard_01.json — Guard

Two routes.

**Route 1 — Quest active:**
- Conditions: `quest_active: lost_amulet`
- Start node: `let_pass`
- Node text: "The Sage sent you? Good luck in there. Watch out for the rats -- they bite."
- Actions: `set_flag guard_approved`
- Terminal

**Route 2 — Default:**
- Start node: `no_entry`
- Node text: "The cellar is off-limits. Too dangerous. Talk to the Sage if you have business here."
- Terminal

### cellar_sign.json — Warning Sign

OneShot: true. Single node.

- Node text: "DANGER: Rats beyond this point. Enter at your own risk."
- Actions: `log "You read the warning sign."`
- Terminal

### found_amulet.json — Amulet Pickup

Single node, triggered on item pickup.

- Node text: "You found the Sage's amulet! It pulses with a faint protective glow."
- Actions: `give_item amulet`, `set_flag found_amulet`
- Terminal

## Quest Definition

File: `quests.json`

```json
{
  "quests": [
    {
      "id": "lost_amulet",
      "name": "The Lost Amulet",
      "description": "The Village Sage's protective amulet was stolen by rats. Clear the cellar and retrieve it.",
      "objectives": [
        {
          "description": "Speak with the Guard",
          "type": "flag",
          "flag": "guard_approved"
        },
        {
          "description": "Defeat the rats",
          "type": "variable_gte",
          "variable": "rats_killed",
          "value": 2
        },
        {
          "description": "Find the Amulet",
          "type": "flag",
          "flag": "found_amulet"
        },
        {
          "description": "Return the Amulet to the Sage",
          "type": "flag",
          "flag": "amulet_returned"
        }
      ],
      "rewards": {
        "set_flags": ["village_safe"],
        "set_variables": { "reputation": "10" }
      }
    }
  ]
}
```

### Objective Resolution Methods

| Objective | Resolution | Requires Return? |
|-----------|-----------|-----------------|
| Speak with Guard | Dialogue action `set_flag guard_approved` | No — resolves on conversation |
| Defeat the rats | Entity property `on_kill_increment: rats_killed` | No — resolves on kill |
| Find the Amulet | Item pickup dialogue action `set_flag found_amulet` | No — resolves on pickup |
| Return the Amulet | Dialogue action in Sage route 2 | Yes — must talk to Sage |

This demonstrates both "auto-resolving" objectives (guard, rats, amulet) and "return trip" objectives (returning to the Sage).

## Feature Coverage Matrix

| Feature | Where Exercised | Notes |
|---------|----------------|-------|
| Routes (conditional entry) | sage_01 (4 routes), guard_01 (2 routes) | First-match-wins evaluation |
| Condition: quest_active | sage routes 2-3, guard route 1 | Checks start flag set + complete flag not set |
| Condition: quest_complete | sage route 1 | Post-quest dialogue |
| Condition: has_item | sage route 2 | Checks player inventory |
| Action: start_quest | sage default route | Sets quest start flag |
| Action: complete_objective | sage route 2 | Sets objective flag |
| Action: set_flag | sage, guard, amulet | General state tracking |
| Action: set_variable | sage reward | Sets reputation |
| Action: give_item | amulet pickup | Adds to inventory |
| Action: remove_item | sage route 2 | Takes amulet from inventory |
| Action: heal | sage reward | Restores player health |
| Action: log | sign, sage | Game log messages |
| OneShot dialogue | cellar_sign | Auto-sets dialogue_shown flag |
| Item pickup dialogue | found_amulet | Triggered on item collection |
| on_kill_increment | rat entities | Combat-driven variable tracking |
| Multi-objective quest | lost_amulet (4 objectives) | Mixed flag + variable_gte types |
| Quest rewards | lost_amulet | set_flags + set_variables |
| Default project | TileForgeGame.cs change | Opens on startup |

## Code Change

One line in `TileForgeGame.cs`:

```csharp
// Line 216: change "TestCavernDungeon.tileforge" to "QuestTestMap.tileforge"
string defaultProject = Path.GetFullPath(Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "TutorialProject", "QuestTestMap.tileforge"));
```

## Files to Create

| File | Content |
|------|---------|
| `TileForge/TutorialProject/QuestTestMap.tileforge` | Project with map, groups, entity placements |
| `TileForge/TutorialProject/dialogues/sage_01.json` | Sage dialogue (overwrite existing) |
| `TileForge/TutorialProject/dialogues/guard_01.json` | Guard dialogue |
| `TileForge/TutorialProject/dialogues/cellar_sign.json` | Sign dialogue |
| `TileForge/TutorialProject/dialogues/found_amulet.json` | Amulet pickup dialogue |
| `TileForge/TutorialProject/quests.json` | Quest definition (overwrite existing) |

## Files to Modify

| File | Change |
|------|--------|
| `TileForge/TileForgeGame.cs` | Line 216: default project path |

## Playtest Script

1. F5 to enter play mode
2. Walk up to Sage (3,3) → see default greeting → choose "I'll get it back" → quest starts
3. Walk to Sage again → see "Please hurry!" (route 3)
4. Walk east to Guard (5,7) → see "The Sage sent you?" → guard_approved flag set, objective 1 complete
5. Walk to Sign (6,7) → see warning → walk to sign again → nothing (oneShot)
6. Walk to Rat 1 (11,4) → bump combat → rat dies → rats_killed=1
7. Walk to Rat 2 (10,9) → bump combat → rat dies → rats_killed=2, objective 2 complete
8. Walk to Amulet (10,11) → pickup dialogue → amulet in inventory, objective 3 complete
9. Walk back to Sage → see "You found it!" (route 2, has_item check) → choose "Here, take it back" → amulet removed, healed, objective 4 complete
10. Quest auto-completes → rewards applied (village_safe flag, reputation=10)
11. Walk to Sage again → see "The village is safe again" (route 1, quest_complete check)
