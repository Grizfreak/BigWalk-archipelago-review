"""Checks that the mod and the apworld carry the same version in all four places."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent

# Where each version is written, and how to read it.
PLACES = {
    "mod/src/BigWalkArchipelago.csproj": r"<Version>([^<]+)</Version>",
    "mod/src/Plugin.cs": r'PluginVersion\s*=\s*"([^"]+)"',
    "apworld/bigwalk/world.py": r'^WORLD_VERSION\s*=\s*"([^"]+)"',
}


def main() -> int:
    found: dict[str, str] = {}
    for path, pattern in PLACES.items():
        match = re.search(pattern, (REPO / path).read_text(encoding="utf-8-sig"), re.MULTILINE)
        found[path] = match.group(1) if match else "(not found)"
    manifest = REPO / "apworld/bigwalk/archipelago.json"
    found["apworld/bigwalk/archipelago.json"] = json.loads(manifest.read_text(encoding="utf-8"))["world_version"]

    for path, version in found.items():
        print(f"{version:12} {path}")
    if len(set(found.values())) != 1:
        print("The versions differ.")
        return 1
    print("Same version everywhere.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
