---
id: EPIC-006
title: Multiplayer sync, spectator, and station ownership
type: epic
status: open
priority: high
source: ultimate-physical-tapper-refinement
owner: codex
children:
  - TASK-045
  - TASK-046
  - TASK-047
  - TASK-048
  - TASK-049
  - TASK-050
  - TASK-051
  - TASK-052
  - TASK-053
  - TASK-054
---

# EPIC-006: Multiplayer sync, spectator, and station ownership

## Goal

Make server play reliable by keeping tap authority, station identity, spectator behavior, and disconnect handling explicit.

## Scope

Host-authoritative taps, synced names and stats, spectator states, late joins, disconnects, overflow, and multiplayer QA.

## Acceptance Criteria

- All child tasks are either done or explicitly deferred under 	asks/INFEASIBLE_DEFERRAL_POLICY.md.
- The game still builds with dotnet build code\sweeper.csproj.
- Any player-facing change is verified in the scene or documented as blocked with exact reproduction notes.

## Implementation Notes

Server correctness comes before presentation; clients should display synced state without becoming hidden authorities.

## Validation

Run the verification commands listed in each child task, then regenerate 	asks/OPEN_TASKS_INVENTORY.md.