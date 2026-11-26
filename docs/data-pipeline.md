# Data / Content Pipeline

EcoBalance is data-driven. Species and resources are **ScriptableObject assets**, not hardcoded values. Every team member must follow the rules below.

## Where code lives

- **Simulation core** (data types, relationships, simulator, registry): `packages/ecosim/Runtime/`, tests in `packages/ecosim/Tests/Editor/`. It must not reference app code.
- **Everything app-specific** (UI, island, camera, spawners, animal brains, audio): `apps/mobile/EcoBalance3D/Assets/EcoBalance/Scripts/`.

## Folder structure (`apps/mobile/EcoBalance3D/Assets/EcoBalance/`)

| Folder | Contents |
|---|---|
| `Scripts/<System>/` | App C# code grouped by system (`EcoSim` clock/brains, `Behaviourtreecore`, `Selection`, `Inspection`, `Creatures`, `UI`) |
| `ScriptableObjects/Species/` | `SpeciesDefinition` assets (e.g. `BlueSheep`, `SnowLeopard`) |
| `ScriptableObjects/Resources/` | `ResourceDefinition` assets (e.g. `Grass`, `Water`) |
| `Data/` | Config assets for other systems (Inspection, Selection, SimulationData) |
| `Prefabs/Creatures/` | Animal prefabs referenced by a species' `visualPrefab` |
| `Prefabs/UI/` | UI prefabs |
| `Audio/` (and `Art/` when models/textures are added) | Imported sounds, models, textures, materials |
| `Tests/Editor/` | App EditMode tests (core tests are in the package) |

Scenes live in `Assets/Scenes/` (referenced by Build Settings); personal test scenes are named `<Initials>_Test_<Topic>`. Nothing else project-specific goes outside `EcoBalance/`. Third-party and imported packages stay in their own folders.

## Naming conventions

- **Assets:** new assets use PascalCase with no spaces: `BlueSheep.asset`, `Grass.asset`. Some imported files (e.g. `blue sheep.prefab`, `snow leopard.fbx`, audio clips) still have spaces; they will be renamed inside the Editor after M1 so references are kept.
- **Scripts:** the file name matches the class name. Interfaces are prefixed with `I` (`ISelectable`).
- **Textures and materials:** `T_<Name>_<Map>` (e.g. `T_Sheep_Albedo`) and `M_<Name>`.
- **Branches:** `<name>` or `feature/<short-topic>`, merged into `main` by pull request.

## Adding a new species (no code needed)

1. Open the **Animal Creator** window (**EcoBalance → Open Animal Creator**), or right-click in `ScriptableObjects/Species/` and choose **Create → EcoSim → Species**.
2. Fill in `speciesName`, the rates, `startingPopulation` and `visualPrefab`.
3. In **Relationships**, add one entry per link: type (**Eats / Needs / Affects**) and target (a species or resource asset), e.g. *Eats → BlueSheep*.
4. Add the asset to the `Species` list on the scene's `EcosystemSimulator`.

## Import / export

- **Models:** `.fbx`, with Scale Factor 1, +Y up, and Read/Write off unless needed.
- **Textures:** `.png`, power-of-two sizes, with ASTC compression for Android.
- **Audio:** `.wav` source files. Unity compresses them to Vorbis.
- **Storage:** binary files (`.png .jpg .jpeg .psd .tga .exr .tif .fbx .obj .glb .gltf .blend .wav .mp3 .ogg .mp4 .ttf .otf .raw .r16`) go through **Git LFS** to our Gitea server automatically (rules in `.gitattributes`). To add a new binary type, add it to `.gitattributes` **before** committing the first file.
- **Export:** simulation metrics go out as CSV (T05).

## Investigation cases (Field Journal, M2)

- **Create:** right-click in `Assets/EcoBalance/Resources/Cases/` → **Create → EcoSim → Investigation Case**. Name files `Case_NN_ShortName`.
- **Fields:** `caseId` (stable, never change it after release), `title`, `hint` (shown while locked), `explanation` (shown once unlocked), `earliestDay`, and `conditions` (all must be true on the same day).
- **Condition types:** `AtMost`, `AtLeast`, `ReachedZero`, `DroppedByFraction` / `RoseByFraction` (0.5 = 50%, compared with `windowDays` ago), `RatioAtLeast` (subject ÷ other). `subject` and `other` are species or resource assets.
- **Check:** `CaseAssetTests` fails if a case has no title, hint or conditions, a duplicate id, or a subject that isn't a species or resource. In Play mode the dev overlay (backquote key) shows each case's status.

## Validation rules

- `EcosystemSimulator` checks its configuration on reset and before each tick. Empty entries in Species, Resources or a species' Relationships are removed and a warning is logged, instead of crashing the simulation.
- Rates (`reproductionRate`, `predationRate`, `starvationThreshold`) must be ≥ 0, and `currentAmount` must be ≤ `maxAmount`.
- EditMode tests (package + app) and repo checks run in CI on every PR. A PR must not be merged while they are failing.
- **Repo checks** (`python tools/ci/repo-checks.py`; run it locally before a PR): every asset has a `.meta`, no orphan `.meta`, no file over 5 MB stored outside LFS, no merge-conflict markers, no `using UnityEditor` in runtime code.


## Terrain data (M2)

- **Heights:** AWS Terrain Tiles (Terrarium PNG, zoom 11), downloaded at runtime and cached in `persistentDataPath/HimalayaCache`; a copy of the 16 tiles and the OpenStreetMap water for the M3 box ships in `Resources/HimalayaData`, so the world builds with no internet and no download wait (26 Nov). Only the live weather, GBIF sightings and Wikidata facts need a connection, and the game runs without them. The M3 map box is the Khumbu valleys, 86.595–87.095°E, 27.64–28.14°N (set in `Terrain/HimalayaConfig.asset`); real heights squeezed into a 900-unit game map (about 6x the M2 area).
- **Habitats:** blue sheep 4,000–5,200 m, snow leopard 4,200–5,500 m, tree line 4,200 m (game uses 4,800 m so the forest-to-alpine change is visible), snow line 5,200 m (from the animal research). Trees: ESA WorldCover tree-cover class + tree line + slope, using UXGD's Pine / Dead Tree models.
- **Water:** the main river and glacier streams are traced from the heights at runtime (`HimalayaRiver`); OpenStreetMap water is only used when the traced river is switched off.

---
*Drafted with AI assistance: Claude Code (Claude Opus 5.5, claude-opus-5-5). Reviewed and edited by Tan Wei Leong.*
