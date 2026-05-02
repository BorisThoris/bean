---
id: EPIC-003
title: Physical arena venue and worldbuilding
type: epic
status: open
priority: high
source: ultimate-physical-tapper-refinement
owner: codex
children:
  - TASK-017
  - TASK-018
  - TASK-019
  - TASK-020
  - TASK-021
  - TASK-022
  - TASK-023
  - TASK-024
  - TASK-025
  - TASK-026
---

# EPIC-003: Physical arena venue and worldbuilding

## Goal

Turn the generated scene into a readable game-show venue with clear lanes, a result stage, lighting, and optional asset hooks.

## Scope

Arena hierarchy, lane geometry, podium, backdrop, ambient detail, lighting, prefab fallbacks, and layout stress tests.

## Acceptance Criteria

- All child tasks are either done or explicitly deferred under 	asks/INFEASIBLE_DEFERRAL_POLICY.md.
- The game still builds with dotnet build code\sweeper.csproj.
- Any player-facing change is verified in the scene or documented as blocked with exact reproduction notes.

## Implementation Notes

Worldbuilding must stay collision-light and gameplay-first so props never block taps or camera readability.

## Validation

Run the verification commands listed in each child task, then regenerate 	asks/OPEN_TASKS_INVENTORY.md.