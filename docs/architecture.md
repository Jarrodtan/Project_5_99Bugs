# EcoBalance — System Architecture (v0.2)

**Owner of this document:** Tan Wei Leong (Tech Lead)
**Status:** v0.2 for Milestone 2 (v0.1 was Milestone 1). It describes what is built and what is planned. Anything planned is marked *(planned)*.

See also: [toolchain.md](toolchain.md) (technology choices), [data-pipeline.md](data-pipeline.md) (asset/data rules) and [deliverables.md](deliverables.md) (M1/M2/M3 targets).

## 1. Tech stack (summary)

| | |
|---|---|
| Engine | Unity **6000.6.0f1** (Unity 6), URP |
| Language | C# |
| Target platform | Android tablet |
| Source control / CI | GitHub + GitHub Actions (GameCI): repo checks + EditMode tests (unit, scene smoke, performance) on PRs into `main` and nightly on the integration branch (`M2-Integration` during M2); `v*` tag / manual Android build, APK stored on team Gitea ([CI details](infra/ci.md)) |
| Project management | Jira (SCRUM project) |

## 2. Architecture diagram

Split into three small diagrams so each stays readable on GitHub.

### 2.1 Overview — main blocks

```mermaid
flowchart LR
    DATA["Data assets<br/>Species, Resources"]
    CORE["Simulation core<br/>packages/ecosim"]
    APP["Unity app<br/>apps/mobile/EcoBalance3D"]
    ANIM["Procedural animation<br/>packages/procanim"]
    CI["Tests + CI<br/>GitHub Actions"]

    DATA -- "read by" --> CORE
    CORE -- "populations" --> APP
    APP -- "Tick / Reset" --> CORE
    APP -- "motion state" --> ANIM
    CI -- "tests + APK" --> CORE
    CI -- "builds" --> APP
```

The core never depends on the app (arrows into `packages/ecosim` are calls through its public API; see section 7).
The two packages don't know about each other: the app's `AnimalMotionBridge` is the only link between the AI and the animation.

### 2.2 Simulation loop — every tick

```mermaid
flowchart TB
    BTN["Speed buttons<br/>Play / Pause / Reset, x1-x8"]
    CLK["SimulationClock"]
    ECO["EcosystemSimulator<br/>Formula: regrow, graze, predation, growth<br/>Events: regrow only"]
    POP["PopulationDisplay<br/>on-screen counts"]
    SPN["CreatureSpawner<br/>3D animals"]
    BRN["Animals (AnimalBrain)<br/>eat, catch, starve"]

    BTN --> CLK
    CLK -- "1. Tick()" --> ECO
    CLK -- "2. Refresh()" --> POP
    CLK -- "3. RefreshVisuals()" --> SPN
    ECO -- "populations" --> POP
    ECO -- "populations" --> SPN
    SPN --> BRN
    BRN -. "Events mode: caught / starved -1" .-> ECO
```

The simulator has two modes (`EcosystemSimulator.mode`):

| Mode | How populations change | Used by |
|---|---|---|
| **Formula** (default) | Each `Tick()`: predation = rate × predators × prey; grazers share grass; well-fed species grow, underfed ones starve. Deterministic. | EditMode tests, balancing |
| **Events** | Only from what the 3D animals do: a leopard catch → sheep −1; an animal whose HP reaches 0 → −1; a male and female pair breeding → +1. Rules follow the design research (Simulation Logic): Fullness / Hydration −20 (sheep) / −12 (leopard) per day, look for food / water at 50% or less, 100 HP lost at 20 per day per empty need, +40 graze / +60 drink / +80 catch, hunt at ≤50% with 70% success and a 1-day rest after a miss, breed at ≥70% Fullness every 5 (sheep) / 12 (leopard) days per pair. `Tick()` only regrows grass and water. | HimalayaSimulation (Free and Guided) |

In both modes the numbers live in the simulator and `CreatureSpawner` keeps one model per animal; the animal that was caught or starved is the one removed.

### 2.3 Animal AI — one animal

```mermaid
flowchart TB
    SPN["CreatureSpawner"]
    BRN["Animal brain<br/>picks most urgent goal"]
    ACT["Actions<br/>Wander, Seek, Flee, Hunt, Idle"]
    REG["EcoRegistry<br/>nearest prey / predator<br/>spatial hash grid"]
    PATCH["ResourcePatch<br/>grass, water"]
    BRG["AnimalMotionBridge<br/>action to motion state"]
    PA["ProcAnimator<br/>packages/procanim"]

    SPN -- "spawns + registers" --> REG
    SPN --> BRN
    BRN --> ACT
    ACT -- "Flee / Hunt" --> REG
    ACT -- "Seek" --> PATCH
    BRN --> BRG
    BRG -- "alert / stalk check" --> REG
    BRG -- "SetState, look target" --> PA
```

Solid box `packages/ecosim` = reusable core; everything in `apps/mobile` depends on it, never the reverse (see section 7).

**How the parts communicate**
- **App flow (M2):** `ScreenFlow` wires the menu screens by button name: `HomeScreen` → `EcosystemSelectionScreen` → `ModeSelectionScreen` → **Guided** (straight into `HimalayaSimulation`, 10 Blue Sheep / 2 Snow Leopards) or **Free** (`ConfigurationScreen`, which loads `HimalayaSimulation` additively behind the menu with time paused). In Free the player sets starting numbers with `SpeciesCounter` (default 10 / 2, max 20 / 5, from the live conservation status); **Start** stores them in `EcosystemSimulator.StartingPopulationOverrides`, resets the ecosystem and resumes time. When a run starts, `SimHud` loads Shu Min's `UI` scene on top as the HUD. The Field Journal opens over the paused run; Back asks before leaving. (The project skips scene reload on Play, so `ScreenFlow` also wires the scene that is already open.)
- **Tick loop:** `SimulationClock` ticks `EcosystemSimulator` (every `secondsPerTick`, or once per game day when a `ClockService` is set); the simulator's `Ticked` event then refreshes `PopulationDisplay` and `CreatureSpawner` once, whoever ticked (single sim clock, Dillon). Play / Pause / Reset / speed (x1-x8) are on the clock, driven by `SimulationSpeedController` buttons. Reset calls `ResetEcosystem()` (starting populations).
- **Agents:** `CreatureSpawner` keeps the number of 3D animals in line with the population (one model per animal, rounded like the label) and registers each one in `EcoRegistry`. Herds and leopard territories spawn in their real habitat heights, away from the map edge, each herd on one river bank. In HimalayaSimulation each animal runs `AnimalBrain` (utility AI): flee > hunt > eat / drink > mate > wander, driven by Fullness, Hydration and HP. `AnimalPerception` decides what an animal can see (vision cone + line-of-sight raycast; sheep see 300°, always sense a leopard within 10 m, a stalking leopard only within 9 m). Leopards stalk, then sprint, and give up after a short chase; sheep that see one warn their herd and remember the spot (`DangerMemory`). Pathfinding is Unity's NavMesh (A*); in Events mode it reports `RecordPredation` / `RecordEat` / `RecordStarvation` to the simulator. Each animal's `GoalOrientedAnimalBrain` scores its goals by need urgency and runs behaviour-tree actions: Seek finds the nearest `ResourcePatch`, Flee and Hunt ask `EcoRegistry` for the nearest predator / prey (from the Eats relationships).
- **Animation:** `AnimalBrain` adds an `AnimalMotionBridge`, which adds a `ProcAnimator` (our procedural animation, `packages/procanim`) and turns the brain's action into a motion state each frame (walk, run, flee, stalk, eat, drink, alert, die). `ProcAnimator` measures speed and turning from how the NavMeshAgent moved the animal, finds the legs, head and tail in the static mesh, and animates a pivot child plus the mesh's vertices. Settings per species: `Resources/Motion/<species asset name>`.
- **Spatial grid:** `EcoRegistry` answers nearest-prey/predator/food queries from an 8 m spatial hash grid once a species has 32+ animals (rebuilt once per frame, searched ring by ring); smaller groups use a plain scan. Measured: 500 animals 0.87 ms per frame (`PerformanceTests`). Press **G** in Play mode (Editor/dev builds) for the debug view.
- **Data in:** the simulator and AI read `SpeciesDefinition` / `ResourceDefinition` assets. Prey comes from `SpeciesDefinition.GetSpeciesTargets(RelationshipType.Eats)`.
- **Data out:** population counts live in `EcosystemSimulator.populations` (a dictionary keyed by species asset). The UI reads them directly for M1. From M2 the UI reads through `ISimulationDataProvider`, so it never depends on simulation internals.
- **Selection → inspection:** `ObjectSelector` publishes the selected object on the shared `SelectionContext` asset, and `InspectorPanel` listens to it and shows `IInspectable` stats.

## 3. Components and owners

Owners follow the M1 Technical Deliverable plan. *Primary* owners are listed first.

| Component | Key scripts / assets | Deliverable | Owner(s) | M1 status |
|---|---|---|---|---|
| Ecosystem Simulation Core | `packages/ecosim`: `EcosystemSimulator`, `EcoRegistry`; app: `SimulationClock` | T03 | Wei Leong, Reginald | Done: Formula + Events modes (eat / catch / starve), registry, Play/Pause/Reset/speed; tested |
| Relationship System | `Relationship`, `SpeciesDefinition.relationships` | T01 | Zhi Jie | Done (Eats / Needs / Affects), with tests |
| Entity Data System | `SpeciesDefinition`, `ResourceDefinition`, Need/Goal/ActionDefinition, species assets | T02 | Li Juan, Yusuf | Done: data-only species incl. needs/goals, audio, 3D model |
| Species authoring tool | `Editor/AnimalCreator` | T02 | Li Juan | Create + edit species from an Editor window |
| Simulation Data Display (UI) | `PopulationDisplay`, `UI/PopulationCounter`, `UI/ResourceLevels`, `UI/EventFeed`, `SimulationData/*` providers | T04 | Yan De, Dillon | Species and resource counts, resource levels and an event feed on screen, with tests; provider adapter planned M2 |
| Configuration flow | `UI/Configuration` (`MainMenu`, `ConfigurationScreen`, `SpeciesCounter`), HomeScreen + ConfigurationScreen scenes | F01 | Reginald, Shu Min | Set starting populations, then Start |
| Agent AI Controller | `GoalOrientedAnimalBrain` + behaviour tree (`Behaviourtreecore/*`), `ResourcePatch` | T07 | Yusuf, Zhi Jie, Reginald | Done: needs → goals; Wander, Seek grass, Flee, Hunt; goal-selection tests |
| Visualisation (population → agents) | `CreatureSpawner`, Blue Sheep and Snow Leopard 3D models | T03/T04 | Wei Leong, Li Juan | Agents spawn by population and register in `EcoRegistry` |
| Procedural animation (M2) | `packages/procanim`: `ProcAnimator`, `MotionStateMachine`, `SpringFloat`, `LegLayout`, `Appendage`, `BodyDeformer`; app: `AnimalMotionBridge`, `Resources/Motion/*` | SCRUM-267 (TI-10) | Wei Leong | Done: our own animation instead of Unity's Animator; 13 motions incl. legs/head/tail found in the static meshes, child-friendly death; data-driven; 36 tests, 0.085 ms per frame for 500 animals |
| Spatial grid (M2) | `packages/ecosim/Runtime/EcoRegistry.cs`; debug view `DevTools/SpatialGridView` | SCRUM-264–266 (TI-16) | Wei Leong | Done: 8 m hash grid with ring search; same answers as a full scan (500-animal test); 20 vs 500 animals benchmark in the perf history |
| Selection & Inspection | `Selection/*`, `Inspection/*`, `InspectorPanel` | F09 | Yan De | Tap-to-select + inspector panel framework |
| Audio | `Audio/AudioManager`, BGM + species SFX | — | Li Juan | Done: BGM/ambience, footsteps |
| Population history (M2) | `packages/ecosim/Runtime/History`: `PopulationHistory`, `PopulationHistoryRecorder` | T05 / F05 | Wei Leong | Done: per-day species and resource values, read by the graph and the case detector; tested |
| Investigation cases (M2, Free Exploration) | `packages/ecosim/Runtime/Cases`: `InvestigationCase`, `CaseEvaluator`, `CaseJournal`, `CaseDetector`; case assets in `Assets/EcoBalance/Resources/Cases` | F07 / Field Journal | Wei Leong (engine), Dillon (UI), design (case content) | Engine done and tested; journal UI done (see Field Journal row) |
| Balance model (M2) | `packages/ecosim/Runtime/Balance`: `BalanceModel`, `BalanceParameters` | T03 | Wei Leong | Headless agent model of design's simulation spec; proves cases are reachable; tested |
| Dev overlay (M2) | `Assets/EcoBalance/Scripts/DevTools/SimDebugOverlay` | — | Wei Leong | Editor and development builds only: day, populations, case status |
| Headless soak test (M2/M3) | `Assets/EcoBalance/Scripts/DevTools/HeadlessSoakRunner`, `EcoBalanceCIBuild.BuildLinuxHeadless`, `tools/vm/soak.sh` | — | Wei Leong | Linux headless build from CI, run weekly on the team VM for 2,000 simulated days; report + history on Gitea ([details](infra/ci.md)) |
| Himalaya terrain (M2) | `Scripts/Terrain`: `HimalayaWorldBuilder`, `HimalayaTerrainBuilder`, `HimalayaSplatPainter`, `HimalayaTreePlacer`, `HimalayaSightings` | — | Yusuf (generator), Li Juan (terrain data, `TerrainConfig`) | Done: real Everest-area heightmap, textures, trees, GBIF sightings |
| Live data (M2) | `IslandPlatform/WeatherManager`, `TerrainConfig` (Wikidata), bundled OpenStreetMap water in `Resources/HimalayaData` | — | Li Juan | Done: live Everest temperature + condition (valley temperature derived), terrain counts |
| Utility AI + reproduction (M2) | `EcoSim/AnimalBrain` (utility scoring: flee, hunt, eat, drink, mate, roam) | T07 | Zhi Jie, Reginald | Done: per-animal hunger/thirst, pair mating; tuning by Wei Leong (spec timings, senses for the big map, fear beats hunger) |
| Integration build (M2) | branch `M2-Integration` | — | Wei Leong | Merges everyone's work into one playable build; merged into `main` at the milestone |
| Realistic world (M2) | `Terrain/HimalayaConfig` (Khumbu valleys box), `HimalayaRiver`, `WaterMeshBuilder`, `HimalayaResourcePlacer` (meadows), `GrassTuftMesh`, shaders `GrassWind`, `WaterFlow`; `CreatureSpawner` habitats | SCRUM-286 | Wei Leong | Done: real Khumbu heights (tree line 4,200 m, snow line 5,200 m), main river from the glacier snout (NavMesh-blocked, walk round over the ice), up to 5 glacier streams with fords, meadows kept off the water, 5 sheep herds + solitary leopard territories by habitat height |
| Research numbers + health (M2) | `EcoSim/AnimalBrain` (Fullness, Hydration, HP, breeding), `EcoSim/AnimalData` (live status → starting numbers) | SCRUM-296 | Wei Leong | Done: values from the design research (see section 2.2, Events mode) |
| Perception (M2) | `EcoSim/AnimalPerception` | T07 | Reginald (Wei Leong: 300° prey vision, stalker and close-sense rules) | Done: vision cone + raycast line of sight, gizmo lines in the Scene view |
| Hunting, herd alarm, danger memory (M2) | `EcoSim/AnimalBrain` (stalk / sprint), `EcoSim/DangerMemory` | T07 | Yusuf (Wei Leong: research hunt rules, merge) | Done: stalk then sprint, give up and rest, herd alarm calls, sheep avoid places they saw a leopard |
| Guided Exploration (M2) | `Guided/GuidedChallenge`, `Guided/GuidedEffects` | SCRUM-297 | Wei Leong (design: Shu Min) | Done: 20-day challenge, events GE01–GE08 with pausing 3-choice popups and timed buffs/debuffs, early collapse, 5 endings, Ending Gallery |
| Menu flow + simulation HUD (M2) | `UI/Configuration/ScreenFlow`, `UI/SimHud`; scenes HomeScreen, EcosystemSelection, ModeSelection, FieldJournal, `UI` | SCRUM-298 | Wei Leong (screens + HUD design: Shu Min) | Done: buttons wired by name; the HUD shows live day, populations, vegetation / water with trends, animal inspection (Fullness, Hydration, Health, action, Did You Know); Field Journal over the paused run |
| Field Journal (M2) | `UI/FieldJournal`: `FieldJournalScreen`, `FieldJournalData`, `PopulationGraph`, `CaseDiscoveryToast`; scene `FieldJournalScene`; `FieldJournalTests` | SCRUM-277 | Dillon | Done: investigations and observations from the case engine, live population graph, case discovery popup during the run; opens over the paused simulation |
| Hunting camouflage (M2) | `EcoSim/TerrainCover` (used by `AnimalPerception`) | T07 | Yusuf | Done: a leopard on rocky or snowy ground is harder for sheep to spot |
| Terrain editor (dev tool, M2) | `Terrain/TerrainEditTool` | — | Yusuf | Editor / development builds only: sculpt and repaint the terrain while playing |
| Tooltips, action menu, camera (M2) | `UI/Tooltip/*`, `UI/ActionMenu/*`, `Camera/*`, `Terrain/HimalayaBuildProfile` | — | Yan De | Done: tooltip system, FAB action menu preview, flat-grass build profile (PR #51) |
| IslandPlatform HUD (M2, test scene) | `IslandPlatform/IslandPlatformHud`, `SpeedControlBar`, `SpawnedPopulationSync` | — | Yan De | Done: speed bar, animal info and population sync for the IslandPlatform test scene (PR #54) |
| Weather & seasons (M2) | `EcoSim/SeasonWeather` | T06 | Wei Leong (system), Li Juan (live weather) | Done: 4 seasons of 5 days (2 real minutes per day at 1x); river refill follows glacier melt (warm = full, freezing = low); heat raises thirst, cold raises hunger; Guided always starts in summer |
| "Watch!" moments + camera (M2) | `EcoSim/DramaDirector`, `HeartPop`, `Camera/TerrainCameraController` | — | Wei Leong (built), Yan De (camera owner: buttons, touch testing) | Done: banners for hunts / catches / mating / births with a one-tap follow; orbit camera (drag to spin / tilt, two fingers to move, pinch to zoom) kept over the terrain and lifted over hills, third-person follow, POV |
| Metrics export (CSV) | *(planned, reads PopulationHistory)* | T05 (optional) | Li Juan | Not started |
| CI / build / test pipeline | `.github/workflows/unity-ci.yml`, `tools/ci/repo-checks.py`, `packages/*/Tests/Editor/*`, `Assets/EcoBalance/Tests/Editor/*` | #4, #5 | Wei Leong | Done: repo checks + unit / smoke / performance tests on PRs into `main`; tests nightly on `M2-Integration`; tag/manual Android APK to Gitea ([details](infra/ci.md)) |
| UI/UX design | Figma / `docs/design/ui-ux`, `Assets/Scenes/UI.unity` | — | Nomitha, Shu Min (Design) | 16 M2 wireframes in `docs/design/ui-ux/wireframes`; menu screens and the simulation HUD built in Unity by Shu Min and wired up |

## 4. Data stores

| Store | What | Where |
|---|---|---|
| ScriptableObject assets | Species, resources, relationships, system configs | `Assets/EcoBalance/ScriptableObjects/`, `Assets/EcoBalance/Data/` |
| Runtime state | Populations and resource amounts (in memory, per play session) | `EcosystemSimulator.populations`, `ResourceDefinition.currentAmount` |
| Scenes / prefabs | Level layout and agent prefabs | `Assets/Scenes/`, `Assets/EcoBalance/Prefabs/` |
| Population history | Per-day species and resource values (in memory, newest 1,000 days) | `PopulationHistoryRecorder.History` |
| Field Journal progress | Unlocked investigation case ids | Device `PlayerPrefs` (`EcoBalance.CaseJournal`) |
| Guided Ending Gallery | Endings found (bit mask) | Device `PlayerPrefs` (`EcoBalance.GuidedEndings`) |
| Exported metrics *(planned, T05)* | Population history as CSV | — |

There is no server or database. EcoBalance is a single offline mobile app.

## 5. External systems

| System | Used for |
|---|---|
| GitHub + GitHub Actions (GameCI Docker images) | Source control, PRs, automated tests and Android builds |
| Unity Licensing (serial activation in CI) | Activating the Unity editor on CI runners |
| Jira | Backlog, epics per feature/deliverable, individual task tracking |
| Team VM soak test | Runs the Linux headless build weekly (`tools/vm/soak.sh`); reports to Gitea packages |
| Self-hosted Gitea (team21 VM) | **Git LFS store** for binary assets, over HTTPS at `https://team21.dp-ext8.com/git/` (Caddy reverse proxy, sign-up off). Code and branches stay on GitHub; CI pulls LFS files with the `GITEA_LFS_TOKEN` secret. See [infra/gitea-rebuild.md](infra/gitea-rebuild.md) |

## 6. Layers (detail)

### Simulation layer (core logic, no visuals)
- **Entity Data System:** species/resource properties are configurable ScriptableObject data, not hardcoded.
- **Relationship System:** defines what each entity eats/needs/affects, configurable per entity.
- **Individual Agent System:** per-organism needs (hunger, thirst) in `AnimalNeedsState`, driving `GoalOrientedAnimalBrain`; shown in tap-to-inspect from M2.
- **Animal AI / Behaviour System:** per-agent decisions (seek food, flee, hunt) driven by agent state.
- **Plants/Resources System:** resource consumption, depletion and regeneration over time.
- **Ecosystem Simulation Core:** a tick-based engine connecting all of the above over time.

### Data / interface layer
- **Simulation Data Tracking** *(planned)*: records population/resource history each tick and feeds the UI graphs/timeline.
- **Simulation Data Provider:** a read-only `ISimulationDataProvider` (string species/resource ids) for Displays 1–2, with a mock for UI-first work.
- **Simulation Event Provider:** an `ISimulationEventProvider` stub for the Display 3 behaviour feed.
- **Contextual Trigger System** *(planned)*: detects meaningful simulation changes and fires events for the UI.
- **Simulation Time Control:** play/pause/reset + speed x1-x8 on `SimulationClock` (F02/F06).

### Presentation layer
- **Visualisation/Feedback:** translates simulation and AI state into visible feedback (population → agents, AI state → procedural animation via `AnimalMotionBridge` → `ProcAnimator`, vegetation depletion).
- **UI** (Design-owned): inspector panel, population graphs/timeline, contextual learning prompts.
- **Selection System:** generic click/tap picking (`ObjectSelector` + `ISelectable`), published on a shared `SelectionContext` asset.
- **Selection Highlight:** `SelectionHighlighter` + `ISelectionHighlightVisual` (ground ring) driven by `SelectionHighlightConfig`.
- **Inspection Data Interface:** read-only `IInspectable` / `StatEntry` via `ISimulationQuery` and an `InspectionConfig` schema catalog.
- **Inspector Panel:** type-agnostic `InspectorPanel` + `StatRowView`, styled by `InspectionUiConfig`.
- **Overlay Positioning:** `IOverlayPositioner` (Screen Space + World Space) driven by `InspectionUiConfig.overlayMode`.
- **Island Platform:** `IslandManager` owns the active island instance (spawn at `worldAnchor`, `centerOffset` pivot fix, swap with fade + ecosystem reset). Per-island data lives on `IslandDefinition` assets; platform knobs on `IslandPlatformConfig`. Implements `IOrbitTarget` for the camera.
- **Orbit Camera:** `OrbitCameraController` + `CameraConfig` (InputActionReferences, clamps, damping); pivots via `IOrbitTarget` only. 

## 7. Repository and module structure

```
Project_5_99Bugs/
├── apps/
│   └── mobile/
│       └── EcoBalance3D/                  # Unity app (assembly: Assembly-CSharp)
│           ├── Assets/
│           │   ├── Scenes/                # HomeScreen → EcosystemSelection → ModeSelection → Guided / Free (ConfigurationScreen) → HimalayaSimulation + UI (HUD); FieldJournalScene; IslandPlatform, T02_Test (test scenes, not built)
│           │   └── EcoBalance/
│           │       ├── Audio/             # BGM + SFX
│           │       ├── Data/              # app assets: AI needs/goals/actions, islands, camera, UI config
│           │       ├── Prefabs/           # creatures, islands
│           │       ├── Resources/Cases/   # investigation case assets (Field Journal)
│           │       ├── Resources/Motion/  # motion profiles per species (BlueSheep, SnowLeopard, Default)
│           │       ├── ScriptableObjects/ # Species (BlueSheep, SnowLeopard), Resources (Grass, Water)
│           │       ├── Scripts/
│           │       │   ├── Audio/             # AudioManager
│           │       │   ├── Behaviourtreecore/ # behaviour tree + action executors (Wander, Seek, Flee, Hunt)
│           │       │   ├── Camera/            # orbit camera
│           │       │   ├── Creatures/         # CreatureSpawner
│           │       │   ├── EcoSim/            # SimulationClock, speed controller, AnimalBrain, AnimalPerception, DangerMemory, SeasonWeather, AnimalMotionBridge
│           │       │   ├── Guided/            # GuidedChallenge (events, endings, gallery), GuidedEffects (buffs / debuffs)
│           │       │   ├── Terrain/           # Himalaya world: terrain, river + glacier streams, meadows, TerrainEditTool (dev)
│           │       │   ├── DevTools/          # SimDebugOverlay + SpatialGridView (Editor/dev builds), HeadlessSoakRunner (-soakDays)
│           │       │   ├── Editor/            # BuildScript, EcoBalanceCIBuild, AnimalCreator window
│           │       │   ├── Inspection/        # IInspectable, inspector data
│           │       │   ├── IslandPlatform/    # IslandManager, island swap
│           │       │   ├── Resources/         # ResourcePatch
│           │       │   ├── Selection/         # tap-to-select
│           │       │   ├── SimulationData/    # ISimulationDataProvider (UI read interface)
│           │       │   └── UI/                # ScreenFlow (menu flow), SimHud (HUD), Configuration, Tooltip, ActionMenu, PopulationCounter, ResourceLevels, EventFeed, InspectorPanel
│           │       └── Tests/             # app EditMode tests
│           ├── Packages/manifest.json     # loads com.99bugs.ecosim and com.99bugs.procanim from packages/
│           └── ProjectSettings/
├── packages/
│   ├── ecosim/                            # Unity package com.99bugs.ecosim
│       ├── package.json
│       ├── Runtime/                       # assembly: EcoSim (no app dependency)
│       │   ├── EcosystemSimulator.cs      # tick: regrow, graze, predation, growth
│       │   ├── SpeciesDefinition.cs  Relationship.cs
│       │   ├── EcoRegistry.cs  EcoEntity.cs   # nearest food / prey / predator
│       │   ├── IEcosystemReset.cs         # implemented by the app (island swap)
│       │   ├── Definitions/               # Resource, Need, Goal, Action definitions
│       │   ├── History/                   # PopulationHistory + recorder (per-day values)
│       │   ├── Cases/                     # investigation cases, evaluator, journal, detector
│       │   └── Balance/                   # headless balance model + parameters
│       └── Tests/Editor/                  # assembly: EcoSim.Tests.Editor (simulator, relationships, registry, history, cases, balance, performance)
│   └── procanim/                          # Unity package com.99bugs.procanim (our procedural animation)
│       ├── Runtime/                       # assembly: EcoProcAnim (no app or EcoSim dependency)
│       │   ├── ProcAnimator.cs            # component: pose from speed/turning + state
│       │   ├── MotionStateMachine.cs  MotionProfile.cs  MotionPose.cs  SpringFloat.cs
│       │   └── LegLayout.cs  Appendage.cs  BodyDeformer.cs   # legs/head/tail found in static meshes
│       └── Tests/Editor/                  # assembly: EcoProcAnim.Tests.Editor
├── docs/                                  # proposal, design, architecture, deliverables
├── tests/                                 # test plans, manual checklists
├── tools/                                 # unity.sh / unity.bat; ci/repo-checks.py; vm/soak.sh (VM soak test)
└── .github/workflows/unity-ci.yml         # CI: repo checks, tests, Android build (docs/infra/ci.md)
```

| Module | Assembly | May depend on | Must not depend on |
|---|---|---|---|
| `packages/ecosim/Runtime` | `EcoSim` | UnityEngine only | Any app code |
| `packages/ecosim/Tests/Editor` | `EcoSim.Tests.Editor` | `EcoSim`, NUnit | App code |
| `packages/procanim/Runtime` | `EcoProcAnim` | UnityEngine only | App code, `EcoSim` |
| `packages/procanim/Tests/Editor` | `EcoProcAnim.Tests.Editor` | `EcoProcAnim`, NUnit | App code |
| `apps/mobile/EcoBalance3D/Assets` | `Assembly-CSharp` (+ `-Editor`) | `EcoSim`, `EcoProcAnim` (auto-referenced) | — |

**Why this split:** the simulation core (species/resource data, relationships, `EcosystemSimulator`,
`EcoRegistry`, AI data types) is the part we most want to keep correct and reusable, so it is a separate
Unity package with its own assembly and tests. The compiler now enforces the one-way dependency
(the core cannot see UI or scene code). When the core needs something from the app it defines an
interface that the app implements (`IEcosystemReset`, used by the island swap). App-specific behaviour
stays in the app because it depends on scenes, TextMeshPro, NavMesh and teammates' systems.

## 8. Design principles

- **Data-driven:** new organisms, resources or relationships are added as data assets, without new simulation code. This has been validated conceptually on two domains (Himalayan blue sheep/snow leopard, and a tree/paper resource cycle).
- **Layered, one-way dependencies:** the simulation never references UI; this is enforced by the `EcoSim` package assembly (section 7). The UI reads simulation state (through interfaces from M2).
- **Testable core:** the simulation logic runs in EditMode tests without a scene, and CI runs those tests on every PR.
- **Engine vs content vs UI (M2):** investigation cases are data assets written by design; the engine (`CaseDetector`) evaluates them against `PopulationHistory`; the UI only listens to `CaseUnlocked`. Each side changes without touching the others.

---
*Diagram and component/owner tables drafted with AI assistance: Claude Code (Claude Opus 5.5, claude-opus-5-5). Reviewed by Tan Wei Leong. Section 6 (Layers) written by the team.*
