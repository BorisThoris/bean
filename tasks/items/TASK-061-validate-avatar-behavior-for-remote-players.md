---
id: TASK-061
title: Validate avatar behavior for remote players
type: task
status: open
priority: high
tier: 2
parent: EPIC-007
source: ultimate-physical-tapper-refinement
owner: codex
children: []
---

# TASK-061: Validate avatar behavior for remote players

## Goal

Complete the refinement pass for Validate avatar behavior for remote players as part of the fully fledged physical fastest-clicker experience.

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