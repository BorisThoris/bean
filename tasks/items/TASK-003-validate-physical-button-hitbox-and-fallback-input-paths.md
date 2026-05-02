---
id: TASK-003
title: Validate physical button hitbox and fallback input paths
type: task
status: open
priority: critical
tier: 1
parent: EPIC-001
source: ultimate-physical-tapper-refinement
owner: codex
children: []
---

# TASK-003: Validate physical button hitbox and fallback input paths

## Goal

Confirm the visible button, invisible hitbox, and fallback click path all award taps intentionally.

## Scope

Inspect PhysicalTapButton, station ownership lookup, hitbox dimensions, and fallback input code.

## Acceptance Criteria

- Button hit area covers the visible target from station camera.
- Fallback input cannot score for the wrong station.
- Misses and ownership failures are logged or surfaced clearly in debug mode.

## Implementation Notes

Keep physical object interaction as the primary path.

## Validation

Click center, edges, and outside the target while watching score changes.

## Parallelization Notes

Can proceed alongside TASK-002 if input mode is already known.