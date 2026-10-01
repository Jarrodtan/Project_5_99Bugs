# tests/

Unity only compiles test code inside the project, so the automated tests live in:

- **Simulation core (package):** `packages/ecosim/Tests/Editor/` (simulator, relationships, registry)
- **App:** `apps/mobile/EcoBalance3D/Assets/_Project/Tests/Editor/` (species assets, goal-oriented AI, population counter, resource levels, event feed, inspector tabs, stat list)

How to run them:
- **Editor:** open **Window → General → Test Runner → EditMode → Run All**.
- **CI:** they run automatically on every push or PR to `main` (`.github/workflows/unity-ci.yml`).

This folder holds test plans, testbed measurement definitions and manual test checklists.

---
*Drafted with AI assistance: Claude Code (Claude Opus 5.5, claude-opus-5-5). Reviewed and edited by Tan Wei Leong.*
