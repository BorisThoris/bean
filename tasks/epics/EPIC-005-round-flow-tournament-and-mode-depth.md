---
id: EPIC-005
title: Round flow, tournament, and mode depth
type: epic
status: open
priority: high
source: ultimate-physical-tapper-refinement
owner: codex
children:
  - TASK-035
  - TASK-036
  - TASK-037
  - TASK-038
  - TASK-039
  - TASK-040
  - TASK-041
  - TASK-042
  - TASK-043
  - TASK-044
---

# EPIC-005: Round flow, tournament, and mode depth

## Goal

Refine the clicker from a single loop into a coherent session with modes, scoring, tie breakers, and readable event direction.

## Scope

Event phases, tournament sequencing, scoring balance, focus bonuses, live leader callouts, resets, and tuning documentation.

## Acceptance Criteria

- All child tasks are either done or explicitly deferred under 	asks/INFEASIBLE_DEFERRAL_POLICY.md.
- The game still builds with dotnet build code\sweeper.csproj.
- Any player-facing change is verified in the scene or documented as blocked with exact reproduction notes.

## Implementation Notes

The game should support short arcade rounds while still feeling like a complete tournament when several rounds are enabled.

## Validation

Run the verification commands listed in each child task, then regenerate 	asks/OPEN_TASKS_INVENTORY.md.