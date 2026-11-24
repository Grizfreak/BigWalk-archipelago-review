"""Universal Tracker support: UT must rebuild a world with the seed's options, not a YAML."""

from __future__ import annotations

import inspect

from .. import world as bigwalk_world
from ..world import BigWalkWorld
from .bases import BigWalkTestBase, build_like_universal_tracker, world_shape

# A seed that differs from the defaults in all that can move a location, region or rule.
UNLIKE_THE_DEFAULTS = {
    "goal": "big_game",
    "gourd_sanity": "every_gourd",
    "gourds_required": 25,
    "radio_sanity": False,
    "shuffle_radio_music": False,
    "start_with_arch_doors_open": [],
    "lock_puzzle_needs": True,
    "gauntlet_mode": "locked_stages",
    "gauntlet_stage_items": "individual",
    "teleport_buttons": "items",
}


class TestTheTrackerRebuildsTheSeed(BigWalkTestBase):
    options = UNLIKE_THE_DEFAULTS

    def test_it_lands_on_this_exact_world(self) -> None:
        """Fed the seed's slot_data, the tracker's second pass lands on the generated world."""
        tracked = build_like_universal_tracker({}, self.world.fill_slot_data())
        self.assertEqual(world_shape(tracked), world_shape(self.multiworld))

    def test_and_without_the_passthrough_it_would_not(self) -> None:
        """Without slot_data the tracker would show a different world."""
        untracked = build_like_universal_tracker({})
        self.assertNotEqual(world_shape(untracked), world_shape(self.multiworld))


class TestSlotDataCarriesEveryTrackedOption(BigWalkTestBase):
    options = UNLIKE_THE_DEFAULTS

    def test_each_one_is_an_option_and_travels(self) -> None:
        slot_data = self.world.fill_slot_data()
        for field, name in bigwalk_world.TRACKER_OPTIONS.items():
            with self.subTest(option=name):
                self.assertIn(name, BigWalkWorld.options_dataclass.type_hints)
                self.assertIn(field, slot_data)

    def test_each_one_reads_back_into_the_value_it_was_generated_with(self) -> None:
        slot_data = self.world.fill_slot_data()
        for field, name in bigwalk_world.TRACKER_OPTIONS.items():
            with self.subTest(option=name):
                option = getattr(self.world.options, name)
                self.assertEqual(option.from_any(slot_data[field]).value, option.value)


class TestTheModStillReadsTheSameStrings(BigWalkTestBase):
    """slot_data keeps the option strings a released mod compares."""

    options = UNLIKE_THE_DEFAULTS

    def test_the_renamed_values_travel_under_their_old_names(self) -> None:
        slot_data = self.world.fill_slot_data()
        self.assertEqual(slot_data["goal"], "second_ending")
        self.assertEqual(slot_data["deposit_locations"], "all")

    def test_every_value_has_a_wire_name(self) -> None:
        from ..options import GOAL_ON_THE_WIRE, GOURD_SANITY_ON_THE_WIRE, Goal, GourdSanity

        # name_lookup has canonical names only; Archipelago adds `false` to a Choice with `off`.
        self.assertEqual(set(GOAL_ON_THE_WIRE), set(Goal.name_lookup.values()))
        self.assertEqual(set(GOAL_ON_THE_WIRE.values()), {"gauntlet", "ending", "deposits", "second_ending"})
        self.assertEqual(set(GOURD_SANITY_ON_THE_WIRE), set(GourdSanity.name_lookup.values()))
        self.assertEqual(set(GOURD_SANITY_ON_THE_WIRE.values()), {"none", "milestones", "all"})


class TestAnAlphaYamlStillReads(BigWalkTestBase):
    """The values' pre-rename spellings are aliases, so a first-alpha YAML still generates."""

    options = {"goal": "deposits", "gourd_sanity": "milestones"}

    def test_the_old_values_land_on_the_new_ones(self) -> None:
        from ..options import Goal, GourdSanity

        self.assertEqual(self.world.options.goal.value, Goal.option_big_collection)
        self.assertEqual(self.world.options.gourd_sanity.value, GourdSanity.option_every_5)


class TestTheHooksAreShapedTheWayUTChecksThem(BigWalkTestBase):
    def test_interpret_slot_data_is_static(self) -> None:
        """UT skips its first pass only for a staticmethod or a classmethod."""
        hook = inspect.getattr_static(BigWalkWorld, "interpret_slot_data", None)
        self.assertIsInstance(hook, staticmethod)

    def test_it_returns_something_so_that_ut_regenerates(self) -> None:
        """Returning None would leave the tracker on its first, YAML-shaped pass."""
        slot_data = self.world.fill_slot_data()
        self.assertIs(BigWalkWorld.interpret_slot_data(slot_data), slot_data)

    def test_ut_is_not_disabled_and_needs_no_yaml(self) -> None:
        self.assertTrue(BigWalkWorld.ut_can_gen_without_yaml)
        self.assertFalse(getattr(BigWalkWorld, "disable_ut", False))


class TestASeedFromBeforeProgressiveDoors(BigWalkTestBase):
    """A seed from before `gauntlet_stage_items` has one door per stage, not progressive ones."""

    options = {"goal": "big_goodbye", "gauntlet_mode": "locked_stages", "gauntlet_stage_items": "individual"}

    def test_it_is_rebuilt_with_one_door_per_stage(self) -> None:
        slot_data = dict(self.world.fill_slot_data())
        del slot_data["gauntlet_stage_items"]
        tracked = build_like_universal_tracker({}, slot_data)
        self.assertEqual(world_shape(tracked), world_shape(self.multiworld))
