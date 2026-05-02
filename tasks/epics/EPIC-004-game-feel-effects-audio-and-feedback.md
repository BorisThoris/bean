---
id: EPIC-004
title: Game feel, effects, audio, and feedback
type: epic
status: open
priority: high
source: ultimate-physical-tapper-refinement
owner: codex
children:
  - TASK-027
  - TASK-028
  - TASK-029
  - TASK-030
  - TASK-031
  - TASK-032
  - TASK-033
  - TASK-034
---

# EPIC-004: Game feel, effects, audio, and feedback

## Goal

Make every tap and phase transition feel physical, legible, and satisfying without overwhelming the click target.

## Scope

Button animation, heat, focus windows, sparks, audio cues, celebration effects, and readability balancing.

## Acceptance Criteria

- All child tasks are either done or explicitly deferred under 	asks/INFEASIBLE_DEFERRAL_POLICY.md.
- The game still builds with dotnet build code\sweeper.csproj.
- Any player-facing change is verified in the scene or documented as blocked with exact reproduction notes.

## Implementation Notes

Effects are allowed to be expressive, but the button and score must remain visible during peak intensity.

## Validation

Run the verification commands listed in each child task, then regenerate 	asks/OPEN_TASKS_INVENTORY.md.