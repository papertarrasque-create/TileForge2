# Phase 1: Foundation Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the foundation layer that all subsequent phases depend on: workspace mode infrastructure, reusable UI widgets (HitTestRegistry, ConditionListEditor, ActionListEditor), QuestConstants + QuestState + quest reward migration, and GameWorldView data classes.

**Architecture:** Four independent work streams that can run in parallel. Each produces tested, committed code with no cross-dependencies. Phase 2 (TriggerManager, DialogueWorkspace, QuestWorkspace) builds on these foundations.

**Tech Stack:** C# / .NET 9.0 / MonoGame 3.8 / xUnit / System.Text.Json

**Spec:** `docs/superpowers/specs/2026-03-15-editor-dialogue-quest-redesign.md`

---

## File Structure

### Task 1: Workspace Infrastructure
- Create: `TileForge/UI/IWorkspace.cs` — interface definition
- Create: `TileForge/UI/WorkspaceMode.cs` — enum
- Modify: `TileForge/Editor/EditorState.cs` — add ActiveWorkspace property + event
- Modify: `TileForge/LayoutConstants.cs` — add workspace toolbar button constants
- Create: `TileForge.Tests/UI/WorkspaceTests.cs` — workspace switching tests

### Task 2: Reusable UI Widgets
- Create: `TileForge/UI/HitTestRegistry.cs` — unified hit-test zone management
- Create: `TileForge/UI/DeferredInputQueue.cs` — deferred dropdown/combobox updates
- Create: `TileForge/UI/ConditionListEditor.cs` — reusable condition list widget
- Create: `TileForge/UI/ActionListEditor.cs` — reusable action list widget
- Create: `TileForge.Tests/UI/HitTestRegistryTests.cs`
- Create: `TileForge.Tests/UI/ConditionListEditorTests.cs`
- Create: `TileForge.Tests/UI/ActionListEditorTests.cs`

### Task 3: Quest Constants, QuestState, Reward Migration
- Create: `TileForge/Game/QuestConstants.cs` — centralized flag prefix constants
- Create: `TileForge/Game/QuestState.cs` — persistent quest progress tracking
- Modify: `TileForge/Game/QuestData.cs` — rewards become `List<DialogueAction>`, computed flag properties
- Modify: `TileForge/Game/QuestManager.cs` — use QuestState dict instead of HashSets, use QuestConstants
- Modify: `TileForge/Game/DialogueRuntime.cs` — use QuestConstants instead of hardcoded prefixes
- Modify: `TileForge/Data/QuestFileManager.cs` — migration for old reward format
- Modify: `TileForge/Game/GameState.cs` — add QuestStates serialization
- Create: `TileForge.Tests/Game/QuestConstantsTests.cs`
- Create: `TileForge.Tests/Game/QuestStateTests.cs`
- Modify: `TileForge.Tests/Game/QuestManagerTests.cs` — update for new types

### Task 4: GameWorldView Data Classes
- Create: `TileForge/Game/GameWorldView.cs` — DTO classes (GameWorldView, MapView, LayerView, EntityView, GroupView)
- Create: `TileForge.Tests/Game/GameWorldViewTests.cs`

---

## Chunk 1: Workspace Infrastructure + GameWorldView Data Classes

### Task 1: Workspace Infrastructure

#### Files:
- Create: `TileForge/UI/IWorkspace.cs`
- Create: `TileForge/UI/WorkspaceMode.cs`
- Modify: `TileForge/Editor/EditorState.cs`
- Modify: `TileForge/LayoutConstants.cs`
- Create: `TileForge.Tests/UI/WorkspaceTests.cs`

- [ ] **Step 1: Write WorkspaceMode enum**

Create `TileForge/UI/WorkspaceMode.cs`:

```csharp
namespace TileForge.UI;

public enum WorkspaceMode
{
    Map,
    Dialogues,
    Quests,
    WorldMap,
}
```

- [ ] **Step 2: Write IWorkspace interface**

Create `TileForge/UI/IWorkspace.cs`:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Editor;

namespace TileForge.UI;

public interface IWorkspace
{
    void Update(EditorState state, MouseState mouse, MouseState prevMouse,
                InputEvent input, SpriteFont font, Rectangle canvasBounds,
                GameTime gameTime, int screenW, int screenH);
    void Draw(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
              Renderer renderer, Rectangle canvasBounds);
    void DrawSidebar(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                     Renderer renderer, Rectangle sidebarBounds);
    void OnEnter(EditorState state);
    void OnExit(EditorState state);
}
```

- [ ] **Step 3: Write tests for EditorState workspace switching**

Create `TileForge.Tests/UI/WorkspaceTests.cs`:

```csharp
using TileForge.Editor;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class WorkspaceTests
{
    [Fact]
    public void ActiveWorkspace_DefaultsToMap()
    {
        var state = new EditorState();
        Assert.Equal(WorkspaceMode.Map, state.ActiveWorkspace);
    }

    [Fact]
    public void ActiveWorkspace_RaisesEvent_OnChange()
    {
        var state = new EditorState();
        WorkspaceMode? received = null;
        state.WorkspaceChanged += mode => received = mode;

        state.ActiveWorkspace = WorkspaceMode.Dialogues;

        Assert.Equal(WorkspaceMode.Dialogues, received);
    }

    [Fact]
    public void ActiveWorkspace_DoesNotRaiseEvent_WhenSameValue()
    {
        var state = new EditorState();
        int callCount = 0;
        state.WorkspaceChanged += _ => callCount++;

        state.ActiveWorkspace = WorkspaceMode.Map; // same as default

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void ActiveWorkspace_SwitchToQuests_ThenBack()
    {
        var state = new EditorState();
        state.ActiveWorkspace = WorkspaceMode.Quests;
        Assert.Equal(WorkspaceMode.Quests, state.ActiveWorkspace);

        state.ActiveWorkspace = WorkspaceMode.Map;
        Assert.Equal(WorkspaceMode.Map, state.ActiveWorkspace);
    }
}
```

- [ ] **Step 4: Run tests to verify they fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~WorkspaceTests" -v minimal`
Expected: FAIL — `EditorState` does not have `ActiveWorkspace` or `WorkspaceChanged` yet.

- [ ] **Step 5: Add ActiveWorkspace to EditorState**

Modify `TileForge/Editor/EditorState.cs`. Add after the existing `DialoguesChanged` event (around line 218):

```csharp
// Workspace
private WorkspaceMode _activeWorkspace = WorkspaceMode.Map;
public event Action<WorkspaceMode> WorkspaceChanged;

public WorkspaceMode ActiveWorkspace
{
    get => _activeWorkspace;
    set
    {
        if (_activeWorkspace == value) return;
        _activeWorkspace = value;
        WorkspaceChanged?.Invoke(_activeWorkspace);
    }
}
```

Add `using TileForge.UI;` to the top of the file.

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~WorkspaceTests" -v minimal`
Expected: All 4 PASS.

- [ ] **Step 7: Add workspace toolbar constants to LayoutConstants**

Modify `TileForge/LayoutConstants.cs`. Add a new section after the Toolbar Ribbon section (around line 48):

```csharp
// -- Workspace Toolbar Buttons --
public const int WorkspaceButtonWidth = 90;
public const int WorkspaceButtonHeight = 22;
public const int WorkspaceButtonSpacing = 4;
public static readonly Color WorkspaceButtonColor = new Color(58, 58, 94);
public static readonly Color WorkspaceButtonActiveColor = new Color(74, 74, 126);
public static readonly Color WorkspaceButtonActiveBorder = new Color(138, 170, 255);
public static readonly Color WorkspaceDialogueColor = new Color(138, 170, 255);
public static readonly Color WorkspaceQuestColor = new Color(200, 132, 64);
public static readonly Color WorkspaceWorldMapColor = new Color(106, 170, 106);
```

- [ ] **Step 8: Run full test suite to verify no regressions**

Run: `dotnet test TileForge.Tests -v minimal`
Expected: All 1601+ tests PASS.

- [ ] **Step 9: Commit**

```bash
git add TileForge/UI/IWorkspace.cs TileForge/UI/WorkspaceMode.cs \
       TileForge/Editor/EditorState.cs TileForge/LayoutConstants.cs \
       TileForge.Tests/UI/WorkspaceTests.cs
git commit -m "feat: add workspace mode infrastructure (IWorkspace, WorkspaceMode enum, EditorState.ActiveWorkspace)"
```

---

### Task 4: GameWorldView Data Classes

#### Files:
- Create: `TileForge/Game/GameWorldView.cs`
- Create: `TileForge.Tests/Game/GameWorldViewTests.cs`

- [ ] **Step 1: Write tests for GameWorldView data classes**

Create `TileForge.Tests/Game/GameWorldViewTests.cs`:

```csharp
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class GameWorldViewTests
{
    [Fact]
    public void GameWorldView_CanBeConstructed_WithEmptyCollections()
    {
        var view = new GameWorldView
        {
            Maps = new(),
            ActiveMapId = "town",
            Groups = new(),
            Dialogues = new(),
            Quests = new(),
        };

        Assert.Equal("town", view.ActiveMapId);
        Assert.Empty(view.Maps);
    }

    [Fact]
    public void MapView_ContainsLayersAndDimensions()
    {
        var map = new MapView
        {
            Width = 20,
            Height = 15,
            Layers = new()
            {
                new LayerView
                {
                    Tiles = new int[15, 20],
                    Entities = new()
                    {
                        new EntityView
                        {
                            Id = "npc_1",
                            GroupName = "elder",
                            X = 5,
                            Y = 3,
                            Properties = new() { ["dialogue_id"] = "elder_01" }
                        }
                    }
                }
            }
        };

        Assert.Equal(20, map.Width);
        Assert.Single(map.Layers);
        Assert.Single(map.Layers[0].Entities);
        Assert.Equal("npc_1", map.Layers[0].Entities[0].Id);
    }

    [Fact]
    public void GroupView_StoresNameTypeAndProperties()
    {
        var group = new GroupView
        {
            Name = "npc_elder",
            Type = GroupType.Entity,
            SpriteIndex = 42,
            Properties = new() { ["behavior"] = "idle" }
        };

        Assert.Equal("npc_elder", group.Name);
        Assert.Equal(GroupType.Entity, group.Type);
        Assert.Equal(42, group.SpriteIndex);
        Assert.Equal("idle", group.Properties["behavior"]);
    }

    [Fact]
    public void EntityView_FullRoundTrip()
    {
        var entity = new EntityView
        {
            Id = "trigger_1",
            GroupName = "cave_entrance",
            X = 10,
            Y = 7,
            Properties = new()
            {
                ["dialogue_id"] = "cave_warning",
                ["target_map"] = "cave_level1",
            }
        };

        Assert.Equal("trigger_1", entity.Id);
        Assert.Equal(10, entity.X);
        Assert.Equal(2, entity.Properties.Count);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~GameWorldViewTests" -v minimal`
Expected: FAIL — classes don't exist yet.

- [ ] **Step 3: Write GameWorldView data classes**

Create `TileForge/Game/GameWorldView.cs`:

```csharp
using System.Collections.Generic;

namespace TileForge.Game;

public class GameWorldView
{
    public Dictionary<string, MapView> Maps { get; set; } = new();
    public string ActiveMapId { get; set; }
    public Dictionary<string, GroupView> Groups { get; set; } = new();
    public Dictionary<string, DialogueData> Dialogues { get; set; } = new();
    public List<QuestDefinition> Quests { get; set; } = new();
    public TileForge.Data.WorldLayout WorldLayout { get; set; }
}

public class MapView
{
    public int Width { get; set; }
    public int Height { get; set; }
    public List<LayerView> Layers { get; set; } = new();
}

public class LayerView
{
    public int[,] Tiles { get; set; }
    public List<EntityView> Entities { get; set; } = new();
}

public class EntityView
{
    public string Id { get; set; }
    public string GroupName { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}

public class GroupView
{
    public string Name { get; set; }
    public GroupType Type { get; set; }
    public int SpriteIndex { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}
```

Required usings for this file:
```csharp
using System.Collections.Generic;
using TileForge.Data;  // for GroupType, WorldLayout
```

`GroupType` is defined in `TileForge/Data/TileGroup.cs` under `namespace TileForge.Data`. `WorldLayout` is in `TileForge/Data/WorldLayout.cs`. `DialogueData` and `QuestDefinition` are in `TileForge.Game` (same namespace).

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~GameWorldViewTests" -v minimal`
Expected: All 4 PASS.

- [ ] **Step 5: Run full test suite**

Run: `dotnet test TileForge.Tests -v minimal`
Expected: All tests PASS.

- [ ] **Step 6: Commit**

```bash
git add TileForge/Game/GameWorldView.cs TileForge.Tests/Game/GameWorldViewTests.cs
git commit -m "feat: add GameWorldView DTO data classes for runtime/editor decoupling"
```

---

## Chunk 2: Reusable UI Widgets

### Task 2a: HitTestRegistry

#### Files:
- Create: `TileForge/UI/HitTestRegistry.cs`
- Create: `TileForge.Tests/UI/HitTestRegistryTests.cs`

- [ ] **Step 1: Write HitTestRegistry tests**

Create `TileForge.Tests/UI/HitTestRegistryTests.cs`:

```csharp
using Microsoft.Xna.Framework;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class HitTestRegistryTests
{
    [Fact]
    public void HitTest_NoRegistrations_ReturnsNegativeOne()
    {
        var registry = new HitTestRegistry();
        Assert.Equal(-1, registry.HitTest("anything", new Point(50, 50)));
    }

    [Fact]
    public void Register_ThenHitTest_ReturnsIndex()
    {
        var registry = new HitTestRegistry();
        registry.Register("buttons", new Rectangle(10, 10, 100, 30), 0);
        registry.Register("buttons", new Rectangle(10, 50, 100, 30), 1);

        Assert.Equal(0, registry.HitTest("buttons", new Point(50, 20)));
        Assert.Equal(1, registry.HitTest("buttons", new Point(50, 60)));
    }

    [Fact]
    public void HitTest_PointOutsideAllRects_ReturnsNegativeOne()
    {
        var registry = new HitTestRegistry();
        registry.Register("buttons", new Rectangle(10, 10, 100, 30), 0);

        Assert.Equal(-1, registry.HitTest("buttons", new Point(500, 500)));
    }

    [Fact]
    public void HitTest_WrongZone_ReturnsNegativeOne()
    {
        var registry = new HitTestRegistry();
        registry.Register("buttons", new Rectangle(10, 10, 100, 30), 0);

        Assert.Equal(-1, registry.HitTest("labels", new Point(50, 20)));
    }

    [Fact]
    public void Clear_RemovesAllRegistrations()
    {
        var registry = new HitTestRegistry();
        registry.Register("buttons", new Rectangle(10, 10, 100, 30), 0);

        registry.Clear();

        Assert.Equal(-1, registry.HitTest("buttons", new Point(50, 20)));
    }

    [Fact]
    public void OverlappingRects_ReturnsLastRegistered()
    {
        var registry = new HitTestRegistry();
        registry.Register("buttons", new Rectangle(10, 10, 100, 100), 0);
        registry.Register("buttons", new Rectangle(10, 10, 100, 100), 1);

        // Last registered wins (topmost in draw order)
        Assert.Equal(1, registry.HitTest("buttons", new Point(50, 50)));
    }

    [Fact]
    public void MultipleZones_Independent()
    {
        var registry = new HitTestRegistry();
        registry.Register("zone-a", new Rectangle(10, 10, 50, 50), 0);
        registry.Register("zone-b", new Rectangle(10, 10, 50, 50), 5);

        Assert.Equal(0, registry.HitTest("zone-a", new Point(30, 30)));
        Assert.Equal(5, registry.HitTest("zone-b", new Point(30, 30)));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~HitTestRegistryTests" -v minimal`
Expected: FAIL — class doesn't exist.

- [ ] **Step 3: Write HitTestRegistry implementation**

Create `TileForge/UI/HitTestRegistry.cs`:

```csharp
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileForge.UI;

public class HitTestRegistry
{
    private readonly Dictionary<string, List<(Rectangle Rect, int Index)>> _zones = new();

    public void Register(string zone, Rectangle rect, int index)
    {
        if (!_zones.TryGetValue(zone, out var list))
        {
            list = new List<(Rectangle, int)>();
            _zones[zone] = list;
        }
        list.Add((rect, index));
    }

    public int HitTest(string zone, Point position)
    {
        if (!_zones.TryGetValue(zone, out var list))
            return -1;

        // Iterate in reverse so last-registered (topmost) wins
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].Rect.Contains(position))
                return list[i].Index;
        }
        return -1;
    }

    public void Clear()
    {
        foreach (var list in _zones.Values)
            list.Clear();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~HitTestRegistryTests" -v minimal`
Expected: All 7 PASS.

- [ ] **Step 5: Commit**

```bash
git add TileForge/UI/HitTestRegistry.cs TileForge.Tests/UI/HitTestRegistryTests.cs
git commit -m "feat: add HitTestRegistry for unified hit-test zone management"
```

---

### Task 2b: DeferredInputQueue

#### Files:
- Create: `TileForge/UI/DeferredInputQueue.cs`

This is a thin wrapper around existing patterns. The existing DialogueTreeEditor already does this inline; we're extracting it. No separate test file needed — the widget is exercised through ConditionListEditor/ActionListEditor tests and integration.

- [ ] **Step 1: Write DeferredInputQueue**

Create `TileForge/UI/DeferredInputQueue.cs`:

```csharp
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;

namespace TileForge.UI;

/// <summary>
/// Queues dropdown and combobox updates during Draw for processing in the next Update.
/// This avoids collision between nested interactive widgets rendered in the same frame.
/// </summary>
public class DeferredInputQueue
{
    private readonly List<(Dropdown Dropdown, Rectangle Rect)> _dropdowns = new();
    private readonly List<(ComboBox ComboBox, Rectangle Rect)> _comboBoxes = new();

    public void EnqueueDropdown(Dropdown dropdown, Rectangle rect)
    {
        _dropdowns.Add((dropdown, rect));
    }

    public void EnqueueComboBox(ComboBox comboBox, Rectangle rect)
    {
        _comboBoxes.Add((comboBox, rect));
    }

    public void ProcessAll(MouseState mouse, MouseState prevMouse, SpriteFont font,
                           int screenW, int screenH)
    {
        foreach (var (dd, rect) in _dropdowns)
            dd.Update(mouse, prevMouse, rect, font, screenW, screenH);

        foreach (var (cb, rect) in _comboBoxes)
            cb.UpdateMouse(mouse, prevMouse, rect, font, screenW, screenH);
    }

    public void Clear()
    {
        _dropdowns.Clear();
        _comboBoxes.Clear();
    }
}
```

- [ ] **Step 2: Verify build succeeds**

Run: `dotnet build TileForge -v minimal`
Expected: Build succeeded.

Note: The exact method signatures for `Dropdown.Update()` and `ComboBox.UpdateMouse()` must match the existing DojoUI API. If the signatures differ (e.g., different parameter order or additional params), adjust the `ProcessAll` method to match. Check `DojoUI/Dropdown.cs` and `DojoUI/ComboBox.cs` for exact signatures.

- [ ] **Step 3: Commit**

```bash
git add TileForge/UI/DeferredInputQueue.cs
git commit -m "feat: extract DeferredInputQueue from inline dropdown/combobox update pattern"
```

---

### Task 2c: ConditionListEditor

#### Files:
- Create: `TileForge/UI/ConditionListEditor.cs`
- Create: `TileForge.Tests/UI/ConditionListEditorTests.cs`

- [ ] **Step 1: Write ConditionListEditor tests**

Create `TileForge.Tests/UI/ConditionListEditorTests.cs`:

```csharp
using TileForge.Game;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class ConditionListEditorTests
{
    [Fact]
    public void FromConditions_Null_CreatesEmptyList()
    {
        var editor = new ConditionListEditor();
        editor.LoadFromConditions(null);
        Assert.Empty(editor.ToConditions());
    }

    [Fact]
    public void FromConditions_EmptyList_CreatesEmptyList()
    {
        var editor = new ConditionListEditor();
        editor.LoadFromConditions(new List<Condition>());
        Assert.Empty(editor.ToConditions());
    }

    [Fact]
    public void FromConditions_HasFlag_RoundTrips()
    {
        var conditions = new List<Condition>
        {
            new() { Type = "has_flag", Flag = "talked_to_npc" }
        };

        var editor = new ConditionListEditor();
        editor.LoadFromConditions(conditions);
        var result = editor.ToConditions();

        Assert.Single(result);
        Assert.Equal("has_flag", result[0].Type);
        Assert.Equal("talked_to_npc", result[0].Flag);
    }

    [Fact]
    public void FromConditions_VariableEq_UsesSeparateFields()
    {
        var conditions = new List<Condition>
        {
            new() { Type = "variable_eq", Variable = "quest_stage", Value = "3" }
        };

        var editor = new ConditionListEditor();
        editor.LoadFromConditions(conditions);
        var result = editor.ToConditions();

        Assert.Single(result);
        Assert.Equal("variable_eq", result[0].Type);
        Assert.Equal("quest_stage", result[0].Variable);
        Assert.Equal("3", result[0].Value);
    }

    [Fact]
    public void FromConditions_MultipleConditions_PreservesOrder()
    {
        var conditions = new List<Condition>
        {
            new() { Type = "has_flag", Flag = "first" },
            new() { Type = "has_item", Item = "sword" },
            new() { Type = "not_flag", Flag = "done" },
        };

        var editor = new ConditionListEditor();
        editor.LoadFromConditions(conditions);
        var result = editor.ToConditions();

        Assert.Equal(3, result.Count);
        Assert.Equal("has_flag", result[0].Type);
        Assert.Equal("has_item", result[1].Type);
        Assert.Equal("not_flag", result[2].Type);
    }

    [Fact]
    public void AddCondition_AppendsDefault()
    {
        var editor = new ConditionListEditor();
        editor.LoadFromConditions(new List<Condition>());
        editor.AddCondition();

        var result = editor.ToConditions();
        Assert.Single(result);
        Assert.Equal("has_flag", result[0].Type); // default type
    }

    [Fact]
    public void RemoveConditionAt_RemovesCorrectIndex()
    {
        var conditions = new List<Condition>
        {
            new() { Type = "has_flag", Flag = "a" },
            new() { Type = "has_flag", Flag = "b" },
            new() { Type = "has_flag", Flag = "c" },
        };

        var editor = new ConditionListEditor();
        editor.LoadFromConditions(conditions);
        editor.RemoveConditionAt(1); // remove "b"

        var result = editor.ToConditions();
        Assert.Equal(2, result.Count);
        Assert.Equal("a", result[0].Flag);
        Assert.Equal("c", result[1].Flag);
    }

    [Fact]
    public void QuestActive_RoundTrips()
    {
        var conditions = new List<Condition>
        {
            new() { Type = "quest_active", Value = "cave_quest" }
        };

        var editor = new ConditionListEditor();
        editor.LoadFromConditions(conditions);
        var result = editor.ToConditions();

        Assert.Single(result);
        Assert.Equal("quest_active", result[0].Type);
        Assert.Equal("cave_quest", result[0].Value);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~ConditionListEditorTests" -v minimal`
Expected: FAIL — class doesn't exist.

- [ ] **Step 3: Write ConditionListEditor**

Create `TileForge/UI/ConditionListEditor.cs`:

```csharp
using System.Collections.Generic;
using TileForge.Game;

namespace TileForge.UI;

/// <summary>
/// Manages a list of conditions for editing. Stores condition data as typed fields
/// (no string packing). Used by both DialogueWorkspace and QuestWorkspace.
///
/// UI rendering (Update/Draw) will be added when the workspace editors are built
/// in Phase 2. This class provides the data layer: load, mutate, export.
/// </summary>
public class ConditionListEditor
{
    public static readonly string[] ConditionTypes = new[]
    {
        "has_flag", "not_flag", "has_item",
        "variable_eq", "variable_gte", "variable_lt",
        "quest_active", "quest_complete"
    };

    private readonly List<ConditionEntry> _entries = new();

    public int Count => _entries.Count;

    public void LoadFromConditions(List<Condition> conditions)
    {
        _entries.Clear();
        if (conditions == null) return;

        foreach (var c in conditions)
        {
            _entries.Add(new ConditionEntry
            {
                Type = c.Type ?? "has_flag",
                PrimaryValue = GetPrimaryValue(c),
                SecondaryValue = GetSecondaryValue(c),
            });
        }
    }

    public List<Condition> ToConditions()
    {
        var result = new List<Condition>();
        foreach (var entry in _entries)
        {
            var c = new Condition { Type = entry.Type };
            switch (entry.Type)
            {
                case "has_flag":
                case "not_flag":
                    c.Flag = entry.PrimaryValue;
                    break;
                case "has_item":
                    c.Item = entry.PrimaryValue;
                    break;
                case "variable_eq":
                case "variable_gte":
                case "variable_lt":
                    c.Variable = entry.PrimaryValue;
                    c.Value = entry.SecondaryValue;
                    break;
                case "quest_active":
                case "quest_complete":
                    c.Value = entry.PrimaryValue;
                    break;
            }
            result.Add(c);
        }
        return result;
    }

    public void AddCondition()
    {
        _entries.Add(new ConditionEntry { Type = "has_flag", PrimaryValue = "", SecondaryValue = "" });
    }

    public void RemoveConditionAt(int index)
    {
        if (index >= 0 && index < _entries.Count)
            _entries.RemoveAt(index);
    }

    public ConditionEntry GetEntry(int index) => _entries[index];

    private static string GetPrimaryValue(Condition c) => c.Type switch
    {
        "has_flag" or "not_flag" => c.Flag ?? "",
        "has_item" => c.Item ?? "",
        "variable_eq" or "variable_gte" or "variable_lt" => c.Variable ?? "",
        "quest_active" or "quest_complete" => c.Value ?? "",
        _ => ""
    };

    private static string GetSecondaryValue(Condition c) => c.Type switch
    {
        "variable_eq" or "variable_gte" or "variable_lt" => c.Value ?? "",
        _ => ""
    };

    public class ConditionEntry
    {
        public string Type { get; set; } = "has_flag";
        public string PrimaryValue { get; set; } = "";
        public string SecondaryValue { get; set; } = "";  // only for variable conditions
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~ConditionListEditorTests" -v minimal`
Expected: All 8 PASS.

- [ ] **Step 5: Commit**

```bash
git add TileForge/UI/ConditionListEditor.cs TileForge.Tests/UI/ConditionListEditorTests.cs
git commit -m "feat: add ConditionListEditor with typed fields (no string packing)"
```

---

### Task 2d: ActionListEditor

#### Files:
- Create: `TileForge/UI/ActionListEditor.cs`
- Create: `TileForge.Tests/UI/ActionListEditorTests.cs`

- [ ] **Step 1: Write ActionListEditor tests**

Create `TileForge.Tests/UI/ActionListEditorTests.cs`:

```csharp
using TileForge.Game;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class ActionListEditorTests
{
    [Fact]
    public void FromActions_Null_CreatesEmptyList()
    {
        var editor = new ActionListEditor();
        editor.LoadFromActions(null);
        Assert.Empty(editor.ToActions());
    }

    [Fact]
    public void FromActions_SetFlag_RoundTrips()
    {
        var actions = new List<DialogueAction>
        {
            new() { Type = "set_flag", Value = "quest_accepted" }
        };

        var editor = new ActionListEditor();
        editor.LoadFromActions(actions);
        var result = editor.ToActions();

        Assert.Single(result);
        Assert.Equal("set_flag", result[0].Type);
        Assert.Equal("quest_accepted", result[0].Value);
    }

    [Fact]
    public void FromActions_SetVariable_RoundTrips()
    {
        var actions = new List<DialogueAction>
        {
            new() { Type = "set_variable", Key = "gold", Value = "100" }
        };

        var editor = new ActionListEditor();
        editor.LoadFromActions(actions);
        var result = editor.ToActions();

        Assert.Single(result);
        Assert.Equal("set_variable", result[0].Type);
        Assert.Equal("gold", result[0].Key);
        Assert.Equal("100", result[0].Value);
    }

    [Fact]
    public void FromActions_GiveItem_RoundTrips()
    {
        var actions = new List<DialogueAction>
        {
            new() { Type = "give_item", Value = "Potion" }
        };

        var editor = new ActionListEditor();
        editor.LoadFromActions(actions);
        var result = editor.ToActions();

        Assert.Single(result);
        Assert.Equal("give_item", result[0].Type);
        Assert.Equal("Potion", result[0].Value);
    }

    [Fact]
    public void FromActions_Log_PreservesColorAndText()
    {
        var actions = new List<DialogueAction>
        {
            new() { Type = "log", Text = "Quest accepted!", Color = "yellow" }
        };

        var editor = new ActionListEditor();
        editor.LoadFromActions(actions);
        var result = editor.ToActions();

        Assert.Single(result);
        Assert.Equal("log", result[0].Type);
        Assert.Equal("Quest accepted!", result[0].Text);
        Assert.Equal("yellow", result[0].Color);
    }

    [Fact]
    public void FromActions_MultipleActions_PreservesOrder()
    {
        var actions = new List<DialogueAction>
        {
            new() { Type = "set_flag", Value = "flag1" },
            new() { Type = "give_item", Value = "Sword" },
            new() { Type = "start_quest", Value = "main_quest" },
        };

        var editor = new ActionListEditor();
        editor.LoadFromActions(actions);
        var result = editor.ToActions();

        Assert.Equal(3, result.Count);
        Assert.Equal("set_flag", result[0].Type);
        Assert.Equal("give_item", result[1].Type);
        Assert.Equal("start_quest", result[2].Type);
    }

    [Fact]
    public void AddAction_AppendsDefault()
    {
        var editor = new ActionListEditor();
        editor.LoadFromActions(new List<DialogueAction>());
        editor.AddAction();

        var result = editor.ToActions();
        Assert.Single(result);
        Assert.Equal("set_flag", result[0].Type);
    }

    [Fact]
    public void RemoveActionAt_RemovesCorrectIndex()
    {
        var actions = new List<DialogueAction>
        {
            new() { Type = "set_flag", Value = "a" },
            new() { Type = "set_flag", Value = "b" },
            new() { Type = "set_flag", Value = "c" },
        };

        var editor = new ActionListEditor();
        editor.LoadFromActions(actions);
        editor.RemoveActionAt(1);

        var result = editor.ToActions();
        Assert.Equal(2, result.Count);
        Assert.Equal("a", result[0].Value);
        Assert.Equal("c", result[1].Value);
    }

    [Fact]
    public void HealAndDamage_RoundTrip()
    {
        var actions = new List<DialogueAction>
        {
            new() { Type = "heal", Value = "20" },
            new() { Type = "damage", Value = "5" },
        };

        var editor = new ActionListEditor();
        editor.LoadFromActions(actions);
        var result = editor.ToActions();

        Assert.Equal(2, result.Count);
        Assert.Equal("heal", result[0].Type);
        Assert.Equal("20", result[0].Value);
        Assert.Equal("damage", result[1].Type);
        Assert.Equal("5", result[1].Value);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~ActionListEditorTests" -v minimal`
Expected: FAIL — class doesn't exist.

- [ ] **Step 3: Write ActionListEditor**

Create `TileForge/UI/ActionListEditor.cs`:

```csharp
using System.Collections.Generic;
using TileForge.Game;

namespace TileForge.UI;

/// <summary>
/// Manages a list of actions for editing. Used by dialogue nodes, dialogue choices,
/// and quest rewards (all share the same DialogueAction type).
///
/// UI rendering (Update/Draw) will be added in Phase 2.
/// This class provides the data layer: load, mutate, export.
/// </summary>
public class ActionListEditor
{
    public static readonly string[] ActionTypes = new[]
    {
        "set_flag", "set_variable", "increment",
        "give_item", "remove_item",
        "start_quest", "complete_objective",
        "heal", "damage", "log"
    };

    private readonly List<ActionEntry> _entries = new();

    public int Count => _entries.Count;

    public void LoadFromActions(List<DialogueAction> actions)
    {
        _entries.Clear();
        if (actions == null) return;

        foreach (var a in actions)
        {
            _entries.Add(new ActionEntry
            {
                Type = a.Type ?? "set_flag",
                Value = a.Value ?? "",
                Key = a.Key ?? "",
                Text = a.Text ?? "",
                Color = a.Color ?? "",
            });
        }
    }

    public List<DialogueAction> ToActions()
    {
        var result = new List<DialogueAction>();
        foreach (var entry in _entries)
        {
            var a = new DialogueAction { Type = entry.Type };
            switch (entry.Type)
            {
                case "set_flag":
                case "increment":
                case "give_item":
                case "remove_item":
                case "start_quest":
                case "complete_objective":
                case "heal":
                case "damage":
                    a.Value = entry.Value;
                    break;
                case "set_variable":
                    a.Key = entry.Key;
                    a.Value = entry.Value;
                    break;
                case "log":
                    a.Text = entry.Text;
                    a.Color = entry.Color;
                    break;
            }
            result.Add(a);
        }
        return result;
    }

    public void AddAction()
    {
        _entries.Add(new ActionEntry { Type = "set_flag" });
    }

    public void RemoveActionAt(int index)
    {
        if (index >= 0 && index < _entries.Count)
            _entries.RemoveAt(index);
    }

    public ActionEntry GetEntry(int index) => _entries[index];

    public class ActionEntry
    {
        public string Type { get; set; } = "set_flag";
        public string Value { get; set; } = "";
        public string Key { get; set; } = "";
        public string Text { get; set; } = "";
        public string Color { get; set; } = "";
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~ActionListEditorTests" -v minimal`
Expected: All 9 PASS.

- [ ] **Step 5: Run full test suite**

Run: `dotnet test TileForge.Tests -v minimal`
Expected: All tests PASS.

- [ ] **Step 6: Commit**

```bash
git add TileForge/UI/ActionListEditor.cs TileForge.Tests/UI/ActionListEditorTests.cs \
       TileForge/UI/DeferredInputQueue.cs
git commit -m "feat: add ActionListEditor and DeferredInputQueue for reusable action/condition editing"
```

---

## Chunk 3: Quest Constants, QuestState, and Reward Migration

### Task 3a: QuestConstants

#### Files:
- Create: `TileForge/Game/QuestConstants.cs`
- Create: `TileForge.Tests/Game/QuestConstantsTests.cs`

- [ ] **Step 1: Write QuestConstants tests**

Create `TileForge.Tests/Game/QuestConstantsTests.cs`:

```csharp
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class QuestConstantsTests
{
    [Fact]
    public void StartedFlag_FormatsCorrectly()
    {
        Assert.Equal("quest_started:cave_quest", QuestConstants.StartedFlag("cave_quest"));
    }

    [Fact]
    public void CompleteFlag_FormatsCorrectly()
    {
        Assert.Equal("quest_complete:cave_quest", QuestConstants.CompleteFlag("cave_quest"));
    }

    [Fact]
    public void ObjectiveFlag_FormatsCorrectly()
    {
        Assert.Equal("objective_complete:find_sword", QuestConstants.ObjectiveFlag("find_sword"));
    }

    [Fact]
    public void Prefixes_MatchExpectedValues()
    {
        Assert.Equal("quest_started:", QuestConstants.StartedPrefix);
        Assert.Equal("quest_complete:", QuestConstants.CompletePrefix);
        Assert.Equal("objective_complete:", QuestConstants.ObjectivePrefix);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~QuestConstantsTests" -v minimal`
Expected: FAIL.

- [ ] **Step 3: Write QuestConstants**

Create `TileForge/Game/QuestConstants.cs`:

```csharp
namespace TileForge.Game;

public static class QuestConstants
{
    public const string StartedPrefix = "quest_started:";
    public const string CompletePrefix = "quest_complete:";
    public const string ObjectivePrefix = "objective_complete:";

    public static string StartedFlag(string questId) => StartedPrefix + questId;
    public static string CompleteFlag(string questId) => CompletePrefix + questId;
    public static string ObjectiveFlag(string objectiveId) => ObjectivePrefix + objectiveId;
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~QuestConstantsTests" -v minimal`
Expected: All 4 PASS.

- [ ] **Step 5: Replace hardcoded prefixes in DialogueRuntime.cs**

Modify `TileForge/Game/DialogueRuntime.cs`:

In `ConditionEvaluator.Evaluate()` (around lines 41-42), replace:
```csharp
// OLD:
"quest_active" => gsm.HasFlag("quest_started:" + condition.Value) && !gsm.HasFlag("quest_complete:" + condition.Value),
"quest_complete" => gsm.HasFlag("quest_complete:" + condition.Value),

// NEW:
"quest_active" => gsm.HasFlag(QuestConstants.StartedFlag(condition.Value)) && !gsm.HasFlag(QuestConstants.CompleteFlag(condition.Value)),
"quest_complete" => gsm.HasFlag(QuestConstants.CompleteFlag(condition.Value)),
```

In `ActionExecutor.Execute()` (around lines 99, 102), replace:
```csharp
// OLD:
case "start_quest":
    gsm.SetFlag("quest_started:" + action.Value);
    break;
case "complete_objective":
    gsm.SetFlag("objective_complete:" + action.Value);
    break;

// NEW:
case "start_quest":
    gsm.SetFlag(QuestConstants.StartedFlag(action.Value));
    break;
case "complete_objective":
    gsm.SetFlag(QuestConstants.ObjectiveFlag(action.Value));
    break;
```

- [ ] **Step 6: Replace hardcoded prefixes in QuestManager.cs**

Modify `TileForge/Game/QuestManager.cs`. Replace all hardcoded prefix strings with `QuestConstants` references. Key locations:

- `GetQuestStatus()`: replace `quest.StartFlag` and `quest.CompletionFlag` checks. Since StartFlag/CompletionFlag will become computed properties in the next task, for now just ensure the existing references continue to work.
- `CompleteQuest()`: replace `gsm.SetFlag(quest.CompletionFlag)` — this will use the computed property after Task 3b.

- [ ] **Step 7: Run full test suite**

Run: `dotnet test TileForge.Tests -v minimal`
Expected: All tests PASS (behavior unchanged, just using constants).

- [ ] **Step 8: Commit**

```bash
git add TileForge/Game/QuestConstants.cs TileForge.Tests/Game/QuestConstantsTests.cs \
       TileForge/Game/DialogueRuntime.cs TileForge/Game/QuestManager.cs
git commit -m "feat: add QuestConstants, replace hardcoded quest flag prefixes"
```

---

### Task 3b: QuestState and QuestDefinition Update

#### Files:
- Create: `TileForge/Game/QuestState.cs`
- Modify: `TileForge/Game/QuestData.cs`
- Modify: `TileForge/Game/GameState.cs`
- Create: `TileForge.Tests/Game/QuestStateTests.cs`

- [ ] **Step 1: Write QuestState tests**

Create `TileForge.Tests/Game/QuestStateTests.cs`:

```csharp
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class QuestStateTests
{
    [Fact]
    public void NewQuestState_DefaultsToNotStarted()
    {
        var qs = new QuestState { QuestId = "q1" };
        Assert.Equal(QuestStatus.NotStarted, qs.Status);
        Assert.Empty(qs.CompletedObjectives);
    }

    [Fact]
    public void CompletedObjectives_TracksIds()
    {
        var qs = new QuestState { QuestId = "q1", Status = QuestStatus.Active };
        qs.CompletedObjectives.Add("obj_1");
        qs.CompletedObjectives.Add("obj_2");

        Assert.Equal(2, qs.CompletedObjectives.Count);
        Assert.Contains("obj_1", qs.CompletedObjectives);
    }

    [Fact]
    public void QuestDefinition_StartFlag_IsComputed()
    {
        var quest = new QuestDefinition { Id = "cave_quest" };
        Assert.Equal("quest_started:cave_quest", quest.StartFlag);
    }

    [Fact]
    public void QuestDefinition_CompletionFlag_IsComputed()
    {
        var quest = new QuestDefinition { Id = "cave_quest" };
        Assert.Equal("quest_complete:cave_quest", quest.CompletionFlag);
    }

    [Fact]
    public void QuestDefinition_Rewards_IsList_OfDialogueAction()
    {
        var quest = new QuestDefinition
        {
            Id = "q1",
            Rewards = new List<DialogueAction>
            {
                new() { Type = "set_flag", Value = "hero" },
                new() { Type = "give_item", Value = "Gold Ring" },
            }
        };

        Assert.Equal(2, quest.Rewards.Count);
        Assert.Equal("set_flag", quest.Rewards[0].Type);
        Assert.Equal("give_item", quest.Rewards[1].Type);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~QuestStateTests" -v minimal`
Expected: FAIL.

- [ ] **Step 3: Create QuestState class**

Create `TileForge/Game/QuestState.cs`:

```csharp
using System.Collections.Generic;

namespace TileForge.Game;

// QuestStatus enum is NOT defined here — it already exists in QuestManager.cs
// as: NotStarted, Active, Completed (with a 'd'). Relocate it to this file.
// Move the existing enum from QuestManager.cs to here, keeping the spelling "Completed".

public enum QuestStatus
{
    NotStarted,
    Active,
    Completed,
}

public class QuestState
{
    public string QuestId { get; set; }
    public QuestStatus Status { get; set; } = QuestStatus.NotStarted;
    public HashSet<string> CompletedObjectives { get; set; } = new();
}
```

- [ ] **Step 4: Update QuestDefinition in QuestData.cs**

Modify `TileForge/Game/QuestData.cs`:

Replace the `QuestDefinition` class. Change `StartFlag` and `CompletionFlag` from settable properties to computed properties. Change `Rewards` from `QuestRewards` to `List<DialogueAction>`. Keep `QuestRewards` class for now (migration needs it for deserialization of old format).

```csharp
// Updated QuestDefinition:
public class QuestDefinition
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public string StartFlag => QuestConstants.StartedFlag(Id);

    [System.Text.Json.Serialization.JsonIgnore]
    public string CompletionFlag => QuestConstants.CompleteFlag(Id);

    public List<QuestObjective> Objectives { get; set; } = new();
    public List<DialogueAction> Rewards { get; set; } = new();
}
```

Keep `QuestRewards` and `QuestObjective` classes unchanged for now.

- [ ] **Step 5: Add QuestStates to GameState.cs**

Modify `TileForge/Game/GameState.cs`. Add after the existing properties:

```csharp
public Dictionary<string, QuestState> QuestStates { get; set; } = new();
```

- [ ] **Step 6: Run QuestState tests**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~QuestStateTests" -v minimal`
Expected: All 5 PASS.

- [ ] **Step 7: Fix existing QuestManager tests for new QuestDefinition**

The existing tests in `QuestManagerTests.cs` use `MakeQuest()` with `startFlag:` and `completionFlag:` parameters, and `QuestRewards`. These need updating since `StartFlag`/`CompletionFlag` are now computed and `Rewards` is now `List<DialogueAction>`.

Update the `MakeQuest` helper to remove `startFlag`/`completionFlag` parameters (they're computed from `id`). Update any tests that pass `QuestRewards` to pass `List<DialogueAction>` instead.

Update the `MakeQuest` helper. Old signature:

```csharp
private static QuestDefinition MakeQuest(string id, string startFlag, string completionFlag,
    List<QuestObjective> objectives = null, QuestRewards rewards = null)
```

New signature (StartFlag/CompletionFlag computed from Id, rewards are actions):

```csharp
private static QuestDefinition MakeQuest(string id,
    List<QuestObjective> objectives = null, List<DialogueAction> rewards = null)
{
    return new QuestDefinition
    {
        Id = id,
        Name = $"Quest: {id}",
        Objectives = objectives ?? new List<QuestObjective>(),
        Rewards = rewards ?? new List<DialogueAction>(),
    };
}
```

Update all call sites:
- `MakeQuest("q1", startFlag: "quest_started:q1", completionFlag: "quest_done:q1", objectives: ...)` becomes `MakeQuest("q1", objectives: ...)`
- Tests that previously checked `gsm.HasFlag("quest_done:q1")` for completion must now check `gsm.HasFlag("quest_complete:q1")` (the computed flag uses `QuestConstants.CompletePrefix` = `"quest_complete:"`, not `"quest_done:"`)
- Tests using `QuestRewards { SetFlags = new() { "hero" } }` become `new List<DialogueAction> { new() { Type = "set_flag", Value = "hero" } }`

- [ ] **Step 8: Update QuestManager.CompleteQuest() for new rewards format**

Modify `TileForge/Game/QuestManager.cs`. In `CompleteQuest()` (around line 139), replace the old reward application:

```csharp
// OLD:
if (quest.Rewards?.SetFlags != null)
    foreach (var flag in quest.Rewards.SetFlags)
        gsm.SetFlag(flag);
if (quest.Rewards?.SetVariables != null)
    foreach (var (key, value) in quest.Rewards.SetVariables)
        gsm.SetVariable(key, value);

// NEW:
if (quest.Rewards != null)
    ActionExecutor.ExecuteAll(quest.Rewards, gsm);
```

This reuses the existing `ActionExecutor` — same action types, same execution logic. Quest rewards now support all 10 action types (give_item, heal, log, etc.), not just set_flag and set_variable.

- [ ] **Step 9: Run full test suite**

Run: `dotnet test TileForge.Tests -v minimal`
Expected: All tests PASS.

- [ ] **Step 10: Commit**

```bash
git add TileForge/Game/QuestState.cs TileForge/Game/QuestData.cs \
       TileForge/Game/GameState.cs TileForge/Game/QuestManager.cs \
       TileForge.Tests/Game/QuestStateTests.cs TileForge.Tests/Game/QuestManagerTests.cs
git commit -m "feat: add QuestState, computed flag properties, rewards as DialogueAction list"
```

---

### Task 3c: Quest Reward Migration in QuestFileManager

#### Files:
- Modify: `TileForge/Data/QuestFileManager.cs`
- Modify: `TileForge.Tests/Data/` (quest file tests, if they exist)

- [ ] **Step 1: Write migration tests**

The migration happens inside `QuestLoader`'s custom `NormalizedQuestFileConverter`, which already handles `rewards` as an object (lines 145-148, 185-226 in `QuestLoader.cs`). The converter must be updated to handle both formats: old object `{ "set_flags": [...], "set_variables": {...} }` and new array `[{ "type": "set_flag", ... }]`.

Add tests to `TileForge.Tests/Game/QuestDataTests.cs` (or create if needed):

```csharp
[Fact]
public void LoadFromJson_OldRewardFormat_MigratesToActionList()
{
    var json = """
    {
        "quests": [{
            "id": "q1",
            "name": "Test Quest",
            "objectives": [],
            "rewards": {
                "set_flags": ["hero_flag", "quest_done"],
                "set_variables": { "gold": "100" }
            }
        }]
    }
    """;

    var quests = QuestLoader.LoadFromJson(json);

    Assert.Single(quests);
    Assert.Equal(3, quests[0].Rewards.Count);
    Assert.Equal("set_flag", quests[0].Rewards[0].Type);
    Assert.Equal("hero_flag", quests[0].Rewards[0].Value);
    Assert.Equal("set_flag", quests[0].Rewards[1].Type);
    Assert.Equal("quest_done", quests[0].Rewards[1].Value);
    Assert.Equal("set_variable", quests[0].Rewards[2].Type);
    Assert.Equal("gold", quests[0].Rewards[2].Key);
    Assert.Equal("100", quests[0].Rewards[2].Value);
}

[Fact]
public void LoadFromJson_NewRewardFormat_LoadsDirectly()
{
    var json = """
    {
        "quests": [{
            "id": "q1",
            "name": "Test Quest",
            "objectives": [],
            "rewards": [
                { "type": "set_flag", "value": "hero_flag" },
                { "type": "give_item", "value": "Gold Ring" }
            ]
        }]
    }
    """;

    var quests = QuestLoader.LoadFromJson(json);

    Assert.Single(quests);
    Assert.Equal(2, quests[0].Rewards.Count);
    Assert.Equal("set_flag", quests[0].Rewards[0].Type);
    Assert.Equal("give_item", quests[0].Rewards[1].Type);
}

[Fact]
public void LoadFromJson_NoRewards_DefaultsToEmptyList()
{
    var json = """
    {
        "quests": [{
            "id": "q1",
            "name": "Test Quest",
            "objectives": []
        }]
    }
    """;

    var quests = QuestLoader.LoadFromJson(json);

    Assert.Single(quests);
    Assert.Empty(quests[0].Rewards);
}

[Fact]
public void LoadFromJson_OldStartFlag_IgnoredInFavorOfComputed()
{
    var json = """
    {
        "quests": [{
            "id": "q1",
            "name": "Test Quest",
            "start_flag": "old_custom_flag",
            "completion_flag": "old_complete_flag",
            "objectives": []
        }]
    }
    """;

    var quests = QuestLoader.LoadFromJson(json);

    // Computed from Id, not from JSON
    Assert.Equal("quest_started:q1", quests[0].StartFlag);
    Assert.Equal("quest_complete:q1", quests[0].CompletionFlag);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~QuestDataTests" -v minimal`
Expected: FAIL — `Rewards` is still `QuestRewards`, not `List<DialogueAction>`.

- [ ] **Step 3: Update NormalizedQuestFileConverter in QuestLoader.cs**

Modify `TileForge/Game/QuestLoader.cs`. In `ReadQuestDefinition()`, update the `"rewards"` case (around line 145) to handle both formats:

```csharp
case "rewards":
    if (reader.TokenType == JsonTokenType.StartObject)
    {
        // Old format: { "set_flags": [...], "set_variables": {...} }
        quest.Rewards = MigrateOldRewards(ref reader);
    }
    else if (reader.TokenType == JsonTokenType.StartArray)
    {
        // New format: [{ "type": "set_flag", ... }]
        quest.Rewards = ReadActionList(ref reader);
    }
    else
    {
        reader.Skip();
    }
    break;
```

Also remove the `"startflag"` and `"completionflag"` cases (lines 133-134) since those properties are now computed and `[JsonIgnore]`. Replace with `reader.Skip()`:

```csharp
case "startflag":
case "completionflag":
    reader.Skip();
    break;
```

Add these helper methods to the converter class:

```csharp
private static List<DialogueAction> MigrateOldRewards(ref Utf8JsonReader reader)
{
    var actions = new List<DialogueAction>();
    var setFlags = new List<string>();
    var setVariables = new Dictionary<string, string>();

    while (reader.Read())
    {
        if (reader.TokenType == JsonTokenType.EndObject) break;
        if (reader.TokenType != JsonTokenType.PropertyName) continue;

        string propName = reader.GetString();
        reader.Read();
        string norm = Normalize(propName);

        switch (norm)
        {
            case "setflags":
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        setFlags.Add(reader.GetString());
                }
                break;
            case "setvariables":
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                    {
                        if (reader.TokenType != JsonTokenType.PropertyName) continue;
                        string key = reader.GetString();
                        reader.Read();
                        setVariables[key] = reader.GetString();
                    }
                }
                break;
            default:
                reader.Skip();
                break;
        }
    }

    foreach (var flag in setFlags)
        actions.Add(new DialogueAction { Type = "set_flag", Value = flag });
    foreach (var (key, value) in setVariables)
        actions.Add(new DialogueAction { Type = "set_variable", Key = key, Value = value });

    return actions;
}

private static List<DialogueAction> ReadActionList(ref Utf8JsonReader reader)
{
    var actions = new List<DialogueAction>();

    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
    {
        if (reader.TokenType != JsonTokenType.StartObject) continue;

        var action = new DialogueAction();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) break;
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            string propName = reader.GetString();
            reader.Read();
            string norm = Normalize(propName);

            switch (norm)
            {
                case "type":  action.Type = reader.GetString(); break;
                case "value": action.Value = reader.GetString(); break;
                case "key":   action.Key = reader.GetString(); break;
                case "color": action.Color = reader.GetString(); break;
                case "text":  action.Text = reader.GetString(); break;
                default:      reader.Skip(); break;
            }
        }
        actions.Add(action);
    }

    return actions;
}
```

Also remove the old `ReadQuestRewards()` method since it's no longer used.

- [ ] **Step 4: Run migration tests**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~QuestDataTests" -v minimal`
Expected: All 4 new tests PASS.

- [ ] **Step 5: Run full test suite**

Run: `dotnet test TileForge.Tests -v minimal`
Expected: All tests PASS.

- [ ] **Step 6: Commit**

```bash
git add TileForge/Game/QuestLoader.cs TileForge.Tests/Game/QuestDataTests.cs
git commit -m "feat: add quest reward migration (old QuestRewards object -> List<DialogueAction> array)"
```

---

## Phase 1 Complete Checkpoint

After all tasks are done:

- [ ] **Run full test suite**

Run: `dotnet test TileForge.Tests -v minimal`
Expected: All tests PASS, 0 failures.

- [ ] **Verify new file count**

New files created:
- `TileForge/UI/IWorkspace.cs`
- `TileForge/UI/WorkspaceMode.cs`
- `TileForge/UI/HitTestRegistry.cs`
- `TileForge/UI/DeferredInputQueue.cs`
- `TileForge/UI/ConditionListEditor.cs`
- `TileForge/UI/ActionListEditor.cs`
- `TileForge/Game/QuestConstants.cs`
- `TileForge/Game/QuestState.cs`
- `TileForge/Game/GameWorldView.cs`

New test files:
- `TileForge.Tests/UI/WorkspaceTests.cs`
- `TileForge.Tests/UI/HitTestRegistryTests.cs`
- `TileForge.Tests/UI/ConditionListEditorTests.cs`
- `TileForge.Tests/UI/ActionListEditorTests.cs`
- `TileForge.Tests/Game/QuestConstantsTests.cs`
- `TileForge.Tests/Game/QuestStateTests.cs`
- `TileForge.Tests/Game/GameWorldViewTests.cs`

Modified files:
- `TileForge/Editor/EditorState.cs` — ActiveWorkspace property
- `TileForge/LayoutConstants.cs` — workspace button constants
- `TileForge/Game/QuestData.cs` — computed flags, List<DialogueAction> rewards
- `TileForge/Game/QuestManager.cs` — QuestConstants, ActionExecutor for rewards
- `TileForge/Game/DialogueRuntime.cs` — QuestConstants
- `TileForge/Game/GameState.cs` — QuestStates dict
- `TileForge/Data/QuestFileManager.cs` — reward migration

- [ ] **Final commit for session docs**

Update `docs/Status.md` and create session summary as per session protocol.
