#!/usr/bin/env bash
# [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
# Weekly headless soak test on the team VM (see docs/infra/ci.md → "Headless soak test on the VM").
#
# 1. downloads the latest Linux headless build that CI published to Gitea (ecobalance-headless/latest)
# 2. runs it with no graphics for SOAK_DAYS simulated days (HeadlessSoakRunner in the game)
# 3. uploads the JSON report and appends one row to the soak history on Gitea (ecobalance-soak)
#
# Needs: a Gitea token with package read/write in ~/.ecobalance-token (chmod 600), curl, tar, timeout.
# Light on the VM: a headless Unity player, no editor (a few hundred MB RAM).
set -uo pipefail

SOAK_DAYS="${SOAK_DAYS:-2000}"
MAX_SECONDS="${MAX_SECONDS:-3600}"
API=https://team21.dp-ext8.com/git/api/packages/team21-admin/generic
WORK="${WORK:-$HOME/soak}"
TOKEN=$(cat "$HOME/.ecobalance-token")
AUTH="team21-admin:$TOKEN"

mkdir -p "$WORK"
cd "$WORK" || exit 1
rm -rf build && mkdir build

echo "== Downloading latest headless build"
curl -fsS --user "$AUTH" -o build.tar.gz "$API/ecobalance-headless/latest/EcoBalanceHeadless.tar.gz" || { echo "download failed"; exit 1; }
tar -xzf build.tar.gz -C build
BIN=$(find build -name 'EcoBalanceHeadless.x86_64' | head -n 1)
[ -n "$BIN" ] || { echo "binary not found in archive"; exit 1; }
chmod +x "$BIN"
COMMIT=$(cat build/COMMIT 2>/dev/null || echo unknown)

echo "== Running soak: $SOAK_DAYS days (commit $COMMIT)"
rm -f report.json
timeout $((MAX_SECONDS + 120)) "$BIN" -batchmode -nographics -logFile soak.log \
  -soakDays "$SOAK_DAYS" -soakMaxSeconds "$MAX_SECONDS" -soakReport "$WORK/report.json"
CODE=$?
echo "exit code $CODE"
[ -s report.json ] || echo "{\"result\":\"no report (exit $CODE)\",\"exitCode\":$CODE}" > report.json
cat report.json

STAMP=$(date -u +%Y%m%d-%H%M)
field() { grep -o "\"$1\": *[^,}]*" report.json | head -n 1 | sed 's/.*: *//; s/"//g'; }

echo "== Uploading report and history"
curl -fsS --user "$AUTH" --upload-file report.json "$API/ecobalance-soak/$STAMP-$COMMIT/report.json" || true
curl -fsS --user "$AUTH" --upload-file soak.log "$API/ecobalance-soak/$STAMP-$COMMIT/soak.log" || true

HIST="$API/ecobalance-soak/history/soak.csv"
curl -fsS --user "$AUTH" "$HIST" -o soak.csv || echo "date,commit,exit,result,days,seconds,errors,peak_mb" > soak.csv
echo "$(date -u +%Y-%m-%d),$COMMIT,$CODE,$(field result),$(field daysReached),$(field realSeconds),$(field errorCount),$(field peakMemoryMB)" >> soak.csv
curl -fsS -X DELETE --user "$AUTH" "$HIST" >/dev/null 2>&1 || true
curl -fsS --user "$AUTH" --upload-file soak.csv "$HIST" || echo "history upload failed"
tail -n 5 soak.csv
exit "$CODE"
# [AI-GENERATED END]
