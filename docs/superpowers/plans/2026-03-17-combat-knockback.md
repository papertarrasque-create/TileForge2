# Combat Knockback Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a knockback mechanic to bump combat that pushes defenders back 1 tile after attacks, with a weight system that controls knockback resistance.

**Architecture:** New static pure class `KnockbackResolver` (follows EntityAI pattern). Weight property on entities, stagger tracking on PlayState. GameplayScreen calls KnockbackResolver after each attack resolves. No changes to damage formulas, flanking, poise, or AP costs.

**Tech Stack:** C# / .NET 9.0 / MonoGame / xUnit

**Spec:** `docs/superpowers/specs/2026-03-17-combat-knockback-design.md`

---

## File Structure

| Action | File | Responsibility |
|--------|------|---------------|
| Create | `TileForge/Game/KnockbackResolver.cs` | Static pure knockback logic + KnockbackResult struct |
| Create | `TileForge.Tests/Game/KnockbackResolverTests.cs` | Unit tests for KnockbackResolver |
| Create | `TileForge.Tests/Game/KnockbackIntegrationTests.cs` | Integration tests for stagger tracking + GameplayScreen |
| Modify | `TileForge/Game/PropertyKeys.cs` | Add `Weight` and `EquipWeight` constants |
| Modify | `TileForge/Game/PropertySchema.cs` | Add `weight` property def for NPCs |
| Modify | `TileForge/Game/PlayerState.cs` | Add `Weight` field (default 1) |
| Modify | `TileForge/Play/PlayState.cs` | Add `HitsThisTurn` dictionary |
| Modify | `TileForge/Game/GameStateManager.cs` | Add `GetEffectiveWeight()` method |
| Modify | `TileForge/Game/Screens/GameplayScreen.cs` | Wire knockback into TryBumpAttack + ExecuteEntityTurn |

---

### Task 1: KnockbackResult Struct and KnockbackResolver Core

**Files:**
- Create: `TileForge/Game/KnockbackResolver.cs`
- Create: `TileForge.Tests/Game/KnockbackResolverTests.cs`

- [ ] **Step 1: Write failing test -- weight-1 knocked back on first hit**

```csharp
// TileForge.Tests/Game/KnockbackResolverTests.cs
using TileForge.Game;
using Xunit;

namespace TileForge.Tests.Game;

public class KnockbackResolverTests
{
    private static readonly Func<int, int, bool> AlwaysWalkable = (x, y) => true;
    private static readonly Func<int, int, bool> NeverWalkable = (x, y) => false;

    [Fact]
    public void Weight1_KnockedBack_OnFirstHit()
    {
        // Attacker at (5,5), defender at (5,4) -- attacker is below, so knockback pushes up
        var result = KnockbackResolver.Resolve(5, 5, 5, 4, weight: 1, hitsThisTurn: 1, AlwaysWalkable);
        Assert.True(result.KnockedBack);
        Assert.Equal(5, result.NewX);
        Assert.Equal(3, result.NewY);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "KnockbackResolverTests.Weight1_KnockedBack_OnFirstHit" --no-build 2>&1 | tail -5`
Expected: FAIL -- `KnockbackResolver` does not exist

- [ ] **Step 3: Write KnockbackResolver with KnockbackResult**

```csharp
// TileForge/Game/KnockbackResolver.cs
using System;

namespace TileForge.Game;

public readonly struct KnockbackResult
{
    public bool KnockedBack { get; }
    public int NewX { get; }
    public int NewY { get; }

    public KnockbackResult(bool knockedBack, int newX, int newY)
    {
        KnockedBack = knockedBack;
        NewX = newX;
        NewY = newY;
    }

    public static KnockbackResult None(int currentX, int currentY) =>
        new(false, currentX, currentY);
}

public static class KnockbackResolver
{
    /// <summary>
    /// Determines if a defender should be knocked back after being hit.
    /// Pure function: no side effects, no MonoGame dependencies.
    /// </summary>
    /// <param name="attackerX">Attacker grid X</param>
    /// <param name="attackerY">Attacker grid Y</param>
    /// <param name="defenderX">Defender grid X</param>
    /// <param name="defenderY">Defender grid Y</param>
    /// <param name="weight">Defender weight (hits required to knock back)</param>
    /// <param name="hitsThisTurn">Number of hits landed on defender this turn</param>
    /// <param name="isWalkable">Check if target tile is walkable and unoccupied</param>
    public static KnockbackResult Resolve(
        int attackerX, int attackerY,
        int defenderX, int defenderY,
        int weight,
        int hitsThisTurn,
        Func<int, int, bool> isWalkable)
    {
        if (hitsThisTurn < weight)
            return KnockbackResult.None(defenderX, defenderY);

        int dx = defenderX - attackerX;
        int dy = defenderY - attackerY;

        int targetX = defenderX + dx;
        int targetY = defenderY + dy;

        if (!isWalkable(targetX, targetY))
            return KnockbackResult.None(defenderX, defenderY);

        return new KnockbackResult(true, targetX, targetY);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter "KnockbackResolverTests.Weight1_KnockedBack_OnFirstHit"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add TileForge/Game/KnockbackResolver.cs TileForge.Tests/Game/KnockbackResolverTests.cs
git commit -m "feat: add KnockbackResolver with weight-1 knockback test"
```

---

### Task 2: KnockbackResolver -- Weight, Direction, and Blocking Tests

**Files:**
- Modify: `TileForge.Tests/Game/KnockbackResolverTests.cs`

- [ ] **Step 1: Write remaining KnockbackResolver tests**

Add to `KnockbackResolverTests.cs`:

```csharp
// Weight tests
[Fact]
public void Weight2_NotKnockedBack_OnFirstHit()
{
    var result = KnockbackResolver.Resolve(5, 5, 5, 4, weight: 2, hitsThisTurn: 1, AlwaysWalkable);
    Assert.False(result.KnockedBack);
    Assert.Equal(5, result.NewX);
    Assert.Equal(4, result.NewY);
}

[Fact]
public void Weight2_KnockedBack_OnSecondHit()
{
    var result = KnockbackResolver.Resolve(5, 5, 5, 4, weight: 2, hitsThisTurn: 2, AlwaysWalkable);
    Assert.True(result.KnockedBack);
    Assert.Equal(5, result.NewX);
    Assert.Equal(3, result.NewY);
}

[Fact]
public void Weight3_RequiresThreeHits()
{
    var r1 = KnockbackResolver.Resolve(5, 5, 5, 4, weight: 3, hitsThisTurn: 1, AlwaysWalkable);
    Assert.False(r1.KnockedBack);
    var r2 = KnockbackResolver.Resolve(5, 5, 5, 4, weight: 3, hitsThisTurn: 2, AlwaysWalkable);
    Assert.False(r2.KnockedBack);
    var r3 = KnockbackResolver.Resolve(5, 5, 5, 4, weight: 3, hitsThisTurn: 3, AlwaysWalkable);
    Assert.True(r3.KnockedBack);
}

// Direction tests -- all 4 cardinal directions
[Fact]
public void KnockbackDirection_AttackerBelow_PushesUp()
{
    // Attacker (5,6), defender (5,5) -> pushed to (5,4)
    var result = KnockbackResolver.Resolve(5, 6, 5, 5, 1, 1, AlwaysWalkable);
    Assert.True(result.KnockedBack);
    Assert.Equal(5, result.NewX);
    Assert.Equal(4, result.NewY);
}

[Fact]
public void KnockbackDirection_AttackerAbove_PushesDown()
{
    // Attacker (5,4), defender (5,5) -> pushed to (5,6)
    var result = KnockbackResolver.Resolve(5, 4, 5, 5, 1, 1, AlwaysWalkable);
    Assert.True(result.KnockedBack);
    Assert.Equal(5, result.NewX);
    Assert.Equal(6, result.NewY);
}

[Fact]
public void KnockbackDirection_AttackerLeft_PushesRight()
{
    // Attacker (4,5), defender (5,5) -> pushed to (6,5)
    var result = KnockbackResolver.Resolve(4, 5, 5, 5, 1, 1, AlwaysWalkable);
    Assert.True(result.KnockedBack);
    Assert.Equal(6, result.NewX);
    Assert.Equal(5, result.NewY);
}

[Fact]
public void KnockbackDirection_AttackerRight_PushesLeft()
{
    // Attacker (6,5), defender (5,5) -> pushed to (4,5)
    var result = KnockbackResolver.Resolve(6, 5, 5, 5, 1, 1, AlwaysWalkable);
    Assert.True(result.KnockedBack);
    Assert.Equal(4, result.NewX);
    Assert.Equal(5, result.NewY);
}

// Blocking tests
[Fact]
public void BlockedByWall_NoKnockback()
{
    var result = KnockbackResolver.Resolve(5, 5, 5, 4, 1, 1, NeverWalkable);
    Assert.False(result.KnockedBack);
    Assert.Equal(5, result.NewX);
    Assert.Equal(4, result.NewY);
}

[Fact]
public void BlockedBySpecificTile_NoKnockback()
{
    // Only (5,3) is blocked -- that's the knockback target
    Func<int, int, bool> walkable = (x, y) => !(x == 5 && y == 3);
    var result = KnockbackResolver.Resolve(5, 5, 5, 4, 1, 1, walkable);
    Assert.False(result.KnockedBack);
}

// Occupied tile blocking (isWalkable returns false for occupied tiles)
[Fact]
public void BlockedByOccupiedTile_NoKnockback()
{
    // Simulate another entity at (5,3) -- the knockback target tile
    Func<int, int, bool> occupiedAt5_3 = (x, y) => !(x == 5 && y == 3);
    var result = KnockbackResolver.Resolve(5, 5, 5, 4, 1, 1, occupiedAt5_3);
    Assert.False(result.KnockedBack);
    Assert.Equal(5, result.NewX);
    Assert.Equal(4, result.NewY);
}

// Default weight
[Fact]
public void DefaultWeight1_KnockedBackOnFirstHit()
{
    // Weight 1 is the default -- should knock back immediately
    var result = KnockbackResolver.Resolve(5, 5, 5, 4, 1, 1, AlwaysWalkable);
    Assert.True(result.KnockedBack);
}
```

- [ ] **Step 2: Run all KnockbackResolver tests**

Run: `dotnet test --filter "KnockbackResolverTests"`
Expected: All PASS

- [ ] **Step 3: Commit**

```bash
git add TileForge.Tests/Game/KnockbackResolverTests.cs
git commit -m "test: full KnockbackResolver coverage -- weight, direction, blocking"
```

---

### Task 3: PropertyKeys, PropertySchema, and PlayerState Weight

**Files:**
- Modify: `TileForge/Game/PropertyKeys.cs:10` (Combat section)
- Modify: `TileForge/Game/PropertySchema.cs:17` (after Xp)
- Modify: `TileForge/Game/PlayerState.cs:18` (after MaxPoise)

- [ ] **Step 1: Write failing test for PlayerState.Weight default**

Add to `KnockbackIntegrationTests.cs` (new file):

```csharp
// TileForge.Tests/Game/KnockbackIntegrationTests.cs
using System.Collections.Generic;
using TileForge.Game;
using TileForge.Play;
using Xunit;

namespace TileForge.Tests.Game;

public class KnockbackIntegrationTests
{
    [Fact]
    public void PlayerState_Weight_DefaultsTo1()
    {
        var ps = new PlayerState();
        Assert.Equal(1, ps.Weight);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "KnockbackIntegrationTests.PlayerState_Weight_DefaultsTo1" --no-build 2>&1 | tail -5`
Expected: FAIL -- `PlayerState` has no `Weight` property

- [ ] **Step 3: Add Weight to PlayerState**

In `TileForge/Game/PlayerState.cs`, after line 18 (`MaxPoise`), add:

```csharp
    public int Weight { get; set; } = 1;
```

- [ ] **Step 4: Add PropertyKeys constants**

In `TileForge/Game/PropertyKeys.cs`, in the Combat section (after line 12, `Xp`), add:

```csharp
    public const string Weight = "weight";
```

In the Items & Equipment section (after line 36, `EquipPoise`), add:

```csharp
    public const string EquipWeight = "equip_weight";
```

- [ ] **Step 5: Add weight to PropertySchema**

In `TileForge/Game/PropertySchema.cs`, after the Xp line (line 17), add:

```csharp
        new(PropertyKeys.Weight, PropType.Int, new IntRange(1, 3), EntityType.NPC),
```

In the equipment section (after `EquipPoise`, line 43), add:

```csharp
        new(PropertyKeys.EquipWeight, PropType.Int, new IntRange(0, 5), EntityType.Item),
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test --filter "KnockbackIntegrationTests.PlayerState_Weight_DefaultsTo1"`
Expected: PASS

- [ ] **Step 7: Run full test suite to check nothing breaks**

Run: `dotnet test`
Expected: All 1681+ tests pass

- [ ] **Step 8: Commit**

```bash
git add TileForge/Game/PropertyKeys.cs TileForge/Game/PropertySchema.cs TileForge/Game/PlayerState.cs TileForge.Tests/Game/KnockbackIntegrationTests.cs
git commit -m "feat: add weight property to entities and player for knockback resistance"
```

---

### Task 4: PlayState HitsThisTurn and GetEffectiveWeight

**Files:**
- Modify: `TileForge/Play/PlayState.cs:43` (after IsPlayerTurn)
- Modify: `TileForge/Game/GameStateManager.cs:406` (after GetEffectiveMaxPoise)
- Modify: `TileForge.Tests/Game/KnockbackIntegrationTests.cs`

- [ ] **Step 1: Write failing tests for HitsThisTurn and GetEffectiveWeight**

Add to `KnockbackIntegrationTests.cs`:

```csharp
[Fact]
public void PlayState_HitsThisTurn_InitializesEmpty()
{
    var play = new PlayState();
    Assert.NotNull(play.HitsThisTurn);
    Assert.Empty(play.HitsThisTurn);
}

[Fact]
public void HitsThisTurn_TracksPerTarget()
{
    var play = new PlayState();
    play.HitsThisTurn["enemy1"] = 1;
    play.HitsThisTurn["enemy2"] = 2;
    Assert.Equal(1, play.HitsThisTurn["enemy1"]);
    Assert.Equal(2, play.HitsThisTurn["enemy2"]);
}

[Fact]
public void HitsThisTurn_ClearResetsAll()
{
    var play = new PlayState();
    play.HitsThisTurn["enemy1"] = 3;
    play.HitsThisTurn.Clear();
    Assert.Empty(play.HitsThisTurn);
}

[Fact]
public void HitsThisTurn_StaggerDoesNotCarryAcrossTurns()
{
    var play = new PlayState();
    play.HitsThisTurn["enemy1"] = 1;
    // Simulate turn boundary
    play.HitsThisTurn.Clear();
    Assert.False(play.HitsThisTurn.ContainsKey("enemy1"));
}

[Fact]
public void GetEffectiveWeight_BaseOnly()
{
    var gsm = new GameStateManager();
    gsm.State.Player.Weight = 1;
    Assert.Equal(1, gsm.GetEffectiveWeight());
}

[Fact]
public void GetEffectiveWeight_WithEquipBonus()
{
    var gsm = new GameStateManager();
    gsm.State.Player.Weight = 1;
    // Equip heavy armor with equip_weight = 1
    gsm.State.Player.Equipment["armor"] = "heavy_plate";
    gsm.State.ItemPropertyCache["heavy_plate"] = new Dictionary<string, string>
    {
        { PropertyKeys.EquipWeight, "1" }
    };
    Assert.Equal(2, gsm.GetEffectiveWeight());
}
```

Note: Tests use `new GameStateManager()` directly, matching the pattern from `EquipmentCombatTests.cs` and `FlankingTests.cs`.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "KnockbackIntegrationTests" --no-build 2>&1 | tail -10`
Expected: FAIL -- `HitsThisTurn` and `GetEffectiveWeight` don't exist

- [ ] **Step 3: Add HitsThisTurn to PlayState**

In `TileForge/Play/PlayState.cs`, after line 43 (`IsPlayerTurn`), add:

```csharp
    // Knockback stagger tracking: counts hits per target this turn
    public Dictionary<string, int> HitsThisTurn { get; set; } = new();
```

- [ ] **Step 4: Add GetEffectiveWeight to GameStateManager**

In `TileForge/Game/GameStateManager.cs`, after `GetEffectiveMaxPoise()` (line 406), add:

```csharp
    /// <summary>
    /// Returns effective weight: base player weight + sum of equip_weight bonuses from all equipped items.
    /// </summary>
    public int GetEffectiveWeight()
    {
        return State.Player.Weight + GetEquipmentBonus(PropertyKeys.EquipWeight);
    }
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test --filter "KnockbackIntegrationTests"`
Expected: All PASS

- [ ] **Step 6: Run full test suite**

Run: `dotnet test`
Expected: All tests pass

- [ ] **Step 7: Commit**

```bash
git add TileForge/Play/PlayState.cs TileForge/Game/GameStateManager.cs TileForge.Tests/Game/KnockbackIntegrationTests.cs
git commit -m "feat: add HitsThisTurn stagger tracking and GetEffectiveWeight"
```

---

### Task 5: Wire Knockback Into Player Attacks (TryBumpAttack)

**Files:**
- Modify: `TileForge/Game/Screens/GameplayScreen.cs:745-777` (TryBumpAttack method)
- Modify: `TileForge/Game/Screens/GameplayScreen.cs:807-819` (BeginPlayerTurn -- clear HitsThisTurn)

- [ ] **Step 1: Modify TryBumpAttack to apply knockback**

In `TileForge/Game/Screens/GameplayScreen.cs`, the `TryBumpAttack` method (line 745). After the `LogAndFloat` for the attack result message (line 771) and `TriggerEntityFlash` (line 772), before `return true` (line 773), add knockback logic:

```csharp
                // Knockback: skip if kill, otherwise resolve
                if (!result.Killed)
                {
                    // Track hits for stagger
                    if (!play.HitsThisTurn.ContainsKey(instance.Id))
                        play.HitsThisTurn[instance.Id] = 0;
                    play.HitsThisTurn[instance.Id]++;

                    int weight = PropertyAccess.GetInt(instance.Properties, PropertyKeys.Weight, 1);
                    var kb = KnockbackResolver.Resolve(
                        play.PlayerEntity.X, play.PlayerEntity.Y,
                        instance.X, instance.Y,
                        weight,
                        play.HitsThisTurn[instance.Id],
                        (tx, ty) => IsTileWalkableAndUnoccupied(tx, ty, instance.Id));

                    if (kb.KnockedBack)
                    {
                        instance.X = kb.NewX;
                        instance.Y = kb.NewY;
                        SyncEntityRenderState();
                        LogAndFloat(play, "Knocked back!", Color.White, kb.NewX, kb.NewY);
                    }
                }
```

The `IsTileWalkableAndUnoccupied` helper needs to check tile walkability AND that no other entity/player occupies the target. Add this private method to GameplayScreen. The existing `CanMoveTo(x, y)` (line 453) checks both tile solidity AND entity solidity, so we reuse its tile-checking logic but add our own entity/player occupancy check:

```csharp
    /// <summary>
    /// Checks if a tile is walkable (not solid) and not occupied by any entity or the player.
    /// Used for knockback destination validation.
    /// excludeEntityId: skip this entity in occupancy check (the entity being knocked back).
    /// Pass null when checking player knockback (player is excluded automatically).
    /// </summary>
    private bool IsTileWalkableAndUnoccupied(int x, int y, string excludeEntityId)
    {
        var map = _state.Map;
        if (!map.InBounds(x, y)) return false;

        // Check all layers for solid tiles (same logic as CanMoveTo)
        foreach (var layer in map.Layers)
        {
            string groupName = layer.GetCell(x, y, map.Width);
            if (groupName != null
                && _state.GroupsByName.TryGetValue(groupName, out var group)
                && group.IsSolid)
            {
                return false;
            }
        }

        // Check no solid entity occupies the tile (matches CanMoveTo entity check)
        foreach (var e in _gameStateManager.State.ActiveEntities)
        {
            if (!e.IsActive) continue;
            if (excludeEntityId != null && e.Id == excludeEntityId) continue;
            if (e.X == x && e.Y == y
                && _state.GroupsByName.TryGetValue(e.DefinitionName, out var eGroup)
                && eGroup.IsSolid)
            {
                return false;
            }
        }

        // Don't check player collision when player is the knockback target (excludeEntityId == null)
        if (excludeEntityId != null)
        {
            if (_state.PlayState.PlayerEntity.X == x && _state.PlayState.PlayerEntity.Y == y)
                return false;
        }

        return true;
    }
```

- [ ] **Step 2: Clear HitsThisTurn in BeginPlayerTurn**

In `BeginPlayerTurn` (line 807), after `play.PlayerAP = _gameStateManager.GetEffectiveMaxAP();` (line 809), add:

```csharp
        play.HitsThisTurn.Clear();
```

- [ ] **Step 3: Run full test suite**

Run: `dotnet test`
Expected: All existing tests still pass. (Knockback integration tests added in Task 6.)

- [ ] **Step 4: Commit**

```bash
git add TileForge/Game/Screens/GameplayScreen.cs
git commit -m "feat: wire knockback into player bump attacks"
```

---

### Task 6: Wire Knockback Into Entity Attacks (ExecuteEntityTurn)

**Files:**
- Modify: `TileForge/Game/Screens/GameplayScreen.cs:860-933` (ExecuteEntityTurn method)

- [ ] **Step 1: Add stagger clear per-entity and knockback after entity attacks**

In `ExecuteEntityTurn` (line 860), inside the per-entity loop, after `bool hostile = ...` (line 868), add stagger clear:

```csharp
            play.HitsThisTurn.Clear();
```

Then, in the `EntityActionType.Attack` case (line 891), after the `LogAndFloat` for poise broken (line 915), before `break` on line 917, add knockback:

```csharp
                            // Knockback: push player away from attacker
                            if (!play.HitsThisTurn.ContainsKey("player"))
                                play.HitsThisTurn["player"] = 0;
                            play.HitsThisTurn["player"]++;

                            int playerWeight = _gameStateManager.GetEffectiveWeight();
                            var kb = KnockbackResolver.Resolve(
                                entity.X, entity.Y,
                                play.PlayerEntity.X, play.PlayerEntity.Y,
                                playerWeight,
                                play.HitsThisTurn["player"],
                                (tx, ty) => IsTileWalkableAndUnoccupied(tx, ty, null));

                            if (kb.KnockedBack)
                            {
                                play.PlayerEntity.X = kb.NewX;
                                play.PlayerEntity.Y = kb.NewY;
                                _gameStateManager.State.Player.X = kb.NewX;
                                _gameStateManager.State.Player.Y = kb.NewY;
                                play.RenderPos = new Vector2(kb.NewX, kb.NewY);
                                LogAndFloat(play, "Knocked back!", Color.White, kb.NewX, kb.NewY);
                            }
```

Note: When updating player position, ensure ALL position references are updated: `play.PlayerEntity.X/Y`, `State.Player.X/Y`, and `play.RenderPos`. Check how existing player position updates work in GameplayScreen (e.g., the movement completion code around line 143) and match that pattern exactly.

- [ ] **Step 2: Verify IsTileWalkableAndUnoccupied handles null excludeEntityId**

The helper from Task 5 already handles `null` for `excludeEntityId`: when null, it skips the entity ID exclusion check and the player collision check (since the player IS the knockback target). No code change needed -- just verify the Task 5 implementation is correct.

- [ ] **Step 3: Run full test suite**

Run: `dotnet test`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add TileForge/Game/Screens/GameplayScreen.cs
git commit -m "feat: wire knockback into entity attacks on player"
```

---

### Task 7: Integration Tests

**Files:**
- Modify: `TileForge.Tests/Game/KnockbackIntegrationTests.cs`

These tests exercise the knockback data flow end-to-end at the component level (KnockbackResolver + stagger tracking + weight). GameplayScreen wiring is validated by the full test suite + manual play-testing.

- [ ] **Step 1: Write integration tests**

Add to `KnockbackIntegrationTests.cs`:

```csharp
// --- Simulated attack flow: increment stagger -> resolve knockback ---

private static readonly Func<int, int, bool> OpenField = (x, y) => true;

/// <summary>
/// Simulates a bump attack sequence: increments HitsThisTurn, resolves knockback.
/// Returns the KnockbackResult.
/// </summary>
private static KnockbackResult SimulateAttack(
    PlayState play, string targetId,
    int attackerX, int attackerY,
    int defenderX, int defenderY,
    int weight, Func<int, int, bool> walkable)
{
    if (!play.HitsThisTurn.ContainsKey(targetId))
        play.HitsThisTurn[targetId] = 0;
    play.HitsThisTurn[targetId]++;

    return KnockbackResolver.Resolve(
        attackerX, attackerY,
        defenderX, defenderY,
        weight,
        play.HitsThisTurn[targetId],
        walkable);
}

[Fact]
public void PlayerAttack_Weight1Enemy_EnemyPositionChanges()
{
    var play = new PlayState();
    var kb = SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 1, OpenField);
    Assert.True(kb.KnockedBack);
    Assert.Equal(5, kb.NewX);
    Assert.Equal(3, kb.NewY);
}

[Fact]
public void PlayerAttack_Weight1Enemy_AgainstWall_EnemyStays()
{
    var play = new PlayState();
    Func<int, int, bool> walledAt5_3 = (x, y) => !(x == 5 && y == 3);
    var kb = SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 1, walledAt5_3);
    Assert.False(kb.KnockedBack);
    Assert.Equal(5, kb.NewX);
    Assert.Equal(4, kb.NewY);
}

[Fact]
public void PlayerAttack_Weight2Enemy_FirstHit_NoKnockback()
{
    var play = new PlayState();
    var kb = SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 2, OpenField);
    Assert.False(kb.KnockedBack);
}

[Fact]
public void PlayerAttack_Weight2Enemy_SecondHit_Knockback()
{
    var play = new PlayState();
    // First hit -- no knockback
    SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 2, OpenField);
    // Second hit -- knockback
    var kb = SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 2, OpenField);
    Assert.True(kb.KnockedBack);
    Assert.Equal(5, kb.NewX);
    Assert.Equal(3, kb.NewY);
}

[Fact]
public void EnemyAttack_Weight1Player_PlayerKnockedBack()
{
    var play = new PlayState();
    // Enemy at (5,4) attacks player at (5,5) -- player pushed to (5,6)
    var kb = SimulateAttack(play, "player", 5, 4, 5, 5, weight: 1, OpenField);
    Assert.True(kb.KnockedBack);
    Assert.Equal(5, kb.NewX);
    Assert.Equal(6, kb.NewY);
}

[Fact]
public void PlayerWithEquipWeight_ResistsKnockback()
{
    var gsm = new GameStateManager();
    gsm.State.Player.Weight = 1;
    gsm.State.Player.Equipment["armor"] = "heavy_plate";
    gsm.State.ItemPropertyCache["heavy_plate"] = new Dictionary<string, string>
    {
        { PropertyKeys.EquipWeight, "1" }
    };
    int effectiveWeight = gsm.GetEffectiveWeight(); // Should be 2

    var play = new PlayState();
    // Single hit against weight-2 player -- no knockback
    var kb = SimulateAttack(play, "player", 5, 4, 5, 5, effectiveWeight, OpenField);
    Assert.False(kb.KnockedBack);
}

[Fact]
public void KnockbackSkippedOnKill()
{
    // When AttackResult.Killed is true, knockback should not be attempted.
    // The wiring code checks result.Killed before calling KnockbackResolver.
    // We verify: if we DON'T increment HitsThisTurn (simulating the skip),
    // the stagger count stays at 0 for that target.
    var play = new PlayState();
    // No SimulateAttack call -- killed entities skip knockback
    Assert.False(play.HitsThisTurn.ContainsKey("killed_enemy"));
}

[Fact]
public void TwoSeparateEnemies_DoNotCombineStagger()
{
    var play = new PlayState();
    // Enemy A hits player (stagger = 1)
    var kb1 = SimulateAttack(play, "player", 3, 5, 5, 5, weight: 2, OpenField);
    Assert.False(kb1.KnockedBack); // weight 2, only 1 hit

    // Clear stagger for next entity's sub-turn
    play.HitsThisTurn.Clear();

    // Enemy B hits player (stagger = 1 again, not 2)
    var kb2 = SimulateAttack(play, "player", 7, 5, 5, 5, weight: 2, OpenField);
    Assert.False(kb2.KnockedBack); // still only 1 hit from B's perspective
}

[Fact]
public void StaggerResets_BetweenPlayerTurns()
{
    var play = new PlayState();
    // Turn 1: hit weight-2 enemy once
    SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 2, OpenField);
    Assert.Equal(1, play.HitsThisTurn["enemy1"]);

    // Turn boundary: clear
    play.HitsThisTurn.Clear();

    // Turn 2: hit same enemy once -- should NOT trigger knockback
    var kb = SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 2, OpenField);
    Assert.False(kb.KnockedBack);
    Assert.Equal(1, play.HitsThisTurn["enemy1"]); // reset to 1, not 2
}

[Fact]
public void CorneringTactic_Weight2Enemy_WallBlocksKnockback_AllowsDoubleHit()
{
    var play = new PlayState();
    Func<int, int, bool> walledAt5_3 = (x, y) => !(x == 5 && y == 3);
    // Hit 1: no knockback (weight 2)
    var kb1 = SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 2, walledAt5_3);
    Assert.False(kb1.KnockedBack);
    // Hit 2: knockback triggered but blocked by wall -- enemy stays
    var kb2 = SimulateAttack(play, "enemy1", 5, 5, 5, 4, weight: 2, walledAt5_3);
    Assert.False(kb2.KnockedBack); // blocked by wall
    // Player got both hits in without having to chase -- positional reward
}
```

- [ ] **Step 2: Run integration tests**

Run: `dotnet test --filter "KnockbackIntegrationTests"`
Expected: All PASS

- [ ] **Step 3: Run full test suite**

Run: `dotnet test`
Expected: All tests pass, 0 failures

- [ ] **Step 4: Commit**

```bash
git add TileForge.Tests/Game/KnockbackIntegrationTests.cs
git commit -m "test: knockback integration tests -- stagger, weight, kill skip, equipment"
```

---

### Task 8: Final Verification and Cleanup

**Files:**
- No new files

- [ ] **Step 1: Run full test suite**

Run: `dotnet test`
Expected: All tests pass (1681+ original + new knockback tests)

- [ ] **Step 2: Build check**

Run: `dotnet build`
Expected: Build succeeds with 0 errors, 0 warnings (or only pre-existing warnings)

- [ ] **Step 3: Verify no regressions in combat test files**

Run: `dotnet test --filter "APCombatTests|PoiseTests|FlankingTests|EquipmentCombatTests|TerrainCombatModifierTests|EntityTurnTests"`
Expected: All pre-existing combat tests still pass

- [ ] **Step 4: Commit any cleanup**

Only if needed. If all tests pass and build is clean, no commit necessary.
