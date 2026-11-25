"""Sums up a fuzzer's report; fails on anything but fills that found no place, whose rate it gives."""

from __future__ import annotations

import json
import sys
from pathlib import Path


def main() -> int:
    variant, path = sys.argv[1], Path(sys.argv[2])
    if not path.is_file():
        print(f"**{variant}**: no report, the fuzzer stopped before the end.")
        return 1
    report = json.loads(path.read_text(encoding="utf-8"))
    stats = report["stats"]
    errors = {name: runs for by_name in report["errors"].values() for name, runs in by_name.items()}
    no_place = len(errors.pop("FillError", []))
    made = stats["success"] + no_place
    rate = f"{100 * no_place / made:.2f} %" if made else "no seed made"

    print(f"**{variant}**: {stats['total']} generations, {stats['ignored']} refused by the options.")
    # The other variants count as a success whatever they do not look for.
    if variant == "default":
        print(f"- {stats['success']} seeds, {no_place} fills without a place ({rate})")
    for name, runs in sorted(errors.items(), key=lambda pair: -len(pair[1])):
        print(f"- {len(runs)} x {name} (runs {', '.join(map(str, runs[:10]))})")
    if stats.get("unfinished"):
        print(f"- {stats['unfinished']} generations never ended")
    return 1 if errors or stats.get("unfinished") or not stats["success"] else 0


if __name__ == "__main__":
    sys.exit(main())
