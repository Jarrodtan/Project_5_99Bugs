# [AI-GENERATED BEGIN] Claude Code (Claude Opus 5.5, claude-opus-5-5) - reviewed by Tan Wei Leong
"""Fast static checks that need no Unity (seconds in CI; locally: python tools/ci/repo-checks.py).

Each check targets a mistake that has broken, or would break, a teammate's project or the Android build.
Exits 1 if any check fails and lists every problem.
"""
import os
import re
import subprocess
import sys

ROOTS = ("apps/mobile/EcoBalance3D/Assets/", "packages/ecosim/", "packages/procanim/")
LARGE = 5 * 1024 * 1024

top = subprocess.check_output(["git", "rev-parse", "--show-toplevel"], text=True).strip()
os.chdir(top)
files = subprocess.check_output(["git", "ls-files", "-z"], text=True).split("\0")
files = [f for f in files if f]
tracked = set(files)
unity = [f for f in files if f.startswith(ROOTS)]
problems = {}


def problem(section, msg):
    problems.setdefault(section, []).append(msg)


S1 = "1. Every Unity asset and folder has a .meta file (missing metas break references for others)"
S2 = "2. No orphan .meta files (a .meta whose asset was deleted leaves a broken GUID)"
S3 = "3. No merge-conflict markers left in code or Unity files"
S4 = "4. Large binaries are in Git LFS, not stored directly in Git"
S5 = "5. No editor-only code in runtime folders (breaks the Android build, see SCRUM-125)"

dirs = set()
for f in unity:
    if f.endswith(".meta"):
        continue
    if f + ".meta" not in tracked:
        problem(S1, f"no .meta for {f}")
    d = os.path.dirname(f)
    while d and not any(d + "/" == r for r in ROOTS) and d.startswith(tuple(r.rstrip("/") for r in ROOTS)):
        dirs.add(d)
        d = os.path.dirname(d)
for d in sorted(dirs):
    if d + ".meta" not in tracked:
        problem(S1, f"no folder .meta for {d}/")

for m in (f for f in unity if f.endswith(".meta")):
    asset = m[: -len(".meta")]
    if asset not in tracked and asset not in dirs:
        problem(S2, f"orphan {m}")

conflict = re.compile(r"^(<<<<<<< |>>>>>>> )", re.M)
editor_using = re.compile(r"^\s*using\s+UnityEditor\b")
if_editor = re.compile(r"^\s*#if\b.*UNITY_EDITOR")
endif = re.compile(r"^\s*#endif\b")

# Size of what Git itself stores (an LFS file is stored as a ~130-byte pointer).
stage = subprocess.check_output(["git", "ls-files", "-s", "-z"], text=True).split("\0")
oid_of = {e.split("\t", 1)[1]: e.split()[1] for e in stage if "\t" in e}
batch = subprocess.run(["git", "cat-file", "--batch-check=%(objectname) %(objectsize)"],
                       input="\n".join(oid_of.values()), capture_output=True, text=True).stdout
blob_size = dict(line.split() for line in batch.splitlines() if len(line.split()) == 2)
for f, oid in oid_of.items():
    stored = int(blob_size.get(oid, 0))
    if stored > LARGE:
        problem(S4, f"{f} is {stored // 1048576} MB in Git; add its type to .gitattributes (LFS)")

for f in files:
    if not os.path.isfile(f):
        continue
    size = os.path.getsize(f)
    if f.endswith((".cs", ".unity", ".prefab", ".asset", ".yml", ".md")) and size < LARGE:
        text = open(f, encoding="utf-8", errors="replace").read()
        for m in conflict.finditer(text):
            line = text.count("\n", 0, m.start()) + 1
            problem(S3, f"{f}:{line}")
        if f.endswith(".cs") and f.startswith(ROOTS) and not re.search(r"/(Editor|Tests?)/", f):
            depth = 0
            for n, line in enumerate(text.splitlines(), 1):
                if if_editor.match(line):
                    depth += 1
                elif endif.match(line) and depth:
                    depth -= 1
                elif editor_using.match(line) and depth == 0:
                    problem(S5, f"{f}:{n}: using UnityEditor outside #if UNITY_EDITOR")

for s in (S1, S2, S3, S4, S5):
    found = problems.get(s, [])
    print(f"{'✗' if found else '✓'} {s}")
    for msg in found[:50]:
        print(f"    {msg}")
    if len(found) > 50:
        print(f"    … and {len(found) - 50} more")

if problems:
    print("\nRepo checks FAILED")
    sys.exit(1)
print("\nRepo checks passed")
# [AI-GENERATED END]
