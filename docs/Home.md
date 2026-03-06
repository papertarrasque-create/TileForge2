---
updated: 2026-03-06
status: current
---
# TileForge Wiki

Welcome to the TileForge project wiki. This is the living documentation for the codebase -- use it to understand systems, plan features, and write specs.

## Start Here

- [[Brief]] -- What TileForge is and what it's trying to be
- [[Status]] -- Current state, active work, known issues
- [[Architecture]] -- System overview and data flow

## Game Runtime

| Page | Covers |
|------|--------|
| [[Combat]] | AP system, damage formula, poise, terrain defense, backstab/flanking, turn sequence |
| [[Entities]] | Entity types, AI behaviors, facing, collision, deactivation |
| [[Quests]] | Quest definitions, objectives, evaluation, entity hooks, rewards |
| [[Dialogue]] | Node structure, branching, conditions, side effects, typewriter reveal |
| [[Maps]] | Map loading, transitions (trigger + edge + exit point), world layout grid |
| [[Equipment]] | Slots, stat bonuses, effective stats, item property cache |
| [[Status Effects]] | Fire, poison, ice, spikes -- duration, damage, movement modifiers |
| [[Save System]] | GameState serialization, save slots, version handling, entity persistence |
| [[Noise and Alertness]] | Tile noise levels, propagation, alert system, aggro range doubling |
| [[Sidebar HUD]] | Retro CRPG sidebar, GameLog, floating messages, scrollable log |

## Editor

| Page | Covers |
|------|--------|
| [[Editor Overview]] | TileForgeGame orchestrator, update/draw pipeline, mode switching |
| [[Group Editor]] | Property editing, entity type presets, sprite selection |
| [[Dialogue Tree Editor]] | Visual node graph, canvas interaction, auto-layout |
| [[Quest Editor]] | Quest authoring form, objectives, rewards |
| [[World Map Editor]] | Grid-based map adjacency, spawn points, exit points |
| [[Map Tab Bar]] | Multimap tab management, context menu |
| [[DojoUI]] | Shared widget library -- Dropdown, Checkbox, NumericField, FormLayout, ScrollPanel |

## Reference

| Page | Covers |
|------|--------|
| [[Property Reference]] | Complete entity property key reference with types and defaults |
| [[File Formats]] | .tileforge project, dialogue JSON, quest JSON, save files, world layout |
| [[Controls]] | Editor and play mode keyboard/mouse controls |
| [[Constants]] | Key numeric constants across all systems |

## Decision Records

- [[ADRs/001-initial-architecture|ADR-001: Initial Architecture]] -- Foundational v1 decisions and their consequences

## Project History

- [[Changelog]] -- Feature history reconstructed from git
- [[Sessions/|Session Log]] -- Per-session work summaries
