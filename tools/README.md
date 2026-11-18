# tools/

Development and production tools that live outside the Unity project.

| Script | Platforms | Commands |
|---|---|---|
| `unity.sh` | Ubuntu 24.04, macOS, Windows (Git Bash) | `open`, `test`, `build android\|windows\|macos\|linux`, `clean`, `zip [name]` |
| `unity.bat` | Windows 11 (cmd, or **double-click for a menu**) | `open`, `test`, `build android\|windows`, `clean`, `zip [name]` |

Both find Unity 6000.6.0f1 in the default Unity Hub folder (override with the `UNITY` environment variable).
`build` calls `BuildScript` (`Assets/EcoBalance/Scripts/Editor/BuildScript.cs`), which builds every scene enabled in Build Settings.
Close the Editor before `test` or `build`.

`zip` packs exactly what is committed (no `Library` cache, `.git`, `UserSettings` or build output) into `../EcoBalance_TechFiles.zip`, or `../<name>.zip`, for submitting the project files. It warns if you have uncommitted changes, because those are not included.

`ci/repo-checks.py` runs the CI static checks locally in about a second: `python tools/ci/repo-checks.py` (missing or orphan `.meta` files, large files outside LFS, merge-conflict markers, `UnityEditor` in runtime code). Run it before opening a PR.

`vm/soak.sh` runs on the team VM (weekly cron): downloads the Linux headless build CI published to Gitea, soak-tests it for 2,000 simulated days and uploads the report and history. Setup: `docs/infra/ci.md`.

`voice/generate_mascot_voice.py` makes the mascot's placeholder voice clips offline with Kokoro-82M (Apache-2.0): `python tools/voice/generate_mascot_voice.py` (`--list` prints every line and its clip name). Each clip is named by a hash of its line (`Mascot.ClipName()`), so **editing a line's text in `Mascot.cs` needs a new clip**. `Resources/MascotVoice/manifest.csv` lists clip, source, text. **To replace a placeholder with recorded VO:** look up the line in `manifest.csv`, save the recording as `<clip>.wav` over the old file, and change its `source` to `recorded` (the script never overwrites recorded clips). Credits: `THIRD_PARTY_NOTICES.md`.

Unity Editor tools (custom inspectors, windows) go in `apps/mobile/EcoBalance3D/Assets/EcoBalance/Scripts/Editor/`, because Unity only compiles code inside `Assets/`.

---
*Drafted with AI assistance: Claude Code (Claude Opus 5.5, claude-opus-5-5). Reviewed and edited by Tan Wei Leong.*
