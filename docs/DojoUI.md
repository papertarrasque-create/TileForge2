---
updated: 2026-03-06
status: current
---

# DojoUI

Shared UI widget library used by TileForge. All widgets are **immediate-mode** -- no retained widget tree, layout computed per frame.

DojoUI knows nothing about TileForge. It provides low-level drawing, input widgets, and layout helpers.

## Renderer

Immediate-mode drawing primitives using a single 1x1 white pixel texture:

| Method | Description |
|--------|-------------|
| `DrawRect(rect, color)` | Solid filled rectangle |
| `DrawRectOutline(rect, color, thickness)` | Rectangle outline |
| `DrawLine(start, end, color, thickness)` | Rotated/scaled line |
| `DrawBezier(p0-p3, color, thickness, segments)` | Cubic Bezier curve via line segments |

## Widgets

### Dropdown

Combo-box with popup selection:
- `SelectedIndex` / `SelectedItem` (string)
- Popup auto-positions below (or above near screen bottom)
- Popup width auto-fits longest item text
- MaxVisible = 8 items before scrolling
- `SetItems(items, selectedIndex)` for dynamic population
- InputEvent-aware: consumes clicks when open (modal behavior)

### Checkbox

14x14 toggle box:
- `IsChecked` (bool)
- Hover state for border color feedback
- Fill inner square when checked
- Accent blue check color

### NumericField

Integer input with bounds:
- Wraps `TextInputField` with digit/minus char filter
- Min/max clamping on blur (`ClampValue()`)
- `Value` (int) property

### TextInputField

Text input with cursor and scissor clipping:
- `Text` (string), `CursorPos` (int), `IsFocused` (bool)
- Character filter (configurable per instance)
- Blinking cursor (0.5s period)
- Text scrolls if cursor moves beyond visible width
- `IsTextOverflowing(font, bounds)` for tooltip detection
- Max length configurable
- **Known issue:** Scissor clipping causes extra SpriteBatch Begin/End pairs (see [[Status]])

### MenuBar

Top-level menu bar with submenus:
- Hover-to-switch when a menu is open
- Popup positioning below/above bar
- Separator support (thin lines with margins)
- Disabled items (dimmed)
- Hotkey text (right-aligned, dim)

### ContextMenu

Right-click popup menu:
- Items with optional separators
- Auto-positioning near click point
- Closes on click outside or item selection

## Layout Helpers

### FormLayout (Struct)

Immediate-mode form layout helper -- zero allocation, created on stack each Draw frame:

| Method | Description |
|--------|-------------|
| `DrawLabeledField(label, field)` | Label + TextInputField row |
| `DrawLabeledDropdown(label, dropdown)` | Label + Dropdown row |
| `DrawLabeledNumeric(label, numeric)` | Label + NumericField row |
| `DrawLabeledCheckbox(label, checkbox)` | Label + Checkbox row |
| `DrawTwoFieldRow(...)` | Side-by-side fields |
| `Space(pixels)` | Advance cursor without drawing |

Config: `ContentX`, `ContentWidth`, `LabelWidth`, `FieldHeight`, `RowHeight`.

### ScrollPanel

Scissor-clipped scrollable region with visual scroll bar:

```
Usage pattern:
1. BeginScroll(spriteBatch, viewport) -> sets scissor, returns adjusted Y
2. Draw content at (Y - ScrollOffset)
3. EndScroll(spriteBatch, totalContentHeight) -> restores scissor, draws bar
4. UpdateScroll(mouse, prevMouse, viewport) in Update -> handle mouse wheel
```

- Scroll bar: 6px wide, thumb proportional to content overflow
- Scroll step: 20px per wheel tick

## Utility

### TextUtils

| Method | Description |
|--------|-------------|
| `TruncateToFit(text, font, maxWidth)` | Truncate with "..." if text exceeds width |
| `WrapText(text, font, maxWidth)` | Word-wrap with char-break fallback |

### TooltipManager

Delayed hover tooltip:
- 0.4s delay before showing
- Tracks hover state per field
- Used with `IsTextOverflowing()` to show full text on overflow

### NodeGraphCamera

Float-precision pan/zoom camera for canvas editors:
- Separate from editor's integer-zoom Camera
- Zoom range: 0.25x - 3.0x
- Used by [[Dialogue Tree Editor]] and [[World Map Editor]]

### ModalResizeHandler (Struct)

Edge-drag resizing for modal editors:
- Minimum size: 500x400
- Detects edge hover, tracks drag state
- Shared by QuestEditor, DialogueTreeEditor, WorldMapEditor
- Eliminated ~110 lines of copy-paste

### GameMenuList (Struct)

Shared cursor/scroll navigation for game screens:
- Up/Down cursor movement
- Scroll offset management
- Used by 6 game screens (PauseScreen, SaveLoadScreen, etc.)

## Color Palette

| Use | Color |
|-----|-------|
| Button background | (50, 50, 50) |
| Button hover | (60, 60, 60) |
| Button border | (100, 100, 100) |
| Hover border | (100, 160, 255) -- accent blue |
| Text | (200, 200, 200) |
| Disabled text | (80, 80, 80) |
| Accent | (100, 160, 255) |

## Related

- [[Editor Overview]] -- How DojoUI widgets are used in the editor
- [[Group Editor]] -- Major consumer of DojoUI widgets
- [[Architecture]] -- Where DojoUI sits in the project structure
