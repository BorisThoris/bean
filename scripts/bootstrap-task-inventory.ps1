$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$tasksRoot = Join-Path $repoRoot "tasks"
$epicsRoot = Join-Path $tasksRoot "epics"
$itemsRoot = Join-Path $tasksRoot "items"
$archiveRoot = Join-Path $tasksRoot "archive"

New-Item -ItemType Directory -Force -Path $tasksRoot, $epicsRoot, $itemsRoot, $archiveRoot | Out-Null

function Write-Utf8File {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Content
    )

    $dir = Split-Path -Parent $Path
    if ($dir -and -not (Test-Path $dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }

    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Format-ChildrenYaml {
    param([string[]]$Children)

    if (-not $Children -or $Children.Count -eq 0) {
        return "children: []"
    }

    return "children:`n" + (($Children | ForEach-Object { "  - $_" }) -join "`n")
}

$epics = @(
    @{
        id = "EPIC-001"; file = "EPIC-001-runtime-stability-and-input-reliability.md"; title = "Runtime stability and input reliability"; priority = "critical";
        goal = "Make the physical clicker playable every time the scene loads, without cursor loss, interop exceptions, duplicate runtime objects, or editor-mode input traps.";
        scope = "Scene startup, component bootstrap, cursor capture, tap routing, editor/play-mode boundaries, and known s&box interop failure modes.";
        notes = "This epic is the first execution lane because every later polish pass depends on being able to load, click, hot reload, and repeat the loop.";
        children = @("TASK-001","TASK-002","TASK-003","TASK-004","TASK-005","TASK-006","TASK-007","TASK-008")
    },
    @{
        id = "EPIC-002"; file = "EPIC-002-camera-composition-and-play-mode-verification.md"; title = "Camera composition and play-mode verification"; priority = "critical";
        goal = "Make the main scene read like a finished arcade game from first spawn through podium results.";
        scope = "Station framing, results framing, spectator cameras, text containment, screenshot evidence, and verification across player counts.";
        notes = "Camera work should privilege the physical button, player head, central board, and race context over decorative arena objects.";
        children = @("TASK-009","TASK-010","TASK-011","TASK-012","TASK-013","TASK-014","TASK-015","TASK-016")
    },
    @{
        id = "EPIC-003"; file = "EPIC-003-physical-arena-venue-and-worldbuilding.md"; title = "Physical arena venue and worldbuilding"; priority = "high";
        goal = "Turn the generated scene into a readable game-show venue with clear lanes, a result stage, lighting, and optional asset hooks.";
        scope = "Arena hierarchy, lane geometry, podium, backdrop, ambient detail, lighting, prefab fallbacks, and layout stress tests.";
        notes = "Worldbuilding must stay collision-light and gameplay-first so props never block taps or camera readability.";
        children = @("TASK-017","TASK-018","TASK-019","TASK-020","TASK-021","TASK-022","TASK-023","TASK-024","TASK-025","TASK-026")
    },
    @{
        id = "EPIC-004"; file = "EPIC-004-game-feel-effects-audio-and-feedback.md"; title = "Game feel, effects, audio, and feedback"; priority = "high";
        goal = "Make every tap and phase transition feel physical, legible, and satisfying without overwhelming the click target.";
        scope = "Button animation, heat, focus windows, sparks, audio cues, celebration effects, and readability balancing.";
        notes = "Effects are allowed to be expressive, but the button and score must remain visible during peak intensity.";
        children = @("TASK-027","TASK-028","TASK-029","TASK-030","TASK-031","TASK-032","TASK-033","TASK-034")
    },
    @{
        id = "EPIC-005"; file = "EPIC-005-round-flow-tournament-and-mode-depth.md"; title = "Round flow, tournament, and mode depth"; priority = "high";
        goal = "Refine the clicker from a single loop into a coherent session with modes, scoring, tie breakers, and readable event direction.";
        scope = "Event phases, tournament sequencing, scoring balance, focus bonuses, live leader callouts, resets, and tuning documentation.";
        notes = "The game should support short arcade rounds while still feeling like a complete tournament when several rounds are enabled.";
        children = @("TASK-035","TASK-036","TASK-037","TASK-038","TASK-039","TASK-040","TASK-041","TASK-042","TASK-043","TASK-044")
    },
    @{
        id = "EPIC-006"; file = "EPIC-006-multiplayer-sync-spectator-and-station-ownership.md"; title = "Multiplayer sync, spectator, and station ownership"; priority = "high";
        goal = "Make server play reliable by keeping tap authority, station identity, spectator behavior, and disconnect handling explicit.";
        scope = "Host-authoritative taps, synced names and stats, spectator states, late joins, disconnects, overflow, and multiplayer QA.";
        notes = "Server correctness comes before presentation; clients should display synced state without becoming hidden authorities.";
        children = @("TASK-045","TASK-046","TASK-047","TASK-048","TASK-049","TASK-050","TASK-051","TASK-052","TASK-053","TASK-054")
    },
    @{
        id = "EPIC-007"; file = "EPIC-007-avatar-head-expression-and-player-identity.md"; title = "Avatar head expression and player identity"; priority = "high";
        goal = "Use the s&box player character head as a core identity and feedback element instead of a flat profile image.";
        scope = "Head-only rendering, expression morphs and fallbacks, camera visibility, winner treatment, hot reload behavior, and remote players.";
        notes = "The avatar head should communicate neutral, focused, happy, and celebration states without becoming a gameplay blocker.";
        children = @("TASK-055","TASK-056","TASK-057","TASK-058","TASK-059","TASK-060","TASK-061","TASK-062")
    },
    @{
        id = "EPIC-008"; file = "EPIC-008-results-podium-replay-and-session-progression.md"; title = "Results, podium, replay, and session progression"; priority = "medium";
        goal = "Make the end of each round feel complete, understandable, and worth watching before the next round begins.";
        scope = "Results board, station summary, race trace, podium camera, replay loop, session stats, winner summary, and tie policies.";
        notes = "Result presentation should explain who won and why without requiring the player to read raw debug-style state.";
        children = @("TASK-063","TASK-064","TASK-065","TASK-066","TASK-067","TASK-068","TASK-069","TASK-070")
    },
    @{
        id = "EPIC-009"; file = "EPIC-009-sbox-asset-prefab-and-release-packaging-horizon.md"; title = "s&box asset, prefab, and release packaging horizon"; priority = "medium";
        goal = "Define how this sample graduates from generated runtime geometry into a packaged s&box experience without blocking current playability.";
        scope = "Prefab hooks, generated fallbacks, map packaging research, safe asset import boundaries, leaderboard readiness, and release checklist.";
        notes = "This epic separates near-term shipped behavior from deferred asset/map work so speculative content does not destabilize the current game.";
        children = @("TASK-071","TASK-072","TASK-073","TASK-074","TASK-075","TASK-076","TASK-077","TASK-078")
    },
    @{
        id = "EPIC-010"; file = "EPIC-010-final-qa-build-gates-and-agent-handoff.md"; title = "Final QA, build gates, and agent handoff"; priority = "critical";
        goal = "Keep long-running implementation passes organized, verifiable, and safe for future AI or human contributors.";
        scope = "Build gates, backlog validation, manual QA, screenshots, hot reload checks, no-Razor/no-scene-JSON guardrails, handoff, and risk policy.";
        notes = "This epic owns the process that prevents polish work from silently regressing the playable loop.";
        children = @("TASK-079","TASK-080","TASK-081","TASK-082","TASK-083","TASK-084","TASK-085","TASK-086","TASK-087","TASK-088")
    }
)

$tasks = @(
    @{id="TASK-001"; title="Verify clean play-camera input mode"; parent="EPIC-001"; priority="critical"; tier=1; goal="Prove the scene can be loaded into actual play mode with mouse clicks reaching the physical button."; scope="Run the current scene outside object-select/editor overlay state, identify the exact mode that captures input correctly, and document the launch path."; acceptance=@("The physical button can be clicked without selecting scene objects.","Cursor visibility and capture state are known at round start.","A repeatable launch path is recorded in ONGOING_TASKS.md."); notes="Start from Assets/scenes/minimal.scene and test the primary local player station first."; validation="Launch the scene in s&box play mode and complete at least one scored round with mouse input."; parallel="Blocks most camera and game-feel QA; keep local."},
    @{id="TASK-002"; title="Harden cursor visibility and mouse capture behavior"; parent="EPIC-001"; priority="critical"; tier=1; goal="Make the cursor/input behavior resilient when entering, leaving, and restarting the round."; scope="Review camera/input code and ensure intended mouse mode is applied at startup, player join, round reset, and hot reload."; acceptance=@("Cursor behavior is deterministic at scene start.","Click input still works after a round reset.","Behavior is documented for editor and packaged play."); notes="Prefer s&box input APIs over ad hoc UI assumptions."; validation="Manual play test with two round resets and one hot reload."; parallel="Can run after TASK-001 establishes the expected behavior."},
    @{id="TASK-003"; title="Validate physical button hitbox and fallback input paths"; parent="EPIC-001"; priority="critical"; tier=1; goal="Confirm the visible button, invisible hitbox, and fallback click path all award taps intentionally."; scope="Inspect PhysicalTapButton, station ownership lookup, hitbox dimensions, and fallback input code."; acceptance=@("Button hit area covers the visible target from station camera.","Fallback input cannot score for the wrong station.","Misses and ownership failures are logged or surfaced clearly in debug mode."); notes="Keep physical object interaction as the primary path."; validation="Click center, edges, and outside the target while watching score changes."; parallel="Can proceed alongside TASK-002 if input mode is already known."},
    @{id="TASK-004"; title="Remove editor object-select overlay from QA flow"; parent="EPIC-001"; priority="high"; tier=1; goal="Prevent future QA from confusing editor object selection with broken gameplay input."; scope="Document play-mode steps, add debug messaging if editor selection mode is detected or suspected, and update task handoff notes."; acceptance=@("QA notes describe how to avoid object-select mode.","A future tester can distinguish editor selection from game input loss.","The known issue is listed in ONGOING_TASKS.md."); notes="This may be documentation-only if s&box exposes no runtime signal."; validation="Follow the documented steps from a fresh editor session."; parallel="Depends on TASK-001 findings."},
    @{id="TASK-005"; title="Harden hot reload object reuse and duplicate prevention"; parent="EPIC-001"; priority="high"; tier=2; goal="Ensure generated arena objects do not duplicate or keep stale state after hot reload."; scope="Review runtime object naming, cleanup, station dictionary population, and generated hierarchy rebuild behavior."; acceptance=@("Hot reload does not create duplicate stations, boards, podiums, or lights.","Existing player assignments recover or reset predictably.","No stale object references throw during the next round."); notes="Use stable names and explicit cleanup boundaries."; validation="Hot reload during lobby and during results, then start another round."; parallel="Can run after base input is verified."},
    @{id="TASK-006"; title="Audit scene startup and component bootstrap order"; parent="EPIC-001"; priority="high"; tier=2; goal="Make initialization order explicit and resistant to null references."; scope="Trace component lifecycle across game, arena, camera, player assignment, network sync, and visual setup."; acceptance=@("Startup dependencies are documented in code or ONGOING_TASKS.md.","Null-sensitive calls have guards or delayed setup.","Scene starts cleanly with one local player."); notes="Avoid broad rewrites; document the actual s&box lifecycle discovered."; validation="Run dotnet build and load scene twice from a fresh editor start."; parallel="Can be done independently after current code review."},
    @{id="TASK-007"; title="Document and guard known interop/GUID failure modes"; parent="EPIC-001"; priority="high"; tier=2; goal="Prevent repeats of the string-to-Guid and invocation interop failures."; scope="Search current code for serialized identifiers, component references, prefab properties, and string IDs that might flow into Guid-backed APIs."; acceptance=@("Known interop errors are listed with causes and fixes.","Risky serialized fields have safer types or validation.","Build and scene load stay clean after changes."); notes="Keep this grounded in actual failures observed in this repo."; validation="Load minimal.scene and verify no interop exceptions on startup."; parallel="Can run with TASK-006."},
    @{id="TASK-008"; title="Create single-player input regression checklist"; parent="EPIC-001"; priority="medium"; tier=2; goal="Create a repeatable checklist for validating the single-player loop after every major change."; scope="Document launch, click, reset, hot reload, result, and no-exception checks."; acceptance=@("Checklist exists in tasks or ONGOING_TASKS.md.","Checklist names expected visual and scoring outcomes.","Checklist includes build command and scene path."); notes="Keep it short enough that it actually gets used."; validation="Run the checklist once and record the result in ONGOING_TASKS.md."; parallel="Can be written after TASK-001 through TASK-004 clarify the flow."},
    @{id="TASK-009"; title="Reframe station camera for button, face, status, and central board"; parent="EPIC-002"; priority="critical"; tier=1; goal="Make the default camera composition clearly show the player's station and the game state."; scope="Tune camera position, FOV, target offsets, station head placement, and central board visibility."; acceptance=@("Button is prominent and clickable from the view.","Avatar head is visible without covering the button.","Central status is readable in peripheral view."); notes="Use Play/viewport screenshots as the source of truth, not intuition."; validation="Capture desktop screenshots for lobby, live, and results from station 1."; parallel="Depends on reliable play-mode input."},
    @{id="TASK-010"; title="Reframe results camera around podium and winner lane"; parent="EPIC-002"; priority="high"; tier=1; goal="Make round end read as a staged result moment instead of a frozen gameplay view."; scope="Tune results camera target, podium placement, winner effects, and board composition."; acceptance=@("Winner, podium, and final scores are visible together.","Camera transition does not clip through arena geometry.","Text remains readable at common desktop aspect ratios."); notes="Treat results as a separate camera composition."; validation="Complete a round and capture the results state."; parallel="Can follow TASK-009."},
    @{id="TASK-011"; title="Build spectator camera cycling behavior"; parent="EPIC-002"; priority="medium"; tier=2; goal="Give unassigned or late-join players a useful view of the game."; scope="Define spectator cameras for overview, leader, podium, and station focus states."; acceptance=@("Spectators never spawn into a blank or obstructed view.","Camera cycling can follow live leader or event phase.","Spectator state is visually distinct from active station ownership."); notes="Keep implementation compatible with multiplayer sync boundaries."; validation="Join beyond available stations or force spectator state locally."; parallel="Can run after station and results framing are stable."},
    @{id="TASK-012"; title="Create screenshot capture checklist for desktop viewports"; parent="EPIC-002"; priority="medium"; tier=2; goal="Define evidence needed before calling the game visually refined."; scope="List required screenshots for lobby, countdown, live, focus, photo finish, results, podium, and spectator states."; acceptance=@("Checklist includes viewport sizes and scene path.","Each screenshot has expected visible elements.","Checklist is referenced by ONGOING_TASKS.md."); notes="Screenshots should catch overlap and nonblank rendering issues."; validation="Dry-run the checklist with at least one captured screenshot."; parallel="Can be authored independently."},
    @{id="TASK-013"; title="Validate text containment and overlap across station counts"; parent="EPIC-002"; priority="high"; tier=2; goal="Ensure generated text remains readable with different player counts and modes."; scope="Check station labels, score text, central board, result rows, callouts, and debug text."; acceptance=@("No text overlaps the button target or avatar head.","Long names and score values fit or truncate cleanly.","Layout works for 1, 2, 4, and 8 stations."); notes="Use stable dimensions and avoid dynamic text that shifts gameplay objects."; validation="Run visual checks with forced sample names and station counts."; parallel="Can run after layout objects are stable."},
    @{id="TASK-014"; title="Tune central board typography and hierarchy"; parent="EPIC-002"; priority="medium"; tier=2; goal="Make the central board communicate phase, timer, leader, and next action at a glance."; scope="Review text sizes, labels, ordering, contrast, and update frequency."; acceptance=@("Phase and timer are the most prominent board elements.","Leader and mode details are readable but secondary.","Board text does not look like debug output."); notes="This can be physical text in the world, not Razor UI."; validation="Review board during every event phase."; parallel="Can run with TASK-013."},
    @{id="TASK-015"; title="Verify camera framing after hot reload and mode changes"; parent="EPIC-002"; priority="medium"; tier=2; goal="Ensure camera state recovers after reloads, round changes, and tournament phase changes."; scope="Test camera initialization and target updates after lifecycle changes."; acceptance=@("Camera returns to station view after hot reload.","Mode transitions do not leave stale results framing.","Spectator and podium cameras restore correctly."); notes="This protects long dev sessions where s&box hot reload is common."; validation="Hot reload in lobby, live, and results states."; parallel="Depends on core camera pass."},
    @{id="TASK-016"; title="Document camera composition constraints for future edits"; parent="EPIC-002"; priority="low"; tier=3; goal="Prevent later arena or avatar changes from breaking camera readability."; scope="Write concise constraints for object placement, text size, and camera targets."; acceptance=@("Constraints name the primary visible elements.","Constraints include safe placement zones around the button.","Future tasks reference this document when touching layout."); notes="Add to ONGOING_TASKS.md or a task note, not a sprawling design doc."; validation="Review constraints against current screenshots."; parallel="Can be done after framing decisions settle."}
)

$moreTasks = @(
    @("TASK-017","Audit generated venue object hierarchy and naming","EPIC-003","high",2),@("TASK-018","Polish arena floor, lanes, rails, and station spacing","EPIC-003","high",2),@("TASK-019","Refine podium geometry and results destination","EPIC-003","high",2),@("TASK-020","Improve world backdrop and ceiling readability","EPIC-003","medium",2),@("TASK-021","Tune ambient lighting motion and crowd silhouettes","EPIC-003","medium",3),@("TASK-022","Validate decorative collider-free policy","EPIC-003","high",2),@("TASK-023","Add optional prefab hook validation and fallback behavior","EPIC-003","medium",3),@("TASK-024","Create s&box asset sourcing checklist","EPIC-003","medium",3),@("TASK-025","Stress test 1, 2, 4, and 8 station venue layouts","EPIC-003","high",2),@("TASK-026","Document worldbuilding performance budgets","EPIC-003","medium",3),
    @("TASK-027","Tune button punch, top depression, and heat response","EPIC-004","high",2),@("TASK-028","Tune focus ring timing and readability","EPIC-004","high",2),@("TASK-029","Refine spark and celebration effects","EPIC-004","medium",2),@("TASK-030","Improve audio feedback and sound throttling","EPIC-004","medium",3),@("TASK-031","Polish winner glow, crown, and streak effects","EPIC-004","medium",2),@("TASK-032","Balance visual intensity across modes","EPIC-004","medium",3),@("TASK-033","Verify effects never obscure click targets","EPIC-004","high",2),@("TASK-034","Create game-feel QA matrix","EPIC-004","medium",3),
    @("TASK-035","Audit event phase transitions","EPIC-005","high",2),@("TASK-036","Balance tournament mode sequence","EPIC-005","high",2),@("TASK-037","Tune final tie-breaker behavior","EPIC-005","medium",2),@("TASK-038","Balance scoring for Classic, Sprint, Endurance, and Combo","EPIC-005","high",2),@("TASK-039","Validate focus-window bonus rules","EPIC-005","medium",2),@("TASK-040","Improve event director board callouts","EPIC-005","medium",3),@("TASK-041","Add deterministic live leader callout rules","EPIC-005","medium",3),@("TASK-042","Verify round reset ownership boundaries","EPIC-005","high",2),@("TASK-043","Create tournament completion and reset flow","EPIC-005","medium",3),@("TASK-044","Document gameplay tuning constants","EPIC-005","medium",3),
    @("TASK-045","Audit host-authoritative tap path","EPIC-006","critical",1),@("TASK-046","Sync display names and station identity data","EPIC-006","high",2),@("TASK-047","Sync tournament result summary data","EPIC-006","high",2),@("TASK-048","Polish spectator state messaging","EPIC-006","medium",3),@("TASK-049","Validate late join behavior","EPIC-006","high",2),@("TASK-050","Validate disconnect and station freeing","EPIC-006","high",2),@("TASK-051","Test two-player server flow","EPIC-006","critical",1),@("TASK-052","Test full-station overflow behavior","EPIC-006","medium",3),@("TASK-053","Harden client display-only state handling","EPIC-006","high",2),@("TASK-054","Create multiplayer manual QA script","EPIC-006","medium",3),
    @("TASK-055","Verify s&box head-only avatar dressing path","EPIC-007","high",2),@("TASK-056","Tune expression morph names and fallbacks","EPIC-007","high",2),@("TASK-057","Improve avatar visibility from station camera","EPIC-007","high",2),@("TASK-058","Polish winner expression and crown composition","EPIC-007","medium",2),@("TASK-059","Polish high-heat and photo-finish facial states","EPIC-007","medium",3),@("TASK-060","Reapply bodygroup policy after hot reload","EPIC-007","medium",3),@("TASK-061","Validate avatar behavior for remote players","EPIC-007","high",2),@("TASK-062","Document avatar rendering constraints","EPIC-007","medium",3),
    @("TASK-063","Refine central results board rows","EPIC-008","medium",2),@("TASK-064","Polish station round summary text","EPIC-008","medium",2),@("TASK-065","Improve race-trace bar readability","EPIC-008","medium",2),@("TASK-066","Add podium camera result verification","EPIC-008","medium",2),@("TASK-067","Validate replay loop and ready-next flow","EPIC-008","medium",3),@("TASK-068","Expand session stat presentation","EPIC-008","low",3),@("TASK-069","Create tournament winner summary flow","EPIC-008","medium",3),@("TASK-070","Document result ordering and tie-break policy","EPIC-008","medium",3),
    @("TASK-071","Audit optional prefab hook API","EPIC-009","medium",3),@("TASK-072","Test generated fallback when prefabs are unset","EPIC-009","medium",3),@("TASK-073","Research compiled map packaging policy","EPIC-009","medium",3),@("TASK-074","Define safe asset import boundaries","EPIC-009","medium",3),@("TASK-075","Plan leaderboard metadata enablement","EPIC-009","low",3),@("TASK-076","Validate offline behavior without leaderboard services","EPIC-009","medium",3),@("TASK-077","Create release packaging checklist","EPIC-009","medium",3),@("TASK-078","Document deferred asset/map decisions","EPIC-009","medium",3),
    @("TASK-079","Classify reliable build and runtime gates","EPIC-010","critical",1),@("TASK-080","Add backlog inventory validation script","EPIC-010","high",2),@("TASK-081","Create manual QA matrix for final refinement","EPIC-010","high",2),@("TASK-082","Create screenshot evidence workflow","EPIC-010","high",2),@("TASK-083","Create hot reload regression procedure","EPIC-010","medium",2),@("TASK-084","Create no-Razor and no-scene-JSON guard checklist","EPIC-010","medium",2),@("TASK-085","Create AI-run claiming and handoff protocol","EPIC-010","high",2),@("TASK-086","Create final release readiness checklist","EPIC-010","high",2),@("TASK-087","Define known-risk and blocked-work policy","EPIC-010","medium",3),@("TASK-088","Keep task board and inventory synchronized","EPIC-010","high",2)
)

foreach ($row in $moreTasks) {
    $id = $row[0]
    $title = $row[1]
    $parent = $row[2]
    $priority = $row[3]
    $tier = [int]$row[4]
    $tasks += @{
        id = $id
        title = $title
        parent = $parent
        priority = $priority
        tier = $tier
        goal = "Complete the refinement pass for $title as part of the fully fledged physical fastest-clicker experience."
        scope = "Inspect the current s&box implementation, make the smallest coherent set of code or content changes required, and update the task notes with what changed."
        acceptance = @("The task's player-facing behavior or project documentation is visibly improved.","The change does not reintroduce Razor gameplay UI or scene JSON hand editing.","The relevant build or manual verification step is recorded.")
        notes = "Favor existing generated physical-game systems unless this task explicitly researches packaged assets or future map work."
        validation = "Run dotnet build code\sweeper.csproj, then perform the manual scene check named by the task."
        parallel = "Can be parallelized only when the worker owns disjoint files or produces documentation without touching gameplay code."
    }
}

Write-Utf8File (Join-Path $tasksRoot "README.md") @'
# Sweeper Physical Tapper Task Tracker

This folder is the active task inventory for the s&box physical fastest-clicker game. `ULTIMATE_TAPPER_EPICS.md` remains useful historical context, but new execution should be tracked here.

## Structure

- `tasks/README.md` explains this tracker.
- `tasks/board.md` is the human-readable board.
- `tasks/OPEN_TASKS_INVENTORY.md` is generated by `node scripts/list-open-tasks.mjs`.
- `tasks/epics/` contains one file per epic.
- `tasks/items/` contains one file per task.
- `tasks/archive/` is reserved for old completed work that becomes noisy.

## Rules

- Keep one file per epic or task.
- Use frontmatter status values: `open`, `in_progress`, `blocked`, `done`.
- Do not move files by status; update frontmatter and regenerate the inventory.
- Keep `children` on epics and `parent` on tasks in sync.
- Update `tasks/board.md` and `ONGOING_TASKS.md` when claiming, completing, blocking, or deferring work.
- Archive only when completed work is no longer useful in the active tree.

## Verification

Regenerate the inventory after task edits:

```powershell
node scripts/list-open-tasks.mjs > tasks/OPEN_TASKS_INVENTORY.md
```

Build the game after any code change:

```powershell
dotnet build code\sweeper.csproj
```
'@

Write-Utf8File (Join-Path $tasksRoot "INFEASIBLE_DEFERRAL_POLICY.md") @'
# Infeasible Deferral Policy

Some desired refinements may depend on unavailable s&box APIs, editor-only behavior, marketplace assets, multiplayer infrastructure, or documentation that changes over time. Do not let those block the playable loop.

When a task cannot be completed in the current pass:

1. Mark the task `blocked` only when no safe local fallback exists.
2. Record the exact blocker, date, and attempted verification.
3. Add a fallback path that preserves the current physical game.
4. Keep speculative asset or map work outside core input, camera, and scoring paths.
5. Prefer generated runtime content until packaged assets have been verified in the scene.

Deferred work is acceptable only when the game still builds, loads, and remains playable.
'@

Write-Utf8File (Join-Path $repoRoot "ONGOING_TASKS.md") @'
# Ongoing Tasks

This is the handoff document for long-running refinement passes on the s&box physical fastest-clicker game.

## How To Use

1. Pick a task from `tasks/OPEN_TASKS_INVENTORY.md`.
2. Set its frontmatter status to `in_progress`.
3. Update `tasks/board.md` with the current focus.
4. Make the scoped change.
5. Run the verification listed in the task.
6. Set status to `done` or `blocked`, regenerate the inventory, and add a dated note here.

## Last Updated

- 2026-05-02: Created the MusicalAppReactConcept-style task inventory for executing the refined physical tapper game in multiple passes.

## Current Focus

- Verify clean play-camera input mode.
- Fix camera composition around the physical button, player head, status text, and result podium.
- Keep build and scene-load gates passing while executing the refinement epics.

## Key Files

- `Assets/scenes/minimal.scene`
- `code/PhysicalFastestTapperGame.cs`
- `code/PhysicalFastestTapperGame.Arena.cs`
- `code/PhysicalFastestTapperGame.Camera.cs`
- `code/PhysicalFastestTapperGame.Network.cs`
- `code/PhysicalFastestTapperGame.Players.cs`
- `code/PhysicalFastestTapperGame.Round.cs`
- `code/PhysicalFastestTapperGame.Visuals.cs`
- `code/PhysicalTapButton.cs`
- `tasks/board.md`
- `tasks/OPEN_TASKS_INVENTORY.md`

## Verification Commands

```powershell
node scripts/list-open-tasks.mjs > tasks/OPEN_TASKS_INVENTORY.md
dotnet build code\sweeper.csproj
```

## Guardrails

- Preserve the physical object game direction.
- Do not reintroduce Razor gameplay pages.
- Do not hand-edit scene JSON unless a task explicitly calls for it.
- Prefer generated runtime fallbacks until packaged s&box assets are verified.
- Record runtime blockers with exact exception text and reproduction steps.
'@

$boardOpen = ($epics | ForEach-Object {
    $children = $_.children -join ", "
    "- $($_.id): $($_.title) ($children)"
}) -join "`n"

Write-Utf8File (Join-Path $tasksRoot "board.md") @"
# Task Board

Last updated: 2026-05-02

## In Progress

- None. Claim the next task by setting its status to `in_progress`.

## Recently Completed

- Created the physical fastest-clicker runtime direction in `ULTIMATE_TAPPER_EPICS.md` and current game code.
- Added tournament, focus, podium, avatar-head, and generated venue systems before this structured tracker existed.

## Open

$boardOpen

## Blocked

- None currently recorded.

## Done

- Historical completed work is summarized in `ULTIMATE_TAPPER_EPICS.md`; new completed task files should stay in `tasks/items/` until they become noisy.
"@

foreach ($epic in $epics) {
    $childrenYaml = Format-ChildrenYaml $epic.children
    $content = @"
---
id: $($epic.id)
title: $($epic.title)
type: epic
status: open
priority: $($epic.priority)
source: ultimate-physical-tapper-refinement
owner: codex
$childrenYaml
---

# $($epic.id): $($epic.title)

## Goal

$($epic.goal)

## Scope

$($epic.scope)

## Acceptance Criteria

- All child tasks are either `done` or explicitly deferred under `tasks/INFEASIBLE_DEFERRAL_POLICY.md`.
- The game still builds with `dotnet build code\sweeper.csproj`.
- Any player-facing change is verified in the scene or documented as blocked with exact reproduction notes.

## Implementation Notes

$($epic.notes)

## Validation

Run the verification commands listed in each child task, then regenerate `tasks/OPEN_TASKS_INVENTORY.md`.
"@
    Write-Utf8File (Join-Path $epicsRoot $epic.file) $content
}

foreach ($task in $tasks) {
    $slug = (($task.title.ToLowerInvariant() -replace "[^a-z0-9]+", "-").Trim("-"))
    $file = "$($task.id)-$slug.md"
    $acceptance = ($task.acceptance | ForEach-Object { "- $_" }) -join "`n"
    $content = @"
---
id: $($task.id)
title: $($task.title)
type: task
status: open
priority: $($task.priority)
tier: $($task.tier)
parent: $($task.parent)
source: ultimate-physical-tapper-refinement
owner: codex
children: []
---

# $($task.id): $($task.title)

## Goal

$($task.goal)

## Scope

$($task.scope)

## Acceptance Criteria

$acceptance

## Implementation Notes

$($task.notes)

## Validation

$($task.validation)

## Parallelization Notes

$($task.parallel)
"@
    Write-Utf8File (Join-Path $itemsRoot $file) $content
}

Write-Utf8File (Join-Path $repoRoot "scripts/list-open-tasks.mjs") @'
import fs from "node:fs";
import path from "node:path";

const repoRoot = process.cwd();
const tasksRoot = path.join(repoRoot, "tasks");
const itemsRoot = path.join(tasksRoot, "items");
const epicsRoot = path.join(tasksRoot, "epics");

function readMarkdownFiles(dir) {
  if (!fs.existsSync(dir)) return [];
  return fs
    .readdirSync(dir)
    .filter((file) => file.endsWith(".md"))
    .sort()
    .map((file) => {
      const fullPath = path.join(dir, file);
      return { file, fullPath, text: fs.readFileSync(fullPath, "utf8") };
    });
}

function parseFrontmatter(text) {
  if (!text.startsWith("---\n")) return {};
  const end = text.indexOf("\n---", 4);
  if (end === -1) return {};

  const lines = text.slice(4, end).split(/\r?\n/);
  const data = {};
  let currentKey = null;

  for (const line of lines) {
    const keyMatch = line.match(/^([A-Za-z0-9_-]+):(?:\s*(.*))?$/);
    if (keyMatch) {
      currentKey = keyMatch[1];
      const value = keyMatch[2] ?? "";
      data[currentKey] = value === "[]" ? [] : value;
      continue;
    }

    const itemMatch = line.match(/^\s*-\s+(.+)$/);
    if (itemMatch && currentKey) {
      if (!Array.isArray(data[currentKey])) data[currentKey] = [];
      data[currentKey].push(itemMatch[1]);
    }
  }

  return data;
}

const epics = readMarkdownFiles(epicsRoot).map((entry) => ({
  ...entry,
  meta: parseFrontmatter(entry.text),
}));

const tasks = readMarkdownFiles(itemsRoot).map((entry) => ({
  ...entry,
  meta: parseFrontmatter(entry.text),
}));

const epicById = new Map(epics.map((epic) => [epic.meta.id, epic]));
const grouped = new Map();

for (const task of tasks) {
  const parent = task.meta.parent || "NO_PARENT";
  if (!grouped.has(parent)) grouped.set(parent, []);
  grouped.get(parent).push(task);
}

const statusOrder = new Map([
  ["in_progress", 0],
  ["blocked", 1],
  ["open", 2],
  ["done", 3],
]);

function taskSort(a, b) {
  const statusDelta = (statusOrder.get(a.meta.status) ?? 99) - (statusOrder.get(b.meta.status) ?? 99);
  if (statusDelta) return statusDelta;
  return String(a.meta.id).localeCompare(String(b.meta.id), undefined, { numeric: true });
}

function formatTask(task) {
  const rel = path.relative(repoRoot, task.fullPath).replaceAll(path.sep, "/");
  const status = task.meta.status || "unknown";
  const priority = task.meta.priority || "unprioritized";
  const tier = task.meta.tier ? `tier ${task.meta.tier}` : "no tier";
  return `- [${status}] ${task.meta.id}: ${task.meta.title} (${priority}, ${tier}) - ${rel}`;
}

const now = new Date().toISOString().slice(0, 10);
const output = [];
output.push("# Open Tasks Inventory");
output.push("");
output.push(`Generated: ${now}`);
output.push("");
output.push("This file is generated by `node scripts/list-open-tasks.mjs`. Edit task frontmatter, then regenerate it.");
output.push("");

for (const epic of epics) {
  const children = (grouped.get(epic.meta.id) || []).sort(taskSort);
  const activeChildren = children.filter((task) => task.meta.status !== "done");
  if (activeChildren.length === 0 && epic.meta.status === "done") continue;

  output.push(`## ${epic.meta.id}: ${epic.meta.title}`);
  output.push("");
  output.push(`- Status: ${epic.meta.status || "unknown"}`);
  output.push(`- Priority: ${epic.meta.priority || "unprioritized"}`);
  output.push("");

  if (activeChildren.length === 0) {
    output.push("- No open child tasks.");
  } else {
    for (const task of activeChildren) output.push(formatTask(task));
  }
  output.push("");
}

const noParent = (grouped.get("NO_PARENT") || []).sort(taskSort);
if (noParent.length > 0) {
  output.push("## Tasks Without Parent");
  output.push("");
  for (const task of noParent) output.push(formatTask(task));
  output.push("");
}

const counts = tasks.reduce((acc, task) => {
  const status = task.meta.status || "unknown";
  acc[status] = (acc[status] || 0) + 1;
  return acc;
}, {});

output.push("## Counts");
output.push("");
for (const status of Object.keys(counts).sort()) {
  output.push(`- ${status}: ${counts[status]}`);
}
output.push(`- total: ${tasks.length}`);
output.push("");

fs.writeFileSync(1, output.join("\n"));
'@

Write-Host "Generated $($epics.Count) epics and $($tasks.Count) tasks."
