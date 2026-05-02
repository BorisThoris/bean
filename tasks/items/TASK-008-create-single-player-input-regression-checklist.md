---
id: TASK-008
title: Create single-player input regression checklist
type: task
status: done
priority: medium
tier: 2
parent: EPIC-001
source: ultimate-physical-tapper-refinement
owner: codex
children: []
---

# TASK-008: Create single-player input regression checklist

## Goal

Create a repeatable checklist for validating the single-player loop after every major change.

## Scope

Document launch, click, reset, hot reload, result, and no-exception checks.

## Acceptance Criteria

- Checklist exists in tasks or ONGOING_TASKS.md.
- Checklist names expected visual and scoring outcomes.
- Checklist includes build command and scene path.

## Implementation Notes

Keep it short enough that it actually gets used.

## Validation

Run the checklist once and record the result in ONGOING_TASKS.md.

## Parallelization Notes

Can be written after TASK-001 through TASK-004 clarify the flow.

## Execution Notes

- 2026-05-02: Completed in the first long-run execution pass. See 	asks/QA_CHECKLISTS.md, scripts/validate-task-inventory.mjs, 	asks/board.md, and ONGOING_TASKS.md for the concrete gate or handoff artifact.
