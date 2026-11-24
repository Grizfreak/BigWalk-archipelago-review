"""In-game scenarios: keeps them in step with the mod, reads the logs of a session, guards a release.

    python tests/ingame/check.py verify
    python tests/ingame/check.py record --version 1.1.0 --host LogOutput.log... --guest LogOutput.log...
    python tests/ingame/check.py release dist/BigWalkArchipelago-1.1.0.zip
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
import zipfile
from pathlib import Path
from typing import Any

HERE = Path(__file__).parent
REPO = HERE.parent.parent
SOURCES = REPO / "mod" / "src"
RESULTS = HERE / "results"
LOADED = re.compile(r"Big Walk Archipelago v(\S+) loaded \(build ([0-9A-F]+)\)")
DLL = "BigWalkArchipelago.dll"
# Not exercised by a scenario of their own.
NOT_COVERED = {"Debug", "bin", "obj"}


def scenarios() -> list[dict[str, Any]]:
    return json.loads((HERE / "scenarios.json").read_text(encoding="utf-8"))


def mod_folders() -> set[str]:
    names = {path.name for part in ("Core", "Patches") for path in (SOURCES / part).iterdir() if path.is_dir()}
    names |= {path.name for path in SOURCES.iterdir() if path.is_dir()} - {"Core", "Patches"}
    return names - NOT_COVERED


def verify() -> list[str]:
    """What went out of step: a log line the mod no longer writes, a folder no scenario covers."""
    problems: list[str] = []
    code = "\n".join(path.read_text(encoding="utf-8-sig") for path in SOURCES.rglob("*.cs")
                     if not set(path.relative_to(SOURCES).parts) & NOT_COVERED)
    folders, covered, seen = mod_folders(), set(), set()
    for scenario in scenarios():
        name = scenario["id"]
        if name in seen:
            problems.append(f"{name}: two scenarios have this id")
        seen.add(name)
        covered |= set(scenario["covers"])
        if not scenario["proofs"] and not scenario["eye"]:
            problems.append(f"{name}: nothing to look at, in the log or by eye")
        for folder in sorted(set(scenario["covers"]) - folders):
            problems.append(f"{name}: covers {folder}, which is not a folder of the mod")
        for proof in scenario["proofs"]:
            for fragment in proof["text"]:
                if proof.get("from") != "bepinex" and fragment not in code:
                    problems.append(f"{name}: the mod no longer writes {fragment!r}")
    for folder in sorted(folders - covered):
        problems.append(f"no scenario covers {folder}")
    return problems


def session_lines(paths: list[Path]) -> tuple[list[str], set[tuple[str, str]]]:
    """Every line of these logs, and the (version, build) pairs they were written by."""
    lines: list[str] = []
    builds: set[tuple[str, str]] = set()
    for path in paths:
        text = path.read_text(encoding="utf-8", errors="replace")
        found = LOADED.search(text)
        if found is None:
            sys.exit(f"{path}: the mod did not load in this log.")
        builds.add((found.group(1), found.group(2)))
        lines += text.splitlines()
    return lines, builds


def proven(proof: dict[str, Any], lines: list[str]) -> bool:
    count = sum(1 for line in lines if all(fragment in line for fragment in proof["text"]))
    return count == 0 if proof.get("absent") else count >= proof.get("times", 1)


def label(proof: dict[str, Any]) -> str:
    words = " ... ".join(part.strip() for part in proof["text"])
    times = f" ({proof['times']} times)" if proof.get("times", 1) > 1 else ""
    return f"{proof['on']} log {'has no' if proof.get('absent') else 'has'} `{words}`{times}"


def ticked(previous: str) -> set[str]:
    return {line[6:] for line in previous.splitlines() if line.startswith("- [x] ")}


def record(version: str, host: list[Path], guest: list[Path]) -> None:
    host_lines, host_builds = session_lines(host)
    guest_lines, guest_builds = session_lines(guest)
    builds = host_builds | guest_builds
    if len(builds) != 1:
        sys.exit(f"The logs come from several builds, test one: {sorted(builds)}")
    written, build = builds.pop()
    if written != version:
        sys.exit(f"The logs come from v{written}, not v{version}.")

    path = RESULTS / f"{version}.md"
    header = f"Build `{build}`, on two machines."
    old = path.read_text(encoding="utf-8") if path.exists() else ""
    # Ticks of another build say nothing about this one.
    kept = ticked(old) if header in old else set()
    logs = {"host": host_lines, "guest": guest_lines}
    out = [f"# In-game tests of {version}", "", header, "",
           "Ticked from the logs by `check.py record`, or by hand for what only shows on screen."]
    left = 0
    for scenario in scenarios():
        out += ["", f"## {scenario['title']}", "", *(f"{step}" for step in scenario["steps"]), ""]
        entries = [(label(proof), proven(proof, logs[proof["on"]])) for proof in scenario["proofs"]]
        for absent, (text, good) in zip([p.get("absent") for p in scenario["proofs"]], entries):
            # A line that must not be there is judged on every log given, never kept from before.
            done = good or (not absent and text in kept)
            left += not done
            out.append(f"- [{'x' if done else ' '}] {text}")
        for text in scenario["eye"]:
            done = f"by eye: {text}" in kept
            left += not done
            out.append(f"- [{'x' if done else ' '}] by eye: {text}")
    RESULTS.mkdir(exist_ok=True)
    path.write_text("\n".join(out) + "\n", encoding="utf-8", newline="\n")
    print(f"{path}: {left} left to tick.")


def release(package: Path) -> list[str]:
    """Why this zip may not be published yet; empty when its own build was tested."""
    with zipfile.ZipFile(package) as archive:
        name = next(entry for entry in archive.namelist() if entry.endswith(DLL))
        build = hashlib.sha256(archive.read(name)).hexdigest().upper()[:16]
    for path in sorted(RESULTS.glob("*.md")):
        text = path.read_text(encoding="utf-8")
        if f"Build `{build}`" not in text:
            continue
        ticks = ticked(text)
        wanted = [label(p) for s in scenarios() for p in s["proofs"]]
        wanted += [f"by eye: {t}" for s in scenarios() for t in s["eye"]]
        return [f"{path.name}: not ticked: {line}" for line in wanted if line not in ticks]
    return [f"no results for build {build}: play the scenarios with this very zip, then record them"]


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    commands = parser.add_subparsers(dest="command", required=True)
    commands.add_parser("verify")
    recording = commands.add_parser("record")
    recording.add_argument("--version", required=True)
    recording.add_argument("--host", type=Path, nargs="+", required=True)
    recording.add_argument("--guest", type=Path, nargs="+", required=True)
    publishing = commands.add_parser("release")
    publishing.add_argument("package", type=Path)
    args = parser.parse_args()

    if args.command == "record":
        record(args.version, args.host, args.guest)
        return
    problems = verify() if args.command == "verify" else release(args.package)
    for problem in problems:
        print(problem)
    if problems:
        sys.exit(1)
    print("in step with the mod" if args.command == "verify" else "this build was tested")


if __name__ == "__main__":
    main()
