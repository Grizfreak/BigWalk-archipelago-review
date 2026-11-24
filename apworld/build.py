#!/usr/bin/env python3
"""Package `bigwalk/` into `dist/bigwalk.apworld`."""

from __future__ import annotations

import argparse
import json
import zipfile
from pathlib import Path

HERE = Path(__file__).parent
PACKAGE = HERE / "bigwalk"
DEFAULT_OUTPUT = HERE / "dist" / "bigwalk.apworld"

# Archipelago reads the world version only with these set, and only in the packaged manifest.
CONTAINER_VERSION = 7
COMPATIBLE_VERSION = 7

EXCLUDED_DIRS = {"__pycache__", ".pytest_cache"}
EXCLUDED_SUFFIXES = {".pyc", ".pyo"}


def iter_files() -> list[Path]:
    files = []
    for path in sorted(PACKAGE.rglob("*")):
        if not path.is_file():
            continue
        if path.suffix in EXCLUDED_SUFFIXES:
            continue
        if EXCLUDED_DIRS & set(path.relative_to(PACKAGE).parts):
            continue
        files.append(path)
    return files


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("-o", "--output", type=Path, default=DEFAULT_OUTPUT,
                        help=f"output path (default: {DEFAULT_OUTPUT.relative_to(HERE)})")
    args = parser.parse_args()

    manifest = json.loads((PACKAGE / "archipelago.json").read_text(encoding="utf-8"))
    files = iter_files()

    args.output.parent.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(args.output, "w", zipfile.ZIP_DEFLATED) as archive:
        for path in files:
            if path == PACKAGE / "archipelago.json":
                packaged = {**manifest, "version": CONTAINER_VERSION, "compatible_version": COMPATIBLE_VERSION}
                archive.writestr("bigwalk/archipelago.json", json.dumps(packaged, indent=2) + "\n")
            else:
                archive.write(path, Path("bigwalk") / path.relative_to(PACKAGE))

    print(f"{args.output} - {manifest['game']} v{manifest['world_version']}, {len(files)} files")


if __name__ == "__main__":
    main()
