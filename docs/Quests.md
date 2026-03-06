---
updated: 2026-03-06
status: current
---

# Quest System

Quests are data-driven definitions stored in `quests.json`. The `QuestManager` evaluates quest state by **polling** flags and variables -- there is no event bus.

## Quest Structure

```
QuestDefinition
  Id: string               -- Unique identifier
  Name: string             -- Display name
  Description: string      -- Quest description text
  StartFlag: string        -- Quest activates when this flag exists
  Objectives: list         -- List of QuestObjective
  CompletionFlag: string   -- Auto-set when all objectives met
  Rewards: QuestRewards    -- Flags/variables applied on completion
```

## Quest States

| State | Condition |
|-------|-----------|
| **NotStarted** | `StartFlag` not set in GameState |
| **Active** | `StartFlag` is set, not all objectives complete |
| **Completed** | `CompletionFlag` is set |

## Objective Types

| Type | Evaluation | Example |
|------|-----------|---------|
| `flag` | `HasFlag(objective.Flag)` | "Enter the cave" -> flag `visited_map:cave` |
| `variable_gte` | `GetVariable(key) >= value` | "Kill 3 goblins" -> `goblin_kills >= 3` |
| `variable_eq` | `GetVariable(key) == value` | "Collect exactly 5 gems" -> `gems == 5` |

Missing variables parse as 0. Non-numeric values parse as 0.

## Evaluation Flow

`QuestManager.CheckForUpdates(gsm)` is called after state-changing actions:

1. Iterate all quest definitions
2. Skip completed quests (CompletionFlag already set)
3. Check start condition (StartFlag must be set)
4. Evaluate each objective independently
5. Report new objective completions (tracked in `_reportedObjectives` set to prevent duplicates)
6. When all objectives met:
   - Set CompletionFlag
   - Apply rewards (SetFlags and SetVariables)
   - Generate QuestCompleted event

**Session-only tracking:** `_reportedStarts` and `_reportedObjectives` prevent duplicate notifications but are not serialized -- rebuilt from flags on load.

## Entity Hooks

[[Entities]] can modify quest state through property bag hooks:

| Property | When | Effect |
|----------|------|--------|
| `on_kill_set_flag` | Entity killed | Sets the named flag |
| `on_kill_increment` | Entity killed | Increments the named variable by 1 |
| `on_collect_set_flag` | Item collected | Sets the named flag |
| `on_collect_increment` | Item collected | Increments the named variable by 1 |

These are processed inline in `GameStateManager.AttackEntity()` and `CollectItem()`.

## Rewards

```
QuestRewards
  SetFlags: List<string>                    -- Flags to set on completion
  SetVariables: Dict<string, string>        -- Variables to assign
```

Rewards are applied atomically when all objectives are met. Reward flags can trigger other quests' StartFlag conditions, creating quest chains.

## Quest Events

`QuestEventType` enum:
- **QuestStarted** -- Displayed when StartFlag first detected
- **ObjectiveCompleted** -- Per-objective notification
- **QuestCompleted** -- All objectives met, rewards applied

Events are displayed as floating messages (Cyan) and logged to the [[Sidebar HUD]] GameLog.

## Quest JSON Format

Stored in `{projectDir}/quests.json`. See [[File Formats]] for the full spec.

```json
{
  "quests": [
    {
      "id": "cave_investigation",
      "name": "Investigate the Caves",
      "description": "The Elder asked you to investigate.",
      "start_flag": "quest_caves_accepted",
      "objectives": [
        { "description": "Enter the cave", "type": "flag", "flag": "visited_map:cave" },
        { "description": "Defeat 3 creatures", "type": "variable_gte",
          "variable": "cave_kills", "value": 3 }
      ],
      "completion_flag": "quest_complete:cave_investigation",
      "rewards": {
        "set_flags": ["caves_cleared"],
        "set_variables": { "reputation": "5" }
      }
    }
  ]
}
```

Supports both snake_case and PascalCase via `NormalizedQuestFileConverter`.

## Authoring

Quests are authored in the [[Quest Editor]] (modal overlay). The [[Group Editor]] exposes entity hook properties (`on_kill_set_flag`, etc.) for linking entities to quest objectives.

## Design Notes

- **No turn-in NPCs.** "Return to NPC" is modeled as a flag objective set by [[Dialogue]].
- **No event bus.** Polling is simple, deterministic, and easy to test.
- **Auto-completion.** When objectives are met, rewards fire immediately.
- **All quests evaluated every check.** No incremental evaluation -- works fine at current scale.

## Related

- [[Quest Editor]] -- UI for authoring quests
- [[Entities]] -- Entity hooks that drive quest progress
- [[Dialogue]] -- Setting flags from dialogue choices
- [[File Formats]] -- Quest JSON spec
- [[Property Reference]] -- Entity hook properties
