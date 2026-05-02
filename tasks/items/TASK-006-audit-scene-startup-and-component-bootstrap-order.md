---
id: TASK-006
title: Audit scene startup and component bootstrap order
type: task
status: open
priority: high
tier: 2
parent: EPIC-001
source: ultimate-physical-tapper-refinement
owner: codex
children: []
---

# TASK-006: Audit scene startup and component bootstrap order

## Goal

Make initialization order explicit and resistant to null references.

## Scope

Trace component lifecycle across game, arena, camera, player assignment, network sync, and visual setup.

## Acceptance Criteria

- Startup dependencies are documented in code or ONGOING_TASKS.md.
- Null-sensitive calls have guards or delayed setup.
- Scene starts cleanly with one local player.

## Implementation Notes

Avoid broad rewrites; document the actual s&box lifecycle discovered.

## Validation

Run dotnet build and load scene twice from a fresh editor start.

## Parallelization Notes

Can be done independently after current code review.