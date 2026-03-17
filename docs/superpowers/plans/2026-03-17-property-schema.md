# Property Schema Registry Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Centralize stringly-typed entity property bags into a schema registry with compile-time key constants, typed access helpers, and editor/load-time validation.

**Architecture:** Keep `Dictionary<string, string>` everywhere for serialization. Add a `PropertySchema` registry that defines all known properties, their types, and constraints. All gameplay code uses `PropertyKeys` constants and `PropertyAccess` helpers instead of raw string literals and inline parsing. A `PropertyValidator` checks properties on play-mode enter.

**Tech Stack:** C# / .NET 9.0 / MonoGame / xUnit

**Spec:** `docs/superpowers/specs/2026-03-17-property-schema-design.md`

---

## File Structure

### New Files
| File | Responsibility |
|------|---------------|
| `TileForge/Game/PropertyKeys.cs` | `const string` fields for all ~30 known property keys |
| `TileForge/Game/PropertyDef.cs` | `IntRange` record, `PropType` enum, `PropertyDef` class with `Validate()` |
| `TileForge/Game/PropertySchema.cs` | Static registry of all `PropertyDef` entries, lookup helpers |
| `TileForge/Game/PropertyAccess.cs` | `GetInt`, `GetBool`, `GetString`, `SetInt` typed helpers |
| `TileForge/Game/PropertyValidator.cs` | `PropertyErrorLevel`, `PropertyError`, `PropertyValidator.Validate()` |
| `TileForge.Tests/Game/PropertyAccessTests.cs` | Tests for typed access helpers |
| `TileForge.Tests/Game/PropertyValidatorTests.cs` | Tests for validation logic |
| `TileForge.Tests/Game/PropertySchemaTests.cs` | Tests for schema completeness and queries |
| `TileForge.Tests/Game/PropertyDefTests.cs` | Tests for PropertyDef.Validate() |

### Modified Files
| File | Change |
|------|--------|
| `TileForge/UI/GroupEditor.cs:104-121, 661-701` | Replace `Presets`/`NumericSpecs` with schema queries |
| `TileForge/Game/EntityAI.cs:7-161` | Replace string literals with `PropertyKeys`, inline parsing with `PropertyAccess` |
| `TileForge/Game/GameStateManager.cs:297-591` | Replace string literals, use `PropertyAccess` (helper deletion deferred to Task 10) |
| `TileForge/Game/Screens/GameplayScreen.cs:500-922` | Replace string literals and inline parsing with `PropertyKeys`/`PropertyAccess`, wire validator |
| `TileForge/Game/TriggerManager.cs:14-22` | Replace string literals with `PropertyKeys` |
| `TileForge/UI/MapCanvas.cs:365-378` | Replace string literal with `PropertyKeys` |
| `TileForge/Game/Screens/InventoryScreen.cs:167-171` | Replace string literals with `PropertyKeys`/`PropertyAccess` |

---

### Task 1: PropertyKeys and PropertyDef (foundation types)

**Files:**
- Create: `TileForge/Game/PropertyKeys.cs`
- Create: `TileForge/Game/PropertyDef.cs`
- Create: `TileForge.Tests/Game/PropertyDefTests.cs`

- [ ] **Step 1: Write PropertyDef tests**

```csharp
using Xunit;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class PropertyDefTests
{
    [Fact]
    public void Validate_Int_ValidValue_ReturnsTrue()
    {
        var def = new PropertyDef("health", PropType.Int, new IntRange(1, 9999), EntityType.NPC);
        Assert.True(def.Validate("100", out var reason));
        Assert.Null(reason);
    }

    [Fact]
    public void Validate_Int_NonNumeric_ReturnsFalse()
    {
        var def = new PropertyDef("health", PropType.Int, new IntRange(1, 9999), EntityType.NPC);
        Assert.False(def.Validate("abc", out var reason));
        Assert.Contains("Expected integer", reason);
    }

    [Fact]
    public void Validate_Int_OutOfRange_ReturnsFalse()
    {
        var def = new PropertyDef("health", PropType.Int, new IntRange(1, 9999), EntityType.NPC);
        Assert.False(def.Validate("0", out var reason));
        Assert.Contains("outside range", reason);
    }

    [Fact]
    public void Validate_Int_NoRange_AnyIntValid()
    {
        var def = new PropertyDef("patrol_origin", PropType.Int, EntityType.NPC);
        Assert.True(def.Validate("-5", out _));
        Assert.True(def.Validate("99999", out _));
    }

    [Fact]
    public void Validate_Bool_ValidValues()
    {
        var def = new PropertyDef("hostile", PropType.Bool, EntityType.NPC);
        Assert.True(def.Validate("true", out _));
        Assert.True(def.Validate("false", out _));
        Assert.True(def.Validate("TRUE", out _));
    }

    [Fact]
    public void Validate_Bool_InvalidValue_ReturnsFalse()
    {
        var def = new PropertyDef("hostile", PropType.Bool, EntityType.NPC);
        Assert.False(def.Validate("yes", out var reason));
        Assert.Contains("Expected true/false", reason);
    }

    [Fact]
    public void Validate_Enum_ValidValue()
    {
        var def = new PropertyDef("behavior", PropType.Enum,
            new[] { "idle", "chase", "patrol" }, EntityType.NPC);
        Assert.True(def.Validate("chase", out _));
    }

    [Fact]
    public void Validate_Enum_InvalidValue_ReturnsFalse()
    {
        var def = new PropertyDef("behavior", PropType.Enum,
            new[] { "idle", "chase", "patrol" }, EntityType.NPC);
        Assert.False(def.Validate("flee", out var reason));
        Assert.Contains("not in", reason);
    }

    [Fact]
    public void Validate_EmptyString_AlwaysValid()
    {
        var def = new PropertyDef("health", PropType.Int, new IntRange(1, 9999), EntityType.NPC);
        Assert.True(def.Validate("", out _));
        Assert.True(def.Validate(null, out _));
    }

    [Fact]
    public void Validate_String_AlwaysValid()
    {
        var def = new PropertyDef("hostile_flag", PropType.String, EntityType.NPC);
        Assert.True(def.Validate("anything", out _));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~PropertyDefTests" --no-build 2>&1 || true`
Expected: Build failure — `PropertyDef` not found

- [ ] **Step 3: Write PropertyKeys.cs**

Create `TileForge/Game/PropertyKeys.cs` with all const string fields exactly as specified in the design spec (lines 32-83).

- [ ] **Step 4: Write PropertyDef.cs**

Create `TileForge/Game/PropertyDef.cs` with `IntRange` record, `PropType` enum, and `PropertyDef` class exactly as specified in the design spec (lines 91-171).

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~PropertyDefTests"`
Expected: All 10 tests PASS

- [ ] **Step 6: Commit**

```bash
git add TileForge/Game/PropertyKeys.cs TileForge/Game/PropertyDef.cs TileForge.Tests/Game/PropertyDefTests.cs
git commit -m "feat: add PropertyKeys constants and PropertyDef with validation"
```

---

### Task 2: PropertySchema registry

**Files:**
- Create: `TileForge/Game/PropertySchema.cs`
- Create: `TileForge.Tests/Game/PropertySchemaTests.cs`

- [ ] **Step 1: Write PropertySchema tests**

```csharp
using System.Linq;
using Xunit;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class PropertySchemaTests
{
    [Fact]
    public void All_NoDuplicateKeys()
    {
        var keys = PropertySchema.All.Select(d => d.Key).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    [Fact]
    public void Get_KnownKey_ReturnsDef()
    {
        var def = PropertySchema.Get("health");
        Assert.NotNull(def);
        Assert.Equal(PropType.Int, def.Type);
    }

    [Fact]
    public void Get_UnknownKey_ReturnsNull()
    {
        Assert.Null(PropertySchema.Get("nonexistent"));
    }

    [Fact]
    public void IsKnown_KnownKey_ReturnsTrue()
    {
        Assert.True(PropertySchema.IsKnown("behavior"));
    }

    [Fact]
    public void IsKnown_UnknownKey_ReturnsFalse()
    {
        Assert.False(PropertySchema.IsKnown("nonexistent"));
    }

    [Fact]
    public void ForEntityType_NPC_IncludesHealth()
    {
        var keys = PropertySchema.ForEntityType(EntityType.NPC).Select(d => d.Key);
        Assert.Contains("health", keys);
    }

    [Fact]
    public void ForEntityType_Item_ExcludesHealth()
    {
        var keys = PropertySchema.ForEntityType(EntityType.Item).Select(d => d.Key);
        Assert.DoesNotContain("health", keys);
    }

    [Fact]
    public void ForEntityType_Item_IncludesHeal()
    {
        var keys = PropertySchema.ForEntityType(EntityType.Item).Select(d => d.Key);
        Assert.Contains("heal", keys);
    }

    [Fact]
    public void ForEntityType_Trigger_IncludesTargetMap()
    {
        var keys = PropertySchema.ForEntityType(EntityType.Trigger).Select(d => d.Key);
        Assert.Contains("target_map", keys);
    }

    [Fact]
    public void DialogueId_AppliesToAllEntityTypes()
    {
        var def = PropertySchema.Get("dialogue_id");
        Assert.NotNull(def);
        Assert.Contains(EntityType.NPC, def.AppliesTo);
        Assert.Contains(EntityType.Item, def.AppliesTo);
        Assert.Contains(EntityType.Trap, def.AppliesTo);
        Assert.Contains(EntityType.Trigger, def.AppliesTo);
        Assert.Contains(EntityType.Interactable, def.AppliesTo);
    }

    [Fact]
    public void AllPropertyKeys_HaveMatchingSchemaEntry()
    {
        // Verify every constant in PropertyKeys is registered in the schema.
        // This catches drift between the two.
        var fields = typeof(PropertyKeys).GetFields(
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        foreach (var field in fields)
        {
            var key = (string)field.GetValue(null);
            Assert.True(PropertySchema.IsKnown(key),
                $"PropertyKeys.{field.Name} = \"{key}\" has no entry in PropertySchema");
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~PropertySchemaTests" --no-build 2>&1 || true`
Expected: Build failure — `PropertySchema` not found

- [ ] **Step 3: Write PropertySchema.cs**

Create `TileForge/Game/PropertySchema.cs` exactly as specified in the design spec (lines 178-249).

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~PropertySchemaTests"`
Expected: All 11 tests PASS

- [ ] **Step 5: Commit**

```bash
git add TileForge/Game/PropertySchema.cs TileForge.Tests/Game/PropertySchemaTests.cs
git commit -m "feat: add PropertySchema registry with lookup helpers"
```

---

### Task 3: PropertyAccess typed helpers

**Files:**
- Create: `TileForge/Game/PropertyAccess.cs`
- Create: `TileForge.Tests/Game/PropertyAccessTests.cs`

- [ ] **Step 1: Write PropertyAccess tests**

```csharp
using System.Collections.Generic;
using Xunit;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class PropertyAccessTests
{
    [Fact]
    public void GetInt_ValidValue_ReturnsInt()
    {
        var props = new Dictionary<string, string> { { "health", "42" } };
        Assert.Equal(42, PropertyAccess.GetInt(props, "health"));
    }

    [Fact]
    public void GetInt_MissingKey_ReturnsDefault()
    {
        var props = new Dictionary<string, string>();
        Assert.Equal(10, PropertyAccess.GetInt(props, "health", 10));
    }

    [Fact]
    public void GetInt_NonNumeric_ReturnsDefault()
    {
        var props = new Dictionary<string, string> { { "health", "abc" } };
        Assert.Equal(0, PropertyAccess.GetInt(props, "health"));
    }

    [Fact]
    public void GetBool_True_ReturnsTrue()
    {
        var props = new Dictionary<string, string> { { "hostile", "true" } };
        Assert.True(PropertyAccess.GetBool(props, "hostile"));
    }

    [Fact]
    public void GetBool_False_ReturnsFalse()
    {
        var props = new Dictionary<string, string> { { "hostile", "false" } };
        Assert.False(PropertyAccess.GetBool(props, "hostile"));
    }

    [Fact]
    public void GetBool_FalseCaseInsensitive()
    {
        var props = new Dictionary<string, string> { { "hostile", "FALSE" } };
        Assert.False(PropertyAccess.GetBool(props, "hostile"));
    }

    [Fact]
    public void GetBool_MissingKey_ReturnsDefault()
    {
        var props = new Dictionary<string, string>();
        Assert.False(PropertyAccess.GetBool(props, "hostile"));
        Assert.True(PropertyAccess.GetBool(props, "hostile", true));
    }

    [Fact]
    public void GetBool_NonBoolValue_ReturnsTrue()
    {
        // Matches existing IsEntityHostile semantics: anything that isn't "false" is true
        var props = new Dictionary<string, string> { { "hostile", "banana" } };
        Assert.True(PropertyAccess.GetBool(props, "hostile"));
    }

    [Fact]
    public void GetString_ValidValue_ReturnsValue()
    {
        var props = new Dictionary<string, string> { { "dialogue_id", "npc_hello" } };
        Assert.Equal("npc_hello", PropertyAccess.GetString(props, "dialogue_id"));
    }

    [Fact]
    public void GetString_MissingKey_ReturnsDefault()
    {
        var props = new Dictionary<string, string>();
        Assert.Equal("", PropertyAccess.GetString(props, "dialogue_id"));
        Assert.Equal("fallback", PropertyAccess.GetString(props, "dialogue_id", "fallback"));
    }

    [Fact]
    public void GetString_EmptyValue_ReturnsDefault()
    {
        var props = new Dictionary<string, string> { { "dialogue_id", "" } };
        Assert.Equal("fallback", PropertyAccess.GetString(props, "dialogue_id", "fallback"));
    }

    [Fact]
    public void SetInt_SetsStringValue()
    {
        var props = new Dictionary<string, string>();
        PropertyAccess.SetInt(props, "health", 42);
        Assert.Equal("42", props["health"]);
    }

    [Fact]
    public void SetInt_OverwritesExisting()
    {
        var props = new Dictionary<string, string> { { "health", "100" } };
        PropertyAccess.SetInt(props, "health", 50);
        Assert.Equal("50", props["health"]);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~PropertyAccessTests" --no-build 2>&1 || true`
Expected: Build failure — `PropertyAccess` not found

- [ ] **Step 3: Write PropertyAccess.cs**

Create `TileForge/Game/PropertyAccess.cs` exactly as specified in the design spec (lines 257-286).

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~PropertyAccessTests"`
Expected: All 13 tests PASS

- [ ] **Step 5: Commit**

```bash
git add TileForge/Game/PropertyAccess.cs TileForge.Tests/Game/PropertyAccessTests.cs
git commit -m "feat: add PropertyAccess typed read/write helpers"
```

---

### Task 4: PropertyValidator

**Files:**
- Create: `TileForge/Game/PropertyValidator.cs`
- Create: `TileForge.Tests/Game/PropertyValidatorTests.cs`

- [ ] **Step 1: Write PropertyValidator tests**

```csharp
using System.Collections.Generic;
using System.Linq;
using Xunit;
using TileForge.Game;

namespace TileForge.Tests.Game;

public class PropertyValidatorTests
{
    private static EntityInstance MakeEntity(string id, params (string key, string value)[] props)
    {
        var entity = new EntityInstance { Id = id, DefinitionName = "Test", Properties = new() };
        foreach (var (k, v) in props)
            entity.Properties[k] = v;
        return entity;
    }

    [Fact]
    public void Validate_ValidProperties_NoErrors()
    {
        var entities = new List<EntityInstance>
        {
            MakeEntity("e1", ("health", "100"), ("behavior", "idle")),
        };
        var errors = PropertyValidator.Validate(entities);
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_UnknownProperty_Warning()
    {
        var entities = new List<EntityInstance>
        {
            MakeEntity("e1", ("custom_prop", "value")),
        };
        var errors = PropertyValidator.Validate(entities);
        Assert.Single(errors);
        Assert.Equal(PropertyErrorLevel.Warning, errors[0].Level);
        Assert.Equal("custom_prop", errors[0].Key);
        Assert.Contains("Unknown", errors[0].Message);
    }

    [Fact]
    public void Validate_InvalidInt_Error()
    {
        var entities = new List<EntityInstance>
        {
            MakeEntity("e1", ("health", "abc")),
        };
        var errors = PropertyValidator.Validate(entities);
        Assert.Single(errors);
        Assert.Equal(PropertyErrorLevel.Error, errors[0].Level);
        Assert.Contains("Expected integer", errors[0].Message);
    }

    [Fact]
    public void Validate_IntOutOfRange_Error()
    {
        var entities = new List<EntityInstance>
        {
            MakeEntity("e1", ("health", "0")),
        };
        var errors = PropertyValidator.Validate(entities);
        Assert.Single(errors);
        Assert.Equal(PropertyErrorLevel.Error, errors[0].Level);
        Assert.Contains("outside range", errors[0].Message);
    }

    [Fact]
    public void Validate_InvalidEnum_Error()
    {
        var entities = new List<EntityInstance>
        {
            MakeEntity("e1", ("behavior", "flee")),
        };
        var errors = PropertyValidator.Validate(entities);
        Assert.Single(errors);
        Assert.Equal(PropertyErrorLevel.Error, errors[0].Level);
        Assert.Contains("not in", errors[0].Message);
    }

    [Fact]
    public void Validate_InvalidBool_Error()
    {
        var entities = new List<EntityInstance>
        {
            MakeEntity("e1", ("hostile", "yes")),
        };
        var errors = PropertyValidator.Validate(entities);
        Assert.Single(errors);
        Assert.Equal(PropertyErrorLevel.Error, errors[0].Level);
        Assert.Contains("Expected true/false", errors[0].Message);
    }

    [Fact]
    public void Validate_EmptyList_NoErrors()
    {
        var errors = PropertyValidator.Validate(new List<EntityInstance>());
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_MultipleEntities_CollectsAll()
    {
        var entities = new List<EntityInstance>
        {
            MakeEntity("e1", ("health", "abc")),
            MakeEntity("e2", ("behavior", "flee")),
        };
        var errors = PropertyValidator.Validate(entities);
        Assert.Equal(2, errors.Count);
        Assert.Equal("e1", errors[0].EntityId);
        Assert.Equal("e2", errors[1].EntityId);
    }

    [Fact]
    public void Validate_EntityId_IncludedInError()
    {
        var entities = new List<EntityInstance>
        {
            MakeEntity("goblin_01", ("health", "abc")),
        };
        var errors = PropertyValidator.Validate(entities);
        Assert.Equal("goblin_01", errors[0].EntityId);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~PropertyValidatorTests" --no-build 2>&1 || true`
Expected: Build failure — `PropertyValidator` not found

- [ ] **Step 3: Write PropertyValidator.cs**

Create `TileForge/Game/PropertyValidator.cs` exactly as specified in the design spec (lines 294-337).

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~PropertyValidatorTests"`
Expected: All 9 tests PASS

- [ ] **Step 5: Run all existing tests to confirm no regressions**

Run: `dotnet test TileForge.Tests`
Expected: All 1681+ tests PASS

- [ ] **Step 6: Commit**

```bash
git add TileForge/Game/PropertyValidator.cs TileForge.Tests/Game/PropertyValidatorTests.cs
git commit -m "feat: add PropertyValidator for load-time diagnostics"
```

---

### Task 5: Migrate EntityAI.cs

**Files:**
- Modify: `TileForge/Game/EntityAI.cs:7-161`

This file reads `behavior`, `aggro_range`, `alert_turns`, `patrol_axis`, `patrol_range`, `patrol_origin`, `patrol_dir` as string literals with inline `int.TryParse` calls.

- [ ] **Step 1: Replace string literals with PropertyKeys**

In `TileForge/Game/EntityAI.cs`, replace all property key string literals:
- `"behavior"` → `PropertyKeys.Behavior`
- `"aggro_range"` → `PropertyKeys.AggroRange`
- `"alert_turns"` → `PropertyKeys.AlertTurns`
- `"patrol_axis"` → `PropertyKeys.PatrolAxis`
- `"patrol_range"` → `PropertyKeys.PatrolRange`
- `"patrol_origin"` → `PropertyKeys.PatrolOrigin`
- `"patrol_dir"` → `PropertyKeys.PatrolDir`

- [ ] **Step 2: Replace inline int parsing with PropertyAccess**

Replace patterns like:
```csharp
entity.Properties.TryGetValue("aggro_range", out var rangeStr);
int range = 5;
if (rangeStr != null) int.TryParse(rangeStr, out range);
```
With:
```csharp
int range = PropertyAccess.GetInt(entity.Properties, PropertyKeys.AggroRange, 5);
```

Apply to all int property reads in EntityAI (aggro_range, alert_turns, patrol_range, patrol_origin, patrol_dir).

Note: Keep `TryGetValue` for `PropertyKeys.Behavior` since it needs the string value, not an int. Use `PropertyAccess.GetString` there.

- [ ] **Step 3: Run existing EntityAI tests**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~EntityAITests"`
Expected: All existing tests PASS (behavior unchanged)

- [ ] **Step 4: Commit**

```bash
git add TileForge/Game/EntityAI.cs
git commit -m "refactor: migrate EntityAI to PropertyKeys + PropertyAccess"
```

---

### Task 6: Migrate GameStateManager.cs

**Files:**
- Modify: `TileForge/Game/GameStateManager.cs:297-591`

This is the largest migration. GameStateManager has its own `GetEntityIntProperty`/`SetEntityIntProperty` helpers (which `PropertyAccess` replaces), plus string literals in `IsEntityHostile`, `CollectItem`, `AttackEntity`, and equipment bonus methods.

- [ ] **Step 1: Replace string literals with PropertyKeys in IsEntityHostile (lines 454-465)**

Replace `"friendly_flag"`, `"hostile_flag"`, `"hostile"` with `PropertyKeys.FriendlyFlag`, `PropertyKeys.HostileFlag`, `PropertyKeys.Hostile`. Use `PropertyAccess.GetString` for the flag lookups.

- [ ] **Step 2: Replace string literals in CollectItem (lines 297-311)**

Replace `"on_collect_set_flag"` and `"on_collect_increment"` with `PropertyKeys.OnCollectSetFlag` and `PropertyKeys.OnCollectIncrement`. Use `PropertyAccess.GetString` for flag lookups.

- [ ] **Step 3: Replace string literals in equipment methods (lines 379-434)**

Replace `"equip_attack"`, `"equip_defense"`, `"equip_ap"`, `"equip_poise"` with PropertyKeys constants. In `GetEquipmentBonus`, replace the inline `TryGetValue` + `int.TryParse` with `PropertyAccess.GetInt`.

- [ ] **Step 4: Replace GetEntityIntProperty/SetEntityIntProperty calls in AttackEntity (lines 488-591)**

Replace all `GetEntityIntProperty(entity, "defense", 0)` calls with `PropertyAccess.GetInt(entity.Properties, PropertyKeys.Defense)`, and similarly for `"health"`, `"poise"`, `"max_health"`, `"xp"`. Replace `SetEntityIntProperty` calls with `PropertyAccess.SetInt`. Also replace `"on_kill_set_flag"` and `"on_kill_increment"` string literals.

- [ ] **Step 5: Migrate GetItemEquipSlot (line 370)**

Replace `"equip_slot"` string literal with `PropertyKeys.EquipSlot`.

- [ ] **Step 6: Migrate IsAttackable (line 476)**

Replace `GetEntityIntProperty(entity, "health", 0)` with `PropertyAccess.GetInt(entity.Properties, PropertyKeys.Health)`.

- [ ] **Step 7: Do NOT delete GetEntityIntProperty/SetEntityIntProperty yet**

These methods are still called by GameplayScreen (Task 7). They will be deleted in Task 10 after all call sites are migrated.

- [ ] **Step 8: Run GameStateManager and combat tests**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~GameStateManager" && dotnet test TileForge.Tests --filter "FullyQualifiedName~AttackTests" && dotnet test TileForge.Tests --filter "FullyQualifiedName~CombatTests" && dotnet test TileForge.Tests --filter "FullyQualifiedName~EquipmentCombatTests" && dotnet test TileForge.Tests --filter "FullyQualifiedName~PoiseTests" && dotnet test TileForge.Tests --filter "FullyQualifiedName~ItemCollectionTests"`
Expected: All PASS

- [ ] **Step 9: Commit**

```bash
git add TileForge/Game/GameStateManager.cs
git commit -m "refactor: migrate GameStateManager to PropertyKeys + PropertyAccess"
```

---

### Task 7: Migrate GameplayScreen.cs

**Files:**
- Modify: `TileForge/Game/Screens/GameplayScreen.cs:500-922`

GameplayScreen reads trap damage, trigger transitions, noise propagation, entity turn speed/attack, and alert turns. It also uses `GameStateManager.GetEntityIntProperty`/`SetEntityIntProperty` which were removed in Task 6 — these calls must be updated too.

- [ ] **Step 1: Replace trap damage (lines 500-518)**

Replace `"damage"` string literal and inline `int.TryParse` with:
```csharp
int damage = PropertyAccess.GetInt(instance.Properties, PropertyKeys.Damage);
```

- [ ] **Step 2: Replace trigger transitions (lines 521-527)**

Replace `"target_map"`, `"target_x"`, `"target_y"` string literals with PropertyKeys constants and `PropertyAccess`:
```csharp
var targetMap = PropertyAccess.GetString(instance.Properties, PropertyKeys.TargetMap);
if (!string.IsNullOrEmpty(targetMap))
{
    int tx = PropertyAccess.GetInt(instance.Properties, PropertyKeys.TargetX);
    int ty = PropertyAccess.GetInt(instance.Properties, PropertyKeys.TargetY);
    ...
}
```

- [ ] **Step 3: Replace PropagateNoise property reads (lines 678-689)**

Replace `"behavior"` check with `PropertyKeys.Behavior`. Replace `_gameStateManager.GetEntityIntProperty(entity, "aggro_range", 5)` with `PropertyAccess.GetInt(entity.Properties, PropertyKeys.AggroRange, 5)`. Same for `"alert_turns"` reads and writes. Replace `_gameStateManager.SetEntityIntProperty` with `PropertyAccess.SetInt`.

- [ ] **Step 4: Replace ExecuteEntityTurn property reads (lines 851-922)**

Replace `"behavior"` check with `PropertyKeys.Behavior`. Replace `_gameStateManager.GetEntityIntProperty(entity, "speed", 1)` with `PropertyAccess.GetInt(entity.Properties, PropertyKeys.Speed, 1)`. Replace `"attack"` reads. Replace `"alert_turns"` reads/writes with PropertyAccess calls.

- [ ] **Step 5: Replace AnyHostileNearby property reads (lines 773-792)**

Replace `ContainsKey("behavior")` with `ContainsKey(PropertyKeys.Behavior)`. Replace `_gameStateManager.GetEntityIntProperty(entity, "aggro_range", 5)` with `PropertyAccess.GetInt(entity.Properties, PropertyKeys.AggroRange, 5)`. Replace `"alert_turns"` reads with `PropertyAccess.GetInt(entity.Properties, PropertyKeys.AlertTurns, 0)`.

- [ ] **Step 6: Replace dialogue property reads (lines ~981, ~1028)**

Replace `"on_pickup_dialogue"` with `PropertyKeys.OnPickupDialogue` and `"dialogue_id"` with `PropertyKeys.DialogueId`. Also replace `ContainsKey("dialogue_id")` and `ContainsKey("dialogue")` at lines ~990-991 with `PropertyKeys.DialogueId` and `PropertyKeys.Dialogue`. Use `PropertyAccess.GetString` where applicable.

- [ ] **Step 7: Wire PropertyValidator into initialization**

After `_gameStateManager.Initialize()` (find the exact location), add:
```csharp
var validationErrors = PropertyValidator.Validate(_gameStateManager.State.ActiveEntities);
foreach (var error in validationErrors)
{
    var prefix = error.Level == PropertyErrorLevel.Error ? "ERROR" : "WARN";
    _gameStateManager.AddLogMessage($"[{prefix}] {error.EntityId}.{error.Key}: {error.Message}");
}
```

Note: Check if `GameStateManager` has an `AddLogMessage` method or if messages go through `LogAndFloat`. Use whatever the existing message log pattern is.

- [ ] **Step 8: Run GameplayScreen and integration tests**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~GameplayScreen" && dotnet test TileForge.Tests --filter "FullyQualifiedName~MapTransitionTests" && dotnet test TileForge.Tests --filter "FullyQualifiedName~EntityTurnTests" && dotnet test TileForge.Tests --filter "FullyQualifiedName~NoiseAlertTests"`
Expected: All PASS

- [ ] **Step 9: Commit**

```bash
git add TileForge/Game/Screens/GameplayScreen.cs
git commit -m "refactor: migrate GameplayScreen to PropertyKeys + PropertyAccess, wire validator"
```

---

### Task 8: Migrate TriggerManager, MapCanvas, InventoryScreen

**Files:**
- Modify: `TileForge/Game/TriggerManager.cs:14-22`
- Modify: `TileForge/UI/MapCanvas.cs:365-378`
- Modify: `TileForge/Game/Screens/InventoryScreen.cs:167-171`

These are small, independent changes.

- [ ] **Step 1: Migrate TriggerManager**

Replace `"dialogue_id"` with `PropertyKeys.DialogueId` and `"dialogue"` with `PropertyKeys.Dialogue`. Keep the existing `TryGetValue` pattern since it checks for non-empty strings — just swap the key constant.

- [ ] **Step 2: Migrate MapCanvas**

Replace `"default_facing"` with `PropertyKeys.DefaultFacing`. Keep the existing `TryGetValue` pattern on `group.DefaultProperties`.

- [ ] **Step 3: Migrate InventoryScreen**

Replace `"heal"` with `PropertyKeys.Heal`. Replace the inline `TryGetValue` + `int.TryParse` with `PropertyAccess.GetInt`:
```csharp
int healAmount = 0;
if (_gameStateManager.State.ItemPropertyCache.TryGetValue(itemName, out var props))
    healAmount = PropertyAccess.GetInt(props, PropertyKeys.Heal);
```

- [ ] **Step 4: Run relevant tests**

Run: `dotnet test TileForge.Tests --filter "FullyQualifiedName~InventoryScreenTests" && dotnet test TileForge.Tests --filter "FullyQualifiedName~DialogueV2Tests"`
Expected: All PASS

- [ ] **Step 5: Commit**

```bash
git add TileForge/Game/TriggerManager.cs TileForge/UI/MapCanvas.cs TileForge/Game/Screens/InventoryScreen.cs
git commit -m "refactor: migrate TriggerManager, MapCanvas, InventoryScreen to PropertyKeys"
```

---

### Task 9: Migrate GroupEditor to schema-driven UI

**Files:**
- Modify: `TileForge/UI/GroupEditor.cs:104-121, 661-701`

This replaces the hardcoded `Presets` and `NumericSpecs` dictionaries with schema queries, and rewrites `CreatePropField` to use `PropertyDef.Type`.

- [ ] **Step 1: Remove Presets and NumericSpecs dictionaries (lines 104-121)**

Delete the `Presets` and `NumericSpecs` static dictionaries. Also delete the static dropdown item arrays (`BehaviorItems`, `EquipSlotItems`, `FacingItems`, `HostileItems`) if they exist — these values now come from `PropertyDef.AllowedValues`.

- [ ] **Step 2: Replace preset usage with schema query**

Wherever `Presets[entityType]` is used to get the list of property keys for an entity type, replace with:
```csharp
PropertySchema.ForEntityType(entityType).Select(d => d.Key).ToArray()
```

- [ ] **Step 3: Rewrite CreatePropField (lines 661-701)**

Replace the entire method body with schema-driven field creation:
```csharp
private PropField CreatePropField(string key, string value)
{
    var def = PropertySchema.Get(key);
    if (def != null)
    {
        switch (def.Type)
        {
            case PropType.Int:
                int v = int.TryParse(value, out int parsed) ? parsed : (def.Range?.Min ?? 0);
                return new PropField
                {
                    Key = key, Kind = PFK.Numeric,
                    NF = new NumericField(v, def.Range?.Min ?? 0, def.Range?.Max ?? 9999)
                };

            case PropType.Bool:
                int boolIdx = string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                return new PropField
                {
                    Key = key, Kind = PFK.Dropdown,
                    DD = new Dropdown(new[] { "true", "false" }, boolIdx)
                };

            case PropType.Enum:
                int enumIdx = Array.IndexOf(def.AllowedValues, value?.ToLower() ?? "");
                return new PropField
                {
                    Key = key, Kind = PFK.Dropdown,
                    DD = new Dropdown(def.AllowedValues, Math.Max(0, enumIdx))
                };

            case PropType.MapRef:
            case PropType.DialogueRef:
                var items = GetDropdownItems(key);
                int refIdx = Array.IndexOf(items, value);
                return new PropField
                {
                    Key = key, Kind = PFK.Dropdown,
                    DD = new Dropdown(items, Math.Max(0, refIdx))
                };
        }
    }

    // Unknown property -- text field (String type handled above via default in switch)
    // Note: PropField.Key must remain the raw key for CollectCurrentProperties.
    // The "* " warning prefix for unknown properties should be applied at the
    // label rendering level, not stored in the key. Check how PropField labels
    // are rendered and add the prefix there if unknown.
    return new PropField { Key = key, Kind = PFK.Text, TF = new TextInputField(value ?? "", maxLength: 512) };
}
```

Note: The `GetDropdownItems` method already exists and handles map/dialogue dropdowns. Check if `key` needs to remain the original key (without the `*` prefix) for `PropField.Key` — the `*` is only for display. Adjust the label vs key handling based on how `PropField` works.

- [ ] **Step 4: Run full test suite**

Run: `dotnet test TileForge.Tests`
Expected: All 1681+ tests PASS

- [ ] **Step 5: Commit**

```bash
git add TileForge/UI/GroupEditor.cs
git commit -m "refactor: replace GroupEditor presets with PropertySchema-driven UI"
```

---

### Task 10: Delete old helpers, verify migration, final cleanup

**Files:**
- Modify: `TileForge/Game/GameStateManager.cs:437-447`

- [ ] **Step 1: Delete GetEntityIntProperty and SetEntityIntProperty from GameStateManager**

Remove these two methods (lines 437-447). All call sites were migrated in Tasks 6 and 7.

- [ ] **Step 2: Build to confirm no remaining references**

Run: `dotnet build TileForge`
Expected: Build succeeds — no code references the deleted methods

- [ ] **Step 3: Search for remaining raw property key strings**

Run a grep across the codebase for known property keys used as raw strings. Check that no gameplay code still uses raw string literals like `"health"`, `"behavior"`, `"damage"`, etc. for property lookups.

Acceptable places for raw strings: test files (which may construct test data), JSON files, documentation, and the `PropertyKeys.cs` constants themselves.

- [ ] **Step 4: Run full test suite**

Run: `dotnet test TileForge.Tests`
Expected: All tests PASS, 0 failures

- [ ] **Step 5: Commit**

```bash
git add TileForge/Game/GameStateManager.cs
git commit -m "refactor: remove old GetEntityIntProperty/SetEntityIntProperty helpers"
```
