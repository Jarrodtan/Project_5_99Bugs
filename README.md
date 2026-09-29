# Project 5 / Team Project
Team Name: 99 Bugs

# EcoBalance

EcoBalance is an interactive educational ecosystem simulation application that allows users to experiment with ecosystem conditions and observe how changes affect organisms, resources, and the environment over time.

## Documentation

Project documentation can be found in the [`docs/`](docs/) directory.

- Project Proposal
- Product Design
- UI/UX Design
- Software Architecture
- Technical Deliverables and Milestone Planning

## Technology Stack

- Engine: Unity 6000.6.0f1 (URP)
- Target Platform: Mobile (Android)
- Language: C#
- Full list with reasons: [`docs/toolchain.md`](docs/toolchain.md)

## Repository Structure

```
apps/mobile/EcoBalance3D/   Unity project (scenes, art, audio, prefabs, app scripts in Assets/_Project/)
packages/ecosim/            EcoSim Unity package: simulation core + its tests (no app dependency)
docs/                       Proposal, product design, architecture, toolchain, data pipeline
tests/  tools/              Test plans / manual checklists, tooling notes
.github/workflows/          CI: EditMode tests + Android build
```
The app loads `packages/ecosim` through `apps/mobile/EcoBalance3D/Packages/manifest.json`
(`"com.99bugs.ecosim": "file:../../../../packages/ecosim"`). Clone the **whole repo** so that path exists.

## Setup (Windows / macOS)

1. Install [Unity Hub](https://unity.com/download), then install Unity **6000.6.0f1** with the **Android Build Support** module.
2. `git clone https://github.com/Jarrodtan/Project_5_99Bugs.git`
3. In Unity Hub, choose **Add → Add project from disk** and select the `apps/mobile/EcoBalance3D` folder.
4. Open the project. The first import takes a few minutes.

## Run

Open the main scene in `Assets/Scenes/` and press **Play**.

## Test

- **In the Editor:** open **Window → General → Test Runner → EditMode → Run All** (runs the app tests and the `com.99bugs.ecosim` package tests).
- **In CI:** tests run automatically on every push or PR to `main` (see the Actions tab).

## Build

- **Local:** open **File → Build Profiles → Android → Build**.
- **CI:** every push to `main` produces an Android build artifact (`EcoBalance-Android`) in GitHub Actions.

CI needs these repository secrets: `UNITY_SERIAL`, `UNITY_EMAIL` and `UNITY_PASSWORD` (serial-based Unity Student/Pro licence) (see the [GameCI activation guide](https://game.ci/docs/github/activation)).

## Workflow

Work on a personal or feature branch, open a pull request into `main`, and merge after CI passes. Don't push directly to `main`.
