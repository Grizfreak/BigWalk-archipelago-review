"""Static Big Walk data shared by items.py, locations.py and rules.py, with their ids."""

from __future__ import annotations

from typing import NamedTuple

from .ids import (
    BASE_ID, RADIO_ID_OFFSET, DEPOSIT_ID_OFFSET, GAUNTLET_ID_OFFSET, CUT_ID_OFFSET,
    KEY_ITEM_ID_OFFSET, ARCH_DOOR_ID_OFFSET, PUZZLE_NEED_ID_OFFSET, CUT_STRIDE, GOURD_ITEM_NAME,
    GOURD_CARTON_ITEM_NAME,
)
from .puzzles import (
    Puzzle, PUZZLES, PuzzleNeed, PUZZLE_NEEDS, EARLY_NEEDS, PUZZLE_NEED_KEYS, PUZZLE_NEEDS_BY_KEY,
    NON_ITEM_TAGS, PUZZLE_TAGS, puzzle_needs, ABSENT_FROM_THE_BUILD, puzzle_location_id,
    puzzle_need_item_id,
)
from .gauntlet import (
    GAUNTLET_STAGE_TAGS, GauntletStage, GAUNTLET_STAGES, PROGRESSIVE_GAUNTLET_ITEM_NAME,
    PROGRESSIVE_GAUNTLET_VALUE, progressive_gauntlet_id, gauntlet_stage_id, gauntlet_needs_through,
    gauntlet_needs_of,
)
from .towers import (
    Tower, DRAWBRIDGE_ITEM_NAME, SPAWN_SECRET_DOOR_ITEM_NAME, TOWERS, TOTAL_CUT_SEGMENTS,
    GREEN_DOME, GREEN_CUP, YELLOW_TWIST, BLACK_MONOLITH, MAX_MONUMENT_SLOTS, RadioStation,
    RADIO_STATIONS, TOWER_REACHED_ID_OFFSET, reachable_towers, reached_towers,
    tower_reached_location_name, MONUMENT_FULL_ID_OFFSET, monument_full_location_name,
    monument_full_location_id, MONUMENT_KEY_ID_OFFSET, monument_key_location_name,
    monument_key_location_id, tower_reached_location_id, PROGRESSIVE_MONUMENT_ITEM_NAME,
    PROGRESSIVE_MONUMENT_ID_OFFSET, progressive_monument_id, MAP_ROOM_ITEM_NAME, tower_by_prop,
    key_deposit_location_id, feature_item_id, key_item_name, key_item_id, cut_location_name,
    cut_location_id, cut_locations, radio_location_id, radio_item_id, deposit_location_name,
    deposit_location_id,
)
from .areas import (
    tutorial_footholds, START_ZONE_PUZZLES, CHAIRLIFT_PUZZLES, CHAIRLIFT_RADIO, TUNNEL_RADIO,
    chairlift_locations, tunnel_locations, ENDING_GATE_SYSTEM, GAUNTLET_COMPLETE_SYSTEM,
)
from .filler import (
    FILLER_ITEMS, FLARE_GUN_ITEM_NAMES, RAINBOW_FLARE_GUN_ITEM_NAME, PALETTE_SIZE, Effect, TRAPS,
    BONUSES, CONFETTI_ITEM_NAME, TRAP_ITEMS, BONUS_ITEMS, DEATH_LINK_TRIGGERS,
)
from .teleports import (
    TELEPORT_ID_OFFSET, TELEPORT_DESTINATIONS, teleport_item_name, teleport_item_id,
)
from .pickups import (
    Pickup, PICKUP_ID_OFFSET, PICKUPS, FLARE_GUN_ID_OFFSET, FLARE_GUN_PICKUPS, Firework,
    FIREWORK_ID_OFFSET, FIREWORKS, pickup_location_id, firework_location_id, flare_gun_location_id,
)
from .abilities import (
    Ability, ABILITY_ID_OFFSET, ABILITIES, ABILITY_LOCK_KEYS, LEGS_ITEM_NAME, SEAT_ITEM_NAME,
    LEFT_ARM_ITEM_NAME, RIGHT_ARM_ITEM_NAME, ARMS_ITEM_NAME, THROWING_ITEM_NAME, CARRYING_ITEM_NAME,
    RUNNING_ITEM_NAME, Body, ARM_RAISING, ARM_POINTING, ARM_EXTENDING, POSES, ABILITY_PUZZLES,
    GAUNTLET_STAGE_ABILITIES, OBJECT_ABILITIES, OBJECT_GUID_ABILITIES, ability_item_id,
)
from .objects import (
    IslandObject, OBJECT_ID_OFFSET, OBJECT_KIND_ID_OFFSET, ISLAND_OBJECTS, OBJECT_KINDS,
    OBJECT_KIND_KEYS, OBJECT_KINDS_LEFT_OUT_BY_DEFAULT, OBJECT_KINDS_NEVER_TAKEN,
    OBJECT_EVENT_ITEM_NAME, object_event_name, object_kind_location_name, object_location_id,
    object_kind_location_id,
)
from .lights import (
    Light, LIGHT_ID_OFFSET, LIGHT_EVENT_ITEM_NAME, LIGHT_SWITCHES, LAMP_KINDS, LIGHTS,
    light_event_name, light_location_id,
)
from .zones import (
    ZONE_LOCK_ID_OFFSET, ZoneLock, ZONE_COLOURS, ZONE_LOCKS, ZONE_LOCK_KEYS, OVERWORLD_ZONES,
    zone_of, zone_lock_for, zone_lock_item_id,
)
from .doors import (
    ArchDoor, FIRST_ARCH_DOOR, LEFT_ARCH_DOOR, RIGHT_ARCH_DOOR, ARCH_DOORS, TOWER_FAR_DOOR,
    ZONE_FAR_DOOR, arch_door_item_id, TUNNEL_DOOR_ID_OFFSET, TunnelDoor, PURPLE_TUNNEL_ITEM_NAME,
    TUNNEL_DOORS, tunnel_door_item_id,
)
