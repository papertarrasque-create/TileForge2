# Quest & Dialogue Template System -- Design Spec

## Overview

A documentation-only template system for stamping out quest definitions and dialogue files. Lives as a single markdown file (`docs/Quest Templates.md`) in the Obsidian vault. No code generator -- templates use `{{UPPER_SNAKE}}` placeholders that a human or AI fills in to produce valid TileForge v2 JSON.

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Format | Documentation template (markdown) | Data-driven, no new code, AI-friendly |
| Archetypes | Fetch, Kill/Clear, Talk-to, Investigation | Maps to existing condition/action system; escort excluded (no movement AI) |
| NPC count | Variable per archetype | Each pattern implies different NPC roles |
| Placeholder syntax | `{{UPPER_SNAKE}}` | Unambiguous in JSON, easy to find-and-replace |

## Document Structure

`docs/Quest Templates.md` contains:

1. **Usage Instructions** -- How to copy, fill in, and save
2. **Condition & Action Syntax** -- How shorthand maps to JSON fields
3. **Variable Reference** -- Table of all placeholders, purposes, and which archetypes use them
4. **Archetype sections** (one per type), each with:
   - Description of the pattern
   - NPC roles
   - Quest JSON template
   - Dialogue JSON template(s) for each NPC

## Condition & Action Syntax

Conditions and actions in template shorthand map to specific JSON fields. The condition type determines which field carries the value:

| Shorthand | JSON |
|-----------|------|
| `quest_complete:{{QUEST_ID}}` | `{ "type": "quest_complete", "value": "{{QUEST_ID}}" }` |
| `quest_active:{{QUEST_ID}}` | `{ "type": "quest_active", "value": "{{QUEST_ID}}" }` |
| `has_flag:some_flag` | `{ "type": "has_flag", "flag": "some_flag" }` |
| `not_flag:some_flag` | `{ "type": "not_flag", "flag": "some_flag" }` |
| `has_item:ItemName` | `{ "type": "has_item", "item": "ItemName" }` |
| `variable_gte:var:N` | `{ "type": "variable_gte", "variable": "var", "value": "N" }` |

Actions use `type` + `value` (primary) + optional `key` (secondary):

| Shorthand | JSON |
|-----------|------|
| `start_quest:{{QUEST_ID}}` | `{ "type": "start_quest", "value": "{{QUEST_ID}}" }` |
| `complete_objective:obj_name` | `{ "type": "complete_objective", "value": "obj_name" }` |
| `set_flag:flag_name` | `{ "type": "set_flag", "value": "flag_name" }` |
| `set_variable:key:value` | `{ "type": "set_variable", "key": "key", "value": "value" }` |
| `remove_item:ItemName` | `{ "type": "remove_item", "value": "ItemName" }` |
| `heal:N` | `{ "type": "heal", "value": "N" }` |
| `log:message` | `{ "type": "log", "value": "message" }` |

**Note on `complete_objective`:** The action value sets flag `objective_complete:{value}`. The corresponding quest objective in `quests.json` must use `"flag": "objective_complete:{value}"` to match. Example: action `complete_objective:return_amulet` sets flag `objective_complete:return_amulet`, and the quest objective checks `"type": "flag", "flag": "objective_complete:return_amulet"`.

**Note on routes vs nodes:** `DialogueRoute` only has `startNode` and `conditions`. It does NOT have actions. All actions go on the `DialogueNode` that the route's `startNode` points to.

## Common Variables

| Variable | Example | Description |
|----------|---------|-------------|
| `{{QUEST_ID}}` | `lost_amulet` | snake_case quest identifier |
| `{{QUEST_NAME}}` | `The Lost Amulet` | Display name |
| `{{QUEST_DESCRIPTION}}` | `Retrieve the stolen amulet...` | Quest log description |
| `{{GIVER_DIALOGUE_ID}}` | `sage_01` | Dialogue file ID for quest-giver |
| `{{GIVER_NAME}}` | `Village Sage` | Speaker name |
| `{{GIVER_INTRO_TEXT}}` | `Dark times, traveler...` | Initial greeting |
| `{{GIVER_ACCEPT_TEXT}}` | `Bless you!...` | Response when player accepts |
| `{{GIVER_DECLINE_TEXT}}` | `I understand...` | Response when player declines |
| `{{GIVER_PROGRESS_TEXT}}` | `Please hurry!...` | In-progress reminder |
| `{{GIVER_COMPLETE_TEXT}}` | `The village is safe...` | Post-completion greeting |
| `{{ACCEPT_CHOICE_TEXT}}` | `I'll help.` | Player accept choice label |
| `{{DECLINE_CHOICE_TEXT}}` | `Not interested.` | Player decline choice label |

## Quest Rewards Format

Quest rewards in `quests.json` use `List<DialogueAction>` format:

```json
"rewards": [
  { "type": "set_flag", "value": "village_safe" },
  { "type": "set_variable", "key": "reputation", "value": "10" }
]
```

**Note:** The tutorial project's `quests.json` currently uses a legacy `{ "set_flags": [], "set_variables": {} }` format. Templates should use the `List<DialogueAction>` format which matches the `QuestDefinition.Rewards` data model.

## Archetype: Fetch Quest

**NPCs:** Quest-giver + optional supporter

**Additional variables:** `{{ITEM_NAME}}`, `{{RETURN_NODE_TEXT}}`, `{{REWARD_TEXT}}`, `{{SUPPORTER_DIALOGUE_ID}}`, `{{SUPPORTER_NAME}}`, `{{SUPPORTER_ACTIVE_TEXT}}`, `{{SUPPORTER_DEFAULT_TEXT}}`, `{{SUPPORTER_COMPLETE_TEXT}}`, reward actions

**Quest-giver routes (4, most-specific first):**
1. `post_quest` -- condition: `quest_complete:{{QUEST_ID}}`
2. `return_item` -- conditions: `quest_active:{{QUEST_ID}}` + `has_item:{{ITEM_NAME}}`
3. `in_progress` -- condition: `quest_active:{{QUEST_ID}}`
4. `start` -- no conditions (fallback)

**Quest-giver nodes:**
- `post_quest` -- complete text, log action
- `return_item` -- player choice to hand over item; node actions: `remove_item`, `complete_objective`, `set_flag`
- `item_returned` -- reward text; node actions: reward (heal/set_variable/etc)
- `in_progress` -- progress reminder
- `start` -- intro text, accept/decline choices; accept choice actions: `start_quest`, `set_flag`
- `quest_accept` -- directions/guidance
- `decline` -- decline response

**Supporter routes (3):**
1. `post_quest` -- condition: `quest_complete:{{QUEST_ID}}`
2. `let_pass` -- condition: `quest_active:{{QUEST_ID}}`
3. `no_entry` -- condition: `not_flag:{{QUEST_ID}}_accepted` (suppressed once quest is known)

**Note:** The supporter's fallback uses a `not_flag` condition rather than being unconditional, following the guard_01.json pattern. This prevents the "no entry" text from showing when the player has talked to the giver but the quest is in an intermediate state.

**Objectives:**
- Talk to supporter (flag)
- Find item (flag)
- Return item to giver (objective flag: `objective_complete:return_{{ITEM_NAME}}`)

## Archetype: Kill/Clear Quest

**NPCs:** Quest-giver only

**Additional variables:** `{{ENEMY_NAME}}`, `{{KILL_COUNT}}`, `{{KILL_VARIABLE}}`, `{{REPORT_FLAG}}`

**Quest-giver routes (4, most-specific first):**
1. `post_quest` -- condition: `quest_complete:{{QUEST_ID}}`
2. `kill_count_met` -- conditions: `quest_active:{{QUEST_ID}}` + `variable_gte:{{KILL_VARIABLE}}:{{KILL_COUNT}}`
3. `in_progress` -- condition: `quest_active:{{QUEST_ID}}`
4. `start` -- no conditions (fallback)

**Quest-giver nodes:**
- `post_quest` -- complete text
- `kill_count_met` -- player choice to report; choice actions: `set_flag:{{REPORT_FLAG}}`, `complete_objective`
- `reported` -- reward text; node actions: reward
- `in_progress` -- progress reminder (e.g., "Keep at it, there are more out there.")
- `start` -- intro, accept/decline; accept choice actions: `start_quest`
- `quest_accept` -- directions
- `decline` -- decline response

**Objectives:**
- Defeat N enemies (variable_gte on `{{KILL_VARIABLE}}`)
- Report back (flag: `{{REPORT_FLAG}}`)

## Archetype: Talk-to / Delivery Quest

**NPCs:** Quest-giver + target

**Additional variables:** `{{TARGET_DIALOGUE_ID}}`, `{{TARGET_NAME}}`, `{{TARGET_ACTIVE_TEXT}}`, `{{TARGET_DEFAULT_TEXT}}`, `{{TARGET_COMPLETE_TEXT}}`, optional `{{DELIVERY_ITEM}}`

**Quest-giver routes (3):**
1. `post_quest` -- condition: `quest_complete:{{QUEST_ID}}`
2. `in_progress` -- condition: `quest_active:{{QUEST_ID}}`
3. `start` -- no conditions (fallback)

**Target routes (3):**
1. `post_quest` -- condition: `quest_complete:{{QUEST_ID}}`
2. `quest_active` -- condition: `quest_active:{{QUEST_ID}}`. The node this route points to has actions: `set_flag:talked_to_{{TARGET_DIALOGUE_ID}}`, optionally `remove_item:{{DELIVERY_ITEM}}`, `complete_objective`
3. `default` -- no conditions (fallback)

**Objectives:**
- Speak to target (flag: `talked_to_{{TARGET_DIALOGUE_ID}}`)
- If delivery: hand over item (flag)

## Archetype: Investigation Quest

**NPCs:** Quest-giver + 2-3 witnesses

**Additional variables:** `{{WITNESS_1_DIALOGUE_ID}}`, `{{WITNESS_1_NAME}}`, `{{WITNESS_1_TEXT}}`, `{{CLUE_1_FLAG}}` (repeat for 2, 3), `{{GIVER_ALL_CLUES_TEXT}}`

**Quest-giver routes (4, most-specific first):**
1. `post_quest` -- condition: `quest_complete:{{QUEST_ID}}`
2. `all_clues` -- conditions: `quest_active:{{QUEST_ID}}` + all `has_flag:{{CLUE_N_FLAG}}`
3. `in_progress` -- condition: `quest_active:{{QUEST_ID}}`
4. `start` -- no conditions (fallback)

**Witness routes (2 each):**
1. `already_talked` -- condition: `has_flag:{{CLUE_N_FLAG}}`
2. `default` -- no conditions. The node this route points to has actions: `set_flag:{{CLUE_N_FLAG}}`

**Objectives:**
- One flag per witness (talk to witness 1, 2, 3)
- Return to giver with all clues

## Route Ordering Rule

All templates follow the same principle: **most-specific condition first, unconditional fallback last.** The dialogue runtime evaluates routes top-to-bottom and takes the first match. This means:
- `quest_complete` routes before `quest_active` routes
- Multi-condition routes (e.g., active + has_item) before single-condition routes
- `in_progress` (quest_active, single condition) before the unconditional fallback
- The fallback route (no conditions or `not_flag` guard) is always last

**Every archetype must have an `in_progress` route** between the specific quest-active checks and the fallback. Without it, an active quest would fall through to the initial `start` route, allowing the player to re-accept.

## Output Files

Each template produces:
- One or more dialogue JSON files saved to `dialogues/{{DIALOGUE_ID}}.json`
- One quest entry added to `quests.json`

All JSON follows TileForge v2 dialogue format (camelCase properties, routes array, conditions/actions on nodes and choices).
