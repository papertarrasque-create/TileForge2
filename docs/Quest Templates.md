---
updated: 2026-03-16
status: current
---

# Quest & Dialogue Templates

Copy-paste templates for generating TileForge quest definitions and dialogue files. Replace all `{{PLACEHOLDER}}` values with your content, save the dialogue JSON to `dialogues/`, and add the quest entry to `quests.json`.

## Usage

1. Pick an archetype below that matches your quest
2. Copy the quest JSON and dialogue JSON template(s)
3. Replace every `{{PLACEHOLDER}}` with your values
4. Save each dialogue as `dialogues/{{DIALOGUE_ID}}.json`
5. Add the quest object to your `quests.json` `"quests"` array
6. Assign `dialogue_id` properties to the relevant entities in the GroupEditor

## Condition & Action Syntax

Conditions and actions use specific JSON fields depending on the type. This reference shows how each maps.

### Conditions

```
quest_complete   -> { "type": "quest_complete", "value": "quest_id" }
quest_active     -> { "type": "quest_active", "value": "quest_id" }
has_flag         -> { "type": "has_flag", "flag": "flag_name" }
not_flag         -> { "type": "not_flag", "flag": "flag_name" }
has_item         -> { "type": "has_item", "item": "ItemName" }
variable_gte     -> { "type": "variable_gte", "variable": "var_name", "value": "N" }
variable_eq      -> { "type": "variable_eq", "variable": "var_name", "value": "N" }
variable_lt      -> { "type": "variable_lt", "variable": "var_name", "value": "N" }
```

### Actions

```
start_quest        -> { "type": "start_quest", "value": "quest_id" }
complete_objective -> { "type": "complete_objective", "value": "objective_name" }
set_flag           -> { "type": "set_flag", "value": "flag_name" }
set_variable       -> { "type": "set_variable", "key": "var_name", "value": "N" }
increment          -> { "type": "increment", "key": "var_name", "value": "N" }
give_item          -> { "type": "give_item", "value": "ItemName" }
remove_item        -> { "type": "remove_item", "value": "ItemName" }
heal               -> { "type": "heal", "value": "N" }
damage             -> { "type": "damage", "value": "N" }
log                -> { "type": "log", "value": "Message text" }
```

### complete_objective Coupling

`complete_objective` sets the flag `objective_complete:{value}`. The quest objective must reference this exact flag:

- Action: `{ "type": "complete_objective", "value": "return_amulet" }`
- Sets flag: `objective_complete:return_amulet`
- Quest objective: `{ "type": "flag", "flag": "objective_complete:return_amulet", ... }`

### Route Rules

- Routes only have `startNode` and `conditions`. **Actions go on nodes, not routes.**
- Routes evaluate top-to-bottom; first match wins.
- Order: most-specific first, unconditional fallback last.
- Every archetype needs an `in_progress` route (quest_active, no extra conditions) between specific checks and the fallback. Without it, active quests fall through to the start route.

---

## Common Variables

| Variable | Example | Description |
|----------|---------|-------------|
| `{{QUEST_ID}}` | `lost_amulet` | snake_case quest identifier |
| `{{QUEST_NAME}}` | `The Lost Amulet` | Display name in quest log |
| `{{QUEST_DESCRIPTION}}` | `Retrieve the stolen amulet...` | Quest log description |
| `{{GIVER_DIALOGUE_ID}}` | `sage_01` | Dialogue file ID for quest-giver |
| `{{GIVER_NAME}}` | `Village Sage` | Speaker name in dialogue |
| `{{GIVER_INTRO_TEXT}}` | `Dark times, traveler...` | Initial greeting text |
| `{{GIVER_ACCEPT_TEXT}}` | `Bless you!...` | Response after player accepts |
| `{{GIVER_DECLINE_TEXT}}` | `I understand...` | Response after player declines |
| `{{GIVER_PROGRESS_TEXT}}` | `Please hurry!...` | In-progress reminder text |
| `{{GIVER_COMPLETE_TEXT}}` | `The village is safe...` | Post-quest greeting text |
| `{{ACCEPT_CHOICE_TEXT}}` | `I'll help.` | Player accept choice label |
| `{{DECLINE_CHOICE_TEXT}}` | `Not interested.` | Player decline choice label |

---

## Fetch Quest

> NPC asks the player to find/retrieve an item and bring it back.

**NPCs:** Quest-giver + optional supporter
**Additional variables:** `{{ITEM_NAME}}`, `{{RETURN_NODE_TEXT}}`, `{{REWARD_TEXT}}`, `{{SUPPORTER_DIALOGUE_ID}}`, `{{SUPPORTER_NAME}}`, `{{SUPPORTER_ACTIVE_TEXT}}`, `{{SUPPORTER_DEFAULT_TEXT}}`, `{{SUPPORTER_COMPLETE_TEXT}}`

### Quest Definition

```json
{
  "id": "{{QUEST_ID}}",
  "name": "{{QUEST_NAME}}",
  "description": "{{QUEST_DESCRIPTION}}",
  "objectives": [
    {
      "description": "Speak with {{SUPPORTER_NAME}}",
      "type": "flag",
      "flag": "{{SUPPORTER_DIALOGUE_ID}}_approved",
      "value": 0
    },
    {
      "description": "Find the {{ITEM_NAME}}",
      "type": "flag",
      "flag": "found_{{QUEST_ID}}_item",
      "value": 0
    },
    {
      "description": "Return the {{ITEM_NAME}}",
      "type": "flag",
      "flag": "objective_complete:return_{{QUEST_ID}}_item",
      "value": 0
    }
  ],
  "rewards": [
    { "type": "set_flag", "value": "{{QUEST_ID}}_done" }
  ]
}
```

### Quest-Giver Dialogue (`dialogues/{{GIVER_DIALOGUE_ID}}.json`)

```json
{
  "id": "{{GIVER_DIALOGUE_ID}}",
  "routes": [
    {
      "startNode": "post_quest",
      "conditions": [
        { "type": "quest_complete", "value": "{{QUEST_ID}}" }
      ]
    },
    {
      "startNode": "return_item",
      "conditions": [
        { "type": "quest_active", "value": "{{QUEST_ID}}" },
        { "type": "has_item", "item": "{{ITEM_NAME}}" }
      ]
    },
    {
      "startNode": "in_progress",
      "conditions": [
        { "type": "quest_active", "value": "{{QUEST_ID}}" }
      ]
    },
    {
      "startNode": "start"
    }
  ],
  "nodes": [
    {
      "id": "post_quest",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_COMPLETE_TEXT}}",
      "actions": [
        { "type": "log", "value": "{{GIVER_NAME}} looks pleased." }
      ]
    },
    {
      "id": "return_item",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{RETURN_NODE_TEXT}}",
      "choices": [
        {
          "text": "Here, take it.",
          "nextNodeId": "item_returned",
          "actions": [
            { "type": "remove_item", "value": "{{ITEM_NAME}}" },
            { "type": "complete_objective", "value": "return_{{QUEST_ID}}_item" },
            { "type": "set_flag", "value": "{{QUEST_ID}}_item_returned" }
          ]
        }
      ]
    },
    {
      "id": "item_returned",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{REWARD_TEXT}}",
      "actions": [
        { "type": "heal", "value": "20" },
        { "type": "log", "value": "{{GIVER_NAME}} rewards you." }
      ]
    },
    {
      "id": "in_progress",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_PROGRESS_TEXT}}"
    },
    {
      "id": "start",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_INTRO_TEXT}}",
      "choices": [
        {
          "text": "{{ACCEPT_CHOICE_TEXT}}",
          "nextNodeId": "quest_accept",
          "actions": [
            { "type": "start_quest", "value": "{{QUEST_ID}}" },
            { "type": "set_flag", "value": "{{QUEST_ID}}_accepted" }
          ]
        },
        {
          "text": "{{DECLINE_CHOICE_TEXT}}",
          "nextNodeId": "decline"
        }
      ]
    },
    {
      "id": "quest_accept",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_ACCEPT_TEXT}}"
    },
    {
      "id": "decline",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_DECLINE_TEXT}}"
    }
  ]
}
```

### Supporter Dialogue (`dialogues/{{SUPPORTER_DIALOGUE_ID}}.json`)

Optional. Omit if the quest has no supporting NPC.

```json
{
  "id": "{{SUPPORTER_DIALOGUE_ID}}",
  "routes": [
    {
      "startNode": "post_quest",
      "conditions": [
        { "type": "quest_complete", "value": "{{QUEST_ID}}" }
      ]
    },
    {
      "startNode": "let_pass",
      "conditions": [
        { "type": "quest_active", "value": "{{QUEST_ID}}" }
      ]
    },
    {
      "startNode": "no_entry",
      "conditions": [
        { "type": "not_flag", "flag": "{{QUEST_ID}}_accepted" }
      ]
    }
  ],
  "nodes": [
    {
      "id": "post_quest",
      "speaker": "{{SUPPORTER_NAME}}",
      "text": "{{SUPPORTER_COMPLETE_TEXT}}"
    },
    {
      "id": "let_pass",
      "speaker": "{{SUPPORTER_NAME}}",
      "text": "{{SUPPORTER_ACTIVE_TEXT}}",
      "actions": [
        { "type": "set_flag", "value": "{{SUPPORTER_DIALOGUE_ID}}_approved" }
      ]
    },
    {
      "id": "no_entry",
      "speaker": "{{SUPPORTER_NAME}}",
      "text": "{{SUPPORTER_DEFAULT_TEXT}}"
    }
  ]
}
```

---

## Kill/Clear Quest

> NPC asks the player to defeat a number of enemies or clear an area.

**NPCs:** Quest-giver only
**Additional variables:** `{{ENEMY_NAME}}`, `{{KILL_COUNT}}`, `{{KILL_VARIABLE}}`, `{{REPORT_FLAG}}`

**Note:** Enemies must have `on_death_variable` set to `{{KILL_VARIABLE}}` in the GroupEditor so kills are tracked.

### Quest Definition

```json
{
  "id": "{{QUEST_ID}}",
  "name": "{{QUEST_NAME}}",
  "description": "{{QUEST_DESCRIPTION}}",
  "objectives": [
    {
      "description": "Defeat {{KILL_COUNT}} {{ENEMY_NAME}}",
      "type": "variable_gte",
      "variable": "{{KILL_VARIABLE}}",
      "value": {{KILL_COUNT}}
    },
    {
      "description": "Report back",
      "type": "flag",
      "flag": "{{REPORT_FLAG}}",
      "value": 0
    }
  ],
  "rewards": [
    { "type": "set_flag", "value": "{{QUEST_ID}}_done" }
  ]
}
```

### Quest-Giver Dialogue (`dialogues/{{GIVER_DIALOGUE_ID}}.json`)

```json
{
  "id": "{{GIVER_DIALOGUE_ID}}",
  "routes": [
    {
      "startNode": "post_quest",
      "conditions": [
        { "type": "quest_complete", "value": "{{QUEST_ID}}" }
      ]
    },
    {
      "startNode": "kill_count_met",
      "conditions": [
        { "type": "quest_active", "value": "{{QUEST_ID}}" },
        { "type": "variable_gte", "variable": "{{KILL_VARIABLE}}", "value": "{{KILL_COUNT}}" }
      ]
    },
    {
      "startNode": "in_progress",
      "conditions": [
        { "type": "quest_active", "value": "{{QUEST_ID}}" }
      ]
    },
    {
      "startNode": "start"
    }
  ],
  "nodes": [
    {
      "id": "post_quest",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_COMPLETE_TEXT}}",
      "actions": [
        { "type": "log", "value": "{{GIVER_NAME}} looks relieved." }
      ]
    },
    {
      "id": "kill_count_met",
      "speaker": "{{GIVER_NAME}}",
      "text": "You've done it! The {{ENEMY_NAME}} are dealt with.",
      "choices": [
        {
          "text": "The job is done.",
          "nextNodeId": "reported",
          "actions": [
            { "type": "set_flag", "value": "{{REPORT_FLAG}}" },
            { "type": "complete_objective", "value": "{{QUEST_ID}}_reported" }
          ]
        }
      ]
    },
    {
      "id": "reported",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{REWARD_TEXT}}",
      "actions": [
        { "type": "heal", "value": "20" },
        { "type": "log", "value": "{{GIVER_NAME}} rewards you." }
      ]
    },
    {
      "id": "in_progress",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_PROGRESS_TEXT}}"
    },
    {
      "id": "start",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_INTRO_TEXT}}",
      "choices": [
        {
          "text": "{{ACCEPT_CHOICE_TEXT}}",
          "nextNodeId": "quest_accept",
          "actions": [
            { "type": "start_quest", "value": "{{QUEST_ID}}" }
          ]
        },
        {
          "text": "{{DECLINE_CHOICE_TEXT}}",
          "nextNodeId": "decline"
        }
      ]
    },
    {
      "id": "quest_accept",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_ACCEPT_TEXT}}"
    },
    {
      "id": "decline",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_DECLINE_TEXT}}"
    }
  ]
}
```

---

## Talk-to / Delivery Quest

> NPC sends the player to speak with (or deliver an item to) another NPC.

**NPCs:** Quest-giver + target
**Additional variables:** `{{TARGET_DIALOGUE_ID}}`, `{{TARGET_NAME}}`, `{{TARGET_ACTIVE_TEXT}}`, `{{TARGET_DEFAULT_TEXT}}`, `{{TARGET_COMPLETE_TEXT}}`
**Optional:** `{{DELIVERY_ITEM}}` -- if set, the player must have and surrender an item

### Quest Definition

```json
{
  "id": "{{QUEST_ID}}",
  "name": "{{QUEST_NAME}}",
  "description": "{{QUEST_DESCRIPTION}}",
  "objectives": [
    {
      "description": "Speak with {{TARGET_NAME}}",
      "type": "flag",
      "flag": "talked_to_{{TARGET_DIALOGUE_ID}}",
      "value": 0
    }
  ],
  "rewards": [
    { "type": "set_flag", "value": "{{QUEST_ID}}_done" }
  ]
}
```

**With delivery item**, add this objective before "Speak with":

```json
{
  "description": "Deliver the {{DELIVERY_ITEM}}",
  "type": "flag",
  "flag": "delivered_{{QUEST_ID}}_item",
  "value": 0
}
```

### Quest-Giver Dialogue (`dialogues/{{GIVER_DIALOGUE_ID}}.json`)

```json
{
  "id": "{{GIVER_DIALOGUE_ID}}",
  "routes": [
    {
      "startNode": "post_quest",
      "conditions": [
        { "type": "quest_complete", "value": "{{QUEST_ID}}" }
      ]
    },
    {
      "startNode": "in_progress",
      "conditions": [
        { "type": "quest_active", "value": "{{QUEST_ID}}" }
      ]
    },
    {
      "startNode": "start"
    }
  ],
  "nodes": [
    {
      "id": "post_quest",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_COMPLETE_TEXT}}"
    },
    {
      "id": "in_progress",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_PROGRESS_TEXT}}"
    },
    {
      "id": "start",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_INTRO_TEXT}}",
      "choices": [
        {
          "text": "{{ACCEPT_CHOICE_TEXT}}",
          "nextNodeId": "quest_accept",
          "actions": [
            { "type": "start_quest", "value": "{{QUEST_ID}}" },
            { "type": "set_flag", "value": "{{QUEST_ID}}_accepted" }
          ]
        },
        {
          "text": "{{DECLINE_CHOICE_TEXT}}",
          "nextNodeId": "decline"
        }
      ]
    },
    {
      "id": "quest_accept",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_ACCEPT_TEXT}}"
    },
    {
      "id": "decline",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_DECLINE_TEXT}}"
    }
  ]
}
```

### Target Dialogue (`dialogues/{{TARGET_DIALOGUE_ID}}.json`)

```json
{
  "id": "{{TARGET_DIALOGUE_ID}}",
  "routes": [
    {
      "startNode": "post_quest",
      "conditions": [
        { "type": "quest_complete", "value": "{{QUEST_ID}}" }
      ]
    },
    {
      "startNode": "quest_active",
      "conditions": [
        { "type": "quest_active", "value": "{{QUEST_ID}}" }
      ]
    },
    {
      "startNode": "default"
    }
  ],
  "nodes": [
    {
      "id": "post_quest",
      "speaker": "{{TARGET_NAME}}",
      "text": "{{TARGET_COMPLETE_TEXT}}"
    },
    {
      "id": "quest_active",
      "speaker": "{{TARGET_NAME}}",
      "text": "{{TARGET_ACTIVE_TEXT}}",
      "actions": [
        { "type": "set_flag", "value": "talked_to_{{TARGET_DIALOGUE_ID}}" },
        { "type": "complete_objective", "value": "{{QUEST_ID}}_talked" },
        { "type": "log", "value": "You delivered the message." }
      ]
    },
    {
      "id": "default",
      "speaker": "{{TARGET_NAME}}",
      "text": "{{TARGET_DEFAULT_TEXT}}"
    }
  ]
}
```

**With delivery item**, add these to the `quest_active` node's actions:

```json
{ "type": "remove_item", "value": "{{DELIVERY_ITEM}}" },
{ "type": "set_flag", "value": "delivered_{{QUEST_ID}}_item" }
```

---

## Investigation Quest

> NPC asks the player to gather information by talking to multiple witnesses.

**NPCs:** Quest-giver + 2-3 witnesses
**Additional variables (per witness):** `{{WITNESS_N_DIALOGUE_ID}}`, `{{WITNESS_N_NAME}}`, `{{WITNESS_N_TEXT}}`, `{{WITNESS_N_DONE_TEXT}}`, `{{CLUE_N_FLAG}}`
**Additional variables (giver):** `{{GIVER_ALL_CLUES_TEXT}}`

Replace N with 1, 2, 3 for each witness. Add or remove witness sections as needed.

### Quest Definition

```json
{
  "id": "{{QUEST_ID}}",
  "name": "{{QUEST_NAME}}",
  "description": "{{QUEST_DESCRIPTION}}",
  "objectives": [
    {
      "description": "Talk to {{WITNESS_1_NAME}}",
      "type": "flag",
      "flag": "{{CLUE_1_FLAG}}",
      "value": 0
    },
    {
      "description": "Talk to {{WITNESS_2_NAME}}",
      "type": "flag",
      "flag": "{{CLUE_2_FLAG}}",
      "value": 0
    },
    {
      "description": "Talk to {{WITNESS_3_NAME}}",
      "type": "flag",
      "flag": "{{CLUE_3_FLAG}}",
      "value": 0
    },
    {
      "description": "Report findings",
      "type": "flag",
      "flag": "objective_complete:{{QUEST_ID}}_reported",
      "value": 0
    }
  ],
  "rewards": [
    { "type": "set_flag", "value": "{{QUEST_ID}}_done" }
  ]
}
```

### Quest-Giver Dialogue (`dialogues/{{GIVER_DIALOGUE_ID}}.json`)

```json
{
  "id": "{{GIVER_DIALOGUE_ID}}",
  "routes": [
    {
      "startNode": "post_quest",
      "conditions": [
        { "type": "quest_complete", "value": "{{QUEST_ID}}" }
      ]
    },
    {
      "startNode": "all_clues",
      "conditions": [
        { "type": "quest_active", "value": "{{QUEST_ID}}" },
        { "type": "has_flag", "flag": "{{CLUE_1_FLAG}}" },
        { "type": "has_flag", "flag": "{{CLUE_2_FLAG}}" },
        { "type": "has_flag", "flag": "{{CLUE_3_FLAG}}" }
      ]
    },
    {
      "startNode": "in_progress",
      "conditions": [
        { "type": "quest_active", "value": "{{QUEST_ID}}" }
      ]
    },
    {
      "startNode": "start"
    }
  ],
  "nodes": [
    {
      "id": "post_quest",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_COMPLETE_TEXT}}"
    },
    {
      "id": "all_clues",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_ALL_CLUES_TEXT}}",
      "choices": [
        {
          "text": "Here is what I found out.",
          "nextNodeId": "conclusion",
          "actions": [
            { "type": "complete_objective", "value": "{{QUEST_ID}}_reported" }
          ]
        }
      ]
    },
    {
      "id": "conclusion",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{REWARD_TEXT}}",
      "actions": [
        { "type": "log", "value": "{{GIVER_NAME}} thanks you for the information." }
      ]
    },
    {
      "id": "in_progress",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_PROGRESS_TEXT}}"
    },
    {
      "id": "start",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_INTRO_TEXT}}",
      "choices": [
        {
          "text": "{{ACCEPT_CHOICE_TEXT}}",
          "nextNodeId": "quest_accept",
          "actions": [
            { "type": "start_quest", "value": "{{QUEST_ID}}" }
          ]
        },
        {
          "text": "{{DECLINE_CHOICE_TEXT}}",
          "nextNodeId": "decline"
        }
      ]
    },
    {
      "id": "quest_accept",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_ACCEPT_TEXT}}"
    },
    {
      "id": "decline",
      "speaker": "{{GIVER_NAME}}",
      "text": "{{GIVER_DECLINE_TEXT}}"
    }
  ]
}
```

### Witness Dialogue (`dialogues/{{WITNESS_N_DIALOGUE_ID}}.json`)

Repeat this template for each witness, replacing N with 1, 2, or 3.

```json
{
  "id": "{{WITNESS_N_DIALOGUE_ID}}",
  "routes": [
    {
      "startNode": "already_talked",
      "conditions": [
        { "type": "has_flag", "flag": "{{CLUE_N_FLAG}}" }
      ]
    },
    {
      "startNode": "default"
    }
  ],
  "nodes": [
    {
      "id": "already_talked",
      "speaker": "{{WITNESS_N_NAME}}",
      "text": "{{WITNESS_N_DONE_TEXT}}"
    },
    {
      "id": "default",
      "speaker": "{{WITNESS_N_NAME}}",
      "text": "{{WITNESS_N_TEXT}}",
      "actions": [
        { "type": "set_flag", "value": "{{CLUE_N_FLAG}}" },
        { "type": "log", "value": "You learned something from {{WITNESS_N_NAME}}." }
      ]
    }
  ]
}
```
