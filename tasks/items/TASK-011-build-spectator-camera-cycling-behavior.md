---
id: TASK-011
title: Build spectator camera cycling behavior
type: task
status: open
priority: medium
tier: 2
parent: EPIC-002
source: ultimate-physical-tapper-refinement
owner: codex
children: []
---

# TASK-011: Build spectator camera cycling behavior

## Goal

Give unassigned or late-join players a useful view of the game.

## Scope

Define spectator cameras for overview, leader, podium, and station focus states.

## Acceptance Criteria

- Spectators never spawn into a blank or obstructed view.
- Camera cycling can follow live leader or event phase.
- Spectator state is visually distinct from active station ownership.

## Implementation Notes

Keep implementation compatible with multiplayer sync boundaries.

## Validation

Join beyond available stations or force spectator state locally.

## Parallelization Notes

Can run after station and results framing are stable.