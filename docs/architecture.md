# EcoBalance — System Architecture

## Engine & Tech Stack

- Engine: Unity 2022 LTS (URP)
- Language: C#
- Platform: 
  
      Mobile (Android/iOS)
- Source Control: GitHub
- Project Management: Jira

## System Architecture

### Simulation Layer (core logic, no visuals)

- **Entity Data System** — species/resource properties stored as configurable data (ScriptableObjects), not hardcoded
- **Relationship System** — defines what each entity eats/needs/affects, configurable per entity
- **Individual Agent System** — per-organism live state (hunger, thirst, health) for tap-to-inspect
- **Animal AI / Behaviour System** — per-agent decisions (seek food, flee, hunt) driven by agent state
- **Plants/Resources System** — resource consumption, depletion, and regeneration over time
- **Ecosystem Simulation Core** — tick-based engine connecting all of the above over time

### Data/Interface Layer

- **Simulation Data Tracking** — records population/resource history each tick, feeds UI graphs/timeline
- **Simulation Data Provider** — read-only `ISimulationDataProvider` (string species/resource ids) for Displays 1–3; mock implementation for UI-first work. 
- **Contextual Trigger System** — detects meaningful simulation changes, fires events for the UI to respond to
- **Simulation Time Control** — play/pause/speed/reset/reconfigure

### Presentation Layer

- **Visualization/Feedback System** (RTIS-owned) — translates simulation and AI state into visible feedback: population-to-visual agent bridge, AI state → animation (e.g. fleeing state triggers flee animation), environmental visual feedback (e.g. vegetation depletion)
- **UI** (Design-owned) — inspector panel, population graphs/timeline, contextual learning prompts
- **Selection System** — generic click/tap picking (`ObjectSelector` + `ISelectable`); selection is tracked by handle and published on a shared `SelectionContext` asset.
- **Selection Highlight** — peer `SelectionHighlighter` + `ISelectionHighlightVisual` (default ground ring) driven by `SelectionHighlightConfig`; listens to `SelectionContext` only; material-independent.
- **Inspection Data Interface** — read-only `IInspectable` / `StatEntry` via `ISimulationQuery` and an `InspectionConfig` schema catalog.
- **Inspector Panel** — type-agnostic `InspectorPanel` + `StatRowView` styled by `InspectionUiConfig`; binds to `SelectionContext` and paints `IInspectable` only.
- **Overlay Positioning** — `IOverlayPositioner` (Screen Space + World Space) driven by `InspectionUiConfig.overlayMode`; content `Show`/`Hide` stays free of follow/billboard math. 

## Data Flow

Simulation Layer produces raw state → Data/Interface Layer records and evaluates it → Presentation Layer renders it to the player.

## Design Principle

The architecture is data-driven: new organisms, resources, or relationships can be added by creating new data assets, without writing new simulation code. This has been validated conceptually across two example domains (Himalayan ecosystem — blue sheep/snow leopard; and a tree/paper resource-cycle example), confirming the systems generalize rather than being hardcoded to one scenario.
