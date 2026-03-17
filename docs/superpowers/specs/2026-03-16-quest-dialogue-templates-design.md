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
2. **Variable Reference** -- Table of all placeholders, purposes, and which archetypes use them
3. **Archetype sections** (one per type), each with:
   - Description of the pattern
   - NPC roles
   - Quest JSON template
   - Dialogue JSON template(s) for each NPC

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
- `return_item` -- player choice to hand over item; actions: `remove_item`, `complete_objective`, `set_flag`
- `item_returned` -- reward text; actions: reward (heal/set_variable/etc)
- `in_progress` -- progress reminder
- `start` -- intro text, accept/decline choices; accept actions: `start_quest`, `set_flag`
- `quest_accept` -- directions/guidance
- `decline` -- decline response

**Supporter routes (3):**
1. `post_quest` -- condition: `quest_complete:{{QUEST_ID}}`
2. `let_pass` -- condition: `quest_active:{{QUEST_ID}}`
3. `no_entry` -- no conditions (fallback)

**Objectives:**
- Talk to supporter (flag)
- Find item (flag)
- Return item to giver (objective flag)

## Archetype: Kill/Clear Quest

**NPCs:** Quest-giver only

**Additional variables:** `{{ENEMY_NAME}}`, `{{KILL_COUNT}}`, `{{KILL_VARIABLE}}`, `{{REPORT_FLAG}}`

**Quest-giver routes (3):**
1. `post_quest` -- condition: `quest_complete:{{QUEST_ID}}`
2. `kill_count_met` -- conditions: `quest_active:{{QUEST_ID}}` + `variable_gte:{{KILL_VARIABLE}}:{{KILL_COUNT}}`
3. `start` -- no conditions (fallback)

**Quest-giver nodes:**
- `post_quest` -- complete text
- `kill_count_met` -- player choice to report; actions: `set_flag:{{REPORT_FLAG}}`, `complete_objective`
- `reported` -- reward text; actions: reward
- `start` -- intro, accept/decline; accept actions: `start_quest`
- `quest_accept` -- directions
- `decline` -- decline response

**Objectives:**
- Defeat N enemies (variable_gte on `{{KILL_VARIABLE}}`)
- Report back (flag)

## Archetype: Talk-to / Delivery Quest

**NPCs:** Quest-giver + target

**Additional variables:** `{{TARGET_DIALOGUE_ID}}`, `{{TARGET_NAME}}`, `{{TARGET_ACTIVE_TEXT}}`, `{{TARGET_DEFAULT_TEXT}}`, `{{TARGET_COMPLETE_TEXT}}`, optional `{{DELIVERY_ITEM}}`

**Quest-giver routes (3):**
1. `post_quest` -- condition: `quest_complete:{{QUEST_ID}}`
2. `in_progress` -- condition: `quest_active:{{QUEST_ID}}`
3. `start` -- no conditions (fallback)

**Target routes (3):**
1. `post_quest` -- condition: `quest_complete:{{QUEST_ID}}`
2. `quest_active` -- condition: `quest_active:{{QUEST_ID}}`; actions: `set_flag:talked_to_{{TARGET_DIALOGUE_ID}}`, optionally `remove_item:{{DELIVERY_ITEM}}`, `complete_objective`
3. `default` -- no conditions (fallback)

**Objectives:**
- Speak to target (flag)
- If delivery: hand over item (flag)

## Archetype: Investigation Quest

**NPCs:** Quest-giver + 2-3 witnesses

**Additional variables:** `{{WITNESS_1_DIALOGUE_ID}}`, `{{WITNESS_1_NAME}}`, `{{WITNESS_1_TEXT}}`, `{{CLUE_1_FLAG}}` (repeat for 2, 3), `{{GIVER_ALL_CLUES_TEXT}}`

**Quest-giver routes (3):**
1. `post_quest` -- condition: `quest_complete:{{QUEST_ID}}`
2. `all_clues` -- conditions: `quest_active:{{QUEST_ID}}` + all `has_flag:{{CLUE_N_FLAG}}`
3. `start` -- no conditions (fallback)

**Witness routes (2 each):**
1. `already_talked` -- condition: `has_flag:{{CLUE_N_FLAG}}`
2. `default` -- no conditions; actions: `set_flag:{{CLUE_N_FLAG}}`

**Objectives:**
- One flag per witness (talk to witness 1, 2, 3)
- Return to giver with all clues

## Route Ordering Rule

All templates follow the same principle: **most-specific condition first, unconditional fallback last.** The dialogue runtime evaluates routes top-to-bottom and takes the first match. This means:
- `quest_complete` routes before `quest_active` routes
- Multi-condition routes (e.g., active + has_item) before single-condition routes
- The fallback route (no conditions) is always last

## Output Files

Each template produces:
- One or more dialogue JSON files saved to `dialogues/{{DIALOGUE_ID}}.json`
- One quest entry added to `quests.json`

All JSON follows TileForge v2 dialogue format (camelCase properties, routes array, conditions/actions on nodes and choices).
