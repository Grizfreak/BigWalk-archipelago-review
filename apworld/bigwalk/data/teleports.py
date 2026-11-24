"""The hub's teleport buttons as items."""

from __future__ import annotations

from .ids import BASE_ID


TELEPORT_ID_OFFSET = 2_700
"""Item-only: the teleporters of `teleport_buttons: items`, by position."""

TELEPORT_DESTINATIONS: tuple[tuple[str, str], ...] = (
    ("red", "Red Funnel Tower"),
    ("green", "Green Cup Tower"),
    ("blue", "Blue Castle Tower"),
    ("yellow", "Yellow Twist Tower"),
    ("black", "Black Tower"),
    ("gauntlet", "Silent Gauntlet"),
    ("drawbridge", "Drawbridge"),
    ("dome", "Green Dome Tower"),
)
"""The hub's teleport buttons (mod key, name); add new ones at the end, the position is the id."""


def teleport_item_name(destination: str) -> str:
    return f"{destination} Teleporter"


def teleport_item_id(position: int) -> int:
    return BASE_ID + TELEPORT_ID_OFFSET + position
