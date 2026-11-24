"""The players' abilities as items, and what places ask of them."""

from __future__ import annotations

from typing import NamedTuple

from .ids import BASE_ID


class Ability(NamedTuple):
    """A part of the player's body as an item, with `lock_abilities`."""

    key: str
    """What slot_data calls it; the mod's AbilityLocks grants by it."""
    item_name: str
    copies: int
    """Copies in the pool: a progressive arm raises, then points, then extends."""
    handed: bool
    """With `lock_abilities: handed` (one arm per item)."""
    ambidextrous: bool
    """With `lock_abilities: ambidextrous` (both arms per item)."""


ABILITY_ID_OFFSET = 13_400
"""Item-only, by position in ABILITIES, so keep the order."""

ABILITIES: tuple[Ability, ...] = (
    # Legs: jumping and crouching. Seat: sitting on a chair or the floor.
    Ability("legs", "Legs", 1, True, True),
    Ability("seat", "Seat", 1, True, True),
    Ability("left_arm", "Progressive Left Arm", 3, True, False),
    Ability("right_arm", "Progressive Right Arm", 3, True, False),
    Ability("arms", "Progressive Arms", 3, False, True),
    # Running is only in the pool with `lock_running`.
    Ability("throwing", "Throwing", 1, True, True),
    Ability("carrying", "Carrying", 1, True, True),
    Ability("running", "Running", 1, True, True),
)

ABILITY_LOCK_KEYS: dict[str, str] = {
    "legs": "legs", "seat": "seat", "left_arm": "arms", "right_arm": "arms", "arms": "arms",
    "throwing": "throwing", "carrying": "carrying",
}
"""The `start_with_abilities` key of each ability item; Running has `lock_running` instead."""

LEGS_ITEM_NAME = "Legs"
"""Needed to leave the starting zone with `lock_abilities`: jumping is needed almost everywhere."""
SEAT_ITEM_NAME = "Seat"
LEFT_ARM_ITEM_NAME = "Progressive Left Arm"
RIGHT_ARM_ITEM_NAME = "Progressive Right Arm"
ARMS_ITEM_NAME = "Progressive Arms"
THROWING_ITEM_NAME = "Throwing"
CARRYING_ITEM_NAME = "Carrying"
RUNNING_ITEM_NAME = "Running"


class Body(NamedTuple):
    """What a place asks of the players' bodies with `lock_abilities`."""

    seat: bool = False
    arms: int = 0
    """Copies of each arm (both arms): ARM_RAISING, ARM_POINTING or ARM_EXTENDING; 0 for none."""
    throwing: bool = False
    carrying: bool = False

ARM_RAISING, ARM_POINTING, ARM_EXTENDING = 1, 2, 3
"""How many copies of a progressive arm each gesture needs."""

POSES = Body(seat=True, arms=ARM_EXTENDING)
"""Sitting and the arm poses, both arms; abilities go to every player, so one item covers both."""

ABILITY_PUZZLES: dict[str, Body] = {
    # Jumping and crouching are left out: Legs is already needed to leave the starting zone.
    "gourdHighButton": Body(carrying=True),
    "gourdFielding": Body(throwing=True),
    "gourdInvisibleInk": Body(arms=ARM_RAISING),
    "gourdObby": Body(carrying=True),
    "gourdCarousel": Body(seat=True),
    "gourdWindowLabyrinth": Body(throwing=True),
    "gourdBasketball": Body(throwing=True),
    "gourdConcert": POSES,
    "gourdIndoorSemaphore": POSES,
    "gourdOpticalTelegraph": POSES,
    # Pointer's Paradise needs carrying only with 3 or 4 players; logic cannot see the player count.
    "gourdCharadesRooms": POSES,
    "gourdPointersParadise": Body(seat=True, arms=ARM_EXTENDING, carrying=True),
    "gourdKickUpPits": Body(throwing=True),
    "gourdDancerAndSelecter": POSES,
    "gourdSpeedObby": Body(carrying=True),
    "gourdCannonballCommute": Body(seat=True),
}

GAUNTLET_STAGE_ABILITIES: tuple[Body, ...] = (
    Body(arms=ARM_EXTENDING), Body(), Body(throwing=True), Body(), Body(throwing=True), Body(arms=ARM_EXTENDING), POSES,
)
"""The same, for the Silent Gauntlet's seven stages, in order."""


OBJECT_ABILITIES: dict[str, Body] = {
    # Megaphones hang out of one player's reach: one climbs on another's shoulders.
    "MegaphoneProp": Body(carrying=True),
}
"""What picking up an island object asks of the body with `lock_abilities`, by its kind."""

OBJECT_GUID_ABILITIES: dict[str, Body] = {
    # Only reached by riding the mini carousel's swings.
    "88942e8e-6fae-4d71-93f0-8500741a82d1": Body(seat=True),  # Binoculars (Carousel)
}
"""The same for one object, on top of its kind's, keyed as IslandObject.guid."""


def ability_item_id(ability: Ability) -> int:
    return BASE_ID + ABILITY_ID_OFFSET + ABILITIES.index(ability)
