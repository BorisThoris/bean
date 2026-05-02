---
id: EPIC-008
title: Results, podium, replay, and session progression
type: epic
status: open
priority: medium
source: ultimate-physical-tapper-refinement
owner: codex
children:
  - TASK-063
  - TASK-064
  - TASK-065
  - TASK-066
  - TASK-067
  - TASK-068
  - TASK-069
  - TASK-070
---

# EPIC-008: Results, podium, replay, and session progression

## Goal

Make the end of each round feel complete, understandable, and worth watching before the next round begins.

## Scope

Results board, station summary, race trace, podium camera, replay loop, session stats, winner summary, and tie policies.

## Acceptance Criteria

- All child tasks are either done or explicitly deferred under 	asks/INFEASIBLE_DEFERRAL_POLICY.md.
- The game still builds with dotnet build code\sweeper.csproj.
- Any player-facing change is verified in the scene or documented as blocked with exact reproduction notes.

## Implementation Notes

Result presentation should explain who won and why without requiring the player to read raw debug-style state.

## Validation

Run the verification commands listed in each child task, then regenerate 	asks/OPEN_TASKS_INVENTORY.md.