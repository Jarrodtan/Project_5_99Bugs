# UI/UX

Wireframes, user flows and mockups for EcoBalance. Export images as `.png` and link them from here.

## Wireframes

| Area | Screens |
|---|---|
| Menus | [Home](wireframes/13-home-menu.png) · [Choose an ecosystem](wireframes/14-choose-ecosystem.png) · [Guided or free](wireframes/15-explore-mode.png) |
| Simulation HUD | [HUD](wireframes/12-sim-hud.png) · [Overview expanded](wireframes/11-sim-overview-expanded.png) · [Animal inspection](wireframes/09-sim-animal-inspection.png) · [Discovery toast](wireframes/08-sim-discovery-toast.png) |
| Prediction loop | [Let's think](wireframes/10-sim-lets-think.png) · [Your prediction](wireframes/16-sim-your-prediction.png) · [What happened](wireframes/01-sim-what-happened.png) |
| Guided Exploration | [Top bar](wireframes/17-guided-hud-top-bar.png) · [Intro: your ecosystem adventure](wireframes/18-guided-intro.png) · [Event](wireframes/19-guided-event.png) · [Choice feedback](wireframes/20-guided-choice-feedback.png) · [Animal inspection](wireframes/21-guided-animal-inspection.png) · [Overview expanded](wireframes/22-guided-overview-expanded.png) · [Ending](wireframes/23-guided-ending.png) · [Ending Gallery](wireframes/24-ending-gallery.png) · [Gallery detail](wireframes/25-ending-gallery-detail.png) |
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
| 08 Discovery toast | `CaseDiscoveryToast` (Dillon) | ✅ | Pops up when a case is discovered during the run |
| 10 Let's think · 16 Your prediction · 01 What happened | `UI` → `ContextualPanel` | ❌ hidden | Prediction questions CQ01–05 (Nomitha) |
| 07 Journal home · 03 Ecosystem · 04 Investigations · 05 Detail · 06 Observations · 02 Fun facts | `FieldJournalScene` + `FieldJournalScreen` (Dillon) | ✅ investigations, observations graph; opens over the paused run, Back returns | Fun facts DY01–DY10 and the research's OG/PP wording still to add |
| 17 Guided top bar (Back, ecosystem + mode, Day x / 20 + season, Field Journal, Ending Gallery) | `GuidedExploration` top bar | ✅ | Day panel shows day / 20, season, weather and °C on one dark line (readable on the tablet); settings button opens the volume slider |
| 18 Guided intro ("Your Ecosystem Adventure!") | `GuidedExploration` `IntroPanel` | ✅ | Run waits paused until Start Simulation; numbers filled from the real start. Pictures still placeholders |
| 19 Event · 20 Choice feedback | `GuidedExploration` `EventPanel` | ✅ | Pick an option card (turns green), Confirm, then the feedback card, Continue resumes. Option pictures still placeholders |
| 21 Animal inspection · 22 Overview expanded | `UI` panels | ✅ | Arrows for trends and coloured values (red low / green up) still to style |
| 23 Ending · 24 Ending Gallery · 25 Gallery detail | `EndingScreen`, `EndingGallery` (`GuidedScreens`) | ✅ | Filled from the run; gallery cards Locked / Unlocked, tap for the detail page, arrows move between endings. Left: overview arrows don't scroll (5th card off-screen) |
