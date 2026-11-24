"""Access rules for the Big Walk world."""

from __future__ import annotations

from typing import TYPE_CHECKING

from itertools import combinations

from BaseClasses import LocationProgressType

from rule_builder.rules import And, CanReachLocation, HasFromList, CanReachRegion, Has, HasAll, HasAny, Or, Rule

from ..options import GauntletMode, GauntletStageItems, Goal
from .common import (abilities_locked, all_of, body_rule, gauntlet_body_through, past_far_door, place_rule,
                     purple_hole, with_all, with_zone)
from .junk import PROGRESSION_MARGIN, exclude_as_junk, room_for_progression, set_pickup_junk
from .towers import (black_tower_door, doors_up_to, five_monuments_full, monument_holding, monuments_up_to,
                     set_gourd_deposit_rules, set_key_cut_rules, set_key_deposit_rules,
                     set_monument_full_rules, set_tower_reached_rules, tower_open)
from .places import (into_the_tunnels, light_rule, object_rule, restrict_object_items, set_firework_rules,
                     set_light_rules, set_object_event_rules, set_object_rules, set_puzzle_rules)
from .goals import (gauntlet_goal_rule, gauntlet_parts_are_items, reach_at_least, set_completion_rule,
                    set_entrance_rules, set_gauntlet_rules, stairways_up_to)

if TYPE_CHECKING:
    from ..world import BigWalkWorld


def set_all_rules(world: BigWalkWorld) -> None:
    set_key_deposit_rules(world)
    set_tower_reached_rules(world)
    set_key_cut_rules(world)
    set_pickup_junk(world)
    set_gourd_deposit_rules(world)
    set_puzzle_rules(world)
    set_gauntlet_rules(world)
    set_object_rules(world)
    set_firework_rules(world)
    set_object_event_rules(world)
    set_monument_full_rules(world)
    set_light_rules(world)
    set_entrance_rules(world)
    set_completion_rule(world)
