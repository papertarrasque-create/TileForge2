# TileForge — Game Runtime PRD

## Mission
Evolve TileForge's embedded play mode into a full RPG game runtime. The editor remains the authoring tool. F5 launches a game loop with state management, screen stacking, map transitions, and save/load.

---

## Completed Phases (G1–G13 + Editors + UI Overhaul)

1443 tests, 0 failures. All systems implemented and tested.

| Phase | What | Tests |
|-------|------|-------|
| G1 | Data model — gameplay props on TileGroup, EntityType enum, registries, GroupEditor | 494 |
| G2 | GameState/PlayerState/GameStateManager, PlayModeController delegation | 537 |
| G3 | GameInputManager (edge detection), ScreenManager (screen stack), GameplayScreen | 574 |
| G4 | MapLoader, trigger-based transitions, flag persistence, SaveManager | 631 |
| G5 | SaveLoadScreen, InventoryScreen, DialogueScreen, SettingsScreen, StatusEffects | 722 |
| G6 | StatusEffect runtime, GameOverScreen, HUD, DefaultProperties, item-use fix | 771 |
| G7 | Bump combat, EntityAI (idle/chase/patrol/chase_patrol), IPathfinder, damage flash | 870 |
| G8 | Quest system — flag/variable objectives, entity hooks, QuestLogScreen | 925 |
| G8+ | Quest Editor UI — QuestPanel + QuestEditor + QuestFileManager | 956 |
| UI | RPG Maker-inspired MenuBar + ToolbarRibbon, smart GroupEditor, DojoUI widgets | 1031 |
| G8++ | Dialogue Editor UI — DialoguePanel + DialogueEditor + DialogueFileManager | 1050 |
| Form | FormLayout, ScrollPanel, resizable modals, overflow tooltips | 1067 |
| G9 | Equipment — weapon/armor/accessory slots, effective stats, InventoryScreen equip | 1158 |
| Cleanup | Dead code removal, GameMenuList/ModalResizeHandler extraction, GC fixes | 1162 |
| G10 | Visual Dialogue Tree — split-pane node-graph editor, BFS auto-layout | 1198 |
| G11 | Multimap projects — MapDocumentState, MapTabBar, ProjectFile V2, pre-exported maps | 1266 |
| G12 | World Map Editor — WorldLayout grid, EdgeTransitionResolver, custom spawn/exit points | 1371 |
| G13 | AP Combat + Floating Messages — 2 AP/turn, directional attack, entity speed, auto-end-turn, floating text | 1443 |

---

## Model Assignment Guide

| Model | Use For | Examples |
|-------|---------|---------|
| **Sonnet** | Mechanical, well-specified work | Data classes, enums, serialization, tests following patterns |
| **Opus** | Architectural integration | GroupEditor UI, PlayModeController, GameplayScreen, multi-system coordination |

---

## Future Phases

### Extension Points (Built into G7/G13)

Seams enabling future systems with zero refactoring:

**A* Pathfinding** — Build `AStarPathfinder` implementing `IPathfinder`. Swap one line in GameplayScreen constructor.

**Ranged Combat** — New behavior `"ranged_chase"` reading `attack_range`, `preferred_distance` from property bag. Uses `IPathfinder.HasLineOfSight()` (Bresenham). Returns `EntityAction` with `AttackTargetX/Y` populated. Entity speed system (G13) already supports multi-action turns for ranged entities.

**Animated Enemy Movement** — Queue `EntityAction` list instead of executing immediately. Play each as sequential lerp. EntityAction struct already contains all needed data. Speed-based entity loop (G13) provides natural animation points.

---

### Standalone Registry Export
Export `tiles.json` / `entities.json` as standalone files for external tooling. Low complexity.

---

## File Map

```
TileForge/Game/
  ├── EntityType.cs              # NPC, Item, Trap, Trigger, Interactable
  ├── TileRegistry.cs            # Tile property lookups
  ├── EntityRegistry.cs          # Entity type lookups
  ├── GameState.cs               # Central serializable state
  ├── PlayerState.cs             # Position, health, inventory, equipment, effects
  ├── EquipmentSlot.cs           # Weapon, Armor, Accessory enum
  ├── Direction.cs               # Up/Down/Left/Right
  ├── EntityInstance.cs          # Runtime entity with properties + active flag
  ├── GameStateManager.cs        # State mutation API
  ├── GameAction.cs              # Input action enum (incl. EndTurn)
  ├── GameInputManager.cs        # Action->key mapping + edge detection
  ├── GameScreen.cs              # Abstract screen base
  ├── ScreenManager.cs           # Play-mode screen stack
  ├── LoadedMap.cs               # Runtime map data
  ├── MapLoader.cs               # Export JSON -> LoadedMap
  ├── MapTransitionRequest.cs    # Transition target data
  ├── SaveManager.cs             # Slot-based save/load
  ├── StatusEffect.cs            # Burn/poison/ice/spikes
  ├── DialogueData.cs            # Dialogue nodes + choices (EditorX/EditorY)
  ├── CombatHelper.cs            # CalculateDamage + AttackResult
  ├── IPathfinder.cs             # Pathfinding interface
  ├── SimplePathfinder.cs        # Axis-priority + Bresenham LOS
  ├── EntityAction.cs            # EntityActionType enum + data
  ├── EntityAI.cs                # Static AI (idle/chase/patrol/chase_patrol)
  ├── FloatingMessage.cs         # Floating text: color, tile pos, timer, drift
  ├── GamePlayContext.cs         # Shared context for GameplayScreen construction
  ├── EdgeTransitionResolver.cs  # WorldLayout edge + exit point resolution
  ├── QuestData.cs               # QuestFile, QuestDefinition, QuestObjective
  ├── QuestLoader.cs             # JSON quest loader (snake_case + PascalCase)
  ├── QuestEvent.cs              # QuestEventType enum + data
  ├── QuestManager.cs            # Quest evaluation engine
TileForge/Data/
  ├── QuestFileManager.cs        # Load/save quests.json
  ├── DialogueFileManager.cs     # Load/save dialogues/*.json
  ├── WorldLayout.cs             # Grid-based map adjacency data
  ├── WorldLayoutHelper.cs       # Pure-logic spatial queries
DojoUI/
  ├── Renderer.cs                # DrawRect, DrawLine, DrawBezier
  ├── Dropdown.cs, MenuBar.cs, Checkbox.cs, NumericField.cs
  ├── TooltipManager.cs, FormLayout.cs, ScrollPanel.cs
TileForge/Editor/
  ├── MapDocumentState.cs        # Per-map state container
TileForge/UI/
  ├── EditorMenus.cs, ToolbarRibbon.cs, MenuActionDispatcher.cs
  ├── MapTabBar.cs               # Multimap tab strip
  ├── IProjectContext.cs         # Project data for browse-dropdowns
  ├── GroupEditor.cs             # Smart property editing
  ├── QuestPanel.cs, QuestEditor.cs
  ├── DialoguePanel.cs, DialogueTreeEditor.cs
  ├── NodeGraphCamera.cs, DialogueAutoLayout.cs, DialogueNodeWidget.cs
  ├── ConnectionRenderer.cs, WorldMapEditor.cs
  ├── ShortcutsDialog.cs, AboutDialog.cs
TileForge/Infrastructure/
  ├── IPathResolver.cs           # Abstraction for file paths (testable)
  ├── IDialogueLoader.cs         # Abstraction for dialogue file loading
  ├── FileDialogueLoader.cs      # Production dialogue loader
  ├── DefaultPathResolver.cs     # Production path resolver
TileForge/Game/Screens/
  ├── GameplayScreen.cs, PauseScreen.cs, SaveLoadScreen.cs
  ├── InventoryScreen.cs, DialogueScreen.cs, SettingsScreen.cs
  ├── GameOverScreen.cs, QuestLogScreen.cs
```
