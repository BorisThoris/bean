---
id: TASK-038
title: Balance scoring for Classic, Sprint, Endurance, and Combo
type: task
status: open
priority: high
tier: 2
parent: EPIC-005
source: ultimate-physical-tapper-refinement
owner: codex
children: []
---

# TASK-038: Balance scoring for Classic, Sprint, Endurance, and Combo

## Goal

Complete the refinement pass for Balance scoring for Classic, Sprint, Endurance, and Combo as part of the fully fledged physical fastest-clicker experience.

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