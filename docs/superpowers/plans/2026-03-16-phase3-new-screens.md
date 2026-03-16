# Phase 3: New Dialogue Screens Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement BarkOverlay, InspectOverlay, and CutsceneScreen dialogue types, plus type-based dispatch in GameplayScreen.

**Architecture:** InspectOverlay and CutsceneScreen are `GameScreen` subclasses pushed onto the ScreenManager stack (like DialogueScreen). BarkOverlay is NOT a GameScreen -- it's a lightweight component owned and drawn by GameplayScreen directly, so it doesn't block player input. A `DialogueScreenFactory` handles type-based dispatch.

**Tech Stack:** C# / .NET 9.0 / MonoGame 3.8 / xUnit

---

## File Structure

| File | Action | Responsibility |
|------|--------|---------------|
| `TileForge/Game/Screens/BarkOverlay.cs` | Create | Floating text bubble, auto-dismiss, NOT a GameScreen -- owned by GameplayScreen |
| `TileForge/Game/Screens/InspectOverlay.cs` | Create | Centered text popup, dismiss on Interact/Cancel |
| `TileForge/Game/Screens/CutsceneScreen.cs` | Create | Full-screen auto-advancing narration, no choices |
| `TileForge/Game/Screens/DialogueScreenFactory.cs` | Create | Type-based dispatch: creates correct screen/overlay for a dialogue |
| `TileForge/Game/Screens/GameplayScreen.cs` | Modify | Add BarkOverlay field, use DialogueScreenFactory in TryShowDialogue/TryShowPickupDialogue |
| `TileForge.Tests/Game/Screens/BarkOverlayTests.cs` | Create | Tests for bark display, auto-dismiss, random tag, oneShot, actions |
| `TileForge.Tests/Game/Screens/InspectOverlayTests.cs` | Create | Tests for inspect display, dismiss on input, multi-node |
| `TileForge.Tests/Game/Screens/CutsceneScreenTests.cs` | Create | Tests for auto-advance, manual advance, action execution, tags |
| `TileForge.Tests/Game/Screens/DialogueDispatchTests.cs` | Create | Tests for type-based screen creation |

---

## Chunk 1: BarkOverlay

### Task 1: BarkOverlay -- Tests

**Files:**
- Create: `TileForge.Tests/Game/Screens/BarkOverlayTests.cs`

- [ ] **Step 1: Write BarkOverlay test file with all tests**

BarkOverlay is NOT a GameScreen. It has `Update(float dt)` and `Draw(...)` methods called by GameplayScreen. Tests call these directly.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game.Screens;

public class BarkOverlayTests
{
    private static GameStateManager CreateGSM()
    {
        var gsm = new GameStateManager();
        gsm.State.Player = new PlayerState();
        return gsm;
    }

    private static DialogueData MakeBark(string text, bool oneShot = false)
    {
        return new DialogueData
        {
            Id = "bark_test",
            Type = "bark",
            OneShot = oneShot ? true : null,
            Nodes = new()
            {
                new DialogueNode { Id = "b1", Speaker = "Guard", Text = text }
            },
            Routes = new() { new DialogueRoute { StartNode = "b1" } },
        };
    }

    [Fact]
    public void BarkOverlay_ShowsText()
    {
        var bark = new BarkOverlay(MakeBark("Halt!"), CreateGSM(),
            new Vector2(100, 50));
        bark.Start();
        Assert.Equal("Halt!", bark.DisplayText);
        Assert.True(bark.IsActive);
    }

    [Fact]
    public void BarkOverlay_AutoDismissesAfterDuration()
    {
        var gsm = CreateGSM();
        var bark = new BarkOverlay(MakeBark("Halt!"), gsm,
            new Vector2(100, 50), durationSeconds: 2f);
        bark.Start();
        Assert.True(bark.IsActive);

        // After 1 second, still showing
        bark.Update(1.0f);
        Assert.True(bark.IsActive);

        // After another 1.5 seconds (total 2.5), should be dismissed
        bark.Update(1.5f);
        Assert.False(bark.IsActive);
    }

    [Fact]
    public void BarkOverlay_ExecutesActionsOnStart()
    {
        var gsm = CreateGSM();
        var dialogue = MakeBark("Watch out!");
        dialogue.Nodes[0].Actions = new()
        {
            new DialogueAction { Type = "set_flag", Value = "warned_player" }
        };
        var bark = new BarkOverlay(dialogue, gsm, new Vector2(0, 0));
        bark.Start();

        Assert.True(gsm.HasFlag("warned_player"));
    }

    [Fact]
    public void BarkOverlay_OneShotSetsFlagOnDismiss()
    {
        var gsm = CreateGSM();
        var bark = new BarkOverlay(MakeBark("Once!", oneShot: true), gsm,
            new Vector2(0, 0), durationSeconds: 1f);
        bark.Start();

        // Dismiss via timeout
        bark.Update(2f);

        Assert.False(bark.IsActive);
        Assert.True(gsm.HasFlag("dialogue_shown:bark_test"));
    }

    [Fact]
    public void BarkOverlay_RandomTag_SelectsRandomNode()
    {
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "bark_random",
            Type = "bark",
            Nodes = new()
            {
                new DialogueNode { Id = "r1", Speaker = "Guard", Text = "Hey!", Tags = new() { "random" } },
                new DialogueNode { Id = "r2", Speaker = "Guard", Text = "Move along!", Tags = new() { "random" } },
                new DialogueNode { Id = "r3", Speaker = "Guard", Text = "No loitering!", Tags = new() { "random" } },
            },
            Routes = new() { new DialogueRoute { StartNode = "r1" } },
        };

        var bark = new BarkOverlay(dialogue, gsm, new Vector2(0, 0));
        bark.Start();
        Assert.Contains(bark.DisplayText, new[] { "Hey!", "Move along!", "No loitering!" });
    }

    [Fact]
    public void BarkOverlay_ConditionsEvaluated()
    {
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "bark_cond",
            Type = "bark",
            Nodes = new()
            {
                new DialogueNode { Id = "b1", Speaker = "Guard", Text = "Normal greeting" },
                new DialogueNode
                {
                    Id = "b2", Speaker = "Guard", Text = "Stop, criminal!",
                    Conditions = new() { new Condition { Type = "has_flag", Flag = "is_criminal" } }
                },
            },
            Routes = new()
            {
                new DialogueRoute
                {
                    StartNode = "b2",
                    Conditions = new() { new Condition { Type = "has_flag", Flag = "is_criminal" } }
                },
                new DialogueRoute { StartNode = "b1" },
            },
        };

        gsm.SetFlag("is_criminal");
        var bark = new BarkOverlay(dialogue, gsm, new Vector2(0, 0));
        bark.Start();
        Assert.Equal("Stop, criminal!", bark.DisplayText);
    }

    [Fact]
    public void BarkOverlay_InactiveByDefault()
    {
        var bark = new BarkOverlay(MakeBark("Hey"), CreateGSM(), new Vector2(0, 0));
        Assert.False(bark.IsActive);
        Assert.Null(bark.DisplayText);
    }
}
```

- [ ] **Step 2: Verify tests fail (BarkOverlay class doesn't exist yet)**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~BarkOverlayTests" --no-restore 2>&1 | tail -5`
Expected: Build error -- `BarkOverlay` type not found

- [ ] **Step 3: Commit test file**

```bash
git add TileForge.Tests/Game/Screens/BarkOverlayTests.cs
git commit -m "test: add BarkOverlay tests (red)"
```

---

### Task 2: BarkOverlay -- Implementation

**Files:**
- Create: `TileForge/Game/Screens/BarkOverlay.cs`

- [ ] **Step 1: Implement BarkOverlay**

BarkOverlay is NOT a GameScreen. It's a lightweight component with `Start()`, `Update(float dt)`, and `Draw(...)` methods. GameplayScreen owns it and calls these directly alongside its own update/draw loop, so the player can keep moving while the bark is visible.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Game.Screens;

/// <summary>
/// Floating text bubble above an entity. Auto-dismisses after a duration.
/// NOT a GameScreen -- owned and drawn by GameplayScreen directly so it
/// does not block player input.
/// </summary>
public class BarkOverlay
{
    private static readonly Random _rng = new();

    private readonly DialogueData _dialogue;
    private readonly GameStateManager _gsm;
    private readonly Vector2 _worldPosition;
    private readonly float _duration;
    private readonly GameLog _gameLog;

    private float _elapsed;
    private DialogueNode _displayNode;

    /// <summary>Whether the bark is currently visible.</summary>
    public bool IsActive { get; private set; }

    /// <summary>The text currently being displayed. Null if not active.</summary>
    public string DisplayText => IsActive ? _displayNode?.Text : null;

    public BarkOverlay(DialogueData dialogue, GameStateManager gsm,
        Vector2 worldPosition, float durationSeconds = 2f, GameLog gameLog = null)
    {
        _dialogue = dialogue;
        _gsm = gsm;
        _worldPosition = worldPosition;
        _duration = durationSeconds;
        _gameLog = gameLog;
    }

    /// <summary>
    /// Resolves the display node, executes actions, and begins showing the bark.
    /// </summary>
    public void Start()
    {
        _elapsed = 0f;
        _displayNode = ResolveDisplayNode();
        IsActive = _displayNode != null;

        if (_displayNode != null)
        {
            ActionExecutor.ExecuteAll(_displayNode.Actions, _gsm, _gameLog);
        }
    }

    private DialogueNode ResolveDisplayNode()
    {
        // Resolve start node via routes
        string startNodeId = null;
        if (_dialogue.Routes != null)
        {
            foreach (var route in _dialogue.Routes)
            {
                if (ConditionEvaluator.EvaluateAll(route.Conditions, _gsm))
                {
                    startNodeId = route.StartNode;
                    break;
                }
            }
        }

        if (startNodeId == null && _dialogue.Nodes?.Count > 0)
            startNodeId = _dialogue.Nodes[0].Id;

        if (startNodeId == null) return null;

        // Check for random-tagged nodes
        var randomNodes = _dialogue.Nodes
            .Where(n => n.Tags != null && n.Tags.Contains("random"))
            .Where(n => ConditionEvaluator.EvaluateAll(n.Conditions, _gsm))
            .ToList();

        if (randomNodes.Count > 0)
            return randomNodes[_rng.Next(randomNodes.Count)];

        // Fall back to resolved start node
        var node = _dialogue.Nodes.FirstOrDefault(n => n.Id == startNodeId);
        if (node != null && !ConditionEvaluator.EvaluateAll(node.Conditions, _gsm))
            return null;

        return node;
    }

    /// <summary>
    /// Called each frame by GameplayScreen. Returns false when the bark has expired.
    /// </summary>
    public void Update(float deltaSeconds)
    {
        if (!IsActive) return;

        _elapsed += deltaSeconds;

        if (_elapsed >= _duration)
        {
            if (_dialogue.OneShot == true && !string.IsNullOrEmpty(_dialogue.Id))
                _gsm.SetFlag($"dialogue_shown:{_dialogue.Id}");
            IsActive = false;
            _displayNode = null;
        }
    }

    /// <summary>
    /// Draws the bark bubble. Called by GameplayScreen during its Draw pass.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, SpriteFont font, Renderer renderer,
        Rectangle canvasBounds)
    {
        if (!IsActive || _displayNode == null) return;

        string text = _displayNode.Text ?? "";
        string speaker = _displayNode.Speaker;
        string fullText = !string.IsNullOrEmpty(speaker) ? $"{speaker}: {text}" : text;

        // Calculate fade: full opacity for first 75% of duration, then fade out
        float fadeStart = _duration * 0.75f;
        float alpha = _elapsed < fadeStart ? 1f :
            1f - ((_elapsed - fadeStart) / (_duration - fadeStart));
        alpha = MathHelper.Clamp(alpha, 0f, 1f);

        var textSize = font.MeasureString(fullText);
        int padding = 6;
        int bubbleWidth = (int)textSize.X + padding * 2;
        int bubbleHeight = (int)textSize.Y + padding * 2;

        // Center above the world position, offset upward
        int bubbleX = (int)_worldPosition.X - bubbleWidth / 2;
        int bubbleY = (int)_worldPosition.Y - bubbleHeight - 16;

        // Clamp to canvas bounds
        bubbleX = Math.Max(canvasBounds.X + 4, Math.Min(bubbleX, canvasBounds.Right - bubbleWidth - 4));
        bubbleY = Math.Max(canvasBounds.Y + 4, bubbleY);

        var bgColor = new Color(0, 0, 0, (int)(180 * alpha));
        var textColor = new Color(255, 255, 255, (int)(255 * alpha));

        var bgRect = new Rectangle(bubbleX, bubbleY, bubbleWidth, bubbleHeight);
        renderer.DrawRect(spriteBatch, bgRect, bgColor);
        spriteBatch.DrawString(font, fullText, new Vector2(bubbleX + padding, bubbleY + padding), textColor);
    }
}
```

- [ ] **Step 2: Run BarkOverlay tests**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~BarkOverlayTests" --no-restore 2>&1 | tail -10`
Expected: All 7 tests PASS

- [ ] **Step 3: Commit**

```bash
git add TileForge/Game/Screens/BarkOverlay.cs
git commit -m "feat: add BarkOverlay component for floating bark dialogue"
```

---

## Chunk 2: InspectOverlay

### Task 3: InspectOverlay -- Tests

**Files:**
- Create: `TileForge.Tests/Game/Screens/InspectOverlayTests.cs`

- [ ] **Step 1: Write InspectOverlay test file**

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game.Screens;

public class InspectOverlayTests
{
    private static GameStateManager CreateGSM()
    {
        var gsm = new GameStateManager();
        gsm.State.Player = new PlayerState();
        return gsm;
    }

    private static GameInputManager SimulateKeyPress(Keys key)
    {
        var input = new GameInputManager();
        input.Update(new KeyboardState());
        input.Update(new KeyboardState(key));
        return input;
    }

    private static GameInputManager NoInput()
    {
        var input = new GameInputManager();
        input.Update(new KeyboardState());
        input.Update(new KeyboardState());
        return input;
    }

    private static readonly GameTime Frame =
        new(TimeSpan.FromSeconds(0), TimeSpan.FromMilliseconds(16));

    private static DialogueData MakeInspect(string text, string speaker = null, bool oneShot = false)
    {
        return new DialogueData
        {
            Id = "inspect_test",
            Type = "inspect",
            OneShot = oneShot ? true : null,
            Nodes = new()
            {
                new DialogueNode { Id = "i1", Speaker = speaker, Text = text }
            },
            Routes = new() { new DialogueRoute { StartNode = "i1" } },
        };
    }

    [Fact]
    public void InspectOverlay_IsOverlay()
    {
        var inspect = new InspectOverlay(MakeInspect("A dusty tome."), CreateGSM());
        Assert.True(inspect.IsOverlay);
    }

    [Fact]
    public void InspectOverlay_DismissesOnInteract()
    {
        var gsm = CreateGSM();
        var inspect = new InspectOverlay(MakeInspect("Old sign."), gsm);
        var sm = new ScreenManager();
        sm.Push(inspect);
        Assert.True(sm.HasScreens);

        // First frame with no input -- still showing
        sm.Update(Frame, NoInput());
        Assert.True(sm.HasScreens);

        // Press Interact -- should dismiss
        sm.Update(Frame, SimulateKeyPress(Keys.E));
        Assert.False(sm.HasScreens);
    }

    [Fact]
    public void InspectOverlay_DismissesOnCancel()
    {
        var gsm = CreateGSM();
        var inspect = new InspectOverlay(MakeInspect("Old sign."), gsm);
        var sm = new ScreenManager();
        sm.Push(inspect);

        sm.Update(Frame, SimulateKeyPress(Keys.Escape));
        Assert.False(sm.HasScreens);
    }

    [Fact]
    public void InspectOverlay_ExecutesActionsOnEnter()
    {
        var gsm = CreateGSM();
        var dialogue = MakeInspect("A clue!");
        dialogue.Nodes[0].Actions = new()
        {
            new DialogueAction { Type = "set_flag", Value = "found_clue" }
        };
        var inspect = new InspectOverlay(dialogue, gsm);
        var sm = new ScreenManager();
        sm.Push(inspect);

        Assert.True(gsm.HasFlag("found_clue"));
    }

    [Fact]
    public void InspectOverlay_OneShotSetsFlagOnDismiss()
    {
        var gsm = CreateGSM();
        var inspect = new InspectOverlay(MakeInspect("Once!", oneShot: true), gsm);
        var sm = new ScreenManager();
        sm.Push(inspect);

        sm.Update(Frame, SimulateKeyPress(Keys.E));
        Assert.True(gsm.HasFlag("dialogue_shown:inspect_test"));
    }

    [Fact]
    public void InspectOverlay_ExposesDisplayText()
    {
        var inspect = new InspectOverlay(MakeInspect("Something seems amiss."), CreateGSM());
        var sm = new ScreenManager();
        sm.Push(inspect);
        Assert.Equal("Something seems amiss.", inspect.DisplayText);
    }

    // NOTE: Spec says "any key/click dismisses." In multi-node inspect dialogues,
    // Interact advances pages rather than closing. Cancel always closes immediately.
    // This is a deliberate narrowing so multi-page inspections (books, multi-line signs)
    // work intuitively -- Interact means "next page" until the last page.
    [Fact]
    public void InspectOverlay_MultiNode_AdvancesOnInteract()
    {
        var gsm = CreateGSM();
        var dialogue = new DialogueData
        {
            Id = "inspect_multi",
            Type = "inspect",
            Nodes = new()
            {
                new DialogueNode { Id = "p1", Text = "Page one.", NextNodeId = "p2" },
                new DialogueNode { Id = "p2", Text = "Page two." },
            },
            Routes = new() { new DialogueRoute { StartNode = "p1" } },
        };
        var inspect = new InspectOverlay(dialogue, gsm);
        var sm = new ScreenManager();
        sm.Push(inspect);

        Assert.Equal("Page one.", inspect.DisplayText);

        // Advance to next page
        sm.Update(Frame, SimulateKeyPress(Keys.E));
        Assert.True(sm.HasScreens); // still open on page 2
        Assert.Equal("Page two.", inspect.DisplayText);

        // Dismiss on last page
        sm.Update(Frame, SimulateKeyPress(Keys.E));
        Assert.False(sm.HasScreens);
    }
}
```

- [ ] **Step 2: Verify tests fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~InspectOverlayTests" --no-restore 2>&1 | tail -5`
Expected: Build error -- `InspectOverlay` type not found

- [ ] **Step 3: Commit test file**

```bash
git add TileForge.Tests/Game/Screens/InspectOverlayTests.cs
git commit -m "test: add InspectOverlay tests (red)"
```

---

### Task 4: InspectOverlay -- Implementation

**Files:**
- Create: `TileForge/Game/Screens/InspectOverlay.cs`

- [ ] **Step 1: Implement InspectOverlay**

```csharp
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Game.Screens;

/// <summary>
/// Compact centered popup for signs, books, item descriptions, and trigger tile text.
/// Supports multi-node sequences (Interact advances pages). Cancel always closes.
/// </summary>
public class InspectOverlay : GameScreen
{
    private readonly DialogueData _dialogue;
    private readonly GameStateManager _gsm;
    private readonly GameLog _gameLog;

    private DialogueNode _currentNode;

    public override bool IsOverlay => true;

    /// <summary>The text currently being displayed. Exposed for testing.</summary>
    public string DisplayText => _currentNode?.Text;

    public InspectOverlay(DialogueData dialogue, GameStateManager gsm, GameLog gameLog = null)
    {
        _dialogue = dialogue;
        _gsm = gsm;
        _gameLog = gameLog;
    }

    public override void OnEnter()
    {
        string startNodeId = ResolveStartNode();
        AdvanceToNode(startNodeId);
    }

    private string ResolveStartNode()
    {
        if (_dialogue.Routes != null)
        {
            foreach (var route in _dialogue.Routes)
            {
                if (ConditionEvaluator.EvaluateAll(route.Conditions, _gsm))
                    return route.StartNode;
            }
        }
        return _dialogue.Nodes?.FirstOrDefault()?.Id;
    }

    private void AdvanceToNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            _currentNode = null;
            return;
        }

        var node = _dialogue.Nodes?.FirstOrDefault(n => n.Id == nodeId);
        if (node == null)
        {
            _currentNode = null;
            return;
        }

        if (!ConditionEvaluator.EvaluateAll(node.Conditions, _gsm))
        {
            AdvanceToNode(node.NextNodeId);
            return;
        }

        _currentNode = node;
        ActionExecutor.ExecuteAll(node.Actions, _gsm, _gameLog);
    }

    public override void Update(GameTime gameTime, GameInputManager input)
    {
        if (_currentNode == null)
        {
            Dismiss();
            return;
        }

        if (input.IsActionJustPressed(GameAction.Cancel))
        {
            Dismiss();
            return;
        }

        if (input.IsActionJustPressed(GameAction.Interact))
        {
            if (!string.IsNullOrEmpty(_currentNode.NextNodeId))
            {
                // Advance to next page
                AdvanceToNode(_currentNode.NextNodeId);
            }
            else
            {
                // Last page -- dismiss
                Dismiss();
            }
        }
    }

    private void Dismiss()
    {
        if (_dialogue.OneShot == true && !string.IsNullOrEmpty(_dialogue.Id))
            _gsm.SetFlag($"dialogue_shown:{_dialogue.Id}");
        ScreenManager.Pop();
    }

    public override void Draw(SpriteBatch spriteBatch, SpriteFont font,
        Renderer renderer, Rectangle canvasBounds)
    {
        if (_currentNode == null) return;

        string text = _currentNode.Text ?? "";
        string speaker = _currentNode.Speaker;

        // Measure text to size the box
        var textSize = font.MeasureString(text);
        int padding = 12;
        int maxWidth = (int)(canvasBounds.Width * 0.6f);
        int boxWidth = System.Math.Min((int)textSize.X + padding * 2, maxWidth);
        int boxHeight = (int)textSize.Y + padding * 2;

        // Add speaker line height if present
        if (!string.IsNullOrEmpty(speaker))
            boxHeight += (int)font.MeasureString(speaker).Y + 4;

        // Center in canvas
        int boxX = canvasBounds.X + (canvasBounds.Width - boxWidth) / 2;
        int boxY = canvasBounds.Y + (canvasBounds.Height - boxHeight) / 2;

        // Semi-transparent background
        var bgRect = new Rectangle(boxX, boxY, boxWidth, boxHeight);
        renderer.DrawRect(spriteBatch, bgRect, new Color(0, 0, 0, 210));

        float textX = boxX + padding;
        float textY = boxY + padding;

        if (!string.IsNullOrEmpty(speaker))
        {
            spriteBatch.DrawString(font, speaker, new Vector2(textX, textY), Color.Yellow);
            textY += font.MeasureString(speaker).Y + 4f;
        }

        spriteBatch.DrawString(font, text, new Vector2(textX, textY), Color.White);

        // Hint text below the box
        string hint = !string.IsNullOrEmpty(_currentNode.NextNodeId)
            ? "[E] Continue  [Esc] Close"
            : "[E] Close  [Esc] Close";
        var hintSize = font.MeasureString(hint);
        spriteBatch.DrawString(font, hint,
            new Vector2(boxX + (boxWidth - hintSize.X) / 2, boxY + boxHeight + 4),
            Color.Gray);
    }
}
```

- [ ] **Step 2: Run InspectOverlay tests**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~InspectOverlayTests" --no-restore 2>&1 | tail -10`
Expected: All 7 tests PASS

- [ ] **Step 3: Commit**

```bash
git add TileForge/Game/Screens/InspectOverlay.cs
git commit -m "feat: add InspectOverlay screen for signs, books, inspection text"
```

---

## Chunk 3: CutsceneScreen

### Task 5: CutsceneScreen -- Tests

**Files:**
- Create: `TileForge.Tests/Game/Screens/CutsceneScreenTests.cs`

- [ ] **Step 1: Write CutsceneScreen test file**

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game.Screens;

public class CutsceneScreenTests
{
    private static GameStateManager CreateGSM()
    {
        var gsm = new GameStateManager();
        gsm.State.Player = new PlayerState();
        return gsm;
    }

    private static GameInputManager SimulateKeyPress(Keys key)
    {
        var input = new GameInputManager();
        input.Update(new KeyboardState());
        input.Update(new KeyboardState(key));
        return input;
    }

    private static GameInputManager NoInput()
    {
        var input = new GameInputManager();
        input.Update(new KeyboardState());
        input.Update(new KeyboardState());
        return input;
    }

    private static readonly GameTime Frame =
        new(TimeSpan.FromSeconds(0), TimeSpan.FromMilliseconds(16));

    private static DialogueData MakeCutscene(params string[] texts)
    {
        var nodes = new List<DialogueNode>();
        for (int i = 0; i < texts.Length; i++)
        {
            nodes.Add(new DialogueNode
            {
                Id = $"c{i}",
                Speaker = "Narrator",
                Text = texts[i],
                NextNodeId = i < texts.Length - 1 ? $"c{i + 1}" : null,
            });
        }
        return new DialogueData
        {
            Id = "cutscene_test",
            Type = "cutscene",
            Nodes = nodes,
            Routes = new() { new DialogueRoute { StartNode = "c0" } },
        };
    }

    [Fact]
    public void CutsceneScreen_IsOverlay()
    {
        var cs = new CutsceneScreen(MakeCutscene("Once upon a time..."), CreateGSM());
        Assert.True(cs.IsOverlay);
    }

    [Fact]
    public void CutsceneScreen_AutoAdvancesAfterDelay()
    {
        var gsm = CreateGSM();
        var cs = new CutsceneScreen(MakeCutscene("Part 1", "Part 2"), gsm,
            autoAdvanceSeconds: 1f);
        var sm = new ScreenManager();
        sm.Push(cs);

        Assert.Equal("Part 1", cs.DisplayText);

        // After 1.5 seconds, should auto-advance to Part 2
        var time = new GameTime(TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(1.5));
        sm.Update(time, NoInput());
        Assert.Equal("Part 2", cs.DisplayText);

        // After another 1.5 seconds, should dismiss (end of cutscene)
        sm.Update(time, NoInput());
        Assert.False(sm.HasScreens);
    }

    [Fact]
    public void CutsceneScreen_ManualAdvanceOnInteract()
    {
        var gsm = CreateGSM();
        var cs = new CutsceneScreen(MakeCutscene("Part 1", "Part 2"), gsm,
            autoAdvanceSeconds: 10f); // long auto so manual is faster
        var sm = new ScreenManager();
        sm.Push(cs);

        Assert.Equal("Part 1", cs.DisplayText);

        // Manual advance
        sm.Update(Frame, SimulateKeyPress(Keys.E));
        Assert.Equal("Part 2", cs.DisplayText);

        // Manual advance again -- end of cutscene
        sm.Update(Frame, SimulateKeyPress(Keys.E));
        Assert.False(sm.HasScreens);
    }

    [Fact]
    public void CutsceneScreen_CancelExitsImmediately()
    {
        var gsm = CreateGSM();
        var cs = new CutsceneScreen(MakeCutscene("Part 1", "Part 2", "Part 3"), gsm);
        var sm = new ScreenManager();
        sm.Push(cs);

        sm.Update(Frame, SimulateKeyPress(Keys.Escape));
        Assert.False(sm.HasScreens);
    }

    [Fact]
    public void CutsceneScreen_ExecutesActionsPerNode()
    {
        var gsm = CreateGSM();
        var dialogue = MakeCutscene("Part 1", "Part 2");
        dialogue.Nodes[0].Actions = new()
        {
            new DialogueAction { Type = "set_flag", Value = "seen_part1" }
        };
        dialogue.Nodes[1].Actions = new()
        {
            new DialogueAction { Type = "set_flag", Value = "seen_part2" }
        };

        var cs = new CutsceneScreen(dialogue, gsm, autoAdvanceSeconds: 1f);
        var sm = new ScreenManager();
        sm.Push(cs);

        Assert.True(gsm.HasFlag("seen_part1"));
        Assert.False(gsm.HasFlag("seen_part2"));

        // Advance to part 2
        var time = new GameTime(TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(1.5));
        sm.Update(time, NoInput());
        Assert.True(gsm.HasFlag("seen_part2"));
    }

    [Fact]
    public void CutsceneScreen_OneShotSetsFlagOnEnd()
    {
        var gsm = CreateGSM();
        var dialogue = MakeCutscene("The end.");
        dialogue.OneShot = true;
        var cs = new CutsceneScreen(dialogue, gsm, autoAdvanceSeconds: 1f);
        var sm = new ScreenManager();
        sm.Push(cs);

        var time = new GameTime(TimeSpan.FromSeconds(1.5), TimeSpan.FromSeconds(1.5));
        sm.Update(time, NoInput());

        Assert.True(gsm.HasFlag("dialogue_shown:cutscene_test"));
    }

    [Fact]
    public void CutsceneScreen_CustomAutoAdvanceTag()
    {
        var gsm = CreateGSM();
        var dialogue = MakeCutscene("Fast", "Slow");
        dialogue.Nodes[0].Tags = new() { "auto_advance_ms:500" };
        dialogue.Nodes[1].Tags = new() { "auto_advance_ms:5000" };

        var cs = new CutsceneScreen(dialogue, gsm);
        var sm = new ScreenManager();
        sm.Push(cs);

        Assert.Equal("Fast", cs.DisplayText);

        // After 0.6s -- should have advanced past the 500ms node
        var time = new GameTime(TimeSpan.FromSeconds(0.6), TimeSpan.FromSeconds(0.6));
        sm.Update(time, NoInput());
        Assert.Equal("Slow", cs.DisplayText);

        // After another 1s (total 1.6s from Slow start) -- should NOT have advanced (5s needed)
        var time2 = new GameTime(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0));
        sm.Update(time2, NoInput());
        Assert.Equal("Slow", cs.DisplayText);
    }
}
```

- [ ] **Step 2: Verify tests fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~CutsceneScreenTests" --no-restore 2>&1 | tail -5`
Expected: Build error -- `CutsceneScreen` type not found

- [ ] **Step 3: Commit test file**

```bash
git add TileForge.Tests/Game/Screens/CutsceneScreenTests.cs
git commit -m "test: add CutsceneScreen tests (red)"
```

---

### Task 6: CutsceneScreen -- Implementation

**Files:**
- Create: `TileForge/Game/Screens/CutsceneScreen.cs`

- [ ] **Step 1: Implement CutsceneScreen**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DojoUI;

namespace TileForge.Game.Screens;

/// <summary>
/// Full-screen auto-advancing narration overlay. No choices.
/// Nodes advance on a timer or manual input. Actions fire per node.
/// Supports per-node timing via "auto_advance_ms:N" tag.
/// </summary>
public class CutsceneScreen : GameScreen
{
    private readonly DialogueData _dialogue;
    private readonly GameStateManager _gsm;
    private readonly GameLog _gameLog;
    private readonly float _defaultAutoAdvance;

    private DialogueNode _currentNode;
    private float _nodeElapsed;

    public override bool IsOverlay => true;

    /// <summary>The text currently being displayed. Exposed for testing.</summary>
    public string DisplayText => _currentNode?.Text;

    public CutsceneScreen(DialogueData dialogue, GameStateManager gsm,
        float autoAdvanceSeconds = 3f, GameLog gameLog = null)
    {
        _dialogue = dialogue;
        _gsm = gsm;
        _defaultAutoAdvance = autoAdvanceSeconds;
        _gameLog = gameLog;
    }

    public override void OnEnter()
    {
        string startNodeId = ResolveStartNode();
        AdvanceToNode(startNodeId);
    }

    private string ResolveStartNode()
    {
        if (_dialogue.Routes != null)
        {
            foreach (var route in _dialogue.Routes)
            {
                if (ConditionEvaluator.EvaluateAll(route.Conditions, _gsm))
                    return route.StartNode;
            }
        }
        return _dialogue.Nodes?.FirstOrDefault()?.Id;
    }

    private void AdvanceToNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            _currentNode = null;
            return;
        }

        var node = _dialogue.Nodes?.FirstOrDefault(n => n.Id == nodeId);
        if (node == null)
        {
            _currentNode = null;
            return;
        }

        if (!ConditionEvaluator.EvaluateAll(node.Conditions, _gsm))
        {
            AdvanceToNode(node.NextNodeId);
            return;
        }

        _currentNode = node;
        _nodeElapsed = 0f;
        ActionExecutor.ExecuteAll(node.Actions, _gsm, _gameLog);
    }

    private float GetAutoAdvanceTime()
    {
        if (_currentNode?.Tags != null)
        {
            foreach (var tag in _currentNode.Tags)
            {
                if (tag.StartsWith("auto_advance_ms:") &&
                    int.TryParse(tag.Substring("auto_advance_ms:".Length), out int ms))
                {
                    return ms / 1000f;
                }
            }
        }
        return _defaultAutoAdvance;
    }

    public override void Update(GameTime gameTime, GameInputManager input)
    {
        if (_currentNode == null)
        {
            EndCutscene();
            return;
        }

        // Cancel exits immediately
        if (input.IsActionJustPressed(GameAction.Cancel))
        {
            EndCutscene();
            return;
        }

        _nodeElapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Manual advance or auto-advance
        bool manualAdvance = input.IsActionJustPressed(GameAction.Interact);
        bool autoAdvance = _nodeElapsed >= GetAutoAdvanceTime();

        if (manualAdvance || autoAdvance)
        {
            if (!string.IsNullOrEmpty(_currentNode.NextNodeId))
                AdvanceToNode(_currentNode.NextNodeId);
            else
                EndCutscene();
        }
    }

    private void EndCutscene()
    {
        if (_dialogue.OneShot == true && !string.IsNullOrEmpty(_dialogue.Id))
            _gsm.SetFlag($"dialogue_shown:{_dialogue.Id}");
        ScreenManager.Pop();
    }

    public override void Draw(SpriteBatch spriteBatch, SpriteFont font,
        Renderer renderer, Rectangle canvasBounds)
    {
        if (_currentNode == null) return;

        // Full-screen dark overlay
        renderer.DrawRect(spriteBatch, canvasBounds, new Color(0, 0, 0, 220));

        string text = _currentNode.Text ?? "";
        string speaker = _currentNode.Speaker;

        var textSize = font.MeasureString(text);

        // Center text vertically and horizontally
        float textX = canvasBounds.X + (canvasBounds.Width - textSize.X) / 2;
        float textY = canvasBounds.Y + (canvasBounds.Height - textSize.Y) / 2;

        if (!string.IsNullOrEmpty(speaker))
        {
            var speakerSize = font.MeasureString(speaker);
            float speakerX = canvasBounds.X + (canvasBounds.Width - speakerSize.X) / 2;
            spriteBatch.DrawString(font, speaker,
                new Vector2(speakerX, textY - speakerSize.Y - 8), Color.Yellow);
        }

        spriteBatch.DrawString(font, text, new Vector2(textX, textY), Color.White);

        // Progress hint at bottom
        int padding = 20;
        string hint = "[E] Continue  [Esc] Skip";
        var hintSize = font.MeasureString(hint);
        spriteBatch.DrawString(font, hint,
            new Vector2(canvasBounds.X + (canvasBounds.Width - hintSize.X) / 2,
                        canvasBounds.Bottom - padding - hintSize.Y),
            Color.Gray);
    }
}
```

- [ ] **Step 2: Run CutsceneScreen tests**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~CutsceneScreenTests" --no-restore 2>&1 | tail -10`
Expected: All 7 tests PASS

- [ ] **Step 3: Commit**

```bash
git add TileForge/Game/Screens/CutsceneScreen.cs
git commit -m "feat: add CutsceneScreen for auto-advancing narration sequences"
```

---

## Chunk 4: DialogueScreenFactory + GameplayScreen Wiring

### Task 7: DialogueScreenFactory -- Tests

**Files:**
- Create: `TileForge.Tests/Game/Screens/DialogueDispatchTests.cs`

- [ ] **Step 1: Write dispatch test file**

```csharp
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TileForge.Game;
using TileForge.Game.Screens;
using Xunit;

namespace TileForge.Tests.Game.Screens;

public class DialogueDispatchTests
{
    private static GameStateManager CreateGSM()
    {
        var gsm = new GameStateManager();
        gsm.State.Player = new PlayerState();
        return gsm;
    }

    private static DialogueData MakeDialogue(string type)
    {
        return new DialogueData
        {
            Id = $"test_{type ?? "null"}",
            Type = type,
            Nodes = new() { new DialogueNode { Id = "n1", Text = "Hello" } },
            Routes = new() { new DialogueRoute { StartNode = "n1" } },
        };
    }

    [Fact]
    public void Create_Conversation_ReturnsDialogueScreen()
    {
        var gsm = CreateGSM();
        var result = DialogueScreenFactory.Create(MakeDialogue("conversation"), gsm);
        Assert.IsType<DialogueScreen>(result.Screen);
        Assert.Null(result.BarkOverlay);
    }

    [Fact]
    public void Create_Bark_ReturnsBarkOverlay()
    {
        var gsm = CreateGSM();
        var result = DialogueScreenFactory.Create(MakeDialogue("bark"), gsm,
            entityWorldPos: new Vector2(10, 20));
        Assert.Null(result.Screen);
        Assert.NotNull(result.BarkOverlay);
    }

    [Fact]
    public void Create_Inspect_ReturnsInspectOverlay()
    {
        var gsm = CreateGSM();
        var result = DialogueScreenFactory.Create(MakeDialogue("inspect"), gsm);
        Assert.IsType<InspectOverlay>(result.Screen);
        Assert.Null(result.BarkOverlay);
    }

    [Fact]
    public void Create_Cutscene_ReturnsCutsceneScreen()
    {
        var gsm = CreateGSM();
        var result = DialogueScreenFactory.Create(MakeDialogue("cutscene"), gsm);
        Assert.IsType<CutsceneScreen>(result.Screen);
        Assert.Null(result.BarkOverlay);
    }

    [Fact]
    public void Create_NullType_DefaultsToDialogueScreen()
    {
        var gsm = CreateGSM();
        var result = DialogueScreenFactory.Create(MakeDialogue(null), gsm);
        Assert.IsType<DialogueScreen>(result.Screen);
    }

    [Fact]
    public void Create_UnknownType_DefaultsToDialogueScreen()
    {
        var gsm = CreateGSM();
        var result = DialogueScreenFactory.Create(MakeDialogue("unknown_type"), gsm);
        Assert.IsType<DialogueScreen>(result.Screen);
    }
}
```

- [ ] **Step 2: Verify tests fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~DialogueDispatchTests" --no-restore 2>&1 | tail -5`
Expected: Build error -- `DialogueScreenFactory` type not found

- [ ] **Step 3: Commit test file**

```bash
git add TileForge.Tests/Game/Screens/DialogueDispatchTests.cs
git commit -m "test: add DialogueScreenFactory dispatch tests (red)"
```

---

### Task 8: DialogueScreenFactory + GameplayScreen Wiring -- Implementation

**Files:**
- Create: `TileForge/Game/Screens/DialogueScreenFactory.cs`
- Modify: `TileForge/Game/Screens/GameplayScreen.cs:941-992`

- [ ] **Step 1: Create DialogueScreenFactory**

Because BarkOverlay is not a GameScreen, the factory returns a result object that can contain either a screen to push or a bark to display.

```csharp
using Microsoft.Xna.Framework;

namespace TileForge.Game.Screens;

/// <summary>
/// Result of DialogueScreenFactory.Create(). Contains either a GameScreen
/// to push onto ScreenManager, or a BarkOverlay for GameplayScreen to own.
/// </summary>
public class DialogueScreenResult
{
    /// <summary>A GameScreen to push (conversation, inspect, cutscene). Null for bark.</summary>
    public GameScreen Screen { get; init; }

    /// <summary>A BarkOverlay for GameplayScreen to own directly. Null for non-bark types.</summary>
    public BarkOverlay BarkOverlay { get; init; }
}

/// <summary>
/// Creates the appropriate screen/overlay based on DialogueData.Type.
/// </summary>
public static class DialogueScreenFactory
{
    public static DialogueScreenResult Create(DialogueData dialogue, GameStateManager gsm,
        GameLog gameLog = null, Vector2? entityWorldPos = null)
    {
        return (dialogue.Type ?? "conversation") switch
        {
            "bark" => new DialogueScreenResult
            {
                BarkOverlay = new BarkOverlay(dialogue, gsm,
                    entityWorldPos ?? Vector2.Zero, gameLog: gameLog),
            },
            "inspect" => new DialogueScreenResult
            {
                Screen = new InspectOverlay(dialogue, gsm, gameLog),
            },
            "cutscene" => new DialogueScreenResult
            {
                Screen = new CutsceneScreen(dialogue, gsm, gameLog: gameLog),
            },
            _ => new DialogueScreenResult
            {
                Screen = new DialogueScreen(dialogue, gsm, gameLog),
            },
        };
    }
}
```

- [ ] **Step 2: Run dispatch tests**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~DialogueDispatchTests" --no-restore 2>&1 | tail -10`
Expected: All 6 tests PASS

- [ ] **Step 3: Wire factory into GameplayScreen**

In `GameplayScreen.cs`, add a `BarkOverlay` field and a helper to dispatch dialogue screen results. Then replace the three direct `new DialogueScreen(...)` calls.

Add field near other private fields (~line 30 area):
```csharp
private BarkOverlay _activeBark;
```

Add helper method after `LoadDialogue()` (~line 1017):
```csharp
    private void ShowDialogue(DialogueData dialogue, EntityInstance instance, PlayState play)
    {
        var entityPos = new Vector2(
            instance.X * _tileSize + _tileSize / 2,
            instance.Y * _tileSize);
        var result = DialogueScreenFactory.Create(dialogue, _gameStateManager,
            _gameLog, entityPos);

        if (result.BarkOverlay != null)
        {
            _activeBark = result.BarkOverlay;
            _activeBark.Start();
        }
        else if (result.Screen != null)
        {
            ScreenManager.Push(result.Screen);
            play.FloatingMessages.Clear();
        }
    }
```

In `TryShowDialogue()` (~line 957-992), replace the three `ScreenManager.Push(new DialogueScreen(...))` calls:

**Concluded dialogue path (~line 966-971):** Replace:
```csharp
                var concluded = LoadDialogue(concludedValue);
                concluded ??= CreateInlineDialogue(instance.DefinitionName, concludedValue);
                ScreenManager.Push(new DialogueScreen(concluded, _gameStateManager, _gameLog));
                play.FloatingMessages.Clear();
```
With:
```csharp
                var concluded = LoadDialogue(concludedValue);
                concluded ??= CreateInlineDialogue(instance.DefinitionName, concludedValue);
                ShowDialogue(concluded, instance, play);
```

**Main dialogue path (~line 989-990):** Replace:
```csharp
        ScreenManager.Push(new DialogueScreen(dialogue, _gameStateManager, _gameLog));
        play.FloatingMessages.Clear();
```
With:
```csharp
        ShowDialogue(dialogue, instance, play);
```

In `TryShowPickupDialogue()` (~line 941-955), replace:
```csharp
        var dialogue = LoadDialogue(pickupDialogue);
        dialogue ??= CreateInlineDialogue(instance.DefinitionName, pickupDialogue);
        ScreenManager.Push(new DialogueScreen(dialogue, _gameStateManager, _gameLog));
        play.FloatingMessages.Clear();
```
With:
```csharp
        var dialogue = LoadDialogue(pickupDialogue);
        dialogue ??= CreateInlineDialogue(instance.DefinitionName, pickupDialogue);
        ShowDialogue(dialogue, instance, play);
```

In GameplayScreen's `Update()` method, add bark update near the top of the game update loop (before screen manager update):
```csharp
        if (_activeBark != null)
        {
            _activeBark.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            if (!_activeBark.IsActive)
                _activeBark = null;
        }
```

In GameplayScreen's `Draw()` method, add bark draw after entity rendering but before UI overlay:
```csharp
        _activeBark?.Draw(spriteBatch, font, renderer, canvasBounds);
```

- [ ] **Step 4: Run all dialogue-related tests**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~Dialogue" --no-restore 2>&1 | tail -15`
Expected: All dialogue tests PASS

- [ ] **Step 5: Run full test suite**

Run: `dotnet test TileForge.Tests --no-restore 2>&1 | tail -5`
Expected: All tests PASS

- [ ] **Step 6: Commit**

```bash
git add TileForge/Game/Screens/DialogueScreenFactory.cs TileForge/Game/Screens/GameplayScreen.cs
git commit -m "feat: add DialogueScreenFactory, wire type-based dispatch into GameplayScreen"
```

---

## Known Gap: ASCII Sanitization

CLAUDE.md requires all strings passed to `DrawString`/`MeasureString` to be printable ASCII (32-126). The existing `DialogueScreen.Draw()` does not sanitize author-supplied text either. Adding sanitization to only the new screens would be inconsistent. A shared `AsciiSanitize(string s)` helper should be added and applied to ALL dialogue rendering screens (DialogueScreen, BarkOverlay, InspectOverlay, CutsceneScreen) as a separate follow-up task, not part of this plan.

---

## Post-Phase Verification

After all 4 chunks are complete:

- [ ] **Run full test suite**
Run: `dotnet test TileForge.Tests --no-restore`
Expected: All tests pass, including ~27 new tests (7 bark + 7 inspect + 7 cutscene + 6 dispatch)

- [ ] **Manual smoke test**
1. Run the app
2. Create a test dialogue with `Type = "bark"` -- verify floating text appears above entity and auto-dismisses while player can still move
3. Create a test dialogue with `Type = "inspect"` -- verify centered popup appears and dismisses on E or Esc
4. Create a test dialogue with `Type = "cutscene"` -- verify full-screen narration auto-advances
5. Verify existing `"conversation"` dialogues still work unchanged
