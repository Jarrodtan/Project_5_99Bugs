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
| Simulation Data Tracking           | Wei Leong (population history), Li Juan (T05 CSV export); display: Yan De, Dillon (T04) | Basic tick recording                                 | Per-day history recorded; feeds real UI graph (done: `PopulationHistory`) | Polished, CSV export |
| Guided Exploration                 | Wei Leong (tech), Shu Min (design) | —                                                    | 20-day challenge, events, endings, gallery (done) | More events, balanced endings             |
| Contextual Trigger System          | Wei Leong (engine), Dillon (UI), design (case content) | Basic threshold detection                            | Investigation cases unlock in Free Exploration (done: `CaseDetector`); fires real UI prompts | Tuned, full case list |
| Simulation Time Control            | Reginald (T03) | Play/pause functional                                | Speed control added (done; 2 real minutes per day, seasons of 5 days) | Full reset/reconfigure                    |
| Visualization/Feedback System      | Wei Leong | Placeholder agents spawn/despawn by population count | Real 3D models (from UXGD design) + animations wired to AI state | Polished, environmental feedback complete |
| UI/UX (Inspector, Graphs, Prompts) | Dillon, Nomitha, Shu Min; wiring: Wei Leong | Wireframes complete                                  | Functional UI wired to real simulation data (done: menu flow + HUD; open: prediction prompts CQ01–05, Field Journal UI) | Polished                                  |
| 3D Environment/Assets              | Terrain: Yusuf, Li Juan (data); river/meadows/water: Wei Leong (M2 split, 22 Oct) | Placeholder biome                                    | Real Himalayan environment assets (done: real Khumbu terrain, glacier river + streams, meadows) | Polished                                  |
| Animal Models/Animations           | Models: Nomitha, Li Juan; animation: Wei Leong (SCRUM-267) | Placeholder models                                   | Real sheep/leopard models + core animation set (done: our procedural animation module `packages/procanim`: walk/run/flee/stalk, eat/drink, alert, legs/head/tail, child-friendly death) | Full animation set complete               |
| CI/CD pipeline                     | Wei Leong (#4, #5) | Tests + Android build on every PR | Repo checks + tests on PRs into `main`; tests nightly on `M2-Integration`; APK on tags/manual to Gitea (`docs/infra/ci.md`) | Headless soak test on the VM, performance history |

## M2 status and open tasks (updated 31 Oct)

Team: 2 UXGD (Shu Min, PM; Nomitha, Design Lead), 6 IMGD programmers, 1 RTIS (Wei Leong, Tech Lead). Integration build: branch `M2-Integration` (merge into it, not `main`, until the submission).

**Done for M2:** both modes playable from HomeScreen (Guided and Free), Shu Min's HUD with live data, research simulation numbers with HP, realistic Khumbu world, hunting / perception / herd behaviour, procedural animation, CI.

| Person | Role | Open for M2 |
| --- | --- | --- |
| Shu Min | UXGD, PM | Icons for the HUD value boxes; style the Guided event popup, ending screen and gallery; update `docs/design/product-design.md` for M2 |
| Nomitha | UXGD, Design Lead | Prediction questions CQ01–05 in the HUD's `ContextualPanel` (SCRUM-46); Did You Know facts DY01–DY10 (SCRUM-18) |
| Dillon | IMGD | Field Journal UI done (investigations, observations graph, discovery popup, merged 2 Nov); left: match the investigations to OG01–03 / PP01–03 from the research doc (SCRUM-277) |
| Yan De | IMGD | IslandPlatform HUD done (PR #54); left: check the FAB, tooltips and camera inside HimalayaSimulation, test on a real tablet; merge into `M2-Integration`, not `main` |
| Zhi Jie | IMGD | Play Guided 5+ times, record which ending each run gets; check every ending can be reached (SCRUM-256) |
| Reginald | IMGD | Tune `AnimalPerception` and hunting at 8x; report balance |
| Yusuf | IMGD | Hunting camouflage done (merged 2 Nov); left: pink terrain bug after terrain edits |
| Li Juan | IMGD | Merge `M2-Integration` into `lijuan_2`, fix the 3 conflicts (animal prefabs, HimalayaSimulation), delete the `._` Mac files; audio into M2 |
| Wei Leong | RTIS, Tech Lead | Review and merge the team's work into `M2-Integration`; PR `M2-Integration` → `main`; release tag `v0.2-m2` (SCRUM-262) |
