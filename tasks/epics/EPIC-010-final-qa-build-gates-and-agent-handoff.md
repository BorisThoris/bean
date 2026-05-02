---
id: EPIC-010
title: Final QA, build gates, and agent handoff
type: epic
status: open
priority: critical
source: ultimate-physical-tapper-refinement
owner: codex
children:
  - TASK-079
  - TASK-080
  - TASK-081
  - TASK-082
  - TASK-083
  - TASK-084
  - TASK-085
  - TASK-086
  - TASK-087
  - TASK-088
---

# EPIC-010: Final QA, build gates, and agent handoff

## Goal

Keep long-running implementation passes organized, verifiable, and safe for future AI or human contributors.

## Scope

Build gates, backlog validation, manual QA, screenshots, hot reload checks, no-Razor/no-scene-JSON guardrails, handoff, and risk policy.

## Acceptance Criteria

- All child tasks are either done or explicitly deferred under 	asks/INFEASIBLE_DEFERRAL_POLICY.md.
- The game still builds with dotnet build code\sweeper.csproj.
- Any player-facing change is verified in the scene or documented as blocked with exact reproduction notes.

## Implementation Notes

This epic owns the process that prevents polish work from silently regressing the playable loop.

## Validation

Run the verification commands listed in each child task, then regenerate 	asks/OPEN_TASKS_INVENTORY.md.