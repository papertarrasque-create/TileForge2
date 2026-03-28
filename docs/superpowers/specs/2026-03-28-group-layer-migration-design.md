# Auto-Migrate Cells on Group Layer Move

**Date:** 2026-03-28
**Status:** Draft

## Problem

When a TileGroup is moved between layers (e.g., dragging "bush" from "Objects" to "Ground" in MapPanel), only the group's `LayerName` metadata is updated. Already-painted cells remain in the old layer's `Cells[]` array. This causes:

- Tiles render at the old layer's depth, not the new one
- Eraser/picker only work on the active layer, so orphaned tiles are hard to manage
- New paintings go to the new layer while old tiles stay on the old layer
- Reorganizing layers becomes painful as placed content gets left behind

## Solution

When a group is moved between layers, automatically migrate all cells referencing that group from the old layer to the new layer, across all maps in the project.

### Conflict Handling

If the target layer already has a tile at a position where the migrating group has a cell, show a `ConfirmDialog`:

> *"Moving [group] to [layer] will overwrite [N] tile(s) on [M] map(s). OK / Cancel"*

- **OK:** Proceed with migration; overwritten tiles are lost (but undoable)
- **Cancel:** Abort the entire move; nothing changes

If there are no conflicts, the move happens silently (no dialog).

### Undo/Redo

A new `MoveGroupLayerCommand : ICommand` captures the full operation:

- The group name, old layer name, new layer name
- Per-map list of `CellChange` records (using the existing `CellChange` struct) covering:
  - Cells moved from old layer (old = groupName, new = null on old layer)
  - Cells placed on new layer (old = whatever was there, new = groupName)
- `Execute()` applies all cell changes and sets `group.LayerName`
- `Undo()` reverses all cell changes and restores `group.LayerName`

This reuses the existing `CellChange` record struct from `CellStrokeCommand.cs`.

### Scope of Changes

| File | Change |
|------|--------|
| `TileForge/Editor/Commands/MoveGroupLayerCommand.cs` | **New.** ICommand for the migration + undo/redo |
| `TileForge/UI/MapPanel.cs` | Replace inline `group.LayerName = targetLayer` with conflict-check + deferred request (`WantsMoveGroupLayer`) |
| `TileForge/TileForgeGame.cs` | Handle `WantsMoveGroupLayer`: scan for conflicts, show ConfirmDialog if needed, execute command via UndoStack |

### MapPanel Changes

The drag-drop handler (lines 228-246) currently sets `group.LayerName` directly. Instead:

1. Set a `WantsMoveGroupLayer` property containing `(groupName, targetLayerName)`
2. Clear it each frame in `BeginUpdate()` (same pattern as `WantsDeleteGroup`)

### TileForgeGame Changes

In the existing `MapPanel.WantsX` handling block:

1. Read `WantsMoveGroupLayer`
2. Scan all maps: count cells where the target layer already has a non-null value at positions where the old layer has the moving group
3. If conflicts > 0: show `ConfirmDialog` with count; on OK execute the command
4. If no conflicts: execute the command directly
5. Command is pushed to `UndoStack` for undo/redo support

### MoveGroupLayerCommand

```
Constructor(group, oldLayerName, newLayerName, allMaps):
  For each map that has BOTH old and new layers:
    For each cell in old layer matching group name:
      Add CellChange on old layer (groupName -> null)
      Add CellChange on new layer (existing value -> groupName)
  Store all changes per-map

Execute():
  Apply all pre-computed CellChanges
  Set group.LayerName = _newLayerName

Undo():
  Reverse all CellChanges
  Set group.LayerName = _oldLayerName

ConflictCount:
  Number of CellChanges on new layer where existing value was non-null
```

Changes are pre-computed during construction so the conflict count is available before execution (for the dialog). Maps that lack the target layer are skipped — their cells remain on the old layer.

### What Does NOT Change

- Painting, erasing, picking, fill — all continue to use active layer
- Rendering — still iterates layers in order
- Group rename/delete — already handles cross-layer cleanup
- Serialization format — no changes
- Entity references — entities use `GroupName`, not layer; unaffected
