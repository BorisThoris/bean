---
id: TASK-013
title: Validate text containment and overlap across station counts
type: task
status: open
priority: high
tier: 2
parent: EPIC-002
source: ultimate-physical-tapper-refinement
owner: codex
children: []
---

# TASK-013: Validate text containment and overlap across station counts

## Goal

Ensure generated text remains readable with different player counts and modes.

## Scope

Check station labels, score text, central board, result rows, callouts, and debug text.

## Acceptance Criteria

- No text overlaps the button target or avatar head.
- Long names and score values fit or truncate cleanly.
- Layout works for 1, 2, 4, and 8 stations.

## Implementation Notes

Use stable dimensions and avoid dynamic text that shifts gameplay objects.

## Validation

Run visual checks with forced sample names and station counts.

## Parallelization Notes

Can run after layout objects are stable.