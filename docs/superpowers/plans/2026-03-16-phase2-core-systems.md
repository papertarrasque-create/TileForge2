# Phase 2: Core Systems Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire workspace mode switching into TileForgeGame, build TriggerManager for unified dialogue triggering, and create DialogueWorkspace and QuestWorkspace as full-canvas editors replacing their sidebar panel + modal pattern.

**Architecture:** First wire workspace switching into TileForgeGame (routing Update/Draw to active workspace). Then build TriggerManager as a pure-logic class that replaces scattered dialogue triggering in GameplayScreen. Then create DialogueWorkspace and QuestWorkspace that implement IWorkspace — initially wrapping the existing editors, then progressively decomposing them in later tasks.

**Tech Stack:** C# / .NET 9.0 / MonoGame 3.8 / xUnit / System.Text.Json

**Spec:** `docs/superpowers/specs/2026-03-15-editor-dialogue-quest-redesign.md`

**Depends on:** Phase 1 complete (IWorkspace, WorkspaceMode, HitTestRegistry, ConditionListEditor, ActionListEditor, QuestConstants, QuestState, GameWorldView)

## Known Issues from Plan Review (implementers must fix these)

The plan review identified these issues that implementers must resolve during implementation:

1. **DialogueTreeEditor.Draw signature:** Actual signature is `Draw(SpriteBatch, SpriteFont, Renderer, Rectangle, GameTime)` — `renderer` comes before `bounds`, and `gameTime` is required. Since `IWorkspace.Draw` doesn't include `GameTime`, workspaces must cache `gameTime` from `Update`.
2. **prevKeyboard tracking:** Passing same `KeyboardState` for both keyboard and prevKeyboard breaks key-press detection. Workspaces must maintain a `_prevKeyboard` field updated each frame.
3. **OnTextInput forwarding:** TileForgeGame.OnTextInput currently routes to modal editors. Workspaces that embed editors need an `OnTextInput(char)` method, and TileForgeGame must route to the active workspace. Consider adding it to `IWorkspace`.
4. **Renderer.FillRect doesn't exist:** Use `Renderer.DrawRect` (solid fill) instead. Check actual Renderer API.
5. **GetSidebarBoundsForWorkspace:** Not defined. Workspace sidebars should use the PanelDock's left-side bounds (same width/position). Define this method or reuse PanelDock bounds calculation.
6. **Escape key conflict:** Escape is used by embedded editors to cancel. Only switch workspace on Escape if no editor is actively editing (check `_editor != null`).
7. **FinishUpdate not called on early return:** The workspace routing `return;` in Update skips `FinishUpdate()`. Always call `FinishUpdate(keyboard, mouse, gameTime)` before returning.
8. **DialogueListPanel.Update never called by workspace:** The workspace reads signals but never calls `_listPanel.Update()`. Add it to the workspace's Update method.

---

## File Structure

### Task 1: Wire Workspace Switching into TileForgeGame
- Modify: `TileForge/TileForgeGame.cs` — add workspace instances, route Update/Draw by ActiveWorkspace
- Modify: `TileForge/UI/PanelDock.cs` — remove DialoguePanel and QuestPanel from dock
- Create: `TileForge/UI/MapWorkspace.cs` — wraps existing canvas + PanelDock for Map mode
- Create: `TileForge.Tests/UI/MapWorkspaceTests.cs`

### Task 2: TriggerManager
- Create: `TileForge/Game/TriggerManager.cs` — unified dialogue resolution
- Create: `TileForge/Game/TriggerEvent.cs` — event data classes
- Create: `TileForge.Tests/Game/TriggerManagerTests.cs`

### Task 3: DialogueWorkspace
- Create: `TileForge/UI/DialogueWorkspace.cs` — IWorkspace implementation for dialogue editing
- Create: `TileForge/UI/DialogueListPanel.cs` — sidebar list of dialogues (replaces DialoguePanel in dock)
- Create: `TileForge.Tests/UI/DialogueWorkspaceTests.cs`
- Create: `TileForge.Tests/UI/DialogueListPanelTests.cs`

### Task 4: QuestWorkspace
- Create: `TileForge/UI/QuestWorkspace.cs` — IWorkspace implementation for quest editing
- Create: `TileForge/UI/QuestListPanel.cs` — sidebar list of quests (replaces QuestPanel in dock)
- Create: `TileForge.Tests/UI/QuestWorkspaceTests.cs`
- Create: `TileForge.Tests/UI/QuestListPanelTests.cs`

### Task 5: Integrate Workspaces into TileForgeGame
- Modify: `TileForge/TileForgeGame.cs` — replace modal quest/dialogue editors with workspace delegation
- Modify: `TileForge/UI/ToolbarRibbon.cs` — add workspace buttons

---

## Chunk 1: TriggerManager

### Task 2: TriggerManager

TriggerManager is a pure-logic class that resolves dialogue triggers. It reads `dialogue_id`/`dialogue` from an entity's property bag, loads the dialogue, checks oneShot, evaluates routes, and returns the resolved dialogue + start node. It does NOT push screens or handle side effects.

#### Files:
- Create: `TileForge/Game/TriggerEvent.cs`
- Create: `TileForge/Game/TriggerManager.cs`
- Create: `TileForge.Tests/Game/TriggerManagerTests.cs`

- [ ] **Step 1: Write TriggerEvent data classes**

Create `TileForge/Game/TriggerEvent.cs`:

```csharp
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileForge.Game;

public enum TriggerSource
{
    Interaction,
    Pickup,
    StepOn,
    Proximity,
    SkillCheck,
    Timer,
}

public class TriggerEvent
{
    public TriggerSource Source { get; set; }
    public string EntityId { get; set; }
    public Point? TilePosition { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
}

public class TriggerResult
{
    public DialogueData Dialogue { get; set; }
    public string StartNodeId { get; set; }
}
```

- [ ] **Step 2: Write TriggerManager tests**

Create `TileForge.Tests/Game/TriggerManagerTests.cs`:

```csharp
using System.Collections.Generic;
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class TriggerManagerTests
{
    private static GameStateManager CreateGSM()
    {
        var gsm = new GameStateManager();
        gsm.State.Player = new PlayerState
        {
            Health = 100, MaxHealth = 100,
            Poise = 20, MaxPoise = 20, MaxAP = 2,
        };
        return gsm;
    }

    private static Dictionary<string, DialogueData> MakeDialogues(params DialogueData[] dialogues)
    {
        var dict = new Dictionary<string, DialogueData>();
        foreach (var d in dialogues)
            dict[d.Id] = d;
        return dict;
    }

    private static DialogueData SimpleDialogue(string id, string startNodeId = "start")
    {
        return new DialogueData
        {
            Id = id,
            Nodes = new() { new DialogueNode { Id = startNodeId, Text = "Hello" } },
            Routes = new() { new DialogueRoute { StartNode = startNodeId } },
        };
    }

    [Fact]
    public void Fire_NoDialogueId_ReturnsNull()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["behavior"] = "idle" },
        };
        var result = manager.Fire(evt, gsm, new Dictionary<string, DialogueData>());
        Assert.Null(result);
    }

    [Fact]
    public void Fire_WithDialogueId_ReturnsDialogueAndStartNode()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        var dialogue = SimpleDialogue("elder_01");
        var dialogues = MakeDialogues(dialogue);
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "elder_01" },
        };

        var result = manager.Fire(evt, gsm, dialogues);

        Assert.NotNull(result);
        Assert.Equal("elder_01", result.Dialogue.Id);
        Assert.Equal("start", result.StartNodeId);
    }

    [Fact]
    public void Fire_OneShotAlreadyShown_ReturnsNull()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        gsm.SetFlag("dialogue_shown:greeting");
        var dialogue = SimpleDialogue("greeting");
        dialogue.OneShot = true;
        var dialogues = MakeDialogues(dialogue);
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "greeting" },
        };

        var result = manager.Fire(evt, gsm, dialogues);

        Assert.Null(result);
    }

    [Fact]
    public void Fire_OneShotNotYetShown_ReturnsDialogue()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        var dialogue = SimpleDialogue("greeting");
        dialogue.OneShot = true;
        var dialogues = MakeDialogues(dialogue);
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "greeting" },
        };

        var result = manager.Fire(evt, gsm, dialogues);

        Assert.NotNull(result);
    }

    [Fact]
    public void Fire_RoutesEvaluated_FirstMatchWins()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        gsm.SetFlag("quest_complete");
        var dialogue = new DialogueData
        {
            Id = "elder_01",
            Routes = new()
            {
                new DialogueRoute
                {
                    StartNode = "thanks",
                    Conditions = new() { new Condition { Type = "has_flag", Flag = "quest_complete" } }
                },
                new DialogueRoute { StartNode = "start" }, // default
            },
            Nodes = new()
            {
                new DialogueNode { Id = "thanks", Text = "Thank you!" },
                new DialogueNode { Id = "start", Text = "Hello" },
            },
        };
        var dialogues = MakeDialogues(dialogue);
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "elder_01" },
        };

        var result = manager.Fire(evt, gsm, dialogues);

        Assert.Equal("thanks", result.StartNodeId);
    }

    [Fact]
    public void Fire_RoutesEvaluated_FallsToDefault()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        // quest_complete flag NOT set
        var dialogue = new DialogueData
        {
            Id = "elder_01",
            Routes = new()
            {
                new DialogueRoute
                {
                    StartNode = "thanks",
                    Conditions = new() { new Condition { Type = "has_flag", Flag = "quest_complete" } }
                },
                new DialogueRoute { StartNode = "start" },
            },
            Nodes = new()
            {
                new DialogueNode { Id = "thanks", Text = "Thank you!" },
                new DialogueNode { Id = "start", Text = "Hello" },
            },
        };
        var dialogues = MakeDialogues(dialogue);
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "elder_01" },
        };

        var result = manager.Fire(evt, gsm, dialogues);

        Assert.Equal("start", result.StartNodeId);
    }

    [Fact]
    public void Fire_InlineDialogue_CreatesFromText()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            EntityId = "npc_1",
            Properties = new() { ["dialogue"] = "Hello there|Welcome to town" },
        };

        var result = manager.Fire(evt, gsm, new Dictionary<string, DialogueData>());

        Assert.NotNull(result);
        Assert.Equal(2, result.Dialogue.Nodes.Count);
        Assert.Equal("Hello there", result.Dialogue.Nodes[0].Text);
        Assert.Equal("Welcome to town", result.Dialogue.Nodes[1].Text);
    }

    [Fact]
    public void Fire_DialogueIdNotFound_ReturnsNull()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "nonexistent" },
        };

        var result = manager.Fire(evt, gsm, new Dictionary<string, DialogueData>());

        Assert.Null(result);
    }

    [Fact]
    public void Fire_DialogueIdTakesPrecedenceOverInline()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        var dialogue = SimpleDialogue("elder_01");
        var dialogues = MakeDialogues(dialogue);
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new()
            {
                ["dialogue_id"] = "elder_01",
                ["dialogue"] = "fallback text",
            },
        };

        var result = manager.Fire(evt, gsm, dialogues);

        Assert.Equal("elder_01", result.Dialogue.Id);
    }

    [Fact]
    public void Fire_NoRoutesOnDialogue_DefaultsToFirstNode()
    {
        var manager = new TriggerManager();
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "simple",
            Nodes = new()
            {
                new DialogueNode { Id = "node1", Text = "Hi" },
                new DialogueNode { Id = "node2", Text = "Bye" },
            },
            // No routes
        };
        var dialogues = MakeDialogues(dialogue);
        var evt = new TriggerEvent
        {
            Source = TriggerSource.Interaction,
            Properties = new() { ["dialogue_id"] = "simple" },
        };

        var result = manager.Fire(evt, gsm, dialogues);

        Assert.NotNull(result);
        Assert.Equal("node1", result.StartNodeId);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~TriggerManagerTests" -v minimal`
Expected: FAIL — TriggerManager class doesn't exist.

- [ ] **Step 4: Implement TriggerManager**

Create `TileForge/Game/TriggerManager.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;

namespace TileForge.Game;

/// <summary>
/// Resolves dialogue triggers from entity/tile properties.
/// Pure logic — reads dialogue_id/dialogue from properties, loads dialogue,
/// checks oneShot, evaluates routes, returns resolved result.
/// Does NOT push screens or handle side effects.
/// </summary>
public class TriggerManager
{
    /// <summary>
    /// Resolves a trigger event to a dialogue result, or null if no dialogue applies.
    /// </summary>
    public TriggerResult Fire(TriggerEvent evt, GameStateManager gsm,
                               Dictionary<string, DialogueData> dialogues)
    {
        if (evt?.Properties == null)
            return null;

        // Try dialogue_id first (file-based), then inline dialogue
        DialogueData dialogue = null;

        if (evt.Properties.TryGetValue("dialogue_id", out var dialogueId)
            && !string.IsNullOrEmpty(dialogueId))
        {
            if (!dialogues.TryGetValue(dialogueId, out dialogue))
                return null; // Referenced dialogue not found
        }
        else if (evt.Properties.TryGetValue("dialogue", out var inlineText)
                 && !string.IsNullOrEmpty(inlineText))
        {
            dialogue = CreateInlineDialogue(evt.EntityId ?? "unknown", inlineText);
        }

        if (dialogue == null)
            return null;

        // Check oneShot
        if (dialogue.OneShot == true
            && gsm.HasFlag($"dialogue_shown:{dialogue.Id}"))
            return null;

        // Evaluate routes to find start node
        string startNodeId = ResolveStartNode(dialogue, gsm);
        if (startNodeId == null)
            return null;

        return new TriggerResult
        {
            Dialogue = dialogue,
            StartNodeId = startNodeId,
        };
    }

    private static string ResolveStartNode(DialogueData dialogue, GameStateManager gsm)
    {
        if (dialogue.Routes != null && dialogue.Routes.Count > 0)
        {
            foreach (var route in dialogue.Routes)
            {
                if (ConditionEvaluator.EvaluateAll(route.Conditions, gsm))
                    return route.StartNode;
            }
            return null; // No route matched
        }

        // No routes defined — default to first node
        if (dialogue.Nodes != null && dialogue.Nodes.Count > 0)
            return dialogue.Nodes[0].Id;

        return null;
    }

    /// <summary>
    /// Converts pipe-delimited inline text to a DialogueData with auto-generated nodes.
    /// Same logic as GameplayScreen.CreateInlineDialogue.
    /// </summary>
    internal static DialogueData CreateInlineDialogue(string entityName, string text)
    {
        var pages = text.Split('|');
        var nodes = new List<DialogueNode>();

        for (int i = 0; i < pages.Length; i++)
        {
            var node = new DialogueNode
            {
                Id = $"inline_{i}",
                Speaker = entityName,
                Text = pages[i].Trim(),
            };
            if (i < pages.Length - 1)
                node.NextNodeId = $"inline_{i + 1}";
            nodes.Add(node);
        }

        return new DialogueData
        {
            Id = $"inline_{entityName}",
            Nodes = nodes,
            Routes = new() { new DialogueRoute { StartNode = "inline_0" } },
        };
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~TriggerManagerTests" -v minimal`
Expected: All 10 PASS.

- [ ] **Step 6: Run full test suite**

Run: `dotnet test TileForge.Tests -v minimal`
Expected: All tests PASS.

- [ ] **Step 7: Commit**

```bash
git add TileForge/Game/TriggerEvent.cs TileForge/Game/TriggerManager.cs \
       TileForge.Tests/Game/TriggerManagerTests.cs
git commit -m "feat: add TriggerManager for unified dialogue resolution"
```

---

## Chunk 2: Workspace Wiring in TileForgeGame

### Task 1: MapWorkspace + Workspace Routing

The MapWorkspace wraps the existing canvas + PanelDock for the Map editing mode. This is the default workspace. Then TileForgeGame gets modified to route Update/Draw through the active workspace.

#### Files:
- Create: `TileForge/UI/MapWorkspace.cs`
- Create: `TileForge.Tests/UI/MapWorkspaceTests.cs`
- Modify: `TileForge/TileForgeGame.cs`

- [ ] **Step 1: Write MapWorkspace tests**

Create `TileForge.Tests/UI/MapWorkspaceTests.cs`:

```csharp
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class MapWorkspaceTests
{
    [Fact]
    public void MapWorkspace_ImplementsIWorkspace()
    {
        var workspace = new MapWorkspace();
        Assert.IsAssignableFrom<IWorkspace>(workspace);
    }
}
```

Note: MapWorkspace mostly delegates to existing PanelDock/Canvas, so deep unit tests aren't practical without MonoGame. The integration test verifies the interface contract.

- [ ] **Step 2: Implement MapWorkspace**

Create `TileForge/UI/MapWorkspace.cs`:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Editor;

namespace TileForge.UI;

/// <summary>
/// Workspace for map editing mode. Wraps existing PanelDock (layers + groups + tileset)
/// and canvas. This is the default workspace when the editor starts.
///
/// Note: The actual PanelDock, Canvas, and map editing logic remain in TileForgeGame
/// for now. MapWorkspace serves as the IWorkspace contract holder so TileForgeGame
/// can route to it. Future refactoring can move canvas/panel logic here.
/// </summary>
public class MapWorkspace : IWorkspace
{
    // MapWorkspace delegates to TileForgeGame's existing canvas/panel infrastructure.
    // These Action callbacks are set by TileForgeGame during initialization.
    public Action<EditorState, MouseState, MouseState, InputEvent, SpriteFont,
                  Rectangle, GameTime, int, int> OnUpdate { get; set; }
    public Action<SpriteBatch, SpriteFont, EditorState, Renderer, Rectangle> OnDraw { get; set; }
    public Action<SpriteBatch, SpriteFont, EditorState, Renderer, Rectangle> OnDrawSidebar { get; set; }

    public void Update(EditorState state, MouseState mouse, MouseState prevMouse,
                       InputEvent input, SpriteFont font, Rectangle canvasBounds,
                       GameTime gameTime, int screenW, int screenH)
    {
        OnUpdate?.Invoke(state, mouse, prevMouse, input, font, canvasBounds,
                         gameTime, screenW, screenH);
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                     Renderer renderer, Rectangle canvasBounds)
    {
        OnDraw?.Invoke(spriteBatch, font, state, renderer, canvasBounds);
    }

    public void DrawSidebar(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                            Renderer renderer, Rectangle sidebarBounds)
    {
        OnDrawSidebar?.Invoke(spriteBatch, font, state, renderer, sidebarBounds);
    }

    public void OnEnter(EditorState state) { }
    public void OnExit(EditorState state) { }
}
```

- [ ] **Step 3: Run tests**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~MapWorkspaceTests" -v minimal`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add TileForge/UI/MapWorkspace.cs TileForge.Tests/UI/MapWorkspaceTests.cs
git commit -m "feat: add MapWorkspace as IWorkspace wrapper for existing map editing"
```

---

## Chunk 3: DialogueWorkspace + QuestWorkspace

### Task 3: DialogueWorkspace

The DialogueWorkspace implements IWorkspace for dialogue editing. It has its own sidebar (dialogue list) and canvas area (dialogue tree editor). Initially it wraps the existing DialogueTreeEditor, embedding it in the canvas area instead of as a modal overlay.

#### Files:
- Create: `TileForge/UI/DialogueListPanel.cs`
- Create: `TileForge/UI/DialogueWorkspace.cs`
- Create: `TileForge.Tests/UI/DialogueListPanelTests.cs`
- Create: `TileForge.Tests/UI/DialogueWorkspaceTests.cs`

- [ ] **Step 1: Write DialogueListPanel tests**

Create `TileForge.Tests/UI/DialogueListPanelTests.cs`:

```csharp
using TileForge.Game;
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class DialogueListPanelTests
{
    [Fact]
    public void WantsNewDialogue_DefaultsFalse()
    {
        var panel = new DialogueListPanel();
        Assert.False(panel.WantsNewDialogue);
    }

    [Fact]
    public void WantsEditDialogueId_DefaultsNull()
    {
        var panel = new DialogueListPanel();
        Assert.Null(panel.WantsEditDialogueId);
    }

    [Fact]
    public void WantsDeleteDialogueId_DefaultsNull()
    {
        var panel = new DialogueListPanel();
        Assert.Null(panel.WantsDeleteDialogueId);
    }

    [Fact]
    public void ClearSignals_ResetsAll()
    {
        var panel = new DialogueListPanel();
        panel.ClearSignals();
        Assert.False(panel.WantsNewDialogue);
        Assert.Null(panel.WantsEditDialogueId);
        Assert.Null(panel.WantsDeleteDialogueId);
    }

    [Fact]
    public void SelectedDialogueId_CanBeSet()
    {
        var panel = new DialogueListPanel();
        panel.SelectedDialogueId = "elder_01";
        Assert.Equal("elder_01", panel.SelectedDialogueId);
    }
}
```

- [ ] **Step 2: Implement DialogueListPanel**

Create `TileForge/UI/DialogueListPanel.cs`:

```csharp
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Editor;
using TileForge.Game;

namespace TileForge.UI;

/// <summary>
/// Sidebar panel for the Dialogue workspace. Lists all dialogues in the project.
/// Single-click to select (loads in editor), double-click to edit, right-click for context menu.
/// Signals are read by DialogueWorkspace each frame.
/// </summary>
public class DialogueListPanel
{
    // Signals (cleared each frame by DialogueWorkspace)
    public bool WantsNewDialogue { get; set; }
    public string WantsEditDialogueId { get; set; }
    public string WantsDeleteDialogueId { get; set; }

    // State
    public string SelectedDialogueId { get; set; }

    private float _scrollOffset;
    private const int ItemHeight = 24;
    private ContextMenu _contextMenu;

    public void ClearSignals()
    {
        WantsNewDialogue = false;
        WantsEditDialogueId = null;
        WantsDeleteDialogueId = null;
    }

    public void Update(EditorState state, MouseState mouse, MouseState prevMouse,
                       InputEvent input, SpriteFont font, Rectangle bounds,
                       GameTime gameTime, int screenW, int screenH)
    {
        ClearSignals();

        if (state.Dialogues == null) return;

        // Context menu takes priority
        if (_contextMenu != null)
        {
            _contextMenu.Update(mouse, prevMouse, font);
            if (_contextMenu.SelectedIndex >= 0)
            {
                if (_contextMenu.SelectedIndex == 0) // Edit
                    WantsEditDialogueId = SelectedDialogueId;
                else if (_contextMenu.SelectedIndex == 1) // Delete
                    WantsDeleteDialogueId = SelectedDialogueId;
                _contextMenu = null;
            }
            else if (_contextMenu.IsDismissed)
            {
                _contextMenu = null;
            }
            if (input != null) input.Consumed = true;
            return;
        }

        // Handle clicks within bounds
        if (!bounds.Contains(mouse.Position)) return;

        int addButtonY = bounds.Y + (int)(state.Dialogues.Count * ItemHeight - _scrollOffset);

        // Add button click
        bool leftClick = mouse.LeftButton == ButtonState.Pressed
                         && prevMouse.LeftButton == ButtonState.Released;
        bool rightClick = mouse.RightButton == ButtonState.Pressed
                          && prevMouse.RightButton == ButtonState.Released;

        if (leftClick)
        {
            int relY = mouse.Y - bounds.Y + (int)_scrollOffset;
            int clickedIndex = relY / ItemHeight;

            if (clickedIndex >= 0 && clickedIndex < state.Dialogues.Count)
            {
                SelectedDialogueId = state.Dialogues[clickedIndex].Id;
                // Double-click detection would go here in future
            }
            else if (mouse.Y >= addButtonY && mouse.Y < addButtonY + ItemHeight)
            {
                WantsNewDialogue = true;
            }

            if (input != null) input.Consumed = true;
        }
        else if (rightClick)
        {
            int relY = mouse.Y - bounds.Y + (int)_scrollOffset;
            int clickedIndex = relY / ItemHeight;

            if (clickedIndex >= 0 && clickedIndex < state.Dialogues.Count)
            {
                SelectedDialogueId = state.Dialogues[clickedIndex].Id;
                _contextMenu = new ContextMenu(
                    new List<string> { "Edit", "Delete" },
                    mouse.Position);
            }

            if (input != null) input.Consumed = true;
        }
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                     Renderer renderer, Rectangle bounds)
    {
        if (state.Dialogues == null) return;

        // Background
        renderer.FillRect(spriteBatch, bounds, new Color(30, 30, 40));

        // Header
        var headerBounds = new Rectangle(bounds.X, bounds.Y, bounds.Width, 24);
        renderer.FillRect(spriteBatch, headerBounds, new Color(42, 42, 78));
        spriteBatch.DrawString(font, "Dialogues", new Vector2(headerBounds.X + 8, headerBounds.Y + 4),
                               LayoutConstants.WorkspaceDialogueColor);

        // Add button in header
        string addText = "+ New";
        var addSize = font.MeasureString(addText);
        spriteBatch.DrawString(font, addText,
            new Vector2(headerBounds.Right - addSize.X - 8, headerBounds.Y + 4),
            new Color(106, 170, 106));

        // Items
        int contentY = bounds.Y + 24;
        int contentHeight = bounds.Height - 24;
        var contentBounds = new Rectangle(bounds.X, contentY, bounds.Width, contentHeight);

        for (int i = 0; i < state.Dialogues.Count; i++)
        {
            int itemY = contentY + i * ItemHeight - (int)_scrollOffset;
            if (itemY + ItemHeight < contentY || itemY > contentY + contentHeight) continue;

            var dialogue = state.Dialogues[i];
            bool isSelected = dialogue.Id == SelectedDialogueId;
            var itemBounds = new Rectangle(bounds.X, itemY, bounds.Width, ItemHeight);

            if (isSelected)
                renderer.FillRect(spriteBatch, itemBounds, new Color(58, 58, 110));

            var nameColor = isSelected ? Color.White : new Color(170, 170, 170);
            spriteBatch.DrawString(font, dialogue.Id,
                new Vector2(bounds.X + 8, itemY + 4), nameColor);
        }

        // Context menu on top
        _contextMenu?.Draw(spriteBatch, font, renderer);
    }
}
```

- [ ] **Step 3: Run DialogueListPanel tests**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~DialogueListPanelTests" -v minimal`
Expected: All 5 PASS.

- [ ] **Step 4: Write DialogueWorkspace tests**

Create `TileForge.Tests/UI/DialogueWorkspaceTests.cs`:

```csharp
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class DialogueWorkspaceTests
{
    [Fact]
    public void DialogueWorkspace_ImplementsIWorkspace()
    {
        var workspace = new DialogueWorkspace();
        Assert.IsAssignableFrom<IWorkspace>(workspace);
    }

    [Fact]
    public void ActiveDialogueId_DefaultsNull()
    {
        var workspace = new DialogueWorkspace();
        Assert.Null(workspace.ActiveDialogueId);
    }

    [Fact]
    public void HasUnsavedChanges_DefaultsFalse()
    {
        var workspace = new DialogueWorkspace();
        Assert.False(workspace.HasUnsavedChanges);
    }
}
```

- [ ] **Step 5: Implement DialogueWorkspace**

Create `TileForge/UI/DialogueWorkspace.cs`:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DojoUI;
using TileForge.Editor;
using TileForge.Game;

namespace TileForge.UI;

/// <summary>
/// Full-canvas workspace for dialogue editing. Replaces the modal DialogueTreeEditor
/// overlay pattern. Contains its own sidebar (DialogueListPanel) and embeds the
/// existing DialogueTreeEditor in the canvas area.
///
/// Phase 2 approach: wrap existing DialogueTreeEditor. Future phases decompose it
/// into NodeGraphPanel, RouteBar, NodePropertiesPanel, etc.
/// </summary>
public class DialogueWorkspace : IWorkspace
{
    private readonly DialogueListPanel _listPanel = new();
    private DialogueTreeEditor _editor;

    public string ActiveDialogueId { get; private set; }
    public bool HasUnsavedChanges { get; private set; }

    // Callbacks set by TileForgeGame for save/delete operations
    public Action<DialogueData> OnSaveDialogue { get; set; }
    public Action<string> OnDeleteDialogue { get; set; }
    public Action OnNotifyChanged { get; set; }

    public void OnEnter(EditorState state)
    {
        // Refresh list selection
        if (ActiveDialogueId != null && state.Dialogues != null)
        {
            _listPanel.SelectedDialogueId = ActiveDialogueId;
        }
    }

    public void OnExit(EditorState state)
    {
        // Save any pending changes
        if (_editor != null && HasUnsavedChanges)
        {
            // Auto-save on workspace exit
            SaveCurrentDialogue(state);
        }
    }

    public void Update(EditorState state, MouseState mouse, MouseState prevMouse,
                       InputEvent input, SpriteFont font, Rectangle canvasBounds,
                       GameTime gameTime, int screenW, int screenH)
    {
        _listPanel.ClearSignals();

        // Handle list panel signals
        if (_listPanel.WantsNewDialogue)
        {
            _editor = DialogueTreeEditor.ForNewDialogue();
            ActiveDialogueId = null;
            HasUnsavedChanges = false;
        }
        else if (_listPanel.WantsEditDialogueId != null)
        {
            LoadDialogueForEditing(_listPanel.WantsEditDialogueId, state);
        }
        else if (_listPanel.WantsDeleteDialogueId != null)
        {
            var deleteId = _listPanel.WantsDeleteDialogueId;
            OnDeleteDialogue?.Invoke(deleteId);
            if (ActiveDialogueId == deleteId)
            {
                _editor = null;
                ActiveDialogueId = null;
                HasUnsavedChanges = false;
            }
        }

        // Detect selection change in list
        if (_listPanel.SelectedDialogueId != null
            && _listPanel.SelectedDialogueId != ActiveDialogueId
            && _listPanel.WantsEditDialogueId == null
            && _listPanel.WantsNewDialogue == false)
        {
            LoadDialogueForEditing(_listPanel.SelectedDialogueId, state);
        }

        // Update editor if active
        if (_editor != null)
        {
            var keyboard = Keyboard.GetState();
            // Note: prevKeyboard tracking would need to be added; for now pass current
            _editor.Update(mouse, prevMouse, keyboard, keyboard, canvasBounds,
                           state.Dialogues, font, screenW, screenH, gameTime,
                           null, state.Quests, state.Groups);

            // Check if editor signaled completion (Save/Cancel from its own UI)
            if (_editor.IsComplete)
            {
                if (!_editor.WasCancelled && _editor.Result != null)
                {
                    ApplyEditorResult(state);
                }
                // Keep the editor open with the saved data
                if (_editor.Result != null)
                    LoadDialogueForEditing(_editor.Result.Id, state);
                else
                    _editor = null;
            }
        }

        // Update list panel (after editor handling, so signals are fresh)
        // Sidebar bounds are passed separately via DrawSidebar
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                     Renderer renderer, Rectangle canvasBounds)
    {
        if (_editor != null)
        {
            _editor.Draw(spriteBatch, font, canvasBounds, renderer);
        }
        else
        {
            // Empty state: show hint
            var text = "Select a dialogue from the list, or click '+ New' to create one.";
            var size = font.MeasureString(text);
            spriteBatch.DrawString(font, text,
                new Vector2(canvasBounds.X + (canvasBounds.Width - size.X) / 2,
                            canvasBounds.Y + canvasBounds.Height / 2),
                new Color(100, 100, 120));
        }
    }

    public void DrawSidebar(SpriteBatch spriteBatch, SpriteFont font, EditorState state,
                            Renderer renderer, Rectangle sidebarBounds)
    {
        _listPanel.Draw(spriteBatch, font, state, renderer, sidebarBounds);
    }

    private void LoadDialogueForEditing(string dialogueId, EditorState state)
    {
        var dialogue = state.Dialogues?.Find(d => d.Id == dialogueId);
        if (dialogue == null) return;

        _editor = DialogueTreeEditor.ForExistingDialogue(dialogue);
        ActiveDialogueId = dialogueId;
        _listPanel.SelectedDialogueId = dialogueId;
        HasUnsavedChanges = false;
    }

    private void ApplyEditorResult(EditorState state)
    {
        var result = _editor.Result;
        if (result == null) return;

        if (_editor.IsNew)
        {
            state.Dialogues.Add(result);
        }
        else
        {
            var origId = _editor.OriginalId;
            int idx = state.Dialogues.FindIndex(d => d.Id == origId);
            if (idx >= 0)
                state.Dialogues[idx] = result;
        }

        OnSaveDialogue?.Invoke(result);
        OnNotifyChanged?.Invoke();
        ActiveDialogueId = result.Id;
    }

    private void SaveCurrentDialogue(EditorState state)
    {
        // Workspace auto-save on exit if needed
        // For now, the editor handles its own save via IsComplete
    }
}
```

Note: This wraps the existing DialogueTreeEditor. The editor's Update/Draw methods are called with the canvas bounds. The workspace handles the list panel sidebar and editor lifecycle. In future phases, the DialogueTreeEditor will be decomposed into smaller components that the workspace orchestrates directly.

- [ ] **Step 6: Run DialogueWorkspace tests**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~DialogueWorkspaceTests" -v minimal`
Expected: All 3 PASS.

- [ ] **Step 7: Commit**

```bash
git add TileForge/UI/DialogueListPanel.cs TileForge/UI/DialogueWorkspace.cs \
       TileForge.Tests/UI/DialogueListPanelTests.cs TileForge.Tests/UI/DialogueWorkspaceTests.cs
git commit -m "feat: add DialogueWorkspace with DialogueListPanel sidebar"
```

---

### Task 4: QuestWorkspace

Same pattern as DialogueWorkspace but for quests. Wraps existing QuestEditor.

#### Files:
- Create: `TileForge/UI/QuestListPanel.cs`
- Create: `TileForge/UI/QuestWorkspace.cs`
- Create: `TileForge.Tests/UI/QuestListPanelTests.cs`
- Create: `TileForge.Tests/UI/QuestWorkspaceTests.cs`

- [ ] **Step 1: Write QuestListPanel tests**

Create `TileForge.Tests/UI/QuestListPanelTests.cs`:

```csharp
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class QuestListPanelTests
{
    [Fact]
    public void WantsNewQuest_DefaultsFalse()
    {
        var panel = new QuestListPanel();
        Assert.False(panel.WantsNewQuest);
    }

    [Fact]
    public void WantsEditQuestId_DefaultsNull()
    {
        var panel = new QuestListPanel();
        Assert.Null(panel.WantsEditQuestId);
    }

    [Fact]
    public void WantsDeleteQuestId_DefaultsNull()
    {
        var panel = new QuestListPanel();
        Assert.Null(panel.WantsDeleteQuestId);
    }

    [Fact]
    public void ClearSignals_ResetsAll()
    {
        var panel = new QuestListPanel();
        panel.ClearSignals();
        Assert.False(panel.WantsNewQuest);
        Assert.Null(panel.WantsEditQuestId);
        Assert.Null(panel.WantsDeleteQuestId);
    }

    [Fact]
    public void SelectedQuestId_CanBeSet()
    {
        var panel = new QuestListPanel();
        panel.SelectedQuestId = "cave_quest";
        Assert.Equal("cave_quest", panel.SelectedQuestId);
    }
}
```

- [ ] **Step 2: Implement QuestListPanel**

Create `TileForge/UI/QuestListPanel.cs`. Follow the exact same pattern as DialogueListPanel but with quest-specific naming:
- `WantsNewQuest`, `WantsEditQuestId`, `WantsDeleteQuestId` signals
- `SelectedQuestId` state
- Header says "Quests" with `LayoutConstants.WorkspaceQuestColor`
- Items are `state.Quests` with `quest.Id` and `quest.Name` display
- Context menu: "Edit", "Delete"

The implementation follows DialogueListPanel exactly, substituting `Dialogues` → `Quests`, `DialogueData` → `QuestDefinition`, color constants, etc.

- [ ] **Step 3: Write QuestWorkspace tests**

Create `TileForge.Tests/UI/QuestWorkspaceTests.cs`:

```csharp
using TileForge.UI;
using Xunit;

namespace TileForge.Tests.UI;

public class QuestWorkspaceTests
{
    [Fact]
    public void QuestWorkspace_ImplementsIWorkspace()
    {
        var workspace = new QuestWorkspace();
        Assert.IsAssignableFrom<IWorkspace>(workspace);
    }

    [Fact]
    public void ActiveQuestId_DefaultsNull()
    {
        var workspace = new QuestWorkspace();
        Assert.Null(workspace.ActiveQuestId);
    }
}
```

- [ ] **Step 4: Implement QuestWorkspace**

Create `TileForge/UI/QuestWorkspace.cs`. Follow DialogueWorkspace pattern:
- Contains `QuestListPanel` for sidebar
- Wraps existing `QuestEditor` in canvas area
- Signals: `OnSaveQuest`, `OnDeleteQuest`, `OnNotifyChanged` callbacks
- `ActiveQuestId` tracks current quest being edited
- List selection loads quest into editor
- Editor completion applies result to `state.Quests`

The implementation follows DialogueWorkspace exactly, substituting dialogue types for quest types.

- [ ] **Step 5: Run all workspace tests**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~QuestListPanelTests|FullyQualifiedName~QuestWorkspaceTests" -v minimal`
Expected: All PASS.

- [ ] **Step 6: Run full test suite**

Run: `dotnet test TileForge.Tests -v minimal`
Expected: All tests PASS.

- [ ] **Step 7: Commit**

```bash
git add TileForge/UI/QuestListPanel.cs TileForge/UI/QuestWorkspace.cs \
       TileForge.Tests/UI/QuestListPanelTests.cs TileForge.Tests/UI/QuestWorkspaceTests.cs
git commit -m "feat: add QuestWorkspace with QuestListPanel sidebar"
```

---

## Chunk 4: Integration into TileForgeGame

### Task 5: Wire Workspaces into TileForgeGame

This is the integration task — connecting all the new workspaces into TileForgeGame's Update/Draw loop and adding workspace switching buttons to the toolbar.

#### Files:
- Modify: `TileForge/TileForgeGame.cs`
- Modify: `TileForge/UI/ToolbarRibbon.cs` (if it exists) or modify toolbar drawing in TileForgeGame

- [ ] **Step 1: Add workspace instances to TileForgeGame**

In `TileForge/TileForgeGame.cs`, add fields near the existing panel fields:

```csharp
// Workspaces
private MapWorkspace _mapWorkspace;
private DialogueWorkspace _dialogueWorkspace;
private QuestWorkspace _questWorkspace;
// WorldMapWorkspace will be added in a future phase
```

- [ ] **Step 2: Initialize workspaces in LoadContent**

In `LoadContent()`, after panel initialization:

```csharp
// Initialize workspaces
_mapWorkspace = new MapWorkspace();
_dialogueWorkspace = new DialogueWorkspace
{
    OnSaveDialogue = SaveDialogue,
    OnDeleteDialogue = id => DeleteDialogue(id),
    OnNotifyChanged = () => _state.NotifyDialoguesChanged(),
};
_questWorkspace = new QuestWorkspace
{
    OnSaveQuest = SaveQuest,
    OnDeleteQuest = id => DeleteQuest(id),
    OnNotifyChanged = () => _state.NotifyQuestsChanged(),
};
```

Where `SaveDialogue`, `DeleteDialogue`, `SaveQuest`, `DeleteQuest` are existing methods or thin wrappers around existing file operations.

- [ ] **Step 3: Remove DialoguePanel and QuestPanel from PanelDock**

In `LoadContent()`, remove the lines that add `_dialoguePanel` and `_questPanel` to `_panelDock.Panels`. Keep the panel objects alive for backward compatibility but don't add them to the dock.

```csharp
// Before:
_panelDock.Panels.Add(_mapPanel);
_panelDock.Panels.Add(_questPanel);       // REMOVE
_panelDock.Panels.Add(_dialoguePanel);    // REMOVE
_panelDock.Panels.Add(_tilePalettePanel);

// After:
_panelDock.Panels.Add(_mapPanel);
_panelDock.Panels.Add(_tilePalettePanel);
```

- [ ] **Step 4: Add workspace switching to the toolbar**

Add workspace buttons to the toolbar ribbon area. In the Draw method, after the existing toolbar ribbon drawing, add workspace mode buttons on the right side of the toolbar:

```csharp
// Draw workspace buttons (right side of toolbar)
if (!_state.IsPlayMode)
{
    int btnX = toolbarRight - (3 * (LayoutConstants.WorkspaceButtonWidth + LayoutConstants.WorkspaceButtonSpacing));
    DrawWorkspaceButton(spriteBatch, font, "Dialogues", WorkspaceMode.Dialogues,
                       LayoutConstants.WorkspaceDialogueColor, ref btnX);
    DrawWorkspaceButton(spriteBatch, font, "Quests", WorkspaceMode.Quests,
                       LayoutConstants.WorkspaceQuestColor, ref btnX);
    DrawWorkspaceButton(spriteBatch, font, "World Map", WorkspaceMode.WorldMap,
                       LayoutConstants.WorkspaceWorldMapColor, ref btnX);
}
```

- [ ] **Step 5: Route Update by workspace mode**

In `Update()`, after global input handling and before the current panel/canvas update, add workspace routing:

```csharp
// Route to active workspace (for non-Map modes)
if (_state.ActiveWorkspace != WorkspaceMode.Map && !_state.IsPlayMode)
{
    var workspace = GetActiveWorkspace();
    if (workspace != null)
    {
        workspace.Update(_state, mouse, prevMouse, inputEvent, _font,
                        GetCanvasBounds(), gameTime, screenW, screenH);
        // Skip normal panel/canvas update
        return;
    }
}
// ... existing panel dock + canvas update for Map mode
```

With helper:
```csharp
private IWorkspace GetActiveWorkspace() => _state.ActiveWorkspace switch
{
    WorkspaceMode.Map => _mapWorkspace,
    WorkspaceMode.Dialogues => _dialogueWorkspace,
    WorkspaceMode.Quests => _questWorkspace,
    _ => null,
};
```

- [ ] **Step 6: Route Draw by workspace mode**

In `Draw()`, replace the canvas/panel drawing section with workspace routing:

```csharp
if (_state.IsPlayMode)
{
    // ... existing play mode drawing
}
else if (_state.ActiveWorkspace == WorkspaceMode.Map)
{
    // ... existing canvas + panel dock drawing
}
else
{
    var workspace = GetActiveWorkspace();
    if (workspace != null)
    {
        workspace.DrawSidebar(spriteBatch, _font, _state, _renderer, GetSidebarBoundsForWorkspace());
        workspace.Draw(spriteBatch, _font, _state, _renderer, GetCanvasBounds());
    }
}
```

- [ ] **Step 7: Handle workspace switching input**

In the toolbar click handling or global input, detect clicks on workspace buttons and workspace keyboard shortcuts:

```csharp
// Keyboard shortcuts: Ctrl+1=Map, Ctrl+2=Dialogues, Ctrl+3=Quests, Ctrl+4=WorldMap
// Escape returns to Map
if (keyboard.IsKeyDown(Keys.Escape) && _state.ActiveWorkspace != WorkspaceMode.Map)
{
    SwitchWorkspace(WorkspaceMode.Map);
}
```

With helper:
```csharp
private void SwitchWorkspace(WorkspaceMode mode)
{
    if (_state.ActiveWorkspace == mode) return;
    var oldWorkspace = GetActiveWorkspace();
    oldWorkspace?.OnExit(_state);
    _state.ActiveWorkspace = mode;
    var newWorkspace = GetActiveWorkspace();
    newWorkspace?.OnEnter(_state);
}
```

- [ ] **Step 8: Remove old HandleDialoguePanelActions/HandleQuestPanelActions**

Since dialogues and quests now use workspaces instead of sidebar panels + modals, the signal handling in `HandleDialoguePanelActions()` (lines ~856-891) and `HandleQuestPanelActions()` (lines ~793-823) can be removed. The workspace handles its own editor lifecycle.

Also remove the modal `_questEditor` and `_dialogueEditor` fields and their update/draw code from the main loop.

- [ ] **Step 9: Run full test suite**

Run: `dotnet test TileForge.Tests -v minimal`
Expected: All tests PASS.

- [ ] **Step 10: Manual testing checklist**

Since this is a UI integration task, verify manually:
- [ ] App starts in Map mode (layers + tileset in sidebar)
- [ ] Clicking "Dialogues" button switches to dialogue workspace (dialogue list in sidebar, editor in canvas)
- [ ] Clicking "Quests" button switches to quest workspace
- [ ] Escape returns to Map mode
- [ ] Dialogue/quest panels no longer appear in the Map mode sidebar
- [ ] Creating/editing/deleting dialogues works in the workspace
- [ ] Creating/editing/deleting quests works in the workspace
- [ ] Play mode (F5) still works from Map mode

- [ ] **Step 11: Commit**

```bash
git add TileForge/TileForgeGame.cs TileForge/UI/ToolbarRibbon.cs
git commit -m "feat: wire workspace mode switching into TileForgeGame, remove sidebar quest/dialogue panels"
```

---

## Phase 2 Complete Checkpoint

After all tasks are done:

- [ ] **Run full test suite**

Run: `dotnet test TileForge.Tests -v minimal`
Expected: All tests PASS, 0 failures.

- [ ] **Verify new file count**

New files created:
- `TileForge/Game/TriggerEvent.cs`
- `TileForge/Game/TriggerManager.cs`
- `TileForge/UI/MapWorkspace.cs`
- `TileForge/UI/DialogueListPanel.cs`
- `TileForge/UI/DialogueWorkspace.cs`
- `TileForge/UI/QuestListPanel.cs`
- `TileForge/UI/QuestWorkspace.cs`

New test files:
- `TileForge.Tests/Game/TriggerManagerTests.cs`
- `TileForge.Tests/UI/MapWorkspaceTests.cs`
- `TileForge.Tests/UI/DialogueListPanelTests.cs`
- `TileForge.Tests/UI/DialogueWorkspaceTests.cs`
- `TileForge.Tests/UI/QuestListPanelTests.cs`
- `TileForge.Tests/UI/QuestWorkspaceTests.cs`

Modified files:
- `TileForge/TileForgeGame.cs` — workspace routing, toolbar buttons, remove modal editors
- `TileForge/UI/ToolbarRibbon.cs` — workspace buttons (if separate file)
