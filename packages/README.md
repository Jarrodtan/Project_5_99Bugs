# packages/

Reusable code that does not depend on any app.

| Package | What | Used by |
|---|---|---|
| [`ecosim/`](ecosim/) (`com.99bugs.ecosim`) | Simulation core: species/resource data, relationships, `EcosystemSimulator`, `EcoRegistry`, AI data types, with EditMode tests | `apps/mobile/EcoBalance3D` via `Packages/manifest.json` |

Rule: packages never reference app code. App-specific behaviour (UI, island, spawners, animal brains) stays in the app.

---
*Drafted with AI assistance: Claude Code (Claude Opus 5.5, claude-opus-5-5). Reviewed and edited by Tan Wei Leong.*
