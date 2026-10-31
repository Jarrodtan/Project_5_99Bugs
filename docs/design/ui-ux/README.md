# UI/UX

Wireframes, user flows and mockups for EcoBalance. Export images as `.png` and link them from here.

## Wireframes

| Area | Screens |
|---|---|
| Menus | [Home](wireframes/13-home-menu.png) · [Choose an ecosystem](wireframes/14-choose-ecosystem.png) · [Guided or free](wireframes/15-explore-mode.png) |
| Simulation HUD | [HUD](wireframes/12-sim-hud.png) · [Overview expanded](wireframes/11-sim-overview-expanded.png) · [Animal inspection](wireframes/09-sim-animal-inspection.png) · [Discovery toast](wireframes/08-sim-discovery-toast.png) |
| Prediction loop | [Let's think](wireframes/10-sim-lets-think.png) · [Your prediction](wireframes/16-sim-your-prediction.png) · [What happened](wireframes/01-sim-what-happened.png) |
| Field Journal | [Home](wireframes/07-journal-home.png) · [Ecosystem](wireframes/03-journal-ecosystem.png) · [Investigations](wireframes/04-journal-investigations.png) · [Investigation detail](wireframes/05-journal-investigation-detail.png) · [Observations](wireframes/06-journal-observations.png) · [Fun facts](wireframes/02-journal-fun-facts.png) |

## Implementation status (M2, 31 Oct)

Which wireframe is built in Unity and whether it runs on live data. Scenes are in `Assets/Scenes`; the simulation HUD is Shu Min's `UI.unity`, loaded on top of `HimalayaSimulation` by `SimHud`, which finds objects **by name** (keep the names when restyling).

| Wireframe | In Unity | Live / wired | Notes |
|---|---|---|---|
| 13 Home | `HomeScreen` | ✅ Start, Field Journal, Quit (`ScreenFlow`) | Settings button not used yet |
| 14 Choose an ecosystem | `EcosystemSelectionScreen` | ✅ Explore, Back | Himalayan Highlands only |
| 15 Guided or free | `ModeSelectionScreen` | ✅ Start Guided / Start Free | Guided → 20-day challenge, Free → Configuration |
| 12 HUD | `UI` → `SimulationPanel`, `DayPanel`, `EcosystemOverviewPanel` | ✅ day, populations, vegetation / water, Play/Pause, speed, Reset, Back | Value icons (`ValueImage`) still placeholders |
| 11 Overview expanded | `UI` → `ExpandedOverviewPanel` | ✅ numbers, trends, Enough / Low, bars | |
| 09 Animal inspection | `UI` → `AnimalInspectionPanel` | ✅ name, sex, baby, Fullness, Hydration, Health, action, Did You Know | Animal portrait placeholder |
| 08 Discovery toast | — | ❌ | Needs the Field Journal investigations (Dillon) |
| 10 Let's think · 16 Your prediction · 01 What happened | `UI` → `ContextualPanel` | ❌ hidden | Prediction questions CQ01–05 (Nomitha) |
| 07 Journal home · 03 Ecosystem · 04 Investigations · 05 Detail · 06 Observations · 02 Fun facts | `FieldJournalScene` | ⚠️ opens over the paused run, Back returns | Content and investigations OG/PP not wired (Dillon) |
| Guided event popup, ending screen, Ending Gallery | built in code (`GuidedChallenge`) | ✅ | No wireframe yet; restyle to match once designed (Shu Min) |
