# AI_CONTEXT.md — Context Map for Sektor.TurnBased

> Quick-reference for AI agents working on this codebase.
> Full details live in `docs/*.md`.

## Project at a Glance

Turn-based game framework (net8.0, C#, WPF) with two demo games built on a shared core pipeline.

- **Build**: `dotnet build Sektor.TurnBased.slnx` — must be **0 warnings**
- **Tests**: `dotnet test Sektor.TurnBased.slnx` (xunit, 87 tests)
- **Rules**: See `AGENTS.md` for coding conventions and invariants

## Solution Structure

```
src/
  Core/
    Sektor.TurnBased.Core.Abstractions/  — IGameState, IGameCommand, PhaseTransition, Result, Result<T>
    Sektor.TurnBased.Core/               — GamePipeline, IGamePhase, GameContext, GameEventBus,
                                           VisualQueue, VisualEvent, GameLog, ContentRegistry, DeterministicRng
  Battle/
    Sektor.TurnBased.Battle/             — First game on the core (turn-based combat)
      Model/      — ActionDefinition, BattleActor, BattleState, ResourceContainer, Stat/Status definitions
      Effects/    — ICombatEffect (Damage, Heal, ModifyStat, ApplyStatus, Summon), ICombatPrecondition
      Rules/      — IOrderRule (Fixed, Speed, TeamAlternation), IWinCondition (Extermination)
      Phases/     — Setup → RoundStart → ActorTurn → BattleEnd
      Content/    — BattleContentCatalog, ContentValidator
      Events/     — ActorDied, ActorStatChanged, RoundStarted, BattleEnded
      Commands/   — UseActionCommand, SkipTurnCommand
      BattleEngine (facade), BattleAi, BattleExecutor
  Dialog/
    Sektor.TurnBased.Dialog/             — Second game on the core (branching dialogue/quest)
      Model/      — DialogNodeDefinition, DialogChoiceDefinition, DialogState
      Phases/     — Setup → Flow (with child pipeline support for sub-dialogs)
      Content/    — DialogContentCatalog, DialogContentValidator
      Events/     — NodeShown, ChoiceChosen, DialogEnded, SubDialogEntered/Completed
      Commands/   — ChooseOptionCommand
      DialogEngine (facade), DialogEventSink
  UI/
    Sektor.TurnBased.UI.Core/            — Session abstractions (BattleSession, DialogSession, SessionDriver)
    Sektor.TurnBased.UI.ViewModels/      — MVVM view models (Battle, Dialog, Lobby, Navigation, Shared)
    Sektor.TurnBased.UI.Wpf/             — WPF app (Views, Controls, Theme converters)

tests/
  Sektor.TurnBased.Core.Tests/           — 24 tests (pipeline, core services)
  Sektor.TurnBased.Battle.Tests/         — 42 tests (model, effects, rules, AI, executor, integration)
  Sektor.TurnBased.Dialog.Tests/         — 21 tests (engine, validator)
  Sektor.TurnBased.UI.Core.Tests/        — Session & ViewModel tests
```

## Core Concepts

### GamePipeline
- Phase-based execution engine. Phases register → start → execute → transition.
- Transitions: `Next(phaseId)`, `Suspend(reason)`, `Resume()`, `Finish()`.
- Supports nested pipelines via `CreateChildPipeline()` (used by Dialog for sub-dialogs).
- Commands routed to suspended phase or active child pipeline.
- **Never throws exceptions** — all errors return `Result`/`Result<T>`.

### GameEventBus
- Domain event bus with `Before`/`After` hooks and `applyBase` callback.
- Before hooks can cancel events. After hooks run after base logic.
- Subscriber errors isolated (try/catch) — never crash the game.

### VisualQueue + VisualEvent
- Immutable snapshots (`EventType` string, `SourceRuntimeId`, `TargetRuntimeId`, `Value`, `Payload`).
- Core mutates state instantly; UI reads FIFO queue for animation.
- Decouples instant logic from smooth rendering.

### ContentRegistry + DeterministicRng
- Data lookup by string Id: `Content.TryGet<T>(id, out value)`.
- All randomness through `DeterministicRng` (seed from `GameContext`).
- Same seed → same log, events, and outcome (determinism for tests/replays).

## Key Invariants

1. **No enum/switch for behavior branching** — polymorphism only (strategies, data resources).
2. **String Ids everywhere** — event types, effects, phases, resources. Validated at load time.
3. **Core knows nothing about games** — Battle and Dialog are independent consumers.
4. **Anemic model forbidden** — entities manage their own state via validating methods.
5. **DI through constructors** — no hardcoded `new` inside logic.
6. **XML docs on all public types and members** — mandatory.
7. **One public type per file** — file named after type.

## Documentation Index

| File | Covers |
|------|--------|
| `docs/architecture.md` | Core pipeline, phases, events, visual queue, RNG, content registry |
| `docs/battle.md` | Combat model: actors, effects, rules, phases, AI, executor |
| `docs/dialog.md` | Dialogue model: nodes, flags, sub-dialogs via child pipelines |
| `docs/principles.md` | SOLID, YAGNI/KISS, data-vs-behavior, layers, errors, determinism |
| `docs/decisions.md` | ADR-001..008: core-as-framework, flat mitigation, string Ids, AI by damage estimate |
| `docs/roadmap.md` | Completed items, next priorities, future ideas |
| `AGENTS.md` | Build/test commands, structure, conventions, invariants, anti-patterns |
