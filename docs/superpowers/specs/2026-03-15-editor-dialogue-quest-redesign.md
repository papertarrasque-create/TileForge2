# Editor, Dialogue & Quest System Redesign

**Date:** 2026-03-15
**Status:** Proposed
**Branch:** `Dialog` (will branch from)

## Problem Statement

Three interconnected pain points have accumulated as TileForge's feature set has grown:

1. **Dialogue and quest authoring is unintuitive.** The DialogueTreeEditor is a 2066-line monolith with 77 private fields and 10+ parallel hit-test rectangle lists. The QuestEditor uses stringly-typed reward parsing (`"gold=100, rep=5"` split by comma and `=`). The relationship between dialogue actions and quest state relies on magic string prefixes scattered across files.

2. **The editor UI is cramped.** Four panels (Map, Quests, Dialogues, Tileset) compete for vertical space in a narrow sidebar. Quest and dialogue panels get ~40px each -- barely room for 2 items before clipping.

3. **Runtime coupling and v1 debt.** GameplayScreen holds an EditorState reference and mutates it directly. V1 fallback code (checking both `Conditions` AND `RequiresFlag`, both `Actions` AND `SetsFlag`) persists throughout the runtime despite v2 migration handling conversion on load.

## Design Overview

Seven coordinated changes, ordered by dependency:

1. **Workspace Modes** -- toolbar-based mode switching replaces sidebar cramming
2. **Dialogue Editor Decomposition** -- split monolith into focused components
3. **Quest Editor Redesign** -- structured rewards, persistent QuestState, constants
4. **Unified Trigger System** -- one code path for all dialogue/event triggers
5. **Dialogue Type Dispatch** -- bark, inspect, cutscene screens
6. **GameWorldView DTO** -- decouple runtime from editor state
7. **V1 Cleanup** -- remove legacy fallbacks and dead code

---

## 1. Workspace Modes & Editor Layout

### Current State
All panels stacked in one sidebar column. Dialogue/quest panels get minimal vertical space. Content editors open as modals overlaying the canvas.

### Design

Replace stacked panels with **workspace modes** switched via toolbar buttons.

| Mode | Toolbar Button | Canvas Shows | Sidebar Shows |
|------|---------------|-------------|---------------|
| **Map** (default) | Paint/Select/Erase/Fill | Map canvas | Layers + Groups + Tileset |
| **Dialogues** | "Dialogues" | Node graph + properties | Dialogue list |
| **Quests** | "Quests" | Quest detail editor | Quest list |
| **World Map** | "World Map" | World layout grid | Map list |

### New Types

```
WorkspaceMode enum: Map, Dialogues, Quests, WorldMap

IWorkspace interface:
  Update(GameTime, InputState)
  Draw(SpriteBatch, SpriteFont)
  DrawSidebar(SpriteBatch, SpriteFont, Rectangle bounds)
  OnEnter()
  OnExit()
```

### Implementation Details

- `EditorState` gets `ActiveWorkspace` property with `WorkspaceChanged` event
- `TileForgeGame` routes Update/Draw to the active workspace
- Each workspace is a self-contained class implementing `IWorkspace`
- Existing `PanelDock` stays for Map workspace (Layers + Groups + Tileset only)
- `DialoguePanel` and `QuestPanel` removed from PanelDock
- Keyboard shortcuts: Escape returns to Map mode; Ctrl+1/2/3/4 for direct mode switching
- Toolbar buttons render as highlighted when their workspace is active

---

## 2. Dialogue Editor Decomposition

### Current State
`DialogueTreeEditor` is 2066 lines with 77 private fields, 10+ parallel hit-test rectangle lists, inner classes for ConditionFields/ActionFields/RouteFields/ChoiceFields, and a massive Update()/Draw() loop handling tree navigation, property editing, condition/action UI, routes, choices, layout computation, and rendering.

### New Structure

```
DialogueWorkspace : IWorkspace (~200L, orchestrator)
  ├── DialogueListPanel (~150L, sidebar: list, search, new/delete)
  ├── NodeGraphPanel (~400L, canvas: visual node cards, connections, pan/zoom)
  ├── RouteBar (~150L, top strip: route pills, reorder, add/remove)
  ├── NodePropertiesPanel (~200L, bottom: selected node fields)
  │   ├── ConditionListEditor (~150L, reusable: add/remove/edit conditions)
  │   └── ActionListEditor (~150L, reusable: add/remove/edit actions)
  └── HitTestRegistry (~50L, shared: replaces parallel rect lists)
```

### HitTestRegistry

Replaces 10+ parallel rectangle lists (`_treeRowRects`, `_choiceRemoveRects`, `_conditionRemoveRects`, etc.) with a single managed registry:

```csharp
class HitTestRegistry
{
    private Dictionary<string, List<(Rectangle Rect, int Index)>> _zones;

    void Clear()                                    // called at frame start
    void Register(string zone, Rectangle rect, int index)
    int HitTest(string zone, Point position)        // returns index or -1
}
```

Components register hit zones during Draw (e.g., `registry.Register("condition-remove", rect, i)`), query them during Update. Auto-clears each frame. No desync risk.

### ConditionFields Fix

Variable conditions currently pack `"name=value"` into one ComboBox string, then unpack with `indexOf('=')`. This breaks on values containing `=`.

New approach: separate Variable and Value fields.

```csharp
class ConditionFields
{
    Dropdown TypeDropdown;          // has_flag, not_flag, has_item, variable_eq, etc.
    ComboBox PrimaryField;          // flag name, item name, or variable name
    ComboBox ValueField;            // only shown for variable_eq/gte/lt types
}
```

### DeferredInputQueue

The existing deferred dropdown/combobox update pattern (queue during Draw, process in next Update) is correct but undocumented. Extract into a named helper:

```csharp
class DeferredInputQueue
{
    void EnqueueDropdown(Dropdown dd, Rectangle rect)
    void EnqueueComboBox(ComboBox cb, Rectangle rect)
    void ProcessAll(MouseState mouse, MouseState prev, SpriteFont font, int screenW, int screenH)
    void Clear()
}
```

### NodeGraphPanel

Replaces the tree-list view with a spatial graph:
- Nodes rendered as cards showing: ID, speaker, text preview, choice count, action icons
- Connection lines between nodes (following NextNodeId and choice targets)
- Pan and zoom (reuse existing Camera class from DojoUI)
- Click node to select; selected node's properties show in NodePropertiesPanel below
- EditorX/EditorY on DialogueNode used for persistent positions (already in data model)

---

## 3. Quest Editor Redesign

### Current State
QuestEditor (677 lines) uses `QuestRewards { SetFlags: List<string>, SetVariables: Dictionary<string,string> }` with comma-separated text parsing. No persistent quest state -- progress reconstructed from flags each frame. Magic string prefixes hardcoded in 3+ files.

### Rewards Become Actions

```csharp
// OLD
class QuestRewards
{
    List<string> SetFlags;
    Dictionary<string, string> SetVariables;
}

// NEW -- reuses the existing DialogueAction type
class QuestDefinition
{
    string Id;
    string Name;
    string Description;
    string StartFlag;               // auto: quest_started:{id}
    string CompletionFlag;          // auto: quest_complete:{id}
    List<QuestObjective> Objectives;
    List<DialogueAction> Rewards;   // same type as dialogue node actions
}
```

The `ActionListEditor` widget (from Section 2) renders rewards in the quest editor identically to how it renders actions in the dialogue editor. No more `ParseRewardFlags()` / `ParseRewardVariables()`.

### QuestState (New Class)

```csharp
class QuestState
{
    string QuestId;
    QuestStatus Status;                    // NotStarted, Active, Complete
    HashSet<string> CompletedObjectives;
    int StartedAtTurn;                     // game turn when started
}

enum QuestStatus { NotStarted, Active, Complete }
```

- `QuestManager` maintains `Dictionary<string, QuestState>`
- Serialized into `GameState.QuestStates` (survives save/load)
- Replaces session-only `_reportedStarts` / `_reportedObjectives` HashSets
- On game load, quest progress is explicit -- no re-evaluating all flags to reconstruct state

### QuestConstants

```csharp
public static class QuestConstants
{
    public const string StartedPrefix = "quest_started:";
    public const string CompletePrefix = "quest_complete:";
    public const string ObjectivePrefix = "objective_complete:";

    public static string StartedFlag(string questId) => StartedPrefix + questId;
    public static string CompleteFlag(string questId) => CompletePrefix + questId;
    public static string ObjectiveFlag(string objId) => ObjectivePrefix + objId;
}
```

Referenced everywhere instead of hardcoded strings.

### Cross-References

Quest detail view shows a "Referenced by" section listing dialogues with `start_quest` or `complete_objective` actions targeting this quest. Clickable -- switches to dialogue workspace and selects that dialogue.

### Migration

Existing `quests.json` with old `QuestRewards { SetFlags, SetVariables }` auto-converts to `List<DialogueAction>` on load:
- Each `SetFlags` entry becomes `{ Type: "set_flag", Value: flagName }`
- Each `SetVariables` entry becomes `{ Type: "set_variable", Key: varName, Value: varValue }`

---

## 4. Unified Trigger System

### Current State
Dialogue triggers are scattered: NPCs check `dialogue_id` in `CheckEntityInteractionAt()`, items have special `TryShowPickupDialogue()`, concluded_flag/concluded_dialogue adds branching. No way for tiles or skill checks to trigger anything.

### TriggerManager

```csharp
class TriggerManager
{
    TriggerResult? Fire(TriggerEvent evt, GameStateManager gsm,
                        Dictionary<string, DialogueData> dialogues)
}

class TriggerEvent
{
    TriggerSource Source;                   // Interaction, Pickup, StepOn, Proximity, SkillCheck
    string? EntityId;
    Point? TilePosition;
    Dictionary<string, string> Properties;  // the entity/tile property bag
}

enum TriggerSource { Interaction, Pickup, StepOn, Proximity, SkillCheck, Timer }

class TriggerResult
{
    TriggerResultType Type;                 // Dialogue, MapTransition, Combat, Custom
    DialogueData? Dialogue;
    string? StartNodeId;                    // resolved from routes
}

enum TriggerResultType { Dialogue, MapTransition, Combat, Custom }
```

### Flow

1. GameplayScreen detects event (interact, step-on, pickup, etc.)
2. Wraps in `TriggerEvent` with source type and entity/tile properties
3. `TriggerManager.Fire()` reads `dialogue_id`/`dialogue` from properties, loads dialogue, checks oneShot, evaluates routes, resolves start node
4. Returns `TriggerResult` -- GameplayScreen dispatches to appropriate screen based on dialogue type

### What This Replaces
- Per-entity-type dialogue wiring in `CheckEntityInteractionAt()` collapses to one `TriggerManager.Fire()` call
- `TryShowPickupDialogue()` becomes a standard trigger with `Source = Pickup`
- `TryShowDialogue()` logic moves into TriggerManager
- V1 `concluded_flag` / `concluded_dialogue` checks removed (routes handle this)

### Future Trigger Sources (hooks only, not implemented)
- `Proximity` -- entity enters radius (for bark dialogue)
- `SkillCheck` -- perception/investigation check passes
- `Timer` -- time-based events in game turns

---

## 5. Dialogue Type Dispatch

### Current State
`DialogueData.Type` field exists. Only `"conversation"` is implemented. Dispatch stub commented out.

### New Screens

| Type | Screen | Behavior | Size |
|------|--------|----------|------|
| `conversation` | `DialogueScreen` (existing) | Full overlay, typewriter, choices | ~250L (unchanged) |
| `bark` | `BarkOverlay` (new) | Floating text above entity, auto-dismiss ~2s | ~100L |
| `inspect` | `InspectOverlay` (new) | Compact popup, dismiss on any key | ~80L |
| `cutscene` | `CutsceneScreen` (new) | Auto-advancing narration, full-screen | ~150L |

### BarkOverlay
- Renders text bubble at entity world position (translated through camera)
- Fades out after duration (default 2s)
- Supports random node selection via `"random"` tag
- No input blocking -- player can keep moving
- Routes and conditions still apply (barks change based on game state)

### InspectOverlay
- Centered text box, smaller than DialogueScreen
- Shows speaker (optional) + text
- Any key/click dismisses
- Good for: signs, books, item descriptions, trigger tiles ("Something seems amiss about that rock...")

### CutsceneScreen
- Full-screen overlay
- Nodes advance on timer (default 3s) or player input
- No choices allowed
- Actions still fire on node entry
- Good for: intro sequences, dream sequences, story beats

### Meaningful Tags
- `"random"` -- BarkOverlay picks random node from tagged set
- `"auto_advance"` -- node advances without player input
- `"auto_advance_ms:2000"` -- custom timing for auto-advance

### Dispatch Location
TriggerManager resolves the dialogue and its start node. GameplayScreen reads `dialogue.Type` and pushes the appropriate screen:

```csharp
switch (result.Dialogue.Type ?? "conversation")
{
    case "conversation": screenManager.Push(new DialogueScreen(...));
    case "bark":         screenManager.Push(new BarkOverlay(...));
    case "inspect":      screenManager.Push(new InspectOverlay(...));
    case "cutscene":     screenManager.Push(new CutsceneScreen(...));
    default:             screenManager.Push(new DialogueScreen(...));  // fallback
}
```

---

## 6. GameWorldView DTO

### Current State
`GameplayScreen` holds an `EditorState` reference and mutates it directly via `SyncEntityRenderState()`. This couples the runtime to the editor.

### New Types

```csharp
class GameWorldView
{
    Dictionary<string, MapView> Maps;
    string ActiveMapId;
    Dictionary<string, GroupView> Groups;
    Dictionary<string, DialogueData> Dialogues;
    List<QuestDefinition> Quests;
    WorldLayoutData WorldLayout;
}

class MapView
{
    int Width, Height;
    List<LayerView> Layers;
}

class LayerView
{
    int[,] Tiles;                          // group indices
    List<EntityView> Entities;
}

class EntityView
{
    string Id;
    string GroupName;
    int X, Y;
    Dictionary<string, string> Properties;
}

class GroupView
{
    string Name;
    GroupType Type;
    int SpriteIndex;
    Dictionary<string, string> Properties;
}
```

### Data Flow

```
Enter Play Mode:
  EditorState --> PlayModeController.BuildWorldView() --> GameWorldView
  GameWorldView passed to GameplayScreen constructor
  GameplayScreen never sees EditorState

Exit Play Mode (revert):
  GameWorldView discarded, EditorState restored from deep-copy snapshot

Exit Play Mode (keep):
  GameWorldView --> PlayModeController.ApplyToEditor() --> EditorState
  Reverse mapping writes runtime changes back to editor
```

### What This Fixes
- `GameplayScreen` no longer imports or references `EditorState`
- `SyncEntityRenderState()` disappears -- entity positions live in `GameWorldView`
- Runtime code (`TileForge/Game/`) has zero dependency on editor code (`TileForge/UI/`, `TileForge/Editor/`)
- Answers the Status.md open question: "Should the runtime be separable from the editor?" -- yes, it just needs `GameWorldView`

### What Doesn't Change
- `PlayModeController` still manages enter/exit lifecycle
- Deep-copy snapshot on enter still happens (for revert)
- `GameStateManager` (flags, variables, inventory) is unchanged -- already decoupled

---

## 7. V1 Cleanup & Migration Finalization

### Removals

| Item | Location | Reason |
|------|----------|--------|
| V1 fallback in DialogueScreen | Dual `Conditions` + `RequiresFlag` checks, dual `Actions` + `SetsFlag` execution | Migration converts on load; v1 fields always empty at runtime |
| V1 fallback in GameplayScreen | `concluded_flag` / `concluded_dialogue` checks in `TryShowDialogue()` | Routes handle this |
| V1 fields on data model | `RequiresFlag`, `SetsFlag`, `SetsVariable` on `DialogueNode` and `DialogueChoice` | Migration populates v2 fields; v1 fields removed from serialization |
| Old QuestRewards class | `QuestData.cs` | Replaced by `List<DialogueAction>` |
| Reward string parsers | `ParseRewardFlags()`, `ParseRewardVariables()` in QuestEditor | Replaced by ActionListEditor widget |
| Legacy DialogueEditor.cs | 738 lines, simple v1 form editor | Superseded by DialogueWorkspace |
| `TryShowPickupDialogue()` | GameplayScreen | Replaced by TriggerManager with `Source = Pickup` |
| `concluded_flag` / `concluded_dialogue` presets | GroupEditor entity presets | Routes handle this |

### What Survives from V1
- `MigrateV1ToV2()` in DialogueFileManager -- keeps working so old project files still load
- `dialogue` property (inline text) -- useful shorthand, not v1 debt
- Migration tests -- verify v1 JSON files still convert correctly

### Test Impact
- ~50 existing tests modified (v1 field references updated to v2 equivalents)
- ~80 new tests: TriggerManager, QuestState, GameWorldView, BarkOverlay, InspectOverlay, CutsceneScreen, HitTestRegistry, workspace switching
- Migration tests preserved and extended (v1 quest rewards format)
- Target: 0 test failures throughout

---

## Implementation Order

Dependency chain determines sequencing:

```
Phase 1 (foundation, parallelizable):
  ├── 1a. IWorkspace + WorkspaceMode + toolbar switching
  ├── 1b. HitTestRegistry + DeferredInputQueue + ConditionListEditor + ActionListEditor
  ├── 1c. QuestConstants + QuestState + quest rewards migration
  └── 1d. GameWorldView + MapView + EntityView + GroupView data classes

Phase 2 (core systems):
  ├── 2a. TriggerManager + TriggerEvent + TriggerResult
  ├── 2b. DialogueWorkspace decomposition (NodeGraphPanel, RouteBar, NodePropertiesPanel)
  └── 2c. QuestWorkspace (full-canvas editor with ActionListEditor for rewards)

Phase 3 (new screens):
  ├── 3a. BarkOverlay
  ├── 3b. InspectOverlay
  └── 3c. CutsceneScreen

Phase 4 (integration):
  ├── 4a. GameplayScreen refactor (use TriggerManager, accept GameWorldView)
  ├── 4b. PlayModeController.BuildWorldView() / ApplyToEditor()
  └── 4c. Cross-references in quest/dialogue editors

Phase 5 (cleanup):
  ├── 5a. Remove v1 fallback code from runtime
  ├── 5b. Remove legacy DialogueEditor.cs
  ├── 5c. Remove old entity presets (concluded_flag, concluded_dialogue)
  └── 5d. Update tests, docs, wiki
```

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| GameWorldView reverse mapping (keep changes) is complex | Runtime entity moves/state must map back to editor | Build ApplyToEditor() with explicit field mapping; test round-trip |
| Dialogue editor decomposition breaks existing workflows | Users lose muscle memory | Keep same keyboard shortcuts; node graph preserves EditorX/EditorY positions |
| Quest reward migration loses data | Old quests.json with comma-separated rewards | Migration test suite; backup file before first save |
| TriggerManager becomes a god class | Scope creep as more trigger sources added | Keep Fire() as pure resolver (trigger in, result out); side effects stay in GameplayScreen |
| Large scope across 7 sections | Risk of partial completion | Phased implementation; each phase is independently testable and mergeable |

## Related Documents

- [[ADRs/002-dialogue-2|ADR-002]] -- Dialogue 2.0 (foundation this builds on)
- [[Architecture]] -- System overview
- [[Status]] -- Current state
- [[Property Reference]] -- Entity property keys
