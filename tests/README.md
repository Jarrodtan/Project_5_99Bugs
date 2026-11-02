# tests/

Unity only compiles test code inside the project, so the automated tests live in:

- **Simulation core (package):** `packages/ecosim/Tests/Editor/` (simulator, relationships, registry and spatial grid, population history, investigation cases, balance model, performance)
- **Procedural animation (package):** `packages/procanim/Tests/Editor/`
- **App:** `apps/mobile/EcoBalance3D/Assets/EcoBalance/Tests/Editor/` (species assets, goal-oriented AI, population counter, resource levels, event feed, inspector tabs, stat list, scene smoke tests, tooltip placement, Field Journal, case assets)

How to run them:
- **Editor:** open **Window → General → Test Runner → EditMode → Run All**.
- **CI:** they run on every pull request into `main` and nightly on the integration branch (`.github/workflows/unity-ci.yml`, see `docs/infra/ci.md`).

This folder holds test plans, testbed measurement definitions and manual test checklists.

---
*Drafted with AI assistance: Claude Code (Claude Opus 5.5, claude-opus-5-5). Reviewed and edited by Tan Wei Leong.*
