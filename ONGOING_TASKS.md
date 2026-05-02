# Ongoing Tasks

This is the handoff document for long-running refinement passes on the s&box physical fastest-clicker game.

## How To Use

1. Pick a task from `tasks/OPEN_TASKS_INVENTORY.md`.
2. Set its frontmatter status to `in_progress`.
3. Update `tasks/board.md` with the current focus.
4. Make the scoped change.
5. Run the verification listed in the task.
6. Set status to `done` or `blocked`, regenerate the inventory, and add a dated note here.

## Last Updated

- 2026-05-02: Converted Construct mode into a real ambient-world setup. The fake generated office/backdrop/crowd/prop layer is now suppressed when Construct loads, and the tapper stage is moved to a compact Construct placement.
- 2026-05-02: Hardened Construct loading diagnostics against null `SceneMap` metadata so `OnLoad`/`OnStart` logging cannot cause the reported null-reference task exception. Added regression tests for null/failed diagnostic formatting.
- 2026-05-02: Moved Construct loading to the documented component `OnLoad` path with async `SceneMap.CreateAsync`. `OnStart` now builds the arena after map load state is known.
- 2026-05-02: Started executing the long-run plan. First implementation pass made mouse input raycast-aware, reframed cameras with explicit look targets, added inventory validation, and created shared QA gates.
- 2026-05-02: Created the MusicalAppReactConcept-style task inventory for executing the refined physical tapper game in multiple passes.

## Current Focus

- Verify Construct map loading in the editor/runtime.
- Capture station, spectator, and results screenshots with the Construct backdrop.
- Keep build and scene-load gates passing while executing the remaining refinement epics.

## Key Files

- `Assets/scenes/minimal.scene`
- `code/PhysicalFastestTapperGame.cs`
- `code/PhysicalFastestTapperGame.Arena.cs`
- `code/PhysicalFastestTapperGame.Camera.cs`
- `code/PhysicalFastestTapperGame.Network.cs`
- `code/PhysicalFastestTapperGame.Players.cs`
- `code/PhysicalFastestTapperGame.Round.cs`
- `code/PhysicalFastestTapperGame.Visuals.cs`
- `code/PhysicalTapButton.cs`
- `tasks/CONSTRUCT_WORLD_DECISION.md`
- `tasks/board.md`
- `tasks/OPEN_TASKS_INVENTORY.md`

## Verification Commands

```powershell
node scripts/validate-task-inventory.mjs
node scripts/list-open-tasks.mjs > tasks/OPEN_TASKS_INVENTORY.md
dotnet build code\sweeper.csproj
```

## QA Gates

- Use `tasks/QA_CHECKLISTS.md` for single-player input, camera screenshots, hot reload, multiplayer, and guardrail verification.
- Runtime/editor checks that cannot be completed from a terminal must be recorded against the task id with exact reproduction steps.

## Guardrails

- Preserve the physical object game direction.
- Do not reintroduce Razor gameplay pages.
- Do not hand-edit scene JSON unless a task explicitly calls for it.
- Prefer generated runtime fallbacks until packaged s&box assets are verified.
- Record runtime blockers with exact exception text and reproduction steps.
