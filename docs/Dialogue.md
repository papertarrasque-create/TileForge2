---
updated: 2026-03-06
status: current
---

# Dialogue System

Dialogues are per-file JSON definitions (`dialogues/{id}.json`) displayed via the DialogueScreen overlay during play mode. They support linear sequences, branching choices, conditional routing, rich conditions, and side-effect actions.

## Top-Level Fields

```
DialogueFile
  id: string              -- Unique dialogue identifier
  type: string            -- "conversation" (default). Reserved: bark, cutscene, shop, inspect
  oneShot: bool           -- If true, auto-sets "dialogue_shown:{id}" after completion; skipped on re-interact
  routes: list            -- Conditional entry points (evaluated top-to-bottom)
  nodes: list             -- The dialogue nodes
```

## Routes

Routes replace the old concluded_flag/concluded_dialogue entity property pattern. A `routes` array on the dialogue file defines conditional entry points:

- Evaluated **top-to-bottom** -- first match wins
- Each route has a `conditions` array and a `startNodeId`
- If no route matches, falls through to the default `"start"` node
- Enables a single dialogue file to handle first-visit, post-quest, and repeat interactions

```
DialogueRoute
  conditions: list        -- All must pass (AND logic)
  startNodeId: string     -- Node to begin dialogue at if conditions match
```

## Node Structure

```
DialogueNode
  id: string              -- Node identifier within dialogue
  speaker: string         -- Who is speaking
  text: string            -- The spoken text
  choices: list or null   -- null = linear auto-advance
  nextNodeId: string      -- For linear sequences (no choices)
  conditions: list        -- Skip node if any condition fails
  actions: list           -- Side effects applied when node is displayed
  tags: list              -- Optional string tags (extension hook, not interpreted yet)
  editorX, editorY: int?  -- Layout positions for [[Dialogue Tree Editor]]
```

```
DialogueChoice
  text: string            -- Choice display text
  nextNodeId: string      -- Where this choice leads
  conditions: list        -- Hide choice if any condition fails
  actions: list           -- Side effects applied when choice is selected
```

## Flow Types

### Linear Dialogue

When `choices` is null, the dialogue auto-advances:
- Player presses Interact to progress
- Follows the `nextNodeId` chain
- Ends when nextNodeId is null or empty
- Good for cutscenes, monologues, NPC greetings

### Branching Dialogue

When `choices` is populated:
- Up/Down arrows select a choice
- Interact confirms the selection
- Each choice has its own `nextNodeId`
- Choices can be conditionally hidden via `conditions`

## Conditions

Nodes and choices share the same `conditions` array. All conditions must pass (AND logic). Available condition types:

| Type | Description |
|------|-------------|
| `has_flag` | Flag is set in GameState |
| `not_flag` | Flag is NOT set in GameState |
| `has_item` | Player has the named item in inventory |
| `variable_eq` | GameState variable equals a value |
| `variable_gte` | GameState variable is >= a value |
| `variable_lt` | GameState variable is < a value |
| `quest_active` | Named quest is currently active |
| `quest_complete` | Named quest has been completed |

### Node-Level Conditions

If any condition fails:
- Node is **skipped entirely** (not shown to player)
- Advances to `nextNodeId` without display
- Enables silent branching based on prior decisions

### Choice-Level Conditions

If any condition fails:
- Choice is **hidden** from the visible list
- Other choices remain visible
- Enables conditional conversation paths

## Actions

Nodes and choices share the same `actions` array. Actions on nodes fire when the node is displayed. Actions on choices fire when the choice is selected. Available action types:

| Type | Description |
|------|-------------|
| `set_flag` | Set a flag in GameState |
| `set_variable` | Set a variable to a value |
| `increment` | Increment a numeric variable by a value |
| `give_item` | Add an item to the player's inventory |
| `remove_item` | Remove an item from the player's inventory |
| `start_quest` | Start the named quest |
| `complete_objective` | Complete an objective on an active quest |
| `heal` | Heal the player by a value |
| `damage` | Damage the player by a value |
| `log` | Write a message to the [[Sidebar HUD]] GameLog |

## OneShot Dialogues

Setting `"oneShot": true` on the dialogue file:
1. After the dialogue completes, automatically sets the flag `dialogue_shown:{id}`
2. On re-interaction, the dialogue is skipped entirely
3. Useful for item inspections, one-time lore, tutorial prompts

## Typewriter Text Reveal

- Speed: 40 characters per second
- During reveal: pressing Interact skips to full text
- After full reveal: can proceed to next node or select choice
- Creates a classic RPG dialogue feel

## Triggering Dialogue

`CheckEntityInteractionAt()` in GameplayScreen:

1. Player uses Interact action adjacent to entity
2. **Any entity type** can trigger dialogue via the `dialogue_id` property (not limited to NPC/Interactable)
3. `dialogue_id` loads from `dialogues/{id}.json` via `IDialogueLoader`
4. Routes are evaluated to determine the start node
5. Creates DialogueScreen overlay on the screen stack
6. Floating messages cleared on dialogue start
7. DialogueScreen closes when node chain exhausts (null nextNodeId, no choices)

Dialogue interaction costs 0 AP -- it's a free action.

## JSON Format

Stored as `{projectDir}/dialogues/{id}.json` using **camelCase** JSON (unlike quests which use snake_case).

```json
{
  "id": "elder_01",
  "type": "conversation",
  "oneShot": false,
  "routes": [
    {
      "conditions": [
        { "type": "has_flag", "flag": "caves_cleared" }
      ],
      "startNodeId": "post_quest"
    },
    {
      "conditions": [
        { "type": "quest_active", "quest": "clear_caves" }
      ],
      "startNodeId": "quest_reminder"
    }
  ],
  "nodes": [
    {
      "id": "start",
      "speaker": "Village Elder",
      "text": "Welcome, traveler.",
      "choices": [
        { "text": "What happened?", "nextNodeId": "explain" },
        {
          "text": "About those caves...",
          "nextNodeId": "quest_reminder",
          "conditions": [
            { "type": "quest_active", "quest": "clear_caves" }
          ]
        },
        { "text": "Just passing through.", "nextNodeId": "decline" }
      ],
      "editorX": 56,
      "editorY": -174
    },
    {
      "id": "explain",
      "speaker": "Village Elder",
      "text": "Strange creatures invaded the caves.",
      "choices": [
        {
          "text": "I will help!",
          "nextNodeId": "quest_accept",
          "actions": [
            { "type": "set_flag", "flag": "quest_caves_accepted" },
            { "type": "start_quest", "quest": "clear_caves" }
          ]
        }
      ]
    },
    {
      "id": "quest_reminder",
      "speaker": "Village Elder",
      "text": "Have you cleared the caves yet? Please hurry!",
      "nextNodeId": null
    },
    {
      "id": "post_quest",
      "speaker": "Village Elder",
      "text": "You saved us all! Take this reward.",
      "actions": [
        { "type": "give_item", "item": "gold_ring" },
        { "type": "log", "message": "The Elder gives you a gold ring." }
      ],
      "nextNodeId": null
    }
  ]
}
```

See [[File Formats]] for the full spec.

## Backward Compatibility

Old v1 dialogue format (using `requiresFlag`, `setsFlag`, `setsVariable` on nodes and choices) is auto-migrated on load:

- `requiresFlag` converts to `conditions: [{ "type": "has_flag", "flag": "..." }]`
- `setsFlag` converts to `actions: [{ "type": "set_flag", "flag": "..." }]`
- `setsVariable` (key=value format) converts to `actions: [{ "type": "set_variable", "variable": "key", "value": "value" }]`

No manual migration needed -- v1 files work as-is.

## Authoring

Dialogues are authored in the [[Dialogue Tree Editor]] -- a visual node-graph editor with pannable/zoomable canvas, draggable nodes, and Bezier connection lines. `editorX`/`editorY` on each node persist layout positions.

## Integration with Quests

Dialogue actions (`set_flag`, `set_variable`, `start_quest`, `complete_objective`) are the primary mechanism for:
- Starting [[Quests]] via `start_quest` action
- Completing "talk to NPC" objectives via `complete_objective`
- Gating conversation paths based on quest progress via `quest_active`/`quest_complete` conditions

## Related

- [[Dialogue Tree Editor]] -- Visual authoring tool
- [[Quests]] -- How dialogue flags drive quest progress
- [[Entities]] -- Which entity types can trigger dialogue
- [[File Formats]] -- Dialogue JSON spec
