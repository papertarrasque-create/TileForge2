# The Lost Amulet Test Project — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a standalone test project that exercises every dialogue v2 and quest system feature end-to-end.

**Architecture:** All data files (JSON). One C# code change to set the default project. The .tileforge file defines the map, tile groups, entity groups, and entity placements. Dialogue files go in a `dialogues/` subfolder. Quest definitions go in `quests.json`.

**Tech Stack:** JSON (System.Text.Json camelCase), .tileforge project format v2

**Spec:** `docs/superpowers/specs/2026-03-16-lost-amulet-test-project.md`

---

## Chunk 1: Data Files

### Task 1: Create the dialogue files

All dialogue files go in `TileForge/TutorialProject/dialogues/`. Use camelCase JSON keys. Each dialogue has an `id`, optional `routes`, `nodes` array, and optional `oneShot` flag. Nodes have `id`, `speaker`, `text`, optional `choices`, `actions`, `conditions`, and `editorX`/`editorY` for the visual editor layout.

**Files:**
- Create: `TileForge/TutorialProject/dialogues/sage_01.json`
- Create: `TileForge/TutorialProject/dialogues/guard_01.json`
- Create: `TileForge/TutorialProject/dialogues/cellar_sign.json`
- Create: `TileForge/TutorialProject/dialogues/found_amulet.json`

- [ ] **Step 1: Create sage_01.json**

This is the most complex dialogue — 4 routes, 7 nodes, demonstrating quest_complete, quest_active + has_item, quest_active, and default entry points.

```json
{
  "id": "sage_01",
  "routes": [
    {
      "startNode": "post_quest",
      "conditions": [
        { "type": "quest_complete", "value": "lost_amulet" }
      ]
    },
    {
      "startNode": "return_amulet",
      "conditions": [
        { "type": "quest_active", "value": "lost_amulet" },
        { "type": "has_item", "item": "Amulet" }
      ]
    },
    {
      "startNode": "in_progress",
      "conditions": [
        { "type": "quest_active", "value": "lost_amulet" }
      ]
    },
    {
      "startNode": "start"
    }
  ],
  "nodes": [
    {
      "id": "post_quest",
      "speaker": "Village Sage",
      "text": "The village is safe again, thanks to you. You are always welcome here.",
      "actions": [
        { "type": "log", "value": "The Sage smiles warmly." }
      ],
      "editorX": 50,
      "editorY": -500
    },
    {
      "id": "return_amulet",
      "speaker": "Village Sage",
      "text": "You found it! I can feel its power from here.",
      "choices": [
        {
          "text": "Here, take it back.",
          "nextNodeId": "amulet_returned",
          "actions": [
            { "type": "remove_item", "value": "Amulet" },
            { "type": "complete_objective", "value": "return_amulet" },
            { "type": "set_flag", "value": "amulet_returned" }
          ]
        }
      ],
      "editorX": 50,
      "editorY": -350
    },
    {
      "id": "amulet_returned",
      "speaker": "Village Sage",
      "text": "Thank you! Let me mend your wounds as thanks.",
      "actions": [
        { "type": "heal", "value": "20" },
        { "type": "set_variable", "key": "reputation", "value": "10" },
        { "type": "log", "value": "The Sage restored your health." }
      ],
      "editorX": 400,
      "editorY": -350
    },
    {
      "id": "in_progress",
      "speaker": "Village Sage",
      "text": "Please hurry! The rats grow bolder by the hour. The cellar is through the gap to the east.",
      "editorX": 50,
      "editorY": -200
    },
    {
      "id": "start",
      "speaker": "Village Sage",
      "text": "Dark times, traveler. Rats stole my amulet of protection and dragged it into the old cellar to the east.",
      "choices": [
        {
          "text": "I'll get it back for you.",
          "nextNodeId": "quest_accept",
          "actions": [
            { "type": "start_quest", "value": "lost_amulet" },
            { "type": "set_flag", "value": "sage_asked_help" }
          ]
        },
        {
          "text": "Not my problem.",
          "nextNodeId": "decline"
        }
      ],
      "editorX": 50,
      "editorY": -50
    },
    {
      "id": "quest_accept",
      "speaker": "Village Sage",
      "text": "Bless you! Speak with the guard by the cellar entrance -- he will let you through. Clear those rats and bring back my amulet.",
      "editorX": 400,
      "editorY": -100
    },
    {
      "id": "decline",
      "speaker": "Village Sage",
      "text": "I understand. But without the amulet, the village is defenseless...",
      "editorX": 400,
      "editorY": 50
    }
  ]
}
```

- [ ] **Step 2: Create guard_01.json**

Three routes: quest_complete, quest_active, and default (with not_flag condition).

```json
{
  "id": "guard_01",
  "routes": [
    {
      "startNode": "post_quest",
      "conditions": [
        { "type": "quest_complete", "value": "lost_amulet" }
      ]
    },
    {
      "startNode": "let_pass",
      "conditions": [
        { "type": "quest_active", "value": "lost_amulet" }
      ]
    },
    {
      "startNode": "no_entry",
      "conditions": [
        { "type": "not_flag", "flag": "sage_asked_help" }
      ]
    }
  ],
  "nodes": [
    {
      "id": "post_quest",
      "speaker": "Guard",
      "text": "The Sage looks much happier now. Well done.",
      "editorX": 50,
      "editorY": -300
    },
    {
      "id": "let_pass",
      "speaker": "Guard",
      "text": "The Sage sent you? Good luck in there. Watch out for the rats -- they bite.",
      "actions": [
        { "type": "set_flag", "value": "guard_approved" }
      ],
      "editorX": 50,
      "editorY": -150
    },
    {
      "id": "no_entry",
      "speaker": "Guard",
      "text": "The cellar is off-limits. Too dangerous. Talk to the Sage if you have business here.",
      "editorX": 50,
      "editorY": 0
    }
  ]
}
```

- [ ] **Step 3: Create cellar_sign.json**

OneShot dialogue — single node, auto-sets `dialogue_shown:cellar_sign` after first display.

```json
{
  "id": "cellar_sign",
  "oneShot": true,
  "nodes": [
    {
      "id": "warning",
      "speaker": "Sign",
      "text": "DANGER: Rats beyond this point. Enter at your own risk.",
      "actions": [
        { "type": "log", "value": "You read the warning sign." }
      ],
      "editorX": 50,
      "editorY": 0
    }
  ]
}
```

- [ ] **Step 4: Create found_amulet.json**

Pickup dialogue — triggered by `on_pickup_dialogue` property on the Item entity. `CollectItem` already adds `Amulet` to inventory, so we only need `set_flag`.

```json
{
  "id": "found_amulet",
  "nodes": [
    {
      "id": "pickup",
      "speaker": "Amulet",
      "text": "You found the Sage's amulet! It pulses with a faint protective glow.",
      "actions": [
        { "type": "set_flag", "value": "found_amulet" }
      ],
      "editorX": 50,
      "editorY": 0
    }
  ]
}
```

- [ ] **Step 5: Commit dialogue files**

```bash
git add TileForge/TutorialProject/dialogues/sage_01.json TileForge/TutorialProject/dialogues/guard_01.json TileForge/TutorialProject/dialogues/cellar_sign.json TileForge/TutorialProject/dialogues/found_amulet.json
git commit -m "content: add Lost Amulet dialogue files (sage, guard, sign, amulet)"
```

### Task 2: Create the quest definition

**Files:**
- Overwrite: `TileForge/TutorialProject/quests.json`

- [ ] **Step 1: Write quests.json**

The quest has 4 objectives using both `flag` and `variable_gte` types. Rewards use old-style format (consistent with existing file, auto-migrated by `QuestLoader.MigrateOldRewards`).

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
          "flag": "guard_approved",
          "value": 0
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
          "flag": "found_amulet",
          "value": 0
        },
        {
          "description": "Return the Amulet to the Sage",
          "type": "flag",
          "flag": "objective_complete:return_amulet",
          "value": 0
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

- [ ] **Step 2: Commit quest file**

```bash
git add TileForge/TutorialProject/quests.json
git commit -m "content: add Lost Amulet quest definition"
```

### Task 3: Create the .tileforge project file

**Files:**
- Create: `TileForge/TutorialProject/QuestTestMap.tileforge`

This is the largest file — it contains groups (tiles + entities), map layers (15x15 = 225 cells per layer), and entity placements.

**Map layout reference (15x15, row-major):**
```
Row 0:  T T T T T T T T T T T T T T T
Row 1:  T . . . . . . T . . . . . . T
Row 2:  T . . . . . . T . . . . . . T
Row 3:  T . . . . . . T . . . . . . T    (Sage entity at 3,3)
Row 4:  T . . . . . . T . . . . . . T    (Rat1 entity at 11,4)
Row 5:  T . . . . . . . . . . . . . T    (Hero entity at 3,5; gap at col 7)
Row 6:  T . . . . . . T . . . . . . T
Row 7:  T . . . . . . . . . . . . . T    (Guard at 5,7; Sign at 6,7; gap at col 7)
Row 8:  T . . . . . . T . . . . . . T
Row 9:  T . . . . . . T . . . . . . T    (Rat2 entity at 10,9)
Row 10: T . . . . . . T . . . . . . T
Row 11: T . . . . . . T . . . . . . T    (Amulet entity at 10,11)
Row 12: T . . . . . . T . . . . . . T
Row 13: T . . . . . . T . . . . . . T
Row 14: T T T T T T T T T T T T T T T
```

The Ground layer cells array (225 entries, row-major: index = y*15 + x):
- Row 0: all "tree" (indices 0-14)
- Row 1: tree at x=0,x=7,x=14; grass elsewhere
- Rows 2-4: same pattern as row 1
- Row 5: tree at x=0,x=14; grass elsewhere (gap at x=7)
- Row 6: same as row 1
- Row 7: tree at x=0,x=14; grass elsewhere (gap at x=7)
- Rows 8-13: same as row 1
- Row 14: all "tree"

- [ ] **Step 1: Create QuestTestMap.tileforge**

Write the complete .tileforge JSON file with:
- Spritesheet reference to `The Roguelike 1-15-16.png` (16x16 tiles)
- 2 tile groups: `grass` (col:4,row:6) and `tree` (col:4,row:4, isSolid)
- 6 entity groups: `Hero`, `Sage`, `Guard`, `Sign`, `Rat`, `Amulet`
- 1 map named `main` (15x15) with Ground layer
- 7 entity placements with unique hex IDs

The full JSON content for this file is provided below. Entity IDs are arbitrary 8-char hex strings.

```json
{
  "version": 2,
  "spritesheet": {
    "path": "The Roguelike 1-15-16.png",
    "tileWidth": 16,
    "tileHeight": 16,
    "padding": 0
  },
  "groups": [
    {
      "name": "grass",
      "type": "Tile",
      "sprites": [{ "col": 4, "row": 6 }],
      "layer": "Ground"
    },
    {
      "name": "tree",
      "type": "Tile",
      "sprites": [{ "col": 4, "row": 4 }],
      "isSolid": true,
      "layer": "Ground"
    },
    {
      "name": "Hero",
      "type": "Entity",
      "sprites": [{ "col": 5, "row": 34 }],
      "isSolid": true,
      "isPlayer": true,
      "layer": "Entities",
      "entityType": "NPC",
      "defaultProperties": {
        "dialogue_id": "0",
        "health": "1",
        "attack": "0",
        "defense": "0",
        "behavior": "idle",
        "default_facing": "right",
        "hostile": "false",
        "aggro_range": "1"
      }
    },
    {
      "name": "Sage",
      "type": "Entity",
      "sprites": [{ "col": 17, "row": 34 }],
      "isSolid": true,
      "layer": "Entities",
      "entityType": "NPC",
      "defaultProperties": {
        "dialogue_id": "sage_01",
        "health": "1",
        "attack": "0",
        "defense": "0",
        "behavior": "idle",
        "default_facing": "right",
        "hostile": "false",
        "aggro_range": "1"
      }
    },
    {
      "name": "Guard",
      "type": "Entity",
      "sprites": [{ "col": 8, "row": 33 }],
      "isSolid": true,
      "layer": "Entities",
      "entityType": "NPC",
      "defaultProperties": {
        "dialogue_id": "guard_01",
        "health": "1",
        "attack": "0",
        "defense": "0",
        "behavior": "idle",
        "default_facing": "left",
        "hostile": "false",
        "aggro_range": "1"
      }
    },
    {
      "name": "Sign",
      "type": "Entity",
      "sprites": [{ "col": 41, "row": 21 }],
      "layer": "Entities",
      "entityType": "Interactable",
      "defaultProperties": {
        "dialogue_id": "cellar_sign"
      }
    },
    {
      "name": "Rat",
      "type": "Entity",
      "sprites": [{ "col": 5, "row": 31 }],
      "isSolid": true,
      "layer": "Entities",
      "entityType": "NPC",
      "defaultProperties": {
        "health": "5",
        "attack": "2",
        "defense": "0",
        "behavior": "chase",
        "default_facing": "left",
        "hostile": "true",
        "aggro_range": "5",
        "on_kill_increment": "rats_killed"
      }
    },
    {
      "name": "Amulet",
      "type": "Entity",
      "sprites": [{ "col": 12, "row": 35 }],
      "layer": "Entities",
      "entityType": "Item",
      "defaultProperties": {
        "on_pickup_dialogue": "found_amulet"
      }
    }
  ],
  "maps": [
    {
      "name": "main",
      "map": {
        "width": 15,
        "height": 15,
        "entityRenderOrder": 0,
        "layers": [
          {
            "name": "Ground",
            "visible": true,
            "cells": [
              SEE STEP DETAIL FOR FULL 225-ENTRY ARRAY
            ]
          }
        ],
        "entities": [
          { "id": "a0000001", "groupName": "Hero", "x": 3, "y": 5 },
          { "id": "a0000002", "groupName": "Sage", "x": 3, "y": 3 },
          { "id": "a0000003", "groupName": "Guard", "x": 5, "y": 7 },
          { "id": "a0000004", "groupName": "Sign", "x": 6, "y": 7 },
          { "id": "a0000005", "groupName": "Rat", "x": 11, "y": 4 },
          { "id": "a0000006", "groupName": "Rat", "x": 10, "y": 9 },
          { "id": "a0000007", "groupName": "Amulet", "x": 10, "y": 11 }
        ]
      }
    }
  ]
}
```

The Ground layer cells array (225 entries, row-major index = y*15 + x):

| Row | Pattern | Indices |
|-----|---------|---------|
| 0 | all `tree` | 0-14 |
| 1 | tree, grass*6, tree, grass*6, tree | 15-29 |
| 2 | same as row 1 | 30-44 |
| 3 | same as row 1 | 45-59 |
| 4 | same as row 1 | 60-74 |
| 5 | tree, grass*13, tree | 75-89 (gap at col 7) |
| 6 | same as row 1 | 90-104 |
| 7 | tree, grass*13, tree | 105-119 (gap at col 7) |
| 8 | same as row 1 | 120-134 |
| 9 | same as row 1 | 135-149 |
| 10 | same as row 1 | 150-164 |
| 11 | same as row 1 | 165-179 |
| 12 | same as row 1 | 180-194 |
| 13 | same as row 1 | 195-209 |
| 14 | all `tree` | 210-224 |

- [ ] **Step 2: Commit project file**

```bash
git add TileForge/TutorialProject/QuestTestMap.tileforge
git commit -m "content: add QuestTestMap.tileforge project file"
```

## Chunk 2: Code Change and Verification

### Task 4: Change default project

**Files:**
- Modify: `TileForge/TileForgeGame.cs:216`

- [ ] **Step 1: Update default project path**

Change line 216 from:
```csharp
AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "TutorialProject", "TestCavernDungeon.tileforge"));
```
to:
```csharp
AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "TutorialProject", "QuestTestMap.tileforge"));
```

- [ ] **Step 2: Build to verify no compilation errors**

Run: `dotnet build TileForge/TileForge.csproj`
Expected: Build succeeded, 0 errors

- [ ] **Step 3: Run existing tests to verify nothing is broken**

Run: `dotnet test TileForge.Tests/TileForge.Tests.csproj`
Expected: All 1681+ tests pass, 0 failures

- [ ] **Step 4: Commit code change**

```bash
git add TileForge/TileForgeGame.cs
git commit -m "feat: set QuestTestMap as default project on startup"
```

### Task 5: Validate JSON files parse correctly

- [ ] **Step 1: Verify all JSON files are valid**

Run: `python3 -c "import json; [json.load(open(f)) for f in ['TileForge/TutorialProject/dialogues/sage_01.json', 'TileForge/TutorialProject/dialogues/guard_01.json', 'TileForge/TutorialProject/dialogues/cellar_sign.json', 'TileForge/TutorialProject/dialogues/found_amulet.json', 'TileForge/TutorialProject/quests.json', 'TileForge/TutorialProject/QuestTestMap.tileforge']]; print('All JSON valid')"`
Expected: "All JSON valid"

- [ ] **Step 2: Verify dialogue IDs match entity references**

Check that:
- Sage entity `dialogue_id: sage_01` matches `dialogues/sage_01.json` id field
- Guard entity `dialogue_id: guard_01` matches `dialogues/guard_01.json` id field
- Sign entity `dialogue_id: cellar_sign` matches `dialogues/cellar_sign.json` id field
- Amulet entity `on_pickup_dialogue: found_amulet` matches `dialogues/found_amulet.json` id field

### Task 6: Manual playtest

- [ ] **Step 1: Run the application**

Run: `dotnet run --project TileForge/TileForge.csproj`
Expected: QuestTestMap loads with the 15x15 map visible

- [ ] **Step 2: Enter play mode and follow the playtest script**

Press F5 to enter play mode. Follow the playtest script from the spec:

1. Walk up to Sage (3,3) -- see default greeting -- choose "I'll get it back" -- quest starts
2. Walk to Sage again -- see "Please hurry!" (route 3)
3. Walk east to Guard (5,7) -- see "The Sage sent you?" -- guard_approved flag set, objective 1 complete
4. Walk to Sign (6,7) -- see warning -- walk to sign again -- nothing (oneShot)
5. Walk to Rat 1 (11,4) -- bump combat -- rat dies -- rats_killed=1
6. Walk to Rat 2 (10,9) -- bump combat -- rat dies -- rats_killed=2, objective 2 complete
7. Walk to Amulet (10,11) -- pickup dialogue -- amulet in inventory, objective 3 complete
8. Walk back to Sage -- see "You found it!" (route 2, has_item check) -- choose "Here, take it back" -- amulet removed, healed, objective 4 complete
9. Quest auto-completes -- rewards applied (village_safe flag, reputation=10)
10. Walk to Sage again -- see "The village is safe again" (route 1, quest_complete check)

- [ ] **Step 3: Document any issues found and fix**
