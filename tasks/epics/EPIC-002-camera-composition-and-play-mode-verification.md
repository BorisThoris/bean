---
id: EPIC-002
title: Camera composition and play-mode verification
type: epic
status: open
priority: critical
source: ultimate-physical-tapper-refinement
owner: codex
children:
  - TASK-009
  - TASK-010
  - TASK-011
  - TASK-012
  - TASK-013
  - TASK-014
  - TASK-015
  - TASK-016
---

# EPIC-002: Camera composition and play-mode verification

## Goal

Make the main scene read like a finished arcade game from first spawn through podium results.

## Scope

Station framing, results framing, spectator cameras, text containment, screenshot evidence, and verification across player counts.

## Acceptance Criteria

- All child tasks are either done or explicitly deferred under 	asks/INFEASIBLE_DEFERRAL_POLICY.md.
- The game still builds with dotnet build code\sweeper.csproj.
- Any player-facing change is verified in the scene or documented as blocked with exact reproduction notes.

## Implementation Notes

Camera work should privilege the physical button, player head, central board, and race context over decorative arena objects.

## Validation

Run the verification commands listed in each child task, then regenerate 	asks/OPEN_TASKS_INVENTORY.md.