---
updated: 2026-03-06
status: current
---

# File Formats

All serialization uses `System.Text.Json`. Files are human-readable JSON.

## Project File (.tileforge)

### V2 Structure (Current)

```json
{
  "version": 2,
  "spritesheet": {
    "path": "sprites.png",
    "tileWidth": 16,
    "tileHeight": 16,
    "padding": 0
  },
  "groups": [
    {
      "name": "Wall",
      "type": "Tile",
      "sprites": [{ "col": 0, "row": 1 }],
      "isSolid": true,
      "layer": "Ground"
    },
    {
      "name": "Goblin",
      "type": "Entity",
      "sprites": [{ "col": 5, "row": 3 }],
      "isSolid": true,
      "entityType": "NPC",
      "layer": "Objects",
      "defaultProperties": {
        "health": "10",
        "attack": "3",
        "defense": "1",
        "behavior": "chase",
        "aggro_range": "5"
      }
    }
  ],
  "maps": [
    {
      "name": "MainMap",
      "map": {
        "width": 20,
        "height": 15,
        "entityRenderOrder": 0,
        "layers": [
          {
            "name": "Ground",
            "visible": true,
            "cells": ["Grass", "Grass", "Wall", ...]
          }
        ]
      },
      "entities": [
        {
          "id": "goblin_01",
          "groupName": "Goblin",
          "x": 10,
          "y": 5,
          "properties": {}
        }
      ]
    }
  ],
  "editorState": {
    "activeLayer": "Ground",
    "activeMapName": "MainMap",
    "cameraX": 0,
    "cameraY": 0,
    "zoomIndex": 1
  },
  "worldLayout": {
    "maps": {
      "MainMap": { "gridX": 0, "gridY": 0 },
      "Cave": { "gridX": 1, "gridY": 0 }
    }
  }
}
```

### V1 Compatibility

V1 projects use single `map` and `entities` fields instead of `maps` array. Auto-upgraded to V2 on load. Legacy `.tileforge2` files also supported for loading.

### Key Fields

| Field | Description |
|-------|-------------|
| `version` | 1 or 2 |
| `spritesheet` | Path (relative to project file), tile dimensions, padding |
| `groups` | Shared tile/entity definitions (see [[Property Reference]]) |
| `maps` | Per-map data (V2), or `map` + `entities` (V1) |
| `editorState` | Camera, zoom, active layer, panel state |
| `worldLayout` | Grid-based map adjacency (null if unconfigured) |

### Cell Arrays

Layer cells are stored as a linear string array indexed as `x + y * width`. Values are group names or null (empty).

## Dialogue JSON

Stored as `{projectDir}/dialogues/{id}.json`. Uses **camelCase** naming.

```json
{
  "id": "elder_01",
  "nodes": [
    {
      "id": "start",
      "speaker": "Village Elder",
      "text": "Welcome, traveler.",
      "choices": [
        {
          "text": "What happened?",
          "nextNodeId": "explain"
        },
        {
          "text": "Just passing through.",
          "nextNodeId": "decline"
        }
      ],
      "editorX": 56,
      "editorY": -174
    },
    {
      "id": "explain",
      "speaker": "Village Elder",
      "text": "Strange creatures invaded.",
      "nextNodeId": null,
      "setsFlag": "elder_explained",
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

See [[Dialogue]] for the full node/choice structure.

## Quest JSON

Stored as `{projectDir}/quests.json`. Uses **snake_case** naming (but `NormalizedQuestFileConverter` also accepts PascalCase).

```json
{
  "quests": [
    {
      "id": "cave_investigation",
      "name": "Investigate the Caves",
      "description": "The Elder asked you to check the caves.",
      "start_flag": "quest_caves_accepted",
      "objectives": [
        {
          "description": "Enter the cave",
          "type": "flag",
          "flag": "visited_map:cave"
        },
        {
          "description": "Defeat 3 creatures",
          "type": "variable_gte",
          "variable": "cave_kills",
          "value": 3
        }
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

See [[Quests]] for objective types and evaluation.

## Save File

Stored at `~/.tileforge/saves/{slotName}.json`.

```json
{
  "version": 2,
  "player": {
    "x": 10, "y": 5,
    "facing": "Down",
    "health": 80, "maxHealth": 100,
    "attack": 5, "defense": 2,
    "maxAP": 2,
    "poise": 15, "maxPoise": 20,
    "inventory": ["Health Potion", "Torch"],
    "equipment": { "Weapon": "Iron Sword", "Armor": "Leather Armor" },
    "activeEffects": []
  },
  "currentMapId": "MainMap",
  "activeEntities": [],
  "flags": ["quest_caves_accepted", "entity_inactive:goblin_01"],
  "variables": { "cave_kills": "2" },
  "itemPropertyCache": {
    "Iron Sword": { "equip_slot": "Weapon", "equip_attack": "3" },
    "Leather Armor": { "equip_slot": "Armor", "equip_defense": "2" }
  }
}
```

See [[Save System]] for what gets serialized and version handling.

## World Layout

Embedded in the project file under `worldLayout`. See [[Maps]] and [[World Map Editor]].

```json
{
  "maps": {
    "Town": {
      "gridX": 0, "gridY": 0,
      "northEntry": { "x": 10, "y": 14 },
      "southExit": { "x": 5, "y": 0 }
    },
    "Cave": {
      "gridX": 1, "gridY": 0,
      "westEntry": { "x": 0, "y": 7 }
    }
  }
}
```

## Key Bindings

Stored at `~/.tileforge/keybindings.json`. Maps `GameAction` enum values to keyboard keys. Editable via SettingsScreen in play mode.

## File Locations Summary

| File | Location |
|------|----------|
| Project | `{anywhere}/{name}.tileforge` |
| Dialogues | `{projectDir}/dialogues/{id}.json` |
| Quests | `{projectDir}/quests.json` |
| Saves | `~/.tileforge/saves/{slot}.json` |
| Key bindings | `~/.tileforge/keybindings.json` |
| Recent files | `~/.tileforge/recent.json` |
| Auto-saves | `{projectPath}.autosave` (sidecar) |

## Naming Convention Inconsistency

Quests use **snake_case** JSON, dialogues use **camelCase** JSON. This is a known inconsistency documented in [[ADRs/001-initial-architecture|ADR-001]].

## Related

- [[Save System]] -- Save/load details
- [[Dialogue]] -- Dialogue data structures
- [[Quests]] -- Quest data structures
- [[Maps]] -- Map data and world layout
- [[Property Reference]] -- Entity property keys
