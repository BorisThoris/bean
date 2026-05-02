---
id: TASK-005
title: Harden hot reload object reuse and duplicate prevention
type: task
status: open
priority: high
tier: 2
parent: EPIC-001
source: ultimate-physical-tapper-refinement
owner: codex
children: []
---

# TASK-005: Harden hot reload object reuse and duplicate prevention

## Goal

Ensure generated arena objects do not duplicate or keep stale state after hot reload.

## Scope

Review runtime object naming, cleanup, station dictionary population, and generated hierarchy rebuild behavior.

## Acceptance Criteria

- Hot reload does not create duplicate stations, boards, podiums, or lights.
- Existing player assignments recover or reset predictably.
- No stale object references throw during the next round.

## Implementation Notes

Use stable names and explicit cleanup boundaries.

## Validation

Hot reload during lobby and during results, then start another round.

## Parallelization Notes

Can run after base input is verified.