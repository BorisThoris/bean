# Bean Tapper

Bean Tapper is a physical s&box arcade game built from the stock `sweeper` sample. Players walk around as s&box citizen/bean characters, claim a platform by pressing that platform's physical button, then race to tap their locked button faster than everyone else.

This repository is kept as a portfolio project for multiplayer gameplay systems, physical world-space UI, runtime arena generation, host-authoritative station ownership, and C# gameplay architecture inside s&box.

The project currently uses world-space objects and `TextRenderer` UI only. There are no Razor gameplay pages.

## Current Gameplay

- Loads `Assets/test.scene` as the startup scene.
- Builds a minimal tapper arena at runtime, using Quaternius CC0 models for the tiled floor, wall bays, and gameplay stations.
- Construct loading remains optional, but the default world is the generated runtime room.
- Spawns each connection as a visible third-person citizen/bean.
- New players start unclaimed and can walk to a platform.
- Pressing an open platform button claims that station.
- A claimed player can only ready/tap their own station.
- Claimed players cannot use other players' buttons.
- Supports Classic, Sprint, Endurance, and Combo scoring modes with tournament rotation.
- Keeps a physical scoreboard, station status text, heat/progress bars, and button punch feedback.

## Controls

- `WASD`: move the local bean.
- `Shift`: sprint.
- `Mouse`: aim/select physical station buttons.
- `Mouse1`: click the button under the cursor.
- `Space` / `Enter`: fallback press for the local claimed station.

## Project Layout

- `.sbproj`: s&box game metadata. Startup scene is `test.scene`.
- `Assets/scenes/minimal.scene`: main scene.
- `code/PhysicalFastestTapperGame*.cs`: main tapper game controller split by subsystem.
- `code/TapperPlayerBean.cs`: third-person bean movement and citizen animation.
- `code/PhysicalTapButton.cs`: physical button click forwarding.
- `code/TapperStationInteractionRules.cs`: pure claim/tap ownership rules.
- `code/unittest/`: MSTest coverage for diagnostics and station ownership rules.
- `docs/sbox-asset-pipeline.md`: documented runtime world generation rules.
- `docs/quaternius-assets.md`: third-party CC0 asset source and import notes.
- `tasks/`: active epic/task inventory for long-running refinement.
- `scripts/`: task inventory validation and helper scripts.

## Build And Test

Run from the repository root:

```powershell
dotnet build code\sweeper.csproj
dotnet test code\unittest\sweeper.tests.csproj
node scripts\validate-sbox-assets.mjs
node scripts\validate-task-inventory.mjs
```

Optional task inventory regeneration:

```powershell
node scripts\list-open-tasks.mjs > tasks\OPEN_TASKS_INVENTORY.md
```

## Development Notes

- Prefer runtime-generated objects over hand-editing scene JSON. Earlier scene serialization work hit interop/GUID failures.
- Keep gameplay physical and in-world.
- Keep station ownership host-authoritative where possible.
- Build world/static geometry at runtime. Quaternius CC0 models under `Assets/models/quaternius/` are the approved imported visual asset set; do not add generated AI mesh assets.
- If Construct loading fails, capture the `[TapperConstruct]` log line before changing map-loading code.
- Build and task validation should pass before pushing.

## Current Verification

Last known passing checks:

- `dotnet build code\sweeper.csproj`
- `dotnet test code\unittest\sweeper.tests.csproj`
- `node scripts\validate-sbox-assets.mjs`
- `node scripts\validate-task-inventory.mjs`

## Repository

GitHub: https://github.com/BorisThoris/sbox-bean-tapper-arcade
