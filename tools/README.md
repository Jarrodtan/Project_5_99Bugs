# tools/

Development and production tools that live outside the Unity project.

| Script | Platforms | Commands |
|---|---|---|
| `unity.sh` | Ubuntu 24.04, macOS, Windows (Git Bash) | `open`, `test`, `build android\|windows\|macos\|linux`, `clean` |
| `unity.bat` | Windows 11 (cmd, or **double-click for a menu**) | `open`, `test`, `build android|windows`, `clean` |\|windows`, `clean` |

Both find Unity 6000.6.0f1 in the default Unity Hub folder (override with the `UNITY` environment variable).
`build` calls `BuildScript` (`Assets/_Project/Scripts/Editor/BuildScript.cs`), which builds every scene enabled in Build Settings.
Close the Editor before `test` or `build`.

Unity Editor tools (custom inspectors, windows) go in `apps/mobile/EcoBalance3D/Assets/_Project/Scripts/Editor/`, because Unity only compiles code inside `Assets/`.

---
*Drafted with AI assistance: Claude Code (Claude Opus 5.5, claude-opus-5-5). Reviewed and edited by Tan Wei Leong.*
