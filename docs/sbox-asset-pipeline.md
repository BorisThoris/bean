# Runtime World Asset Pipeline

Bean Tapper generates its game world at runtime from ready model assets and code-defined placement.

## Current Rule

- Game-world geometry must be placed by runtime code, not hand-authored into scene JSON.
- World props, stations, bars, rails, room panels, lights, signs, and effects should use approved ready assets under `Assets/models/quaternius/`.
- Do not add built-in dev box/sphere model paths, generated AI mesh assets, AI source asset folders, or unapproved imported mesh ModelDoc nodes for world assets.
- Player avatar/citizen models are allowed because they are character assets, not generated world geometry.

## Validation

Run from the repo root:

```powershell
node scripts\validate-sbox-assets.mjs
```

This fails if generated AI asset folders, AI generation scripts, primitive dev model references, unapproved imported world model references, AI source references, or imported ModelDoc mesh references are present.
