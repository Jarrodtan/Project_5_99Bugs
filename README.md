# EcoBalance

**Team:** 99 Bugs · CSD3401 / UXG3400, Fall 2026

EcoBalance is an interactive, educational 3D ecosystem simulation. Players set up a small ecosystem
(grass → blue sheep → snow leopard), run it, and watch how populations and resources change each other over time.

## Documentation

| Document | Contents |
|---|---|
| [`docs/proposal.md`](docs/proposal.md) | Concept, problem, target users, player experience |
| [`docs/design/product-design.md`](docs/design/product-design.md) | Team, user journey, MVP, M1 feature plan |
| [`docs/design/ui-ux/`](docs/design/ui-ux/) | Wireframes, user flows, mockups |
| [`docs/architecture.md`](docs/architecture.md) | System architecture, diagram, components/owners, repo & module structure |
| [`docs/deliverables.md`](docs/deliverables.md) | Technical deliverables, ownership, M1/M2/M3 targets |
| [`docs/toolchain.md`](docs/toolchain.md) | Technology stack and why |
| [`docs/data-pipeline.md`](docs/data-pipeline.md) | How content/data is authored, naming, folder rules |
| [`docs/infra/gitea-rebuild.md`](docs/infra/gitea-rebuild.md) | Self-hosted Gitea server: security incident, rebuild, lessons learned |
| [`docs/infra/ci.md`](docs/infra/ci.md) | CI/CD: what runs when, where APKs and results are, rubric mapping |

## Technology Stack

- **Engine:** Unity 6000.6.0f1 (URP) · **Language:** C# · **Target:** Android (mobile)
- **Tests:** Unity Test Framework (EditMode) · **CI:** GitHub Actions + GameCI (repo checks, unit/smoke/performance tests, Android build), details in [`docs/infra/ci.md`](docs/infra/ci.md)
- **Tracking:** Jira · Details: [`docs/toolchain.md`](docs/toolchain.md)

## Repository Structure

```
Project_5_99Bugs/
├── README.md
├── docs/
│   ├── proposal.md
│   ├── design/
│   │   ├── product-design.md
│   │   └── ui-ux/                 # wireframes, user flows, mockups
│   ├── architecture.md            # system architecture + technology stack
│   ├── deliverables.md            # ownership + M1/M2/M3 targets
│   ├── toolchain.md
│   ├── data-pipeline.md
│   └── infra/                    # gitea-rebuild.md (VM/Gitea), ci.md (CI/CD)
├── apps/
│   └── mobile/
│       └── EcoBalance3D/          # Unity project (scenes, art, audio, prefabs, app scripts, app tests)
├── packages/
│   └── ecosim/                    # Unity package com.99bugs.ecosim: simulation core + tests
├── tools/
│   ├── unity.sh                   # open / test / build / clean / zip (Ubuntu, macOS, Git Bash)
│   └── unity.bat                  # same commands for Windows cmd
├── tests/                         # test plans and manual checklists
├── .github/workflows/unity-ci.yml # CI: repo checks, EditMode tests, Android build (docs/infra/ci.md)
├── .gitignore  .gitattributes  .editorconfig
```

The app loads the simulation core from `packages/ecosim` through
`apps/mobile/EcoBalance3D/Packages/manifest.json` (`"com.99bugs.ecosim": "file:../../../../packages/ecosim"`),
so always clone the **whole repository**.

## Setup

Supported development environments: **Windows 11, macOS Tahoe, Ubuntu 24.04**.

1. Install [Git](https://git-scm.com/) and [Git LFS](https://git-lfs.com/).
2. Install [Unity Hub](https://unity.com/download):
   - **Windows / macOS:** installer from the Unity website.
   - **Ubuntu 24.04:** follow Unity's [Linux Hub install guide](https://docs.unity3d.com/hub/manual/InstallHub.html#install-hub-linux) (apt repository), then `unityhub`.
3. In Unity Hub → **Installs → Install Editor**, choose **6000.6.0f1** and tick **Android Build Support** (with OpenJDK and Android SDK & NDK).
4. Clone:
   ```bash
   git clone https://github.com/Jarrodtan/Project_5_99Bugs.git
   cd Project_5_99Bugs
   git lfs pull
   ```
   Large binary assets (images, audio, models, fonts, heightmaps) are stored with **Git LFS on our
   self-hosted Gitea** (`https://team21.dp-ext8.com/git/`), not on GitHub. The first LFS download or
   upload asks for your **Gitea** username and password; Windows remembers it after that.
   Accounts are created by the tech lead (sign-up is off).
5. Unity Hub → **Add → Add project from disk** → select **`apps/mobile/EcoBalance3D`**.
   The first open imports everything (a few minutes).

## Run

Open `Assets/Scenes/HomeScreen` and press **Play**. From the home screen go to the configuration screen, set the starting numbers of blue sheep and snow leopards, and press **Start**.
Use **Play / Pause / Reset** and **x1 / x2 / x4 / x8** to control the simulation.

The built app starts from the first scene in Build Settings (HomeScreen).

## Test

- **Editor:** **Window → General → Test Runner → EditMode → Run All** (app tests + `com.99bugs.ecosim` package tests).
- **Command line** (Editor closed):
  - Ubuntu / macOS / Git Bash: `tools/unity.sh test`
  - Windows: `tools\unity.bat test`

  Results are written to `test-results.xml`.
- **CI:** every pull request runs the repo checks (`python tools/ci/repo-checks.py`, also runnable locally). The EditMode tests (including scene smoke tests and performance tests) run **nightly** on `main` (02:00 SGT, only if `main` changed) to save Actions minutes, so run them in the Unity Test Runner before opening a PR. Failures are printed in the job log.

## Build

- **Editor:** **File → Build Profiles → Android → Build**.
- **Command line** (Editor closed): `tools/unity.sh build android` or `tools\unity.bat build android`
  (also `windows`, and `macos` / `linux` with `unity.sh`). Output: `apps/mobile/EcoBalance3D/Build/<Platform>/`.
- **CI:** the Android APK is built for every `v*` tag and on demand via **Actions → Unity CI → Run workflow** (choose `release` or `development`; development includes the debug overlay).
  APKs are stored on the team Gitea, not GitHub: **Gitea → team21-admin → Packages → `ecobalance-apk`** (newest 10 `build-*` versions kept; `v*` tags kept).
  The run's GitHub summary links to the APK. Tag `main` (e.g. `v0.2-m2`) for a versioned milestone release.

CI needs these repository secrets: `UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD`
(serial-based Unity licence; see the [GameCI activation guide](https://game.ci/docs/github/activation)),
`GITEA_LFS_TOKEN` (LFS pull) and `GITEA_PACKAGE_TOKEN` (APK upload; Gitea token with package read/write).
On `v*` tags (and on demand with `headless` ticked) CI also builds a **Linux headless** version that the team VM soak-tests (`tools/vm/soak.sh`, results on Gitea → Packages → `ecobalance-soak`).
APKs and performance history go to Gitea, so GitHub artifact storage stays at 0 (it filled up in October 2026 and locked Actions).
CI uses about 1,400 of the 3,000 student-Pro minutes per month; an Actions budget with "stop usage" makes it pause rather than charge.

## Clean

`tools/unity.sh clean` or `tools\unity.bat clean` (menu option 5) deletes the Unity cache (`Library`, `Temp`, `Logs`, `obj`, `Build`)
and every other Git-ignored file (`UserSettings`, `.vs`, `.vscode`, `*.csproj`, `*.slnx`), leaving the folder like a fresh clone.
Committed files, new uncommitted work and `.git` are kept. The next open re-imports everything and resets your Unity layout.

**Submission zip:** `tools\unity.bat zip` (or menu option 6) or `tools/unity.sh zip` packs only the committed files (about 80 MB) into `EcoBalance_TechFiles.zip` next to the repo folder. You do not need to run Clean first.

Generated files, IDE folders, build output, logs and secrets are excluded by `.gitignore`; never commit them.

## Workflow

- Work on your personal branch, merge `main` into it often, and open a **pull request into `main`**.
- `main` is protected: merge only after the **EditMode tests** check passes. Never push directly to `main`.
- Rename or move Unity files **inside the Unity Editor** so their `.meta` files move with them.
- Jira tracks the work; this repository holds the implementation and evidence.
- **Access:** all 9 team members and the instructor are collaborators on the GitHub repository.
- **Code freeze:** before each milestone, `main` is frozen (M1: Sat 3 Oct 2026, 5pm). After the freeze, only bug fixes go in, through a reviewed pull request with passing tests.
- **Submission build:** the milestone zip is made from frozen `main` with `tools\unity.bat zip` / `tools/unity.sh zip`, so it matches exactly what was reviewed.
