---
id: TASK-002
title: Harden cursor visibility and mouse capture behavior
type: task
status: open
priority: critical
tier: 1
parent: EPIC-001
source: ultimate-physical-tapper-refinement
owner: codex
children: []
---

# TASK-002: Harden cursor visibility and mouse capture behavior

## Goal

Make the cursor/input behavior resilient when entering, leaving, and restarting the round.

## Scope

Review camera/input code and ensure intended mouse mode is applied at startup, player join, round reset, and hot reload.

## Acceptance Criteria

- Cursor behavior is deterministic at scene start.
- Click input still works after a round reset.
- Behavior is documented for editor and packaged play.

## Implementation Notes

Prefer s&box input APIs over ad hoc UI assumptions.

## Validation

Manual play test with two round resets and one hot reload.

## Parallelization Notes

Can run after TASK-001 establishes the expected behavior.