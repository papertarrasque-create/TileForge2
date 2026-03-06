---
updated: 2026-03-06
status: current
---

# Constants Reference

Key numeric constants across all TileForge systems.

## Combat

| Constant | Value | Location |
|----------|-------|----------|
| Base MaxAP | 2 | PlayerState default |
| Move cost | 1 AP | GameplayScreen |
| Attack cost | 1 AP | GameplayScreen |
| Min damage | 1 | CombatHelper |
| Backstab multiplier | 2.0x | CombatHelper |
| Flank multiplier | 1.5x | CombatHelper |
| Front multiplier | 1.0x | CombatHelper |

## Poise

| Constant | Value | Location |
|----------|-------|----------|
| Base MaxPoise | 20 | PlayerState default |
| Regen amount | max(1, maxPoise/4) | GameStateManager |
| Regen condition | No hostiles nearby | GameplayScreen |

## Entity AI

| Constant | Value | Location |
|----------|-------|----------|
| Default aggro range | 5 | EntityAI |
| Alert duration | 3 turns | Noise system |
| Alert aggro multiplier | 2x | EntityAI |
| Speed range | [1, 3] | EntityAI (clamped) |
| Default patrol range | 3 | EntityAI |

## Noise

| Constant | Value | Location |
|----------|-------|----------|
| Propagation radius | 3 * noiseLevel | GameplayScreen |
| Silent | noiseLevel = 0 | TileGroup |
| Normal | noiseLevel = 1 | TileGroup (default) |
| Loud | noiseLevel = 2 | TileGroup |

## Status Effects

| Effect | Duration | Damage/Step | Movement |
|--------|----------|-------------|----------|
| Fire | 3 steps | 1 | 1.0x |
| Poison | 6 steps | 1 | 1.0x |
| Ice | 3 steps | 0 | 2.0x |
| Spikes | Instant | varies | -- |

## Player Defaults

| Stat | Default |
|------|---------|
| Health | 100 |
| MaxHealth | 100 |
| Attack | 5 |
| Defense | 2 |
| MaxAP | 2 |
| Poise | 20 |
| MaxPoise | 20 |
| Facing | Down |

## Visual

| Constant | Value | Location |
|----------|-------|----------|
| Sidebar width | 280px | SidebarHUD |
| GameLog max entries | 200 | GameLog |
| Floating message duration | 1.0s | FloatingMessage |
| Floating message drift | 16px/s up | FloatingMessage |
| Floating message fade | Last 0.3s | FloatingMessage |
| Typewriter speed | 40 chars/s | DialogueScreen |
| Base move duration | 0.15s | GameplayScreen |

## Editor UI

| Constant | Value | Location |
|----------|-------|----------|
| MenuBar height | 22px | LayoutConstants |
| ToolbarRibbon height | 32px | LayoutConstants |
| MapTabBar height | 22px | MapTabBar |
| Scroll step | 20px/tick | ScrollPanel |
| Scroll bar width | 6px | ScrollPanel |
| Tooltip delay | 0.4s | TooltipManager |
| NodeGraph zoom range | 0.25x - 3.0x | NodeGraphCamera |
| NodeGraph zoom step | 0.15/tick | NodeGraphCamera |
| Grid dot spacing | 40px | DialogueTreeEditor |
| Modal min size | 500 x 400 | ModalResizeHandler |
| Name field max | 32 chars | GroupEditor |
| Text field max | 512 chars | Various editors |
| Dropdown max visible | 8 items | Dropdown |

## Save/Load

| Constant | Value |
|----------|-------|
| GameState version | 2 |
| Save location | ~/.tileforge/saves/ |
| Key bindings | ~/.tileforge/keybindings.json |

## Related

- [[Combat]] -- Combat constant usage
- [[Entities]] -- AI constant usage
- [[DojoUI]] -- UI constant usage
