"""Filler items, traps and bonuses."""

from __future__ import annotations

from typing import NamedTuple


FILLER_ITEMS: tuple[tuple[str, int], ...] = (
    ("Megaphone", 9_001),
    ("Walkie-Talkie", 9_002),
    ("Backpack", 9_003),
    ("Belt", 9_004),
    ("Flare Gun", 9_005),
    ("Laser", 9_006),
    ("Binoculars", 9_007),
    ("Compass", 9_008),
    ("Folding Map", 9_009),
    ("Radio", 9_010),
    ("Gourd Carton", 9_011),
    ("Torch", 9_012),
    ("Lamp", 9_013),
    ("Scanner", 9_014),
    ("Blue Flare Gun", 9_015),
    ("Green Flare Gun", 9_016),
    ("Yellow Flare Gun", 9_017),
    ("Rainbow Flare Gun", 9_018),
)
"""The island's hand props, spawned on receipt; the mod maps ids by position, so keep the order."""

FLARE_GUN_ITEM_NAMES: tuple[str, ...] = ("Flare Gun", "Blue Flare Gun", "Green Flare Gun", "Yellow Flare Gun")
"""The four flare guns: in the pool with `flare_gun_sanity`, never drawn as filler."""

RAINBOW_FLARE_GUN_ITEM_NAME = "Rainbow Flare Gun"

PALETTE_SIZE = 10
"""Colours in the mod's palette; `random_colors` draws 5 to 10 of them by index."""


# 9_201..9_213 are retired joke filler ids; never reuse them.


class Effect(NamedTuple):
    """A trap or a bonus: an item whose effect the mod plays on every player of the session."""

    item_name: str
    offset: int
    # What the mod knows the effect by (slot data, the ModChannel).
    key: str
    # A trap that costs time or progress: `hard_traps` keeps them out.
    hard: bool = False


TRAPS: tuple[Effect, ...] = (
    Effect("Big Drop", 9_102, "drop"),
    Effect("Big Throw", 9_103, "throw", hard=True),
    Effect("Big Trip", 9_104, "trip", hard=True),
    Effect("Big Meeting", 9_105, "meeting", hard=True),
    Effect("Big Night", 9_106, "night"),
    Effect("Big Sleep", 9_107, "sleep", hard=True),
    Effect("Big Load", 9_108, "load", hard=True),
    Effect("Big Mask", 9_109, "mask", hard=True),
    Effect("Big Flare", 9_110, "flare"),
    # A clock in every player's hands, showing the real time of their own computer.
    Effect("Big Time", 9_111, "time"),
    Effect("Big Silence", 9_112, "silence"),
    Effect("Big Slide", 9_113, "slide"),
    # Every player turns white, as in the Silent Gauntlet.
    Effect("Big Ghost", 9_114, "ghost"),
    # The players swap colours: two swap, more go round.
    Effect("Big Paint", 9_115, "paint"),
    # A timed tomato, then up in the air.
    Effect("Big Air", 9_116, "air", hard=True),
    Effect("Big Goggles", 9_117, "goggles"),
    Effect("Big Pixels", 9_118, "pixels"),
    Effect("Big Head", 9_119, "head"),
    # The players' heads become a glowing lamp.
    Effect("Big Brain", 9_120, "brain"),
    # The players' hands grow.
    Effect("Big Arms", 9_121, "arms"),
    # A green gas that pushes the players apart.
    Effect("Big Smell", 9_122, "smell"),
    # Every player becomes small.
    Effect("Big Brother", 9_123, "brother"),
    # The nearest puzzle still holding its gourd starts again.
    Effect("Big Reset", 9_124, "reset", hard=True),
    # A folding chair for every player, who is sat on it.
    Effect("Big Break", 9_125, "break"),
)
"""Traps hit every player of the session. 9_101 is a retired id; never reuse it."""

BONUSES: tuple[Effect, ...] = (
    Effect("Big Speed", 9_301, "speed"),
    Effect("Big Jump", 9_302, "jump"),
    Effect("Big Day", 9_303, "day"),
    # Only confetti and a puzzle-won sound, so it counts as filler.
    Effect("Big Confetti", 9_304, "confetti"),
)
"""The traps' good twins: a boost for every player for `trap_duration` seconds, or a treat."""

CONFETTI_ITEM_NAME = "Big Confetti"

TRAP_ITEMS: tuple[tuple[str, int], ...] = tuple((trap.item_name, trap.offset) for trap in TRAPS)
BONUS_ITEMS: tuple[tuple[str, int], ...] = tuple((bonus.item_name, bonus.offset) for bonus in BONUSES)

DEATH_LINK_TRIGGERS: tuple[str, ...] = ("puzzle_failed", "big_fall")
"""What sends a DeathLink: a failed puzzle, or a fall that floors a player."""
