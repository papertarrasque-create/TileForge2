---
updated: 2026-03-06
status: current
---

# Quest Editor

The QuestEditor is a form-based modal for authoring [[Quests]]. Accessed from the QuestPanel sidebar.

## Two-Tier UI

- **QuestPanel** (sidebar) -- List of quests with CRUD operations
  - Click to select, double-click to edit
  - Right-click context menu: Edit, Delete
  - "+ Add Quest" button
- **QuestEditor** (modal overlay) -- Full quest editing form

## Form Layout

```
+-- Quest Editor ------------------------------------------+
| Id:          [________________]                          |
| Name:        [________________]                          |
| Description: [________________]                          |
| Start Flag:  [________________]                          |
| Completion:  [________________]                          |
|                                                          |
| -- Objectives --                                         |
| Description: [________________]                          |
| Type: [flag/var>=/var==]                                 |
|   Flag: [________________]    (for "flag" type)          |
|   Variable: [________] Value: [___]  (for var types)     |
| [Remove]                                                 |
| ---                                                      |
| [+ Add Objective]                                        |
|                                                          |
| -- Rewards --                                            |
| Set Flags:     [________________]  (comma-separated)     |
| Set Variables: [________________]  (key=value, comma-sep) |
|                                                          |
| [Enter] Save  [Esc] Cancel  [Tab] Next Field            |
+----------------------------------------------------------+
```

## Dynamic Objective Fields

When objective type changes:
- **flag:** Shows Flag field only
- **variable_gte / variable_eq:** Shows Variable + Value fields

Fields adapt dynamically on type dropdown change.

## Validation

- Id and Name are required
- Id uniqueness checked against existing quests
- Empty fields are acceptable for optional properties

## Focus Management

- Tab cycles through all text input fields
- Enter saves
- Escape cancels
- Back/Delete/Left/Right/Home/End routed to active field

## Overflow Tooltips

Fields with text wider than their bounds show overflow tooltips on hover via `TooltipManager`.

## Related

- [[Quests]] -- Runtime quest system
- [[Entities]] -- Entity hooks that feed quest objectives
- [[Editor Overview]] -- Where QuestEditor sits in the update chain
