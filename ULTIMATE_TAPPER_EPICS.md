# Ultimate Tapper Development Epics

This document is the working backlog for turning the current physical fastest-tapper prototype into a refined multiplayer s&box arcade game. Each epic is designed to be completed in multiple small passes. Every implementation pass should end with `dotnet build code/sweeper.csproj` passing with zero errors, and preferably zero warnings.

## Current Baseline

- Project: `sweeper`
- Startup scene: `Assets/scenes/minimal.scene`
- Game type: multiplayer in `.sbproj`
- Current gameplay: runtime-generated physical tapper arena with station-aware buttons, round states, local fallback input, generated world-space text, and s&box avatar head expressions.
- Known constraint: prefer runtime-generated objects over hand-editing scene JSON because prior scene serialization caused interop/GUID errors.
- UI constraint: no Razor/web UI for gameplay; use physical world objects and `TextRenderer`.

## Task Status Legend

- `[TODO]` Not started.
- `[IN_PROGRESS]` Started but not complete.
- `[DONE]` Implemented and build-verified.
- `[BLOCKED]` Waiting on an external decision, API, or asset.

## Run Progress

- `2026-05-02` Run 1 Foundation Hardening: `[DONE]`
  - Split state/player/station data models into `code/PhysicalFastestTapperGame.Models.cs`.
  - Added disconnected-player cleanup and a guarded local fallback player path.
  - Verified with `dotnet build code/sweeper.csproj`: 0 warnings, 0 errors.
- `2026-05-02` Run 2 World And Camera Readability: `[DONE]`
  - Added generated station floor markers, ready lights, winner glow pads, and station number labels.
  - Added station/results/spectator camera mode selection without changing the startup scene JSON.
  - Added open/ready/heat/winner station identity coloring.
  - Verified with `dotnet build code/sweeper.csproj`: 0 warnings, 0 errors.
- `2026-05-02` Run 3 Multiplayer Assignment Guard: `[DONE]`
  - Added explicit spectator state for late joiners and full-station overflow.
  - Filtered ready checks, winners, and leaderboard rows to active station competitors.
  - Promotes spectators into open stations between rounds.
  - Verified with `dotnet build code/sweeper.csproj`: 0 warnings, 0 errors.
- `2026-05-02` Run 4 Authority, Modes, Results, And Avatar State: `[DONE]`
  - Centralized station press validation so ready/scoring rules share one path.
  - Added mode settings for Classic 10s, Sprint 5s, Endurance 30s, and Combo.
  - Added combo scoring/pulse behavior, throttled tap sounds, session wins, total taps, best-score result display, and winner-aware avatar expressions.
  - Verified with `dotnet build code/sweeper.csproj`: 0 warnings, 0 errors.
- `2026-05-02` Run 5 Effects, Audio, And Readability: `[DONE]`
  - Added countdown tick sounds, countdown button pulse, round-end flash, stronger winner glow, and central arena key glow.
  - Kept audio behind `TryPlaySound` and existing tap throttling.
  - Verified with `dotnet build code/sweeper.csproj`: 0 warnings, 0 errors.
- `2026-05-02` Run 6 Partial Architecture Split: `[DONE]`
  - Split camera/cursor code into `code/PhysicalFastestTapperGame.Camera.cs`.
  - Split shared generated-object, text, math, and sound helpers into `code/PhysicalFastestTapperGame.Utilities.cs`.
  - Verified with `dotnet build code/sweeper.csproj`: 0 warnings, 0 errors.
- `2026-05-02` Run 7 Architecture And Network Pass: `[DONE]`
  - Split round flow, player/session handling, arena generation, and visuals into dedicated partial files.
  - Added hot-reload recovery and controller-side station validation for physical button clicks.
  - Added `[Rpc.Host]` station press requests, `[Rpc.Broadcast]` press feedback, and synced round/player state containers.
  - Verified `.sbproj` still starts `scenes/minimal.scene`, no Razor gameplay files exist, and `dotnet build code/sweeper.csproj` has 0 warnings and 0 errors.
- `2026-05-02` Run 8 Final Sync And Verification Pass: `[DONE]`
  - Added explicit synced result ordering for networked leaderboard/result display.
  - Added central camera/pressability debug status so station, results, and spectator camera states are visible in-world.
  - Verified `.sbproj` still starts `scenes/minimal.scene`, no Razor gameplay files exist, and `dotnet build code/sweeper.csproj` has 0 warnings and 0 errors.
  - Remaining runtime-only check: manually click-test each camera mode inside s&box.
- `2026-05-02` Run 9 World Building Pass 1: `[DONE]`
  - Added runtime-generated arcade venue backdrop, high sky panel, boundary rails, truss frame, header signage, speaker stacks, light rigs, and crowd silhouettes.
  - Kept decorative world assets collider-free and named stably for hot reload reuse.
  - Verified with `dotnet build code/sweeper.csproj`: 0 warnings, 0 errors.
- `2026-05-02` Run 10 Existing s&box Asset Venue Pass: `[DONE]`
  - Added a stable runtime `Venue Asset Root` and cloned installed base `.vmap` prefabs for the interior shell, rails, billboards, ceiling lights, workshop lights, CCTV, fire extinguishers, and bins.
  - Kept cloned decorative assets collider-free and retained generated geometry as fallback when a base asset cannot be cloned.
  - Verified with `dotnet build code/sweeper.csproj`: 0 warnings, 0 errors.
- `2026-05-02` Run 11 MapInstance Venue Loading Pass: `[DONE]`
  - Replaced failed `.vmap` prefab cloning with the documented `MapInstance` component path.
  - Loads the compiled s&box `core/maps/dev/preview_flat.vpk` map through a stable runtime `Venue Map Instance`.
  - Keeps generated office dressing under `Venue Generated Fallback Root` and hides it when the map instance reports loaded.
  - Verified with `dotnet build code/sweeper.csproj`: 0 warnings, 0 errors.
- `2026-05-02` Run 12 Turbo Refinement Pass: `[DONE]`
  - Removed unreliable runtime map loading from normal play after local/core map paths logged load failures.
  - Kept the generated office venue always active and pushed the ceiling/walls back for cleaner camera composition.
  - Enlarged physical tap buttons, tightened station text, removed camera debug clutter, and added a stronger local-station marker.
  - Verified with `dotnet build code/sweeper.csproj`: 0 warnings, 0 errors.
- `2026-05-02` Run 13 Ultimate Refinement Pass: `[DONE]`
  - Added explicit auto-rotating game modes, per-round summary stats, consecutive win tracking, and mode-specific accent colors.
  - Added low-cost ambient venue motion for ceiling lights, sign glow, light rigs, and crowd silhouettes without adding gameplay colliders.
  - Repositioned and downscaled central arena text for cleaner station-camera framing.
  - Verified with `dotnet build code/sweeper.csproj`: 0 warnings, 0 errors.
- `2026-05-02` Run 14 Full Epic Systems Pass: `[DONE]`
  - Added event phase direction for warmup, ready check, countdown, live play, photo finish, podium, and next-mode preview.
  - Added tournament mode properties, round sequencing, final tie-breaker mode selection, tournament points, placements, focus hits, and synced tournament display data.
  - Added focus-window timing, station callouts, race-trace bars, generated podium geometry, winner lane, and avatar crown/streak effects.
  - Added optional prefab hook properties for future venue, podium, and light-rig assets while keeping generated fallback geometry active.
  - Verified with `dotnet build code/sweeper.csproj`: 0 warnings, 0 errors.

## Milestone 1: Foundation

### Epic F1: Stable Round State Machine

**Intent:** Make the game loop impossible to confuse or break before layering multiplayer and presentation polish on top.

**Tasks**

- `[DONE]` `F1.1` Define the authoritative round states as `WaitingForPlayers`, `Countdown`, `Playing`, `Results`, and `Intermission`.
- `[DONE]` `F1.2` Move all state transitions into explicit methods such as `EnterWaiting`, `EnterCountdown`, `EnterPlaying`, `EnterResults`, and `EnterIntermission`.
- `[DONE]` `F1.3` Ensure every state entry resets only the values it owns.
- `[DONE]` `F1.4` Ensure first valid tap in a round counts as tap `1`.
- `[DONE]` `F1.5` Ensure taps during countdown never score.
- `[DONE]` `F1.6` Ensure taps during results/intermission only mark ready for the next round.
- `[DONE]` `F1.7` Add clear world-space status text for each state.

**Acceptance Criteria**

- A solo player can ready, countdown, play, see results, and start the next round.
- Timer never displays a negative value.
- Round score resets every round; best score persists for the session.
- Repeated Space/Enter/mouse presses during transitions do not corrupt state.

### Epic F2: Clean Game Architecture

**Intent:** Keep the codebase workable as the project grows beyond a prototype.

**Tasks**

- `[DONE]` `F2.1` Split responsibilities conceptually into round manager, station model, player score model, station visuals, avatar visuals, and input forwarding.
- `[DONE]` `F2.2` Keep `PhysicalTapButton` thin: it should only raycast/identify the button and call the game controller with its station index.
- `[DONE]` `F2.3` Keep generated object names stable and unique.
- `[DONE]` `F2.4` Avoid per-frame scene-wide searches except for known bootstrap or fallback paths.
- `[DONE]` `F2.5` Add comments only for non-obvious systems such as networking assumptions and runtime generation.

**Acceptance Criteria**

- Hot reload does not duplicate stations or text.
- Adding a new station does not require copy/paste of gameplay logic.
- Button input code does not know scoring rules.

### Epic F3: Build And Regression Discipline

**Intent:** Every pass should remain playable.

**Tasks**

- `[DONE]` `F3.1` Run `dotnet build code/sweeper.csproj` after every task batch.
- `[DONE]` `F3.2` Treat warnings as follow-up tasks unless they are unavoidable engine warnings.
- `[DONE]` `F3.3` Keep `.sbproj` startup scene pointing at `scenes/minimal.scene`.
- `[DONE]` `F3.4` Avoid destructive scene edits unless a backup or generated recovery path exists.

**Acceptance Criteria**

- Build is green after every milestone.
- Startup scene loads the game manager.
- No Razor gameplay files are reintroduced.

## Milestone 2: Refined Physical World

### Epic W1: Arcade Arena Layout

**Intent:** Make the world feel like a real competitive arcade installation instead of test objects.

**Tasks**

- `[DONE]` `W1.1` Create a central arena floor large enough for 1-8 stations.
- `[DONE]` `W1.2` Place stations in a readable arc or row with consistent spacing.
- `[DONE]` `W1.3` Add a central leaderboard tower visible from all station cameras.
- `[DONE]` `W1.4` Add station floor markings so each player understands their assigned area.
- `[DONE]` `W1.5` Add physical button pedestals with layered button parts.
- `[DONE]` `W1.6` Add avatar head panels near each station.
- `[DONE]` `W1.7` Add score/speed/combo/rank panels per station.

**Acceptance Criteria**

- Default 4-station layout is readable.
- 1-station and 8-station configurations still place objects without overlap.
- Every station has a distinct button, text panel, avatar area, and status line.

### Epic W2: Camera Composition

**Intent:** The game should be instantly readable without player-controlled movement.

**Tasks**

- `[DONE]` `W2.1` Build a station camera mode that frames the local station button, station text, and avatar head.
- `[DONE]` `W2.2` Build a results camera mode that frames the central leaderboard and winning station.
- `[DONE]` `W2.3` Build a spectator camera mode for unassigned/late-joining clients.
- `[DONE]` `W2.4` Adjust field of view and camera position for 16:9 desktop.
- `[DONE]` `W2.5` Verify raycast clicking still works from station camera mode with the visible button, button-top, hitbox, and local mouse fallback paths.

**Acceptance Criteria**

- Local station button is easy to click.
- Text is readable from the assigned camera.
- Results view clearly shows winner and leaderboard.

### Epic W3: Lighting And Readability

**Intent:** Improve mood and clarity with simple, robust lighting.

**Tasks**

- `[DONE]` `W3.1` Keep a stable key light for the whole arena.
- `[DONE]` `W3.2` Add station accent lights that brighten with heat.
- `[DONE]` `W3.3` Add winner spotlight behavior during results.
- `[DONE]` `W3.4` Avoid overbright colors that wash out white text.
- `[DONE]` `W3.5` Keep text in front of dark panels for contrast.

**Acceptance Criteria**

- Button and station state are readable in idle and high-heat states.
- Winner highlight is obvious.
- Avatar head remains visible and not overlit.

### Epic W4: World Building, Skybox, And Asset Direction

**Intent:** Make the tapper arena feel like a complete s&box arcade venue instead of a floating test layout, while keeping the physical gameplay readable and performant.

**Tasks**

- `[DONE]` `W4.1` Define the venue theme: high-energy competitive arcade stage with clear station lanes, central leaderboard focus, and readable player silhouettes.
- `[DONE]` `W4.2` Add a skybox or world backdrop that frames the arena without distracting from buttons, avatar heads, or score panels.
- `[DONE]` `W4.3` Add outer arena boundaries such as walls, rails, truss frames, or stage barriers so the play space feels grounded.
- `[DONE]` `W4.4` Add overhead signage and environmental branding for the fastest-tapper competition.
- `[DONE]` `W4.5` Add decorative but non-interactive props such as cables, speaker stacks, light rigs, floor decals, arcade panels, and crowd/spectator silhouettes.
- `[DONE]` `W4.6` Create asset-selection rules: prefer existing s&box/dev/base models first, generated simple geometry second, and custom imported assets only when they materially improve the scene.
- `[DONE]` `W4.7` Add color and material direction for world assets so gameplay colors remain reserved for button heat, ready state, winner state, and progress.
- `[DONE]` `W4.8` Add background motion or ambient effects only if they do not affect click readability or frame stability.
- `[DONE]` `W4.9` Add scalable layout rules so 1, 2, 4, and 8 station modes still fit inside the world dressing.
- `[DONE]` `W4.10` Add performance guardrails: stable object names, no per-frame spawning, no expensive scene searches, and no dense prop fields near interactive buttons.

**Acceptance Criteria**

- The arena reads as a complete venue from station, results, and spectator camera modes.
- Buttons, avatar heads, leaderboard, status text, and progress/heat bars remain visually dominant.
- The skybox/backdrop improves depth without washing out text or confusing click targets.
- Decorative assets never block raycasts to tap buttons.
- Hot reload does not duplicate world-building objects.
- 1, 2, 4, and 8 station layouts remain readable and non-overlapping.
- `dotnet build code/sweeper.csproj` passes with zero errors after each world-building pass.

### Epic W5: Existing s&box Asset Venue Pass

**Intent:** Use shipped s&box/base map prefabs and props as the venue foundation so the game looks like it belongs in s&box instead of being built only from generated dev boxes.

**Tasks**

- `[DONE]` `W5.1` Add a stable `Venue Asset Root` for all cloned decorative prefabs.
- `[DONE]` `W5.2` Clone existing base environment content for the room/interior shell while keeping `Assets/scenes/minimal.scene` as the startup scene.
- `[DONE]` `W5.3` Replace generated-only venue details with installed base assets such as rails, billboards, ceiling lights, workshop lights, CCTV, fire extinguishers, and bins.
- `[DONE]` `W5.4` Disable cloned decorative colliders so base assets never block tap-button raycasts.
- `[DONE]` `W5.5` Keep generated fallback dressing for missing or incompatible prefabs.
- `[DONE]` `W5.6` Runtime-check the exact prefab positions in s&box and tune scale/rotation if any asset appears too large, clipped, or camera-obstructing. Result: `.vmap` prefab cloning reported `loaded:0 fallback:25`, so the approach was replaced by W5.7.
- `[DONE]` `W5.7` Use `MapInstance` for compiled map loading instead of `GameObject.Clone`.
- `[DONE]` `W5.8` Runtime-check `MapInstance.IsLoaded` in s&box and tune the arena/map transform if the map loads but frames poorly. Result: `MapInstance` reported misleading loaded state while logs showed map failures, so runtime map loading was removed from normal play.
- `[DONE]` `W5.9` Promote the generated office venue to the active world-building path until a compiled project/workshop map is available.

**Acceptance Criteria**

- The venue uses existing installed s&box/base assets before generated placeholders.
- Startup still uses `Assets/scenes/minimal.scene`; no scene JSON is hand-edited.
- Decorative prefab clones do not duplicate on hot reload.
- Buttons, avatar heads, leaderboard, and station text remain unobstructed.
- `dotnet build code/sweeper.csproj` passes with zero errors.
- `.vmap` environment content is loaded through `MapInstance`, not prefab cloning.

## Milestone 3: Multiplayer

### Epic M1: Connection And Station Assignment

**Intent:** Every connected player gets one station and cannot interfere with another station.

**Tasks**

- `[DONE]` `M1.1` Use `Component.INetworkListener.OnActive(Connection)` to assign new connections.
- `[DONE]` `M1.2` Keep a stable mapping from connection ID to station index.
- `[DONE]` `M1.3` Assign late joiners to open stations if not currently playing.
- `[DONE]` `M1.4` Put late joiners in spectator/waiting state if the round is active.
- `[DONE]` `M1.5` Free or mark stations when players disconnect.
- `[DONE]` `M1.6` Keep a local fallback player for editor/single-player testing.

**Acceptance Criteria**

- Two connected players get different stations.
- Empty stations display `OPEN STATION` or equivalent.
- Local single-player still works without a lobby.

### Epic M2: Server-Authoritative Taps

**Intent:** Multiplayer scoring must be fair and consistent.

**Tasks**

- `[DONE]` `M2.1` Add a client-to-host tap request path for assigned station input.
- `[DONE]` `M2.2` Validate tap requests on host: correct station, active round, assigned player.
- `[DONE]` `M2.3` Reject taps during countdown/results/intermission except ready actions.
- `[DONE]` `M2.4` Keep score, combo, heat, speed, and peak speed calculated on host.
- `[DONE]` `M2.5` Broadcast updated player score state to all clients.
- `[DONE]` `M2.6` Keep local fallback path identical to server logic.

**Acceptance Criteria**

- Client cannot score another player’s station.
- Two clients see the same score/result ordering.
- Late joiners cannot affect an active round.

### Epic M3: Networked Round State

**Intent:** Every client sees the same round timing and results.

**Tasks**

- `[DONE]` `M3.1` Sync round state.
- `[DONE]` `M3.2` Sync countdown timer and gameplay timer from host.
- `[DONE]` `M3.3` Sync per-player ready state.
- `[DONE]` `M3.4` Sync results ordering and winner station.
- `[DONE]` `M3.5` Make clients treat synced state as display-only except for local input requests.

**Acceptance Criteria**

- Countdown reaches zero for all clients at the same time.
- Results match on every client.
- Intermission starts and ends consistently.

## Milestone 4: Game Feel

### Epic G1: Button Feedback

**Intent:** Tapping should feel physical and satisfying.

**Tasks**

- `[DONE]` `G1.1` Depress the button top on every valid tap.
- `[DONE]` `G1.2` Add scale punch to the button body.
- `[DONE]` `G1.3` Add heat-based color ramp from red to yellow/white.
- `[DONE]` `G1.4` Add cooldown return animation.
- `[DONE]` `G1.5` Add stronger pulse for combo milestones.

**Acceptance Criteria**

- Every valid tap gives visible feedback.
- Button feedback never blocks click raycasts.
- High-speed play feels more intense than low-speed play.

### Epic G2: Effects

**Intent:** Communicate speed and round state through physical effects.

**Tasks**

- `[DONE]` `G2.1` Add station sparks that appear only at meaningful heat.
- `[DONE]` `G2.2` Add heat bar fill and color ramp.
- `[DONE]` `G2.3` Add progress bar drain.
- `[DONE]` `G2.4` Add countdown pulse.
- `[DONE]` `G2.5` Add round-end flash.
- `[DONE]` `G2.6` Add winner spotlight/celebration effect.

**Acceptance Criteria**

- Effects are readable but do not obscure the button or text.
- Effects scale safely with 4-8 stations.

### Epic G3: Audio

**Intent:** Add satisfying sound without making the project fragile.

**Tasks**

- `[DONE]` `G3.1` Add safe wrapper for all `Sound.Play(...)` calls.
- `[DONE]` `G3.2` Add tap sound.
- `[DONE]` `G3.3` Add ready sound.
- `[DONE]` `G3.4` Add countdown tick sound.
- `[DONE]` `G3.5` Add round start/end sounds.
- `[DONE]` `G3.6` Add combo milestone and winner sounds.

**Acceptance Criteria**

- Missing sound names never crash the game.
- Repeated taps do not produce painfully loud overlapping audio.

## Milestone 5: Avatar Head Experience

### Epic A1: Head-Only Avatar Rendering

**Intent:** Show the player’s s&box character face, not Steam avatar and not a full body.

**Tasks**

- `[DONE]` `A1.1` Use `ClothingContainer.CreateFromLocalUser()`.
- `[DONE]` `A1.2` Use `SkinnedModelRenderer` and `Dresser`.
- `[DONE]` `A1.3` Hide bodygroups `Chest`, `Legs`, `Hands`, and `Feet`.
- `[DONE]` `A1.4` Keep `Head` enabled.
- `[DONE]` `A1.5` Reapply head-only bodygroups after dressing and during updates.

**Acceptance Criteria**

- Avatar display is a head/face.
- No Steam profile image is used.
- Head remains visible after hot reload.

### Epic A2: Expression System

**Intent:** Avatar face should react to player performance.

**Tasks**

- `[DONE]` `A2.1` Neutral expression while waiting.
- `[DONE]` `A2.2` Focused expression during countdown.
- `[DONE]` `A2.3` Smile increases with heat.
- `[DONE]` `A2.4` Squint increases at high heat.
- `[DONE]` `A2.5` Open mouth/jaw activates at very high heat.
- `[DONE]` `A2.6` Winner gets celebratory expression.
- `[DONE]` `A2.7` Non-winners settle into relaxed/tired expression.

**Acceptance Criteria**

- Expression changes are visible from gameplay camera.
- Expression is driven by player-specific heat, not global heat.

## Milestone 6: Results And Replayability

### Epic R1: Results Screen

**Intent:** Round end should clearly tell everyone who won and why.

**Tasks**

- `[DONE]` `R1.1` Sort by score descending.
- `[DONE]` `R1.2` Tie-break by peak taps/sec.
- `[DONE]` `R1.3` Show name, taps, peak speed, max combo, and rank.
- `[DONE]` `R1.4` Highlight winner station.
- `[DONE]` `R1.5` Show personal best updates.

**Acceptance Criteria**

- Results ordering is deterministic.
- Winner is obvious from both station view and results view.

### Epic R2: Replay Loop

**Intent:** Players should naturally continue into the next round.

**Tasks**

- `[DONE]` `R2.1` Let players press their station button to ready during intermission.
- `[DONE]` `R2.2` Start next countdown when all active players are ready.
- `[DONE]` `R2.3` Start automatically when intermission timer expires.
- `[DONE]` `R2.4` Clear round-only values cleanly before countdown.

**Acceptance Criteria**

- No manual reset is needed between rounds.
- Players understand how to start the next round.

## Milestone 7: Game Modes

### Epic GM1: Mode Framework

**Intent:** Add variety without duplicating the game loop.

**Tasks**

- `[DONE]` `GM1.1` Add mode enum: `Classic10`, `Sprint5`, `Endurance30`, `Combo`.
- `[DONE]` `GM1.2` Define duration per mode.
- `[DONE]` `GM1.3` Define heat gain/decay per mode.
- `[DONE]` `GM1.4` Define scoring modifier per mode.
- `[DONE]` `GM1.5` Show active mode in central display.

**Acceptance Criteria**

- Switching mode changes rules without breaking station visuals.

### Epic GM2: Individual Modes

**Tasks**

- `[DONE]` `GM2.1` Classic 10s: current baseline.
- `[DONE]` `GM2.2` Sprint 5s: faster heat gain, shorter timer.
- `[DONE]` `GM2.3` Endurance 30s: longer timer, stronger decay.
- `[DONE]` `GM2.4` Combo Mode: rewards consistent fast intervals.

**Acceptance Criteria**

- Every mode produces correct results and ranking.

## Milestone 8: Persistence And Release

### Epic P1: Session Stats

**Tasks**

- `[DONE]` `P1.1` Track session wins.
- `[DONE]` `P1.2` Track session total taps.
- `[DONE]` `P1.3` Track best score per mode.
- `[DONE]` `P1.4` Track best peak speed.

**Acceptance Criteria**

- Stats survive across rounds in one session.

### Epic P2: Persistent Leaderboards

**Tasks**

- `P2.1` Enable leaderboard project metadata after local scoring is stable.
- `P2.2` Submit only valid completed rounds.
- `P2.3` Submit Classic score first.
- `P2.4` Add visible leaderboard status/failure handling.

**Acceptance Criteria**

- Invalid/unfinished rounds never submit.
- Game still works offline or if leaderboard service fails.

## Milestone 9: Event-Grade Arcade Experience

### Epic E1: Event Director

**Intent:** Make each round feel like a hosted arcade event with clear phase changes and dramatic presentation.

**Tasks**

- `[DONE]` `E1.1` Add event phases for warmup, ready check, countdown, live play, photo finish, podium, and next-mode preview.
- `[DONE]` `E1.2` Drive central board headline text from the event phase instead of only raw round state.
- `[DONE]` `E1.3` Add photo-finish callouts for the final seconds of active play.
- `[DONE]` `E1.4` Add winner headline text during podium/results.

**Acceptance Criteria**

- The central board communicates the current event beat.
- The final seconds are visually distinct from normal play.
- Results read as a podium moment, not only a timer stop.

### Epic T1: Tournament Progression

**Intent:** Give the session a larger arc beyond one-off rounds.

**Tasks**

- `[DONE]` `T1.1` Add `TournamentMode`, `TournamentRounds`, and `UseFinalTieBreaker` properties.
- `[DONE]` `T1.2` Add deterministic tournament mode sequence with final tie-breaker support.
- `[DONE]` `T1.3` Award tournament points by placement.
- `[DONE]` `T1.4` Show tournament points in leaderboard rows and station summaries.
- `[DONE]` `T1.5` Sync tournament round, points, and event phase for clients.

**Acceptance Criteria**

- Multiple rounds create a session standings arc.
- Tournament mode remains host-authoritative.
- Single-player editor flow still works with tournament mode enabled.

### Epic S1: Skill Pressure Mechanics

**Intent:** Add a rhythm layer without making clicking less important.

**Tasks**

- `[DONE]` `S1.1` Add focus-window timing per station during active play.
- `[DONE]` `S1.2` Reward focus hits with combo protection and extra heat.
- `[DONE]` `S1.3` Add `FOCUS`, `CHAIN`, `OVERHEAT`, and `PHOTO FINISH` station callouts.
- `[DONE]` `S1.4` Add focus ring visuals around the physical button area.

**Acceptance Criteria**

- Focus timing is a bonus, never a penalty.
- Fast tapping remains the main scoring action.
- Focus visuals do not block button clicks.

### Epic V1: Venue And Podium Expansion

**Intent:** Push the generated venue toward a complete arcade competition space.

**Tasks**

- `[DONE]` `V1.1` Add generated podium geometry.
- `[DONE]` `V1.2` Add winner lane lighting.
- `[DONE]` `V1.3` Add station race-trace bars for round progress/results.
- `[DONE]` `V1.4` Add winner crown and streak crown behavior above avatar heads.
- `[DONE]` `V1.5` Add optional future prefab hooks for venue prop, podium, and light rig assets.

**Acceptance Criteria**

- The arena has a visible results/podium destination.
- Winner and streak state are readable from station/results cameras.
- Missing prefabs do not break the generated fallback venue.

### Epic P3: Final QA

**Tasks**

- `P3.1` Test single-player editor flow.
- `P3.2` Test two-player server flow.
- `P3.3` Test late join.
- `P3.4` Test disconnect.
- `P3.5` Test hot reload.
- `P3.6` Test 1, 2, 4, and 8 station layouts.
- `[DONE]` `P3.7` Verify no Razor gameplay UI exists.
- `[DONE]` `P3.8` Verify build has zero errors and zero warnings.

**Acceptance Criteria**

- Game is playable from startup scene.
- No scene interop exceptions.
- Multiplayer result state is stable.
- Visuals remain readable across station counts.

## Recommended Execution Order

1. `F1`, `F2`, `F3`
2. `W1`, `W2`, `W3`, `W4`, `W5`
3. `M1`, `M2`, `M3`
4. `G1`, `G2`, `G3`
5. `A1`, `A2`
6. `R1`, `R2`
7. `GM1`, `GM2`
8. `P1`, `P2`, `P3`

## Definition Of Done For Every Pass

- Build command succeeds:
  - `dotnet build E:\SteamLibrary\steamapps\common\sbox\samples\sweeper\code\sweeper.csproj`
- Startup scene still loads:
  - `Assets/scenes/minimal.scene`
- No unrelated files are reformatted.
- No duplicate generated objects after hot reload.
- Gameplay remains testable with one local player.
