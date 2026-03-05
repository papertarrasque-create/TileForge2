# TileForge Manual

> Tile Map Editor & RPG Runtime

---

## Table of Contents

- [1. Introduction](#1-introduction)
- [2. Interface Overview](#2-interface-overview)
  - [2.1 Menu Bar](#21-menu-bar)
  - [2.2 Toolbar Ribbon](#22-toolbar-ribbon)
  - [2.3 Sidebar Panels](#23-sidebar-panels)
  - [2.4 Map Canvas](#24-map-canvas)
  - [2.5 Status Bar](#25-status-bar)
- [3. Projects](#3-projects)
  - [3.1 Creating a New Project](#31-creating-a-new-project)
  - [3.2 Opening a Project](#32-opening-a-project)
  - [3.3 Saving](#33-saving)
  - [3.4 Auto-Save and Recovery](#34-auto-save-and-recovery)
  - [3.5 Project File Structure](#35-project-file-structure)
- [4. Sprite Sheets](#4-sprite-sheets)
- [5. Layers](#5-layers)
  - [5.1 Creating Layers](#51-creating-layers)
  - [5.2 Layer Visibility](#52-layer-visibility)
  - [5.3 Active Layer](#53-active-layer)
  - [5.4 Entity Render Order](#54-entity-render-order)
- [6. Groups](#6-groups)
  - [6.1 Tile Groups](#61-tile-groups)
  - [6.2 Entity Groups](#62-entity-groups)
  - [6.3 The Group Editor](#63-the-group-editor)
- [7. Tools](#7-tools)
  - [7.1 Brush](#71-brush)
  - [7.2 Eraser](#72-eraser)
  - [7.3 Fill Bucket](#73-fill-bucket)
  - [7.4 Entity Placer](#74-entity-placer)
  - [7.5 Tile Picker](#75-tile-picker)
  - [7.6 Selection](#76-selection)
- [8. Tiles](#8-tiles)
  - [8.1 Tile Properties](#81-tile-properties)
  - [8.2 Hazardous Tiles](#82-hazardous-tiles)
  - [8.3 Movement Cost](#83-movement-cost)
- [9. Entities](#9-entities)
  - [9.1 Entity Types](#91-entity-types)
  - [9.2 NPCs](#92-npcs)
  - [9.3 Items](#93-items)
  - [9.4 Traps](#94-traps)
  - [9.5 Triggers (Map Transitions)](#95-triggers-map-transitions)
  - [9.6 Interactables](#96-interactables)
  - [9.7 The Player Entity](#97-the-player-entity)
- [10. Entity AI and Behavior](#10-entity-ai-and-behavior)
  - [10.1 Idle](#101-idle)
  - [10.2 Chase](#102-chase)
  - [10.3 Patrol](#103-patrol)
  - [10.4 Chase Patrol](#104-chase-patrol)
  - [10.5 Aggro Range and Line of Sight](#105-aggro-range-and-line-of-sight)
- [11. Combat](#11-combat)
  - [11.1 Bump Combat](#111-bump-combat)
  - [11.2 Damage Formula](#112-damage-formula)
  - [11.3 Entity Turns](#113-entity-turns)
  - [11.4 Death and Deactivation](#114-death-and-deactivation)
- [12. Dialogue System](#12-dialogue-system)
  - [12.1 Creating Dialogues](#121-creating-dialogues)
  - [12.2 Dialogue Nodes](#122-dialogue-nodes)
  - [12.3 Choices and Branching](#123-choices-and-branching)
  - [12.4 Flags and Conditions](#124-flags-and-conditions)
  - [12.5 Linking Dialogues to Entities](#125-linking-dialogues-to-entities)
- [13. Quest System](#13-quest-system)
  - [13.1 Creating Quests](#131-creating-quests)
  - [13.2 Quest Objectives](#132-quest-objectives)
  - [13.3 Entity Hooks](#133-entity-hooks)
  - [13.4 Quest Rewards](#134-quest-rewards)
  - [13.5 Quest Lifecycle](#135-quest-lifecycle)
- [14. Flags and Variables](#14-flags-and-variables)
  - [14.1 Flags](#141-flags)
  - [14.2 Variables](#142-variables)
  - [14.3 Built-in Flags](#143-built-in-flags)
- [15. Play Mode](#15-play-mode)
  - [15.1 Starting Play Mode](#151-starting-play-mode)
  - [15.2 Gameplay Controls](#152-gameplay-controls)
  - [15.3 The HUD](#153-the-hud)
  - [15.4 Pause Menu](#154-pause-menu)
  - [15.5 Inventory Screen](#155-inventory-screen)
  - [15.6 Quest Log](#156-quest-log)
  - [15.7 Settings and Key Rebinding](#157-settings-and-key-rebinding)
  - [15.8 Save and Load](#158-save-and-load)
  - [15.9 Game Over](#159-game-over)
- [16. Map Transitions](#16-map-transitions)
- [17. Status Effects](#17-status-effects)
- [18. Exporting](#18-exporting)
- [19. Keyboard Shortcut Reference](#19-keyboard-shortcut-reference)
- [20. Tutorial: Building a Complete RPG Map](#20-tutorial-building-a-complete-rpg-map)
  - [Step 1: Create the Project](#step-1-create-the-project)
  - [Step 2: Set Up Terrain](#step-2-set-up-terrain)
  - [Step 3: Add Layers and Objects](#step-3-add-layers-and-objects)
  - [Step 4: Configure the Player](#step-4-configure-the-player)
  - [Step 5: Place NPCs](#step-5-place-npcs)
  - [Step 6: Create Dialogue](#step-6-create-dialogue)
  - [Step 7: Place Items](#step-7-place-items)
  - [Step 8: Add Enemies](#step-8-add-enemies)
  - [Step 9: Create Hazards and Traps](#step-9-create-hazards-and-traps)
  - [Step 10: Build a Second Map](#step-10-build-a-second-map)
  - [Step 11: Set Up Map Transitions](#step-11-set-up-map-transitions)
  - [Step 12: Create Quests](#step-12-create-quests)
  - [Step 13: Wire Entity Hooks](#step-13-wire-entity-hooks)
  - [Step 14: Playtest](#step-14-playtest)
- [21. Known Limitations and Future Improvements](#21-known-limitations-and-future-improvements)

---

## 1. Introduction

TileForge is a tile-based level editor with an embedded RPG game runtime. You author maps, entities, dialogues, and quests in the editor, then press **F5** to play your creation instantly. No external engine or build step is required — everything runs inside TileForge.

**What you can build:**

- Top-down RPG maps with multiple terrain layers
- NPCs with branching dialogue trees
- Enemies with AI behaviors (chase, patrol, idle)
- Collectible items with inventory management
- Traps and environmental hazards with damage types
- Map transitions between interconnected areas
- Data-driven quests with flag and variable objectives
- Bump-to-attack combat (Brogue-style)
- Save/load slots for player progress

**Design philosophy:** TileForge is data-driven. New content is created through the editor UI — no code required. Entity behavior, quest triggers, and dialogue branching are all controlled by properties you set in the Group Editor.

---

## 2. Interface Overview

TileForge uses a dark-themed interface with five main regions:

```
┌──────────────────────────────────────────────────┐
│  Menu Bar (File, Edit, View, Tools, Play, Help)  │  22px
├──────────────────────────────────────────────────┤
│  Toolbar Ribbon (icons + tooltips)               │  32px
├────────────┬─────────────────────────────────────┤
│            │                                     │
│  Sidebar   │        Map Canvas                   │
│  Panels    │    (tile/entity editing area)        │
│  (200px)   │                                     │
│            │                                     │
│  - Map     │                                     │
│  - Quests  │                                     │
│  - Dialog  │                                     │
│  - Palette │                                     │
│            │                                     │
├────────────┴─────────────────────────────────────┤
│  Status Bar (cursor position, tool hints)        │  22px
└──────────────────────────────────────────────────┘
```

### 2.1 Menu Bar

The menu bar provides access to all editor commands. Keyboard shortcuts are displayed next to each item.

**File**
| Item | Shortcut | Description |
|------|----------|-------------|
| New Project | `Ctrl+N` | Create a new project with sprite sheet, tile size, and map size |
| Open... | `Ctrl+O` | Open an existing `.tileforge` project or sprite sheet image |
| Open Recent | `Ctrl+Shift+O` | Open from a list of recently used projects |
| Save | `Ctrl+S` | Save the current project |
| Save As... | — | Save to a new file path |
| Export... | `Ctrl+E` | Export the map as JSON or PNG |
| Exit | — | Close TileForge |

**Edit**
| Item | Shortcut | Description |
|------|----------|-------------|
| Undo | `Ctrl+Z` | Undo the last action |
| Redo | `Ctrl+Y` | Redo the last undone action |
| Copy | `Ctrl+C` | Copy selected tiles (Selection tool) |
| Paste | `Ctrl+V` | Paste copied tiles |
| Delete | `Del` | Delete selected tiles or entities |
| Resize Map... | `Ctrl+R` | Change map dimensions (e.g., `50x40`) |

**View**
| Item | Shortcut | Description |
|------|----------|-------------|
| Toggle Minimap | `Ctrl+M` | Show or hide the minimap overlay |
| Cycle Grid | `G` | Cycle through Normal → Fine → Off grid modes |
| Toggle Layer Visibility | `V` | Show or hide the active layer |
| Next Layer | `Tab` | Switch to the next layer |

**Tools**
| Item | Shortcut | Description |
|------|----------|-------------|
| Brush | `B` | Paint tiles on the canvas |
| Eraser | `E` | Remove tiles from the canvas |
| Fill Bucket | `F` | Flood-fill connected area with selected group |
| Entity Placer | `N` | Place and drag entities |
| Tile Picker | `I` | Click a tile on canvas to select its group |
| Selection | `M` | Select rectangular regions for copy/paste/delete |

**Play**
| Item | Shortcut | Description |
|------|----------|-------------|
| Play / Stop | `F5` | Enter or exit play mode |

**Help**
| Item | Description |
|------|-------------|
| Keyboard Shortcuts | Open the keyboard shortcuts reference dialog |
| About TileForge | Show version and application information |

### 2.2 Toolbar Ribbon

The toolbar ribbon provides one-click access to common actions. Buttons are organized into groups separated by vertical dividers. Hover over any button for 0.5 seconds to see its tooltip.

**Button Groups:**

1. **File**: New, Open, Save
2. **History**: Undo, Redo
3. **Tools**: Brush, Eraser, Fill, Entity, Picker, Selection
4. **Play**: Play/Stop toggle
5. **Export**: Export map

The active tool is highlighted. Disabled buttons (e.g., Undo with no history, Export with no sprite sheet) appear dimmed.

During **play mode**, the ribbon shows only the Stop button with the message "PLAY MODE - F5 to return" in gold text.

### 2.3 Sidebar Panels

The left sidebar contains four collapsible panels. Click a panel header to expand or collapse it.

**Map Panel** — Layer and group management

- Lists all layers with visibility toggles (eye icon)
- Click a layer header to make it the active drawing layer
- Groups are listed under their assigned layer with preview thumbnails
- Click a group to select it for painting
- Double-click a group to open the Group Editor
- **Add Layer** button at the bottom
- **Add Group** button opens the Group Editor for a new group

**Quest Panel** — Quest management

- Lists all quests defined in the project
- Click to select, double-click to edit
- **Add Quest** opens the Quest Editor
- Delete button removes the selected quest

**Dialogue Panel** — Dialogue management

- Lists all dialogue files in the project
- Click to select, double-click to edit
- **Add Dialogue** opens the Dialogue Editor
- Delete button removes the selected dialogue

**Tile Palette Panel** — Sprite selection

- Shows the loaded sprite sheet in a scrollable grid
- Click a sprite to select it
- `Shift+Click` to select multiple sprites
- `Ctrl+Click` to toggle individual sprites
- Used when creating or editing groups in the Group Editor

### 2.4 Map Canvas

The main editing area where you build your map.

**Navigation:**
- **Scroll wheel** — Zoom in/out
- **Middle-click + drag** — Pan the viewport
- **Minimap click** — Jump to a location on the map

**Editing:**
- **Left-click** — Use the active tool (paint, erase, fill, place entity, pick, select)
- **Shift + Left-click** — Draw a line from last point to cursor (Brush tool)
- **Alt + Left-click** — Quick-pick: select the group or entity under the cursor without changing tools

**Visual feedback:**
- A semi-transparent tool preview follows your cursor
- Each tool has a distinct preview color (white for brush, red for eraser, green for fill, blue for entity, yellow for picker, cyan for selection)
- Selected entities show a blue highlight outline
- The grid can be toggled between normal, fine (half-tile subdivisions), and off

### 2.5 Status Bar

The status bar at the bottom of the window shows contextual information:

**Left side:** `(X, Y)  GroupName  Layer: LayerName  Grid:Mode`
- Cursor grid coordinates
- Name of the tile group at the cursor position
- Active layer name
- Current grid display mode

**Right side:** Tool-specific hints
- **Brush**: "Click to paint | Shift+Click line | Ctrl+Z undo"
- **Eraser**: "Click to erase | Ctrl+Z undo"
- **Fill**: "Click to fill connected area | Ctrl+Z undo"
- **Entity**: "Click to place entity | Right-click to interact"
- **Picker**: "Click to pick tile or entity from canvas"
- **Selection**: "Click+Drag to select | Ctrl+C copy | Ctrl+V paste | Del delete"
- **Clipboard active**: "Click to stamp | Esc clear clipboard"

---

## 3. Projects

### 3.1 Creating a New Project

Press `Ctrl+N` or go to **File → New Project**. The New Project dialog asks for:

1. **Sprite Sheet** — Browse to select a `.png` or `.jpg` tile atlas image
2. **Tile Size** — The dimensions of each tile in pixels. Accepts three formats:
   - `16` — Square tiles, 16×16 pixels, no padding
   - `16x24` — Rectangular tiles, 16 wide × 24 tall, no padding
   - `16+1` — Square 16×16 tiles with 1 pixel of padding between sprites
3. **Map Size** — Grid dimensions in tiles (e.g., `40x30` for 40 columns × 30 rows)

Press `Enter` to create the project or `Esc` to cancel. Use `Tab` to move between fields.

Every new project automatically includes:

- Two layers: **Ground** and **Objects**
- A **Player** entity group placed at the center of the map
- Empty quest and dialogue lists

### 3.2 Opening a Project

Press `Ctrl+O` or go to **File → Open**. You can open:

- **`.tileforge` files** — Loads the complete project with all groups, layers, entities, quests, and dialogues
- **Image files (`.png`, `.jpg`)** — Opens the image as a sprite sheet and prompts for tile size, creating a fresh project

Press `Ctrl+Shift+O` for **Open Recent**, which shows the last 10 projects you worked on.

### 3.3 Saving

Press `Ctrl+S` or go to **File → Save**. If the project has never been saved, you will be prompted to choose a file path. The title bar shows an asterisk (`*`) when there are unsaved changes.

Saving writes three types of files:
- The main `.tileforge` project file (map, groups, entities, editor state)
- `quests.json` alongside the project file (if quests exist)
- Individual dialogue files in a `dialogues/` folder (one JSON per dialogue)

### 3.4 Auto-Save and Recovery

TileForge automatically saves your work every **2 minutes** while you have unsaved changes. The auto-save is written to a sidecar file (e.g., `MyProject.tileforge.autosave`) alongside your project.

If TileForge detects an auto-save that is newer than the project file when you open it, you will be asked: **"An autosave was found. Recover unsaved changes?"** Choose yes to recover or no to load the original.

The auto-save sidecar is automatically cleaned up each time you perform a manual save.

### 3.5 Project File Structure

A TileForge project on disk looks like this:

```
MyProject/
├── MyProject.tileforge             # Main project file (JSON)
├── spritesheet.png                 # Your tile atlas (referenced by relative path)
├── quests.json                     # Quest definitions
└── dialogues/                      # One JSON file per dialogue
    ├── elder_greeting.json
    └── shopkeeper.json
```

**Additional system files:**
- `~/.tileforge/saves/{slot}.json` — Game save files (created during play mode)
- `~/.tileforge/keybindings.json` — Custom key bindings
- `~/.tileforge/recent.json` — Recent project list

---

## 4. Sprite Sheets

TileForge uses a single sprite sheet (tile atlas) per project. A sprite sheet is a grid of equally-sized tiles packed into one image.

**Requirements:**
- Format: PNG or JPG
- Layout: Regular grid of tiles, all the same size
- Optional padding between tiles (specified at project creation)

**Example:** A 1536×1024 pixel image with 16×16 tiles and 0 padding produces a grid of 96 columns × 64 rows = 6,144 available sprites.

Sprites are referenced by their column and row position in the grid (zero-indexed). When you create a group, you select one or more sprites from the sheet to define the group's visual appearance.

**Multi-sprite groups:** Groups with multiple sprites are used for visual variety — when painted, TileForge picks a sprite from the group using position-based seeding for deterministic variation.

---

## 5. Layers

Layers control the vertical stacking of tiles on your map. Tiles on higher layers render on top of tiles on lower layers.

### 5.1 Creating Layers

Click **Add Layer** in the Map Panel. You will be prompted to name the new layer. Layer names must be unique.

New projects start with two layers:
- **Ground** — Base terrain (grass, dirt, water, paths)
- **Objects** — Decoration and obstacles (trees, rocks, furniture)

### 5.2 Layer Visibility

Click the eye icon next to a layer header to toggle its visibility. Hidden layers are not rendered on the canvas but their data is preserved. Press `V` to quickly toggle the active layer's visibility.

### 5.3 Active Layer

Click a layer header in the Map Panel to make it the active layer. The active layer is where the Brush, Eraser, and Fill tools operate. Press `Tab` to cycle through layers.

Groups are assigned to a layer. When you select a group and paint with the Brush tool, tiles are placed on that group's assigned layer.

### 5.4 Entity Render Order

Entities are rendered between layers based on the map's **Entity Render Order** setting. By default, entities render after the first layer (Ground), so object layers render on top of entity sprites. This lets trees and walls visually overlap entity characters.

---

## 6. Groups

Groups are the building blocks of your map. Every tile and entity belongs to a group. A group defines the visual appearance (sprites) and gameplay properties (solid, hazardous, entity type, etc.) for all instances placed on the map.

### 6.1 Tile Groups

Tile groups represent terrain, decoration, and environmental features. They can have:

- One or more sprites for visual variety
- Collision properties (solid, passable)
- Hazard properties (damage type, damage per tick)
- Movement cost (affects player speed)

### 6.2 Entity Groups

Entity groups represent interactive objects in your world: NPCs, items, traps, triggers, and interactables. Each entity group has:

- An **Entity Type** that determines its behavior category
- **Default Properties** inherited by every instance placed on the map
- A **Player** flag (only one group should be marked as Player)

### 6.3 The Group Editor

Double-click a group in the Map Panel or click **Add Group** to open the Group Editor modal.

**Header Row:**
- **Name** — Unique name for the group (e.g., "Grass", "Goblin", "HealthPotion")
- **Type** dropdown — `Tile` or `Entity` (press `T` to toggle)
- **Solid** checkbox — Blocks movement (press `S` to toggle)
- **Player** checkbox — Marks this as the player entity (Entity type only, press `P` to toggle)

**Tile Properties (shown when Type is Tile):**
- **Passable** — Can be walked through (even if Solid is set for other interactions)
- **Hazard** — Damages the player when stepped on
- **Cost** — Movement cost multiplier: 0.5 (fast), 1.0 (normal), 1.5–5.0 (slow)
- **Damage Type** — Visual/mechanical damage category: none, fire, poison, spikes, ice
- **Damage Per Tick** — Hit points lost each time the player steps on this tile: 0–50

**Entity Properties (shown when Type is Entity):**
- **Entity Type** dropdown — NPC, Item, Trap, Trigger, Interactable

Below the type selection, context-sensitive property fields appear based on the chosen entity type. See [Section 9: Entities](#9-entities) for the full property reference.

**Sprite Selection (bottom half of modal):**
- The sprite sheet is displayed as a scrollable, zoomable grid
- Left-click to select a sprite
- `Shift+Click` to add to multi-selection (Tile groups support multiple sprites for variation)
- `Ctrl+Click` to toggle a sprite in the selection
- Selected sprites are highlighted in blue

**Controls:**
- `Enter` — Save and close
- `Esc` — Cancel and close
- `Tab` — Cycle between input fields (Entity mode)

---

## 7. Tools

### 7.1 Brush

**Shortcut:** `B`

Paint tiles on the active layer using the selected tile group. Click and drag to paint continuously.

- **Shift+Click** draws a straight line from the last painted position to the cursor
- Painting is recorded as a single undo operation per stroke

### 7.2 Eraser

**Shortcut:** `E`

Remove tiles from the active layer. Click and drag to erase continuously. Only affects the active layer.

### 7.3 Fill Bucket

**Shortcut:** `F`

Flood-fill a connected region of identical tiles with the selected group. Click on any tile to fill all connected tiles of the same type on the active layer.

### 7.4 Entity Placer

**Shortcut:** `N`

Place and manage entities on the map.

- **Click empty space** with an entity group selected — Places a new entity instance, inheriting the group's Default Properties
- **Click an existing entity** — Selects it for editing or repositioning
- **Drag a selected entity** — Move it to a new grid position

### 7.5 Tile Picker

**Shortcut:** `I`

Click any tile or entity on the canvas to select its group. The selected group becomes active for painting. This is the tool equivalent of the `Alt+Click` eyedropper shortcut.

### 7.6 Selection

**Shortcut:** `M`

Select rectangular regions of the map.

- **Click+Drag** — Draw a selection rectangle
- **Ctrl+C** — Copy the selected region to the clipboard
- **Ctrl+V** — Paste from clipboard (click to stamp)
- **Del** — Delete tiles and entities within the selection
- **Esc** — Clear the clipboard/selection

---

## 8. Tiles

Tiles are the visual and structural foundation of your map. Each cell in each layer can hold one tile (a reference to a tile group).

### 8.1 Tile Properties

| Property | Values | Description |
|----------|--------|-------------|
| Solid | on/off | Blocks player and entity movement |
| Passable | on/off | Overrides Solid for walk-through (e.g., tall grass) |
| Hazard | on/off | Damages the player when walked on |
| Movement Cost | 0.5 – 5.0 | Multiplier for movement speed (1.0 is normal) |
| Damage Type | none, fire, poison, spikes, ice | Category of hazard damage |
| Damage Per Tick | 0 – 50 | HP lost per step on this tile |

**Collision rules in play mode:**
- A tile is blocked if any layer at that position has a Solid group (unless Passable is also set)
- An entity at a position also blocks movement if its group is Solid

### 8.2 Hazardous Tiles

When the player steps onto a hazardous tile, they take `DamagePerTick` points of damage immediately. The damage type is cosmetic (affects the HUD message color) but can be used to trigger status effects through dialogue or item interactions.

**Examples:**
- Lava: Solid + Hazard + fire damage + 10 DPT
- Poison swamp: Hazard + poison damage + 2 DPT + Movement Cost 2.0
- Spike floor: Hazard + spikes damage + 5 DPT

### 8.3 Movement Cost

Movement cost affects how long it takes the player to cross a tile. The base movement duration is multiplied by the tile's cost:

| Cost | Effect |
|------|--------|
| 0.5 | Double speed (roads, paths) |
| 1.0 | Normal speed (default) |
| 1.5 | Slightly slow (rough terrain) |
| 2.0 | Half speed (water, mud) |
| 3.0 | Very slow (deep snow) |
| 5.0 | Extremely slow (spider webs, tar) |

---

## 9. Entities

Entities are interactive objects placed on the map: characters, items, hazards, portals, and anything the player can engage with. Every entity is an instance of an entity group and inherits that group's default properties.

### 9.1 Entity Types

| Type | Purpose | Attackable | Collectible |
|------|---------|------------|-------------|
| NPC | Characters with dialogue and/or combat | Yes (if has health) | No |
| Item | Collectible objects that go into inventory | No | Yes |
| Trap | Hazardous entities that deal damage | Yes (if has health) | No |
| Trigger | Invisible portals for map transitions | No | No |
| Interactable | Generic interactive objects (signs, chests) | No | No |

### 9.2 NPCs

NPCs are characters the player can talk to or fight. Their behavior depends on the properties you set.

**Properties:**

| Property | Type | Range | Description |
|----------|------|-------|-------------|
| health | Numeric | 1–9999 | Hit points (makes the NPC attackable) |
| attack | Numeric | 0–999 | Damage dealt to the player per hit |
| defense | Numeric | 0–999 | Damage reduction when attacked |
| behavior | Dropdown | idle, chase, patrol, chase_patrol | AI behavior pattern |
| aggro_range | Numeric | 1–50 | Detection distance for chase behaviors |
| dialogue_id | Browse | dialogue files | Dialogue shown on interaction |
| on_kill_set_flag | Text | — | Flag set when this NPC is killed |
| on_kill_increment | Text | — | Variable incremented when this NPC is killed |

**Friendly NPCs** typically have `dialogue_id` set and no `health` (or behavior set to `idle`). The player presses Interact to talk.

**Hostile NPCs** have `health`, `attack`, `behavior` (chase or chase_patrol), and optionally `aggro_range`. They move toward the player and attack on contact.

### 9.3 Items

Items are collectible objects. When the player walks onto an item, it is added to their inventory and removed from the map.

**Properties:**

| Property | Type | Range | Description |
|----------|------|-------|-------------|
| heal | Numeric | 1–9999 | HP restored when used from inventory |
| on_collect_set_flag | Text | — | Flag set when collected |
| on_collect_increment | Text | — | Variable incremented when collected |

Items are persistent — once collected, they remain gone across map transitions and save/load cycles.

### 9.4 Traps

Traps are hazardous entities that damage the player on contact. Unlike hazardous tiles, traps can optionally be destroyed by attacking them.

**Properties:**

| Property | Type | Range | Description |
|----------|------|-------|-------------|
| damage | Numeric | 1–9999 | Damage dealt to the player on contact |
| health | Numeric | 1–9999 | If set, the trap can be attacked and destroyed |
| on_kill_set_flag | Text | — | Flag set when the trap is destroyed |
| on_kill_increment | Text | — | Variable incremented when destroyed |

### 9.5 Triggers (Map Transitions)

Triggers are invisible entities that transport the player to another map when stepped on. They are the mechanism for connecting multiple maps together.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| target_map | Browse | The `.tileforge` file to transition to |
| target_x | Numeric (0–999) | X coordinate for the player's spawn position in the target map |
| target_y | Numeric (0–999) | Y coordinate for the player's spawn position in the target map |

The `target_map` dropdown shows all `.tileforge` files in your project directory and includes a **"Create New..."** option to create a new map file directly from the Group Editor.

See [Section 16: Map Transitions](#16-map-transitions) for details on how transitions work at runtime.

### 9.6 Interactables

Interactables are generic interactive objects such as signs, treasure chests, bookshelves, or levers. The player presses Interact to engage with them.

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| dialogue_id | Browse | Dialogue shown on interaction |

### 9.7 The Player Entity

Every project has exactly one entity group marked with the **Player** flag. This group defines the player's starting sprite and is placed at the center of the map when a new project is created.

The player's combat stats (`health`, `max_health`, `attack`, `defense`) are set on the PlayerState at runtime:
- Default health: 100
- Default max health: 100
- Default attack: 5
- Default defense: 2

> **Loose End:** Player combat stats are hardcoded at runtime and cannot be configured in the editor. A future release should allow setting starting stats in the Group Editor's Player properties.
<!-- IMPROVEMENT: Allow editing player starting stats (health, attack, defense) in GroupEditor -->

---

## 10. Entity AI and Behavior

Entity AI is driven by the `behavior` property on NPC and Trap entity groups. Each entity acts once per turn, after the player finishes their move or attack.

### 10.1 Idle

The entity does nothing each turn. Use this for friendly NPCs, decorative entities, or stationary guards.

### 10.2 Chase

The entity actively pursues the player.

1. If the player is within `aggro_range` (Manhattan distance) and adjacent (distance = 1): **attack**
2. If within range but not adjacent: **move toward the player** using pathfinding
3. If outside range: **idle**

The pathfinder uses axis-priority movement — the entity prefers moving along the axis with the greater distance to the player. If the primary axis is blocked, it tries the secondary axis.

### 10.3 Patrol

The entity walks back and forth along a single axis.

**Properties that control patrol:**
- `patrol_axis` — `"x"` (horizontal) or `"y"` (vertical). Default: `"x"`
- `patrol_range` — Distance in tiles to patrol. Default: 3

The entity automatically records its starting position (`patrol_origin`) and direction (`patrol_dir`) on its first turn. It walks in one direction until it reaches the patrol boundary or hits an obstacle, then reverses.

### 10.4 Chase Patrol

A combination behavior: the entity patrols normally, but switches to chase behavior when the player enters its `aggro_range`. When the player leaves aggro range, the entity returns to patrolling.

This is the most common behavior for dungeon enemies — they guard an area but actively pursue intruders.

### 10.5 Aggro Range and Line of Sight

**Aggro Range** (`aggro_range` property, default: 5) defines the detection radius in Manhattan distance (sum of horizontal and vertical distance). An entity with aggro_range 5 can detect a player up to 5 tiles away.

**Line of Sight** uses Bresenham's algorithm to trace a line between the entity and the player. All intermediate tiles must be passable (not solid) for the entity to have line of sight.

> **Loose End:** The chase behavior checks distance but does not currently require line of sight for detection. Entities can "see through walls" within their aggro range. A future release should optionally gate aggro on LOS.
<!-- IMPROVEMENT: Add optional LOS check to chase/chase_patrol aggro detection -->

---

## 11. Combat

TileForge uses a turn-based bump combat system inspired by traditional roguelikes.

### 11.1 Bump Combat

To attack an entity, simply walk into it. If the entity is attackable (NPC or Trap with health > 0), the player automatically attacks instead of moving.

### 11.2 Damage Formula

```
damage = max(1, attacker_attack - target_defense)
```

Damage is always at least 1, even if the target's defense exceeds the attacker's attack. This ensures combat always progresses.

**Example:**
- Player (attack: 5) vs. Goblin (defense: 2) → `max(1, 5-2)` = 3 damage
- Goblin (attack: 3) vs. Player (defense: 2) → `max(1, 3-2)` = 1 damage

### 11.3 Entity Turns

After every player action (move or attack), all active entities with AI behaviors get a turn. Entity turns process in order:

1. Each entity calls `EntityAI.DecideAction()` to determine its action
2. The action is executed: idle (nothing), move (reposition), or attack (damage player)
3. If an entity is adjacent to the player and has an attack value, it will attack

Combat is simultaneous from the player's perspective — you bump an enemy, deal damage, then the enemy (and all other entities) take their turn.

### 11.4 Death and Deactivation

When an entity's health reaches 0:

1. The entity is **deactivated** (removed from the map)
2. A persistence flag `entity_inactive:{id}` is set
3. If the entity has `on_kill_set_flag`, that flag is set
4. If the entity has `on_kill_increment`, that variable is incremented
5. The entity remains gone across map transitions and save/load

The HUD shows a combat message: "Hit {name} for {damage}!" in gold for player attacks, or "{name} hits you for {damage}!" in red for enemy attacks.

---

## 12. Dialogue System

The dialogue system lets you create branching conversations with NPCs and interactable objects. Dialogues are authored in the editor and stored as individual JSON files.

### 12.1 Creating Dialogues

Open the **Dialogue Panel** in the sidebar and click **Add Dialogue**. The Dialogue Editor modal opens.

**Top-level fields:**
- **Dialogue ID** — Unique identifier (e.g., `elder_greeting`, `shop_welcome`). This is the filename and the reference used by entities.

### 12.2 Dialogue Nodes

A dialogue is a list of **nodes**. Each node represents one "screen" of text shown to the player.

**Node fields:**

| Field | Description |
|-------|-------------|
| ID | Unique identifier within this dialogue (e.g., `start`, `explain`, `farewell`) |
| Speaker | Character name displayed above the text (optional) |
| Text | The dialogue text shown to the player |
| Next Node ID | For linear dialogues, the next node to show (leave blank for end) |
| Requires Flag | Only show this node if this flag is set (conditional content) |
| Sets Flag | Set this flag when the node is displayed |
| Sets Variable | Set a variable when displayed (format: `key=value`) |

**Linear dialogue:** Set `Next Node ID` on each node to chain them together. The player presses Interact to advance.

**Branching dialogue:** Add choices to a node instead of setting a Next Node ID.

### 12.3 Choices and Branching

Each node can have zero or more **choices**. When choices are present, the player selects from a list instead of auto-advancing.

**Choice fields:**

| Field | Description |
|-------|-------------|
| Text | The choice text shown to the player |
| Next Node ID | Which node to jump to when selected |
| Requires Flag | Only show this choice if this flag is set |
| Sets Flag | Set this flag when the choice is selected |

**Example branching structure:**

```
Node: start
  Text: "Welcome, traveler. What brings you here?"
  Choices:
    "I need supplies" → shop_menu
    "Tell me about the caves" → cave_info  (requires: talked_to_guard)
    "Goodbye" → farewell

Node: cave_info
  Text: "The caves to the north are full of spiders..."
  Sets Flag: quest_caves_accepted
  Next: start

Node: farewell
  Text: "Safe travels!"
  (end of dialogue)
```

### 12.4 Flags and Conditions

Dialogues interact with the game state through flags and variables:

- **Requires Flag** on a node or choice hides that content unless the player has the specified flag. Use this for progressive dialogue that reveals new options as the player accomplishes things.
- **Sets Flag** on a node or choice adds a flag to the player's state when shown/selected. Use this to trigger quests, mark conversations as completed, or unlock new dialogue branches.
- **Sets Variable** on a node sets a key=value pair (e.g., `reputation=5`). Use this for quest counters or story tracking.

### 12.5 Linking Dialogues to Entities

To connect a dialogue to an entity:

1. Create the dialogue in the Dialogue Panel
2. Open the Group Editor for an NPC or Interactable entity group
3. Set the **dialogue_id** field — use the browse dropdown to select from available dialogues, or type the ID directly
4. The dropdown includes a **"Create New..."** option to create a dialogue without leaving the Group Editor

When the player interacts with the entity in play mode, the dialogue opens with typewriter text animation. Press Interact to advance text or select choices with the arrow keys.

---

## 13. Quest System

The quest system tracks player objectives using flags and variables. Quests are defined in the editor and evaluated automatically during play.

### 13.1 Creating Quests

Open the **Quest Panel** in the sidebar and click **Add Quest**. The Quest Editor modal opens.

**Quest fields:**

| Field | Description |
|-------|-------------|
| Quest ID | Unique identifier (e.g., `cave_investigation`) |
| Quest Name | Display name shown in the Quest Log (e.g., "Investigate the Caves") |
| Description | Flavor text describing the quest goal |

### 13.2 Quest Objectives

Each quest has one or more objectives. Click **Add Objective** to add one.

**Objective types:**

| Type | Description | Fields |
|------|-------------|--------|
| Flag | Complete when a specific flag is set | `flag` — the flag name |
| Variable (≥) | Complete when a variable reaches a threshold | `variable`, `value` — the variable name and minimum value |
| Variable (=) | Complete when a variable equals an exact value | `variable`, `value` — the variable name and exact value |

**Examples:**

- "Enter the cave" — Flag objective: `visited_map:TestCavernDungeon`
- "Defeat 3 spiders" — Variable ≥ objective: `spider_kills >= 3`
- "Find the magic sword" — Flag objective: `found_magic_sword`

### 13.3 Entity Hooks

Entity hooks are the bridge between gameplay actions and quest objectives. They are properties set on entity groups that automatically modify flags/variables when things happen.

| Hook Property | Trigger | Effect |
|---------------|---------|--------|
| `on_kill_set_flag` | Entity killed (NPC/Trap) | Sets the named flag |
| `on_kill_increment` | Entity killed (NPC/Trap) | Increments the named variable by 1 |
| `on_collect_set_flag` | Item collected | Sets the named flag |
| `on_collect_increment` | Item collected | Increments the named variable by 1 |

**Example workflow:**
1. Create a quest "Defeat 3 Goblins" with objective: `goblin_kills >= 3`
2. On the Goblin entity group, set `on_kill_increment` to `goblin_kills`
3. Each time the player kills a goblin, the `goblin_kills` variable increments
4. When it reaches 3, the quest objective completes automatically

### 13.4 Quest Rewards

When all objectives are complete, the quest auto-completes and rewards are applied:

- **Set Flags** — A list of flags to set (e.g., `caves_cleared`)
- **Set Variables** — Key-value pairs to set (e.g., `reputation = 5`)

Quest completion is also tracked with a flag: `quest_complete:{quest_id}`.

### 13.5 Quest Lifecycle

```
1. NOT STARTED
   └─ Start flag is not set
   └─ Quest does not appear in Quest Log

2. ACTIVE (start flag is set)
   └─ Quest appears in Quest Log with objectives
   └─ Objectives checked after every player action
   └─ HUD shows cyan notification: "Quest Started: {name}"

3. COMPLETED (all objectives met)
   └─ Rewards applied automatically
   └─ Completion flag set
   └─ HUD shows cyan notification: "Quest Complete: {name}"
   └─ Quest moves to "Completed" section in Quest Log
```

**Starting a quest:** Set the quest's start flag. This is typically done through dialogue (using Sets Flag on a dialogue node or choice) or by entering a map (the `visited_map:{id}` auto-flag).

> **Loose End:** There is no way to define `start_flag` or `completion_flag` in the Quest Editor UI — these fields exist in the data model but the editor form uses implicit conventions. A future release should expose these fields explicitly.
<!-- IMPROVEMENT: Expose start_flag and completion_flag in QuestEditor UI -->

---

## 14. Flags and Variables

Flags and variables are the state system that connects dialogues, quests, entity hooks, and game progression.

### 14.1 Flags

A flag is a boolean marker — it either exists or it doesn't. Flags are used for:

- Quest start conditions (`quest_caves_accepted`)
- Quest completion tracking (`quest_complete:cave_investigation`)
- Dialogue branching (`talked_to_elder`)
- Entity persistence (`entity_inactive:{id}`)
- Map visit tracking (`visited_map:{map_id}`)

**Setting flags:**
- Dialogue nodes/choices: `Sets Flag` field
- Entity kill hooks: `on_kill_set_flag` property
- Entity collect hooks: `on_collect_set_flag` property
- Quest rewards: `Set Flags` list
- Built-in: map transitions set `visited_map:{id}` automatically

### 14.2 Variables

A variable is a named counter stored as a string. Variables are used for:

- Kill counters (`goblin_kills`)
- Collection counters (`gems_collected`)
- Story state (`reputation`)

**Setting variables:**
- Dialogue nodes: `Sets Variable` field (format: `key=value`)
- Entity kill hooks: `on_kill_increment` property (increments by 1)
- Entity collect hooks: `on_collect_increment` property (increments by 1)
- Quest rewards: `Set Variables` dictionary

### 14.3 Built-in Flags

TileForge automatically sets certain flags:

| Flag Pattern | Set When |
|--------------|----------|
| `entity_inactive:{entity_id}` | An entity is killed or collected |
| `visited_map:{map_id}` | The player enters a map |

These can be used directly in quest objectives. For example, a "Visit the dungeon" objective can check for the flag `visited_map:DungeonLevel1`.

---

## 15. Play Mode

### 15.1 Starting Play Mode

Press **F5** or click the Play button in the toolbar. TileForge:

1. Exports the current map to a runtime format
2. Finds the Player entity and initializes the game state
3. Loads any defined quests and dialogues
4. Switches to the gameplay screen

The editor state (map, groups, camera position) is preserved and restored when you exit play mode.

### 15.2 Gameplay Controls

**Default key bindings:**

| Action | Key |
|--------|-----|
| Move Up | Up Arrow / W |
| Move Down | Down Arrow / S |
| Move Left | Left Arrow / A |
| Move Right | Right Arrow / D |
| Wait (skip turn) | Space |
| Interact | E |
| Open Inventory | I |
| Open Quest Log | Q |
| Pause Menu | P / Escape |

All keys can be rebound in the Settings screen.

**Movement** is tile-based and turn-based. The player moves one tile per action. Movement speed is affected by tile movement cost and active status effects.

### 15.3 The HUD

During gameplay, the HUD displays:

- **Health bar** (top-left) — Green when healthy, yellow at half, red when critical
- **Status effects** — Active effects shown as colored tags: `[FIRE 3]` (orange), `[PSN 2]` (purple), `[ICE 1]` (blue), with remaining steps
- **Combat messages** — Color-coded feedback:
  - Gold: Player attack results ("Hit goblin for 3!")
  - Red: Damage taken ("Goblin hits you for 2!")
  - Cyan: Quest updates ("Quest Started: Investigate the Caves")
- **Damage flash** — Screen briefly tints red when the player takes damage; entities flash white when hit

### 15.4 Pause Menu

Press **P** or **Escape** to open the pause menu overlay. Options:

- **Resume** — Return to gameplay
- **Inventory** — Open the inventory screen
- **Save Game** — Save current progress
- **Load Game** — Load a saved game
- **Settings** — Key rebinding
- **Quest Log** — View active and completed quests
- **Quit** — Return to the editor

### 15.5 Inventory Screen

Press **I** to open the inventory. The inventory shows all collected items grouped by type.

- **Use** — Use a consumable item (e.g., health potion heals `heal` HP)
- **Drop** — Remove an item from inventory (it is not returned to the map)

Item properties are cached when collected, so items retain their properties even after leaving the map where they were found.

### 15.6 Quest Log

Press **Q** to open the quest log. It shows:

- **Active Quests** — Name, description, and objective progress
- **Completed Quests** — Finished quests (moved here after auto-completion)

### 15.7 Settings and Key Rebinding

The Settings screen lets you rebind all gameplay keys:

1. Select an action
2. Press any key to bind it
3. The new binding takes effect immediately

Bindings are saved to `~/.tileforge/keybindings.json` and persist across sessions.

### 15.8 Save and Load

**Save:** Open the pause menu → Save Game. Select a slot to save your current progress. The save includes:
- Player position, health, inventory, and stats
- Current map
- All flags and variables (quest progress, entity states, story state)
- Active entity states
- Item property cache

**Load:** Open the pause menu → Load Game. Select a saved slot to restore. Loading replaces all current game state.

Save files are stored in `~/.tileforge/saves/`.

### 15.9 Game Over

When the player's health reaches 0, the Game Over screen appears with two options:

- **Restart** — Reset all game state and start from the beginning
- **Return to Editor** — Exit play mode and return to the editor

---

## 16. Map Transitions

Map transitions let you connect multiple maps into a larger game world.

**Setup:**

1. **Create a Trigger entity group** with Entity Type set to `Trigger`
2. Set the `target_map` property to the destination `.tileforge` file (use the browse dropdown)
3. Set `target_x` and `target_y` to the coordinates where the player should appear in the destination map
4. Place the Trigger entity on the map where you want the portal/door to be

**At runtime:**

1. The player steps onto the trigger tile
2. The game loads the target map
3. The player appears at (`target_x`, `target_y`)
4. The flag `visited_map:{map_id}` is set
5. Entity persistence is evaluated — killed/collected entities remain gone

**What persists across transitions:**
- Player health, inventory, attack, defense
- All flags and variables
- Entity deactivation states (via persistence flags)

**What resets:**
- Active status effects are cleared
- Map entities are reloaded fresh (with persistence applied)

**Bidirectional travel:** To allow the player to travel back, create a Trigger in the destination map that points back to the origin map with appropriate spawn coordinates.

> **Loose End:** There is no visual indicator in the editor for where trigger entities point. A future release could show transition arrows or target map previews.
<!-- IMPROVEMENT: Show visual indicators for trigger destinations in editor -->

---

## 17. Status Effects

Status effects modify the player over time. They tick down with each movement step.

| Effect | Damage | Movement | Visual |
|--------|--------|----------|--------|
| Fire (burn) | Yes (per step) | Normal | Orange-red tag |
| Poison | Yes (per step) | Normal | Purple tag |
| Ice | No | Slowed | Blue tag |
| Spikes | Yes (per step) | Normal | White tag |

Status effects display on the HUD as colored tags showing the effect name and remaining steps: `[FIRE 3]` means 3 steps of fire damage remaining.

Multiple status effects can be active simultaneously. Movement speed multipliers stack multiplicatively (e.g., two 0.5× effects result in 0.25× speed).

If a status effect of the same type is applied while one is already active, the new one replaces the old one.

> **Loose End:** Status effects can only be applied programmatically — there is no editor UI to configure which hazard tiles or traps apply specific status effects. Hazardous tiles deal instant damage but don't apply lingering effects. A future release should allow linking tile/trap damage types to status effect application.
<!-- IMPROVEMENT: Add status effect application to hazard tiles and trap entities via editor properties -->

---

## 18. Exporting

Press `Ctrl+E` or go to **File → Export** to export your map.

**Export formats:**

| Format | Extension | Description |
|--------|-----------|-------------|
| JSON | `.json` | Complete map data with layers, groups, entities, and properties. Used by the game runtime's MapLoader. |
| PNG | `.png` | Rendered image of the map as it appears in the editor. Useful for documentation or preview images. |

The JSON export includes everything needed for the game runtime to load and play the map: tile layout, group definitions with gameplay properties, entity positions and properties.

---

## 19. Keyboard Shortcut Reference

### Editor Shortcuts

| Category | Shortcut | Action |
|----------|----------|--------|
| **File** | `Ctrl+N` | New Project |
| | `Ctrl+O` | Open Project |
| | `Ctrl+Shift+O` | Open Recent |
| | `Ctrl+S` | Save |
| | `Ctrl+E` | Export |
| **Edit** | `Ctrl+Z` | Undo |
| | `Ctrl+Y` | Redo |
| | `Ctrl+C` | Copy selection |
| | `Ctrl+V` | Paste |
| | `Del` | Delete selection |
| | `Ctrl+R` | Resize Map |
| **View** | `Ctrl+M` | Toggle Minimap |
| | `G` | Cycle Grid (Normal → Fine → Off) |
| | `V` | Toggle Layer Visibility |
| | `Tab` | Next Layer |
| **Tools** | `B` | Brush |
| | `E` | Eraser |
| | `F` | Fill Bucket |
| | `N` | Entity Placer |
| | `I` | Tile Picker |
| | `M` | Selection |
| **Canvas** | `Scroll Wheel` | Zoom |
| | `Middle Mouse` | Pan |
| | `Alt+Click` | Quick-pick (eyedropper) |
| | `Shift+Click` | Line draw (Brush) |
| **Play** | `F5` | Play / Stop |

### Group Editor Shortcuts

| Shortcut | Action |
|----------|--------|
| `Enter` | Save and close |
| `Esc` | Cancel and close |
| `Tab` | Cycle input fields |
| `T` | Toggle Tile/Entity type |
| `S` | Toggle Solid |
| `P` | Toggle Player flag |
| `Shift+Click` | Multi-select sprites |
| `Ctrl+Click` | Toggle sprite selection |

### Play Mode Controls

| Action | Default Key |
|--------|-------------|
| Move Up | Up Arrow / W |
| Move Down | Down Arrow / S |
| Move Left | Left Arrow / A |
| Move Right | Right Arrow / D |
| Wait | Space |
| Interact | E |
| Inventory | I |
| Quest Log | Q |
| Pause | P / Escape |

---

## 20. Tutorial: Building a Complete RPG Map

This tutorial walks through creating a complete playable scenario using every major feature of TileForge. You will build a village with an NPC quest-giver, a connected dungeon with enemies, collectible items, traps, hazardous terrain, branching dialogue, and a quest that ties it all together.

**What you will create:**
- A village map with a friendly NPC, items, and a cave entrance
- A dungeon map with enemies, traps, and hazards
- Dialogue with branching choices and flag-gated content
- A quest: "Clear the Dungeon" with kill-count and exploration objectives
- Map transitions between village and dungeon

### Step 1: Create the Project

1. Press `Ctrl+N` to open the New Project dialog
2. **Sprite Sheet**: Browse to your tile atlas PNG (a 16×16 roguelike tileset works well)
3. **Tile Size**: Enter `16` (or match your sprite sheet's tile size)
4. **Map Size**: Enter `30x20` — this will be the village map
5. Press `Enter` to create the project

You should see an empty map with a **Player** entity at the center. Save immediately with `Ctrl+S` and name it `Village.tileforge`.

### Step 2: Set Up Terrain

First, create tile groups for your village terrain.

**Create the Grass group:**
1. Click **Add Group** in the Map Panel
2. Name: `Grass`
3. Type: `Tile` (default)
4. Leave Solid unchecked
5. In the sprite sheet area, click a grass sprite
6. Press `Enter` to save

**Create the Water group:**
1. Add Group → Name: `Water`
2. Check **Solid** (blocks movement)
3. Set **Cost** to `2.0`
4. Select a water sprite
5. Press `Enter`

**Create the Path group:**
1. Add Group → Name: `Path`
2. Set **Cost** to `0.5` (faster movement)
3. Select a stone/path sprite
4. Press `Enter`

Now paint the village:
1. Press `B` for the Brush tool
2. Select `Grass` in the Map Panel and paint the entire ground layer
3. Select `Water` and paint a river or pond
4. Select `Path` and paint walkways connecting areas

**Tip:** Use `F` (Fill) to quickly flood-fill large areas, and `Shift+Click` to draw straight lines.

### Step 3: Add Layers and Objects

1. Click **Add Layer** in the Map Panel and name it `Decoration`
2. Create object groups on this layer:

**Create the Tree group:**
1. Add Group → Name: `Tree`
2. Type: `Tile`
3. Check **Solid** (blocks movement)
4. Layer: `Decoration` (or assign after creation)
5. Select a tree sprite
6. Press `Enter`

**Create the House group:**
1. Add Group → Name: `HouseWall`
2. Check **Solid**
3. Select wall sprites
4. Press `Enter`

Paint trees around the village perimeter and build a house structure using wall tiles.

### Step 4: Configure the Player

1. Double-click the **Player** group in the Map Panel to open the Group Editor
2. Verify it is set to Type: `Entity` with **Player** checked
3. Select a hero/character sprite from the sprite sheet
4. Press `Enter`
5. Drag the Player entity to a good starting position (e.g., the village entrance) using the Entity Placer tool (`N`)

### Step 5: Place NPCs

**Create the Village Elder:**
1. Add Group → Name: `Elder`
2. Type: `Entity`
3. Entity Type: `NPC`
4. Set `behavior` to `idle`
5. Leave health/attack/defense empty (friendly NPC)
6. Select an elder/wizard character sprite
7. Press `Enter`
8. With the Entity Placer (`N`), click on the map to place the Elder near the house

### Step 6: Create Dialogue

1. In the **Dialogue Panel**, click **Add Dialogue**
2. Set Dialogue ID: `elder_quest`

**Add the start node:**
- ID: `start`
- Speaker: `Village Elder`
- Text: `Welcome, adventurer. A great darkness has filled the caves to the north. Will you help us?`
- Add two choices:
  - Choice 1: Text: `I will help!` → Next Node: `accept`
  - Choice 2: Text: `What's in it for me?` → Next Node: `reward_info`

**Add the accept node:**
- ID: `accept`
- Speaker: `Village Elder`
- Text: `Thank you, brave soul! Take this healing potion for the journey. The cave entrance is to the north.`
- Sets Flag: `quest_dungeon_accepted`
- Next Node: (leave blank — ends dialogue)

**Add the reward_info node:**
- ID: `reward_info`
- Speaker: `Village Elder`
- Text: `Our village will reward you with gold and the gratitude of all who live here.`
- Next Node: `start` (returns to the main question)

**Add a return node (for after the quest):**
- ID: `return_complete`
- Speaker: `Village Elder`
- Text: `You've done it! The caves are clear. The village is forever in your debt.`
- Requires Flag: `quest_complete:clear_dungeon`

3. Save the dialogue
4. Back in the Group Editor for `Elder`, set `dialogue_id` to `elder_quest` using the browse dropdown

### Step 7: Place Items

**Create a Health Potion:**
1. Add Group → Name: `HealthPotion`
2. Type: `Entity`
3. Entity Type: `Item`
4. Set `heal` to `25`
5. Select a potion sprite
6. Press `Enter`
7. Place a few potions around the village and near the dungeon entrance

**Create a Key item (quest-related):**
1. Add Group → Name: `DungeonKey`
2. Type: `Entity`, Entity Type: `Item`
3. Set `on_collect_set_flag` to `has_dungeon_key`
4. Select a key sprite
5. Place it inside the Elder's house (reward for accepting the quest)

### Step 8: Add Enemies

**Create a Spider enemy:**
1. Add Group → Name: `Spider`
2. Type: `Entity`
3. Entity Type: `NPC`
4. Set properties:
   - `health`: `8`
   - `attack`: `3`
   - `defense`: `1`
   - `behavior`: `chase_patrol`
   - `aggro_range`: `5`
   - `patrol_range`: `3`
   - `on_kill_increment`: `dungeon_kills`
5. Select a spider sprite
6. Press `Enter`

> Don't place spiders on the village map yet — they belong in the dungeon. We'll place them in Step 10.

### Step 9: Create Hazards and Traps

**Create a Lava tile:**
1. Add Group → Name: `Lava`
2. Type: `Tile`
3. Check **Hazard**
4. Set Damage Type: `fire`
5. Set Damage Per Tick: `5`
6. Set Cost: `2.0`
7. Select a lava sprite

**Create a Spike Trap entity:**
1. Add Group → Name: `SpikeTrap`
2. Type: `Entity`
3. Entity Type: `Trap`
4. Set `damage`: `3`
5. Set `health`: `5` (destructible)
6. Set `on_kill_set_flag`: `trap_destroyed`
7. Select a spike sprite

**Create a Spider Web tile:**
1. Add Group → Name: `SpiderWeb`
2. Type: `Tile`
3. Set Cost: `5.0` (extremely slow)
4. Select a web sprite

### Step 10: Build a Second Map

1. Save the village map (`Ctrl+S`)
2. Press `Ctrl+N` to create a new project
3. Sprite Sheet: same sprite sheet as the village
4. Tile Size: `16`
5. Map Size: `25x20`
6. Save as `Dungeon.tileforge` **in the same folder** as `Village.tileforge`

Build the dungeon:
1. Create a `CaveFloor` tile group and fill the ground
2. Create a `CaveWall` tile group (Solid) and build walls and corridors
3. Paint `Lava` tiles in a hazardous area (reuse the group from the village, or create a new one)
4. Paint `SpiderWeb` tiles in narrow corridors
5. Place **Spider** entities throughout the dungeon (3–5 of them)
6. Place **SpikeTrap** entities in ambush positions
7. Place **HealthPotion** items in hidden alcoves
8. Move the Player entity to the dungeon entrance (where the player will arrive from the village)

### Step 11: Set Up Map Transitions

**In the Village map** (open `Village.tileforge`):

1. Add Group → Name: `CaveEntrance`
2. Type: `Entity`
3. Entity Type: `Trigger`
4. Set `target_map` to `Dungeon.tileforge` (use the browse dropdown)
5. Set `target_x` to the X coordinate of the dungeon entrance
6. Set `target_y` to the Y coordinate of the dungeon entrance
7. Place the trigger at the north edge of the village map

**In the Dungeon map** (open `Dungeon.tileforge`):

1. Create a `DungeonExit` trigger entity group
2. Set `target_map` to `Village.tileforge`
3. Set `target_x` and `target_y` to coordinates near the cave entrance on the village map
4. Place it at the dungeon entrance (where the player arrives)

### Step 12: Create Quests

Open either map (quests are per-project), go to the **Quest Panel**, and click **Add Quest**.

**Quest: "Clear the Dungeon"**
- Quest ID: `clear_dungeon`
- Quest Name: `Clear the Dungeon`
- Description: `Eliminate the spider threat in the northern caves`

**Add objectives:**

1. Objective 1:
   - Type: `Flag`
   - Flag: `visited_map:Dungeon`
   - Description: `Enter the dungeon`

2. Objective 2:
   - Type: `Variable ≥`
   - Variable: `dungeon_kills`
   - Value: `3`
   - Description: `Defeat 3 dungeon creatures`

**Rewards:**
- Set Flag: `dungeon_cleared`

### Step 13: Wire Entity Hooks

Go back to the **Spider** entity group and verify:
- `on_kill_increment` is set to `dungeon_kills`

This ensures every spider kill increments the `dungeon_kills` counter, which the quest objective watches.

The quest starts when the `quest_dungeon_accepted` flag is set (done by the Elder's dialogue in Step 6).

The quest completes when:
1. The player has visited the dungeon map (auto-flag: `visited_map:Dungeon`)
2. The player has killed 3 or more dungeon creatures (`dungeon_kills >= 3`)

### Step 14: Playtest

1. Open `Village.tileforge`
2. Press **F5** to enter play mode
3. Walk to the Elder and press **E** to talk
4. Accept the quest — you should see "Quest Started: Clear the Dungeon" in cyan
5. Walk to the cave entrance trigger — you should transition to the dungeon
6. Fight spiders by walking into them (bump combat)
7. After killing 3, you should see "Quest Complete: Clear the Dungeon"
8. Press **Q** to check the quest log
9. Press **I** to check your inventory
10. Use a health potion if needed
11. Walk to the dungeon exit to return to the village
12. Talk to the Elder again
13. Press **F5** to return to the editor

**Congratulations!** You have built a complete RPG scenario with:
- Two connected maps (village and dungeon)
- A quest-giving NPC with branching dialogue
- Enemies with AI behaviors
- Collectible items and inventory
- Environmental hazards (lava, spider webs)
- Destructible traps
- A quest with flag and variable objectives
- Map transitions with entity persistence

---

## 21. Known Limitations and Future Improvements

The following are areas where TileForge's current implementation has gaps, inconsistencies, or opportunities for enhancement. These are documented to help authors work around limitations and to guide future development.

### Editor Limitations

1. **Player starting stats are hardcoded.** The player always starts with 100 HP, 5 ATK, 2 DEF. There is no way to configure starting stats in the Group Editor. Workaround: modify default values in code.

2. **No visual indicator for trigger destinations.** Trigger entities show no on-canvas hint about where they lead. You must open the Group Editor to see the target map and coordinates. Enhancement: draw connection arrows or target map name labels on triggers.

3. **Quest start/completion flags not exposed in editor.** The QuestDefinition data model supports `start_flag` and `completion_flag` fields, but the Quest Editor UI does not surface them — they follow implicit naming conventions. Enhancement: add explicit fields to the Quest Editor form.

4. **Single sprite sheet per project.** Each project can only use one tile atlas image. If you need more variety, you must combine sprites into a single large sheet before importing. Enhancement: support multiple sprite sheets.

5. **No entity property editing per-instance.** Entity instances inherit group default properties. There is no way to override a property on a specific placed entity (e.g., give one goblin more health than others of the same group). Workaround: create separate groups for variants. Enhancement: per-instance property overrides in the Entity tool.

6. **No multi-map project management.** Each `.tileforge` file is a separate project. Working with multi-map games requires opening each map as a separate project. The trigger system connects them at runtime, but the editor has no unified map browser. Enhancement: multi-map project container with a map list panel.

7. **No map preview for triggers.** The `target_map` browse dropdown shows filenames but no visual preview of the destination map. Enhancement: show a thumbnail preview.

8. **Undo does not cover quest/dialogue edits.** The undo system tracks map and entity changes but not quest or dialogue modifications made in the editor panels.

9. **No drag-and-drop layer reordering UI.** Layers can be reordered but the interface for doing so is minimal. Enhancement: drag-and-drop layer ordering in the Map Panel.

### Gameplay Limitations

10. **Chase AI ignores line of sight.** Entities with chase or chase_patrol behavior detect the player within aggro_range using Manhattan distance only — they can "see through walls." The LOS system exists (Bresenham algorithm) but is not used for aggro detection. Enhancement: optional LOS gating on aggro.

11. **Status effects cannot be applied from tiles or traps via the editor.** Hazardous tiles deal instant damage but don't apply lingering status effects (burn, poison, etc.). Status effects can only be applied programmatically. Enhancement: add a "status_effect" property to hazard tiles and trap entities.

12. **No equipment system.** The inventory supports items and healing potions, but there is no equipment slot system (weapon, armor, accessory). Items cannot modify player stats. This is planned for a future G9 phase.

13. **No ranged combat.** All combat is bump-to-attack (melee). Ranged enemies and ranged player attacks are not yet implemented. The EntityAction system has fields for ranged targeting but they are unused. This is planned for a future phase.

14. **Simple pathfinding only.** The current pathfinder uses axis-priority movement, not A*. Entities can get stuck in complex maze layouts. Enhancement: implement A* pathfinding (the IPathfinder interface already supports this swap).

15. **No XP or leveling system.** Entities have an `xp` property field but there is no experience or level-up system at runtime. Enhancement: implement XP accumulation and stat growth.

16. **Status effects clear on map transition.** When the player moves to a new map, all active status effects are removed. This may be intentional (fresh start per area) or an oversight depending on design intent.

17. **Dropped items are lost forever.** When a player drops an item from inventory, it is simply removed — not placed back on the map. Enhancement: place dropped items at the player's feet.

18. **No NPC movement animation.** Entity movement during their turn is instantaneous (teleport). The player has smooth lerp movement but entities snap to their new position. The animation system exists in the codebase but is not connected for entity movement.

### Data and Format Limitations

19. **Mixed JSON conventions.** Quest files use snake_case (`start_flag`, `completion_flag`) while dialogue files use camelCase (`nextNodeId`, `setsFlag`). This inconsistency could cause confusion for anyone inspecting the JSON files directly. Enhancement: standardize on one convention.

20. **No data validation on quest/dialogue references.** If a dialogue_id references a non-existent dialogue file, or a quest references a flag that nothing sets, there is no editor-time warning. Enhancement: add validation warnings for broken references.

21. **No dialogue preview.** There is no way to preview a dialogue conversation flow from the editor without entering play mode and talking to the entity. Enhancement: add a dialogue preview/test mode.

22. **Entity IDs are auto-generated.** When placing entities, IDs are generated automatically. There is no way to set a human-readable ID for a specific entity instance, which makes debugging quest hooks harder. Enhancement: allow custom entity IDs.

23. **No map-level metadata.** Maps have dimensions and layers but no metadata fields (display name, description, ambient properties, background music reference). Enhancement: add a map properties panel.

---

*TileForge — Tile Map Editor & RPG Runtime*
