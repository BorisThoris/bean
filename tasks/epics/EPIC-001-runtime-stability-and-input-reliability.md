---
id: EPIC-001
title: Runtime stability and input reliability
type: epic
status: open
priority: critical
source: ultimate-physical-tapper-refinement
owner: codex
children:
  - TASK-001
  - TASK-002
  - TASK-003
  - TASK-004
  - TASK-005
  - TASK-006
  - TASK-007
  - TASK-008
---

# EPIC-001: Runtime stability and input reliability

## Goal

Make the physical clicker playable every time the scene loads, without cursor loss, interop exceptions, duplicate runtime objects, or editor-mode input traps.

## Scope

Scene startup, component bootstrap, cursor capture, tap routing, editor/play-mode boundaries, and known s&box interop failure modes.

## Acceptance Criteria

- All child tasks are either done or explicitly deferred under 	asks/INFEASIBLE_DEFERRAL_POLICY.md.
- The game still builds with dotnet build code\sweeper.csproj.
- Any player-facing change is verified in the scene or documented as blocked with exact reproduction notes.

## Implementation Notes

This epic is the first execution lane because every later polish pass depends on being able to load, click, hot reload, and repeat the loop.

## Validation

Run the verification commands listed in each child task, then regenerate 	asks/OPEN_TASKS_INVENTORY.md.