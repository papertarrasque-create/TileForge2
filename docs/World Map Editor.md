---
updated: 2026-03-06
status: current
---

# World Map Editor

A visual grid editor (`WorldMapEditor`) for defining spatial map adjacency. Accessed via View > World Map or Ctrl+W.

## Layout

```
+-- Canvas (65%) ----------------------------+-- Properties (35%) --+
|                                            |                      |
|   +------+  +------+                      | Unplaced Maps:       |
|   | Town |--| Cave |                      |   [ Forest ]         |
|   +------+  +------+                      |   [ Ruins  ]         |
|      |                                     |                      |
|   +------+                                | Selected: Town       |
|   | Road |                                | Grid: (0, 0)         |
|   +------+                                |                      |
|                                            | Entry Spawns:        |
|   (pannable, zoomable grid)                | North: X[__] Y[__]   |
|                                            | South: X[__] Y[__]   |
|                                            | Exit Points:         |
|                                            | NorthExit: X[__]Y[__]|
+--------------------------------------------+----------------------+
```

## Grid System

- 2D integer grid with configurable cell size
- Maps placed at integer positions
- **Adjacency is automatic:** grid neighbors are map neighbors
  - (0,0) west of (1,0)
  - (0,0) north of (0,1)
  - Auto-bidirectional (placing creates mutual neighbors)

## Canvas Interaction

| Action | Effect |
|--------|--------|
| Left-click empty cell (place mode) | Place selected unplaced map |
| Left-click filled cell | Select map |
| Drag placed map | Reposition on grid |
| Right-click cell | Context menu (Place/Remove) |
| Middle-mouse / Space+drag | Pan |
| Scroll wheel | Zoom |

## Placement Mode

1. Click an unplaced map in the properties panel
2. Enter "place mode" (cursor changes)
3. Click empty grid cell to place
4. Map appears at that grid position with auto-neighbors

## Properties Panel (Right 35%)

**Unplaced maps list** -- maps not yet on the grid

**Selected map properties:**

### Entry Spawns

Custom spawn coordinates when entering from each direction:
- North Entry: X, Y (NumericField) -- where to spawn when arriving from the north
- South Entry: X, Y
- East Entry: X, Y
- West Entry: X, Y

If not set, defaults to opposite edge with clamped coordinate (see [[Maps]]).

### Exit Points

Portal-style transition tiles at arbitrary positions on the map:
- NorthExit: X, Y -- stepping on this tile triggers transition to north neighbor
- SouthExit: X, Y
- EastExit: X, Y
- WestExit: X, Y

Exit points coexist with edge-of-map transitions.

## Data Storage

`WorldLayout` stored in `ProjectFile.ProjectData.WorldLayout`:
- Null when unconfigured (backward compatible with pre-G12 projects)
- Contains `Maps: Dictionary<string, MapPlacement>`
- Each `MapPlacement` has GridX/GridY + optional EdgeSpawns + optional ExitPoints

## Grid Visualization

| Element | Appearance |
|---------|------------|
| Empty cell | Dim outline |
| Filled cell | Brighter fill, map name centered |
| Hover cell | Highlight color |
| Selected cell | Blue border + brighter fill |
| Neighbor badges | N/S/E/W indicators on connected edges |

## Related

- [[Maps]] -- Map transitions and edge resolution
- [[Editor Overview]] -- Where WorldMapEditor sits in the update chain
- [[File Formats]] -- WorldLayout data structure
