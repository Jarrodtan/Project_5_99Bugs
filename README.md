# EcoBalance

**Team:** 99 Bugs · CSD3401 / UXG3400, Fall 2026

EcoBalance is an interactive, educational 3D ecosystem simulation for children aged 6–9, set in the
Himalayan Highlands (grass → blue sheep → snow leopard). Two modes:

- **Free Exploration:** choose the starting blue sheep and snow leopards, run the ecosystem, observe, and discover investigations in the Field Journal.
- **Guided Exploration:** a 20-day survival challenge. Events pause the game and ask for a choice; the ecosystem's real state decides one of 5 endings, kept in an Ending Gallery.

## Documentation

| Document | Contents |
|---|---|
| [`docs/proposal.md`](docs/proposal.md) | Concept, problem, target users, player experience |
| [`docs/design/product-design.md`](docs/design/product-design.md) | Team, user journey, MVP, M1 feature plan, Milestone 2 status |
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
│   ├── ecosim/                    # Unity package com.99bugs.ecosim: simulation core + tests
│   └── procanim/                  # Unity package com.99bugs.procanim: our procedural animation + tests
├── tools/
│   ├── unity.sh                   # open / test / build / clean / zip (Ubuntu, macOS, Git Bash)
│   └── unity.bat                  # same commands for Windows cmd
├── tests/                         # test plans and manual checklists
├── .github/workflows/unity-ci.yml # CI: repo checks, EditMode tests, Android build (docs/infra/ci.md)
├── .gitignore  .gitattributes  .editorconfig
```

The app loads the simulation core from `packages/ecosim` and the animation module from `packages/procanim`
through `apps/mobile/EcoBalance3D/Packages/manifest.json` (`"com.99bugs.ecosim": "file:../../../../packages/ecosim"`,
same for `procanim`), so always clone the **whole repository**.

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
5. *(Optional, live weather)* Ask the tech lead for `ApiKeys.json` and put it in
   `apps/mobile/EcoBalance3D/Assets/EcoBalance/Resources/Secrets/` (git-ignored, never commit it).
   Without it the game runs normally and the weather label says "offline".
6. Unity Hub → **Add → Add project from disk** → select **`apps/mobile/EcoBalance3D`**.
   The first open imports everything (a few minutes).

## Run

Open `Assets/Scenes/HomeScreen` and press **Play** (always start from HomeScreen: it sets up the mode).
**Start → Explore (Himalayan Highlands) →** then either:

- **Start Guided:** the 20-day challenge begins at once (10 blue sheep, 2 snow leopards).
- **Start Free:** set the starting numbers (default 10 / 2, up to 50 each) and press **Start**.

In the simulation: **Play/Pause**, the **speed** button (1x → 2x → 4x → 8x) and **Reset** at the bottom left; tap an animal to inspect it;
**View Details** expands the ecosystem overview; **Field Journal** opens over the paused run; **Back** asks before leaving.
Drag to spin the camera, two fingers to move, pinch to zoom. One game day is 1 real minute at 1x (a 20-day Guided run is 20 minutes, 2.5 at 8x).

The built app starts from the first scene in Build Settings (HomeScreen).

## Test

- **Editor:** **Window → General → Test Runner → EditMode → Run All** (app tests + `com.99bugs.ecosim` and `com.99bugs.procanim` package tests).
- **Command line** (Editor closed):
  - Ubuntu / macOS / Git Bash: `tools/unity.sh test`
  - Windows: `tools\unity.bat test`

  Results are written to `test-results.xml`.
- **CI:** every pull request into `main` runs the repo checks (`python tools/ci/repo-checks.py`, also runnable locally) **and** the EditMode tests (including scene smoke tests and performance tests). There is no nightly run: everything reaches `main` through a PR, which runs the same tests. Each PR run also adds a row to the performance history on Gitea. Failures are printed in the job log.

## Build

- **Editor:** **File → Build Profiles → Android → Build**.
- **Command line** (Editor closed): `tools/unity.sh build android` or `tools\unity.bat build android` (release; `android-dev` for a development APK with the debug overlay, saved as `EcoBalance3D-dev.apk`)
  (also `windows`, and `macos` / `linux` with `unity.sh`). Output: `apps/mobile/EcoBalance3D/Build/<Platform>/`.
- **CI:** the Android APK is built for every `v*` tag and on demand via **Actions → Unity CI → Run workflow** (choose `release` or `development`; development includes the debug overlay).
  APKs are stored on the team Gitea, not GitHub: **Gitea → team21-admin → Packages → `ecobalance-apk`** (newest 10 `build-*` versions kept; `v*` tags kept).
  The run's GitHub summary links to the APK. Tag `main` (e.g. `v0.2-m2`) for a versioned milestone release.

CI needs these repository secrets: `UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD`
(serial-based Unity licence; see the [GameCI activation guide](https://game.ci/docs/github/activation)),
`GITEA_LFS_TOKEN` (LFS pull) and `GITEA_PACKAGE_TOKEN` (APK upload; Gitea token with package read/write).
On `v*` tags (and on demand with `headless` ticked) CI also builds a **Linux headless** version that the team VM soak-tests (`tools/vm/soak.sh`, results on Gitea → Packages → `ecobalance-soak`).
APKs and performance history go to Gitea, so GitHub artifact storage stays at 0 (it filled up in October 2026 and locked Actions).
CI uses about 600 of the 3,000 student-Pro minutes per month (tests ~12 min, an APK build ~50 min); an Actions budget with "stop usage" makes it pause rather than charge.

## Clean

`tools/unity.sh clean` or `tools\unity.bat clean` (menu option 6) deletes the Unity cache (`Library`, `Temp`, `Logs`, `obj`, `Build`)
and every other Git-ignored file (`UserSettings`, `.vs`, `.vscode`, `*.csproj`, `*.slnx`), leaving the folder like a fresh clone.
Committed files, new uncommitted work and `.git` are kept. The next open re-imports everything and resets your Unity layout.

**Submission zip:** `tools\unity.bat zip` (or menu option 7) or `tools/unity.sh zip` packs only the committed files (about 80 MB) into `EcoBalance_TechFiles.zip` next to the repo folder. You do not need to run Clean first.

Generated files, IDE folders, build output, logs and secrets are excluded by `.gitignore`; never commit them.

## Workflow

- Work on your personal branch. During a milestone, merge it into the **integration branch** (`M2-Integration` for M2), not `main`. Pull the integration branch before you start, and write real commit messages.
- At the milestone the integration branch goes into `main` through one **pull request**.
- `main` is protected: merge only after the **EditMode tests** check passes. Never push directly to `main`.
- Rename or move Unity files **inside the Unity Editor** so their `.meta` files move with them.
- Jira tracks the work; this repository holds the implementation and evidence.
- **Pull requests (from 5 Nov, lecturer's rule):** one Jira subtask per change; branch `feature/SCRUM-<key>-<name>`; commit messages start with the key (`SCRUM-321: …`); the PR says what changed, why, how it was tested and links the Jira key; at least one teammate reviews with comments on specific lines (not just "LGTM"); the author answers every comment; merge only after approval and passing checks.
- **Access:** all 9 team members and the instructor are collaborators on the GitHub repository.
- **Code freeze:** before each milestone, `main` is frozen (M1: Sat 3 Oct 2026, 5pm; M2: Fri 6 Nov 2026, 23:59, submission Sun 8 Nov). After the freeze, only bug fixes go in, through a reviewed pull request with passing tests. The submitted state is kept on a `M<n>-Submission` branch and a `v0.<n>-m<n>` tag.
- **Submission build:** the milestone zip is made from frozen `main` with `tools\unity.bat zip` / `tools/unity.sh zip`, so it matches exactly what was reviewed.

## Credits & Copyright

© 2026 Team 99 Bugs (CSD3401 / UXG3400, DigiPen Institute of Technology Singapore). All rights reserved.
Shown for portfolio purposes with the course lecturers' permission. Third-party assets, data and tools are
credited in [`THIRD_PARTY_NOTICES.md`](THIRD_PARTY_NOTICES.md).
