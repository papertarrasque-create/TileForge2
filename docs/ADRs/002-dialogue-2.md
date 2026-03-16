---
status: proposed
date: 2026-03-06
---

# ADR-002: Dialogue System 2.0

## Context

The current dialogue system works for simple conversations but breaks down as entity interactions grow more complex. Key pain points:

1. **Fragmented triggering** -- Three separate code paths for NPC dialogue, item pickup dialogue, and concluded dialogue state. Each entity type has its own dialogue wiring in GameplayScreen.
2. **State tracking split across two layers** -- "Has this dialogue concluded?" requires manual flag coordination between the dialogue tree (SetsFlag on final node) and the entity (concluded_flag/concluded_dialogue properties). Two sources of truth.
3. **No dialogue routing** -- An NPC with 3 conversation states (fresh, quest-in-progress, quest-complete) must chain conditional entry nodes via RequiresFlag. Works but is an authoring burden and error-prone.
4. **Quest integration is indirect** -- Dialogue sets flags, quests poll flags. No way to say "this dialogue advances this quest" directly.
5. **Entity types artificially limit dialogue** -- Traps and Triggers can't have dialogue. Items need a special property. The system doesn't treat dialogue as a universal entity capability.
6. **No repeatable/one-shot distinction** -- The system has no concept of whether a dialogue should replay or not.

## Decision

Redesign the dialogue system around three concepts: **dialogue routing**, **unified triggering**, and **direct quest hooks**.

### 1. Dialogue Router (replaces concluded_flag/concluded_dialogue)

Instead of entity-level properties choosing between dialogues, the dialogue file itself contains a **router** -- an ordered list of conditional entry points.

```json
{
  "id": "elder_01",
  "routes": [
    { "requiresFlag": "quest_caves_complete", "startNode": "thanks" },
    { "requiresFlag": "quest_caves_accepted", "startNode": "reminder" },
    { "requiresFlag": "quest_declined", "startNode": "changed_mind" },
    { "startNode": "start" }
  ],
  "nodes": [ ... ]
}
```

**Routes are evaluated top-to-bottom. First match wins.** The last route (no requiresFlag) is the default. This replaces the RequiresFlag-chaining pattern for conditional entry AND the concluded_flag/concluded_dialogue entity properties.

Routes can also check variables:

```json
{ "requiresVariable": "quest_stage", "operator": "gte", "value": "3", "startNode": "stage3" }
```

**Migration:** Existing dialogues without `routes` default to `[{ "startNode": firstNode.Id }]`.

### 2. Unified Dialogue Trigger (replaces per-entity-type wiring)

Any entity can trigger dialogue. One property, one code path:

- **`dialogue_id`** -- The single property for file-based dialogue (on any entity type)
- **`dialogue`** -- Inline text fallback (unchanged)

The `on_pickup_dialogue` property stays for items (it's a *collection event* trigger, not an interaction trigger). But `concluded_flag` and `concluded_dialogue` are **removed** -- the router handles this.

In GameplayScreen, `CheckEntityInteractionAt` changes from a per-type switch to:

```
For any entity type:
  1. If entity has dialogue_id or dialogue → TryShowDialogue()
  2. Then handle type-specific behavior (collect item, trigger map transition, etc.)
```

Traps can now have dialogue ("A rune glows beneath your feet..."). Triggers can have dialogue ("The portal hums with energy..."). The entity type controls *what happens* (damage, transition), not *whether dialogue can occur*.

### 3. Dialogue Actions (replaces SetsFlag/SetsVariable on nodes)

Currently nodes can set one flag and one variable. This is limiting -- a quest-advancing dialogue might need to set a flag, increment a variable, give an item, and start a quest all in one node.

Replace the single `SetsFlag`/`SetsVariable` with an **actions list**:

```json
{
  "id": "quest_accept",
  "speaker": "Elder",
  "text": "Thank you! Take this healing potion.",
  "actions": [
    { "type": "set_flag", "value": "quest_caves_accepted" },
    { "type": "set_variable", "key": "quest_stage", "value": "1" },
    { "type": "give_item", "item": "Potion" },
    { "type": "start_quest", "quest": "caves_quest" },
    { "type": "log", "text": "Quest accepted: Clear the Caves", "color": "yellow" }
  ]
}
```

**Action types:**
- `set_flag` -- Set a flag in GameState (replaces SetsFlag)
- `set_variable` -- Set a variable (replaces SetsVariable)
- `increment` -- Increment a variable by 1
- `give_item` -- Add item to player inventory
- `remove_item` -- Remove item from inventory
- `start_quest` -- Directly activate a quest (replaces manual flag → poll pattern)
- `complete_objective` -- Mark a quest objective as done
- `heal` -- Heal the player
- `damage` -- Damage the player
- `log` -- Add a custom GameLog message

Actions also work on choices:

```json
{
  "text": "I accept!",
  "nextNodeId": "accepted",
  "actions": [
    { "type": "set_flag", "value": "quest_accepted" }
  ]
}
```

**Migration:** Existing `SetsFlag` and `SetsVariable` are converted to single-element `actions` arrays during load. Old format still deserializes.

### 4. Conditions (replaces RequiresFlag)

Generalize `RequiresFlag` to a **conditions** system that can check flags, variables, inventory, and quest state:

```json
{
  "id": "has_sword_node",
  "conditions": [
    { "type": "has_flag", "flag": "found_sword" },
    { "type": "has_item", "item": "Magic Sword" }
  ],
  "text": "You found the sword! Well done."
}
```

All conditions must be met (AND logic). Nodes/choices with unmet conditions are skipped/hidden (same behavior as current RequiresFlag).

**Condition types:**
- `has_flag` -- Flag is set (replaces RequiresFlag)
- `not_flag` -- Flag is NOT set
- `has_item` -- Item in inventory
- `variable_eq` / `variable_gte` / `variable_lt` -- Variable comparisons
- `quest_active` / `quest_complete` -- Quest state checks

**Migration:** Existing `RequiresFlag` becomes `[{ "type": "has_flag", "flag": "..." }]`.

### 5. One-Shot Dialogues

Add a top-level `oneShot` flag to DialogueData:

```json
{
  "id": "found_sword",
  "oneShot": true,
  "nodes": [...]
}
```

When `oneShot` is true, the system auto-sets `dialogue_shown:{dialogueId}` after the dialogue completes. On re-interaction, the dialogue is skipped (returns false from TryShowDialogue). This replaces the `pickup_dialogue_shown` flag pattern with a general mechanism.

### 6. Dialogue Type (extension hook)

Add a top-level `type` field to DialogueData. Default is `"conversation"` (the current full-overlay DialogueScreen behavior). The runtime dispatches to different handlers based on type:

```json
{
  "id": "elder_01",
  "type": "conversation",
  "routes": [...],
  "nodes": [...]
}
```

**Implemented now:**
- `"conversation"` -- Full dialogue overlay with typewriter, choices, speaker name (current behavior)

**Reserved for future (not implemented, just hooks):**
- `"bark"` -- Quick floating text above the entity, no overlay, no player input. Auto-dismisses after a timer. Could support random node selection for variety.
- `"cutscene"` -- Auto-advancing narration. Nodes progress on a timer (or manually). No choices. Full-screen overlay.
- `"shop"` -- Opens a shop/trade UI instead of the dialogue overlay. Nodes define shop inventory and pricing via actions.
- `"inspect"` -- Lore text displayed in a compact popup (item description, sign text). One-shot by default.

The runtime hook is a simple dispatch in `TryShowDialogue`:

```
switch (dialogue.Type ?? "conversation")
{
    case "conversation": Push(new DialogueScreen(...)); break;
    case "bark":         // future: Push(new BarkOverlay(...));
    case "cutscene":     // future: Push(new CutsceneScreen(...));
    default:             Push(new DialogueScreen(...)); break;  // fallback
}
```

Unknown types fall back to `"conversation"`. This means future dialogue types can be added by:
1. Creating a new screen class
2. Adding a case to the dispatch
3. No data model changes needed -- Routes, Conditions, and Actions work for all types

### 7. Node Tags (extension hook)

Add an optional `tags` list to DialogueNode for future metadata without changing the core schema:

```json
{
  "id": "greeting",
  "tags": ["random", "repeatable"],
  "text": "Hello there!"
}
```

**Not interpreted by the runtime now.** Reserved for future use:
- `"random"` -- For bark dialogue, pick one tagged node at random instead of sequential
- `"auto_advance"` -- Node advances after typewriter completes (no player input needed)
- `"camera_shake"` -- Visual effect trigger
- `"sound:door_open"` -- Sound effect trigger

Tags are a string list, serialized as JSON array, ignored if absent. This avoids adding boolean fields for every future feature.

## Data Model Changes

### DialogueData (v2)
```
DialogueData
  Id: string
  Type: string                    // NEW (default "conversation"; hook for future types)
  Routes: List<DialogueRoute>     // NEW (nullable, defaults to first-node entry)
  Nodes: List<DialogueNode>
  OneShot: bool                   // NEW (default false)
```

### DialogueRoute (NEW)
```
DialogueRoute
  StartNode: string               // Node ID to start from
  Conditions: List<Condition>     // All must pass (empty = always matches)
```

### DialogueNode (v2)
```
DialogueNode
  Id: string
  Speaker: string
  Text: string
  Choices: List<DialogueChoice>
  NextNodeId: string
  Conditions: List<Condition>     // Replaces RequiresFlag
  Actions: List<DialogueAction>   // Replaces SetsFlag + SetsVariable
  Tags: List<string>              // NEW (extension hook, nullable)
  EditorX: int?
  EditorY: int?
```

### DialogueChoice (v2)
```
DialogueChoice
  Text: string
  NextNodeId: string
  Conditions: List<Condition>     // Replaces RequiresFlag
  Actions: List<DialogueAction>   // Replaces SetsFlag
```

### Condition (NEW)
```
Condition
  Type: string          // has_flag, not_flag, has_item, variable_eq, etc.
  Flag: string          // For flag conditions
  Item: string          // For item conditions
  Variable: string      // For variable conditions
  Value: string         // Comparison value
  Operator: string      // eq, gte, lt (for variable conditions)
```

### DialogueAction (NEW)
```
DialogueAction
  Type: string          // set_flag, set_variable, give_item, etc.
  Value: string         // Primary value (flag name, variable value, etc.)
  Key: string           // Secondary key (variable name, item name)
  Color: string         // For log action
  Text: string          // For log action
```

## Entity Property Changes

### Removed
- `concluded_flag` -- Router handles this
- `concluded_dialogue` -- Router handles this
- `on_pickup_dialogue` -- Replaced by `dialogue_id` + oneShot on the dialogue

### Kept
- `dialogue_id` -- Now works on ALL entity types
- `dialogue` -- Inline text fallback (unchanged)

### GroupEditor Changes
- All entity type presets get `dialogue_id` as first property
- Remove `concluded_flag`, `concluded_dialogue`, `on_pickup_dialogue` from presets
- Dialogue dropdown picker works the same way

## Migration Strategy

### Backward Compatibility
- Old JSON format (SetsFlag, RequiresFlag, SetsVariable as strings) auto-converts on load
- DialogueFileManager.LoadOne detects v1 format and upgrades in memory
- Saving always writes v2 format
- No flag renaming needed -- existing flags continue to work

### Conversion Rules
- `RequiresFlag: "x"` → `conditions: [{ type: "has_flag", flag: "x" }]`
- `SetsFlag: "x"` → `actions: [{ type: "set_flag", value: "x" }]`
- `SetsVariable: "k=v"` → `actions: [{ type: "set_variable", key: "k", value: "v" }]`
- No `routes` → `routes: [{ startNode: firstNodeId }]`

## Implementation Plan

### Phase 1: Data Model + Migration (Sonnet-parallelizable)
- New data classes: DialogueRoute, Condition, DialogueAction
- Migration logic in DialogueFileManager (v1 → v2 on load)
- Serialization tests for new format
- Backward compatibility tests

### Phase 2: Runtime (Opus)
- DialogueScreen: evaluate Conditions instead of RequiresFlag, execute Actions instead of SetsFlag
- Route evaluation in TryShowDialogue (top-to-bottom, first match)
- OneShot flag tracking
- Unified entity triggering (remove per-type dialogue wiring)
- Action executor (set_flag, give_item, start_quest, etc.)

### Phase 3: Editor (Opus)
- DialogueTreeEditor: routes panel, conditions editor, actions editor
- GroupEditor: simplified presets (dialogue_id on all types)
- Remove concluded_flag/concluded_dialogue UI

### Phase 4: Cleanup
- Remove old entity properties from presets
- Delete legacy DialogueEditor.cs
- Update docs and wiki

## Consequences

### Positive
- One dialogue file = complete conversation logic (no entity-property coordination)
- Any entity type can have dialogue
- Direct quest integration (start_quest, complete_objective actions)
- Conditions are composable (AND multiple checks)
- Actions are composable (do multiple things per node)
- Router eliminates manual RequiresFlag entry-node chaining
- OneShot replaces manual pickup_dialogue_shown flag pattern
- Type field enables future dialogue modes (bark, cutscene, shop) without schema changes
- Tags field enables future per-node metadata without adding boolean fields
- Framework is dialogue-type-agnostic -- Routes, Conditions, Actions work for all types

### Negative
- More complex data model (Condition and Action objects vs simple strings)
- DialogueTreeEditor needs significant UI work for routes/conditions/actions
- Migration adds temporary complexity (v1/v2 format handling)
- Actions that affect game state (give_item, damage) blur the line between dialogue and gameplay logic

### Risks
- Action executor could become a mini scripting language if scope creeps
- Conditions + Routes could make simple dialogues harder to author (mitigated by defaults)
- Need to keep the "simple case simple" -- a basic NPC greeting shouldn't require routes or actions

## Related

- [[ADRs/001-initial-architecture]] -- Property bag extensibility
- [[Dialogue]] -- Current system docs
- [[Quests]] -- Quest flag polling (replaced by direct actions)
- [[Property Reference]] -- Entity property keys
