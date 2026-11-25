"""Option sets drawn at random, always the same ones: a set is refused with a reason, or it makes a sound world."""

import json
import unittest
from random import Random
from typing import Any

from BaseClasses import MultiWorld
from Options import Choice, Option, OptionError, OptionSet, PerGameCommonOptions, Range, Toggle
from test.general import setup_multiworld

from ..options import BigWalkOptions
from ..world import BigWalkWorld
from .bases import BigWalkTestBase

SETS = 150
ENDINGS = ("big_wall", "big_goodbye", "big_game")


def drawn(option: type[Option[Any]], random: Random) -> Any:
    if issubclass(option, Toggle):
        return random.choice([False, True])
    if issubclass(option, Choice):
        return random.choice(sorted(option.name_lookup))
    if issubclass(option, Range):
        return random.randint(option.range_start, option.range_end)
    if issubclass(option, OptionSet) and option.valid_keys:
        keys = sorted(option.valid_keys)
        return random.sample(keys, random.randint(0, len(keys)))
    return option.default


def option_set(number: int) -> dict[str, Any]:
    values: dict[str, Any] = {}
    for name, option in BigWalkOptions.type_hints.items():
        if name in PerGameCommonOptions.type_hints or name == "start_inventory_from_pool":
            continue
        # One generator per option: a new option leaves the draws of the others as they were.
        random = Random(f"{number} {name}")
        # A quarter of the options stay at their default, as players leave them.
        if random.random() >= 0.25:
            values[name] = drawn(option, random)
    # A listed ending is always refused.
    if "objectives" in values:
        values["objectives"] = [key for key in values["objectives"] if key not in ENDINGS]
    return values


class TestRandomOptions(unittest.TestCase):
    def test_each_set_is_refused_or_sound(self) -> None:
        accepted = 0
        for number in range(SETS):
            with self.subTest(set=number):
                try:
                    multiworld = setup_multiworld(BigWalkWorld, seed=number, options=option_set(number))
                except OptionError:
                    continue
                accepted += 1
                self.assert_sound(multiworld)
        # Were they all refused, nothing would be checked above.
        self.assertGreater(accepted, SETS // 5)

    def assert_sound(self, multiworld: MultiWorld) -> None:
        world = multiworld.worlds[1]
        useful: list = []
        filler: list = []
        world.fill_hook([], useful, filler, [])
        waiting = len(multiworld.itempool) + len(useful) + len(filler)
        self.assertEqual(waiting, len(multiworld.get_unfilled_locations(1)), "items and places")
        everything = multiworld.get_all_state()
        unreached = [location.name for location in multiworld.get_locations(1) if not location.can_reach(everything)]
        self.assertFalse(unreached, "out of reach with every item")
        self.assertTrue(multiworld.can_beat_game(everything), "cannot be won with every item")
        json.dumps(world.fill_slot_data())


class TestTooManyItems(BigWalkTestBase):
    auto_construct = False
    options = {"lock_puzzle_needs": True, "cut_key_sanity": "off", "lock_abilities": "handed",
               "teleport_buttons": "items", "tower_mode": "by_item"}

    def test_it_is_refused_with_the_count(self) -> None:
        with self.assertRaisesRegex(OptionError, r"need \d+ items for only \d+ locations"):
            self.world_setup()
