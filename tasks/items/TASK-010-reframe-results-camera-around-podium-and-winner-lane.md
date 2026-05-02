---
id: TASK-010
title: Reframe results camera around podium and winner lane
type: task
status: open
priority: high
tier: 1
parent: EPIC-002
source: ultimate-physical-tapper-refinement
owner: codex
children: []
---

# TASK-010: Reframe results camera around podium and winner lane

## Goal

Make round end read as a staged result moment instead of a frozen gameplay view.

## Scope

Tune results camera target, podium placement, winner effects, and board composition.

## Acceptance Criteria

- Winner, podium, and final scores are visible together.
- Camera transition does not clip through arena geometry.
- Text remains readable at common desktop aspect ratios.

## Implementation Notes

Treat results as a separate camera composition.

## Validation

Complete a round and capture the results state.

## Parallelization Notes

Can follow TASK-009.