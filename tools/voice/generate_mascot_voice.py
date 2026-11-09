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

# Lines built at runtime that the string scan cannot see (Mascot.FindLine).
EXTRA = [
    "Can you find a Snow Leopard? Click on one when you spot it!",
    "Can you find a Blue Sheep? Click on one when you spot it!",
]


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
        # Spoken lines are sentences: have a space and end in . ! or ?  (skips object names, prefs, labels)
        if " " in s and s.rstrip()[-1:] in ".!?" and "{" not in s and len(s) >= 15:
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
    ap.add_argument("--speed", type=float, default=0.9, help="a little slower for ages 6-9")
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
    pipe = KPipeline(lang_code="a")  # American English
    made = 0
    for s in lines:
        path = OUT / f"{clip_name(s)}.wav"
        if path.exists() and not args.force:
            continue
        audio = [a for _, _, a in pipe(s, voice=args.voice, speed=args.speed)]
        wav = np.concatenate(audio)
        wav = wav / max(1e-6, float(np.abs(wav).max())) * 0.9  # same loudness for every clip
        sf.write(path, wav, 24000)
        made += 1
        print("made", path.name, s)

    # Who-says-what list for the team and the credits.
    with open(OUT / "manifest.csv", "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["clip", "text", "voice", "speed"])
        for s in lines:
            w.writerow([clip_name(s), s, args.voice, args.speed])
    print(f"{made} new clips, {len(lines)} lines total -> {OUT}")


if __name__ == "__main__":
    main()
