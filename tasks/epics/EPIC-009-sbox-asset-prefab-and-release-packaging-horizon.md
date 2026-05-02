---
id: EPIC-009
title: s&box asset, prefab, and release packaging horizon
type: epic
status: open
priority: medium
source: ultimate-physical-tapper-refinement
owner: codex
children:
  - TASK-071
  - TASK-072
  - TASK-073
  - TASK-074
  - TASK-075
  - TASK-076
  - TASK-077
  - TASK-078
---

# EPIC-009: s&box asset, prefab, and release packaging horizon

## Goal

Define how this sample graduates from generated runtime geometry into a packaged s&box experience without blocking current playability.

## Scope

Prefab hooks, generated fallbacks, map packaging research, safe asset import boundaries, leaderboard readiness, and release checklist.

## Acceptance Criteria

- All child tasks are either done or explicitly deferred under 	asks/INFEASIBLE_DEFERRAL_POLICY.md.
- The game still builds with dotnet build code\sweeper.csproj.
- Any player-facing change is verified in the scene or documented as blocked with exact reproduction notes.

## Implementation Notes

This epic separates near-term shipped behavior from deferred asset/map work so speculative content does not destabilize the current game.

## Validation

Run the verification commands listed in each child task, then regenerate 	asks/OPEN_TASKS_INVENTORY.md.