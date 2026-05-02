# Sweeper Physical Tapper QA Checklists

These checks are the execution gate for the task inventory. Record failures in `ONGOING_TASKS.md` with exact exception text, scene state, and the task id that exposed the problem.

## Build And Inventory Gate

```powershell
node scripts/validate-task-inventory.mjs
node scripts/list-open-tasks.mjs > tasks/OPEN_TASKS_INVENTORY.md
dotnet build code\sweeper.csproj
```

Expected result:

- Inventory validator reports 10 epics and 88 tasks.
- Build succeeds with 0 errors.
- Generated inventory counts match the frontmatter statuses.

## Single-Player Input Gate

- Open `Assets/scenes/minimal.scene`.
- Enter real play mode, not editor object-selection mode.
- Verify the cursor is visible and uses the pointer cursor.
- Click the physical red station button and confirm the station becomes ready.
- Start the countdown, click during live play, and confirm score increases only for the clicked station.
- Press `SPACE` and `ENTER` and confirm they route to the local station.
- Click outside station buttons and confirm no wrong-station score is awarded.
- Complete a round, ready up from results/intermission, and start another round.

## Camera And Screenshot Gate

Capture or inspect these states at a normal desktop viewport:

- World load: central board shows `CONSTRUCT` if `facepunch.construct` is mounted, or `CONSTRUCT FALLBACK` if unavailable.
- Lobby or warmup: local station button, avatar head, station text, and central board visible.
- Countdown: timer readable and button still clickable.
- Live: score, speed, combo, heat/focus effects visible without covering the button.
- Photo finish: final seconds visible and station status readable.
- Results/podium: winner, podium lane, and results board visible together.
- Spectator or overflow: camera shows the arena rather than a blank or obstructed view.

## Hot Reload Gate

- Load the scene and reach warmup.
- Trigger a hot reload while waiting.
- Verify there is still one generated venue, one central board, and one set of stations.
- Start a round, trigger another hot reload during results, then ready for the next round.
- Confirm no stale references, duplicate station objects, or interop exceptions appear.

## Construct Map Log Gate

- Launch `Assets/scenes/minimal.scene`.
- In the editor console, filter for `[TapperConstruct]`.
- Expected successful sequence:
  - `[TapperConstruct] phase=OnLoad.Start`
  - `[TapperConstruct] phase=CreateAsync.Start`
  - `[TapperConstruct] phase=CreateAsync.Completed`
  - `[TapperConstruct] phase=OnStart.EnsureArena`
- If the map fails, capture the `[TapperConstruct] phase=CreateAsync.Failed` line including `exception=` and `message=`.
- In successful Construct mode, confirm `[TapperConstruct] phase=Stage.Placement` reports `suppressGeneratedAmbient=True`.
- Confirm there is no fake office shell, fake backdrop wall, fake crowd, fake speaker stack, CCTV, fire extinguisher, or ash-bin prop layer visible over Construct.
- Confirm only the compact tapper stage, station pads, central board, podium, and small stage markers are added over the Construct environment.
- If log files are written locally, run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\find-construct-logs.ps1
```

## Multiplayer Manual Gate

- Host a local lobby.
- Join with a second client if local tooling permits.
- Verify each active connection receives a unique station.
- Verify taps are host-authoritative and remote clients display synced score, ready, tournament points, focus hits, and result order.
- Disconnect one player and verify the station frees during lobby/results/intermission.
- Join after stations are full and verify spectator state is readable.

## Guardrail Gate

- No Razor gameplay page is added.
- `Assets/scenes/minimal.scene` is not manually rewritten for generated arena content.
- Construct is loaded through `Sandbox.SceneMap`; gameplay still starts from `Assets/scenes/minimal.scene`.
- Decorative props do not add blocking colliders.
- Optional prefab/map/leaderboard work has generated/offline fallbacks.
