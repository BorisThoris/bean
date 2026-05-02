# Infeasible Deferral Policy

Some desired refinements may depend on unavailable s&box APIs, editor-only behavior, marketplace assets, multiplayer infrastructure, or documentation that changes over time. Do not let those block the playable loop.

When a task cannot be completed in the current pass:

1. Mark the task `blocked` only when no safe local fallback exists.
2. Record the exact blocker, date, and attempted verification.
3. Add a fallback path that preserves the current physical game.
4. Keep speculative asset or map work outside core input, camera, and scoring paths.
5. Prefer generated runtime content until packaged assets have been verified in the scene.

Deferred work is acceptable only when the game still builds, loads, and remains playable.