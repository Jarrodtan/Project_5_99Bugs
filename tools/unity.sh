#!/usr/bin/env bash
# [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
# EcoBalance helper for Ubuntu 24.04 / macOS / Windows (Git Bash).
#
#   tools/unity.sh open            open the project in the Unity Editor
#   tools/unity.sh test            run all EditMode tests headless -> test-results.xml
#   tools/unity.sh build android   build APK   -> apps/mobile/EcoBalance3D/Build/Android/
#   tools/unity.sh build windows   build EXE   -> .../Build/Windows/   (also: macos, linux)
#   tools/unity.sh clean           delete generated folders (Library, Temp, Logs, obj, Build)
#
# Unity 6000.6.0f1 is found in the default Unity Hub location; override with UNITY=/path/to/Unity.
# Close the Editor before test/build (a project can only be open once).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT="$ROOT/apps/mobile/EcoBalance3D"
VERSION="$(sed -n 's/^m_EditorVersion: //p' "$PROJECT/ProjectSettings/ProjectVersion.txt" | tr -d '\r')"

find_unity() {
  if [ -n "${UNITY:-}" ]; then echo "$UNITY"; return; fi
  case "$(uname -s)" in
    Darwin) echo "/Applications/Unity/Hub/Editor/$VERSION/Unity.app/Contents/MacOS/Unity" ;;
    Linux)  echo "$HOME/Unity/Hub/Editor/$VERSION/Editor/Unity" ;;
    *)      echo "/c/Program Files/Unity/Hub/Editor/$VERSION/Editor/Unity.exe" ;;
  esac
}

UNITY_BIN="$(find_unity)"
need_unity() {
  if [ ! -x "$UNITY_BIN" ] && [ ! -f "$UNITY_BIN" ]; then
    echo "Unity $VERSION not found at: $UNITY_BIN" >&2
    echo "Install it with Unity Hub, or set UNITY=/path/to/Unity" >&2
    exit 1
  fi
}

case "${1:-help}" in
  open)
    need_unity
    "$UNITY_BIN" -projectPath "$PROJECT" &
    ;;
  test)
    need_unity
    "$UNITY_BIN" -batchmode -nographics -projectPath "$PROJECT" \
      -runTests -testPlatform EditMode -testResults "$ROOT/test-results.xml" -logFile -
    echo "Results: $ROOT/test-results.xml"
    ;;
  build)
    need_unity
    case "${2:-android}" in
      android) METHOD=BuildScript.BuildAndroid; TARGET=Android ;;
      windows) METHOD=BuildScript.BuildWindows; TARGET=Win64 ;;
      macos)   METHOD=BuildScript.BuildMacOS;   TARGET=OSXUniversal ;;
      linux)   METHOD=BuildScript.BuildLinux;   TARGET=Linux64 ;;
      *) echo "Unknown platform '$2' (android|windows|macos|linux)" >&2; exit 1 ;;
    esac
    "$UNITY_BIN" -batchmode -nographics -quit -projectPath "$PROJECT" \
      -buildTarget "$TARGET" -executeMethod "$METHOD" -logFile -
    ;;
  clean)
    for d in Library Temp Logs obj Build; do
      rm -rf "${PROJECT:?}/$d" && echo "removed $d"
    done
    rm -f "$ROOT/test-results.xml"
    echo "Clean. The next open re-imports everything (a few minutes)."
    ;;
  *)
    sed -n '3,12p' "$0"
    ;;
esac
# [AI-GENERATED END]
