# packages/

Reusable code that does not depend on any app.

| Package | What | Used by |
|---|---|---|
| [`ecosim/`](ecosim/) (`com.99bugs.ecosim`) | Simulation core: species/resource data, relationships, `EcosystemSimulator`, `EcoRegistry` (with a spatial hash grid), AI data types, history, cases, balance model, with EditMode tests | `apps/mobile/EcoBalance3D` via `Packages/manifest.json` |
| [`procanim/`](procanim/) (`com.99bugs.procanim`) | Our procedural animation, used instead of Unity's Animator: motion states, springs, legs/head/tail found in static meshes, child-friendly death, data-driven motion profiles, with EditMode tests | `apps/mobile/EcoBalance3D` via `Packages/manifest.json`; linked to the AI by the app's `AnimalMotionBridge` |

Rules: packages never reference app code, and packages don't reference each other (`procanim` knows nothing about `ecosim`). App-specific behaviour (UI, island, spawners, animal brains) and the glue between packages (`AnimalMotionBridge`) stay in the app.

---
*Drafted with AI assistance: Claude Code (Claude Opus 5.5, claude-opus-5-5). Reviewed and edited by Tan Wei Leong.*
