# Data / Content Pipeline

EcoBalance is data-driven. Species and resources are **ScriptableObject assets**, not hardcoded values. Every team member must follow the rules below.

## Where code lives

- **Simulation core** (data types, relationships, simulator, registry): `packages/ecosim/Runtime/`, tests in `packages/ecosim/Tests/Editor/`. It must not reference app code.
- **Everything app-specific** (UI, island, camera, spawners, animal brains, audio): `apps/mobile/EcoBalance3D/Assets/_Project/Scripts/`.

## Folder structure (`apps/mobile/EcoBalance3D/Assets/_Project/`)

| Folder | Contents |
|---|---|
| `Scripts/<System>/` | App C# code grouped by system (`EcoSim` clock/brains, `Behaviourtreecore`, `Selection`, `Inspection`, `Creatures`, `UI`) |
| `ScriptableObjects/Species/` | `SpeciesDefinition` assets (e.g. `BlueSheep`, `SnowLeopard`) |
| `ScriptableObjects/Resources/` | `ResourceDefinition` assets (e.g. `Grass`, `Water`) |
| `Data/` | Config assets for other systems (Inspection, Selection, SimulationData) |
| `Prefabs/Creatures/` | Animal prefabs referenced by a species' `visualPrefab` |
| `Prefabs/UI/` | UI prefabs |
| `Art/`, `Audio/` | Imported models, textures, materials and sounds |
| `Scenes/` | Game scenes. Personal test scenes are named `<Initials>_Test_<Topic>` |
| `Tests/Editor/` | App EditMode tests (core tests are in the package) |

Nothing project-specific goes outside `_Project/`. Third-party and imported packages stay in their own folders.

## Naming conventions

- **Assets:** PascalCase, no spaces: `BlueSheep.asset`, `SnowLeopard.prefab`, `Grass.asset`.
- **Scripts:** the file name matches the class name. Interfaces are prefixed with `I` (`ISelectable`).
- **Textures and materials:** `T_<Name>_<Map>` (e.g. `T_Sheep_Albedo`) and `M_<Name>`.
- **Branches:** `<name>` or `feature/<short-topic>`, merged into `main` by pull request.

## Adding a new species (no code needed)

1. Right-click in `ScriptableObjects/Species/` and choose **Create → EcoSim → Species**.
2. Fill in `speciesName`, `eats` (prey species), `needs` (resources), the rates and `visualPrefab`.
3. Add the asset to the `Species` list on the scene's `EcosystemSimulator`.

## Import / export

- **Models:** `.fbx`, with Scale Factor 1, +Y up, and Read/Write off unless needed.
- **Textures:** `.png`, power-of-two sizes, with ASTC compression for Android.
- **Audio:** `.wav` source files. Unity compresses them to Vorbis.
- **Export:** simulation metrics go out as CSV (T05).

## Validation rules

- `EcosystemSimulator` checks its configuration before each tick. Empty entries in Species, Resources or `eats` are removed and a warning is logged, instead of crashing the simulation.
- Rates (`reproductionRate`, `predationRate`, `starvationThreshold`) must be ≥ 0, and `currentAmount` must be ≤ `maxAmount`.
- EditMode tests (package + app) run in CI on every PR. A PR must not be merged while tests are failing.

---
*Drafted with AI assistance: Claude Code (Claude Opus 5.5, claude-opus-5-5). Reviewed and edited by Tan Wei Leong.*
