"""Id offsets and the gourd item names."""

from __future__ import annotations


# Ids reuse the game's enum values, so the mod computes them instead of keeping a copy of this file.
BASE_ID = 8_600_000

RADIO_ID_OFFSET = 1_000
DEPOSIT_ID_OFFSET = 2_000
GAUNTLET_ID_OFFSET = 2_500
"""Gauntlet stages, location and item alike: `BASE_ID + 2_500 + GauntletChamberN` (2550..2556)."""
CUT_ID_OFFSET = 3_000
KEY_ITEM_ID_OFFSET = 4_000
ARCH_DOOR_ID_OFFSET = 5_000
# 9_000 and up: item-only ids for filler and traps. 10_000..10_159 stay unused, never reuse them.
PUZZLE_NEED_ID_OFFSET = 11_000
"""Item-only: the `lock_puzzle_needs` items, numbered by their position in PUZZLE_NEEDS."""

CUT_STRIDE = 10
"""Ids reserved per big key for its cuts; ten where the widest key has five, room to grow."""

GOURD_ITEM_NAME = "Gourd"
GOURD_CARTON_ITEM_NAME = "Gourd Carton"
