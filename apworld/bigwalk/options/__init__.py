"""Player-facing YAML options for the Big Walk world."""

from __future__ import annotations

from dataclasses import dataclass

from Options import (
    Choice, DefaultOnToggle, FreeText, OptionCounter, OptionGroup, OptionSet, PerGameCommonOptions, Range, StartInventoryPool, Toggle, Visibility,
)

from .. import data, tiles

from .goal_options import (
    Goal, GOAL_ON_THE_WIRE, OBJECTIVE_ENDINGS, EXCLUSIVE_ENDINGS, OBJECTIVE_KEYS, OBJECTIVE_ALIASES, Objectives,
    RandomObjectives, LockEndingsUntilObjectives, KeysRequired, LightsRequired, ObjectsRequired, BodyRequired,
    RadioStationsRequired, GourdsRequired, TowerMode, FirstTowerOpen, EveryoneGathers, TowersRequired,
)
from .sanity_options import (
    GourdSanity, GOURD_SANITY_ON_THE_WIRE, RadioSanity, PackSanity, FireworkSanity, ObjectSanity, ObjectSanityKinds,
    ObjectSanityItems, LightSanity, LightSanityItems, ObjectSanityJunkPercentage, LightSanityJunkPercentage, ZoneLocks,
    StartWithZoneLocks, TunnelSanity, FlareGunSanity, TowerSanity, ShuffleRadioMusic, MonumentSanity,
    CutKeySanity,
)
from .logic_options import (
    StartWithDrawbridgeOpen, StartWithMapRoomOpen, LinearKeys, LinearArchDoors, LinearTowers, LinearPuzzles,
    IncludeBigGamePuzzles, StartWithArchDoorsOpen, RequireArchDoors, LockPuzzleNeeds, StartWithPuzzleNeeds,
    StartWithRandomPuzzleNeed, GauntletMode, GauntletPuzzlesRequired, LockGauntletNeeds, GauntletStageItems,
    OpenBlackTower,
)
from .movement_options import LockAbilities, StartWithAbilities, LockRunning, RidesRequireSeat
from .pool_options import (
    BackpacksInPool, BeltsInPool, GourdCartonsInPool, TrapFillPercentage, TrapWeights, TrapLink, HardTraps,
    BonusFillPercentage, BonusWeights, IslandObjectLimits, TrapDuration, TrapsSpareTheGauntlet,
)
from .deathlink_options import (
    BigWalkDeathLink, DeathLinkTriggers, DeathLinkAmnesty, DeathLinkEffect, DeathLinkTrapWeights, DeathLinkTarget,
    DeathLinkTrapOnSend, DeathLinkRouletteHardTraps,
)
from .cosmetic_options import (
    RandomColors, CustomTiles, CustomDrawings, MirroredPoses, CustomTileChoices, CustomTileWeights,
)
from .qol_options import (
    HintDiscoveredKeyLocations, HintDiscoveredDepositBoxes, FastGolf, TeleportBackToHub, TrackerBoard,
    MapTracker, HintBoard, TowerResyncStations, TeleportButtons, CabinFeverSeconds, CabinFeverHelp,
    CabinFeverHideTimer, CabinFeverLongSeconds, CabinFeverLongHelp, CabinFeverLongHideTimer, TileThief,
)


@dataclass
class BigWalkOptions(PerGameCommonOptions):
    goal: Goal
    objectives: Objectives
    random_objectives: RandomObjectives
    lock_endings_until_objectives: LockEndingsUntilObjectives
    keys_required: KeysRequired
    radio_stations_required: RadioStationsRequired
    gourds_required: GourdsRequired
    gourd_sanity: GourdSanity
    radio_sanity: RadioSanity
    shuffle_radio_music: ShuffleRadioMusic
    pack_sanity: PackSanity
    firework_sanity: FireworkSanity
    start_with_drawbridge_open: StartWithDrawbridgeOpen
    start_with_arch_doors_open: StartWithArchDoorsOpen
    require_arch_doors: RequireArchDoors
    lock_puzzle_needs: LockPuzzleNeeds
    start_with_puzzle_needs: StartWithPuzzleNeeds
    start_with_random_puzzle_need: StartWithRandomPuzzleNeed
    lock_abilities: LockAbilities
    start_with_map_room_open: StartWithMapRoomOpen
    linear_keys: LinearKeys
    linear_arch_doors: LinearArchDoors
    tower_sanity: TowerSanity
    monument_sanity: MonumentSanity
    linear_towers: LinearTowers
    linear_puzzles: LinearPuzzles
    include_big_game_puzzles: IncludeBigGamePuzzles
    cut_key_sanity: CutKeySanity
    start_with_abilities: StartWithAbilities
    lock_running: LockRunning
    rides_require_seat: RidesRequireSeat
    fast_golf: FastGolf
    open_black_tower: OpenBlackTower
    tower_resync_stations: TowerResyncStations
    tracker_board: TrackerBoard
    hint_board: HintBoard
    map_tracker: MapTracker
    teleport_buttons: TeleportButtons
    teleport_back_to_hub: TeleportBackToHub
    cabin_fever_seconds: CabinFeverSeconds
    cabin_fever_help: CabinFeverHelp
    cabin_fever_long_seconds: CabinFeverLongSeconds
    cabin_fever_long_help: CabinFeverLongHelp
    cabin_fever_hide_timer: CabinFeverHideTimer
    cabin_fever_long_hide_timer: CabinFeverLongHideTimer
    tile_thief: TileThief
    custom_tiles: CustomTiles
    custom_tile_choices: CustomTileChoices
    custom_drawings: CustomDrawings
    custom_tile_weights: CustomTileWeights
    mirrored_poses: MirroredPoses
    gauntlet_mode: GauntletMode
    gauntlet_puzzles_required: GauntletPuzzlesRequired
    lock_gauntlet_needs: LockGauntletNeeds
    gauntlet_stage_items: GauntletStageItems
    trap_fill_percentage: TrapFillPercentage
    trap_weights: TrapWeights
    hard_traps: HardTraps
    bonus_fill_percentage: BonusFillPercentage
    bonus_weights: BonusWeights
    island_object_limits: IslandObjectLimits
    trap_duration: TrapDuration
    traps_spare_the_gauntlet: TrapsSpareTheGauntlet
    death_link: BigWalkDeathLink
    death_link_triggers: DeathLinkTriggers
    death_link_amnesty: DeathLinkAmnesty
    death_link_effect: DeathLinkEffect
    death_link_roulette_hard_traps: DeathLinkRouletteHardTraps
    death_link_trap_weights: DeathLinkTrapWeights
    death_link_trap_on_send: DeathLinkTrapOnSend
    death_link_target: DeathLinkTarget
    trap_link: TrapLink
    backpacks_in_pool: BackpacksInPool
    belts_in_pool: BeltsInPool
    gourd_cartons_in_pool: GourdCartonsInPool
    flare_gun_sanity: FlareGunSanity
    object_sanity: ObjectSanity
    object_sanity_kinds: ObjectSanityKinds
    object_sanity_items: ObjectSanityItems
    object_sanity_junk_percentage: ObjectSanityJunkPercentage
    light_sanity: LightSanity
    light_sanity_items: LightSanityItems
    light_sanity_junk_percentage: LightSanityJunkPercentage
    zone_locks: ZoneLocks
    start_with_zone_locks: StartWithZoneLocks
    tunnel_sanity: TunnelSanity
    lights_required: LightsRequired
    objects_required: ObjectsRequired
    body_required: BodyRequired
    random_colors: RandomColors
    tower_mode: TowerMode
    first_tower_open: FirstTowerOpen
    everyone_gathers: EveryoneGathers
    towers_required: TowersRequired
    hint_discovered_key_locations: HintDiscoveredKeyLocations
    hint_discovered_deposit_boxes: HintDiscoveredDepositBoxes
    start_inventory_from_pool: StartInventoryPool


option_groups = [
    OptionGroup("Goal", [
        Goal, Objectives, RandomObjectives, EveryoneGathers, LockEndingsUntilObjectives,
        GourdsRequired, KeysRequired, RadioStationsRequired, TowersRequired, LightsRequired, ObjectsRequired, BodyRequired,
    ]),
    OptionGroup("Towers", [TowerMode, FirstTowerOpen]),
    OptionGroup("Sanity", [
        GourdSanity, RadioSanity, ShuffleRadioMusic, PackSanity, FireworkSanity, FlareGunSanity, TowerSanity,
        MonumentSanity, CutKeySanity,
        ObjectSanity, ObjectSanityKinds, ObjectSanityItems, ObjectSanityJunkPercentage, LightSanity, LightSanityItems,
        LightSanityJunkPercentage, ZoneLocks, StartWithZoneLocks, TunnelSanity,
    ]),
    OptionGroup("Logic", [
        StartWithDrawbridgeOpen, StartWithMapRoomOpen, StartWithArchDoorsOpen, RequireArchDoors, OpenBlackTower,
        LockPuzzleNeeds, StartWithPuzzleNeeds, StartWithRandomPuzzleNeed, IncludeBigGamePuzzles,
        GauntletMode, GauntletStageItems, GauntletPuzzlesRequired, LockGauntletNeeds,
    ]),
    OptionGroup("Vanilla Behaviours", [LinearKeys, LinearArchDoors, LinearTowers, LinearPuzzles]),
    OptionGroup("Movement Locking", [LockAbilities, StartWithAbilities, LockRunning, RidesRequireSeat]),
    OptionGroup("Item Pool", [BackpacksInPool, BeltsInPool, GourdCartonsInPool, IslandObjectLimits]),
    OptionGroup("Colors", [RandomColors]),
    OptionGroup("Custom Tiles", [CustomTiles, CustomTileChoices, CustomTileWeights, CustomDrawings, MirroredPoses]),
    OptionGroup("Traps and Bonuses", [
        TrapFillPercentage, TrapWeights, HardTraps, BonusFillPercentage, BonusWeights, TrapDuration, TrapsSpareTheGauntlet,
        TrapLink,
    ]),
    OptionGroup("DeathLink", [
        BigWalkDeathLink, DeathLinkTriggers, DeathLinkAmnesty, DeathLinkEffect, DeathLinkTrapWeights,
        DeathLinkRouletteHardTraps, DeathLinkTrapOnSend, DeathLinkTarget,
    ]),
    OptionGroup("QoL", [
        HintDiscoveredKeyLocations, HintDiscoveredDepositBoxes, HintBoard, TrackerBoard, MapTracker,
        TeleportButtons, TeleportBackToHub, TowerResyncStations,
    ]),
    OptionGroup("Puzzle QoL", [
        FastGolf, CabinFeverSeconds, CabinFeverHelp, CabinFeverHideTimer, CabinFeverLongSeconds, CabinFeverLongHelp,
        CabinFeverLongHideTimer, TileThief,
    ]),
]

option_presets = {
    "Full Run": {
        "goal": Goal.option_big_goodbye,
        "gourd_sanity": GourdSanity.option_every_gourd,
        "radio_sanity": True,
        "shuffle_radio_music": True,
    },
    "Short": {
        "goal": Goal.option_big_wall,
        "gourd_sanity": GourdSanity.option_every_5,
        "radio_sanity": True,
        "shuffle_radio_music": True,
    },
}
