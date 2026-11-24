"""Towers, big keys, monuments, radio stations and the gourd deposits."""

from __future__ import annotations

from typing import NamedTuple

from .ids import (
    BASE_ID, CUT_ID_OFFSET, CUT_STRIDE, DEPOSIT_ID_OFFSET, KEY_ITEM_ID_OFFSET, RADIO_ID_OFFSET,
)


# Towers, big keys and monuments.


class Tower(NamedTuple):
    prop_name: str
    """`SaveablePropName` enum name of the big key itself."""
    prop_value: int
    """`SaveablePropName` enum value; item id and location id are BASE_ID + this."""
    item_name: str
    """The feature the key opens (chairlift, train...); the physical key is a second item."""
    key_name: str
    """The tower's name as players say it, prefix of the key item and its checks."""
    location_name: str
    """Player-facing name of the location "this key was placed in its plinth"."""
    plinth_name: str
    """`SaveableHomeName` enum name of the plinth the key is deposited into."""
    monument_prefix: str
    """`SaveableHomeName` prefix of the monument whose slots unlock this key."""
    slots: int
    """Gourds the tower's monument takes before its key is freed."""
    segments: int
    """Cuts on the key, each one a check; the Black and Green Dome keys come already cut."""


# The item is the feature a key opens; the key itself only carries checks.
DRAWBRIDGE_ITEM_NAME = "Drawbridge"
SPAWN_SECRET_DOOR_ITEM_NAME = "Spawn Secret Door"

TOWERS: tuple[Tower, ...] = (
    Tower("bigKeyIntro", 300, DRAWBRIDGE_ITEM_NAME, "Drawbridge", "Drawbridge Key Deposit",
          "bigKeyPlinthIntro", "monoumentIntro", 4, 5),
    Tower("bigKeyRedZone", 301, "Map Room", "Red Funnel Tower", "Red Funnel Tower Key Deposit",
          "bigKeyPlinthMapRoom", "monoument0", 5, 5),
    Tower("bigKeyGreenZone", 302, "Chairlift", "Green Cup Tower", "Green Cup Tower Key Deposit",
          "bigKeyPlinthSkiLift", "monoument1", 5, 5),
    Tower("bigKeyBlueZone", 303, "Train", "Blue Castle Tower", "Blue Castle Tower Key Deposit",
          "bigKeyPlinthTrain", "monoument2", 5, 5),
    Tower("bigKeyYellowZone", 304, "Tunnels", "Yellow Twist Tower", "Yellow Twist Tower Key Deposit",
          "bigKeyPlinthTunnels", "monoument3", 5, 5),
    Tower("bigKeyBoss", 305, "Big Wall Door", "Black Monolith Tower", "Black Monolith Tower Key Deposit",
          "bigKeyPlinthEnding", "monoumentFinal", 6, 0),
    Tower("bigKeyOverflow", 306, SPAWN_SECRET_DOOR_ITEM_NAME, "Green Dome Tower", "Green Dome Tower Key Deposit",
          "bigKeyPlinthGoodbye2", "monoumentOverflow", 15, 0),
)

TOTAL_CUT_SEGMENTS = sum(tower.segments for tower in TOWERS)
"""25: the drawbridge and the four coloured towers, five each."""

# Which of monoument0-3 is which colour is unknown; their slot counts match, so it does not matter.

GREEN_DOME = TOWERS[-1]
"""The Green Dome tower (`bigKeyOverflow`), the postgame/true-ending key."""

GREEN_CUP = TOWERS[2]
"""The Green Tower (`bigKeyGreenZone`); its feature is the chairlift."""

YELLOW_TWIST = TOWERS[4]
"""The Yellow Tower (`bigKeyYellowZone`); its feature is the tunnels."""

BLACK_MONOLITH = TOWERS[5]
"""The Black Tower (`bigKeyBoss`); its feature, the Big Wall Door, leads to the ending."""

MAX_MONUMENT_SLOTS = sum(tower.slots for tower in TOWERS)
"""Every monument slot on the island: 4 + 5 + 5 + 5 + 5 + 6 + 15 = 45."""

# Radio stations: switching one on is the check, its music plays once the item arrives.


class RadioStation(NamedTuple):
    system_name: str
    """`SavableSystem` enum name, the literal `SaveManager` key the game writes."""
    system_value: int
    """`SavableSystem` enum value; both ids are BASE_ID + RADIO_ID_OFFSET + this."""
    location_name: str
    """Player-facing name of the location "this station was switched on"."""
    item_name: str
    """Player-facing name of the item that actually starts the music playing."""


# Named after the music each station plays and its place on the dial, not after the enum names.
RADIO_STATIONS: tuple[RadioStation, ...] = (
    RadioStation("FmStationSleuthFm", 30, "Radio Station 02: Fourth Space", "Radio Music 02: Fourth Space"),
    RadioStation("FmStationKosmische", 31, "Radio Station 07: Bristol", "Radio Music 07: Bristol"),
    RadioStation("FmStationDanceFm", 32, "Radio Station 05: Dance FM", "Radio Music 05: Dance FM"),
    RadioStation("FmStationBreathwork", 33, "Radio Station 01: Bobby", "Radio Music 01: Bobby"),
    RadioStation("FmStationJourneyBeat", 34, "Radio Station 04: Journey Beat", "Radio Music 04: Journey Beat"),
    RadioStation("FmStationAFJ", 35, "Radio Station 06: Mallets", "Radio Music 06: Mallets"),
    RadioStation("FmStationFourthSpace", 36, "Radio Station 03: Breathwork", "Radio Music 03: Breathwork"),
)


TOWER_REACHED_ID_OFFSET = 13_200
"""A "reached" check per tower, by its position in reached_towers()."""


def reachable_towers() -> tuple[Tower, ...]:
    """The four coloured towers and the Black Tower: the ones with a button to reach."""
    return tuple(tower for tower in TOWERS if tower.prop_name not in ("bigKeyIntro", "bigKeyOverflow"))


def reached_towers() -> tuple[Tower, ...]:
    """Every tower with a "reached" check, in id order; the last two are reached by position."""
    return (*reachable_towers(), tower_by_prop("bigKeyIntro"), tower_by_prop("bigKeyOverflow"))


def tower_reached_location_name(tower: Tower) -> str:
    return f"Reached the {tower.key_name}"


MONUMENT_FULL_ID_OFFSET = 13_800
"""A "monument full" check per tower, by its position in TOWERS."""


def monument_full_location_name(tower: Tower) -> str:
    return f"{tower.key_name} Monument Full"


def monument_full_location_id(tower: Tower) -> int:
    return BASE_ID + MONUMENT_FULL_ID_OFFSET + TOWERS.index(tower)


MONUMENT_KEY_ID_OFFSET = 13_850
"""With `linear_towers: one_deposit_box`, the slot that frees a tower's key, by TOWERS order."""


def monument_key_location_name(tower: Tower) -> str:
    return f"{tower.key_name} Monument Key"


def monument_key_location_id(tower: Tower) -> int:
    return BASE_ID + MONUMENT_KEY_ID_OFFSET + TOWERS.index(tower)


def tower_reached_location_id(tower: Tower) -> int:
    return BASE_ID + TOWER_REACHED_ID_OFFSET + reached_towers().index(tower)


PROGRESSIVE_MONUMENT_ITEM_NAME = "Progressive Monument"
PROGRESSIVE_MONUMENT_ID_OFFSET = 13_300


def progressive_monument_id() -> int:
    """One id shared by every copy."""
    return BASE_ID + PROGRESSIVE_MONUMENT_ID_OFFSET


MAP_ROOM_ITEM_NAME = "Map Room"


def tower_by_prop(prop_name: str) -> Tower:
    return next(tower for tower in TOWERS if tower.prop_name == prop_name)


def key_deposit_location_id(tower: Tower) -> int:
    return BASE_ID + tower.prop_value


def feature_item_id(tower: Tower) -> int:
    """The feature the tower's key opens: map room, chairlift, train, tunnels..."""
    return BASE_ID + tower.prop_value


def key_item_name(tower: Tower) -> str:
    """The physical key, named after its tower like its own cut and deposit checks."""
    return f"{tower.key_name} Key"


def key_item_id(tower: Tower) -> int:
    return BASE_ID + KEY_ITEM_ID_OFFSET + tower.prop_value


def cut_location_name(tower: Tower, index: int) -> str:
    """Numbered from 1 for the player, while `index` counts from 0 like the mod."""
    return f"{tower.key_name} Key Cut {index + 1}"


def cut_location_id(tower: Tower, index: int) -> int:
    return BASE_ID + CUT_ID_OFFSET + tower.prop_value * CUT_STRIDE + index


def cut_locations(tower: Tower) -> tuple[str, ...]:
    return tuple(cut_location_name(tower, index) for index in range(tower.segments))


def radio_location_id(station: RadioStation) -> int:
    return BASE_ID + RADIO_ID_OFFSET + station.system_value


def radio_item_id(station: RadioStation) -> int:
    # Same id as the location: item and location ids are separate namespaces in Archipelago.
    return BASE_ID + RADIO_ID_OFFSET + station.system_value


def deposit_location_name(amount: int) -> str:
    return f"Gourd Deposit {amount}"


def deposit_location_id(amount: int) -> int:
    return BASE_ID + DEPOSIT_ID_OFFSET + amount
