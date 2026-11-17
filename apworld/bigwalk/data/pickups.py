"""Pick-up checks: packs, flare guns and fireworks."""

from __future__ import annotations

from typing import NamedTuple

from .ids import BASE_ID


class Pickup(NamedTuple):
    """A backpack, belt or gourd carton whose first pick-up is a check (`pack_sanity`)."""

    guid: str
    """The prop's `savablePropGuid`, the same on every machine."""
    location_name: str
    past_chairlift: bool = False
    past_tunnels: bool = False


PICKUP_ID_OFFSET = 13_000
"""A pick-up's location id: `BASE_ID + 13_000 + position` in PICKUPS."""

PICKUPS: tuple[Pickup, ...] = (
    Pickup("2d8101c4-223f-4939-8df6-809562469516", "Sky Blue Backpack"),
    Pickup("76601ed6-da1c-4326-9c8d-2c6168d31f36", "Yellow Backpack"),
    Pickup("730affd5-3139-473c-aaf2-4ecd5142c82f", "Teal Backpack"),
    Pickup("22009189-d33d-4e65-8b3c-c8257188743d", "Orange Backpack"),
    Pickup("f2dea8ab-08ea-434d-b98b-7e66a499392e", "Pink Backpack"),
    Pickup("02e75b25-ffa8-49e9-9fa3-7c453206aad9", "Maroon Backpack", past_chairlift=True),
    Pickup("9057af3c-12c5-48d8-8251-07233a203861", "Green Belt"),
    Pickup("d73ec09b-8e2b-432e-8e62-b5ff71db2d23", "Purple Belt", past_tunnels=True),
    Pickup("3bfe0dec-42de-4c31-87ab-a72f0b94722a", "Yellow Belt"),
    Pickup("ef09f445-cd71-4414-938a-7a7122dcd121", "Orange Belt"),
    Pickup("3e944e74-2d2e-40e2-b1ba-a1afa8c937ee", "Blue Belt", past_chairlift=True),
    Pickup("5fa8468a-dfaa-48fe-8057-46d47b9e56a5", "Gourd Carton Pickup"),
)
"""Every backpack, belt (`HolsterProp`) and gourd carton, named by their `_TintColor`."""


FLARE_GUN_ID_OFFSET = 13_050
"""A flare gun pick-up's location id: `BASE_ID + 13_050 + position` in FLARE_GUN_PICKUPS."""

FLARE_GUN_PICKUPS: tuple[Pickup, ...] = (
    Pickup("80381b52-ede7-494f-b800-0c77a71f57e0", "Red Flare Gun Pickup"),
    Pickup("72bc4c22-4418-4b7c-9c1d-e8cfa5040102", "Green Flare Gun Pickup"),
    Pickup("66de92ed-177d-49d5-aa97-e86d98083ac0", "Yellow Flare Gun Pickup"),
    Pickup("8b9fc973-4200-4934-8169-dde2de5b1d10", "Blue Flare Gun Pickup"),
)
"""The four flare guns; "Pickup" in the name so the location does not read like the item."""


class Firework(NamedTuple):
    """A firework launcher whose first firing is a check (`firework_sanity`)."""

    key: str
    """The landmark among the launcher's parents that the mod finds it by."""
    location_name: str
    past_chairlift: bool = False
    tower: str | None = None
    """The `prop_name` of the tower whose roof it stands on."""


FIREWORK_ID_OFFSET = 13_100
"""A firework's location id: `BASE_ID + 13_100 + position` in FIREWORKS."""

FIREWORKS: tuple[Firework, ...] = (
    Firework("Hub", "Hub Fireworks"),
    Firework("Lookout_RedCone", "Red Funnel Tower Fireworks", tower="bigKeyRedZone"),
    Firework("Lookout_YellowZigzag", "Yellow Twist Tower Fireworks", tower="bigKeyYellowZone"),
    Firework("Lookout_BlueTube", "Blue Castle Tower Fireworks", tower="bigKeyBlueZone"),
    Firework("Lookout_GreenHourglass", "Green Cup Tower Fireworks", tower="bigKeyGreenZone"),
    Firework("PeakMarker", "Peak Fireworks"),
    Firework("SouthPeak", "South Peak Fireworks", past_chairlift=True),
    Firework("FireworksPlatform_Intro", "Intro Platform Fireworks"),
)
"""The eight launchers; the game saves nothing, so the mod watches each one's button."""


def pickup_location_id(position: int) -> int:
    return BASE_ID + PICKUP_ID_OFFSET + position


def firework_location_id(position: int) -> int:
    return BASE_ID + FIREWORK_ID_OFFSET + position


def flare_gun_location_id(position: int) -> int:
    return BASE_ID + FLARE_GUN_ID_OFFSET + position
