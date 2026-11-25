"""Checks that the mod and the apworld compute the same Archipelago ids."""

from __future__ import annotations

import importlib.util
import re
import sys
from pathlib import Path
from types import ModuleType

REPO = Path(__file__).resolve().parent.parent
MOD_FILE = REPO / "mod" / "src" / "Core" / "Net" / "ApLocationIds.cs"
DATA = REPO / "apworld" / "bigwalk" / "data"

# The mod's constant -> the apworld's, an attribute of the data package.
SAME = {
    "Base": "BASE_ID",
    "RadioOffset": "RADIO_ID_OFFSET",
    "DepositOffset": "DEPOSIT_ID_OFFSET",
    "GauntletOffset": "GAUNTLET_ID_OFFSET",
    "TeleportOffset": "TELEPORT_ID_OFFSET",
    "CutOffset": "CUT_ID_OFFSET",
    "CutStride": "CUT_STRIDE",
    "KeyItemOffset": "KEY_ITEM_ID_OFFSET",
    "ArchDoorOffset": "ARCH_DOOR_ID_OFFSET",
    "PuzzleNeedOffset": "PUZZLE_NEED_ID_OFFSET",
    "AbilityOffset": "ABILITY_ID_OFFSET",
    "PickupOffset": "PICKUP_ID_OFFSET",
    "FireworkOffset": "FIREWORK_ID_OFFSET",
    "TowerOffset": "TOWER_REACHED_ID_OFFSET",
    "FlareGunOffset": "FLARE_GUN_ID_OFFSET",
    "ObjectOffset": "OBJECT_ID_OFFSET",
    "ObjectKindOffset": "OBJECT_KIND_ID_OFFSET",
    "LightOffset": "LIGHT_ID_OFFSET",
    "MonumentOffset": "MONUMENT_FULL_ID_OFFSET",
    "MonumentKeyOffset": "MONUMENT_KEY_ID_OFFSET",
}
# The game's name differs from the item's.
RENAMED = {"XrayGoggles": "Scanner"}


def load_data() -> ModuleType:
    # Loaded alone: the world itself needs Archipelago.
    spec = importlib.util.spec_from_file_location("bigwalk_data", DATA / "__init__.py",
                                                  submodule_search_locations=[str(DATA)])
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    sys.modules["bigwalk_data"] = module
    spec.loader.exec_module(module)
    return module


def letters(name: str) -> list[str]:
    return sorted(re.sub(r"[^a-z]", "", name.lower()))


def problems() -> list[str]:
    found: list[str] = []
    data = load_data()
    code = MOD_FILE.read_text(encoding="utf-8-sig")
    constants = {name: int(value.replace("_", ""))
                 for name, value in re.findall(r"const (?:long|int) (\w+) = ([\d_]+);", code)}

    for name, theirs in SAME.items():
        if constants.get(name) != getattr(data, theirs):
            found.append(f"{name} is {constants.get(name)} in the mod, {theirs} is {getattr(data, theirs)}")

    by_hand = {"ProgressiveGauntletValue": data.progressive_gauntlet_id() - data.BASE_ID - data.GAUNTLET_ID_OFFSET,
               "GadgetItemBaseOffset": data.FILLER_ITEMS[0][1]}
    for name, value in by_hand.items():
        if constants.get(name) != value:
            found.append(f"{name} is {constants.get(name)} in the mod, the apworld has {value}")

    for name in sorted(set(constants) - set(SAME) - set(by_hand)):
        found.append(f"{name} is not compared with anything: add it to {Path(__file__).name}")

    order = re.search(r"GadgetItemOrder =\s*\{(.*?)\}", code, re.S)
    kinds = re.findall(r"GadgetKind\.(\w+)", order.group(1)) if order else []
    for position in range(max(len(kinds), len(data.FILLER_ITEMS))):
        kind = kinds[position] if position < len(kinds) else "nothing"
        item, offset = data.FILLER_ITEMS[position] if position < len(data.FILLER_ITEMS) else ("nothing", 0)
        if letters(RENAMED.get(kind, kind)) != letters(item) or offset != by_hand["GadgetItemBaseOffset"] + position:
            found.append(f"gadget {position}: {kind} in the mod, {item} at {offset} in the apworld")
    return found


if __name__ == "__main__":
    wrong = problems()
    print("\n".join(wrong) or "The mod and the apworld agree on the ids.")
    sys.exit(1 if wrong else 0)
