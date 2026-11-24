"""Renames the gourds outside the world: data package, spoiler and slot_data."""

from __future__ import annotations

import logging
import unicodedata
from collections.abc import Iterable, Mapping
from typing import Any

from worlds.AutoWorld import data_package_checksum

from . import data

MAX_LENGTH = 24  # Same as GourdNames.MaxLength in the mod.


# Punctuation kept besides letters, marks, digits and spaces; no `<`, the hub board is TextMeshPro.
ALLOWED_PUNCTUATION = frozenset("'-_.,!?&+:;()@")


def carton_name(name: str) -> str:
    """The carton follows the gourds: "Plumbus" gives "Plumbus Carton"."""
    return f"{name} Carton"


def carrier_group(name: str) -> str:
    """An alias group for the carton, the word players use for it: "Plumbus Carrier"."""
    return f"{name} Carrier"


def keep(character: str) -> bool:
    return (character == " " or character in ALLOWED_PUNCTUATION
            or unicodedata.category(character)[0] in ("L", "M", "N"))


def filtered(wanted: str) -> str:
    """NFKC first, so decorated letters (Ⓐⓑⓒ, full-width) become plain ones the fonts can draw."""
    wanted = unicodedata.normalize("NFKC", wanted)
    return "".join(character for character in " ".join(wanted.split()) if keep(character))


def clean(wanted: str, taken: Iterable[str], player_name: str) -> str:
    """Trims and cuts the name; "Gourd" if it is empty or already an item or group name."""
    name = filtered(wanted)
    if name != " ".join(wanted.split()):
        logging.warning(f"Big Walk ({player_name}): gourd_name lost the characters it may not hold.")
    name = " ".join(name.split())
    if len(name) > MAX_LENGTH:
        logging.warning(f"Big Walk ({player_name}): gourd_name cut to {MAX_LENGTH} characters.")
        name = name[:MAX_LENGTH].rstrip()
    if not any(unicodedata.category(character)[0] in ("L", "M") for character in name):
        # No letter: empty, or only digits and signs, like `{0}` once the braces are gone.
        if name:
            logging.warning(f"Big Walk ({player_name}): gourd_name has no letter, keeping Gourd.")
        return data.GOURD_ITEM_NAME
    if name.casefold() == data.GOURD_ITEM_NAME.casefold():
        return data.GOURD_ITEM_NAME
    # "Victory" is the goal's event; the carton's name and alias group must be free as well.
    wanted_names = {name.casefold(), carton_name(name).casefold(), carrier_group(name).casefold()}
    if wanted_names & ({other.casefold() for other in taken if other != data.GOURD_ITEM_NAME} | {"victory"}):
        logging.warning(f"Big Walk ({player_name}): gourd_name '{name}' is already taken, keeping Gourd.")
        return data.GOURD_ITEM_NAME
    return name


def shared(wanted: Mapping[str, str]) -> str:
    """The name every slot agrees on, else "Gourd"."""
    names = set(wanted.values())
    if len(names) == 1:
        return names.pop()
    if names:
        asked = ", ".join(f"{player}: '{name}'" for player, name in sorted(wanted.items()))
        logging.warning(f"Big Walk: slots disagree on gourd_name ({asked}), keeping Gourd.")
    return data.GOURD_ITEM_NAME


def renamed_package(package: Mapping[str, Any], name: str) -> dict[str, Any]:
    """The data package with the gourd and carton renamed, plus alias groups for the old names."""
    renames = {data.GOURD_ITEM_NAME: name, data.GOURD_CARTON_ITEM_NAME: carton_name(name)}

    def rename(item: str) -> str:
        return renames.get(item, item)

    groups = {group: sorted(rename(item) for item in items) for group, items in package["item_name_groups"].items()}
    groups[data.GOURD_ITEM_NAME] = [name]
    groups[data.GOURD_CARTON_ITEM_NAME] = [carton_name(name)]
    groups[carrier_group(name)] = [carton_name(name)]
    # Same key order as get_data_package_data, or the checksum differs.
    renamed: dict[str, Any] = {
        "item_name_groups": {group: groups[group] for group in sorted(groups)},
        "item_name_to_id": {rename(item): item_id for item, item_id in package["item_name_to_id"].items()},
        "location_name_groups": package["location_name_groups"],
        "location_name_to_id": package["location_name_to_id"],
    }
    renamed["checksum"] = data_package_checksum(renamed)
    return renamed
