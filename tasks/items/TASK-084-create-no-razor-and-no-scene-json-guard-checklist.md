---
id: TASK-084
title: Create no-Razor and no-scene-JSON guard checklist
type: task
status: done
priority: medium
tier: 2
parent: EPIC-010
source: ultimate-physical-tapper-refinement
owner: codex
children: []
---

# TASK-084: Create no-Razor and no-scene-JSON guard checklist

## Goal

Complete the refinement pass for Create no-Razor and no-scene-JSON guard checklist as part of the fully fledged physical fastest-clicker experience.

## Scope

Inspect the current s&box implementation, make the smallest coherent set of code or content changes required, and update the task notes with what changed.

## Acceptance Criteria

- The task's player-facing behavior or project documentation is visibly improved.
- The change does not reintroduce Razor gameplay UI or scene JSON hand editing.
- The relevant build or manual verification step is recorded.

## Implementation Notes

Favor existing generated physical-game systems unless this task explicitly researches packaged assets or future map work.

## Validation

Run dotnet build code\sweeper.csproj, then perform the manual scene check named by the task.

## Parallelization Notes

Can be parallelized only when the worker owns disjoint files or produces documentation without touching gameplay code.

## Execution Notes

- 2026-05-02: Completed in the first long-run execution pass. See 	asks/QA_CHECKLISTS.md, scripts/validate-task-inventory.mjs, 	asks/board.md, and ONGOING_TASKS.md for the concrete gate or handoff artifact.
