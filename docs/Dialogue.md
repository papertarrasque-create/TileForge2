---
updated: 2026-03-06
status: current
---

# Dialogue System

Dialogues are per-file JSON definitions (`dialogues/{id}.json`) displayed via the DialogueScreen overlay during play mode. They support linear sequences, branching choices, flag conditions, and side effects.

## Node Structure

```
DialogueNode
  Id: string              -- Node identifier within dialogue
  Speaker: string         -- Who is speaking
  Text: string            -- The spoken text
  Choices: list or null   -- null = linear auto-advance
  NextNodeId: string      -- For linear sequences (no choices)
  RequiresFlag: string    -- Skip node if flag not set
  SetsFlag: string        -- Set flag when node is displayed
  SetsVariable: string    -- "key=value" format, applied when shown
  EditorX, EditorY: int?  -- Layout positions for [[Dialogue Tree Editor]]
```

```
DialogueChoice
  Text: string            -- Choice display text
  NextNodeId: string      -- Where this choice leads
  RequiresFlag: string    -- Hide choice if flag not set
  SetsFlag: string        -- Set flag when choice selected
```

## Flow Types

### Linear Dialogue

When `Choices` is null, the dialogue auto-advances:
- Player presses Interact to progress
- Follows the `NextNodeId` chain
- Ends when NextNodeId is null or empty
- Good for cutscenes, monologues, NPC greetings

### Branching Dialogue

When `Choices` is populated:
- Up/Down arrows select a choice
- Interact confirms the selection
- Each choice has its own `NextNodeId`
- Choices can be conditionally hidden via `RequiresFlag`

## Conditions

### Node-Level (`RequiresFlag`)

If the flag is not set in GameState:
- Node is **skipped entirely** (not shown to player)
- Advances to `NextNodeId` without display
- Enables silent branching based on prior decisions

### Choice-Level (`RequiresFlag`)

If the flag is not set:
- Choice is **hidden** from the visible list
- Other choices remain visible
- Enables conditional conversation paths

## Side Effects

**When a node is shown:**
1. Text logged to [[Sidebar HUD]] GameLog
2. `SetsFlag` applied (if set)
3. `SetsVariable` applied (if set, "key=value" format parsed)
4. Visible choices filtered by RequiresFlag

**When a choice is selected:**
1. Choice's `SetsFlag` applied
2. Advance to choice's `NextNodeId`

Side effects are sequential -- RequiresFlag is checked before entry, SetsFlag is applied during display, choice flags update after selection.

## Typewriter Text Reveal

- Speed: 40 characters per second
- During reveal: pressing Interact skips to full text
- After full reveal: can proceed to next node or select choice
- Creates a classic RPG dialogue feel

## Triggering Dialogue

`CheckEntityInteractionAt()` in GameplayScreen:

1. Player uses Interact action adjacent to entity
2. Entity must be NPC or Interactable type
3. Checks for `dialogue_id` property (preferred) or `dialogue` property (inline fallback)
4. `dialogue_id` loads from `dialogues/{id}.json` via `IDialogueLoader`
5. Creates DialogueScreen overlay on the screen stack
6. Floating messages cleared on dialogue start
7. DialogueScreen closes when node chain exhausts (null NextNodeId, no choices)

Dialogue interaction costs 0 AP -- it's a free action.

## Concluded Dialogue

When an NPC's dialogue tree has been fully exhausted, the entity can show a different reminder/repeat dialogue:

1. Set `concluded_flag` on the entity (e.g., `elder_quest_done`)
2. Set `concluded_dialogue` on the entity (dialogue ID or inline text, e.g., `Have you found my hat?`)
3. The main dialogue's final node should set the flag via `SetsFlag`

On interaction, if `concluded_flag` is set in GameState, `concluded_dialogue` is shown instead of the main dialogue. This avoids replaying the full tree and gives the NPC a contextual reminder line.

## Pickup Dialogue

Item entities can show dialogue on first pickup via the `on_pickup_dialogue` property:

1. Set `on_pickup_dialogue` on an item entity (dialogue ID or inline text)
2. When the player picks up an item with this property, the dialogue is shown once
3. Subsequent pickups of the same item group do not trigger the dialogue again
4. Tracked via the `pickup_dialogue_shown:{DefinitionName}` flag

## JSON Format

Stored as `{projectDir}/dialogues/{id}.json` using **camelCase** JSON (unlike quests which use snake_case).

```json
{
  "id": "elder_01",
  "nodes": [
    {
      "id": "start",
      "speaker": "Village Elder",
      "text": "Welcome, traveler.",
      "choices": [
        { "text": "What happened?", "nextNodeId": "explain" },
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
          "setsFlag": "quest_caves_accepted"
        }
      ]
    }
  ]
}
```

See [[File Formats]] for the full spec.

## Authoring

Dialogues are authored in the [[Dialogue Tree Editor]] -- a visual node-graph editor with pannable/zoomable canvas, draggable nodes, and Bezier connection lines. `EditorX`/`EditorY` on each node persist layout positions.

## Integration with Quests

Dialogue side effects (`SetsFlag`, `SetsVariable`) are the primary mechanism for:
- Starting [[Quests]] (setting a quest's `StartFlag`)
- Completing "talk to NPC" objectives
- Gating conversation paths based on quest progress

## Related

- [[Dialogue Tree Editor]] -- Visual authoring tool
- [[Quests]] -- How dialogue flags drive quest progress
- [[Entities]] -- Which entity types can trigger dialogue
- [[File Formats]] -- Dialogue JSON spec
