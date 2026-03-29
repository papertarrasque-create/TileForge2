# Combat Rebalance Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebalance player base stats and create/update 5 enemy entity types across two progressive difficulty tiers.

**Architecture:** Two-part change: (1) modify PlayerState defaults and GameStateManager initialization, then fix all tests referencing old values, (2) use the adventure-designer skill to generate/update entity groups in the tutorial project `.tileforge` files.

**Tech Stack:** C# / .NET 9.0, xUnit, System.Text.Json, TileForge adventure-designer skill

---

### Task 1: Update PlayerState defaults

**Files:**
- Modify: `TileForge/Game/PlayerState.cs:15,17-18` (Defense, Poise, MaxPoise)

- [ ] **Step 1: Update Defense default from 2 to 3**

In `TileForge/Game/PlayerState.cs`, change line 15:

```csharp
// Old:
public int Defense { get; set; } = 2;
// New:
public int Defense { get; set; } = 3;
```

- [ ] **Step 2: Update Poise and MaxPoise defaults from 20 to 10**

In `TileForge/Game/PlayerState.cs`, change lines 17-18:

```csharp
// Old:
public int Poise { get; set; } = 20;
public int MaxPoise { get; set; } = 20;
// New:
public int Poise { get; set; } = 10;
public int MaxPoise { get; set; } = 10;
```

- [ ] **Step 3: Commit**

```bash
git add TileForge/Game/PlayerState.cs
git commit -m "feat: rebalance player defaults -- defense 3, poise 10"
```

---

### Task 2: Update GameStateManager hardcoded player initialization

**Files:**
- Modify: `TileForge/Game/GameStateManager.cs:46-47,78-79`

- [ ] **Step 1: Update LoadState backward-compat fixup**

In `GameStateManager.cs` around line 46-47, the zero-poise fixup sets `MaxPoise = 20` and `Poise = 20`. Change to 10:

```csharp
// Old:
State.Player.MaxPoise = 20;
State.Player.Poise = 20;
// New:
State.Player.MaxPoise = 10;
State.Player.Poise = 10;
```

- [ ] **Step 2: Update Initialize player creation**

In `GameStateManager.cs` around lines 77-79, the Initialize method creates a PlayerState with hardcoded poise. Change to 10:

```csharp
// Old:
Poise = 20,
MaxPoise = 20,
// New:
Poise = 10,
MaxPoise = 10,
```

- [ ] **Step 3: Commit**

```bash
git add TileForge/Game/GameStateManager.cs
git commit -m "feat: update GameStateManager poise initialization to 10"
```

---

### Task 3: Fix CombatHelperTests for new Defense default

**Files:**
- Modify: `TileForge.Tests/Game/CombatHelperTests.cs:101-106`

- [ ] **Step 1: Update PlayerState_Defense_DefaultIsTwo test**

The test name and assertion need to reflect the new default of 3. In `CombatHelperTests.cs` around line 100-106:

```csharp
// Old:
[Fact]
public void PlayerState_Defense_DefaultIsTwo()
{
    var player = new PlayerState();

    Assert.Equal(2, player.Defense);
}

// New:
[Fact]
public void PlayerState_Defense_DefaultIsThree()
{
    var player = new PlayerState();

    Assert.Equal(3, player.Defense);
}
```

- [ ] **Step 2: Run test to verify it passes**

Run: `dotnet test --filter "PlayerState_Defense_DefaultIsThree" --verbosity normal`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add TileForge.Tests/Game/CombatHelperTests.cs
git commit -m "test: update defense default test to expect 3"
```

---

### Task 4: Fix PoiseTests for new poise defaults

**Files:**
- Modify: `TileForge.Tests/Game/PoiseTests.cs`

Several tests use the old default poise of 20 either explicitly or via the helper's default parameter. The helper signature is `CreateManagerWithPlayer(int health = 100, int poise = 20, int maxPoise = 20)`. Tests that pass explicit poise values are fine. Tests that rely on the default parameter or assert against 20 need updating.

- [ ] **Step 1: Update CreateManagerWithPlayer default parameters**

Change the helper at line 10:

```csharp
// Old:
private GameStateManager CreateManagerWithPlayer(int health = 100, int poise = 20, int maxPoise = 20)
// New:
private GameStateManager CreateManagerWithPlayer(int health = 100, int poise = 10, int maxPoise = 10)
```

- [ ] **Step 2: Update DamagePlayer_FullAbsorption_HealthUnchanged**

This test at line 31 calls `CreateManagerWithPlayer(poise: 20)` -- this is an explicit value so it stays. No change needed.

- [ ] **Step 3: Update DamagePlayer_PoiseNotBroken_FlagNotSet**

At line 80, this test uses `poise: 20` explicitly. No change needed.

- [ ] **Step 4: Update RegeneratePoise_RestoresQuarterOfMax**

At line 109, this test creates `poise: 0, maxPoise: 20` and expects regen of 5. Change to use new defaults:

```csharp
// Old:
var mgr = CreateManagerWithPlayer(poise: 0, maxPoise: 20);
int amount = mgr.RegeneratePoise();

Assert.Equal(5, amount);
Assert.Equal(5, mgr.State.Player.Poise);

// New:
var mgr = CreateManagerWithPlayer(poise: 0, maxPoise: 10);
int amount = mgr.RegeneratePoise();

Assert.Equal(2, amount);   // max(1, 10/4) = 2
Assert.Equal(2, mgr.State.Player.Poise);
```

- [ ] **Step 5: Update RegeneratePoise_CapsAtMax**

At line 119, uses `poise: 18, maxPoise: 20` and expects regen of 2, cap at 20. Update:

```csharp
// Old:
var mgr = CreateManagerWithPlayer(poise: 18, maxPoise: 20);
int amount = mgr.RegeneratePoise();

Assert.Equal(2, amount);
Assert.Equal(20, mgr.State.Player.Poise);

// New:
var mgr = CreateManagerWithPlayer(poise: 8, maxPoise: 10);
int amount = mgr.RegeneratePoise();

Assert.Equal(2, amount);   // max(1, 10/4) = 2
Assert.Equal(10, mgr.State.Player.Poise);
```

- [ ] **Step 6: Update RegeneratePoise_AlreadyFull_ReturnsZero**

At line 129, uses `poise: 20, maxPoise: 20`. Update:

```csharp
// Old:
var mgr = CreateManagerWithPlayer(poise: 20, maxPoise: 20);
int amount = mgr.RegeneratePoise();

Assert.Equal(0, amount);
Assert.Equal(20, mgr.State.Player.Poise);

// New:
var mgr = CreateManagerWithPlayer(poise: 10, maxPoise: 10);
int amount = mgr.RegeneratePoise();

Assert.Equal(0, amount);
Assert.Equal(10, mgr.State.Player.Poise);
```

- [ ] **Step 7: Update GetEffectiveMaxPoise_BaseOnly**

At line 150, uses default maxPoise: 20, asserts 20. Update:

```csharp
// Old:
var mgr = CreateManagerWithPlayer(maxPoise: 20);
Assert.Equal(20, mgr.GetEffectiveMaxPoise());

// New:
var mgr = CreateManagerWithPlayer(maxPoise: 10);
Assert.Equal(10, mgr.GetEffectiveMaxPoise());
```

- [ ] **Step 8: Update GetEffectiveMaxPoise_WithEquipment**

At line 157, uses default maxPoise: 20 with +10 equip, expects 30. Update:

```csharp
// Old:
var mgr = CreateManagerWithPlayer(maxPoise: 20);
// ...
Assert.Equal(30, mgr.GetEffectiveMaxPoise());

// New:
var mgr = CreateManagerWithPlayer(maxPoise: 10);
// ...
Assert.Equal(20, mgr.GetEffectiveMaxPoise());
```

- [ ] **Step 9: Update Initialize_SetsPoise**

At line 224-226, asserts poise and maxPoise are 20 after Initialize. Update:

```csharp
// Old:
Assert.Equal(20, mgr.State.Player.Poise);
Assert.Equal(20, mgr.State.Player.MaxPoise);

// New:
Assert.Equal(10, mgr.State.Player.Poise);
Assert.Equal(10, mgr.State.Player.MaxPoise);
```

- [ ] **Step 10: Update LoadState_FixupZeroPoise**

At line 239-240, asserts the zero-poise fixup sets MaxPoise=20, Poise=20. Update:

```csharp
// Old:
Assert.Equal(20, mgr.State.Player.MaxPoise);
Assert.Equal(20, mgr.State.Player.Poise);

// New:
Assert.Equal(10, mgr.State.Player.MaxPoise);
Assert.Equal(10, mgr.State.Player.Poise);
```

- [ ] **Step 11: Run all poise tests**

Run: `dotnet test --filter "PoiseTests" --verbosity normal`
Expected: All PASS

- [ ] **Step 12: Commit**

```bash
git add TileForge.Tests/Game/PoiseTests.cs
git commit -m "test: update poise tests for new default of 10"
```

---

### Task 5: Fix EntityTurnTests for new Defense default

**Files:**
- Modify: `TileForge.Tests/Game/EntityTurnTests.cs:127,142-145`

- [ ] **Step 1: Update ChaseEntity_AttacksAdjacentPlayer test**

At line 127, the player is created with `Defense = 2`. The goblin attacks with `attack = "6"`. Old damage: `max(1, 6-2) = 4`, health `100 - 4 = 96`. New with Defense 3: `max(1, 6-3) = 3`, health `100 - 3 = 97`.

```csharp
// Old (line 127):
Player = new PlayerState { X = 3, Y = 3, Health = 100, MaxHealth = 100, Defense = 2, Poise = 0 },
// ...
// Old (lines 142-145):
// Damage = max(1, 6 - 2) = 4
Assert.Equal(96, state.Player.Health);
Assert.Single(messages);
Assert.Equal("Goblin hit you for 4 damage!", messages[0]);

// New:
Player = new PlayerState { X = 3, Y = 3, Health = 100, MaxHealth = 100, Defense = 3, Poise = 0 },
// ...
// Damage = max(1, 6 - 3) = 3
Assert.Equal(97, state.Player.Health);
Assert.Single(messages);
Assert.Equal("Goblin hit you for 3 damage!", messages[0]);
```

- [ ] **Step 2: Run test to verify**

Run: `dotnet test --filter "ChaseEntity_AttacksAdjacentPlayer" --verbosity normal`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add TileForge.Tests/Game/EntityTurnTests.cs
git commit -m "test: update entity turn test for player defense 3"
```

---

### Task 6: Fix DialogueRuntimeTests poise values

**Files:**
- Modify: `TileForge.Tests/Game/DialogueRuntimeTests.cs:17-18`

- [ ] **Step 1: Update CreateGSM helper**

At line 17-18, the helper creates player with `Poise = 20, MaxPoise = 20`. Update:

```csharp
// Old:
Poise = 20,
MaxPoise = 20,
// New:
Poise = 10,
MaxPoise = 10,
```

- [ ] **Step 2: Run dialogue tests**

Run: `dotnet test --filter "DialogueRuntimeTests" --verbosity normal`
Expected: All PASS

- [ ] **Step 3: Commit**

```bash
git add TileForge.Tests/Game/DialogueRuntimeTests.cs
git commit -m "test: update dialogue tests for poise default 10"
```

---

### Task 7: Run full test suite to catch any remaining failures

**Files:** None (verification only)

- [ ] **Step 1: Run all tests**

Run: `dotnet test --verbosity normal`
Expected: All 1768+ tests PASS, 0 failures

- [ ] **Step 2: If any failures, fix them**

Search for any remaining hardcoded `20` poise or `2` defense values in test files that now fail. Apply the same pattern: update the value and the expected assertion.

- [ ] **Step 3: Commit any remaining fixes**

```bash
git add -A
git commit -m "test: fix remaining test assertions for rebalanced player stats"
```

---

### Task 8: Update entity groups via adventure-designer

**Files:**
- Modify: `TileForge/TutorialProject/QuestTestMap.tileforge` (Rat and Goblin groups)
- Modify: `TileForge/TutorialProject/adventureland.tileforge` (goblin group)
- Create entity groups: Skeleton, Zombie, Ghost (in appropriate `.tileforge` file)

Use the `/adventure-designer` skill to generate these entities. Deploy all 5 entity definitions via the skill's direct command mode.

- [ ] **Step 1: Update Rat entity group in QuestTestMap.tileforge**

Use adventure-designer to update the Rat entity in `QuestTestMap.tileforge` with these stats:

| Property | Value |
|----------|-------|
| health | 8 |
| attack | 2 |
| defense | 0 |
| speed | 2 |
| weight | 1 |
| poise | 0 |
| aggro_range | 6 |
| behavior | chase |
| hostile | true |
| xp | 5 |

Keep existing properties: `default_facing`, `on_kill_increment`, `dialogue_id`, `dialogue`.

- [ ] **Step 2: Update Goblin entity group in QuestTestMap.tileforge**

Update the Goblin entity with:

| Property | Value |
|----------|-------|
| health | 15 |
| attack | 5 |
| defense | 1 |
| speed | 1 |
| weight | 1 |
| poise | 0 |
| aggro_range | 8 |
| behavior | chase |
| hostile | true |
| xp | 15 |

Keep existing properties: `spawn_requires_flag`, `dialogue_id`, `dialogue`, `default_facing`.
Remove `max_health: 1` (this looks like a data bug -- should match health or be omitted).

- [ ] **Step 3: Update goblin entity group in adventureland.tileforge**

Update the goblin entity with same stats as step 2:

| Property | Value |
|----------|-------|
| health | 15 |
| attack | 5 |
| defense | 1 |
| speed | 1 |
| poise | 0 |
| aggro_range | 8 |
| behavior | chase_patrol |
| hostile | true |
| xp | 15 |

Note: this one uses `chase_patrol` behavior -- keep that.

- [ ] **Step 4: Create Skeleton entity group**

Create a new Skeleton entity group in `QuestTestMap.tileforge` (or whichever map is appropriate for Tier 1 enemies):

| Property | Value |
|----------|-------|
| health | 18 |
| attack | 4 |
| defense | 2 |
| speed | 1 |
| weight | 1 |
| poise | 0 |
| aggro_range | 7 |
| behavior | chase_patrol |
| hostile | true |
| xp | 15 |
| default_facing | right |

Sprite: Use a skeleton-appropriate sprite from the sheet (row/col TBD by adventure-designer).

- [ ] **Step 5: Create Zombie entity group**

Create a new Zombie entity group:

| Property | Value |
|----------|-------|
| health | 40 |
| attack | 8 |
| defense | 0 |
| speed | 1 |
| weight | 3 |
| poise | 0 |
| aggro_range | 5 |
| behavior | chase |
| hostile | true |
| xp | 30 |
| default_facing | right |

- [ ] **Step 6: Create Ghost entity group**

Create a new Ghost entity group:

| Property | Value |
|----------|-------|
| health | 12 |
| attack | 7 |
| defense | 1 |
| speed | 2 |
| weight | 1 |
| poise | 0 |
| aggro_range | 10 |
| behavior | chase |
| hostile | true |
| xp | 25 |
| default_facing | right |

- [ ] **Step 7: Commit entity data changes**

```bash
git add TileForge/TutorialProject/QuestTestMap.tileforge TileForge/TutorialProject/adventureland.tileforge
git commit -m "feat: rebalance enemies and add skeleton, zombie, ghost entity types"
```

---

### Task 9: Final verification

- [ ] **Step 1: Run full test suite**

Run: `dotnet test --verbosity normal`
Expected: All tests PASS

- [ ] **Step 2: Verify entity data loads**

Launch the app or check that `.tileforge` files are valid JSON by deserializing them in a quick test or simply opening the editor.

- [ ] **Step 3: Final commit if needed**

```bash
git add -A
git commit -m "chore: final verification pass for combat rebalance"
```
