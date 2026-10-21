# CI/CD pipeline (GitHub Actions)

Workflow: [`.github/workflows/unity-ci.yml`](../../.github/workflows/unity-ci.yml). Runs on GitHub-hosted runners
(student GitHub Pro: 3,000 min/month). APKs and performance history are stored on the team Gitea VM, so
GitHub artifact storage stays at 0.

## What runs when

| Event | Repo checks | EditMode tests (incl. smoke + perf) | Android APK | Perf history |
|---|---|---|---|---|
| Pull request into `main` | ✅ | ✅ (required to merge) | — | — |
| Push / merge to `main` | ✅ | ✅ | — | — |
| Weekly, Sunday 02:00 SGT (only if `main` changed) | — | ✅ | ✅ release | ✅ |
| Actions → Unity CI → **Run workflow** | ✅ | ✅ | ✅ release **or** development | — |
| Tag `v*` (e.g. `v0.2-m2`) | ✅ | ✅ | ✅ release, kept permanently | — |

Approximate use: ~1,200 of 3,000 minutes/month, 0 GB GitHub storage. An Actions budget of $1 with
"stop usage" means CI pauses rather than charges if the limit is ever reached.

## Where results are

- **Pass/fail:** the PR checks and the Actions tab. Failed tests and Unity errors are printed in the job log.
- **APKs:** Gitea → team21-admin → Packages → `ecobalance-apk` (newest 10 `build-*`; `v*` kept; `-dev` = development build).
- **Performance history:** Gitea → Packages → `ecobalance-perf` → `history/perf.csv` (one row per week: date, commit, timings in ms).

## Rubric mapping (M1 / M2 / M3)

| Rubric item | Level | Covered by |
|---|---|---|
| T4 Automated build: CI checks `main`, reports failures | REQUIRED | Every PR/push; failures shown on the PR |
| T5 Automated checks/tests, team can show results | REQUIRED | EditMode tests (ecosim package + app) |
| CI/Build pipeline: automating builds | REQUIRED | Weekly / tag / manual Android APK |
| …testing on target device/platform | REQUIRED | APK from Gitea installed on the Android tablet |
| …versioned releases | REQUIRED | `v*` tags → permanent APK on Gitea |
| Static analysis build | BASIC | **Repo checks** (`tools/ci/repo-checks.py`): missing/orphan `.meta`, large files outside LFS, merge markers, `UnityEditor` in runtime code |
| Create all builds (debug/release), verify no errors, let the team know | BASIC | Run workflow → release or development APK; failures fail the run |
| Smoke tests | ADVANCED | `SceneSmokeTests`: every build scene opens with no errors or missing scripts; simulator wired to real assets |
| Performance tests with historical representation | ADVANCED | `PerformanceTests` (budgets) + weekly row appended to `perf.csv` |
| Tech Items TI-31 / TI-32 (simulation model, determinism) | Optional | `BalanceModel` + determinism tests |

## Planned (M3)

- **Headless (dedicated server) build run on the VM** for long soak tests (Tomas's suggestion): CI builds a
  Linux headless build; the VM (too small for the Unity editor, fine for a headless player) runs it nightly.
- Self-hosted runner on a team PC as an unlimited fallback if minutes run low.
