---
id: EPIC-007
title: Avatar head expression and player identity
type: epic
status: open
priority: high
source: ultimate-physical-tapper-refinement
owner: codex
children:
  - TASK-055
  - TASK-056
  - TASK-057
  - TASK-058
  - TASK-059
  - TASK-060
  - TASK-061
  - TASK-062
---

# EPIC-007: Avatar head expression and player identity

## Goal

Use the s&box player character head as a core identity and feedback element instead of a flat profile image.

## Scope

Head-only rendering, expression morphs and fallbacks, camera visibility, winner treatment, hot reload behavior, and remote players.

## Acceptance Criteria

- All child tasks are either done or explicitly deferred under 	asks/INFEASIBLE_DEFERRAL_POLICY.md.
- The game still builds with dotnet build code\sweeper.csproj.
- Any player-facing change is verified in the scene or documented as blocked with exact reproduction notes.

## Implementation Notes

The avatar head should communicate neutral, focused, happy, and celebration states without becoming a gameplay blocker.

## Validation

Run the verification commands listed in each child task, then regenerate 	asks/OPEN_TASKS_INVENTORY.md.