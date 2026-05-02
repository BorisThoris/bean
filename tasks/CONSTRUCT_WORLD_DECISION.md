# Construct World Decision

Date: 2026-05-02

The physical tapper game should use the official Construct map as its preferred world backdrop. The generated box-room/office shell is fallback-only.

## Decision

- Preferred map package: `facepunch.construct`
- Runtime API: `Sandbox.SceneMap`
- Startup scene remains `Assets/scenes/minimal.scene`
- Gameplay objects remain generated physical objects: stations, buttons, central board, avatar heads, podium, and effects
- Construct provides the world and visual context, not a free-roam sandbox mode
- `UseConstructWorld` defaults to `true`, and map loading happens in component `OnLoad` using `SceneMap.CreateAsync` so s&box can keep the loading screen up until map load completes.

## Fallback

If Construct is disabled, not mounted, or fails to load, `OnLoad` catches the failure and the game keeps running with the generated venue fallback. The central board reports the fallback state so the failure is visible during QA without crashing the scene.

Construct diagnostics must never read `SceneMap` metadata directly. Use the null-safe helpers on `PhysicalFastestTapperGame` so logging cannot create a null-reference exception while reporting a map-load failure.

## Visual Target

The first view should read as a staged arcade/game-show setup placed inside Construct. The tapper platform must stay readable and clickable even if Construct loads a busy environment behind it.

## Verification

- Build passes with `dotnet build code\sweeper.csproj`.
- Scene loads without interop/GUID exceptions.
- Central board shows `CONSTRUCT` when the map loads.
- Central board shows `CONSTRUCT FALLBACK` when the map is unavailable.
- Station, spectator, and results cameras frame both gameplay objects and the map backdrop.
