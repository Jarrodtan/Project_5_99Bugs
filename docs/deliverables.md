# EcoBalance — Deliverables & Ownership

*Owners follow the M1 Technical Deliverable plan (Txx in brackets). "Unassigned" rows need an owner, so raise them at the next team meeting.*

| Deliverable                        | Owner | M1 Target                                            | M2 Target                                       | M3 Target                                 |
| ---------------------------------- | ----- | ---------------------------------------------------- | ----------------------------------------------- | ----------------------------------------- |
| Entity Data System                 | Li Juan, Yusuf (T02) | Working data model, 2 test species                   | Full species/resource set defined               | Documented, stable                        |
| Relationship System                | Zhi Jie (T01) | Basic eats/needs links functional                    | Full relationship graph                         | Stable                                    |
| Individual Agent System            | Yusuf, Wei Leong | Per-agent hunger/thirst/health tracked               | Inspectable via tap interaction (done: Fullness, Hydration, HP from the research, shown in the HUD inspection panel) | Polished                                  |
| Animal AI System                   | Zhi Jie, Reginald, Yusuf (T07); Wei Leong (integration) | Basic seek/flee behaviour                            | Full state machine (done: utility AI, stalk/sprint hunting, perception raycasts, herd alarm, danger memory, male/female breeding) | Tuned and polished                        |
| Plants/Resources System            | Wei Leong (T03) | Basic depletion/regeneration                         | Full resource loop integrated                   | Balanced/tuned                            |
| Ecosystem Simulation Core          | Wei Leong, Reginald (T03) | Tick loop functional with mock data                  | Full integration across all systems             | Stable, performant                        |
| Simulation Data Tracking           | Wei Leong (population history), Li Juan (T05 CSV export); display: Yan De, Dillon (T04) | Basic tick recording                                 | Per-day history recorded; feeds real UI graph (done: `PopulationHistory`) | Polished, CSV export (done in M2: `DataLogging`, one row per tick) |
| Guided Exploration                 | Wei Leong (tech), Shu Min (design) | —                                                    | 20-day challenge, events, endings, gallery (done, on Shu Min's Guided HUD, ending and gallery screens) | More events, balanced endings             |
| Contextual Trigger System          | Wei Leong (engine), Dillon (UI), design (case content) | Basic threshold detection                            | Investigation cases unlock in Free Exploration (done: `CaseDetector`); fires real UI prompts | Tuned, full case list |
| Simulation Time Control            | Reginald (T03) | Play/pause functional                                | Speed control added (done; 1 real minute per day, seasons of 5 days) | Full reset/reconfigure                    |
| Visualization/Feedback System      | Wei Leong | Placeholder agents spawn/despawn by population count | Real 3D models (from UXGD design) + animations wired to AI state | Polished, environmental feedback complete |
| UI/UX (Inspector, Graphs, Prompts) | Dillon, Nomitha, Shu Min; wiring: Wei Leong | Wireframes complete                                  | Functional UI wired to real simulation data (done: menu flow + HUD, Field Journal, prediction questions CQ01–05 and the mascot, 4 Nov) | Polished                                  |
| 3D Environment/Assets              | Terrain: Yusuf, Li Juan (data); river/meadows/water: Wei Leong (M2 split, 22 Oct) | Placeholder biome                                    | Real Himalayan environment assets (done: real Khumbu terrain, glacier river + streams, meadows) | Polished                                  |
| Animal Models/Animations           | Models: Nomitha, Li Juan; animation: Wei Leong (SCRUM-267) | Placeholder models                                   | Real sheep/leopard models + core animation set (done: our procedural animation module `packages/procanim`: walk/run/flee/stalk, eat/drink, alert, swim (crossing fords, 3 Nov), legs/head/tail, child-friendly death) | Full animation set complete               |
| CI/CD pipeline                     | Wei Leong (#4, #5) | Tests + Android build on every PR | Repo checks + tests (+ perf history) on PRs into `main`, no nightly run; APK on tags/manual to Gitea (`docs/infra/ci.md`) | Headless soak test on the VM, performance history |

## M3 status and open tasks (updated Thu 26 Nov)

**Schedule:** feature freeze **Fri 27 Nov, 11:59pm** → Sat 28 Nov: `M3-Integration` → `main` (one reviewed PR), final build + zip `GAM300_M3_Team21_...` → live presentation **Mon 30 Nov**.

Integration build: branch `M3-Integration` (teammates' branches merged in, only the final PR to `main` is reviewed). Do **not** merge `Nomitha` / `Nomitha-new` / `Nomitha2` as whole branches: they contain a revert of `M3-Integration` that would delete M3 code; their assets were brought in by commit.

**Done for M3:** Guided as the main mode with a named family (Miloy the Blue Sheep / Snowy the Snow Leopard), 4 seasonal chapters, Save & Exit / resume, 3 camera views, thought bubbles, player actions (fire, planting), weather (rain, snow, heatwave) with visuals, 6x real Khumbu map with UXGD trees by real land-cover, offline elevation tiles, faster load, recorded VO + AI placeholders, past runs with stars in the Field Journal, Sound Settings (Music / Voice / Sound Effects), demo hotkeys. Details: [architecture.md, section 3b](architecture.md).

| Person | Role | Open for M3 |
| --- | --- | --- |
| Shu Min | UXGD, PM | Rename Milo → Miloy in AnimalSelection / MeetAnimal scenes; design doc PDF: ages 7–9, Miloy; event library feasibility with tech |
| Nomitha | UXGD, Design Lead | Re-record "Milo" lines as "Miloy" if time; realistic grass / rock / snow textures (M4) |
| Dillon | IMGD | Endings + Restart Story (SCRUM-376) and Achievements (SCRUM-377) moved to **M4** (agreed with Shu Min 26 Nov; the doc's achievement list is not final yet); Field Journal alignment also M4. M3 keeps the existing Guided ending screens + Ending Gallery |
| Zhi Jie | IMGD | Focal animal protection rule (SCRUM-381) |
| Reginald | IMGD | Done: flocking, hotkeys 1–5; check hunting / pathing with trees (trunks are Not Walkable) |
| Yan De | IMGD | Done: camera modes, thought bubbles |
| Yusuf | IMGD | Done: bigger map |
| Li Juan | IMGD | Done: CSV outside Assets, past runs + stars, mascot button SFX, rain / snow effects, spatial audio |
| Wei Leong | RTIS, Tech Lead | Done: chapters, save, fire, planting, family wiring, VO wiring, trees, offline tiles, NavMesh speed-up, Sound Settings. Left: tablet test, PR → `main`, final build + zip |

## M2 status and open tasks (updated 7 Nov)

**Schedule:** code freeze **Fri 6 Nov, 11:59pm** → Sat 7 Nov: `M2-Integration` → `main` (reviewed PR), `M2-Submission` branch, tag `v0.2-m2` (APK), tablet test, recordings (demo 5 min, engine 5 min, presentation) → submission **Sun 8 Nov**.

Team: 2 UXGD (Shu Min, PM; Nomitha, Design Lead), 6 IMGD programmers, 1 RTIS (Wei Leong, Tech Lead). Integration build: branch `M2-Integration` (merge into it, not `main`, until the submission).

**Done for M2:** both modes playable from HomeScreen (Guided and Free), Shu Min's HUD with live data (Guided on her own Guided screens), CSV export, animal and river audio, volume slider, research simulation numbers with HP, realistic Khumbu world, hunting / perception / herd behaviour, procedural animation, CI.

| Person | Role | Open for M2 |
| --- | --- | --- |
| Shu Min | UXGD, PM | Guided HUD, intro, event, feedback, ending and gallery screens done and wired (merged 2 Nov); left: icons and pictures (grey/green placeholders), gallery scrolling (5th card off-screen), Home screen design, `docs/design/product-design.md` for M2 |
| Nomitha | UXGD, Design Lead | Prediction questions CQ01–05 are in the game (wired 4 Nov from the research doc): playtest the wording; Did You Know facts DY01–DY10 (SCRUM-18); icons for the HUD and cards |
| Dillon | IMGD | Field Journal UI done (investigations, observations graph, discovery popup, merged 2 Nov); left: match the investigations to OG01–03 / PP01–03 from the research doc (SCRUM-277) |
| Yan De | IMGD | IslandPlatform HUD done (PR #54); left: check the FAB, tooltips and camera inside HimalayaSimulation, test on a real tablet; merge into `M2-Integration`, not `main` |
| Zhi Jie | IMGD | Play Guided 5+ times, record which ending each run gets; check every ending can be reached (SCRUM-256) |
| Reginald | IMGD | Tune `AnimalPerception` and hunting at 8x; report balance |
| Yusuf | IMGD | Hunting camouflage done (merged 2 Nov); left: pink mountains (shows in Guided and Free, fix before the recording) |
| Li Juan | IMGD | CSV data logging + animal / river audio merged 2 Nov (her own commits, not the old branch); left: Tree column is 0 on the Khumbu terrain, finish the stream audio. Start new work from `M2-Integration` |
| Wei Leong | RTIS, Tech Lead | Done 7 Nov: polish from playtesting: Back before Start in both modes, Guided intro blocks the run until Start (mascot, Settings, Back still usable), animal icons in the HUD and Guided intro, Enough / Low / Very low pills with colours, 22 px minimum text (menus + Guided HUD, Mode Selection re-laid out), opening prediction CQ00 in every Free run, Free +/- fixed (was +2), Shu Min's 7 Nov UI edits merged. Done 5 Nov: merged Shu Min's buttons UI, Zhi Jie's mascot, Nomitha's leopard animation; river fords (SCRUM-320); crowded-run freeze fix + Free max 50 (SCRUM-321); reverted PR #61 on `main` (FAB emptied the world; Yande's water fix kept). Left: PR `M2-Integration` → `main`, `M2-Submission` + tag `v0.2-m2` (SCRUM-262), engine video |

---
*Drafted with AI assistance: Claude Code (Claude Opus 5.5, claude-opus-5-5). Reviewed and edited by Tan Wei Leong.*
