# tests/

Unity only compiles test code inside the project, so the automated tests live in:

- **EditMode tests:** `apps/mobile/EcoBalance3D/Assets/_Project/Tests/Editor/`

How to run them:
- **Editor:** open **Window → General → Test Runner → EditMode → Run All**.
- **CI:** they run automatically on every push or PR to `main` (`.github/workflows/unity-ci.yml`).

This folder holds test plans, testbed measurement definitions and manual test checklists.
