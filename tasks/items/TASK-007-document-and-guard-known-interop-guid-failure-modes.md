---
id: TASK-007
title: Document and guard known interop/GUID failure modes
type: task
status: open
priority: high
tier: 2
parent: EPIC-001
source: ultimate-physical-tapper-refinement
owner: codex
children: []
---

# TASK-007: Document and guard known interop/GUID failure modes

## Goal

Prevent repeats of the string-to-Guid and invocation interop failures.

## Scope

Search current code for serialized identifiers, component references, prefab properties, and string IDs that might flow into Guid-backed APIs.

## Acceptance Criteria

- Known interop errors are listed with causes and fixes.
- Risky serialized fields have safer types or validation.
- Build and scene load stay clean after changes.

## Implementation Notes

Keep this grounded in actual failures observed in this repo.

## Validation

Load minimal.scene and verify no interop exceptions on startup.

## Parallelization Notes

Can run with TASK-006.