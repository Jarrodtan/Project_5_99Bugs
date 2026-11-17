# [AI-GENERATED] Claude Code (Claude Opus 5.5) - reviewed by Tan Wei Leong
"""Generate the mascot voice clips with Kokoro-82M (Apache-2.0), offline, on a PC.

Reads every mascot line from Mascot.cs, speaks it, and writes one WAV per line into
Assets/EcoBalance/Resources/MascotVoice/<ClipName>.wav, where ClipName is the same
FNV-1a hash Mascot.ClipName() uses. Unity compresses the WAVs to Vorbis on import.
Only the clips ship in the game: no TTS engine runs on the tablet.

Setup (once):  pip install kokoro soundfile numpy
               (Windows: also install espeak-ng from its GitHub releases page)
Run:           python tools/voice/generate_mascot_voice.py [--voice af_heart] [--speed 0.9] [--force]
Voices list:   https://huggingface.co/hexgrad/Kokoro-82M  (VOICES.md)
"""
import argparse
import csv
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
UNITY = ROOT / "apps/mobile/EcoBalance3D/Assets/EcoBalance"
MASCOT_CS = UNITY / "Scripts/UI/Mascot.cs"
OUT = UNITY / "Resources/MascotVoice"

# Other scripts whose lines the mascot says through Mascot.Announce / React:
# season + weather news (SeasonWeather) and the Guided choice feedback (GuidedChallenge, feedback = "...").
OTHER_SOURCES = [
    (UNITY / "Scripts/EcoSim/SeasonWeather.cs", r'=>\s*"((?:[^"\\\n]|\\.)*)"|\?\s*"((?:[^"\\\n]|\\.)*)"|:\s*"((?:[^"\\\n]|\\.)*)"'),
    (UNITY / "Scripts/Guided/GuidedChallenge.cs", r'feedback\s*=\s*"((?:[^"\\\n]|\\.)*)"'),
]

# Lines built at runtime that the string scan cannot see (Mascot.FindLine, AnimalBrain mating line).
EXTRA = [
    "Can you find a Snow Leopard? Click on one when you spot it!",
    "Can you find a Blue Sheep? Click on one when you spot it!",
    "Two blue sheep are starting a family! ♥",
    "Two snow leopards are starting a family! ♥",
]


def spoken(text: str) -> str:
    """What the voice says: the on-screen text without symbols such as ♥."""
    return re.sub(r"[♥]", "", text).strip()


def is_sentence(s: str) -> bool:
    # Spoken lines are sentences: have a space and end in . ! or ?  (skips object names, prefs, labels)
    return " " in s and s.rstrip()[-1:] in ".!?" and "{" not in s and "}" not in s and s[:1].isalpha() and len(s) >= 15


def clip_name(text: str) -> str:
    h = 2166136261
    for b in text.strip().encode("utf-8"):
        h ^= b
        h = (h * 16777619) & 0xFFFFFFFF
    return f"{h:08x}"


def mascot_lines() -> list[str]:
    src = MASCOT_CS.read_text(encoding="utf-8")
    found = []
    src = re.sub(r"(?m)^\s*//.*$", "", src)  # comment lines (quoted examples in docs)
    src = re.sub(r'\$"(?:[^"\\\n]|\\.)*"', "", src)  # interpolated strings: listed in EXTRA instead
    for m in re.finditer(r'"((?:[^"\\\n]|\\.)*)"', src):
        s = m.group(1)
        if is_sentence(s):
            found.append(s)
    for path, pattern in OTHER_SOURCES:
        other = re.sub(r"(?m)^\s*//.*$", "", path.read_text(encoding="utf-8"))
        for m in re.finditer(pattern, other):
            s = next(g for g in m.groups() if g is not None)
            if is_sentence(s):
                found.append(s)
    seen, lines = set(), []
    for s in found + EXTRA:
        if s not in seen:
            seen.add(s)
            lines.append(s)
    return lines


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--voice", default="af_heart", help="Kokoro voice id")
    ap.add_argument("--speed", type=float, default=0.9, help="a little slower for ages 7-9")
    ap.add_argument("--force", action="store_true", help="regenerate clips that already exist")
    ap.add_argument("--list", action="store_true", help="only print the lines and their clip names")
    args = ap.parse_args()

    lines = mascot_lines()
    if args.list:
        for s in lines:
            print(clip_name(s), s)
        print(f"{len(lines)} lines")
        return

    import numpy as np
    import soundfile as sf
    from kokoro import KPipeline

    OUT.mkdir(parents=True, exist_ok=True)
    manifest = OUT / "manifest.csv"
    # Clips the team has replaced with recorded VO keep source=recorded and are never overwritten.
    recorded = set()
    if manifest.exists():
        with open(manifest, newline="", encoding="utf-8") as f:
            recorded = {r["clip"] for r in csv.DictReader(f) if r.get("source") == "recorded"}
    pipe = KPipeline(lang_code="a")  # American English
    made = 0
    for s in lines:
        path = OUT / f"{clip_name(s)}.wav"
        if clip_name(s) in recorded or (path.exists() and not args.force):
            continue
        audio = [a for _, _, a in pipe(spoken(s), voice=args.voice, speed=args.speed)]
        wav = np.concatenate(audio)
        wav = wav / max(1e-6, float(np.abs(wav).max())) * 0.9  # same loudness for every clip
        sf.write(path, wav, 24000)
        made += 1
        print("made", path.name, s)

    # Who-says-what list for the team and the credits.
    # source = kokoro (AI placeholder, replace later) or recorded (team VO: set by hand after dropping
    # the recording in as <clip>.wav). Sort by source so the placeholders still to replace are on top.
    with open(manifest, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["clip", "source", "text", "voice", "speed"])
        rows = [[clip_name(s), "recorded" if clip_name(s) in recorded else "kokoro", s,
                 "" if clip_name(s) in recorded else args.voice, "" if clip_name(s) in recorded else args.speed]
                for s in lines]
        for r in sorted(rows, key=lambda r: r[1] == "recorded"):
            w.writerow(r)
    print(f"{made} new clips, {len(lines)} lines total -> {OUT}")


if __name__ == "__main__":
    main()
