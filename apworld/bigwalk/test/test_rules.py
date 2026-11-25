"""What a place asks for stays asked: each rule is checked by taking its item away."""

from .. import data, regions
from ..rules.common import gauntlet_body_through
from .bases import BigWalkTestBase, NeedsChecks

LEFT = data.LEFT_ARCH_DOOR.item_name
RIGHT = data.RIGHT_ARCH_DOOR.item_name
BLUE = data.tower_by_prop("bigKeyBlueZone").item_name


class RulesTestBase(NeedsChecks, BigWalkTestBase):
    run_default_tests = False


class TestLightEvents(RulesTestBase):
    options = {"objectives": ["big_lights"], "light_sanity": "lamps_and_switches"}

    def test_the_trains_lights_need_the_train(self) -> None:
        found = [light for light in data.LIGHTS if light.zone == "train"]
        self.assertTrue(found)
        for light in found:
            self.assert_needs(data.light_event_name(light), BLUE)
            self.assert_needs(light.location_name, BLUE)

    def test_the_lights_past_the_purple_hole_need_the_tunnels(self) -> None:
        found = [light for light in data.LIGHTS if light.zone == "purple"]
        self.assertTrue(found)
        for light in found:
            self.assert_needs(data.light_event_name(light), data.YELLOW_TWIST.item_name)


class TestTrainLightsWithoutSeat(RulesTestBase):
    options = {"light_sanity": "lamps_and_switches", "lock_abilities": "handed", "rides_require_seat": False}

    def test_they_do_not_need_seat(self) -> None:
        light = next(light for light in data.LIGHTS if light.zone == "train")
        self.assertTrue(self.reached_without(light.location_name, data.SEAT_ITEM_NAME))


class TestTrainLightsWithSeat(RulesTestBase):
    options = {"light_sanity": "lamps_and_switches", "lock_abilities": "handed", "rides_require_seat": True}

    def test_they_need_seat(self) -> None:
        light = next(light for light in data.LIGHTS if light.zone == "train")
        self.assert_needs(light.location_name, data.SEAT_ITEM_NAME)


class TestObjectEvents(RulesTestBase):
    options = {"objectives": ["big_objects"], "object_sanity": "every", "lock_abilities": "handed"}

    def test_an_event_needs_what_its_object_needs(self) -> None:
        swing = next(obj for obj in data.ISLAND_OBJECTS if obj.guid in data.OBJECT_GUID_ABILITIES)
        self.assert_needs(swing.location_name, data.SEAT_ITEM_NAME)
        self.assert_needs(data.object_event_name(swing.location_name), data.SEAT_ITEM_NAME)


class TestNoObjectEvents(RulesTestBase):
    options = {"objectives": ["big_collection"], "object_sanity": "every"}

    def test_only_with_the_objective(self) -> None:
        names = {location.name for location in self.multiworld.get_locations(self.player)}
        self.assertFalse([obj for obj in data.ISLAND_OBJECTS if data.object_event_name(obj.location_name) in names])


class TestFirstPickup(RulesTestBase):
    options = {"object_sanity": "first_pickup", "object_sanity_items": "any", "lock_abilities": "handed"}

    def label(self, kind: str) -> str:
        return data.object_kind_location_name(dict(data.OBJECT_KINDS)[kind])

    def test_a_kind_with_no_free_object_keeps_its_rule(self) -> None:
        self.assert_needs(self.label("MegaphoneProp"), data.CARRYING_ITEM_NAME)

    def test_only_a_kind_found_in_the_tutorial_starts_there(self) -> None:
        for kind, label in data.OBJECT_KINDS:
            in_tutorial = any(obj.kind == kind and obj.zone == "start" for obj in data.ISLAND_OBJECTS)
            name = data.object_kind_location_name(label)
            if name in {location.name for location in self.multiworld.get_locations(self.player)}:
                region = self.multiworld.get_location(name, self.player).parent_region.name
                self.assertEqual(region == regions.STARTING_ZONE, in_tutorial, name)


class TestFarDoors(RulesTestBase):
    options = {"require_arch_doors": True, "start_with_arch_doors_open": [], "tower_mode": "all_open",
               "object_sanity": "every"}

    def test_a_towers_object_needs_the_door_before_it(self) -> None:
        for prop, door in (("bigKeyBlueZone", LEFT), ("bigKeyGreenZone", RIGHT)):
            obj = next(obj for obj in data.ISLAND_OBJECTS if obj.zone == f"tower:{prop}")
            self.assert_needs(obj.location_name, door)


class TestFarDoorsInOrder(RulesTestBase):
    options = {"require_arch_doors": True, "start_with_arch_doors_open": [], "tower_mode": "in_order",
               "gourd_sanity": "every_gourd"}

    def test_the_last_deposit_needs_every_far_door(self) -> None:
        last = data.deposit_location_name(max(self.world.deposit_amounts))
        self.assert_needs(last, LEFT)
        self.assert_needs(last, RIGHT)


class TestBlackTowerDoor(RulesTestBase):
    options = {"require_arch_doors": True, "start_with_arch_doors_open": [], "tower_mode": "all_open",
               "open_black_tower": False, "tower_sanity": True}

    def test_it_needs_the_doors_of_the_five_monuments(self) -> None:
        reached = data.tower_reached_location_name(data.BLACK_MONOLITH)
        self.assert_needs(reached, LEFT)
        self.assert_needs(reached, RIGHT)


class TestProgressiveGauntlet(RulesTestBase):
    options = {"gauntlet_mode": "locked_stages", "gauntlet_stage_items": "progressive", "lock_puzzle_needs": False}

    def test_stage_by_stage(self) -> None:
        doors = [item for item in self.multiworld.itempool if item.name == data.PROGRESSIVE_GAUNTLET_ITEM_NAME]
        self.assertEqual(len(doors), len(data.GAUNTLET_STAGES))
        self.collect_all_but(data.PROGRESSIVE_GAUNTLET_ITEM_NAME)
        for stage in data.GAUNTLET_STAGES:
            self.assertTrue(self.can_reach_location(stage.location_name), stage.location_name)
            for later in data.GAUNTLET_STAGES[stage.index + 1:]:
                self.assertFalse(self.can_reach_location(later.location_name), later.location_name)
            self.collect(doors[stage.index])


class TestGauntletBody(RulesTestBase):
    options = {"gauntlet_mode": "locked_stages", "gauntlet_stage_items": "individual", "lock_puzzle_needs": False,
               "lock_abilities": "handed"}

    def test_the_last_stage_needs_what_the_stages_below_ask_of_the_body(self) -> None:
        stages = data.GAUNTLET_STAGE_ABILITIES
        wanted = [data.SEAT_ITEM_NAME] * any(s.seat for s in stages)
        wanted += [data.THROWING_ITEM_NAME] * any(s.throwing for s in stages)
        wanted += [data.CARRYING_ITEM_NAME] * any(s.carrying for s in stages)
        wanted += [data.LEFT_ARM_ITEM_NAME, data.RIGHT_ARM_ITEM_NAME] * any(s.arms for s in stages)
        self.assertTrue(wanted)
        for item in wanted:
            self.assert_needs(data.GAUNTLET_STAGES[-1].location_name, item)

    def test_no_stage_asks_nothing(self) -> None:
        self.assertIsNone(gauntlet_body_through(self.world, 0))
