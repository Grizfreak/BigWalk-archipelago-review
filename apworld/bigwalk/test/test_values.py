"""Counts, start items and slot_data values, each against a number worked out here."""

import json
from io import StringIO

from BaseClasses import LocationProgressType

from .. import checks, data, items, monuments
from ..rules import junk
from .bases import BigWalkTestBase

MONUMENT = data.PROGRESSIVE_MONUMENT_ITEM_NAME


class ValuesTestBase(BigWalkTestBase):
    run_default_tests = False

    def local_early(self) -> dict[str, int]:
        return self.multiworld.local_early_items[self.player]

    def start_items(self) -> set[str]:
        return {item.name for item in self.multiworld.precollected_items[self.player]}


class TestTheFirstMonumentComesEarly(ValuesTestBase):
    options = {"tower_mode": "by_item", "first_tower_open": False}

    def test_early_or_given(self) -> None:
        self.assertTrue(self.local_early().get(MONUMENT) == 1 or MONUMENT in self.start_items())


class TestNoMonumentItemNoEarlyMonument(ValuesTestBase):
    options = {"tower_mode": "in_order"}

    def test_nothing_asked_for(self) -> None:
        self.assertNotIn(MONUMENT, self.local_early())


class TestNoNeedsNoEarlyNeeds(ValuesTestBase):
    options = {"lock_puzzle_needs": False}

    def test_nothing_asked_for(self) -> None:
        self.assertFalse({need.item_name for need in data.PUZZLE_NEEDS} & set(self.multiworld.early_items[self.player]))


class TestTheWayOutIsTheDrawbridgesKey(ValuesTestBase):
    options = {"linear_arch_doors": True, "linear_keys": True, "start_with_drawbridge_open": False}

    def test_its_own_key(self) -> None:
        self.assertEqual(self.world.way_out, (data.key_item_name(data.TOWERS[0]),))


class TestTheTutorialMonumentFirst(ValuesTestBase):
    options = {"tower_mode": "in_order"}

    def test_only_when_the_order_starts_with_it(self) -> None:
        props = [tower.prop_name for tower in data.TOWERS]
        self.world.tower_order = tuple(props)
        self.assertTrue(monuments.tutorial_monument_first(self.world))
        self.world.tower_order = (props[1], props[0], *props[2:])
        self.assertFalse(monuments.tutorial_monument_first(self.world))


class TestMonumentKeys(ValuesTestBase):
    options = {"linear_towers": "one_deposit_box"}

    def test_each_frees_its_key_within_its_slots(self) -> None:
        for tower in data.TOWERS:
            self.assertIn(self.world.monument_key_gourds[tower.prop_name], range(1, tower.slots + 1))


class TestEveryObjectRequired(ValuesTestBase):
    options = {"object_sanity": "every", "objects_required": 100}

    def test_all_of_them(self) -> None:
        self.assertEqual(checks.objects_target(self.world), len(checks.object_location_names(self.world)))


class TestTheSmallestShares(ValuesTestBase):
    options = {"object_sanity": "every", "objects_required": 1, "light_sanity": "lamps", "lights_required": 1,
               "lock_abilities": "handed", "body_required": 1}

    def test_at_least_one(self) -> None:
        self.assertEqual(checks.lights_target(self.world), 1)
        self.assertEqual(items.body_target(self.world), 1)
        self.assertEqual(checks.objects_target(self.world), 1 + (len(checks.object_location_names(self.world)) > 100))


class TestNoneToCount(ValuesTestBase):
    options = {"object_sanity": "off", "light_sanity": "off", "lock_abilities": "disabled"}

    def test_no_target(self) -> None:
        self.assertEqual(checks.objects_target(self.world), 0)
        self.assertEqual(checks.lights_target(self.world), 0)
        self.assertEqual(items.body_target(self.world), 0)


class TestZoneLockedGadgets(ValuesTestBase):
    options = {"object_sanity": "every", "light_sanity": "lamps", "zone_locks": True}

    def test_each_gadgets_item_hides_gadgets_only(self) -> None:
        sent = self.world.fill_slot_data()["zone_locked_gadgets"]
        gadgets = {str(data.zone_lock_item_id(lock)) for lock in data.ZONE_LOCKS if not lock.lamps}
        self.assertEqual(set(sent), gadgets)
        kinds = {obj.guid: obj.kind for obj in data.ISLAND_OBJECTS}
        for guids in sent.values():
            self.assertTrue(guids)
            self.assertFalse([guid for guid in guids if kinds[guid] in data.LAMP_KINDS])


class TestTheJunkBudget(ValuesTestBase):
    options = {"object_sanity": "every", "object_sanity_items": "junk", "light_sanity": "off"}

    def test_what_is_left_is_the_junk_not_spoken_for(self) -> None:
        own = [item for item in self.multiworld.itempool if item.player == self.player]
        junk_items = sum(1 for item in own if not item.advancement and not item.useful)
        excluded = sum(1 for location in self.multiworld.get_locations(self.player)
                       if location.progress_type == LocationProgressType.EXCLUDED)
        self.assertGreater(excluded, 0)
        self.assertEqual(self.world.__dict__["junk_budget"], junk_items - excluded)

    def test_room_for_progression_counts_each_location_once(self) -> None:
        progression = sum(1 for item in self.multiworld.itempool if item.player == self.player and item.advancement)
        free = [location for location in self.multiworld.get_unfilled_locations(self.player)
                if location.address is not None and location.progress_type != LocationProgressType.EXCLUDED]
        margin = max(junk.PROGRESSION_MARGIN, progression // 2)
        self.assertEqual(junk.room_for_progression(self.world), max(0, len(free) - progression - margin))


class TestPacksGiveThemselves(ValuesTestBase):
    options = {"pack_sanity": "vanilla"}

    def test_each_pack_holds_its_own_kind(self) -> None:
        for pickup in data.PICKUPS:
            item = self.multiworld.get_location(pickup.location_name, self.player).item.name
            if pickup.location_name.endswith(("Backpack", "Belt")):
                self.assertTrue(pickup.location_name.endswith(item), pickup.location_name)
            else:
                self.assertEqual(item, data.GOURD_CARTON_ITEM_NAME)


class TestDeathLinkRoulette(ValuesTestBase):
    soft = next(trap for trap in data.TRAPS if not trap.hard)
    options = {"death_link_trap_weights": {soft.item_name: 1}}

    def test_a_weight_of_one_counts(self) -> None:
        self.assertEqual(checks.death_link_roulette(self.world), {self.soft.key: 1})


class TestTheSpoilersGourdName(ValuesTestBase):
    options = {"gourd_name": "Pumpkin"}

    def spoiler(self) -> str:
        out = StringIO()
        self.world.write_spoiler_header(out)
        return out.getvalue()

    def test_a_chosen_name_is_written(self) -> None:
        self.assertIn("Pumpkin", self.spoiler())

    def test_the_games_own_is_not(self) -> None:
        self.world.gourd_name = data.GOURD_ITEM_NAME
        self.assertNotIn("Gourds shown in game as", self.spoiler())


class TestTheExportedRules(ValuesTestBase):
    def test_each_distinct_rule_is_sent_once(self) -> None:
        rules = [json.dumps(rule) for rule in self.world.fill_slot_data()["logic"]["rules"]]
        self.assertEqual(len(rules), len(set(rules)))
