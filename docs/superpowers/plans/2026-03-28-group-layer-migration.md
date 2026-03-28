# Group Layer Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** When a TileGroup is moved between layers, auto-migrate all placed cells from the old layer to the new layer across all maps, with conflict detection and undo support.

**Architecture:** New `MoveGroupLayerCommand` handles cell migration + undo/redo. MapPanel signals the move request via a `WantsMoveGroupLayer` property. TileForgeGame scans for conflicts, optionally shows a ConfirmDialog, then executes the command.

**Tech Stack:** C# / .NET 9.0, xUnit

---

### Task 1: MoveGroupLayerCommand — Failing Tests

**Files:**
- Create: `TileForge.Tests/Editor/Commands/MoveGroupLayerCommandTests.cs`

- [ ] **Step 1: Write failing tests for MoveGroupLayerCommand**

```csharp
using TileForge.Data;
using TileForge.Editor.Commands;
using Xunit;

namespace TileForge.Tests.Editor.Commands;

public class MoveGroupLayerCommandTests
{
    private static MapData MakeMap(int w, int h, string[] layerNames)
    {
        var map = new MapData(w, h);
        map.Layers.Clear();
        foreach (var name in layerNames)
            map.Layers.Add(new MapLayer(name, w, h));
        return map;
    }

    [Fact]
    public void Execute_MovesCells_FromOldLayerToNewLayer()
    {
        var map = MakeMap(3, 3, new[] { "Ground", "Objects" });
        var group = new TileGroup { Name = "bush", LayerName = "Ground" };
        map.Layers[0].SetCell(1, 1, 3, "bush");
        map.Layers[0].SetCell(2, 0, 3, "bush");

        var cmd = new MoveGroupLayerCommand(group, "Ground", "Objects", new[] { map });
        cmd.Execute();

        Assert.Null(map.Layers[0].GetCell(1, 1, 3));
        Assert.Null(map.Layers[0].GetCell(2, 0, 3));
        Assert.Equal("bush", map.Layers[1].GetCell(1, 1, 3));
        Assert.Equal("bush", map.Layers[1].GetCell(2, 0, 3));
        Assert.Equal("Objects", group.LayerName);
    }

    [Fact]
    public void Undo_RestoresOriginalState()
    {
        var map = MakeMap(3, 3, new[] { "Ground", "Objects" });
        var group = new TileGroup { Name = "bush", LayerName = "Ground" };
        map.Layers[0].SetCell(1, 1, 3, "bush");

        var cmd = new MoveGroupLayerCommand(group, "Ground", "Objects", new[] { map });
        cmd.Execute();
        cmd.Undo();

        Assert.Equal("bush", map.Layers[0].GetCell(1, 1, 3));
        Assert.Null(map.Layers[1].GetCell(1, 1, 3));
        Assert.Equal("Ground", group.LayerName);
    }

    [Fact]
    public void Execute_OverwritesConflicts_OnTargetLayer()
    {
        var map = MakeMap(3, 3, new[] { "Ground", "Objects" });
        var group = new TileGroup { Name = "bush", LayerName = "Ground" };
        map.Layers[0].SetCell(1, 1, 3, "bush");
        map.Layers[1].SetCell(1, 1, 3, "rock"); // conflict

        var cmd = new MoveGroupLayerCommand(group, "Ground", "Objects", new[] { map });
        cmd.Execute();

        Assert.Null(map.Layers[0].GetCell(1, 1, 3));
        Assert.Equal("bush", map.Layers[1].GetCell(1, 1, 3));
    }

    [Fact]
    public void Undo_RestoresOverwrittenConflicts()
    {
        var map = MakeMap(3, 3, new[] { "Ground", "Objects" });
        var group = new TileGroup { Name = "bush", LayerName = "Ground" };
        map.Layers[0].SetCell(1, 1, 3, "bush");
        map.Layers[1].SetCell(1, 1, 3, "rock"); // conflict

        var cmd = new MoveGroupLayerCommand(group, "Ground", "Objects", new[] { map });
        cmd.Execute();
        cmd.Undo();

        Assert.Equal("bush", map.Layers[0].GetCell(1, 1, 3));
        Assert.Equal("rock", map.Layers[1].GetCell(1, 1, 3));
    }

    [Fact]
    public void ConflictCount_ReturnsCorrectCount()
    {
        var map = MakeMap(3, 3, new[] { "Ground", "Objects" });
        var group = new TileGroup { Name = "bush", LayerName = "Ground" };
        map.Layers[0].SetCell(0, 0, 3, "bush");
        map.Layers[0].SetCell(1, 1, 3, "bush");
        map.Layers[1].SetCell(1, 1, 3, "rock"); // one conflict

        var cmd = new MoveGroupLayerCommand(group, "Ground", "Objects", new[] { map });

        Assert.Equal(1, cmd.ConflictCount);
    }

    [Fact]
    public void SkipsMap_WhenTargetLayerMissing()
    {
        var map = MakeMap(3, 3, new[] { "Ground" }); // no "Objects" layer
        var group = new TileGroup { Name = "bush", LayerName = "Ground" };
        map.Layers[0].SetCell(1, 1, 3, "bush");

        var cmd = new MoveGroupLayerCommand(group, "Ground", "Objects", new[] { map });
        cmd.Execute();

        // Cell stays on Ground because Objects layer doesn't exist
        Assert.Equal("bush", map.Layers[0].GetCell(1, 1, 3));
    }

    [Fact]
    public void WorksAcrossMultipleMaps()
    {
        var map1 = MakeMap(2, 2, new[] { "Ground", "Objects" });
        var map2 = MakeMap(2, 2, new[] { "Ground", "Objects" });
        var group = new TileGroup { Name = "bush", LayerName = "Ground" };
        map1.Layers[0].SetCell(0, 0, 2, "bush");
        map2.Layers[0].SetCell(1, 1, 2, "bush");

        var cmd = new MoveGroupLayerCommand(group, "Ground", "Objects", new[] { map1, map2 });
        cmd.Execute();

        Assert.Null(map1.Layers[0].GetCell(0, 0, 2));
        Assert.Equal("bush", map1.Layers[1].GetCell(0, 0, 2));
        Assert.Null(map2.Layers[0].GetCell(1, 1, 2));
        Assert.Equal("bush", map2.Layers[1].GetCell(1, 1, 2));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~MoveGroupLayerCommandTests" --no-build 2>&1 | head -5`
Expected: Build error — `MoveGroupLayerCommand` does not exist yet.

- [ ] **Step 3: Commit**

```bash
git add TileForge.Tests/Editor/Commands/MoveGroupLayerCommandTests.cs
git commit -m "test: add failing tests for MoveGroupLayerCommand"
```

---

### Task 2: MoveGroupLayerCommand — Implementation

**Files:**
- Create: `TileForge/Editor/Commands/MoveGroupLayerCommand.cs`

- [ ] **Step 1: Implement MoveGroupLayerCommand**

```csharp
using System.Collections.Generic;
using TileForge.Data;

namespace TileForge.Editor.Commands;

public class MoveGroupLayerCommand : ICommand
{
    private readonly TileGroup _group;
    private readonly string _oldLayerName;
    private readonly string _newLayerName;

    // Per-map: list of (cellIndex, oldValueOnNewLayer) for cells that were migrated
    private readonly List<(MapData Map, List<(int Index, string OldValueOnNewLayer)> Migrations)> _allMigrations;

    public int ConflictCount { get; }

    public MoveGroupLayerCommand(TileGroup group, string oldLayerName, string newLayerName, IEnumerable<MapData> maps)
    {
        _group = group;
        _oldLayerName = oldLayerName;
        _newLayerName = newLayerName;
        _allMigrations = new();

        int conflicts = 0;

        foreach (var map in maps)
        {
            var oldLayer = map.GetLayer(oldLayerName);
            var newLayer = map.GetLayer(newLayerName);
            if (oldLayer == null || newLayer == null) continue;

            var migrations = new List<(int Index, string OldValueOnNewLayer)>();
            for (int i = 0; i < oldLayer.Cells.Length; i++)
            {
                if (oldLayer.Cells[i] == group.Name)
                {
                    string existing = newLayer.Cells[i];
                    if (existing != null) conflicts++;
                    migrations.Add((i, existing));
                }
            }

            if (migrations.Count > 0)
                _allMigrations.Add((map, migrations));
        }

        ConflictCount = conflicts;
    }

    public void Execute()
    {
        foreach (var (map, migrations) in _allMigrations)
        {
            var oldLayer = map.GetLayer(_oldLayerName);
            var newLayer = map.GetLayer(_newLayerName);
            if (oldLayer == null || newLayer == null) continue;

            foreach (var (index, _) in migrations)
            {
                oldLayer.Cells[index] = null;
                newLayer.Cells[index] = _group.Name;
            }
        }

        _group.LayerName = _newLayerName;
    }

    public void Undo()
    {
        foreach (var (map, migrations) in _allMigrations)
        {
            var oldLayer = map.GetLayer(_oldLayerName);
            var newLayer = map.GetLayer(_newLayerName);
            if (oldLayer == null || newLayer == null) continue;

            foreach (var (index, oldValueOnNewLayer) in migrations)
            {
                oldLayer.Cells[index] = _group.Name;
                newLayer.Cells[index] = oldValueOnNewLayer;
            }
        }

        _group.LayerName = _oldLayerName;
    }
}
```

- [ ] **Step 2: Run tests to verify they pass**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~MoveGroupLayerCommandTests"`
Expected: All 7 tests PASS.

- [ ] **Step 3: Commit**

```bash
git add TileForge/Editor/Commands/MoveGroupLayerCommand.cs
git commit -m "feat: add MoveGroupLayerCommand for cell migration on group layer move"
```

---

### Task 3: MapPanel — WantsMoveGroupLayer Signal

**Files:**
- Modify: `TileForge/UI/MapPanel.cs`

- [ ] **Step 1: Add WantsMoveGroupLayer property**

After line 99 (`public (int fromIndex, int toIndex)? PendingLayerReorder { get; private set; }`), add:

```csharp
public (string GroupName, string TargetLayerName)? WantsMoveGroupLayer { get; private set; }
```

- [ ] **Step 2: Clear it each frame in BeginUpdate/UpdateContent**

In the `UpdateContent` method, after line 190 (`PendingLayerReorder = null;`), add:

```csharp
WantsMoveGroupLayer = null;
```

- [ ] **Step 3: Replace direct LayerName assignment with signal**

Replace the group drag handler (lines 228-246):

```csharp
// Handle ongoing group drag
if (_isDraggingGroup)
{
    if (leftReleased)
    {
        string targetLayer = GetGroupDragTargetLayer(mouse.Y);
        if (targetLayer != null && _dragGroupName != null
            && state.GroupsByName.TryGetValue(_dragGroupName, out var group)
            && group.LayerName != targetLayer)
        {
            group.LayerName = targetLayer;
            state.ActiveLayerName = targetLayer;
        }
        _isDraggingGroup = false;
        _dragGroupName = null;
        _mouseDownGroupName = null;
    }
    return;
}
```

With:

```csharp
// Handle ongoing group drag
if (_isDraggingGroup)
{
    if (leftReleased)
    {
        string targetLayer = GetGroupDragTargetLayer(mouse.Y);
        if (targetLayer != null && _dragGroupName != null
            && state.GroupsByName.TryGetValue(_dragGroupName, out _)
            && state.GroupsByName[_dragGroupName].LayerName != targetLayer)
        {
            WantsMoveGroupLayer = (_dragGroupName, targetLayer);
        }
        _isDraggingGroup = false;
        _dragGroupName = null;
        _mouseDownGroupName = null;
    }
    return;
}
```

- [ ] **Step 4: Build to verify no errors**

Run: `dotnet build TileForge`
Expected: Build succeeds.

- [ ] **Step 5: Commit**

```bash
git add TileForge/UI/MapPanel.cs
git commit -m "refactor: MapPanel signals group layer move instead of applying directly"
```

---

### Task 4: TileForgeGame — Handle WantsMoveGroupLayer

**Files:**
- Modify: `TileForge/TileForgeGame.cs`

- [ ] **Step 1: Add using directive if not present**

Ensure this using is at the top of the file:

```csharp
using TileForge.Editor.Commands;
```

- [ ] **Step 2: Add handler after PendingLayerReorder block**

After the `PendingLayerReorder` handling block (around line 615), add:

```csharp
if (_mapPanel.WantsMoveGroupLayer.HasValue)
{
    var (groupName, targetLayerName) = _mapPanel.WantsMoveGroupLayer.Value;
    if (_state.GroupsByName.TryGetValue(groupName, out var group))
    {
        var allMaps = new List<MapData>();
        foreach (var doc in _state.MapDocuments)
            if (doc.Map != null)
                allMaps.Add(doc.Map);

        var cmd = new MoveGroupLayerCommand(group, group.LayerName, targetLayerName, allMaps);

        if (cmd.ConflictCount > 0)
        {
            _dialogManager.Show(
                new ConfirmDialog($"Moving \"{groupName}\" to \"{targetLayerName}\" will overwrite {cmd.ConflictCount} tile(s). Continue?"),
                dialog =>
                {
                    if (!dialog.WasCancelled)
                    {
                        cmd.Execute();
                        _state.UndoStack.Push(cmd);
                        _state.ActiveLayerName = targetLayerName;
                    }
                });
        }
        else
        {
            cmd.Execute();
            _state.UndoStack.Push(cmd);
            _state.ActiveLayerName = targetLayerName;
        }
    }
}
```

- [ ] **Step 3: Build to verify no errors**

Run: `dotnet build TileForge`
Expected: Build succeeds.

- [ ] **Step 4: Run full test suite**

Run: `dotnet test TileForge.Tests`
Expected: All tests pass (including the 7 new MoveGroupLayerCommand tests).

- [ ] **Step 5: Commit**

```bash
git add TileForge/TileForgeGame.cs
git commit -m "feat: wire group layer move with conflict dialog and undo support"
```

---

### Task 5: Manual Verification

- [ ] **Step 1: Launch TileForge and test the happy path**

1. Create a group "test_tile" on "Ground" layer
2. Paint several tiles on the map
3. Drag "test_tile" from "Ground" to "Objects" in the layer panel
4. Verify: painted tiles now appear on "Objects" layer (switch layers and use eraser/picker to confirm)
5. Press Ctrl+Z — verify tiles return to "Ground" layer

- [ ] **Step 2: Test conflict path**

1. Create "tile_a" on "Ground", paint at (0,0)
2. Create "tile_b" on "Objects", paint at (0,0)
3. Drag "tile_a" from "Ground" to "Objects"
4. Verify: dialog appears warning about 1 overwrite
5. Click Cancel — verify nothing changed
6. Drag again, click OK — verify "tile_a" overwrites "tile_b" at (0,0) on Objects
7. Ctrl+Z — verify both tiles restored to original layers

- [ ] **Step 3: Test skip-missing-layer case**

1. Open a multi-map project
2. Ensure one map has both "Ground" and "Objects", another has only "Ground"
3. Move a group from "Ground" to "Objects"
4. Verify: cells migrated on the map that has both layers; unchanged on the map missing "Objects"
