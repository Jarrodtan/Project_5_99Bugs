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

## Technology Stack

- **Engine:** Unity 6000.6.0f1 (URP) · **Language:** C# · **Target:** Android (mobile)
- **Tests:** Unity Test Framework (EditMode) · **CI:** GitHub Actions + GameCI (tests + Android build)
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
│   └── data-pipeline.md
├── apps/
│   └── mobile/
│       └── EcoBalance3D/          # Unity project (scenes, art, audio, prefabs, app scripts, app tests)
├── packages/
│   └── ecosim/                    # Unity package com.99bugs.ecosim: simulation core + tests
├── tools/
│   ├── unity.sh                   # open / test / build / clean (Ubuntu, macOS, Git Bash)
│   └── unity.bat                  # same commands for Windows cmd
├── tests/                         # test plans and manual checklists
├── .github/workflows/unity-ci.yml # CI: EditMode tests + Android build
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
- **CI:** runs automatically on every push and pull request to `main` (GitHub → Actions).

## Build

- **Editor:** **File → Build Profiles → Android → Build**.
- **Command line** (Editor closed): `tools/unity.sh build android` or `tools\unity.bat build android`
  (also `windows`, and `macos` / `linux` with `unity.sh`). Output: `apps/mobile/EcoBalance3D/Build/<Platform>/`.
- **CI:** every push to `main` produces the Android build artifact **`EcoBalance3D-Android`** (GitHub → Actions → run → Artifacts).

CI needs these repository secrets: `UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD`
(serial-based Unity licence; see the [GameCI activation guide](https://game.ci/docs/github/activation)).

## Clean

Delete generated folders (`Library`, `Temp`, `Logs`, `obj`, `Build`) if the project gets into a bad state:
`tools/unity.sh clean` or `tools\unity.bat clean`. The next open re-imports everything.

**Submission zip:** `tools\unity.bat zip` (or menu option 6) or `tools/unity.sh zip` packs only the committed files (about 80 MB) into `EcoBalance_TechFiles.zip` next to the repo folder. You do not need to run Clean first.

Generated files, IDE folders, build output, logs and secrets are excluded by `.gitignore`; never commit them.

## Workflow

- Work on your personal branch, merge `main` into it often, and open a **pull request into `main`**.
- `main` is protected: merge only after the **EditMode tests** check passes. Never push directly to `main`.
- Rename or move Unity files **inside the Unity Editor** so their `.meta` files move with them.
- Jira tracks the work; this repository holds the implementation and evidence.
