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
| 3D Environment/Assets              | Yan De | Placeholder biome                                    | Real Himalayan environment assets               | Polished                                  |
| Animal Models/Animations           | Nomitha, Li Juan | Placeholder models                                   | Real sheep/leopard models + core animation set  | Full animation set complete               |
| CI/CD pipeline                     | Wei Leong (#4, #5) | Tests + Android build on every PR | Repo checks, smoke + performance tests, weekly APK to Gitea (`docs/infra/ci.md`) | Headless soak test on the VM, performance history |
