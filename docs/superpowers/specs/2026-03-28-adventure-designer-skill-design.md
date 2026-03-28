# Adventure Designer Skill -- Design Spec

**Date:** 2026-03-28
**Type:** Claude Code Skill (user-invoked via `/adventure-designer`)
**Scope:** Quest, dialogue, entity group, and entity property authoring

---

## Overview

A Claude Code skill that translates plain language into TileForge game artifacts. The agent acts as an expert adventure designer with deep knowledge of TileForge's quest, dialogue, entity, and property systems. It can handle everything from quick one-liner commands to full encounter blueprints.

## Skill Architecture

```
adventure-designer/
├── SKILL.md                      # Workflow, modes, git-check, generation rules
├── references/
│   ├── property-catalog.md       # PropertyKeys, EntityTypes, schema ranges
│   ├── dialogue-format.md        # Dialogue JSON format, conditions, actions
│   ├── quest-format.md           # Quest JSON format, objective types, rewards
│   └── project-format.md        # .tileforge structure, entity/group definitions
```

- **SKILL.md** (~200-300 lines): Workflow logic, mode detection, generation rules, safety checks
- **references/**: On-demand deep knowledge loaded when the agent needs format details

## Invocation Flow

### Step 1: Git Update Check

On every invocation, the agent runs `git log --oneline --name-only -20` and scans the changed file paths for any of these key files:

- `TileForge/Game/DialogueData.cs`
- `TileForge/Game/QuestData.cs`
- `TileForge/Game/PropertyKeys.cs`
- `TileForge/Game/PropertySchema.cs`
- `TileForge/Data/MapData.cs`
- `TileForge/Game/DialogueRuntime.cs`

If any key file appears in the recent commit list: read those files to pick up new condition types, action types, properties, or format changes.
If none appear: skip the deep read and proceed to the user's request.

### Step 2: Project Scan

Read the `.tileforge` project file to know:

- What maps exist and their layers
- What entity groups are defined (names, types, properties)
- What layers are available

Scan `quests.json` for existing quest IDs.
Scan `dialogues/` for existing dialogue file names.

This prevents ID collisions and lets the agent reference existing content.

### Step 3: Mode Detection & Execution

Based on user input, the agent selects one of four modes.

## Modes of Operation

### 1. Direct Command Mode

For quick, specific tasks. The agent generates immediately with sensible defaults.

**Examples:**
- "Add a choice to elder_01 node 'start' that starts quest cave_rescue"
- "Create a bark dialogue for the guard that says 'Halt! No entry.'"
- "Add an on_kill_increment hook to the Goblin group for goblin_kills"
- "Make the cellar_door dialogue oneShot"

**Behavior:** Generate artifact changes, report what changed.

### 2. Conversational Design Mode

For open-ended requests. The agent asks 2-3 focused questions to flesh out the design.

**Examples:**
- "I need a sidequest involving a missing cat"
- "Design a full encounter for a bandit camp"
- "Create an NPC merchant with a quest chain"

**Behavior:** Ask about narrative hook, entities involved, objectives/rewards. Propose IDs and structure. Wait for confirmation. Generate full artifact set.

### 3. Blueprint Mode

For structured markdown input. The agent parses each section and generates all artifacts in one pass, skipping the back-and-forth.

**Input format:**
```markdown
## Quest: <Quest Name>
<Description of the quest narrative and objectives>

## NPCs
- <NPC Name> -- <role, behavior, notes>

## Dialogue
- <NPC Name>: <dialogue description, routes, branches>

## Items
- <Item Name> -- <properties, hooks>

## Environment
- <Feature Name> -- <type, properties, triggers>
```

**Behavior:** Parse sections, resolve cross-references between them (e.g., NPC drops item that completes quest), propose IDs, generate all artifacts.

### 4. Modification Mode

For editing existing content.

**Examples:**
- "Make the blacksmith dialogue branch differently if the player has the cursed sword"
- "Add a third objective to quest haunted_cellar"
- "Change the Bandit group's health from 10 to 15"

**Behavior:** Read the existing artifact, propose changes, apply them.

## Entity Creation Convention

When the agent creates new entity groups:

1. **Creates the entity group** in the project's `groups` array with full property configuration (type, entityType, defaultProperties, isSolid, etc.)
2. **Assigns sprite `(0,0)`** -- the first tile in the spritesheet as a default placeholder image
3. **Places an entity instance** on a `_Staging` layer at position `(1,1)` on the target map
4. **Ensures `_Staging` layer exists** -- creates it if not present
5. **Reports what was created** so the user knows to reassign sprites and move entities to final positions

The user is responsible for:
- Assigning the correct sprite in the editor
- Moving entities from `_Staging` to their intended map positions

## Artifact Generation Rules

### IDs & Naming

| Artifact | Convention | Example |
|----------|-----------|---------|
| Quest IDs | `snake_case` | `haunted_cellar_investigation` |
| Dialogue IDs | `snake_case`, one file per dialogue | `dialogues/cellar_keeper.json` |
| Entity group names | `PascalCase` | `CellarGhost`, `CellarKeeper` |
| Entity instance IDs | `snake_case` with suffix | `cellar_ghost_01` |

The agent proposes all IDs up front. The user can override before the agent writes files.

### Dialogue Rules

- Always include `editorX`/`editorY` on nodes (BFS-style grid: 250px horizontal spacing, 200px vertical)
- Routes ordered most-specific first: `quest_complete` > `quest_active` > default
- Use `oneShot: true` for barks and one-time interactions
- ASCII-only in all rendered text (printable 32-126, no unicode)
- Use `camelCase` for JSON keys

### Quest Rules

- Objectives wired to entity hooks:
  - `on_kill_increment` property on entity -> `variable_gte` objective type
  - `on_collect_set_flag` property on entity -> `flag` objective type
- Rewards expressed as `DialogueAction[]` (new format, not legacy `set_flags`/`set_variables`)
- Use `snake_case` for JSON keys (quest file convention)

### Entity Property Rules

- All properties must exist in `PropertyKeys.cs` -- never invent keys
- Property values must conform to `PropertySchema` ranges and types
- If unsure about a property, read the source to verify before using it

### Cross-Reference Wiring

When generating a full encounter, the agent wires everything together:

```
Entity Group (on_kill_increment: "bandit_kills")
    -> Quest Objective (type: variable_gte, variable: "bandit_kills", value: 3)

Dialogue Action (type: start_quest, value: "clear_bandits")
    -> Quest StartFlag (quest_started:clear_bandits)

Dialogue Route Condition (type: quest_complete, value: "clear_bandits")
    -> Quest CompletionFlag (quest_complete:clear_bandits)

Entity Property (on_collect_set_flag: "has_amulet")
    -> Quest Objective (type: flag, flag: "has_amulet")
    -> Dialogue Condition (type: has_flag, flag: "has_amulet")
```

## Safety Rules

1. **Never overwrite** existing dialogue files without asking
2. **Append** to `quests.json` rather than replacing the file
3. **Warn on ID collision** if a proposed ID matches an existing quest, dialogue, or entity group
4. **Validate properties** against PropertySchema before writing
5. **ASCII-only** in any text that will be rendered by SpriteFont
6. **Report all changes** -- list every file created or modified and every ID chosen

## Key Source Files (for git update check)

| Purpose | File |
|---------|------|
| Property keys | `TileForge/Game/PropertyKeys.cs` |
| Property schema | `TileForge/Game/PropertySchema.cs` |
| Dialogue data model | `TileForge/Game/DialogueData.cs` |
| Quest data model | `TileForge/Game/QuestData.cs` |
| Condition types | `TileForge/Game/DialogueRuntime.cs` (ConditionEvaluator) |
| Action types | `TileForge/Game/DialogueRuntime.cs` (ActionExecutor) |
| Map data model | `TileForge/Data/MapData.cs` |
| Entity types | `TileForge/Game/EntityType.cs` (enum) |
| Project file format | `TileForge/Data/ProjectFile.cs` |

## Reference File Contents

### `references/property-catalog.md`
- Full PropertyKeys constant list with descriptions
- EntityType enum values
- PropertySchema ranges and types per entity type
- Default property examples per entity type

### `references/dialogue-format.md`
- DialogueData JSON schema with all fields
- Condition types table (has_flag, not_flag, has_item, variable_eq, variable_gte, variable_lt, quest_active, quest_complete)
- Action types table (set_flag, set_variable, increment, give_item, remove_item, start_quest, complete_objective, heal, damage, log, map_transition)
- Route evaluation rules (first match wins, fallback to first node)
- Example dialogue JSON

### `references/quest-format.md`
- QuestDefinition JSON schema
- Objective types (flag, variable_gte, variable_eq)
- Reward format (DialogueAction array)
- Quest state model (NotStarted/Active/Completed via flags)
- Example quest JSON

### `references/project-format.md`
- .tileforge v2 file structure
- TileGroup fields for Entity type groups
- Entity instance fields
- MapData and MapLayer structure
- SpriteRef format
- Layer conventions including `_Staging`

## Out of Scope

- **Visual map placement** -- agent defines groups and stages entities, user places them
- **Sprite assignment** -- agent uses (0,0) placeholder, user assigns correct sprite
- **In-editor AI panel** -- future enhancement, not part of this skill
- **Combat balancing** -- agent sets stats as requested, doesn't auto-balance encounters
- **Map creation** -- agent works with existing maps, doesn't create new ones
