# React Wall Screen Concept

## Current Recommendation

Keep the arena wall screen on s&box Razor for the production path. The current wall is a `PanelComponent` rendered through `WorldPanel`, which gives direct access to C# game state without a JavaScript build pipeline.

## Why React Is Not A Drop-In

The Razor wall screen is not a normal browser DOM. React cannot mount inside `ArenaWallScreen.razor` the way it would in a web page. Actual React would need a browser-backed surface, most likely `Sandbox.UI.WebPanel`, which the engine describes as an interactive web page panel with access to an HTML surface.

## Option A: Razor Components With React-Like Structure

Split the current wall into smaller Razor components while keeping the same runtime path:

- `ArenaWallHeader`: title, debug badge, mode summary.
- `ArenaWallMain`: headline and leaderboard.
- `ArenaWallStationGrid`: station rows and status styling.

This keeps the stable `WorldPanel` path, avoids npm/Vite, and still gives a cleaner component model.

## Option B: Real React Prototype

Build a separate local React app and render it on a web surface:

- Create a small bundled React app under a future `web/wall-screen/` folder.
- Build static `index.html`, JavaScript, and CSS assets into a local package path.
- Replace or layer the current Razor wall with a `WebPanel`.
- Feed C# state into the web app as a compact wall-screen DTO:
  - title
  - headline
  - mode summary
  - leaderboard text or rows
  - station rows with station, name, status, meta, and CSS class
- Keep the web surface non-interactive unless player interaction is explicitly needed.

## State Shape

```json
{
  "title": "TAPPER ARENA",
  "headline": "READY UP",
  "mode": "CLASSIC | ROUND 1/4 | 2 CLAIMED | GENERATED VENUE",
  "leaderboard": "1. Player 24 taps 6.1/s 3 pts",
  "stations": [
    {
      "station": "S1",
      "name": "Player",
      "status": "READY",
      "meta": "24 taps 6.1/s",
      "className": "ready"
    }
  ]
}
```

## Prototype Acceptance

- The existing Razor wall remains the fallback production UI.
- The React build is local and does not load from a CDN.
- The wall still renders in world space, left-to-right, with fallback text hidden when the primary UI is valid.
- Build and test gates remain `dotnet build code\sweeper.csproj` and `dotnet test code\unittest\sweeper.tests.csproj`.
