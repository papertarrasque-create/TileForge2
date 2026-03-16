# Phase 4: Integration — TriggerManager Wiring + Cross-References

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire TriggerManager into GameplayScreen (replacing inline dialogue resolution), add a pre-loaded dialogue dictionary, and implement cross-reference validation in the dialogue and quest workspace editors.

**Architecture:** PlayModeController builds a `Dictionary<string, DialogueData>` at play mode start and passes it through GamePlayContext. GameplayScreen delegates all dialogue resolution to TriggerManager.Fire(), then dispatches the result via DialogueScreenFactory. Cross-reference helpers scan dialogues/quests for broken references and surface warnings in the workspace UIs.

**Tech Stack:** C# / .NET 9.0 / MonoGame 3.8 / xUnit

**Scope note:** The GameWorldView DTO full integration (replacing all `_state.` references in GameplayScreen, BuildWorldView, ApplyToEditor) is deferred to a dedicated Phase 4b plan due to its scope and risk (~40 reference sites in a 1059-line file).

---

## File Structure

| File | Action | Responsibility |
|------|--------|---------------|
| `TileForge/Game/GamePlayContext.cs` | Modify | Add `Dialogues` dictionary property |
| `TileForge/Game/Screens/GameplayScreen.cs` | Modify | Replace TryShowDialogue/TryShowPickupDialogue with TriggerManager |
| `TileForge/PlayModeController.cs` | Modify | Build dialogue dictionary, pass through context |
| `TileForge/Game/CrossReferenceValidator.cs` | Create | Static validation helpers for dialogue/quest cross-references |
| `TileForge/UI/DialogueWorkspace.cs` | Modify | Show cross-reference info after save |
| `TileForge/UI/QuestWorkspace.cs` | Modify | Show "Referenced by" info in quest list |
| `TileForge.Tests/Game/TriggerManagerIntegrationTests.cs` | Create | Tests for TriggerManager wired through GameplayScreen flow |
| `TileForge.Tests/Game/CrossReferenceValidatorTests.cs` | Create | Tests for cross-reference validation |

---

## Chunk 1: TriggerManager Wiring

### Task 1: Add Dialogues to GamePlayContext + Build in PlayModeController

**Files:**
- Modify: `TileForge/Game/GamePlayContext.cs`
- Modify: `TileForge/PlayModeController.cs`

- [ ] **Step 1: Add Dialogues property to GamePlayContext**

In `TileForge/Game/GamePlayContext.cs`, first add the necessary imports at the top of the file:

```csharp
using System.Collections.Generic;
using TileForge.Game;
```

Then add a new property and constructor parameter:

```csharp
public Dictionary<string, DialogueData> Dialogues { get; }
```

Add it as the last constructor parameter with a default of `null`:

```csharp
public GamePlayContext(
    GameStateManager stateManager,
    SaveManager saveManager,
    GameInputManager inputManager,
    string bindingsPath,
    QuestManager questManager,
    Func<Rectangle> getCanvasBounds,
    EdgeTransitionResolver edgeResolver = null,
    IDialogueLoader dialogueLoader = null,
    Dictionary<string, DialogueData> dialogues = null)
{
    // ... existing assignments ...
    Dialogues = dialogues ?? new Dictionary<string, DialogueData>();
}
```

- [ ] **Step 2: Build dialogue dictionary in PlayModeController.Enter()**

In `TileForge/PlayModeController.cs`, in the `Enter()` method, after the dialogue loader is created (~line 138-144), build a dialogue dictionary from EditorState.Dialogues and pass it to context:

```csharp
// Build dialogue dictionary from editor state (pre-loaded for TriggerManager)
var dialogues = new Dictionary<string, DialogueData>(StringComparer.OrdinalIgnoreCase);
if (_state.Dialogues != null)
{
    foreach (var d in _state.Dialogues)
    {
        if (!string.IsNullOrEmpty(d.Id))
        {
            var migrated = d; // already v2 in memory
            dialogues[d.Id] = migrated;
        }
    }
}

// Also load from disk if loader is available (catches files not in editor state)
if (dialogueLoader != null && !string.IsNullOrEmpty(MapBaseDirectory))
{
    string dialoguesDir = System.IO.Path.Combine(MapBaseDirectory, "dialogues");
    if (System.IO.Directory.Exists(dialoguesDir))
    {
        foreach (var file in System.IO.Directory.GetFiles(dialoguesDir, "*.json"))
        {
            string id = System.IO.Path.GetFileNameWithoutExtension(file);
            if (!dialogues.ContainsKey(id))
            {
                var loaded = dialogueLoader.LoadDialogue(id);
                if (loaded != null)
                {
                    Data.DialogueFileManager.MigrateV1ToV2(loaded);
                    dialogues[id] = loaded;
                }
            }
        }
    }
}
```

Then update the context creation to pass the dialogues:

```csharp
_context = new GamePlayContext(
    _gameStateManager, _saveManager, _inputManager,
    _bindingsPath, _questManager,
    _getCanvasBounds, _edgeResolver, dialogueLoader, dialogues);
```

- [ ] **Step 3: Run existing tests to verify no regressions**

Run: `dotnet test TileForge.Tests --no-restore 2>&1 | tail -5`
Expected: All tests PASS (context constructor is backward-compatible with default null)

- [ ] **Step 4: Commit**

```bash
git add TileForge/Game/GamePlayContext.cs TileForge/PlayModeController.cs
git commit -m "feat: add Dialogues dictionary to GamePlayContext, build in PlayModeController"
```

---

### Task 2: Wire TriggerManager into GameplayScreen

**Files:**
- Modify: `TileForge/Game/Screens/GameplayScreen.cs`
- Create: `TileForge.Tests/Game/TriggerManagerIntegrationTests.cs`

- [ ] **Step 1: Write integration tests**

These tests verify the full flow: TriggerManager resolves dialogue → DialogueScreenFactory creates screen → correct screen type returned. We test TriggerManager directly since GameplayScreen is hard to unit test.

```csharp
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game;

public class TriggerManagerIntegrationTests
{
    private static GameStateManager CreateGSM()
    {
        var gsm = new GameStateManager();
        gsm.State.Player = new PlayerState();
        return gsm;
    }

    private static Dictionary<string, DialogueData> MakeDialogues(params DialogueData[] dialogues)
    {
        var dict = new Dictionary<string, DialogueData>();
        foreach (var d in dialogues) dict[d.Id] = d;
        return dict;
    }

    [Fact]
    public void TriggerManager_Fire_ThenFactory_Conversation()
    {
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "npc_talk",
            Type = "conversation",
            Nodes = new() { new DialogueNode { Id = "n1", Text = "Hello" } },
            Routes = new() { new DialogueRoute { StartNode = "n1" } },
        };
        var tm = new TriggerManager();
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "npc_talk" },
        };
        var result = tm.Fire(evt, gsm, MakeDialogues(dialogue));
        Assert.NotNull(result);

        var screen = DialogueScreenFactory.Create(result.Dialogue, gsm);
        Assert.IsType<DialogueScreen>(screen.Screen);
    }

    [Fact]
    public void TriggerManager_Fire_ThenFactory_Bark()
    {
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "guard_bark",
            Type = "bark",
            Nodes = new() { new DialogueNode { Id = "b1", Text = "Halt!" } },
            Routes = new() { new DialogueRoute { StartNode = "b1" } },
        };
        var tm = new TriggerManager();
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "guard_bark" },
        };
        var result = tm.Fire(evt, gsm, MakeDialogues(dialogue));
        Assert.NotNull(result);

        var screen = DialogueScreenFactory.Create(result.Dialogue, gsm,
            entityWorldPos: new Vector2(10, 20));
        Assert.NotNull(screen.BarkOverlay);
    }

    [Fact]
    public void TriggerManager_Fire_ThenFactory_Inspect()
    {
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "sign_text",
            Type = "inspect",
            Nodes = new() { new DialogueNode { Id = "i1", Text = "Beware!" } },
            Routes = new() { new DialogueRoute { StartNode = "i1" } },
        };
        var tm = new TriggerManager();
        var evt = new TriggerEvent
        {
            Source = TriggerSource.StepOn,
            Properties = new() { ["dialogue_id"] = "sign_text" },
        };
        var result = tm.Fire(evt, gsm, MakeDialogues(dialogue));
        Assert.NotNull(result);

        var screen = DialogueScreenFactory.Create(result.Dialogue, gsm);
        Assert.IsType<InspectOverlay>(screen.Screen);
    }

    [Fact]
    public void TriggerManager_Fire_InlineDialogue_WorksWithFactory()
    {
        var gsm = CreateGSM();
        var tm = new TriggerManager();
        var evt = new TriggerEvent
        {
            Source = TriggerSource.StepOn,
            EntityId = "rock",
            Properties = new() { ["dialogue"] = "Something seems amiss about that rock..." },
        };
        var result = tm.Fire(evt, gsm, new Dictionary<string, DialogueData>());
        Assert.NotNull(result);
        Assert.Equal("Something seems amiss about that rock...", result.Dialogue.Nodes[0].Text);

        // Inline dialogues have no Type set, so default to conversation
        var screen = DialogueScreenFactory.Create(result.Dialogue, gsm);
        Assert.IsType<DialogueScreen>(screen.Screen);
    }

    [Fact]
    public void TriggerManager_Fire_OneShotBlocks()
    {
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "one_time",
            Type = "inspect",
            OneShot = true,
            Nodes = new() { new DialogueNode { Id = "o1", Text = "Once!" } },
            Routes = new() { new DialogueRoute { StartNode = "o1" } },
        };
        var tm = new TriggerManager();
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "one_time" },
        };

        // First fire succeeds
        var result1 = tm.Fire(evt, gsm, MakeDialogues(dialogue));
        Assert.NotNull(result1);

        // Simulate oneShot flag being set (would happen when screen dismisses)
        gsm.SetFlag("dialogue_shown:one_time");

        // Second fire blocked
        var result2 = tm.Fire(evt, gsm, MakeDialogues(dialogue));
        Assert.Null(result2);
    }

    [Fact]
    public void TriggerManager_Fire_PickupSource()
    {
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "sword_pickup",
            Type = "inspect",
            Nodes = new() { new DialogueNode { Id = "p1", Text = "You found the sword!" } },
            Routes = new() { new DialogueRoute { StartNode = "p1" } },
        };
        var tm = new TriggerManager();
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Pickup,
            EntityId = "magic_sword",
            Properties = new() { ["dialogue_id"] = "sword_pickup" },
        };
        var result = tm.Fire(evt, gsm, MakeDialogues(dialogue));
        Assert.NotNull(result);
    }
}
```

- [ ] **Step 2: Verify integration tests pass (TriggerManager already exists)**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~TriggerManagerIntegrationTests" --no-restore 2>&1 | tail -10`
Expected: All 6 tests PASS

- [ ] **Step 3: Refactor GameplayScreen to use TriggerManager**

In `GameplayScreen.cs`, add a TriggerManager field and replace TryShowDialogue/TryShowPickupDialogue.

Add field after `_activeBark` (~line 33):
```csharp
private readonly TriggerManager _triggerManager = new();
private readonly Dictionary<string, DialogueData> _dialogues;
```

In constructor, add after `_gameLog = gameLog;`:
```csharp
_dialogues = context.Dialogues;
```

Replace the entire `TryShowDialogue` method (lines 991-1024) with:
```csharp
    private bool TryShowDialogue(EntityInstance instance, PlayState play)
    {
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            EntityId = instance.Id,
            Properties = instance.Properties,
        };

        // Try TriggerManager first (handles dialogue_id, inline dialogue, oneShot, routes)
        var result = _triggerManager.Fire(evt, _gameStateManager, _dialogues);

        // Fallback: try loading dialogue by reference if not in pre-loaded dictionary
        if (result == null)
        {
            instance.Properties.TryGetValue("dialogue_id", out var dialogueId);
            if (!string.IsNullOrEmpty(dialogueId) && !_dialogues.ContainsKey(dialogueId))
            {
                var loaded = LoadDialogue(dialogueId);
                if (loaded != null)
                {
                    _dialogues[dialogueId] = loaded;
                    result = _triggerManager.Fire(evt, _gameStateManager, _dialogues);
                }
            }
        }

        if (result == null) return false;

        ShowDialogue(result.Dialogue, instance, play);
        return true;
    }
```

Replace the entire `TryShowPickupDialogue` method (lines 976-989) with:
```csharp
    private void TryShowPickupDialogue(EntityInstance instance, PlayState play)
    {
        instance.Properties.TryGetValue("on_pickup_dialogue", out var pickupDialogue);
        if (string.IsNullOrEmpty(pickupDialogue)) return;

        string flag = $"pickup_dialogue_shown:{instance.DefinitionName}";
        if (_gameStateManager.HasFlag(flag)) return;

        _gameStateManager.SetFlag(flag);

        // Build a trigger event with dialogue reference from the pickup property
        var props = new Dictionary<string, string>(instance.Properties);
        if (!props.ContainsKey("dialogue_id") && !props.ContainsKey("dialogue"))
            props["dialogue_id"] = pickupDialogue;

        var evt = new TriggerEvent
        {
            Source = TriggerSource.Pickup,
            EntityId = instance.Id,
            Properties = props,
        };

        var result = _triggerManager.Fire(evt, _gameStateManager, _dialogues);

        // Fallback: try loading or inline
        if (result == null)
        {
            var dialogue = LoadDialogue(pickupDialogue);
            dialogue ??= CreateInlineDialogue(instance.DefinitionName, pickupDialogue);
            ShowDialogue(dialogue, instance, play);
            return;
        }

        ShowDialogue(result.Dialogue, instance, play);
    }
```

Note: Keep `LoadDialogue()` and `CreateInlineDialogue()` as fallback methods — they're still needed for pickup dialogues that reference inline text strings rather than dialogue IDs.

- [ ] **Step 4: Run full test suite**

Run: `dotnet test TileForge.Tests --no-restore 2>&1 | tail -5`
Expected: All tests PASS

- [ ] **Step 5: Commit**

```bash
git add TileForge/Game/Screens/GameplayScreen.cs TileForge.Tests/Game/TriggerManagerIntegrationTests.cs
git commit -m "feat: wire TriggerManager into GameplayScreen, replace inline dialogue resolution"
```

---

## Chunk 2: Cross-Reference Validation

### Task 3: CrossReferenceValidator — Tests + Implementation

**Files:**
- Create: `TileForge/Game/CrossReferenceValidator.cs`
- Create: `TileForge.Tests/Game/CrossReferenceValidatorTests.cs`

- [ ] **Step 1: Write CrossReferenceValidator tests**

```csharp
using System.Collections.Generic;
using System.Linq;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class CrossReferenceValidatorTests
{
    private static DialogueData MakeDialogue(string id, params (string actionType, string value)[] actions)
    {
        var node = new DialogueNode
        {
            Id = "n1",
            Text = "Hello",
            Actions = actions.Select(a => new DialogueAction { Type = a.actionType, Value = a.value }).ToList(),
        };
        return new DialogueData
        {
            Id = id,
            Nodes = new() { node },
            Routes = new() { new DialogueRoute { StartNode = "n1" } },
        };
    }

    private static QuestDefinition MakeQuest(string id)
    {
        return new QuestDefinition
        {
            Id = id,
            Name = $"Quest: {id}",
            Objectives = new() { new QuestObjective { Description = "Do thing" } },
        };
    }

    [Fact]
    public void FindDialoguesReferencingQuest_FindsStartQuestAction()
    {
        var dialogues = new List<DialogueData>
        {
            MakeDialogue("d1", ("start_quest", "quest_a")),
            MakeDialogue("d2", ("set_flag", "some_flag")),
            MakeDialogue("d3", ("complete_objective", "quest_a:obj1")),
        };

        var refs = CrossReferenceValidator.FindDialoguesReferencingQuest("quest_a", dialogues);
        Assert.Equal(2, refs.Count);
        Assert.Contains(refs, r => r.DialogueId == "d1");
        Assert.Contains(refs, r => r.DialogueId == "d3");
    }

    [Fact]
    public void FindDialoguesReferencingQuest_NoReferences_ReturnsEmpty()
    {
        var dialogues = new List<DialogueData>
        {
            MakeDialogue("d1", ("set_flag", "some_flag")),
        };

        var refs = CrossReferenceValidator.FindDialoguesReferencingQuest("quest_b", dialogues);
        Assert.Empty(refs);
    }

    [Fact]
    public void FindQuestsReferencedByDialogue_FindsStartQuestActions()
    {
        var dialogue = MakeDialogue("d1", ("start_quest", "quest_a"), ("complete_objective", "quest_b:obj1"));
        var quests = new List<QuestDefinition>
        {
            MakeQuest("quest_a"),
            MakeQuest("quest_b"),
        };

        var refs = CrossReferenceValidator.FindQuestsReferencedByDialogue(dialogue, quests);
        Assert.Equal(2, refs.Count);
        Assert.Contains(refs, r => r.QuestId == "quest_a");
        Assert.Contains(refs, r => r.QuestId == "quest_b");
    }

    [Fact]
    public void FindBrokenQuestReferences_DetectsMissingQuest()
    {
        var dialogue = MakeDialogue("d1", ("start_quest", "nonexistent_quest"));
        var quests = new List<QuestDefinition> { MakeQuest("quest_a") };

        var broken = CrossReferenceValidator.FindBrokenQuestReferences(
            new List<DialogueData> { dialogue }, quests);
        Assert.Single(broken);
        Assert.Equal("d1", broken[0].DialogueId);
        Assert.Equal("nonexistent_quest", broken[0].ReferencedId);
    }

    [Fact]
    public void FindBrokenDialogueReferences_DetectsMissingDialogue()
    {
        // Entity references a dialogue_id that doesn't exist
        var dialogues = new List<DialogueData> { MakeDialogue("real_dialogue") };
        var entityRefs = new List<string> { "real_dialogue", "missing_dialogue" };

        var broken = CrossReferenceValidator.FindBrokenDialogueReferences(entityRefs, dialogues);
        Assert.Single(broken);
        Assert.Equal("missing_dialogue", broken[0]);
    }

    [Fact]
    public void FindBrokenQuestReferences_NoBrokenRefs_ReturnsEmpty()
    {
        var dialogue = MakeDialogue("d1", ("start_quest", "quest_a"));
        var quests = new List<QuestDefinition> { MakeQuest("quest_a") };

        var broken = CrossReferenceValidator.FindBrokenQuestReferences(
            new List<DialogueData> { dialogue }, quests);
        Assert.Empty(broken);
    }
}
```

- [ ] **Step 2: Verify tests fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~CrossReferenceValidatorTests" --no-restore 2>&1 | tail -5`
Expected: Build error — `CrossReferenceValidator` not found

- [ ] **Step 3: Implement CrossReferenceValidator**

```csharp
using System.Collections.Generic;
using System.Linq;

namespace TileForge.Game;

/// <summary>
/// Reference from a dialogue to a quest (via start_quest or complete_objective actions).
/// </summary>
public class QuestReference
{
    public string DialogueId { get; init; }
    public string NodeId { get; init; }
    public string ActionType { get; init; }
    public string QuestId { get; init; }
}

/// <summary>
/// Reference from a dialogue node with a broken quest reference.
/// </summary>
public class BrokenReference
{
    public string DialogueId { get; init; }
    public string NodeId { get; init; }
    public string ActionType { get; init; }
    public string ReferencedId { get; init; }
}

/// <summary>
/// Static helpers for validating cross-references between dialogues, quests, and entities.
/// </summary>
public static class CrossReferenceValidator
{
    /// <summary>
    /// Finds all dialogues that reference a specific quest via start_quest or complete_objective actions.
    /// </summary>
    public static List<QuestReference> FindDialoguesReferencingQuest(
        string questId, List<DialogueData> dialogues)
    {
        var refs = new List<QuestReference>();
        foreach (var dialogue in dialogues)
        {
            if (dialogue.Nodes == null) continue;
            foreach (var node in dialogue.Nodes)
            {
                if (node.Actions == null) continue;
                foreach (var action in node.Actions)
                {
                    if (action.Type == "start_quest" && action.Value == questId)
                    {
                        refs.Add(new QuestReference
                        {
                            DialogueId = dialogue.Id,
                            NodeId = node.Id,
                            ActionType = action.Type,
                            QuestId = questId,
                        });
                    }
                    else if (action.Type == "complete_objective" &&
                             action.Value != null && action.Value.StartsWith(questId + ":"))
                    {
                        refs.Add(new QuestReference
                        {
                            DialogueId = dialogue.Id,
                            NodeId = node.Id,
                            ActionType = action.Type,
                            QuestId = questId,
                        });
                    }
                }

                // Also check choice actions
                if (node.Choices != null)
                {
                    foreach (var choice in node.Choices)
                    {
                        if (choice.Actions == null) continue;
                        foreach (var action in choice.Actions)
                        {
                            if (action.Type == "start_quest" && action.Value == questId)
                            {
                                refs.Add(new QuestReference
                                {
                                    DialogueId = dialogue.Id,
                                    NodeId = node.Id,
                                    ActionType = action.Type,
                                    QuestId = questId,
                                });
                            }
                            else if (action.Type == "complete_objective" &&
                                     action.Value != null && action.Value.StartsWith(questId + ":"))
                            {
                                refs.Add(new QuestReference
                                {
                                    DialogueId = dialogue.Id,
                                    NodeId = node.Id,
                                    ActionType = action.Type,
                                    QuestId = questId,
                                });
                            }
                        }
                    }
                }
            }
        }
        return refs;
    }

    /// <summary>
    /// Finds all quests referenced by a specific dialogue's actions.
    /// </summary>
    public static List<QuestReference> FindQuestsReferencedByDialogue(
        DialogueData dialogue, List<QuestDefinition> quests)
    {
        var questIds = new HashSet<string>(quests.Select(q => q.Id));
        var refs = new List<QuestReference>();
        var seen = new HashSet<string>();

        if (dialogue.Nodes == null) return refs;

        foreach (var node in dialogue.Nodes)
        {
            foreach (var action in AllActionsInNode(node))
            {
                string questId = ExtractQuestId(action);
                if (questId != null && questIds.Contains(questId) && seen.Add(questId))
                {
                    refs.Add(new QuestReference
                    {
                        DialogueId = dialogue.Id,
                        NodeId = node.Id,
                        ActionType = action.Type,
                        QuestId = questId,
                    });
                }
            }
        }
        return refs;
    }

    /// <summary>
    /// Finds dialogue actions that reference quests that don't exist.
    /// </summary>
    public static List<BrokenReference> FindBrokenQuestReferences(
        List<DialogueData> dialogues, List<QuestDefinition> quests)
    {
        var questIds = new HashSet<string>(quests.Select(q => q.Id));
        var broken = new List<BrokenReference>();

        foreach (var dialogue in dialogues)
        {
            if (dialogue.Nodes == null) continue;
            foreach (var node in dialogue.Nodes)
            {
                foreach (var action in AllActionsInNode(node))
                {
                    string questId = ExtractQuestId(action);
                    if (questId != null && !questIds.Contains(questId))
                    {
                        broken.Add(new BrokenReference
                        {
                            DialogueId = dialogue.Id,
                            NodeId = node.Id,
                            ActionType = action.Type,
                            ReferencedId = questId,
                        });
                    }
                }
            }
        }
        return broken;
    }

    /// <summary>
    /// Finds dialogue_id references (from entities) that don't match any loaded dialogue.
    /// </summary>
    public static List<string> FindBrokenDialogueReferences(
        List<string> entityDialogueIds, List<DialogueData> dialogues)
    {
        var dialogueIds = new HashSet<string>(dialogues.Select(d => d.Id));
        return entityDialogueIds.Where(id => !dialogueIds.Contains(id)).ToList();
    }

    private static IEnumerable<DialogueAction> AllActionsInNode(DialogueNode node)
    {
        if (node.Actions != null)
            foreach (var a in node.Actions) yield return a;
        if (node.Choices != null)
            foreach (var choice in node.Choices)
                if (choice.Actions != null)
                    foreach (var a in choice.Actions) yield return a;
    }

    private static string ExtractQuestId(DialogueAction action)
    {
        if (action.Type == "start_quest")
            return action.Value;
        if (action.Type == "complete_objective" && action.Value != null)
        {
            int colonIdx = action.Value.IndexOf(':');
            return colonIdx > 0 ? action.Value.Substring(0, colonIdx) : action.Value;
        }
        return null;
    }
}
```

- [ ] **Step 4: Run CrossReferenceValidator tests**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~CrossReferenceValidatorTests" --no-restore 2>&1 | tail -10`
Expected: All 6 tests PASS

- [ ] **Step 5: Commit**

```bash
git add TileForge/Game/CrossReferenceValidator.cs TileForge.Tests/Game/CrossReferenceValidatorTests.cs
git commit -m "feat: add CrossReferenceValidator for dialogue/quest cross-reference validation"
```

---

### Task 4: Wire Cross-References into QuestWorkspace

**Files:**
- Modify: `TileForge/UI/QuestWorkspace.cs`
- Modify: `TileForge/UI/QuestListPanel.cs`

This task adds a "Referenced by N dialogues" label next to each quest in the quest list, and shows warnings for broken references.

- [ ] **Step 1: Add reference count to QuestListPanel**

In `QuestListPanel.cs`, add a method or property that accepts reference counts from the workspace:

```csharp
/// <summary>
/// Map of quest ID → number of dialogues referencing it. Set by QuestWorkspace.
/// </summary>
public Dictionary<string, int> ReferenceCounts { get; set; }
```

In the Draw method where quest names are rendered, after the quest name, if `ReferenceCounts` has an entry, draw the count:

```csharp
// After drawing quest name:
if (ReferenceCounts != null && ReferenceCounts.TryGetValue(quest.Id, out int refCount) && refCount > 0)
{
    string refText = $"({refCount} refs)";
    var refSize = font.MeasureString(refText);
    spriteBatch.DrawString(font, refText,
        new Vector2(nameX + nameSize.X + 8, nameY), Color.Gray);
}
```

- [ ] **Step 2: Compute reference counts in QuestWorkspace**

In `QuestWorkspace.cs`, in the `OnEnter()` method or `Update()`, compute reference counts:

```csharp
public void OnEnter(EditorState state)
{
    // Compute cross-reference counts for quest list
    var refCounts = new Dictionary<string, int>();
    foreach (var quest in state.Quests)
    {
        var refs = CrossReferenceValidator.FindDialoguesReferencingQuest(quest.Id, state.Dialogues);
        if (refs.Count > 0)
            refCounts[quest.Id] = refs.Count;
    }
    _listPanel.ReferenceCounts = refCounts;
}
```

Add the `using TileForge.Game;` import at the top.

- [ ] **Step 3: Run full test suite**

Run: `dotnet test TileForge.Tests --no-restore 2>&1 | tail -5`
Expected: All tests PASS

- [ ] **Step 4: Commit**

```bash
git add TileForge/UI/QuestWorkspace.cs TileForge/UI/QuestListPanel.cs
git commit -m "feat: show dialogue reference counts in quest list"
```

---

### Task 5: Wire Cross-References into DialogueWorkspace

**Files:**
- Modify: `TileForge/UI/DialogueWorkspace.cs`

This task adds a warning after saving a dialogue if it references quests that don't exist.

- [ ] **Step 1: Add validation after save in HandleEditorResult**

In `DialogueWorkspace.cs`, in `HandleEditorResult()` after the save succeeds (~line 179), add:

```csharp
        _saveDialogue(result);
        state.NotifyDialoguesChanged();

        // Validate cross-references after save
        var brokenRefs = CrossReferenceValidator.FindBrokenQuestReferences(
            new List<DialogueData> { result }, state.Quests);
        if (brokenRefs.Count > 0)
        {
            _lastSaveWarning = $"Warning: {brokenRefs.Count} broken quest reference(s)";
            _warningTimer = 5f; // show for 5 seconds
        }
        else
        {
            _lastSaveWarning = null;
        }
```

Add fields:
```csharp
    private string _lastSaveWarning;
    private float _warningTimer;
```

In `Update()`, tick the warning timer:
```csharp
        if (_warningTimer > 0)
        {
            _warningTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_warningTimer <= 0)
                _lastSaveWarning = null;
        }
```

In `Draw()`, show the warning text if set:
```csharp
        if (!string.IsNullOrEmpty(_lastSaveWarning))
        {
            var warnSize = font.MeasureString(_lastSaveWarning);
            spriteBatch.DrawString(font, _lastSaveWarning,
                new Vector2(canvasBounds.X + 8, canvasBounds.Bottom - 24),
                Color.Orange);
        }
```

Add `using TileForge.Game;` import.

- [ ] **Step 2: Run full test suite**

Run: `dotnet test TileForge.Tests --no-restore 2>&1 | tail -5`
Expected: All tests PASS

- [ ] **Step 3: Commit**

```bash
git add TileForge/UI/DialogueWorkspace.cs
git commit -m "feat: show broken quest reference warnings after dialogue save"
```

---

## Post-Phase Verification

- [ ] **Run full test suite**
Run: `dotnet test TileForge.Tests --no-restore`
Expected: All tests pass

- [ ] **Manual smoke test**
1. Run the app, enter play mode, interact with an NPC — dialogue should still work (now via TriggerManager)
2. Switch to Quest workspace — quest list should show "(N refs)" next to quests referenced by dialogues
3. In Dialogue workspace, save a dialogue with a `start_quest` action for a nonexistent quest — should see orange warning
4. Add the quest, re-save — warning should disappear
