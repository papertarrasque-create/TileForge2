---
updated: 2026-03-06
status: current
---

# Map Tab Bar

Horizontal tab strip for multimap project navigation. 22px tall, sits below the ToolbarRibbon.

## Features

- Tab width: `Clamp(textWidth + 28, MinWidth, MaxWidth)`
- Tab text truncated if needed
- Close button (X, 14x14) on hover/active tab
- Add button (+) at right end
- Active tab indicator: bottom line (blue accent)
- Scroll if tabs exceed available width

## Interaction

| Action | Effect |
|--------|--------|
| Left-click tab | Switch to map |
| Double-click tab | Enter rename mode |
| Right-click tab | Context menu |
| Click X (close) | Close tab (confirms if >1 map) |
| Click + | Create new map |

## Context Menu

- **Rename** -- Edit map name inline
- **Duplicate** -- Copy map with new name
- **Delete** -- Remove map (confirms, requires >1 map)

## Signals

| Signal | Trigger |
|--------|---------|
| `WantsSelectTab` | Tab clicked |
| `WantsNewMap` | + button clicked |
| `WantsCloseTab` | X button clicked |
| `WantsRenameTab` | Double-click or Rename menu |
| `WantsDuplicateTab` | Duplicate menu |

## Data Model

Each tab corresponds to a `MapDocumentState` in `EditorState.MapDocuments`. Switching tabs changes `ActiveMapIndex`, which rewires the undo stack and fires `ActiveMapChanged`.

## Related

- [[Maps]] -- Map data model
- [[Editor Overview]] -- Where MapTabBar fits in the UI
