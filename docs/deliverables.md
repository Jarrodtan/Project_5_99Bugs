# EcoBalance — Deliverables & Ownership

*Owners follow the M1 Technical Deliverable plan (Txx in brackets). "Unassigned" rows need an owner, so raise them at the next team meeting.*

| Deliverable                        | Owner | M1 Target                                            | M2 Target                                       | M3 Target                                 |
| ---------------------------------- | ----- | ---------------------------------------------------- | ----------------------------------------------- | ----------------------------------------- |
| Entity Data System                 | Li Juan, Yusuf (T02) | Working data model, 2 test species                   | Full species/resource set defined               | Documented, stable                        |
| Relationship System                | Zhi Jie (T01) | Basic eats/needs links functional                    | Full relationship graph                         | Stable                                    |
| Individual Agent System            | Yusuf | Per-agent hunger/thirst/health tracked               | Inspectable via tap interaction                 | Polished                                  |
| Animal AI System                   | Zhi Jie, Reginald, Yusuf (T07) | Basic seek/flee behaviour                            | Full state machine (idle/graze/flee/hunt)       | Tuned and polished                        |
| Plants/Resources System            | Wei Leong (T03) | Basic depletion/regeneration                         | Full resource loop integrated                   | Balanced/tuned                            |
| Ecosystem Simulation Core          | Wei Leong, Reginald (T03) | Tick loop functional with mock data                  | Full integration across all systems             | Stable, performant                        |
| Simulation Data Tracking           | Wei Leong (population history), Li Juan (T05 CSV export); display: Yan De, Dillon (T04) | Basic tick recording                                 | Per-day history recorded; feeds real UI graph (done: `PopulationHistory`) | Polished, CSV export |
| Contextual Trigger System          | Wei Leong (engine), Dillon (UI), design (case content) | Basic threshold detection                            | Investigation cases unlock in Free Exploration (done: `CaseDetector`); fires real UI prompts | Tuned, full case list |
| Simulation Time Control            | Reginald (T03) | Play/pause functional                                | Speed control added                             | Full reset/reconfigure                    |
| Visualization/Feedback System      | Wei Leong | Placeholder agents spawn/despawn by population count | Real 3D models (from UXGD design) + animations wired to AI state | Polished, environmental feedback complete |
| UI/UX (Inspector, Graphs, Prompts) | Dillon, Nomitha, Shu Min | Wireframes complete                                  | Functional UI wired to real simulation data     | Polished                                  |
| 3D Environment/Assets              | Terrain: Yusuf, Li Juan (data); river/meadows/water: Wei Leong (M2 split, 22 Oct) | Placeholder biome                                    | Real Himalayan environment assets               | Polished                                  |
| Animal Models/Animations           | Models: Nomitha, Li Juan; animation: Wei Leong (SCRUM-267) | Placeholder models                                   | Real sheep/leopard models + core animation set (done: our procedural animation module `packages/procanim`: walk/run/flee/stalk, eat/drink, alert, legs/head/tail, child-friendly death) | Full animation set complete               |
| CI/CD pipeline                     | Wei Leong (#4, #5) | Tests + Android build on every PR | Repo checks on every PR; smoke + performance tests nightly on `main`; APK on tags/manual to Gitea (`docs/infra/ci.md`) | Headless soak test on the VM, performance history |

## M2 open tasks by person (from the 22 Oct task split, updated 26 Oct)

Team: 2 UXGD (Shu Min, PM; Nomitha, Design Lead), 6 IMGD programmers, 1 RTIS (Wei Leong, Tech Lead). Integration build: branch `M2-Integration`.

| Person | Role | M2 focus | Still open |
| --- | --- | --- | --- |
| Shu Min | UXGD, PM | Game flow, wireframes, player goals (Tomas: "the player has little to do") | Confirm M2 target screens; HUD layout to mock-up (with Dillon) |
| Nomitha | UXGD, Design Lead | Visual design, investigation case content | 2 missing case hints; final colours for HUD and animal card |
| Dillon | IMGD | Field Journal UI | Journal screen + "new case" popup on the case engine; HUD re-anchoring for tablet |
| Yan De | IMGD | Camera | Follow / POV / Back / Zoom buttons on `TerrainCameraController`; test on a real tablet |
| Zhi Jie | IMGD | Utility AI + reproduction | Gender, leopard litters of 2–3, sheep herding |
| Reginald | IMGD | AI + breeding with Zhi Jie, time control | Population balance across seasons (sheep / leopard cycles) |
| Yusuf | IMGD | Terrain generation | Real tree model (tree slot holds a grass prefab), remove the 3 stray `New Terrain` assets |
| Li Juan | IMGD | Terrain / weather / live animal data, audio | Move the weather API key out of the code; CSV export (T05) |
| Wei Leong | RTIS, Tech Lead | Animation, engine systems, integration, CI | PR `M2-Integration` to `main`; courtship nuzzle animation; tablet build + test session (1–2 Nov) |
