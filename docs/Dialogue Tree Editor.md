---
updated: 2026-03-06
status: current
---

# Dialogue Tree Editor

A visual node-graph editor (`DialogueTreeEditor`) for authoring [[Dialogue]] trees. Split-pane layout with pannable/zoomable canvas and properties panel.

## Layout

```
+-- Canvas (65%) ----------------------------+-- Properties (35%) --+
|                                            |                      |
|   [Node: start]---->[Node: explain]        | Node: start          |
|        |                   |               | Speaker: [________]  |
|        +-->[Node: decline] |               | Text: [___________]  |
|                   [Node: quest_accept]<----+| Next: [________]     |
|                                            | RequiresFlag: [____] |
|   (pannable, zoomable grid)                | SetsFlag: [________] |
|                                            |                      |
|                                            | Choices:             |
|                                            | 1. [text] -> [next]  |
|                                            | 2. [text] -> [next]  |
|                                            | [+ Add Choice]       |
+--------------------------------------------+----------------------+
```

## Canvas Features

- **Pan:** Middle-mouse drag or Space+drag
- **Zoom:** Scroll wheel (0.25x to 3.0x, step 0.15)
- **Grid:** 40px dot spacing, scales with zoom
- **Camera:** `NodeGraphCamera` (float-precision, separate from editor's integer-zoom Camera)

## Node Widgets

`DialogueNodeWidget` -- retro card-style nodes:

- **Header:** Node ID (small, truncated)
- **Body:** Speaker + text (word-wrapped)
- **Choices:** List of choice buttons with flag indicators
- **Ports:** Input port (top-left), output ports per choice + next
- **Drag:** Entire node is draggable (world coords persisted as `EditorX`/`EditorY`)
- **Selection:** Click to select, blue border + brighter header when selected

## Connections

Bezier curves rendered via `ConnectionRenderer`:
- From output port (per choice or next) to input port
- Drag from output port to create new connection
- Visual feedback on port hover
- Right-click node context menu includes "Disconnect All"

## Properties Panel

Scrollable FormLayout panel (right 35%) shows fields for the selected node:

**Node fields:**
- ID, Speaker, Text (512 chars max)
- Next (NextNodeId)
- RequiresFlag, SetsFlag, SetsVariable

**Choices section:**
- Dynamic list of choice rows
- Per choice: Text, Next, RequiresFlag, SetsFlag
- [+ Add Choice] / [Remove] buttons

## Context Menus

- **Right-click canvas:** Add Node (creates new node at cursor position)
- **Right-click node:** Delete Node, Disconnect All

## Auto-Layout

`DialogueAutoLayout` uses BFS from the start node to position all nodes:
- Arranges nodes in columns by depth
- Vertical spacing between siblings
- Triggered via "Auto Layout" button in properties panel
- Only positions nodes without existing `EditorX`/`EditorY` (or all nodes on explicit request)

## Deep Copy on Edit

Edits modify a **copy** of the dialogue data. The original is untouched until the user saves. This allows clean cancel/discard.

## Key Constants

| Constant | Value |
|----------|-------|
| Canvas split | 65% left, 35% right |
| Zoom range | 0.25x - 3.0x |
| Zoom step | 0.15 per scroll tick |
| Grid dot spacing | 40px |

## Related

- [[Dialogue]] -- Runtime dialogue system
- [[Editor Overview]] -- Where the editor fits in the update chain
- [[DojoUI]] -- FormLayout, ScrollPanel, TextInputField used in properties
