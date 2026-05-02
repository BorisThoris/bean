---
id: TASK-004
title: Remove editor object-select overlay from QA flow
type: task
status: open
priority: high
tier: 1
parent: EPIC-001
source: ultimate-physical-tapper-refinement
owner: codex
children: []
---

# TASK-004: Remove editor object-select overlay from QA flow

## Goal

Prevent future QA from confusing editor object selection with broken gameplay input.

## Scope

Document play-mode steps, add debug messaging if editor selection mode is detected or suspected, and update task handoff notes.

## Acceptance Criteria

- QA notes describe how to avoid object-select mode.
- A future tester can distinguish editor selection from game input loss.
- The known issue is listed in ONGOING_TASKS.md.

## Implementation Notes

This may be documentation-only if s&box exposes no runtime signal.

## Validation

Follow the documented steps from a fresh editor session.

## Parallelization Notes

Depends on TASK-001 findings.