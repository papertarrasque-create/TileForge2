---
status: implemented
date: 2026-03-06
---

# ADR-002: Dialogue System 2.0

## Context

The v1 dialogue system had six pain points: fragmented per-entity-type triggering, state tracking split between dialogue and entity properties, no conditional routing, indirect quest integration (flag polling), entity types limiting dialogue capability, and no one-shot distinction.

## Decision

Redesign around three concepts:

1. **Dialogue Routes** -- Ordered conditional entry points on the dialogue file itself. Top-to-bottom, first match wins. Replaces `concluded_flag`/`concluded_dialogue` entity properties and RequiresFlag node chaining.

2. **Unified Triggering** -- Any entity type can trigger dialogue via `dialogue_id`. One property, one code path. Traps, triggers, items all support dialogue.

3. **Conditions + Actions** -- Generalized `conditions` array (AND logic) replaces `RequiresFlag`. Composable `actions` array replaces `SetsFlag`/`SetsVariable`. Actions include `start_quest`, `complete_objective`, `give_item`, `heal`, `damage`, `log`.

Additional: `oneShot` flag auto-sets `dialogue_shown:{id}` after completion. `type` field dispatches to different screen handlers (conversation, bark, cutscene, inspect). `tags` list reserved for future per-node metadata.

## Entity Property Changes

**Removed:** `concluded_flag`, `concluded_dialogue`, `on_pickup_dialogue`
**Kept:** `dialogue_id` (now on all entity types), `dialogue` (inline fallback)

## Migration

V1 format auto-converts on load: `RequiresFlag` -> `conditions`, `SetsFlag`/`SetsVariable` -> `actions`. V1 compat code has been removed; all dialogues now use native v2 format.

## Consequences

**Positive:** Single dialogue file = complete conversation logic. Any entity can have dialogue. Direct quest integration. Composable conditions and actions. Type field enables future dialogue modes without schema changes.

**Negative:** More complex data model. Migration added temporary complexity (now removed).

## Related

See [[Dialogue]] for the full runtime documentation.
