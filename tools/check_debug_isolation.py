"""Checks that the mod's Debug/ code only runs in a Debug build with Debug.Enabled, without the game's DLLs."""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent

HARMONY_PATCH = re.compile(r"\[\s*(?:HarmonyLib\.)?HarmonyPatch\b")
PATCH_ALL = re.compile(r"\bPatchAll\s*\(")
DEBUG_NAMESPACE = re.compile(r"^\s*namespace\s+BigWalkArchipelago\.Debug\b", re.MULTILINE)
TYPE_DECLARATION = re.compile(r"\b(?:class|struct|enum|interface|record)\s+(\w+)")
PARTIAL_CLASS = re.compile(r"\bpartial\s+class\s+(\w+)")
# An internal or public member of a partial class: the name before its (, ;, = or {.
MEMBER = re.compile(r"^\s*(?:internal|public)\b[^=;({]*?\b(\w+)\s*(?:\(|;|=|\{)", re.MULTILINE)


def strip_comments(line: str) -> str:
    """Drops a // comment; good enough for this code base, which has no // inside strings that matter."""
    code, _, _ = line.partition("//")
    return code


def debug_only_lines(text: str) -> list[tuple[int, str, bool]]:
    """Each line with whether it only compiles in a Debug build (inside #if DEBUG, or #else of #if !DEBUG)."""
    stack: list[bool] = []  # per #if: is the branch being read debug-only
    conditions: list[str] = []
    out = []
    for number, line in enumerate(text.splitlines(), 1):
        directive = line.strip()
        if directive.startswith("#if"):
            condition = directive[3:].strip()
            conditions.append(condition)
            stack.append(condition == "DEBUG")
        elif directive.startswith("#elif") and stack:
            stack[-1] = directive[5:].strip() == "DEBUG"
        elif directive.startswith("#else") and stack:
            stack[-1] = conditions[-1] == "!DEBUG"
        elif directive.startswith("#endif") and stack:
            stack.pop()
            conditions.pop()
        else:
            out.append((number, strip_comments(line), any(stack)))
    return out


def sources(root: Path) -> list[Path]:
    return [p for p in root.rglob("*.cs") if not {"bin", "obj"} & set(p.relative_to(root).parts)]


def check(src: Path) -> list[str]:
    errors: list[str] = []
    debug_dir = src / "Debug"
    debug_files = [p for p in sources(src) if debug_dir in p.parents]
    shared_files = [p for p in sources(src) if debug_dir not in p.parents]

    debug_types: set[str] = set()
    partial_members: dict[str, set[str]] = {}
    for path in debug_files:
        text = path.read_text(encoding="utf-8-sig")
        name = path.relative_to(src).as_posix()
        for number, line, _ in debug_only_lines(text):
            if HARMONY_PATCH.search(line):
                errors.append(f"{name}:{number}: [HarmonyPatch] in Debug/ would be applied by PatchAll; "
                              "give it a TryApply called under Debug.Enabled")
            if PATCH_ALL.search(line):
                errors.append(f"{name}:{number}: PatchAll in Debug/")
        if DEBUG_NAMESPACE.search(text):
            debug_types.update(TYPE_DECLARATION.findall(text))
        else:
            # A partial of a shared class: its members exist only in a Debug build.
            for cls in PARTIAL_CLASS.findall(text):
                partial_members.setdefault(cls, set()).update(MEMBER.findall(text))

    debug_use = re.compile(r"\bBigWalkArchipelago\.Debug\b|\bDebug\.(?:" + "|".join(sorted(debug_types)) + r")\b") \
        if debug_types else re.compile(r"\bBigWalkArchipelago\.Debug\b")
    member_uses = [(cls, re.compile(rf"\b{cls}\.({'|'.join(sorted(members))})\b"))
                   for cls, members in partial_members.items() if members]

    for path in shared_files:
        name = path.relative_to(src).as_posix()
        for number, line, debug_only in debug_only_lines(path.read_text(encoding="utf-8-sig")):
            if debug_only:
                continue
            if debug_use.search(line):
                errors.append(f"{name}:{number}: uses the Debug namespace outside #if DEBUG: {line.strip()}")
            for cls, pattern in member_uses:
                match = pattern.search(line)
                if match:
                    errors.append(f"{name}:{number}: {cls}.{match.group(1)} is defined in Debug/ "
                                  f"but used outside #if DEBUG")
    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--src", type=Path, default=REPO / "mod" / "src", help="the mod's src folder")
    args = parser.parse_args(argv)
    errors = check(args.src)
    for error in errors:
        print(error)
    if errors:
        print(f"{len(errors)} problem(s): Debug/ code could run in a Debug build with Debug.Enabled=false.")
        return 1
    print("Debug/ code is isolated: no [HarmonyPatch] for PatchAll, no use outside #if DEBUG.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
