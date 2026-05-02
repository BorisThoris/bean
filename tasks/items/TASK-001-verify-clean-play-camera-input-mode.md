---
id: TASK-001
title: Verify clean play-camera input mode
type: task
status: open
priority: critical
tier: 1
parent: EPIC-001
source: ultimate-physical-tapper-refinement
owner: codex
children: []
---

# TASK-001: Verify clean play-camera input mode

## Goal

Prove the scene can be loaded into actual play mode with mouse clicks reaching the physical button.

## Scope

Run the current scene outside object-select/editor overlay state, identify the exact mode that captures input correctly, and document the launch path.

## Acceptance Criteria

- The physical button can be clicked without selecting scene objects.
- Cursor visibility and capture state are known at round start.
- A repeatable launch path is recorded in ONGOING_TASKS.md.

## Implementation Notes

Start from Assets/scenes/minimal.scene and test the primary local player station first.

## Validation

Launch the scene in s&box play mode and complete at least one scored round with mouse input.

## Parallelization Notes

Blocks most camera and game-feel QA; keep local.