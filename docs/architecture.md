# EcoBalance — System Architecture (v0.1)

**Owner of this document:** Tan Wei Leong (Tech Lead)
**Status:** v0.1 for Milestone 1. It describes what is built and what is planned. Anything planned is marked *(planned)*.

See also: [toolchain.md](toolchain.md) (technology choices), [data-pipeline.md](data-pipeline.md) (asset/data rules) and [deliverables.md](deliverables.md) (M1/M2/M3 targets).

## 1. Tech stack (summary)

| | |
|---|---|
| Engine | Unity **6000.6.0f1** (Unity 6), URP |
| Language | C# |
| Target platform | Mobile (Android first) |
| Source control / CI | GitHub + GitHub Actions (GameCI): EditMode tests + Android build on every PR to `main` |
| Project management | Jira (SCRUM project) |

## 2. Architecture diagram

```mermaid
flowchart TB
    subgraph PKG["packages/ecosim (com.99bugs.ecosim) - simulation core, no app dependency"]
        subgraph DATA["Data types (ScriptableObjects)"]
            SD["SpeciesDefinition<br/>BlueSheep, SnowLeopard"]
            RD["ResourceDefinition<br/>Grass, Water"]
            REL["Relationship<br/>Eats / Needs / Affects"]
            NGA["Need / Goal / ActionDefinition<br/>Hunger, SatisfyHunger, Seek grass"]
            SD --- REL
            SD --- NGA
        end
        ECO["EcosystemSimulator<br/>Tick(): regrowth, predation, grazing, growth"]
        REG["EcoRegistry + EcoEntity<br/>nearest food / prey / predator"]
        RST["IEcosystemReset"]
        ECO -. implements .-> RST
    end

    subgraph APP["apps/mobile/EcoBalance3D (Unity app)"]
        subgraph CTRL["Simulation control"]
            CLK["SimulationClock<br/>tick timer, Play/Pause/Reset, speed"]
            SPD["SimulationSpeedController<br/>UI buttons x1-x8"]
        end
        subgraph AIL["Animal AI (per agent, NavMesh)"]
            BRN["GoalOrientedAnimalBrain<br/>needs to goal selector"]
            BT["Behaviour tree actions<br/>Wander, Seek, Flee, Hunt, Idle"]
            PATCH["ResourcePatch<br/>grass that shrinks and regrows"]
            BRN --> BT
        end
        subgraph PRES["Presentation"]
            SPN["CreatureSpawner<br/>population to agents"]
            POP["PopulationDisplay<br/>on-screen counts"]
            SEL["Selection + InspectorPanel<br/>tap-to-inspect"]
            ISL["IslandManager + OrbitCamera"]
            AUD["AudioManager<br/>BGM + SFX"]
        end
        PROV["ISimulationDataProvider / IInspectable<br/>read-only interfaces for UI"]
    end

    subgraph TOOLS["Tools and pipeline"]
        AC["AnimalCreator window"]
        TEST["EditMode tests<br/>25 package + 8 app"]
        CI["GitHub Actions<br/>tests + Android build"]
    end

    SD --> ECO
    RD --> ECO
    SPD --> CLK
    CLK -- "Tick()" --> ECO
    CLK -- "Refresh()" --> POP
    CLK -- "RefreshVisuals()" --> SPN
    ECO -- "populations" --> POP
    ECO -- "populations" --> SPN
    SPN -- "spawns + registers" --> REG
    SPN --> BRN
    BT -- "Flee / Hunt" --> REG
    BT -- "Seek" --> PATCH
    NGA --> BRN
    ISL -- "ResetEcosystem()" --> RST
    ECO -.-> PROV
    PROV --> SEL
    AC -- "writes .asset" --> SD
    TEST -- "exercises" --> ECO
    TEST -- "exercises" --> REG
    CI -- "runs" --> TEST
```

Solid box `packages/ecosim` = reusable core; everything in `apps/mobile` depends on it, never the reverse (see §7).

**How the parts communicate**
- **Tick loop:** `SimulationClock.Update()` calls `EcosystemSimulator.Tick()`, then `PopulationDisplay.Refresh()` and `CreatureSpawner.RefreshVisuals()`. Play / Pause / Reset / speed (x1-x8) are on the clock, driven by `SimulationSpeedController` buttons. Reset calls `ResetEcosystem()` (starting populations).
- **Agents:** `CreatureSpawner` keeps the number of 3D animals in line with the population and registers each one in `EcoRegistry`. Each animal's `GoalOrientedAnimalBrain` scores its goals by need urgency and runs behaviour-tree actions: Seek finds the nearest `ResourcePatch`, Flee and Hunt ask `EcoRegistry` for the nearest predator / prey (from the Eats relationships).
- **Data in:** the simulator and AI read `SpeciesDefinition` / `ResourceDefinition` assets. Prey comes from `SpeciesDefinition.GetSpeciesTargets(RelationshipType.Eats)`.
- **Data out:** population counts live in `EcosystemSimulator.populations` (a dictionary keyed by species asset). The UI reads them directly for M1. From M2 the UI reads through `ISimulationDataProvider`, so it never depends on simulation internals.
- **Selection → inspection:** `ObjectSelector` publishes the selected object on the shared `SelectionContext` asset, and `InspectorPanel` listens to it and shows `IInspectable` stats.

## 3. Components and owners

Owners follow the M1 Technical Deliverable plan. *Primary* owners are listed first.

| Component | Key scripts / assets | Deliverable | Owner(s) | M1 status |
|---|---|---|---|---|
| Ecosystem Simulation Core | `packages/ecosim`: `EcosystemSimulator`, `EcoRegistry`; app: `SimulationClock` | T03 | Wei Leong, Reginald | Done: predation + grazing tick loop, registry, Play/Pause/Reset/speed; tested |
| Relationship System | `Relationship`, `SpeciesDefinition.relationships` | T01 | Zhi Jie | Done (Eats / Needs / Affects), with tests |
| Entity Data System | `SpeciesDefinition`, `ResourceDefinition`, Need/Goal/ActionDefinition, species assets | T02 | Li Juan, Yusuf | Done: data-only species incl. needs/goals, audio, 3D model |
| Species authoring tool | `Editor/AnimalCreator` | T02 | Li Juan | Create + edit species from an Editor window |
| Simulation Data Display (UI) | `PopulationDisplay`, `SimulationData/*` providers | T04 | Yan De, Dillon | Species (and resource) counts on screen; provider adapter planned M2 |
| Agent AI Controller | `GoalOrientedAnimalBrain` + behaviour tree (`Behaviourtreecore/*`), `ResourcePatch` | T07 | Yusuf, Zhi Jie, Reginald | Done: needs → goals; Wander, Seek grass, Flee, Hunt; goal-selection tests |
| Visualisation (population → agents) | `CreatureSpawner`, Blue Sheep model, placeholder leopard | T03/T04 | Wei Leong, Li Juan | Agents spawn by population and register in `EcoRegistry` |
| Selection & Inspection | `Selection/*`, `Inspection/*`, `InspectorPanel` | F09 | Yan De | Tap-to-select + inspector panel framework |
| Audio | `Audio/AudioManager`, BGM + species SFX | — | Li Juan | Done: BGM/ambience, footsteps |
| Metrics & Data Logging | *(planned)* | T05 (optional) | Unassigned | Not started |
| Weather & Hazard Events | *(planned)* | T06 (optional) | Unassigned | Not started |
| CI / build / test pipeline | `.github/workflows/unity-ci.yml`, `packages/ecosim/Tests/Editor/*`, `Assets/_Project/Tests/Editor/*` | #4, #5 | Wei Leong | Done: tests + Android build green on `main` |
| UI/UX design | Figma / `docs/design/ui-ux` | — | Nomitha, Shu Min (Design) | Wireframes / mockups |

## 4. Data stores

| Store | What | Where |
|---|---|---|
| ScriptableObject assets | Species, resources, relationships, system configs | `Assets/_Project/ScriptableObjects/`, `Assets/_Project/Data/` |
| Runtime state | Populations and resource amounts (in memory, per play session) | `EcosystemSimulator.populations`, `ResourceDefinition.currentAmount` |
| Scenes / prefabs | Level layout and agent prefabs | `Assets/Scenes/`, `Assets/_Project/Prefabs/` |
| Exported metrics *(planned, T05)* | Per-tick population history as CSV | — |

There is no server or database. EcoBalance is a single offline mobile app.

## 5. External systems

| System | Used for |
|---|---|
| GitHub + GitHub Actions (GameCI Docker images) | Source control, PRs, automated tests and Android builds |
| Unity Licensing (serial activation in CI) | Activating the Unity editor on CI runners |
| Jira | Backlog, epics per feature/deliverable, individual task tracking |

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
- **Visualisation/Feedback:** translates simulation and AI state into visible feedback (population → agents, AI state → animation, vegetation depletion).
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
apps/mobile/EcoBalance3D/   Unity app: scenes, art, audio, prefabs, app scripts (UI, island, camera,
                            clock, spawners, animal brains / behaviour tree), app tests
packages/ecosim/            Unity package com.99bugs.ecosim: simulation core + tests
docs/  tests/  tools/  .github/workflows/
```

| Module | Assembly | May depend on | Must not depend on |
|---|---|---|---|
| `packages/ecosim/Runtime` | `EcoSim` | UnityEngine only | Any app code |
| `packages/ecosim/Tests/Editor` | `EcoSim.Tests.Editor` | `EcoSim`, NUnit | App code |
| `apps/mobile/EcoBalance3D/Assets` | `Assembly-CSharp` (+ `-Editor`) | `EcoSim` (auto-referenced) | — |

**Why this split:** the simulation core (species/resource data, relationships, `EcosystemSimulator`,
`EcoRegistry`, AI data types) is the part we most want to keep correct and reusable, so it is a separate
Unity package with its own assembly and tests. The compiler now enforces the one-way dependency
(the core cannot see UI or scene code). When the core needs something from the app it defines an
interface that the app implements (`IEcosystemReset`, used by the island swap). App-specific behaviour
stays in the app because it depends on scenes, TextMeshPro, NavMesh and teammates' systems.

## 8. Design principles

- **Data-driven:** new organisms, resources or relationships are added as data assets, without new simulation code. This has been validated conceptually on two domains (Himalayan blue sheep/snow leopard, and a tree/paper resource cycle).
- **Layered, one-way dependencies:** the simulation never references UI; this is enforced by the `EcoSim` package assembly (§7). The UI reads simulation state (through interfaces from M2).
- **Testable core:** the simulation logic runs in EditMode tests without a scene, and CI runs those tests on every PR.

---
*Diagram and component/owner tables drafted with AI assistance: Claude Code (Claude Opus 5.5, claude-opus-5-5). Reviewed by Tan Wei Leong. Layer descriptions in §6 written by the team.*
