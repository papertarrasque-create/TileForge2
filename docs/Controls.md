---
updated: 2026-03-06
status: current
---

# Controls Reference

## Editor Mode

### Tools

| Key | Tool |
|-----|------|
| B | Brush |
| E | Eraser |
| F | Fill bucket |
| N | Entity placer |
| I | Picker (eyedropper) |
| M | Selection |

### File Operations

| Key | Action |
|-----|--------|
| Ctrl+S | Save |
| Ctrl+O | Open file |
| Ctrl+Shift+O | Open recent |
| Ctrl+N | New project |

### Edit Operations

| Key | Action |
|-----|--------|
| Ctrl+Z | Undo |
| Ctrl+Y | Redo |
| Ctrl+C | Copy selection |
| Ctrl+V | Paste |
| Delete | Clear selection / remove entity |

### View & Navigation

| Key | Action |
|-----|--------|
| G | Cycle grid (Normal / Fine / Off) |
| V | Toggle layer visibility |
| Tab | Cycle active layer |
| Shift+Up/Down | Reorder layers |
| Ctrl+M | Toggle minimap |
| Ctrl+R | Resize map |
| Ctrl+E | Export (JSON / PNG) |
| Ctrl+W | Open [[World Map Editor]] |
| Middle drag | Pan canvas |
| Scroll | Zoom |

### Play Mode Toggle

| Key | Action |
|-----|--------|
| F5 | Enter play mode |

### Escape Chain

Escape dismisses in priority order:
1. Exit play mode
2. Clear clipboard (stamp brush)
3. Clear selection
4. Deselect entity

### Group Editor

| Key | Action |
|-----|--------|
| S | Toggle solid |
| P | Toggle player |
| T | Toggle tile/entity type |
| Enter | Confirm |
| Escape | Cancel |

### Modal Editors (Quest, Dialogue, World Map)

| Key | Action |
|-----|--------|
| Tab | Cycle focus to next field |
| Enter | Save/confirm |
| Escape | Cancel/close |

## Play Mode

### Movement & Combat

| Key | Default Action |
|-----|----------------|
| Arrow keys | Move player (1 AP) |
| Z | Directional attack / Interact (1 AP for attack, 0 AP for dialogue) |
| Space | End turn (forfeit remaining AP) |

### Screens

| Key | Action |
|-----|--------|
| Escape | Pause menu |
| I | Open inventory |
| Q | Open quest log |
| F5 / Escape (from pause) | Exit play mode |

### Sidebar

| Action | Effect |
|--------|--------|
| Mouse wheel over sidebar | Scroll message log (3 entries/tick) |

### Key Rebinding

All play mode keys are rebindable via Settings screen (accessible from Pause menu). Bindings saved to `~/.tileforge/keybindings.json`.

`GameAction` enum values:
- MoveUp, MoveDown, MoveLeft, MoveRight
- Interact
- Cancel
- Pause
- OpenInventory
- OpenQuestLog
- EndTurn

## Related

- [[Editor Overview]] -- Editor UI layout and input routing
- [[Combat]] -- AP costs for combat actions
- [[File Formats]] -- Key bindings file location
