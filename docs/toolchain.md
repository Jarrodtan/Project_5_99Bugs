# Toolchain Decision Document

This document is the baseline for EcoBalance's technology choices. **Any change requires instructor approval.**

| Area | Decision | Reason |
|---|---|---|
| Engine | Unity **6000.6.0f1** (Unity 6) | The team knows Unity well; it has strong 3D, mobile deployment and ScriptableObject-based data authoring. |
| Render pipeline | Universal Render Pipeline (URP) 17.6.0 | Built for mobile performance while still supporting stylised 3D visuals. |
| Language | C# | Unity's scripting language, and all team programmers use it. |
| Target platform | Mobile (Android first) | The product is an educational app for mobile users. Android builds can be made in CI on Linux runners without extra hardware. |
| Input | Unity Input System 1.20.0 | Handles touch and mouse input the same way, so we can test in the Editor and play on mobile. |
| UI | uGUI 2.6.0 + TextMesh Pro | A mature, well-documented runtime UI for HUD and population displays. |
| AI navigation | AI Navigation 2.0.14 (NavMesh) | Pathfinding for animal agents (wander, flee, hunt). |
| Testing | Unity Test Framework 1.8.0 (NUnit, EditMode) | Tests the simulation logic deterministically without entering Play mode. |
| Version control | Git + GitHub (`Project_5_99Bugs`), feature branches → PR → `main` | Industry standard, and it integrates with GitHub Actions CI. |
| Large files | Git LFS on self-hosted Gitea (`https://team21.dp-ext8.com/git/`) | Keeps binary assets (models, textures, audio, heightmaps) off GitHub; the team controls the storage. |
| CI/CD | GitHub Actions + GameCI (`unity-test-runner`, `unity-builder`) | Free for our repo. Runs the tests and makes an Android build on every push or PR to `main`. |
| Task tracking | Jira (SCRUM project) | Epics for each feature (Fxx) and technical deliverable (Txx), plus stories with acceptance criteria. |
| IDEs | Visual Studio / JetBrains Rider | Both have official Unity integration packages in the project. |
| AI tools | Unity AI Assistant, Claude Code | Used to help with code, scaffolding and documentation. All output is reviewed by a team member before merge. |
| Data format | Unity ScriptableObject assets (`.asset`, YAML) | Designers can add or tune species and resources without code changes (T02). |
| Exported data | CSV | Population metrics export (T05), which opens in Excel or Sheets. |

---
*Drafted with AI assistance: Claude Code (Claude Opus 5.5, claude-opus-5-5). Reviewed and edited by Tan Wei Leong.*
