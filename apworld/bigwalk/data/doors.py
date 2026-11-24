"""Arch doors and tunnel doors."""

from __future__ import annotations

from typing import NamedTuple

from .ids import ARCH_DOOR_ID_OFFSET, BASE_ID
from .zones import ZONE_COLOURS


# Arch doors: the first one and the Drawbridge lead out of the start; the other two are shortcuts.


class ArchDoor(NamedTuple):
    system_name: str
    """`SavableSystem` enum name, the SaveManager key the door's state lives under."""
    system_value: int
    """`SavableSystem` enum value; the item id is BASE_ID + ARCH_DOOR_ID_OFFSET + this."""
    item_name: str


FIRST_ARCH_DOOR = ArchDoor("SpawnHubGate", 11, "First Arch Door")
LEFT_ARCH_DOOR = ArchDoor("HubShortcutToSportsCreek", 13, "Left Arch Door")
RIGHT_ARCH_DOOR = ArchDoor("HubTunnel", 12, "Right Arch Door")
ARCH_DOORS: tuple[ArchDoor, ...] = (FIRST_ARCH_DOOR, LEFT_ARCH_DOOR, RIGHT_ARCH_DOOR)


# Both doors can be walked round; `require_arch_doors` only keeps a shortcut from coming too late.
TOWER_FAR_DOOR: dict[str, ArchDoor] = {
    "bigKeyGreenZone": RIGHT_ARCH_DOOR,
    "bigKeyBlueZone": LEFT_ARCH_DOOR,
    "bigKeyYellowZone": LEFT_ARCH_DOOR,
    "bigKeyBoss": LEFT_ARCH_DOOR,
    "bigKeyOverflow": LEFT_ARCH_DOOR,
}
"""The far door each tower lies past, by the tower's `prop_name`. The drawbridge has none."""

ZONE_FAR_DOOR: dict[str, ArchDoor | None] = {
    "chairlift": RIGHT_ARCH_DOOR,
    "tunnels": LEFT_ARCH_DOOR,
    # The ending can be walked to round the Left door, so it is never held behind it.
    "ending": None,
    "green_dome": LEFT_ARCH_DOOR,
}
"""The far door each zone behind a big key lies past (`regions.py` names the entrances)."""


def arch_door_item_id(door: ArchDoor) -> int:
    return BASE_ID + ARCH_DOOR_ID_OFFSET + door.system_value


# Tunnel doors (`tunnel_sanity`): each GateLogic opens alone; the purple hole is SouthPeakTunnel.

TUNNEL_DOOR_ID_OFFSET = 14_400
"""By position in TUNNEL_DOORS."""


class TunnelDoor(NamedTuple):
    item_name: str
    gates: tuple[str, ...]
    """Paths of the `GateLogic` states the mod opens."""
    tower: str | None
    """`prop_name` of the tower the outlet sits by; None for the purple hole."""


PURPLE_TUNNEL_ITEM_NAME = "Purple Tunnel"

TUNNEL_DOORS: tuple[TunnelDoor, ...] = (
    *(TunnelDoor(f"{colour.title()} Tunnel Door", (f"TunnelSystem/TunnelOutlet - {colour.title()}/GateLogic",), prop)
      for colour, prop in ZONE_COLOURS),
    TunnelDoor(PURPLE_TUNNEL_ITEM_NAME, (
        "TunnelSystem/SouthPeakTunnel/TunnelOutlet_Simple/GateLogic",
        "TunnelSystem/SouthPeakTunnel/TunnelOutlet_Simple (1)/GateLogic",
    ), None),
)


def tunnel_door_item_id(door: TunnelDoor) -> int:
    return BASE_ID + TUNNEL_DOOR_ID_OFFSET + TUNNEL_DOORS.index(door)
