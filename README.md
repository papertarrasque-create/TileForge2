# TileForge2

An interactive map editor for top-down 2D games. Paint tiles, place entities, play the map.

Built with C# / MonoGame (DesktopGL), targeting .NET 9.0.

![TileForge2](https://img.shields.io/badge/.NET-9.0-blue) ![MonoGame](https://img.shields.io/badge/MonoGame-3.8-green)

## What It Does

- **Paint tiles** onto a grid using named groups (grass, wall, floor, etc.)
- **Place entities** with identity (doors, chests, NPCs, player start)
- **Multiple layers** with visibility toggles and reordering
- **Play mode** (F5) — walk your map with collision and entity interaction
- **Save/Load** projects as `.tileforge2` JSON files
- **Undo/Redo** for all editing actions

## Quick Start

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Build & Run

```bash
git clone https://github.com/papertarrasque-create/TileForge2.git
cd TileForge2
dotnet run --project TileForge2
```

Window opens at 1440x900. To get started:

1. **Open the sample project**: `Ctrl+O`, navigate to `TileForge2/TutorialProject/TestCavernDungeon.tileforge2`
2. **Or start fresh**: drag-and-drop any spritesheet PNG onto the window, enter tile dimensions

### Try the Sample Project

The `TileForge2/TutorialProject/` folder contains a sample project with a pre-built dungeon map and "The Roguelike" spritesheet (16x16 tiles). Open `TestCavernDungeon.tileforge2` to explore.

## Controls

### Editor

| Key | Action |
|-----|--------|
| B | Brush tool |
| E | Eraser tool |
| F | Fill tool |
| N | Entity tool |
| G | Toggle grid |
| V | Toggle layer visibility |
| Tab | Cycle active layer |
| Ctrl+S | Save |
| Ctrl+O | Open file |
| Ctrl+Z / Ctrl+Y | Undo / Redo |
| F5 | Enter play mode |
| Middle drag | Pan |
| Scroll | Zoom |

### Play Mode

| Key | Action |
|-----|--------|
| Arrow keys | Move player |
| F5 / Escape | Exit play mode |

### Group Editor

| Key | Action |
|-----|--------|
| S | Toggle solid |
| P | Toggle player |
| T | Toggle tile/entity type |
| Enter | Confirm |
| Escape | Cancel |

## Creating a Map

1. Drop a spritesheet PNG onto the window (or `Ctrl+O`)
2. Enter tile width/height when prompted
3. Click **+ Add Group** under a layer in the Map panel
4. Select sprites from the sheet, name your group, set type
5. Paint with the Brush tool (B), fill areas (F), place entities (N)
6. Mark wall groups as **Solid** (S in editor), mark one entity group as **Player** (P)
7. Press **F5** to play your map

## Project Structure

```
TileForge2/         Main application
DojoUI/             Shared UI library (Camera, SpriteSheet, Dialogs, etc.)
TileForge2.sln      Solution file
```

## File Format

Projects save as `.tileforge2` JSON files — human-readable, hand-editable. Spritesheets are referenced by relative path.

## Feedback

This is a testing release. If you find bugs or have suggestions, please open an issue.
