---
id: TASK-015
title: Verify camera framing after hot reload and mode changes
type: task
status: open
priority: medium
tier: 2
parent: EPIC-002
source: ultimate-physical-tapper-refinement
owner: codex
children: []
---

# TASK-015: Verify camera framing after hot reload and mode changes

## Goal

Ensure camera state recovers after reloads, round changes, and tournament phase changes.

## Scope

Test camera initialization and target updates after lifecycle changes.

## Acceptance Criteria

- Camera returns to station view after hot reload.
- Mode transitions do not leave stale results framing.
- Spectator and podium cameras restore correctly.

## Implementation Notes

This protects long dev sessions where s&box hot reload is common.

## Validation

Hot reload in lobby, live, and results states.

## Parallelization Notes

Depends on core camera pass.