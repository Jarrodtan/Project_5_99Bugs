# CI/CD pipeline (GitHub Actions)

Workflow: [`.github/workflows/unity-ci.yml`](../../.github/workflows/unity-ci.yml). Runs on GitHub-hosted runners
(student GitHub Pro: about $18 of compute a month, reset on the 1st). APKs and performance history are stored on the team Gitea VM, so
GitHub artifact storage stays at 0.

## What runs when

| Event | Repo checks | EditMode tests (incl. smoke + perf) | Android APK | Linux headless build | Perf history |
|---|---|---|---|---|---|
| Pull request into `main` | ✅ | ✅ | — | — | ✅ |
| Actions → Unity CI → **Run workflow** | ✅ | ✅ | ✅ release **or** development | if `headless` ticked | — |
| Tag `v*` (e.g. `v0.2-m2`) | ✅ | ✅ | ✅ release, kept permanently | ✅ | ✅ |

**No nightly run (removed 2 Nov).** The team merges into `M2-Integration`, and everything reaches `main`
through a PR, which runs the same tests, so a nightly run only re-tested the same code. Before opening
the PR, the integrator plays the merged branch in Unity (compile errors show there first). From 22 to
31 Oct the tests ran only nightly, to stay inside the minutes (October used $15 of $18, most of it APK
uploads before they moved to Gitea).

**Minutes per run:** repo checks 1, EditMode tests ~12, Android APK ~50, + headless ~20 (each job rounded
up to a whole minute, about $0.006 a minute). A month in M2 (about 6 PRs, 4 manual APKs, 1 release tag)
is roughly 80 + 200 + 70 = **~350 of 3,000 minutes (~12%, ~$2 of the $18), so $0 to pay.** An Actions budget of $1 with
"stop usage" means CI pauses rather than charges if the limit is ever reached.

## Where results are

- **Pass/fail:** the PR checks and the Actions tab. Failed tests and Unity errors are printed in the job log.
- **APKs:** Gitea → team21-admin → Packages → `ecobalance-apk` (newest 10 `build-*`; `v*` kept; `-dev` = development build).
- **Headless build (for the VM):** Gitea → Packages → `ecobalance-headless` → `latest/EcoBalanceHeadless.tar.gz`.
- **Soak test results:** Gitea → Packages → `ecobalance-soak` (one report per run + `history/soak.csv`).
- **Performance history:** Gitea → Packages → `ecobalance-perf` → `history/perf.csv` (one row per PR or tag run: date, commit, timings in ms; includes the spatial grid and animation benchmarks).

## Rubric mapping (M1 / M2 / M3)

| Rubric item | Level | Covered by |
|---|---|---|
| T4 Automated build: CI checks `main`, reports failures | REQUIRED | Repo checks + EditMode tests on every PR into `main` and every `v*` tag |
| T5 Automated checks/tests, team can show results | REQUIRED | EditMode tests (ecosim package + app) |
| CI/Build pipeline: automating builds | REQUIRED | Tag / manual Android APK |
| …testing on target device/platform | REQUIRED | APK from Gitea installed on the Android tablet |
| …versioned releases | REQUIRED | `v*` tags → permanent APK on Gitea |
| Static analysis build | BASIC | **Repo checks** (`tools/ci/repo-checks.py`): missing/orphan `.meta`, large files outside LFS, merge markers, `UnityEditor` in runtime code |
| Create all builds (debug/release), verify no errors, let the team know | BASIC | Run workflow → release or development APK; failures fail the run |
| Smoke tests | ADVANCED | `SceneSmokeTests`: every build scene opens with no errors or missing scripts; simulator wired to real assets |
| Performance tests with historical representation | ADVANCED | `PerformanceTests` (budgets) + one row per PR/tag run appended to `perf.csv` |
| Stability / soak tests ("test the build", Tomas's VM guide) | ADVANCED | Headless Linux build (built on tags / on demand) soak-tested weekly on the team VM (`HeadlessSoakRunner` + `tools/vm/soak.sh`), history in `soak.csv` |
| Tech Items TI-31 / TI-32 (simulation model, determinism) | Optional | `BalanceModel` + determinism tests |

## Headless soak test on the VM

The VM (3.7 GB RAM) can't run the Unity **editor**, but it can run a **headless player**: the game built for Linux and
started with no graphics. CI builds it on `v*` tags and on demand (Run workflow, tick `headless`) and publishes it to Gitea; the VM downloads it, runs the simulation for
2,000 simulated days, and records crashes, errors, memory and final populations.

- **Game side:** `Assets/EcoBalance/Scripts/DevTools/HeadlessSoakRunner.cs` only activates when started with `-soakDays N`.
  Exit code 0 = passed, 1 = errors logged, 2 = no simulator, 3 = timed out. Report: JSON.
- **Run locally on Linux:** `./EcoBalanceHeadless.x86_64 -batchmode -nographics -soakDays 200 -soakReport report.json`

**One-time VM setup** (SSH in as `team21`):

```bash
sudo apt-get install -y curl tar coreutils
mkdir -p ~/soak && cd ~/soak
curl -fsSL -o soak.sh https://raw.githubusercontent.com/Jarrodtan/Project_5_99Bugs/main/tools/vm/soak.sh   # or copy it with MobaXterm
chmod +x soak.sh
nano ~/.ecobalance-token        # paste a Gitea token with package read/write, save
chmod 600 ~/.ecobalance-token
./soak.sh                       # first run by hand (needs a headless build on Gitea first)
crontab -e                      # add the line below: every Sunday 05:00 SGT (tests the newest headless build)
# 0 21 * * 6 $HOME/soak/soak.sh >> $HOME/soak/cron.log 2>&1
```

The repository is private, so the `raw.githubusercontent.com` download needs a GitHub token; copying `tools/vm/soak.sh`
over with MobaXterm/scp is simpler. The VM clock is UTC, so `0 21 * * 6` (Saturday 21:00 UTC) is Sunday 05:00 SGT.

## Other options

- Self-hosted runner on a team PC as an unlimited fallback if minutes run low (GitHub → Settings → Actions → Runners → New self-hosted runner; then `runs-on: self-hosted` in the workflow).
