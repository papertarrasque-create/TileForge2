# Phase 5: V1 Cleanup Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove all v1 backward-compatibility fallback code that is now dead (migration converts on load), delete the legacy DialogueEditor, and clean up unused data classes and property presets.

**Architecture:** Pure deletion/cleanup. No new features. Migration code (`MigrateV1ToV2`) and v1 field deserialization properties stay — they enable loading old JSON files. Everything else that checks v1 fields at runtime gets removed since migration guarantees v1 fields are empty by the time they reach screens.

**Tech Stack:** C# / .NET 9.0 / MonoGame 3.8 / xUnit

---

## File Structure

| File | Action | What changes |
|------|--------|-------------|
| `TileForge/UI/DialogueEditor.cs` | **DELETE** | 738-line legacy v1 editor, replaced by DialogueTreeEditor |
| `TileForge.Tests/UI/DialogueEditorTests.cs` | **DELETE** | Tests for deleted editor |
| `TileForge/Game/QuestData.cs` | Modify | Remove dead `QuestRewards` class |
| `TileForge/Game/Screens/DialogueScreen.cs` | Modify | Remove v1 fallback code (~20 lines) |
| `TileForge/UI/GroupEditor.cs` | Modify | Remove `concluded_flag`/`concluded_dialogue` from property presets |
| `TileForge.Tests/Game/Screens/GameplayFeatureTests.cs` | Modify | Remove concluded_flag test cases |
| `TileForge.Tests/Game/Screens/DialogueScreenTests.cs` | Modify | Update tests that use v1 fields to use v2 equivalents |

**Files that STAY unchanged:**
- `TileForge/Game/DialogueData.cs` — v1 properties needed for JSON deserialization
- `TileForge/Data/DialogueFileManager.cs` — `MigrateV1ToV2()` stays
- `TileForge.Tests/Game/DialogueV2Tests.cs` — migration tests stay
- `TileForge/UI/DialogueTreeEditor.cs` — already v2 native

---

## Chunk 1: Delete Legacy Editor + Dead Classes

### Task 1: Delete DialogueEditor and its tests

**Files:**
- Delete: `TileForge/UI/DialogueEditor.cs`
- Delete: `TileForge.Tests/UI/DialogueEditorTests.cs`

- [ ] **Step 1: Verify DialogueEditor is not referenced anywhere**

Run: `grep -r "DialogueEditor" TileForge/ TileForge.Tests/ --include="*.cs" -l`

Expected: Only `DialogueEditor.cs` and `DialogueEditorTests.cs` should appear. If other files reference it, they need updating first.

- [ ] **Step 2: Delete the files**

```bash
rm TileForge/UI/DialogueEditor.cs
rm TileForge.Tests/UI/DialogueEditorTests.cs
```

- [ ] **Step 3: Verify build succeeds**

Run: `dotnet build TileForge --no-restore 2>&1 | tail -5`
Expected: Build succeeded, 0 errors

- [ ] **Step 4: Run full test suite**

Run: `dotnet test TileForge.Tests --no-restore 2>&1 | tail -5`
Expected: All tests pass (count will drop by however many DialogueEditorTests existed)

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "cleanup: delete legacy DialogueEditor (replaced by DialogueTreeEditor)"
```

---

### Task 2: Remove dead QuestRewards class

**Files:**
- Modify: `TileForge/Game/QuestData.cs`

- [ ] **Step 1: Verify QuestRewards is not referenced anywhere**

Run: `grep -r "QuestRewards" TileForge/ TileForge.Tests/ --include="*.cs" -l`

Expected: Only `QuestData.cs` should appear.

- [ ] **Step 2: Remove the QuestRewards class**

In `TileForge/Game/QuestData.cs`, delete the entire `QuestRewards` class definition (approximately lines 69-73):

```csharp
// DELETE THIS:
public class QuestRewards
{
    public List<string> SetFlags { get; set; } = new();
    public Dictionary<string, string> SetVariables { get; set; } = new();
}
```

- [ ] **Step 3: Run full test suite**

Run: `dotnet test TileForge.Tests --no-restore 2>&1 | tail -5`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add TileForge/Game/QuestData.cs
git commit -m "cleanup: remove dead QuestRewards class (replaced by List<DialogueAction>)"
```

---

## Chunk 2: Remove V1 Fallbacks from DialogueScreen

### Task 3: Remove v1 fallback code from DialogueScreen

**Files:**
- Modify: `TileForge/Game/Screens/DialogueScreen.cs`

The v1 fallback code is safe to remove because `MigrateV1ToV2()` runs on every load path, guaranteeing v1 fields are always null/empty by the time DialogueScreen processes a dialogue.

- [ ] **Step 1: Remove choice SetsFlag fallback (lines 116-118)**

In the `Update()` method, in the choice selection block, remove:
```csharp
                    // v1 fallback
                    if (!string.IsNullOrEmpty(choice.SetsFlag))
                        _gameStateManager.SetFlag(choice.SetsFlag);
```

Keep the v2 line above it: `ActionExecutor.ExecuteAll(choice.Actions, _gameStateManager, _gameLog);`

- [ ] **Step 2: Remove node RequiresFlag fallback (lines 161-166)**

In `AdvanceToNode()`, remove:
```csharp
        // v1 fallback: check RequiresFlag
        if (!string.IsNullOrEmpty(node.RequiresFlag) && !_gameStateManager.HasFlag(node.RequiresFlag))
        {
            AdvanceToNode(node.NextNodeId);
            return;
        }
```

Keep the v2 check above it: `if (!ConditionEvaluator.EvaluateAll(node.Conditions, _gameStateManager))`

- [ ] **Step 3: Remove node SetsFlag/SetsVariable fallbacks (lines 183-196)**

In `AdvanceToNode()`, remove:
```csharp
        // v1 fallback: apply SetsFlag/SetsVariable
        if (!string.IsNullOrEmpty(node.SetsFlag))
            _gameStateManager.SetFlag(node.SetsFlag);

        if (!string.IsNullOrEmpty(node.SetsVariable))
        {
            var eqIndex = node.SetsVariable.IndexOf('=');
            if (eqIndex > 0)
            {
                var key = node.SetsVariable.Substring(0, eqIndex);
                var value = node.SetsVariable.Substring(eqIndex + 1);
                _gameStateManager.SetVariable(key, value);
            }
        }
```

Keep the v2 line above it: `ActionExecutor.ExecuteAll(node.Actions, _gameStateManager, _gameLog);`

- [ ] **Step 4: Remove choice RequiresFlag filter (line 203)**

In `AdvanceToNode()`, in the visible choices filter, remove:
```csharp
                .Where(c => string.IsNullOrEmpty(c.RequiresFlag) || _gameStateManager.HasFlag(c.RequiresFlag))
```

Keep the v2 filter: `.Where(c => ConditionEvaluator.EvaluateAll(c.Conditions, _gameStateManager))`

- [ ] **Step 5: Run full test suite**

Run: `dotnet test TileForge.Tests --no-restore 2>&1 | tail -10`
Expected: All tests pass. If any DialogueScreenTests fail, they need to be updated to use v2 fields (Task 4).

- [ ] **Step 6: Commit**

```bash
git add TileForge/Game/Screens/DialogueScreen.cs
git commit -m "cleanup: remove v1 fallback code from DialogueScreen (migration handles conversion)"
```

---

### Task 4: Update DialogueScreen tests to use v2 fields

**Files:**
- Modify: `TileForge.Tests/Game/Screens/DialogueScreenTests.cs`

If any tests broke in Task 3 because they set v1 fields (SetsFlag, RequiresFlag) directly on test data instead of using v2 Conditions/Actions, update them.

- [ ] **Step 1: Search for v1 field usage in DialogueScreenTests**

Run: `grep -n "SetsFlag\|RequiresFlag\|SetsVariable" TileForge.Tests/Game/Screens/DialogueScreenTests.cs`

For each occurrence, replace:
- `SetsFlag = "flag_name"` → `Actions = new() { new DialogueAction { Type = "set_flag", Value = "flag_name" } }`
- `RequiresFlag = "flag_name"` → `Conditions = new() { new Condition { Type = "has_flag", Flag = "flag_name" } }`
- On choices: same pattern

- [ ] **Step 2: Run DialogueScreen tests**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~DialogueScreenTests" --no-restore 2>&1 | tail -10`
Expected: All tests PASS

- [ ] **Step 3: Run full test suite**

Run: `dotnet test TileForge.Tests --no-restore 2>&1 | tail -5`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add TileForge.Tests/Game/Screens/DialogueScreenTests.cs
git commit -m "cleanup: update DialogueScreen tests to use v2 Conditions/Actions"
```

---

## Chunk 3: Remove Obsolete Property Presets and Tests

### Task 5: Remove concluded_flag/concluded_dialogue from GroupEditor

**Files:**
- Modify: `TileForge/UI/GroupEditor.cs`

- [ ] **Step 1: Remove concluded_dialogue from property dropdown checks**

In `GroupEditor.cs`, find and update the property key checks at ~lines 694 and 707.

At line 694, change:
```csharp
if (key is "dialogue" or "dialogue_id" or "concluded_dialogue" or "on_pickup_dialogue")
```
To:
```csharp
if (key is "dialogue" or "dialogue_id" or "on_pickup_dialogue")
```

At line 707, change:
```csharp
if (key is "dialogue" or "dialogue_id" or "concluded_dialogue" or "on_pickup_dialogue") return _projectContext.GetAvailableDialogues();
```
To:
```csharp
if (key is "dialogue" or "dialogue_id" or "on_pickup_dialogue") return _projectContext.GetAvailableDialogues();
```

Also search for `concluded_flag` in the same file and remove any property preset references.

- [ ] **Step 2: Run full test suite**

Run: `dotnet test TileForge.Tests --no-restore 2>&1 | tail -5`
Expected: All tests pass

- [ ] **Step 3: Commit**

```bash
git add TileForge/UI/GroupEditor.cs
git commit -m "cleanup: remove concluded_flag/concluded_dialogue property presets"
```

---

### Task 6: Remove obsolete concluded_flag test cases

**Files:**
- Modify: `TileForge.Tests/Game/Screens/GameplayFeatureTests.cs`

- [ ] **Step 1: Identify and remove concluded_flag tests**

Run: `grep -n "concluded" TileForge.Tests/Game/Screens/GameplayFeatureTests.cs`

Remove any test methods that test the concluded_flag/concluded_dialogue fallback behavior. These test a code path that no longer exists (TryShowDialogue was refactored in Phase 4 to use TriggerManager).

- [ ] **Step 2: Run full test suite**

Run: `dotnet test TileForge.Tests --no-restore 2>&1 | tail -5`
Expected: All tests pass (count drops by removed test count)

- [ ] **Step 3: Commit**

```bash
git add TileForge.Tests/Game/Screens/GameplayFeatureTests.cs
git commit -m "cleanup: remove obsolete concluded_flag test cases"
```

---

## Post-Phase Verification

- [ ] **Run full test suite**
Run: `dotnet test TileForge.Tests --no-restore`
Expected: All tests pass, 0 failures

- [ ] **Verify no remaining v1 runtime references**
Run: `grep -rn "RequiresFlag\|SetsFlag\|SetsVariable" TileForge/Game/Screens/ TileForge/UI/DialogueEditor.cs 2>/dev/null`
Expected: No results (migration code in Data/ is fine)

- [ ] **Verify build**
Run: `dotnet build TileForge --no-restore`
Expected: Build succeeded, 0 errors
