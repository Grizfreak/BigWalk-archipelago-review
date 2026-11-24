"""Id tables are a contract with the mod, which computes ids from the game's enum values."""

import unittest

from .. import data, items, locations


class TestIdTables(unittest.TestCase):
    def test_no_duplicate_location_ids(self) -> None:
        ids = list(locations.LOCATION_NAME_TO_ID.values())
        self.assertEqual(len(ids), len(set(ids)))

    def test_no_duplicate_item_ids(self) -> None:
        ids = list(items.ITEM_NAME_TO_ID.values())
        self.assertEqual(len(ids), len(set(ids)))

    def test_location_ids_derive_from_game_enums(self) -> None:
        for puzzle in data.PUZZLES:
            self.assertEqual(
                locations.LOCATION_NAME_TO_ID[puzzle.location_name],
                data.BASE_ID + puzzle.prop_value,
            )
        for tower in data.TOWERS:
            self.assertEqual(
                locations.LOCATION_NAME_TO_ID[tower.location_name],
                data.BASE_ID + tower.prop_value,
            )
        for station in data.RADIO_STATIONS:
            self.assertEqual(
                locations.LOCATION_NAME_TO_ID[station.location_name],
                data.BASE_ID + data.RADIO_ID_OFFSET + station.system_value,
            )

    def test_cut_ids_match_the_formula_the_mod_uses(self) -> None:
        """Spells out ApLocationIds.TryResolveCut again, so a change in the formula shows."""
        for tower in data.TOWERS:
            for index in range(tower.segments):
                self.assertEqual(
                    locations.LOCATION_NAME_TO_ID[data.cut_location_name(tower, index)],
                    data.BASE_ID + data.CUT_ID_OFFSET + tower.prop_value * data.CUT_STRIDE + index,
                )

    def test_cut_ids_cannot_reach_any_other_range(self) -> None:
        # The stride keeps a key's segments out of the next key's; the offset keeps other ids out.
        cut_ids = [data.cut_location_id(tower, index)
                   for tower in data.TOWERS for index in range(tower.segments)]
        self.assertEqual(len(cut_ids), data.TOTAL_CUT_SEGMENTS)

        others = [location_id for name, location_id in locations.LOCATION_NAME_TO_ID.items()
                  if location_id not in set(cut_ids)]
        # The packs and fireworks (13_000 and up) sit above the block, clear of it.
        inside = [location_id for location_id in others if min(cut_ids) <= location_id <= max(cut_ids)]
        self.assertFalse(inside, "the cut block overlaps something else in the id space")

        for tower in data.TOWERS:
            self.assertLessEqual(tower.segments, data.CUT_STRIDE,
                                 f"{tower.prop_name} has more segments than the stride allows")

    def test_item_ids_derive_from_game_enums(self) -> None:
        # Still keyed on the big key's enum value: the mod resolves it as `id - base`.
        for tower in data.TOWERS:
            self.assertEqual(items.ITEM_NAME_TO_ID[tower.item_name],
                             data.BASE_ID + tower.prop_value)

    def test_puzzle_count_matches_the_game(self) -> None:
        # One puzzle per monument slot: 13 of the enum's 58 values produce nothing in the game.
        self.assertEqual(len(data.PUZZLES), 45)
        self.assertEqual(len({puzzle.prop_name for puzzle in data.PUZZLES}), 45)
        self.assertEqual(len({puzzle.location_name for puzzle in data.PUZZLES}), 45)
        self.assertEqual(len(data.PUZZLES), data.MAX_MONUMENT_SLOTS)

    def test_the_thirteen_absent_puzzles_are_not_locations(self) -> None:
        # Such a location can never be checked, so progression placed there blocks the seed.
        self.assertEqual(len(data.ABSENT_FROM_THE_BUILD), 13)
        table = {puzzle.prop_name for puzzle in data.PUZZLES}
        for prop_name in data.ABSENT_FROM_THE_BUILD:
            self.assertNotIn(prop_name, table)

    def test_cutting_the_thirteen_left_every_other_id_alone(self) -> None:
        # Ids are BASE_ID + the enum value, so removing rows cannot shift the others.
        self.assertEqual(data.puzzle_location_id(data.PUZZLES[0]), data.BASE_ID + 100)
        self.assertEqual(data.puzzle_location_id(data.PUZZLES[-1]), data.BASE_ID + 159)

    def test_cut_segment_counts_match_the_game(self) -> None:
        # Five segments each on the drawbridge and the four coloured towers, none on the rest.
        self.assertEqual(data.TOTAL_CUT_SEGMENTS, 25)
        with_segments = [tower for tower in data.TOWERS if tower.segments]
        self.assertEqual(len(with_segments), 5)
        for tower in with_segments:
            self.assertEqual(tower.segments, 5, tower.prop_name)

    def test_monument_slot_counts(self) -> None:
        # 4 + 5*4 + 6 + 15 slots across the monuments.
        self.assertEqual(data.MAX_MONUMENT_SLOTS, 45)

    def test_every_deposit_amount_has_a_location(self) -> None:
        for amount in range(1, data.MAX_MONUMENT_SLOTS + 1):
            self.assertIn(data.deposit_location_name(amount), locations.LOCATION_NAME_TO_ID)
