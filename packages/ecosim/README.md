# EcoSim (`com.99bugs.ecosim`)

The EcoBalance simulation core as a local Unity package. It has **no dependency on the app**: the app
(`apps/mobile/EcoBalance3D`) depends on this package, never the other way round.

| Folder | Contents |
|---|---|
| `Runtime/` | `SpeciesDefinition`, `ResourceDefinition`, `Relationship` (Eats / Needs / Affects), `EcosystemSimulator` (tick-based populations), `EcoRegistry` + `EcoEntity` (find nearest food / prey / predator; species with 32+ instances use an 8 m spatial hash grid instead of a full scan), `IEcosystemReset`, and the AI data types in `Definitions/` (`NeedDefinition`, `GoalDefinition`, `ActionDefinition`) |
| `Tests/Editor/` | EditMode tests for the simulator, relationships and registry |

## How the app uses it
`apps/mobile/EcoBalance3D/Packages/manifest.json`:
```json
"com.99bugs.ecosim": "file:../../../../packages/ecosim"
```
plus `"testables": ["com.99bugs.ecosim"]` so its tests appear in the Test Runner and run in CI.

The `EcoSim` assembly is auto-referenced, so app scripts use these types directly (no `using` needed).
Moving scripts here kept their `.meta` GUIDs, so existing assets (BlueSheep, SnowLeopard, Grass, scenes) still resolve.

## Rules
- Nothing in `Runtime/` may reference app code (UI, island, spawners, scenes). If the core needs something
  from the app, define an interface here and implement it in the app (see `IEcosystemReset`).
- Every change here needs a test in `Tests/Editor/`.
