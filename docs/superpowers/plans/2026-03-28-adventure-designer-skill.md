# Adventure Designer Skill Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a Claude Code skill that translates plain language into TileForge quest, dialogue, and entity artifacts.

**Architecture:** A user-invoked skill (`/adventure-designer`) with a lean SKILL.md defining workflow and four operation modes (Direct Command, Conversational Design, Blueprint, Modification), plus four reference files providing on-demand system knowledge. The skill lives as a local plugin in `~/.claude/plugins/local/tileforge-adventure-designer/`.

**Tech Stack:** Claude Code skill system (SKILL.md + references), JSON artifact generation, git CLI for update checks.

---

## File Structure

```
~/.claude/plugins/local/tileforge-adventure-designer/
├── .claude-plugin/
│   └── plugin.json                 # Plugin metadata
├── skills/
│   └── adventure-designer/
│       ├── SKILL.md                # Main skill: workflow, modes, rules
│       └── references/
│           ├── property-catalog.md # PropertyKeys, EntityTypes, schema
│           ├── dialogue-format.md  # Dialogue JSON, conditions, actions
│           ├── quest-format.md     # Quest JSON, objectives, rewards
│           └── project-format.md   # .tileforge structure, groups, entities
```

---

### Task 1: Create Plugin Scaffold

**Files:**
- Create: `~/.claude/plugins/local/tileforge-adventure-designer/.claude-plugin/plugin.json`

- [ ] **Step 1: Create plugin directory structure**

```bash
mkdir -p ~/.claude/plugins/local/tileforge-adventure-designer/.claude-plugin
mkdir -p ~/.claude/plugins/local/tileforge-adventure-designer/skills/adventure-designer/references
```

- [ ] **Step 2: Write plugin.json**

Create `~/.claude/plugins/local/tileforge-adventure-designer/.claude-plugin/plugin.json`:

```json
{
  "name": "tileforge-adventure-designer",
  "description": "An adventure design agent for TileForge that translates plain language into quests, dialogues, entities, and encounter artifacts",
  "author": {
    "name": "TileForge"
  }
}
```

- [ ] **Step 3: Commit**

```bash
cd ~/.claude/plugins/local/tileforge-adventure-designer
git init
git add .
git commit -m "feat: scaffold adventure-designer plugin"
```

---

### Task 2: Write Property Catalog Reference

**Files:**
- Create: `~/.claude/plugins/local/tileforge-adventure-designer/skills/adventure-designer/references/property-catalog.md`

This reference is loaded when the agent needs to set entity properties. It must contain every valid property key, its type, valid values, and which entity types it applies to.

- [ ] **Step 1: Write property-catalog.md**

Create `~/.claude/plugins/local/tileforge-adventure-designer/skills/adventure-designer/references/property-catalog.md`:

```markdown
# TileForge Property Catalog

## Entity Types

| EntityType | Description |
|-----------|-------------|
| NPC | Hostile or friendly characters with AI behavior |
| Item | Collectible objects (weapons, potions, quest items) |
| Trap | Hazardous entities that deal damage |
| Trigger | Teleport/map transition points |
| Interactable | Dialogue/loot objects the player interacts with |

## Property Keys by Category

### Combat (NPC)

| Key | Type | Range | Description |
|-----|------|-------|-------------|
| health | Int | 1-9999 | Current/starting health |
| max_health | Int | 1-9999 | Maximum health |
| attack | Int | 0-999 | Attack power |
| defense | Int | 0-999 | Defense rating |
| poise | Int | 0-9999 | Stagger resistance |
| damage | Int | 0-999 | Damage dealt (Trap) |
| xp | Int | 0-9999 | XP awarded on kill |
| weight | Int | 1-3 | Knockback weight class |

### AI & Behavior (NPC)

| Key | Type | Values/Range | Description |
|-----|------|-------------|-------------|
| behavior | Enum | idle, chase, patrol, chase_patrol | AI movement pattern |
| speed | Int | 1-3 | Movement speed |
| default_facing | Enum | right, left | Initial facing direction |
| hostile | Bool | true/false | Whether entity attacks on sight |
| hostile_flag | String | any flag name | Becomes hostile when flag is set |
| friendly_flag | String | any flag name | Becomes friendly when flag is set |
| aggro_range | Int | 1-50 | Detection range in tiles |
| alert_turns | Int | 1-99 | Turns entity stays alert |

### Patrol (NPC, runtime-mutated)

| Key | Type | Values | Description |
|-----|------|--------|-------------|
| patrol_axis | Enum | x, y | Patrol direction |
| patrol_range | Int | 1-50 | Patrol distance |
| patrol_origin | String | "x,y" | Starting point (set at runtime) |
| patrol_dir | String | 1/-1 | Current direction (set at runtime) |

### Items & Equipment (Item)

| Key | Type | Range | Description |
|-----|------|-------|-------------|
| heal | Int | 0-9999 | HP restored on use |
| equip_slot | Enum | "", weapon, armor, accessory | Equipment slot |
| equip_attack | Int | 0-999 | Attack bonus when equipped |
| equip_defense | Int | 0-999 | Defense bonus when equipped |
| equip_ap | Int | 0-99 | AP bonus when equipped |
| equip_poise | Int | 0-9999 | Poise bonus when equipped |
| equip_weight | Int | 0-5 | Weight when equipped |
| on_collect_set_flag | String | any flag name | Flag set when item collected |
| on_collect_increment | String | any variable name | Variable incremented on collect |

### Triggers (Trigger)

| Key | Type | Description |
|-----|------|-------------|
| target_map | MapRef | Destination map name |
| target_x | Int | Destination X coordinate |
| target_y | Int | Destination Y coordinate |

### Dialogue (all entity types)

| Key | Type | Description |
|-----|------|-------------|
| dialogue_id | DialogueRef | Dialogue file ID to load on interaction |
| dialogue | String | Legacy field (use "0" for none) |
| on_pickup_dialogue | DialogueRef | Dialogue shown when item picked up |

### Kill Hooks (NPC)

| Key | Type | Description |
|-----|------|-------------|
| on_kill_set_flag | String | Flag set when entity killed |
| on_kill_increment | String | Variable incremented when entity killed |

### Spawn Conditions (all entity types)

| Key | Type | Description |
|-----|------|-------------|
| spawn_requires_flag | String | Entity only spawns if this flag is set |
| spawn_forbids_flag | String | Entity does not spawn if this flag is set |

## Example Entity Configurations

### Hostile NPC (chase behavior)
```json
{
  "health": "10",
  "attack": "3",
  "defense": "1",
  "poise": "0",
  "behavior": "chase",
  "speed": "1",
  "default_facing": "right",
  "hostile": "true",
  "aggro_range": "5",
  "on_kill_increment": "goblin_kills"
}
```

### Quest Item
```json
{
  "dialogue_id": "found_amulet",
  "heal": "0",
  "on_collect_set_flag": "found_amulet"
}
```

### Friendly NPC (quest giver)
```json
{
  "dialogue_id": "sage_01",
  "health": "50",
  "behavior": "idle",
  "hostile": "false"
}
```

### Map Trigger (door)
```json
{
  "target_map": "Cellar",
  "target_x": "1",
  "target_y": "1",
  "dialogue_id": "cellar_door"
}
```
```

- [ ] **Step 2: Commit**

```bash
cd ~/.claude/plugins/local/tileforge-adventure-designer
git add skills/adventure-designer/references/property-catalog.md
git commit -m "feat: add property catalog reference"
```

---

### Task 3: Write Dialogue Format Reference

**Files:**
- Create: `~/.claude/plugins/local/tileforge-adventure-designer/skills/adventure-designer/references/dialogue-format.md`

- [ ] **Step 1: Write dialogue-format.md**

Create `~/.claude/plugins/local/tileforge-adventure-designer/skills/adventure-designer/references/dialogue-format.md`:

```markdown
# TileForge Dialogue Format

Dialogue files use **camelCase** JSON keys. One file per dialogue, stored in `{projectDir}/dialogues/{id}.json`.

## DialogueData Schema

```json
{
  "id": "dialogue_id",
  "type": "conversation",
  "oneShot": false,
  "routes": [],
  "nodes": []
}
```

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| id | string | required | Unique dialogue identifier (snake_case) |
| type | string | "conversation" | "conversation", "bark", "cutscene", "shop", "inspect" |
| oneShot | bool | false | If true, auto-sets flag "dialogue_shown:{id}" after first play |
| routes | Route[] | [] | Conditional entry points, evaluated top-to-bottom |
| nodes | Node[] | required | All dialogue nodes |

## Routes

Routes determine which node to start from based on game state. First matching route wins. If no route matches, falls back to the first node in the nodes array.

```json
{
  "startNode": "node_id",
  "conditions": [
    { "type": "quest_complete", "value": "lost_amulet" }
  ]
}
```

**Order routes most-specific first:**
1. quest_complete conditions
2. quest_active + specific flag conditions
3. quest_active conditions
4. has_flag / not_flag conditions
5. Default (no conditions or empty conditions)

## Nodes

```json
{
  "id": "node_id",
  "speaker": "Character Name",
  "text": "What the character says.",
  "choices": [],
  "nextNodeId": "next_node",
  "conditions": [],
  "actions": [],
  "editorX": 50,
  "editorY": -200
}
```

| Field | Type | Description |
|-------|------|-------------|
| id | string | Unique within this dialogue |
| speaker | string | Display name above text box |
| text | string | ASCII-only (printable 32-126). Use `...` not ellipsis, `--` not em-dash |
| choices | Choice[] or null | null = linear (auto-advance via nextNodeId) |
| nextNodeId | string or null | Next node for linear flow. null = end dialogue |
| conditions | Condition[] | Node skipped silently if conditions fail |
| actions | Action[] | Executed when node is displayed |
| editorX | float | Visual editor X position |
| editorY | float | Visual editor Y position |

**Editor layout convention:** Space nodes on a grid with ~250px horizontal and ~200px vertical spacing. Routes start at x=50, branch nodes at x=400, further branches at x=750.

## Choices

```json
{
  "text": "Player choice text",
  "nextNodeId": "target_node",
  "conditions": [],
  "actions": []
}
```

| Field | Type | Description |
|-------|------|-------------|
| text | string | ASCII-only player-visible choice text |
| nextNodeId | string | Node to go to when selected |
| conditions | Condition[] | Choice hidden if conditions fail |
| actions | Action[] | Executed when choice is selected |

## Condition Types

All conditions use AND logic (all must pass). Empty/null = always true.

| Type | Fields | Evaluates |
|------|--------|-----------|
| has_flag | flag: string | Flag is set in GameState |
| not_flag | flag: string | Flag is NOT set |
| has_item | item: string | Player has item in inventory |
| variable_eq | variable: string, value: string | Variable equals value |
| variable_gte | variable: string, value: string | Variable >= value |
| variable_lt | variable: string, value: string | Variable < value |
| quest_active | value: string (quest id) | Quest started AND not completed |
| quest_complete | value: string (quest id) | Quest completed |

## Action Types

Executed in sequence when a node is displayed or a choice is selected.

| Type | Fields | Effect |
|------|--------|--------|
| set_flag | value: flag name | Sets flag in GameState |
| set_variable | key: var name, value: string | Sets variable to value |
| increment | value: var name | Increments numeric variable by 1 |
| give_item | value: item name | Adds item to player inventory |
| remove_item | value: item name | Removes item from inventory |
| start_quest | value: quest id | Sets quest StartFlag, begins quest |
| complete_objective | value: objective id | Sets "objective_complete:{value}" flag |
| heal | value: int (as string) | Heals player by amount |
| damage | value: int (as string) | Damages player by amount |
| log | value: message, color: color name | Writes to GameLog. Colors: yellow, red, green, cyan, white |
| map_transition | value: map name, key: "x,y" | Transitions to map at coordinates |

## Complete Example

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
      "text": "The Sage sent you? Good luck in there.",
      "actions": [
        { "type": "set_flag", "value": "guard_approved" }
      ],
      "editorX": 50,
      "editorY": -150
    },
    {
      "id": "no_entry",
      "speaker": "Guard",
      "text": "The cellar is off-limits. Talk to the Sage if you have business here.",
      "editorX": 50,
      "editorY": 0
    }
  ]
}
```
```

- [ ] **Step 2: Commit**

```bash
cd ~/.claude/plugins/local/tileforge-adventure-designer
git add skills/adventure-designer/references/dialogue-format.md
git commit -m "feat: add dialogue format reference"
```

---

### Task 4: Write Quest Format Reference

**Files:**
- Create: `~/.claude/plugins/local/tileforge-adventure-designer/skills/adventure-designer/references/quest-format.md`

- [ ] **Step 1: Write quest-format.md**

Create `~/.claude/plugins/local/tileforge-adventure-designer/skills/adventure-designer/references/quest-format.md`:

```markdown
# TileForge Quest Format

Quest definitions use **snake_case** JSON keys. All quests live in a single file: `{projectDir}/quests.json`.

## Quest File Schema

```json
{
  "quests": [
    {
      "id": "quest_id",
      "name": "Display Name",
      "description": "Quest log description.",
      "objectives": [],
      "rewards": []
    }
  ]
}
```

## Quest State Model

Quests have three implicit states tracked via flags (no explicit state field):

| State | Condition |
|-------|-----------|
| NotStarted | `quest_started:{id}` flag NOT set |
| Active | `quest_started:{id}` flag IS set AND `quest_complete:{id}` flag NOT set |
| Completed | `quest_complete:{id}` flag IS set (auto-set when all objectives met) |

- **StartFlag** (`quest_started:{id}`) is set by the `start_quest` dialogue action
- **CompletionFlag** (`quest_complete:{id}`) is set automatically by QuestManager when all objectives evaluate to true
- Rewards are applied automatically on completion

## Objective Types

```json
{
  "description": "Human-readable objective text",
  "type": "flag",
  "flag": "flag_name",
  "variable": "var_name",
  "value": 0
}
```

| Type | Required Fields | Evaluates |
|------|----------------|-----------|
| flag | flag: string | `HasFlag(flag)` is true |
| variable_gte | variable: string, value: int | `GetVariable(variable) >= value` |
| variable_eq | variable: string, value: int | `GetVariable(variable) == value` |

### Wiring Objectives to Entity Hooks

| Objective Type | Entity Property | How It Works |
|----------------|----------------|--------------|
| flag | on_kill_set_flag | Entity killed -> flag set -> objective met |
| flag | on_collect_set_flag | Item collected -> flag set -> objective met |
| flag | (dialogue action: set_flag) | Dialogue sets flag -> objective met |
| flag | (dialogue action: complete_objective) | Sets "objective_complete:{value}" flag |
| variable_gte | on_kill_increment | Each kill increments variable -> threshold met |
| variable_gte | on_collect_increment | Each collect increments -> threshold met |

## Reward Format

Rewards are `DialogueAction[]` -- the same action types used in dialogues.

```json
"rewards": [
  { "type": "set_flag", "value": "village_safe" },
  { "type": "set_variable", "key": "reputation", "value": "10" },
  { "type": "give_item", "value": "Gold Ring" },
  { "type": "heal", "value": "20" }
]
```

Common reward actions: set_flag, set_variable, give_item, heal.

## Complete Example

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
      "rewards": [
        { "type": "set_flag", "value": "village_safe" },
        { "type": "set_variable", "key": "reputation", "value": "10" }
      ]
    }
  ]
}
```

## Quest Chaining

Quests can chain by using completion flags as conditions:

1. Quest A completes -> sets `quest_complete:quest_a` flag
2. NPC dialogue has a route with condition `{ "type": "quest_complete", "value": "quest_a" }`
3. That route's dialogue offers Quest B via `{ "type": "start_quest", "value": "quest_b" }`

Alternatively, Quest A's rewards can set a flag that a spawn condition uses:
- Quest A reward: `{ "type": "set_flag", "value": "unlock_boss" }`
- Boss entity: `spawn_requires_flag: "unlock_boss"`
```

- [ ] **Step 2: Commit**

```bash
cd ~/.claude/plugins/local/tileforge-adventure-designer
git add skills/adventure-designer/references/quest-format.md
git commit -m "feat: add quest format reference"
```

---

### Task 5: Write Project Format Reference

**Files:**
- Create: `~/.claude/plugins/local/tileforge-adventure-designer/skills/adventure-designer/references/project-format.md`

- [ ] **Step 1: Write project-format.md**

Create `~/.claude/plugins/local/tileforge-adventure-designer/skills/adventure-designer/references/project-format.md`:

```markdown
# TileForge Project File Format

Project files use the `.tileforge` extension and contain all map, group, and entity data. Version 2 format.

## Root Structure

```json
{
  "version": 2,
  "spritesheet": {
    "path": "sprites.png",
    "tileWidth": 16,
    "tileHeight": 16,
    "padding": 0
  },
  "groups": [],
  "maps": []
}
```

## Groups (TileGroup Definitions)

Groups define reusable tile and entity templates. Entity groups include default properties inherited by all instances.

### Tile Group
```json
{
  "name": "FloorBoards",
  "type": "Tile",
  "sprites": [{ "col": 2, "row": 3 }],
  "layer": "Ground"
}
```

### Entity Group
```json
{
  "name": "Goblin",
  "type": "Entity",
  "sprites": [{ "col": 10, "row": 28 }],
  "isSolid": true,
  "layer": "Ground",
  "entityType": "NPC",
  "defaultProperties": {
    "health": "10",
    "attack": "3",
    "defense": "1",
    "behavior": "chase",
    "speed": "1",
    "hostile": "true",
    "aggro_range": "5"
  }
}
```

| Field | Type | Description |
|-------|------|-------------|
| name | string | PascalCase unique group name |
| type | string | "Tile" or "Entity" |
| sprites | SpriteRef[] | Sprite positions in spritesheet |
| isSolid | bool | Blocks movement (entities/walls) |
| layer | string | Default layer name |
| entityType | string | NPC, Item, Trap, Trigger, Interactable |
| defaultProperties | object | Property bag inherited by instances |

### SpriteRef
```json
{ "col": 0, "row": 0 }
```

**Default placeholder sprite:** `{ "col": 0, "row": 0 }` (first tile in spritesheet, usually blank).

## Maps

Each map is a named entry in the maps array.

```json
{
  "name": "main",
  "map": {
    "width": 25,
    "height": 15,
    "entityRenderOrder": 0,
    "layers": [],
    "entities": []
  }
}
```

### Map Layers

```json
{
  "name": "Ground",
  "visible": true,
  "cells": ["FloorBoards", null, "Wall", ...]
}
```

- `cells` is a linear array indexed as `x + y * width`
- Values are group names or null (empty cell)
- Common layers: "Ground", "Objects"

### Entity Instances

```json
{
  "id": "debc6a6e",
  "groupName": "CellarDoor",
  "x": 11,
  "y": 9
}
```

| Field | Type | Description |
|-------|------|-------------|
| id | string | 8-character hex ID (unique within map) |
| groupName | string | References a group name |
| x | int | Map X position |
| y | int | Map Y position |

Instance-level `properties` override group `defaultProperties` when present.

## _Staging Layer Convention

When the adventure designer creates new entities:

1. Add the entity group to the `groups` array
2. Assign sprite `{ "col": 0, "row": 0 }` as placeholder
3. Find the target map in the `maps` array
4. Ensure a layer named `_Staging` exists -- if not, add it:
   ```json
   { "name": "_Staging", "visible": true, "cells": [...null...] }
   ```
   The cells array must be `width * height` nulls.
5. Add an entity instance at position (1, 1):
   ```json
   { "id": "<8-hex-chars>", "groupName": "NewEntity", "x": 1, "y": 1 }
   ```

The user moves entities from _Staging to their final positions and reassigns sprites in the editor.

## Generating Entity Instance IDs

Use 8 lowercase hex characters. In the skill, generate with:
```
Use Bash: python3 -c "import random; print(f'{random.randint(0, 0xFFFFFFFF):08x}')"
```

## File Locations Summary

| Artifact | Path | Format |
|----------|------|--------|
| Project file | `{projectDir}/{name}.tileforge` | JSON (this doc) |
| Quest definitions | `{projectDir}/quests.json` | snake_case JSON |
| Dialogue files | `{projectDir}/dialogues/{id}.json` | camelCase JSON |
| Spritesheet | `{projectDir}/{path from spritesheet.path}` | PNG |
```

- [ ] **Step 2: Commit**

```bash
cd ~/.claude/plugins/local/tileforge-adventure-designer
git add skills/adventure-designer/references/project-format.md
git commit -m "feat: add project format reference"
```

---

### Task 6: Write the Main SKILL.md

**Files:**
- Create: `~/.claude/plugins/local/tileforge-adventure-designer/skills/adventure-designer/SKILL.md`

This is the core skill file. It defines the workflow, mode detection, generation rules, and safety checks.

- [ ] **Step 1: Write SKILL.md**

Create `~/.claude/plugins/local/tileforge-adventure-designer/skills/adventure-designer/SKILL.md`:

```markdown
---
name: adventure-designer
description: "Use when the user asks to create, edit, or design quests, dialogues, NPCs, encounters, items, or any game content for TileForge. Also use when the user provides a structured adventure blueprint, asks to wire up quest objectives, or wants to modify existing dialogue trees or entity properties."
argument-hint: [description of what to create or modify]
allowed-tools: [Read, Write, Edit, Glob, Grep, Bash, Agent]
---

# TileForge Adventure Designer

You are an expert adventure designer for TileForge. You translate plain language descriptions into game artifacts: dialogue JSON files, quest definitions, and entity groups with properties. You understand how these systems interconnect and wire cross-references correctly.

## On Invocation

### Step 1: Git Update Check

Run this command to check for recent system changes:

```bash
git log --oneline --name-only -20
```

Scan the output for changes to these files:
- `TileForge/Game/DialogueData.cs`
- `TileForge/Game/QuestData.cs`
- `TileForge/Game/PropertyKeys.cs`
- `TileForge/Game/PropertySchema.cs`
- `TileForge/Data/MapData.cs`
- `TileForge/Game/DialogueRuntime.cs`

**If any appear:** Read those files to pick up new condition types, action types, properties, or format changes. Note any differences from your reference files.

**If none appear:** Skip to Step 2.

### Step 2: Project Scan

Find and read the project file:

```bash
# Find the .tileforge file
```

Use Glob to find `**/*.tileforge`, then read it to learn:
- What maps exist and their layer names
- What entity groups are defined
- What the spritesheet configuration is

Also scan existing content:
- Read `quests.json` for existing quest IDs
- Use Glob on `dialogues/*.json` and note existing dialogue IDs

This prevents ID collisions.

### Step 3: Detect Mode

Based on the user's input, select a mode:

| Input Pattern | Mode |
|--------------|------|
| Short, specific command ("add X to Y", "change Z") | **Direct Command** |
| Open-ended request ("I need a quest about...", "design an encounter...") | **Conversational Design** |
| Structured markdown with ## sections (Quest, NPCs, Dialogue, Items, Environment) | **Blueprint** |
| References existing artifact by ID ("modify guard_01", "update lost_amulet quest") | **Modification** |

## Modes

### Direct Command Mode

Generate immediately with sensible defaults. After generating:
1. List all files created or modified
2. List all IDs chosen
3. Note any entities placed on _Staging layer

### Conversational Design Mode

Ask 2-3 focused questions to understand:
1. **Narrative hook** -- who, what, why
2. **Entities involved** -- NPCs, enemies, items, triggers
3. **Objectives and rewards** -- what the player must do and what they get

Then propose all IDs (quest, dialogue, entity group names) and wait for confirmation before generating.

### Blueprint Mode

The user provides structured markdown. Parse each section:
- **## Quest:** -- extract quest name, description, objectives
- **## NPCs** -- extract NPC definitions with roles and behaviors
- **## Dialogue** -- extract dialogue descriptions, routes, branches
- **## Items** -- extract items with properties and hooks
- **## Environment** -- extract triggers, doors, interactables

Resolve cross-references between sections (e.g., NPC drops item that completes quest). Propose all IDs, then generate all artifacts in one pass.

### Modification Mode

1. Read the existing artifact (dialogue file, quest in quests.json, or group in .tileforge)
2. Propose specific changes
3. Apply changes using Edit tool (preserve everything not being changed)

## Generation Rules

### IDs & Naming

| Artifact | Convention | Example |
|----------|-----------|---------|
| Quest IDs | snake_case | lost_amulet |
| Dialogue IDs | snake_case | cellar_keeper |
| Dialogue filenames | dialogues/{id}.json | dialogues/cellar_keeper.json |
| Entity group names | PascalCase | CellarKeeper |
| Entity instance IDs | 8 lowercase hex chars | a1b2c3d4 |
| Flags | snake_case | guard_approved |
| Variables | snake_case | rats_killed |

Propose all IDs before writing. User can override.

### Creating Dialogue Files

Read `references/dialogue-format.md` for the complete schema.

Key rules:
- Always include `editorX`/`editorY` on every node
- Layout: routes start at x=50, branches at x=400, x=750. Vertical spacing ~150px between nodes at same depth.
- Order routes most-specific first (quest_complete > quest_active + flag > quest_active > has_flag > default)
- Use `oneShot: true` for barks and one-time events
- ASCII-only text (printable 32-126). Use `...` not unicode ellipsis, `--` not em-dash, straight quotes only
- Use camelCase for JSON keys

### Creating Quest Definitions

Read `references/quest-format.md` for the complete schema.

Key rules:
- Append new quests to the existing `quests` array in quests.json (never replace the file)
- Wire objectives to entity hooks (on_kill_increment -> variable_gte, on_collect_set_flag -> flag)
- Use new reward format (DialogueAction array), not legacy set_flags/set_variables
- Use snake_case for JSON keys

### Creating Entity Groups

Read `references/property-catalog.md` for all valid properties and `references/project-format.md` for the group structure.

Key rules:
- Add group to the `groups` array in the .tileforge file
- Assign sprite `{ "col": 0, "row": 0 }` as placeholder
- Set all relevant defaultProperties for the entityType
- Only use property keys that exist in PropertyKeys.cs
- Place an entity instance at (1, 1) on a `_Staging` layer on the target map

### Cross-Reference Wiring

When creating related artifacts, wire them together:

- Entity `on_kill_increment: "bandit_kills"` -> Quest objective `variable_gte`, `variable: "bandit_kills"`
- Dialogue action `start_quest` -> Quest `id` must match
- Dialogue route condition `quest_active`/`quest_complete` -> Quest `id` must match
- Entity `on_collect_set_flag: "has_item"` -> Quest objective `flag: "has_item"` AND/OR dialogue condition `has_flag: "has_item"`
- Dialogue action `complete_objective: "obj_id"` -> Quest objective `flag: "objective_complete:obj_id"`

## Safety Rules

1. **Never overwrite** existing dialogue files without asking the user first
2. **Append** to quests.json -- read existing content, add to the quests array, write back
3. **Warn on ID collision** if a proposed ID matches an existing quest, dialogue, or entity group
4. **Validate properties** -- only use keys from PropertyKeys.cs with valid values per PropertySchema
5. **ASCII-only** in any text rendered by SpriteFont (dialogue text, choice text, speaker names)
6. **Report all changes** after generation:
   - Files created (with paths)
   - Files modified (with what changed)
   - IDs chosen
   - Entities placed on _Staging (remind user to move them and assign sprites)

## Output Format

After generating artifacts, always report:

```
## Created Artifacts

### Quests
- `quest_id` -- "Quest Name" (added to quests.json)

### Dialogues
- `dialogues/npc_name.json` -- NPC dialogue with N nodes, M routes

### Entity Groups
- `GroupName` (EntityType) -- placed on _Staging layer of MapName at (1,1)
  - Reminder: assign sprite and move to final position

### Flags & Variables Introduced
- `flag_name` -- set by [source], checked by [consumer]
- `variable_name` -- incremented by [source], threshold N in [quest objective]
```
```

- [ ] **Step 2: Verify skill structure is complete**

```bash
find ~/.claude/plugins/local/tileforge-adventure-designer -type f | sort
```

Expected output:
```
.claude-plugin/plugin.json
skills/adventure-designer/SKILL.md
skills/adventure-designer/references/dialogue-format.md
skills/adventure-designer/references/project-format.md
skills/adventure-designer/references/property-catalog.md
skills/adventure-designer/references/quest-format.md
```

- [ ] **Step 3: Commit**

```bash
cd ~/.claude/plugins/local/tileforge-adventure-designer
git add skills/adventure-designer/SKILL.md
git commit -m "feat: add main adventure-designer skill"
```

---

### Task 7: Test the Skill

- [ ] **Step 1: Verify plugin is detected**

Restart Claude Code (or start a new session) and check that `/adventure-designer` appears in the skill list. Run `/help` or check for the skill in the available skills list.

- [ ] **Step 2: Test Direct Command mode**

Invoke the skill with a simple command:
```
/adventure-designer Create a bark dialogue for a sign that says "Beware of traps ahead."
```

Verify:
- Git update check runs
- Project file is scanned
- A dialogue file is created at `dialogues/sign_bark.json` (or similar)
- The dialogue has `oneShot: true`, correct node structure, `editorX`/`editorY`
- ASCII-only text
- Output report lists the created file

- [ ] **Step 3: Test Blueprint mode**

Invoke with a structured blueprint:
```
/adventure-designer
## Quest: Bandit Camp Raid
The town mayor asks you to clear a bandit camp east of town.

## NPCs
- Mayor -- quest giver, friendly, in town
- Bandit Leader -- hostile, tough, guards the camp

## Dialogue
- Mayor: offers quest, checks progress, thanks on completion

## Items
- Mayor's Seal -- proof of authority, on_collect_set_flag

## Environment
- Camp Gate -- trigger, leads to camp map
```

Verify:
- All artifacts are generated (quest, dialogues, entity groups)
- Cross-references are wired correctly
- Entity groups placed on _Staging
- IDs proposed and reported

- [ ] **Step 4: Test Modification mode**

Invoke to modify an existing artifact:
```
/adventure-designer Add a new route to sage_01 that triggers when the player has the flag "secret_discovered" -- the sage should reveal hidden knowledge
```

Verify:
- Existing sage_01.json is read
- New route and nodes are added without disrupting existing content
- Route is inserted at the correct priority position

- [ ] **Step 5: Commit test results**

If any adjustments were needed to the skill files, commit them:
```bash
cd ~/.claude/plugins/local/tileforge-adventure-designer
git add -A
git commit -m "fix: adjust skill based on testing"
```
