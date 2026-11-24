"""Static Big Walk data shared by items.py, locations.py and rules.py, with their ids."""

from __future__ import annotations

from typing import NamedTuple

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

# Puzzles (SaveablePropName.gourdXxx); dev-only values and gourdSecretZoneVice are left out.


class Puzzle(NamedTuple):
    prop_name: str
    """`SaveablePropName` enum name, exactly as the game writes it to the save."""
    prop_value: int
    """`SaveablePropName` enum value; the location id is BASE_ID + this."""
    location_name: str


PUZZLES: tuple[Puzzle, ...] = (
    Puzzle("gourdCabinFever", 100, "Cabin Fever Puzzle"),
    Puzzle("gourdHighButton", 101, "High Button Puzzle"),
    Puzzle("gourdFielding", 103, "Fielding Puzzle"),
    Puzzle("gourdCannonBall", 104, "Cannonball Puzzle"),
    Puzzle("gourdInvisibleInk", 105, "Invisible Ink Puzzle"),
    Puzzle("gourdTrapRoom", 106, "Trap Room Puzzle"),
    Puzzle("gourdMediumSimPress", 107, "Medium Simultaneous Press Puzzle"),
    Puzzle("gourdEasySimPress", 108, "Easy Simultaneous Press Puzzle"),
    Puzzle("gourdRingRoom", 109, "Ring Room Puzzle"),
    Puzzle("gourdObby", 113, "Obby Puzzle"),
    Puzzle("gourdCarousel", 114, "Carousel Puzzle"),
    Puzzle("gourdCoordinates", 115, "Coordinates Puzzle"),
    Puzzle("gourdTelescopeToBox", 116, "Telescope to Box Puzzle"),
    Puzzle("gourdObservationRoom", 117, "Observation Room Puzzle"),
    Puzzle("gourdWindowLabyrinth", 118, "Window Labyrinth Puzzle"),
    Puzzle("gourdBasketball", 121, "Basketball Puzzle"),
    Puzzle("gourdConcert", 122, "Concert Puzzle"),
    Puzzle("gourdIndoorSemaphore", 123, "Indoor Semaphore Puzzle"),
    Puzzle("gourdOpticalTelegraph", 124, "Optical Telegraph Puzzle"),
    # The game's own enum misspells "Priest"; the location name does not.
    Puzzle("gourdPoetAndPreist", 125, "Poet and Priest Puzzle"),
    Puzzle("gourdMemoryBombs", 127, "Memory Bombs Puzzle"),
    Puzzle("gourdTileThief", 133, "Tile Thief Puzzle"),
    Puzzle("gourdCharadesRooms", 134, "Charades Rooms Puzzle"),
    Puzzle("gourdMicrophoneArray", 135, "Microphone Array Puzzle"),
    Puzzle("gourdPointersParadise", 137, "Pointer's Paradise Puzzle"),
    Puzzle("gourdCoordinatesHolding", 138, "Coordinates Holding Puzzle"),
    Puzzle("gourdEggHunt", 139, "Egg Hunt Puzzle"),
    Puzzle("gourdTellerWindow", 140, "Teller Window Puzzle"),
    Puzzle("gourdSignalFlags", 141, "Signal Flags Puzzle"),
    Puzzle("gourdCabinFeverLong", 142, "Cabin Fever Long Puzzle"),
    Puzzle("gourdBreadcrumbLoop", 143, "Breadcrumb Loop Puzzle"),
    Puzzle("gourdScoutBombs", 144, "Scout Bombs Puzzle"),
    # Another game-side typo ("Centuron"), kept only in prop_name.
    Puzzle("gourdCenturonSong", 147, "Centurion Song Puzzle"),
    Puzzle("gourdMusicalHoliday", 148, "Musical Holiday Puzzle"),
    Puzzle("gourdKickUpPits", 149, "Kick Up Pits Puzzle"),
    Puzzle("gourdSingerAndSelecter", 150, "Singer and Selector Puzzle"),
    Puzzle("gourdDancerAndSelecter", 151, "Dancer and Selector Puzzle"),
    Puzzle("gourdSpeedObby", 152, "Speed Obby Puzzle"),
    Puzzle("gourdBlindfoldCatwalk", 153, "Blindfold Catwalk Puzzle"),
    Puzzle("gourdBlindfoldFishtrap", 154, "Blindfold Fishtrap Puzzle"),
    Puzzle("gourdPerspectiveCounting", 155, "Perspective Counting Puzzle"),
    Puzzle("gourdCenturionSeance", 156, "Centurion Seance Puzzle"),
    Puzzle("gourdFlareRun", 157, "Flare Run Puzzle"),
    Puzzle("gourdCannonballCommute", 158, "Cannonball Commute Puzzle"),
    Puzzle("gourdPoetAndPontiff", 159, "Poet and Pontiff Puzzle"),
)

class PuzzleNeed(NamedTuple):
    key: str
    """Tag used in PUZZLE_TAGS and as the player-facing option value."""
    item_name: str


PUZZLE_NEEDS: tuple[PuzzleNeed, ...] = (
    PuzzleNeed("buttons", "Buttons Unlock"),
    PuzzleNeed("sync_buttons", "Synchronized Buttons Unlock"),
    PuzzleNeed("icon_panels", "Icon Panels Unlock"),
    PuzzleNeed("drawing_panels", "Drawing Panels Unlock"),
    PuzzleNeed("pose_panels", "Pose Panels Unlock"),
    PuzzleNeed("sound_panels", "Sound Panels Unlock"),
    PuzzleNeed("point_panels", "Point Panels Unlock"),
    PuzzleNeed("speakers", "Speakers Unlock"),
    PuzzleNeed("lights", "Lights Unlock"),
    PuzzleNeed("teapots", "Teapots Unlock"),
    PuzzleNeed("timed_tomato", "Timed Tomato Unlock"),
    PuzzleNeed("golf_ball", "Golf Ball Unlock"),
    PuzzleNeed("big_head", "Big Head Mask Unlock"),
    PuzzleNeed("ink_viewer", "Ink Viewer Unlock"),
    PuzzleNeed("counter", "Counter Unlock"),
    PuzzleNeed("coordinates_computer", "Coordinates Computer Unlock"),
    PuzzleNeed("eggs", "Terrain Pegs Unlock"),
)
"""The needs `lock_puzzle_needs` turns into items; keep the order, an item's id is its position."""

EARLY_NEEDS: tuple[PuzzleNeed, ...] = PUZZLE_NEEDS[0:3]
"""Buttons, Synchronized Buttons, Icon Panels: the needs most puzzles share, asked for early."""

PUZZLE_NEED_KEYS: frozenset[str] = frozenset(need.key for need in PUZZLE_NEEDS)
PUZZLE_NEEDS_BY_KEY: dict[str, PuzzleNeed] = {need.key: need for need in PUZZLE_NEEDS}

NON_ITEM_TAGS: frozenset[str] = frozenset({"coop", "throwable", "item_pickup"})

PUZZLE_TAGS: dict[str, frozenset[str]] = {
    "gourdCabinFever": frozenset({"coop", "buttons"}),
    "gourdHighButton": frozenset({"coop", "buttons"}),
    "gourdFielding": frozenset({"coop", "timed_tomato", "buttons"}),
    "gourdCannonBall": frozenset({"golf_ball"}),
    "gourdInvisibleInk": frozenset({"coop", "buttons", "icon_panels", "ink_viewer"}),
    "gourdTrapRoom": frozenset({"coop", "buttons", "icon_panels"}),
    "gourdMediumSimPress": frozenset({"coop", "sync_buttons"}),
    "gourdEasySimPress": frozenset({"coop", "sync_buttons"}),
    "gourdRingRoom": frozenset({"coop", "speakers", "icon_panels", "lights", "buttons"}),
    "gourdObby": frozenset({"coop", "sync_buttons", "timed_tomato", "buttons"}),
    "gourdCarousel": frozenset({"buttons", "icon_panels"}),
    "gourdCoordinates": frozenset({"coop", "sync_buttons", "coordinates_computer"}),
    "gourdTelescopeToBox": frozenset({"coop", "buttons"}),
    "gourdObservationRoom": frozenset({"coop", "lights", "icon_panels", "buttons"}),
    "gourdWindowLabyrinth": frozenset({"coop", "timed_tomato"}),
    "gourdBasketball": frozenset({"throwable"}),
    "gourdConcert": frozenset({"coop", "buttons"}),
    "gourdIndoorSemaphore": frozenset({"coop", "pose_panels", "buttons", "icon_panels"}),
    "gourdOpticalTelegraph": frozenset({"coop", "pose_panels", "buttons"}),
    "gourdPoetAndPreist": frozenset({"coop", "drawing_panels", "buttons"}),
    "gourdMemoryBombs": frozenset({"buttons"}),
    "gourdTileThief": frozenset({"icon_panels", "drawing_panels", "pose_panels", "sound_panels", "buttons"}),
    "gourdCharadesRooms": frozenset({"coop", "icon_panels", "buttons", "pose_panels"}),
    "gourdMicrophoneArray": frozenset({"coop", "buttons"}),
    "gourdPointersParadise": frozenset({"coop", "buttons"}),
    "gourdCoordinatesHolding": frozenset({"coop", "buttons", "coordinates_computer"}),
    "gourdEggHunt": frozenset({"eggs", "buttons"}),
    "gourdTellerWindow": frozenset({"coop", "icon_panels", "buttons"}),
    "gourdSignalFlags": frozenset({"coop", "icon_panels", "counter", "buttons"}),
    "gourdCabinFeverLong": frozenset({"coop", "buttons"}),
    "gourdBreadcrumbLoop": frozenset({"coop", "sync_buttons"}),
    "gourdScoutBombs": frozenset({"coop", "buttons", "teapots", "point_panels"}),
    "gourdCenturonSong": frozenset({"coop", "buttons"}),
    "gourdMusicalHoliday": frozenset({"coop", "buttons"}),
    "gourdKickUpPits": frozenset(),
    "gourdSingerAndSelecter": frozenset({"coop", "buttons", "sound_panels", "speakers"}),
    "gourdDancerAndSelecter": frozenset({"coop", "buttons", "sound_panels"}),
    "gourdSpeedObby": frozenset({"coop", "timed_tomato", "sync_buttons", "buttons"}),
    "gourdBlindfoldCatwalk": frozenset({"coop", "big_head", "buttons"}),
    "gourdBlindfoldFishtrap": frozenset({"coop", "big_head", "icon_panels", "buttons"}),
    "gourdPerspectiveCounting": frozenset({"coop", "counter", "buttons"}),
    "gourdCenturionSeance": frozenset({"coop", "buttons", "speakers", "lights"}),
    "gourdFlareRun": frozenset({"coop", "buttons"}),
    "gourdCannonballCommute": frozenset({"golf_ball"}),
    "gourdPoetAndPontiff": frozenset({"coop", "buttons", "drawing_panels"}),
}
"""Every puzzle's needs, by `prop_name`: a PUZZLE_NEEDS key or one of NON_ITEM_TAGS."""

GAUNTLET_STAGE_TAGS: tuple[frozenset[str], ...] = (
    frozenset({"coop", "icon_panels", "buttons"}),
    frozenset({"coop", "buttons"}),
    frozenset({"coop", "timed_tomato"}),
    frozenset({"coop", "sync_buttons"}),
    frozenset({"coop", "teapots", "buttons"}),
    frozenset({"coop", "icon_panels", "buttons", "ink_viewer"}),
    frozenset({"coop", "buttons"}),
)
"""The needs of the Silent Gauntlet's seven stages, in order."""


class GauntletStage(NamedTuple):
    index: int
    """0..6: the game's `Level<index>` under `SilentGauntlet 2Player/Positioner`."""
    system_name: str
    """`SavableSystem` enum name, written 0 -> 1 when the stage's puzzle is solved."""
    system_value: int
    """`SavableSystem` enum value; both ids are BASE_ID + GAUNTLET_ID_OFFSET + this."""
    chamber: str
    """The stage's puzzle, in the words of the game's chamber names."""

    @property
    def number(self) -> int:
        return self.index + 1

    @property
    def location_name(self) -> str:
        return f"Gauntlet Stage {self.number}: {self.chamber}"

    @property
    def item_name(self) -> str:
        """Opens the stairway out of this stage, the door `GourdTower_Gate All Hold`."""
        return f"Gauntlet Stage {self.number} Door"


GAUNTLET_STAGES: tuple[GauntletStage, ...] = (
    GauntletStage(0, "GauntletChamber0", 50, "Peg Board"),
    GauntletStage(1, "GauntletChamber1", 51, "Pointers"),
    GauntletStage(2, "GauntletChamber2", 52, "Kick To Kick"),
    GauntletStage(3, "GauntletChamber3", 53, "Sim Press"),
    GauntletStage(4, "GauntletChamber4", 54, "Volley Ball"),
    GauntletStage(5, "GauntletChamber5", 55, "Invisible"),
    GauntletStage(6, "GauntletChamber6", 56, "Sculptures"),
)


PROGRESSIVE_GAUNTLET_ITEM_NAME = "Progressive Gauntlet Door"
PROGRESSIVE_GAUNTLET_VALUE = 60
"""Past the stage values (50..56); each copy opens the lowest stage still shut."""


def progressive_gauntlet_id() -> int:
    return BASE_ID + GAUNTLET_ID_OFFSET + PROGRESSIVE_GAUNTLET_VALUE


def gauntlet_stage_id(stage: GauntletStage) -> int:
    return BASE_ID + GAUNTLET_ID_OFFSET + stage.system_value


def gauntlet_needs_through(stage: GauntletStage) -> tuple[PuzzleNeed, ...]:
    """The needs of this stage and of every stage below it."""
    tags = frozenset().union(*GAUNTLET_STAGE_TAGS[:stage.index + 1])
    return tuple(need for need in PUZZLE_NEEDS if need.key in tags)


def gauntlet_needs_of(stage: GauntletStage) -> tuple[PuzzleNeed, ...]:
    """The needs of this stage alone, for when the stages below it can be skipped."""
    tags = GAUNTLET_STAGE_TAGS[stage.index]
    return tuple(need for need in PUZZLE_NEEDS if need.key in tags)


def puzzle_needs(puzzle: Puzzle) -> tuple[PuzzleNeed, ...]:
    """The needs of this puzzle, in PUZZLE_NEEDS order."""
    tags = PUZZLE_TAGS[puzzle.prop_name]
    return tuple(need for need in PUZZLE_NEEDS if need.key in tags)


def tutorial_footholds(started: frozenset[str] | set[str]) -> tuple[PuzzleNeed, ...]:
    """Needs that, added to `started`, open a tutorial puzzle; empty if one is already open."""
    def opens(have: set[str]) -> bool:
        return any(all(need.key in have for need in puzzle_needs(puzzle))
                   for puzzle in PUZZLES if puzzle.prop_name in START_ZONE_PUZZLES)

    if opens(set(started)):
        return ()
    return tuple(need for need in PUZZLE_NEEDS if opens(set(started) | {need.key}))


ABSENT_FROM_THE_BUILD: tuple[str, ...] = (
    "gourdBunker",
    "gourdHighPegBoard",
    "gourdFirstPegBoard",
    "gourdMagiciansTrick",
    "gourdButtonBoothChallenge",
    "gourdTileSoup",
    "gourdPanopticon",
    "gourdMaypole",
    "gourdBlindfoldCircus",
    "gourdMessengerRun",
    "gourdHotPotato",
    "gourdScoutTiles",
    "gourdScoutCounting",
)
"""Gourd values with a valet home that the shipped game never produces; kept in case it does."""


# Towers, big keys and monuments.


class Tower(NamedTuple):
    prop_name: str
    """`SaveablePropName` enum name of the big key itself."""
    prop_value: int
    """`SaveablePropName` enum value; item id and location id are BASE_ID + this."""
    item_name: str
    """The feature the key opens (chairlift, train...); the physical key is a second item."""
    key_name: str
    """The tower's name as players say it, prefix of the key item and its checks."""
    location_name: str
    """Player-facing name of the location "this key was placed in its plinth"."""
    plinth_name: str
    """`SaveableHomeName` enum name of the plinth the key is deposited into."""
    monument_prefix: str
    """`SaveableHomeName` prefix of the monument whose slots unlock this key."""
    slots: int
    """Gourds the tower's monument takes before its key is freed."""
    segments: int
    """Cuts on the key, each one a check; the Black and Green Dome keys come already cut."""


# The item is the feature a key opens; the key itself only carries checks.
DRAWBRIDGE_ITEM_NAME = "Drawbridge"
SPAWN_SECRET_DOOR_ITEM_NAME = "Spawn Secret Door"

TOWERS: tuple[Tower, ...] = (
    Tower("bigKeyIntro", 300, DRAWBRIDGE_ITEM_NAME, "Drawbridge", "Drawbridge Key Deposit",
          "bigKeyPlinthIntro", "monoumentIntro", 4, 5),
    Tower("bigKeyRedZone", 301, "Map Room", "Red Funnel Tower", "Red Funnel Tower Key Deposit",
          "bigKeyPlinthMapRoom", "monoument0", 5, 5),
    Tower("bigKeyGreenZone", 302, "Chairlift", "Green Cup Tower", "Green Cup Tower Key Deposit",
          "bigKeyPlinthSkiLift", "monoument1", 5, 5),
    Tower("bigKeyBlueZone", 303, "Train", "Blue Castle Tower", "Blue Castle Tower Key Deposit",
          "bigKeyPlinthTrain", "monoument2", 5, 5),
    Tower("bigKeyYellowZone", 304, "Tunnels", "Yellow Twist Tower", "Yellow Twist Tower Key Deposit",
          "bigKeyPlinthTunnels", "monoument3", 5, 5),
    Tower("bigKeyBoss", 305, "Big Wall Door", "Black Monolith Tower", "Black Monolith Tower Key Deposit",
          "bigKeyPlinthEnding", "monoumentFinal", 6, 0),
    Tower("bigKeyOverflow", 306, SPAWN_SECRET_DOOR_ITEM_NAME, "Green Dome Tower", "Green Dome Tower Key Deposit",
          "bigKeyPlinthGoodbye2", "monoumentOverflow", 15, 0),
)

TOTAL_CUT_SEGMENTS = sum(tower.segments for tower in TOWERS)
"""25: the drawbridge and the four coloured towers, five each."""

# Which of monoument0-3 is which colour is unknown; their slot counts match, so it does not matter.

GREEN_DOME = TOWERS[-1]
"""The Green Dome tower (`bigKeyOverflow`), the postgame/true-ending key."""

GREEN_CUP = TOWERS[2]
"""The Green Tower (`bigKeyGreenZone`); its feature is the chairlift."""

YELLOW_TWIST = TOWERS[4]
"""The Yellow Tower (`bigKeyYellowZone`); its feature is the tunnels."""

BLACK_MONOLITH = TOWERS[5]
"""The Black Tower (`bigKeyBoss`); its feature, the Big Wall Door, leads to the ending."""

MAX_MONUMENT_SLOTS = sum(tower.slots for tower in TOWERS)
"""Every monument slot on the island: 4 + 5 + 5 + 5 + 5 + 6 + 15 = 45."""

# Radio stations: switching one on is the check, its music plays once the item arrives.


class RadioStation(NamedTuple):
    system_name: str
    """`SavableSystem` enum name, the literal `SaveManager` key the game writes."""
    system_value: int
    """`SavableSystem` enum value; both ids are BASE_ID + RADIO_ID_OFFSET + this."""
    location_name: str
    """Player-facing name of the location "this station was switched on"."""
    item_name: str
    """Player-facing name of the item that actually starts the music playing."""


# Named after the music each station plays and its place on the dial, not after the enum names.
RADIO_STATIONS: tuple[RadioStation, ...] = (
    RadioStation("FmStationSleuthFm", 30, "Radio Station 02: Fourth Space", "Radio Music 02: Fourth Space"),
    RadioStation("FmStationKosmische", 31, "Radio Station 07: Bristol", "Radio Music 07: Bristol"),
    RadioStation("FmStationDanceFm", 32, "Radio Station 05: Dance FM", "Radio Music 05: Dance FM"),
    RadioStation("FmStationBreathwork", 33, "Radio Station 01: Bobby", "Radio Music 01: Bobby"),
    RadioStation("FmStationJourneyBeat", 34, "Radio Station 04: Journey Beat", "Radio Music 04: Journey Beat"),
    RadioStation("FmStationAFJ", 35, "Radio Station 06: Mallets", "Radio Music 06: Mallets"),
    RadioStation("FmStationFourthSpace", 36, "Radio Station 03: Breathwork", "Radio Music 03: Breathwork"),
)

# What sits behind a big key, by game id rather than display name so a rename cannot empty a list.
START_ZONE_PUZZLES: tuple[str, ...] = (
    "gourdHighButton",
    "gourdEasySimPress",
    "gourdTellerWindow",
    "gourdTelescopeToBox",
)
"""The puzzles of the starting zone, short of the drawbridge and the first arch door."""

CHAIRLIFT_PUZZLES: tuple[str, ...] = (
    "gourdCannonballCommute",
    "gourdCharadesRooms",
    "gourdDancerAndSelecter",
    "gourdPoetAndPontiff",
    "gourdCenturionSeance",
    "gourdCabinFeverLong",
    "gourdSpeedObby",
)
"""The puzzles past the chairlift: the seven purple "variant challenge" gourds."""

CHAIRLIFT_RADIO: str | None = "FmStationSleuthFm"
"""The station past the chairlift (dial position 2)."""

TUNNEL_RADIO: str | None = "FmStationDanceFm"
"""The station past the tunnels."""


def chairlift_locations() -> tuple[str, ...]:
    """Player-facing names of every location behind the Chairlift."""
    return _locations_for(CHAIRLIFT_PUZZLES, CHAIRLIFT_RADIO)


def tunnel_locations() -> tuple[str, ...]:
    """Player-facing names of every location behind the Tunnels."""
    return _locations_for((), TUNNEL_RADIO)


def _locations_for(puzzle_prop_names: tuple[str, ...], radio_system: str | None) -> tuple[str, ...]:
    names = [puzzle.location_name for puzzle in PUZZLES if puzzle.prop_name in puzzle_prop_names]
    names += [station.location_name for station in RADIO_STATIONS if station.system_name == radio_system]
    return tuple(names)


# EndingGate is the door's live state and can go back to 0: the mod latches its first write.
ENDING_GATE_SYSTEM = "EndingGate"
GAUNTLET_COMPLETE_SYSTEM = "GauntletComplete"


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

TELEPORT_ID_OFFSET = 2_700
"""Item-only: the teleporters of `teleport_buttons: items`, by position."""

TELEPORT_DESTINATIONS: tuple[tuple[str, str], ...] = (
    ("red", "Red Funnel Tower"),
    ("green", "Green Cup Tower"),
    ("blue", "Blue Castle Tower"),
    ("yellow", "Yellow Twist Tower"),
    ("black", "Black Tower"),
    ("gauntlet", "Silent Gauntlet"),
    ("drawbridge", "Drawbridge"),
    ("dome", "Green Dome Tower"),
)
"""The hub's teleport buttons (mod key, name); add new ones at the end, the position is the id."""


def teleport_item_name(destination: str) -> str:
    return f"{destination} Teleporter"


def teleport_item_id(position: int) -> int:
    return BASE_ID + TELEPORT_ID_OFFSET + position


# 9_201..9_213 are retired joke filler ids; never reuse them.


class Pickup(NamedTuple):
    """A backpack, belt or gourd carton whose first pick-up is a check (`pack_sanity`)."""

    guid: str
    """The prop's `savablePropGuid`, the same on every machine."""
    location_name: str
    past_chairlift: bool = False
    past_tunnels: bool = False


PICKUP_ID_OFFSET = 13_000
"""A pick-up's location id: `BASE_ID + 13_000 + position` in PICKUPS."""

PICKUPS: tuple[Pickup, ...] = (
    Pickup("2d8101c4-223f-4939-8df6-809562469516", "Sky Blue Backpack"),
    Pickup("76601ed6-da1c-4326-9c8d-2c6168d31f36", "Yellow Backpack"),
    Pickup("730affd5-3139-473c-aaf2-4ecd5142c82f", "Teal Backpack"),
    Pickup("22009189-d33d-4e65-8b3c-c8257188743d", "Orange Backpack"),
    Pickup("f2dea8ab-08ea-434d-b98b-7e66a499392e", "Pink Backpack"),
    Pickup("02e75b25-ffa8-49e9-9fa3-7c453206aad9", "Maroon Backpack", past_chairlift=True),
    Pickup("9057af3c-12c5-48d8-8251-07233a203861", "Green Belt"),
    Pickup("d73ec09b-8e2b-432e-8e62-b5ff71db2d23", "Purple Belt", past_tunnels=True),
    Pickup("3bfe0dec-42de-4c31-87ab-a72f0b94722a", "Yellow Belt"),
    Pickup("ef09f445-cd71-4414-938a-7a7122dcd121", "Orange Belt"),
    Pickup("3e944e74-2d2e-40e2-b1ba-a1afa8c937ee", "Blue Belt", past_chairlift=True),
    Pickup("5fa8468a-dfaa-48fe-8057-46d47b9e56a5", "Gourd Carton Pickup"),
)
"""Every backpack, belt (`HolsterProp`) and gourd carton, named by their `_TintColor`."""


FLARE_GUN_ID_OFFSET = 13_050
"""A flare gun pick-up's location id: `BASE_ID + 13_050 + position` in FLARE_GUN_PICKUPS."""

FLARE_GUN_PICKUPS: tuple[Pickup, ...] = (
    Pickup("80381b52-ede7-494f-b800-0c77a71f57e0", "Red Flare Gun Pickup"),
    Pickup("72bc4c22-4418-4b7c-9c1d-e8cfa5040102", "Green Flare Gun Pickup"),
    Pickup("66de92ed-177d-49d5-aa97-e86d98083ac0", "Yellow Flare Gun Pickup"),
    Pickup("8b9fc973-4200-4934-8169-dde2de5b1d10", "Blue Flare Gun Pickup"),
)
"""The four flare guns; "Pickup" in the name so the location does not read like the item."""


class Firework(NamedTuple):
    """A firework launcher whose first firing is a check (`firework_sanity`)."""

    key: str
    """The landmark among the launcher's parents that the mod finds it by."""
    location_name: str
    past_chairlift: bool = False
    tower: str | None = None
    """The `prop_name` of the tower whose roof it stands on."""


FIREWORK_ID_OFFSET = 13_100
"""A firework's location id: `BASE_ID + 13_100 + position` in FIREWORKS."""

FIREWORKS: tuple[Firework, ...] = (
    Firework("Hub", "Hub Fireworks"),
    Firework("Lookout_RedCone", "Red Funnel Tower Fireworks", tower="bigKeyRedZone"),
    Firework("Lookout_YellowZigzag", "Yellow Twist Tower Fireworks", tower="bigKeyYellowZone"),
    Firework("Lookout_BlueTube", "Blue Castle Tower Fireworks", tower="bigKeyBlueZone"),
    Firework("Lookout_GreenHourglass", "Green Cup Tower Fireworks", tower="bigKeyGreenZone"),
    Firework("PeakMarker", "Peak Fireworks"),
    Firework("SouthPeak", "South Peak Fireworks"),
    Firework("FireworksPlatform_Intro", "Intro Platform Fireworks"),
)
"""The eight launchers; the game saves nothing, so the mod watches each one's button."""


TOWER_REACHED_ID_OFFSET = 13_200
"""A "reached" check per tower, by its position in reached_towers()."""


def reachable_towers() -> tuple[Tower, ...]:
    """The four coloured towers and the Black Tower: the ones with a button to reach."""
    return tuple(tower for tower in TOWERS if tower.prop_name not in ("bigKeyIntro", "bigKeyOverflow"))


def reached_towers() -> tuple[Tower, ...]:
    """Every tower with a "reached" check, in id order; the last two are reached by position."""
    return (*reachable_towers(), tower_by_prop("bigKeyIntro"), tower_by_prop("bigKeyOverflow"))


def tower_reached_location_name(tower: Tower) -> str:
    return f"Reached the {tower.key_name}"


MONUMENT_FULL_ID_OFFSET = 13_800
"""A "monument full" check per tower, by its position in TOWERS."""


def monument_full_location_name(tower: Tower) -> str:
    return f"{tower.key_name} Monument Full"


def monument_full_location_id(tower: Tower) -> int:
    return BASE_ID + MONUMENT_FULL_ID_OFFSET + TOWERS.index(tower)


MONUMENT_KEY_ID_OFFSET = 13_850
"""With `linear_towers: one_deposit_box`, the slot that frees a tower's key, by TOWERS order."""


def monument_key_location_name(tower: Tower) -> str:
    return f"{tower.key_name} Monument Key"


def monument_key_location_id(tower: Tower) -> int:
    return BASE_ID + MONUMENT_KEY_ID_OFFSET + TOWERS.index(tower)


def tower_reached_location_id(tower: Tower) -> int:
    return BASE_ID + TOWER_REACHED_ID_OFFSET + reached_towers().index(tower)


PROGRESSIVE_MONUMENT_ITEM_NAME = "Progressive Monument"
PROGRESSIVE_MONUMENT_ID_OFFSET = 13_300


def progressive_monument_id() -> int:
    """One id shared by every copy."""
    return BASE_ID + PROGRESSIVE_MONUMENT_ID_OFFSET


class Ability(NamedTuple):
    """A part of the player's body as an item, with `lock_abilities`."""

    key: str
    """What slot_data calls it; the mod's AbilityLocks grants by it."""
    item_name: str
    copies: int
    """Copies in the pool: a progressive arm raises, then points, then extends."""
    handed: bool
    """With `lock_abilities: handed` (one arm per item)."""
    ambidextrous: bool
    """With `lock_abilities: ambidextrous` (both arms per item)."""


ABILITY_ID_OFFSET = 13_400
"""Item-only, by position in ABILITIES, so keep the order."""

ABILITIES: tuple[Ability, ...] = (
    # Legs: jumping and crouching. Seat: sitting on a chair or the floor.
    Ability("legs", "Legs", 1, True, True),
    Ability("seat", "Seat", 1, True, True),
    Ability("left_arm", "Progressive Left Arm", 3, True, False),
    Ability("right_arm", "Progressive Right Arm", 3, True, False),
    Ability("arms", "Progressive Arms", 3, False, True),
    # Running is only in the pool with `lock_running`.
    Ability("throwing", "Throwing", 1, True, True),
    Ability("carrying", "Carrying", 1, True, True),
    Ability("running", "Running", 1, True, True),
)

ABILITY_LOCK_KEYS: dict[str, str] = {
    "legs": "legs", "seat": "seat", "left_arm": "arms", "right_arm": "arms", "arms": "arms",
    "throwing": "throwing", "carrying": "carrying",
}
"""The `start_with_abilities` key of each ability item; Running has `lock_running` instead."""

LEGS_ITEM_NAME = "Legs"
"""Needed to leave the starting zone with `lock_abilities`: jumping is needed almost everywhere."""
SEAT_ITEM_NAME = "Seat"
LEFT_ARM_ITEM_NAME = "Progressive Left Arm"
RIGHT_ARM_ITEM_NAME = "Progressive Right Arm"
ARMS_ITEM_NAME = "Progressive Arms"
THROWING_ITEM_NAME = "Throwing"
CARRYING_ITEM_NAME = "Carrying"
RUNNING_ITEM_NAME = "Running"


class Body(NamedTuple):
    """What a place asks of the players' bodies with `lock_abilities`."""

    seat: bool = False
    arms: int = 0
    """Copies of each arm (both arms): ARM_RAISING, ARM_POINTING or ARM_EXTENDING; 0 for none."""
    throwing: bool = False
    carrying: bool = False

ARM_RAISING, ARM_POINTING, ARM_EXTENDING = 1, 2, 3
"""How many copies of a progressive arm each gesture needs."""

POSES = Body(seat=True, arms=ARM_EXTENDING)
"""Sitting and the arm poses, both arms; abilities go to every player, so one item covers both."""

ABILITY_PUZZLES: dict[str, Body] = {
    # Jumping and crouching are left out: Legs is already needed to leave the starting zone.
    "gourdHighButton": Body(carrying=True),
    "gourdFielding": Body(throwing=True),
    "gourdInvisibleInk": Body(arms=ARM_RAISING),
    "gourdObby": Body(carrying=True),
    "gourdCarousel": Body(seat=True),
    "gourdWindowLabyrinth": Body(throwing=True),
    "gourdBasketball": Body(throwing=True),
    "gourdConcert": POSES,
    "gourdIndoorSemaphore": POSES,
    "gourdOpticalTelegraph": POSES,
    # Pointer's Paradise needs carrying only with 3 or 4 players; logic cannot see the player count.
    "gourdCharadesRooms": POSES,
    "gourdPointersParadise": Body(seat=True, arms=ARM_EXTENDING, carrying=True),
    "gourdKickUpPits": Body(throwing=True),
    "gourdDancerAndSelecter": POSES,
    "gourdSpeedObby": Body(carrying=True),
    "gourdCannonballCommute": Body(seat=True),
}

GAUNTLET_STAGE_ABILITIES: tuple[Body, ...] = (
    Body(arms=ARM_EXTENDING), Body(), Body(throwing=True), Body(), Body(throwing=True), Body(arms=ARM_EXTENDING), POSES,
)
"""The same, for the Silent Gauntlet's seven stages, in order."""


OBJECT_ABILITIES: dict[str, Body] = {
    # Megaphones hang out of one player's reach: one climbs on another's shoulders.
    "MegaphoneProp": Body(carrying=True),
}
"""What picking up an island object asks of the body with `lock_abilities`, by its kind."""

OBJECT_GUID_ABILITIES: dict[str, Body] = {
    # Only reached by riding the mini carousel's swings.
    "88942e8e-6fae-4d71-93f0-8500741a82d1": Body(seat=True),  # Binoculars (Carousel)
}
"""The same for one object, on top of its kind's, keyed as IslandObject.guid."""


def ability_item_id(ability: Ability) -> int:
    return BASE_ID + ABILITY_ID_OFFSET + ABILITIES.index(ability)


class IslandObject(NamedTuple):
    """A gadget of the island whose pick-up is a check with `object_sanity`."""

    guid: str
    """The prop's `savablePropGuid`, the same on every machine."""
    kind: str
    """The prop's name in the game, which the mod matches a pick-up by (`first_pickup`)."""
    location_name: str
    zone: str
    """start, overworld, chairlift, purple, tunnels, big_wall, map_room or tower:<big key prop>."""


OBJECT_ID_OFFSET = 13_500
"""`object_sanity: every`: by position in ISLAND_OBJECTS."""
OBJECT_KIND_ID_OFFSET = 13_700
"""`object_sanity: first_pickup`: by position in OBJECT_KINDS."""

ISLAND_OBJECTS: tuple[IslandObject, ...] = (
    # Packs, flare guns and the puzzles' own tools have their own checks or none.
    IslandObject("6364d493-5c48-4480-a02d-ae086ef06825", "BinocularsProp", "Binoculars (Blue Castle Tower)", "tower:bigKeyBlueZone"),
    IslandObject("88942e8e-6fae-4d71-93f0-8500741a82d1", "BinocularsProp", "Binoculars (Carousel)", "overworld"),
    IslandObject("16e1e4a2-aadf-43e1-a67b-f2513dd63303", "BinocularsProp", "Binoculars (Green Cup Tower)", "tower:bigKeyGreenZone"),
    IslandObject("ed2f8967-b4c1-43d9-91b5-11679be10bc6", "BinocularsProp", "Binoculars (Peak Marker)", "overworld"),
    IslandObject("f40709b7-b15b-4131-ab88-ae08571224ac", "BinocularsProp", "Binoculars (Red Funnel Tower)", "tower:bigKeyRedZone"),
    IslandObject("eed83af3-1958-4718-a634-5280bdc6b3cb", "BinocularsProp", "Binoculars (South Peak)", "chairlift"),
    IslandObject("6debc10d-8e02-40d5-a5be-bfc992b89a9a", "BinocularsProp", "Binoculars (Yellow Twist Tower)", "tower:bigKeyYellowZone"),
    IslandObject("1c9f8bc9-5850-4085-80f6-05c2821b505d", "ClockProp", "Clock (Green Diffuser Train Stop)", "overworld"),
    IslandObject("44015a0e-20ca-441a-b6e9-bbd28657b5f3", "ClockProp", "Clock (Ring Room Train Stop)", "overworld"),
    IslandObject("e56da5eb-bac5-4c5e-b489-a8de3d204e7c", "ClockProp", "Clock (South Valley Beach Train Stop)", "overworld"),
    IslandObject("209a7dc3-9dcb-425e-86c5-3a84aa9bd0ec", "ClockProp", "Clock (Sports Valley Train Stop)", "overworld"),
    IslandObject("f468d4dc-594e-4c62-80d4-3a85d9e25b3e", "ClockProp", "Clock (Trap Room Train Stop)", "overworld"),
    IslandObject("a08daa18-2fef-4a49-a062-2baafdf48686", "ClockProp", "Clock (Upper Hub Train Stop)", "overworld"),
    IslandObject("d793e0fe-a145-496f-a348-2d8a65add9e8", "CompassProp", "Compass (Map Room)", "map_room"),
    IslandObject("672cd949-1e5b-465a-a1cb-3860594f9e9f", "FoldingMapProp", "Folding Map (Map Room)", "map_room"),
    IslandObject("cf0fd97f-0f39-4b0c-9474-54c665aeb2e9", "CowBellLargeProp", "Large Cowbell (Tutorial)", "start"),
    IslandObject("f68a6dbc-0b73-47b6-b9e6-fd28e016606e", "LaserProp", "Laser (Blue Castle Tower)", "tower:bigKeyBlueZone"),
    IslandObject("b2913ee0-2c3f-4582-a2f5-6c9f33b40b3b", "LaserProp", "Laser (Green Cup Tower)", "tower:bigKeyGreenZone"),
    IslandObject("c7ba38c6-f0fe-45fd-ad4a-6d1907b408ae", "LaserProp", "Laser (Map Room 1)", "map_room"),
    IslandObject("cb8326bc-26c2-4214-8dc4-3ebafe0b5603", "LaserProp", "Laser (Map Room 2)", "map_room"),
    IslandObject("7c788690-d373-4b31-9be9-31bbb3ec1f88", "LaserProp", "Laser (Map Room 3)", "map_room"),
    IslandObject("8fbe1de2-1ea0-4da4-8ef5-282f5d19b492", "LaserProp", "Laser (Red Funnel Tower)", "tower:bigKeyRedZone"),
    IslandObject("e69463b0-e27c-4247-93e4-50df64cd12ad", "LaserProp", "Laser (Yellow Twist Tower)", "tower:bigKeyYellowZone"),
    IslandObject("a418a7cd-0945-44c1-9f7b-14eac4584cf1", "CowBellMediumProp", "Medium Cowbell (Tutorial)", "start"),
    IslandObject("7e2f3ba8-2124-4fe9-8707-1621be5a0926", "MegaphoneProp", "Megaphone (Pyramid Holder)", "overworld"),
    IslandObject("c6259d07-2499-4063-b033-b72cddd951c2", "MegaphoneProp", "Megaphone (Ramp Holder)", "overworld"),
    IslandObject("48da7ac6-988f-4f4b-a376-3686fe550f69", "MegaphoneProp", "Megaphone (Spiral Holder)", "chairlift"),
    IslandObject("2835fac9-81b1-4312-a576-cc977e6e8c85", "FmRadioProp", "Radio (Abbey Music Garden)", "chairlift"),
    IslandObject("21ab205e-db90-452b-b620-8f64ebcabd63", "FmRadioProp", "Radio (Doof Chamber)", "tunnels"),
    IslandObject("ec17f547-5b1f-4fcf-828e-f2932dc2f620", "FmRadioProp", "Radio (Factory Music Garden)", "overworld"),
    IslandObject("d8e70c55-9800-498e-af4e-7054093950e4", "FmRadioProp", "Radio (Floorplan Music Garden)", "overworld"),
    IslandObject("87dbbb84-5c58-43a4-96ba-e43c1c7308b9", "FmRadioProp", "Radio (Greensteps Music Garden)", "overworld"),
    IslandObject("41e7b481-2e10-4d62-9136-5e6e0ba9fa7d", "FmRadioProp", "Radio (Lighthouse Music Garden)", "overworld"),
    IslandObject("070cf8f2-6824-413e-9c2a-43a1027c24c5", "FmRadioProp", "Radio (Musical Holiday)", "overworld"),
    IslandObject("ea8c71ac-810f-4426-84a1-17e0e3b0b81d", "FmRadioProp", "Radio (Peak Marker)", "overworld"),
    IslandObject("ebdec567-58c0-48c7-a62a-bbd843bef36f", "FmRadioProp", "Radio (Upper Hub)", "overworld"),
    IslandObject("b4dbfae3-89e9-45c0-9dd0-3fd815cd3904", "XrayGogglesProp", "Scanner (Blue Castle Tower)", "tower:bigKeyBlueZone"),
    IslandObject("044daaf7-fb90-4f73-a9bb-00b8a3df9c60", "XrayGogglesProp", "Scanner (Fireworks Platform)", "overworld"),
    IslandObject("ed7b657e-5949-4a1e-8b82-e50ad2a8d785", "XrayGogglesProp", "Scanner (Green Cup Tower)", "tower:bigKeyGreenZone"),
    IslandObject("0132c70b-9a0c-4b5a-be06-b9bb077a6e5a", "XrayGogglesProp", "Scanner (Red Funnel Tower)", "tower:bigKeyRedZone"),
    IslandObject("c420401c-ba23-4e0c-aeac-2ea3ebcafa26", "XrayGogglesProp", "Scanner (South Peak)", "chairlift"),
    IslandObject("28cf2386-5e54-4b7e-99ba-283298aa3816", "XrayGogglesProp", "Scanner (Upper Hub)", "overworld"),
    IslandObject("9fadd6c5-af79-437f-80dd-ebb74d8526d4", "XrayGogglesProp", "Scanner (Yellow Twist Tower)", "tower:bigKeyYellowZone"),
    IslandObject("307d526d-780f-4fe2-b3fd-95f74b86319c", "CowBellSmallProp", "Small Cowbell (Tutorial)", "start"),
    IslandObject("381abbdb-da55-4243-a5df-692acd3fb2c0", "TorchProp", "Torch (Blue Castle Tower)", "tower:bigKeyBlueZone"),
    IslandObject("cf899912-898f-4486-86ae-91ed78025a9d", "TorchProp", "Torch (Blue Tunnel Outlet)", "tunnels"),
    IslandObject("b25443e0-ec2d-4b55-b8e9-01ac48604ba5", "TorchProp", "Torch (Green Cup Tower)", "tower:bigKeyGreenZone"),
    IslandObject("57632bfc-688b-40e4-aabd-d020f489b291", "TorchProp", "Torch (Green Tunnel Outlet)", "tunnels"),
    IslandObject("d6270f5f-1ab7-4526-9e64-cccebde9389d", "TorchProp", "Torch (Red Funnel Tower)", "tower:bigKeyRedZone"),
    IslandObject("cbbf5ba7-0026-4737-9547-2e69187a2845", "TorchProp", "Torch (Red Tunnel Outlet)", "tunnels"),
    IslandObject("ac52bd4b-990f-4908-999d-5badeb8b6eda", "TorchProp", "Torch (Tutorial Exit)", "overworld"),
    IslandObject("56a5d535-d1a6-4ada-a1b3-cf6951bb2e66", "TorchProp", "Torch (Yellow Tunnel Outlet)", "tunnels"),
    IslandObject("b459af97-5a07-4024-8950-02f752c5292b", "TorchProp", "Torch (Yellow Twist Tower)", "tower:bigKeyYellowZone"),
    IslandObject("cee6114c-2dc0-4d02-a393-5686a2f2e9b8", "WalkieTalkieProp", "Walkie-Talkie (Back Beach Hill, Yellow)", "overworld"),
    IslandObject("56a7313e-9e55-485b-ac52-fcc6f8030133", "WalkieTalkieProp", "Walkie-Talkie (Beach, Blue)", "overworld"),
    IslandObject("cd159c6e-88aa-4158-a11f-2790cf581052", "WalkieTalkieProp", "Walkie-Talkie (Ocean Treasure, Yellow)", "overworld"),
    IslandObject("00d7ed74-a4d9-4969-b8e2-bf8d4975ac8a", "WalkieTalkieProp", "Walkie-Talkie (Ocean Treasure, Blue)", "overworld"),
    IslandObject("6efb6db4-76c1-4ffa-b249-8badc6d4bf82", "WalkieTalkieProp", "Walkie-Talkie (Peak, Blue)", "overworld"),
    IslandObject("a5992995-191a-45b7-aa23-e9a64a4fede7", "WalkieTalkieProp", "Walkie-Talkie (Peak, Yellow)", "overworld"),
    IslandObject("185e3864-0dcb-498b-baee-6da1a4b21a8e", "WalkieTalkieProp", "Walkie-Talkie (Red Cone Hill, Yellow)", "overworld"),
    IslandObject("f29614f5-0c70-4ed8-b152-9e57b371f20d", "WalkieTalkieProp", "Walkie-Talkie (Red Cone Valley, Blue)", "overworld"),
    # Paintbrushes are left out by default and never taken off the island: the Salon needs them.
    IslandObject("52fedcd9-caa6-4ec6-a285-f73e6ff40f50", "SalonBrushProp", "Paintbrush (Salon)", "overworld"),
    IslandObject("64d92f9a-e44c-4218-ab98-4ea48d0c9b33", "SalonBrushProp", "Paintbrush (Secret Salon)", "tunnels"),
    # Lamps outside the puzzles, each placed in the zone of the puzzle it stands nearest to.
    IslandObject("1cdcd9a1-0343-4234-b1d9-7be646dec4ae", "BuoyProp", "Lamp (Blue Castle Tower)", "tower:bigKeyBlueZone"),
    IslandObject("24d2f9a5-5376-4a7c-b08c-17e9c060855b", "BuoyProp", "Lamp (Diffuser Peak)", "overworld"),
    IslandObject("fcda7d44-81af-4526-be44-f16a90e5b0ec", "BuoyProp", "Lamp (Green Cup Tower)", "tower:bigKeyGreenZone"),
    IslandObject("98c2cd14-bbcf-4066-b58c-d7d23d2757f1", "BuoyProp", "Lamp (South Peak 1)", "purple"),
    IslandObject("d252953b-ebd1-484a-a555-953bccb904c0", "BuoyProp", "Lamp (South Peak 2)", "chairlift"),
    IslandObject("2141d27e-647c-401f-9a0c-a3c1aa029434", "BuoyProp", "Lamp (Tutorial 1)", "start"),
    IslandObject("0cb88fcc-6d33-4b08-9e5e-124a34f7f15b", "BuoyProp", "Lamp (Tutorial 2)", "start"),
    IslandObject("0460483f-1636-4ee6-8635-2168a9acaf34", "BuoyProp", "Lamp (Tutorial 3)", "start"),
    IslandObject("4df70c57-8d1b-447e-bad5-5aa66c1169f4", "BuoyProp", "Lamp (Hub)", "overworld"),
    IslandObject("1ff0231c-4933-4bec-b878-4e6f1fe88856", "BuoyProp", "Lamp (Yellow Twist Tower)", "tower:bigKeyYellowZone"),
    IslandObject("c5e792c4-080b-4377-8d68-f09c00debc91", "BuoyProp", "Lamp (near Cabin Fever Long)", "overworld"),
    IslandObject("b5461e87-1a09-4f2c-b9b5-158f195adbf3", "BuoyProp", "Lamp (near Cannonball Commute)", "chairlift"),
    IslandObject("5d10fec3-7a2d-4bfd-be0e-6196152d88f5", "BuoyProp", "Lamp (near Cannonball 1)", "overworld"),
    IslandObject("12a7f64b-8175-4857-a371-77dc2989cd68", "BuoyProp", "Lamp (near Cannonball 2)", "overworld"),
    # Behind the Big Wall, on the far side from the Centurion Song.
    IslandObject("b95078fd-d8f2-4fd9-b2aa-ecf8b84fdad7", "BuoyProp", "Lamp (Big Wall)", "big_wall"),
    IslandObject("59a73457-6368-4639-8ec7-335902797343", "BuoyProp", "Lamp (near Coordinates)", "overworld"),
    IslandObject("f7f2e9ed-14fd-4a54-a918-fc06b444a39e", "BuoyProp", "Lamp (near Easy Simultaneous Press)", "overworld"),
    IslandObject("80e38f3e-d054-4187-9675-66d6e70c8356", "BuoyProp", "Lamp (near Egg Hunt)", "overworld"),
    IslandObject("cec1412b-4727-48c4-b6a1-5a5e7989af18", "BuoyProp", "Lamp (near High Button 1)", "overworld"),
    IslandObject("94876c24-50ba-41cb-8fdd-9bd026f3be32", "BuoyProp", "Lamp (near High Button 2)", "overworld"),
    IslandObject("087f1e73-309b-4db2-8e39-dee6c926ad59", "BuoyProp", "Lamp (near Invisible Ink)", "overworld"),
    IslandObject("df814b39-e550-4e17-ad44-e32028ab3c47", "BuoyProp", "Lamp (near Kick Up Pits)", "overworld"),
    IslandObject("f4819494-6b1d-48ef-823b-4b26d2ceda53", "BuoyProp", "Lamp (near Memory Bombs)", "overworld"),
    IslandObject("13956d25-97a1-4b5c-9992-00c187092476", "BuoyProp", "Lamp (near Musical Holiday)", "overworld"),
    IslandObject("982f4dbf-22bb-4553-aa9c-dd6588c19fda", "BuoyProp", "Lamp (near Optical Telegraph)", "overworld"),
    IslandObject("3bfaaa40-7101-4f52-ad72-a283a8e39cac", "BuoyProp", "Lamp (near Poet and Pontiff)", "chairlift"),
    IslandObject("17e22f02-4f01-4aa4-83fb-d2871acd97cd", "BuoyProp", "Lamp (near Pointer's Paradise)", "overworld"),
    IslandObject("6a673617-992d-49ca-87f9-223aef4c9fe4", "BuoyProp", "Lamp (near Scout Bombs)", "overworld"),
    IslandObject("6beb48a3-60de-41e8-a9f6-d39772bcdfa4", "BuoyProp", "Lamp (near Singer and Selector)", "overworld"),
    IslandObject("d3e57e13-a91d-492c-bd46-2c59da8bef9e", "BuoyProp", "Lamp (near Telescope to Box)", "overworld"),
    IslandObject("5ee8fa69-8937-4841-b9a7-ee8a10c3a933", "BuoyProp", "Lamp (near Teller Window 1)", "overworld"),
    IslandObject("4b9cad35-ab44-4dfe-9e4b-390cb165aca5", "BuoyProp", "Lamp (near Teller Window 2)", "overworld"),
    IslandObject("632fc258-f424-4b88-930a-a62c45044965", "BuoyProp", "Lamp (near Trap Room)", "overworld"),
    IslandObject("d9f93a0a-19e3-4702-b5be-7b79e39d32c8", "BuoyRedProp", "Red Lamp (Tutorial 1)", "start"),
    IslandObject("1a014620-29dd-4517-8990-3854045129bd", "BuoyRedProp", "Red Lamp (Tutorial 2)", "start"),
    IslandObject("abc5a86c-4618-4bf6-bdba-e97e488ef81e", "BuoyRedProp", "Red Lamp (Tutorial 3)", "start"),
    # The pedestal lamps: a second lamp model, under the same rules.
    IslandObject("0537b69a-6e49-406a-b93c-d6bc7f6cc4b3", "BuoyLight", "Pedestal Lamp (Blue Castle Tower)", "tower:bigKeyBlueZone"),
    IslandObject("dab5cfc8-f4d9-44f2-abb0-90bc42b3cade", "BuoyLight", "Pedestal Lamp (Diffuser Peak)", "overworld"),
    IslandObject("eaa97883-12ad-4e8e-9f6d-cdf7f1a3a061", "BuoyLight", "Pedestal Lamp (Green Cup Tower 1)", "tower:bigKeyGreenZone"),
    IslandObject("68287599-1f13-42f0-9424-cabd187e8a55", "BuoyLight", "Pedestal Lamp (Green Cup Tower 2)", "tower:bigKeyGreenZone"),
    IslandObject("52b0dfcb-4588-4ce7-a78c-bdb9cb800fe6", "BuoyLight", "Pedestal Lamp (Peak Marker 1)", "overworld"),
    IslandObject("f84cb1c7-1702-4fa1-9ddb-f4c334041237", "BuoyLight", "Pedestal Lamp (Peak Marker 2)", "overworld"),
    IslandObject("cd88c773-6375-42c7-9475-76b288bd1c91", "BuoyLight", "Pedestal Lamp (Peak Marker 3)", "overworld"),
    IslandObject("1b047167-6e68-49c3-abf2-e15fbbeca0a5", "BuoyLight", "Pedestal Lamp (Red Funnel Tower 1)", "tower:bigKeyRedZone"),
    IslandObject("d27fe6ce-2b92-42bd-8d60-8885e368877c", "BuoyLight", "Pedestal Lamp (Red Funnel Tower 2)", "tower:bigKeyRedZone"),
    IslandObject("b28ac861-6c5d-4702-8b11-ef38df1eac22", "BuoyLight", "Pedestal Lamp (South Peak 1)", "purple"),
    IslandObject("4cd9c5fc-7414-4bfc-b4a2-6fb4fa0312c5", "BuoyLight", "Pedestal Lamp (South Peak 2)", "chairlift"),
    IslandObject("3d09137d-fbbd-4cb8-a17a-05de7676f61c", "BuoyLight", "Pedestal Lamp (Yellow Twist Tower 1)", "tower:bigKeyYellowZone"),
    IslandObject("d0f4305f-b577-41f7-a879-18be53553509", "BuoyLight", "Pedestal Lamp (Yellow Twist Tower 2)", "tower:bigKeyYellowZone"),
    IslandObject("60412245-4b6c-47eb-a4e7-67eec146bf20", "BuoyLight", "Pedestal Lamp (near Basketball)", "overworld"),
    IslandObject("b2f9b7af-39f5-431c-9908-c2ebf6d6453b", "BuoyLight", "Pedestal Lamp (near Breadcrumb Loop 1)", "overworld"),
    IslandObject("16990dcc-ebb8-453e-88fc-6478e3980728", "BuoyLight", "Pedestal Lamp (near Breadcrumb Loop 2)", "overworld"),
    IslandObject("db6fc3f6-366c-4cf9-81f1-9c45cbdceef4", "BuoyLight", "Pedestal Lamp (near Cabin Fever Long)", "purple"),
    IslandObject("cf2e3191-4c77-4ce9-8c56-f43a73d6e531", "BuoyLight", "Pedestal Lamp (Big Wall)", "big_wall"),
    IslandObject("080554bb-bc16-41cd-bc96-4765cec96beb", "BuoyLight", "Pedestal Lamp (near Egg Hunt)", "overworld"),
    IslandObject("d0a1c556-d62f-42df-a36e-019636da7679", "BuoyLight", "Pedestal Lamp (near Fielding)", "overworld"),
    IslandObject("5efbf72b-9dff-4251-a536-f625ba8ff315", "BuoyLight", "Pedestal Lamp (near High Button 1)", "overworld"),
    IslandObject("fee806b8-8617-49f8-99af-bdea0c148ed0", "BuoyLight", "Pedestal Lamp (near High Button 2)", "overworld"),
    IslandObject("4ec6aca0-9961-4550-9c26-7cacb9917566", "BuoyLight", "Pedestal Lamp (near High Button 3)", "overworld"),
    IslandObject("b46c590f-7038-4033-b84d-e3fb664e095c", "BuoyLight", "Pedestal Lamp (near Musical Holiday 1)", "overworld"),
    IslandObject("d069a848-37f5-40e0-ac18-64516951f481", "BuoyLight", "Pedestal Lamp (near Musical Holiday 2)", "overworld"),
    IslandObject("c7141d1b-96bf-4c2c-8c05-3d2067e64a93", "BuoyLight", "Pedestal Lamp (near Musical Holiday 3)", "overworld"),
    IslandObject("11c1d018-3dbf-46d4-b1de-f9aba4d29fc2", "BuoyLight", "Pedestal Lamp (near Observation Room 1)", "overworld"),
    IslandObject("bef6d32e-57a9-4ba1-b984-05fdec08800c", "BuoyLight", "Pedestal Lamp (near Observation Room 2)", "overworld"),
    IslandObject("accb5df0-1452-4b84-8607-9f01bd517103", "BuoyLight", "Pedestal Lamp (near Observation Room 3)", "overworld"),
    IslandObject("0e8faaf2-b5fd-4906-aaab-788f8ccbf603", "BuoyLight", "Pedestal Lamp (near Perspective Counting)", "overworld"),
    IslandObject("a552b903-066f-45a8-8550-4cbf5652de74", "BuoyLight", "Pedestal Lamp (near Poet and Priest)", "overworld"),
    IslandObject("38722a78-da82-4d6e-80c4-efd70e1be90f", "BuoyLight", "Pedestal Lamp (near Pointer's Paradise 1)", "overworld"),
    IslandObject("d368b4c7-8389-4f63-a20f-ba88ef17f0a5", "BuoyLight", "Pedestal Lamp (near Pointer's Paradise 2)", "overworld"),
    IslandObject("7017169d-96f6-4578-8c1f-eb65f3549e32", "BuoyLight", "Pedestal Lamp (near Ring Room)", "overworld"),
    IslandObject("4ee18fc7-2a43-44e0-9a2b-4508e9d2705d", "BuoyLight", "Pedestal Lamp (near Singer and Selector 1)", "overworld"),
    IslandObject("c716c86d-81a7-40cc-873f-0405821dbd72", "BuoyLight", "Pedestal Lamp (near Singer and Selector 2)", "overworld"),
    IslandObject("cef7ae31-a105-491d-8d0d-daab0253909c", "BuoyLight", "Pedestal Lamp (near Singer and Selector 3)", "overworld"),
    IslandObject("007a1599-17ad-426f-a68b-37718423e9f7", "BuoyLight", "Pedestal Lamp (Hub 1)", "overworld"),
    IslandObject("99d8a99a-89eb-4ad0-8736-104aafe1fdd7", "BuoyLight", "Pedestal Lamp (Hub 2)", "overworld"),
    IslandObject("fa653354-bdfa-431d-aa19-7453e5730e80", "BuoyLight", "Pedestal Lamp (Hub 3)", "overworld"),
    IslandObject("942347f0-cd23-445f-8019-d296e5c92ead", "BuoyLight", "Pedestal Lamp (Hub 4)", "overworld"),
    IslandObject("0abc8402-398d-4f0d-bf5e-fb3051a16e21", "BuoyLight", "Pedestal Lamp (Hub 5)", "overworld"),
    IslandObject("c2756ad3-4f4a-45e5-93a3-4c10e892ebdc", "BuoyLight", "Pedestal Lamp (near Teller Window 1)", "overworld"),
    IslandObject("226bdfbc-c2ad-4bda-b9ec-2d72c09f1502", "BuoyLight", "Pedestal Lamp (near Teller Window 2)", "overworld"),
    IslandObject("d467b790-8968-46b1-9be9-e36eac9ebe52", "BuoyLight", "Pedestal Lamp (near Teller Window 3)", "overworld"),
    IslandObject("2cb8963f-ac2f-4f2a-b720-11a701bc4b08", "BuoyLight", "Pedestal Lamp (near Teller Window 4)", "overworld"),
    IslandObject("d2b218b4-279c-4e3e-9751-53a8cbfade87", "BuoyLight", "Pedestal Lamp (near Trap Room)", "overworld"),
)

OBJECT_KINDS: tuple[tuple[str, str], ...] = (
    ("WalkieTalkieProp", "Walkie-Talkie"), ("MegaphoneProp", "Megaphone"), ("FmRadioProp", "Radio"),
    ("BinocularsProp", "Binoculars"), ("LaserProp", "Laser"), ("TorchProp", "Torch"), ("XrayGogglesProp", "Scanner"),
    ("ClockProp", "Clock"), ("CompassProp", "Compass"), ("FoldingMapProp", "Folding Map"),
    ("CowBellLargeProp", "Large Cowbell"), ("CowBellMediumProp", "Medium Cowbell"),
    ("CowBellSmallProp", "Small Cowbell"), ("SalonBrushProp", "Paintbrush"),
    ("BuoyProp", "Lamp"), ("BuoyRedProp", "Red Lamp"), ("BuoyLight", "Pedestal Lamp"),
)
"""The kinds of ISLAND_OBJECTS, in id order: (the game's name, the location's)."""

OBJECT_KIND_KEYS: dict[str, str] = {
    "WalkieTalkieProp": "walkie_talkie", "MegaphoneProp": "megaphone", "FmRadioProp": "radio",
    "BinocularsProp": "binoculars", "LaserProp": "laser", "TorchProp": "torch", "XrayGogglesProp": "scanner",
    "ClockProp": "clock", "CompassProp": "compass", "FoldingMapProp": "map",
    "CowBellLargeProp": "cowbell", "CowBellMediumProp": "cowbell", "CowBellSmallProp": "cowbell",
    "SalonBrushProp": "paintbrush",
    "BuoyProp": "lamp", "BuoyRedProp": "lamp", "BuoyLight": "lamp",
}
"""The `object_sanity_kinds` key of each kind (the three cowbells share one)."""

OBJECT_KINDS_LEFT_OUT_BY_DEFAULT: frozenset[str] = frozenset({"paintbrush"})
OBJECT_KINDS_NEVER_TAKEN: frozenset[str] = frozenset({"SalonBrushProp"})
"""Kinds the mod never takes off the island, picked up or left out."""


class Light(NamedTuple):
    """A light whose first switching on is a check with `light_sanity`."""

    key: str
    """A lamp's `savablePropGuid`, or a wall switch's TrackedPeckState path."""
    location_name: str
    zone: str
    """As IslandObject.zone, plus "train" (the train's headlights: the Train item)."""
    switch: bool
    """A wall switch (`lamps_and_switches` only), not a lamp."""


LIGHT_ID_OFFSET = 14_000
"""`light_sanity`: by position in LIGHTS, up to 14_199."""

LIGHT_EVENT_ITEM_NAME = "Light"
"""The event every light holds beside its check, so the logic can count them for `big_lights`."""

OBJECT_EVENT_ITEM_NAME = "Object"
"""The event each object check holds beside it, so the logic can count them for `big_objects`."""


def object_event_name(location_name: str) -> str:
    return f"{location_name} (event)"

LIGHT_SWITCHES: tuple[tuple[str, str, str], ...] = (
    # Wall switches outside the puzzles, as (path, name, zone); the puzzles' own come with `lights`.
    ("LandmarksNonChallenge/DoofChamber/Positioner/FXControls/BasicLightSwitchHouse/LightSwitchPeckLogic",
     "Light Switch (Doof Chamber, House)", "tunnels"),
    ("LandmarksNonChallenge/DoofChamber/Positioner/FXControls/BasicLightSwitchRing/LightSwitchPeckLogic",
     "Light Switch (Doof Chamber, Ring)", "tunnels"),
    ("LandmarksNonChallenge/DoofChamber/Positioner/FXControls/ColouredSpotSwitches/BasicLightSwitchPurple/LightSwitchPeckLogic",
     "Light Switch (Doof Chamber, Purple)", "tunnels"),
    ("LandmarksNonChallenge/DoofChamber/Positioner/FXControls/ColouredSpotSwitches/BasicLightSwitchReds/LightSwitchPeckLogic",
     "Light Switch (Doof Chamber, Reds)", "tunnels"),
    ("LandmarksNonChallenge/DoofChamber/Positioner/FXControls/ColouredSpotSwitches/BasicLightSwitchWhiteSpots/LightSwitchPeckLogic",
     "Light Switch (Doof Chamber, White Spots)", "tunnels"),
    ("LandmarksNonChallenge/Salon/Positioner/SalonBuilding/BasicLightSwitch - Salon Main/LightSwitchPeckLogic",
     "Light Switch (Salon, Main)", "overworld"),
    ("LandmarksNonChallenge/Salon/Positioner/SalonBuilding/BasicLightSwitch - Mirror/LightSwitchPeckLogic",
     "Light Switch (Salon, Mirror)", "overworld"),
    ("LandmarksNonChallenge/Salon_Secret/Positioner/SalonBuilding/BasicLightSwitch - Salon Main/LightSwitchPeckLogic",
     "Light Switch (Secret Salon, Main)", "tunnels"),
    ("LandmarksNonChallenge/Salon_Secret/Positioner/SalonBuilding/BasicLightSwitch - Mirror/LightSwitchPeckLogic",
     "Light Switch (Secret Salon, Mirror)", "tunnels"),
    ("LandmarksNonChallenge/SpawnCourtyardandHubPlatform/Positioner/TeachingArea/Positioner/ObjectsTeaching/Switch/LightSwitch/BasicLightSwitch/LightSwitchPeckLogic",
     "Light Switch (Tutorial)", "start"),
    # Three trains, each switch on its engine's dashboard, named by the engine's tint.
    ("TrainSystem/TrainNew/TrainCarNew/CarriageBody/CarriageContentsFront/BasicLightSwitch - headlight/LightSwitchPeckLogic",
     "Light Switch (Red Train)", "train"),
    ("TrainSystem/TrainNew/TrainCarNew B(5)/CarriageBody/CarriageContentsFront/BasicLightSwitch - headlight/LightSwitchPeckLogic",
     "Light Switch (Green Train)", "train"),
    ("TrainSystem/TrainNew/TrainCarNew C(10)/CarriageBody/CarriageContentsFront/BasicLightSwitch - headlight/LightSwitchPeckLogic",
     "Light Switch (Yellow Train)", "train"),
    ("TrainSystem/TrainStop - Green Diffuser/Lighting/BasicLightSwitch/LightSwitchPeckLogic",
     "Light Switch (Green Diffuser Train Stop)", "overworld"),
    ("TrainSystem/TrainStop - Ring Room/Lighting/BasicLightSwitch/LightSwitchPeckLogic",
     "Light Switch (Ring Room Train Stop)", "overworld"),
    ("TrainSystem/TrainStop - South Valley Beach/Lighting/BasicLightSwitch/LightSwitchPeckLogic",
     "Light Switch (South Valley Beach Train Stop)", "overworld"),
    ("TrainSystem/TrainStop - Sports Valley/Lighting/BasicLightSwitch/LightSwitchPeckLogic",
     "Light Switch (Sports Valley Train Stop)", "overworld"),
    ("TrainSystem/TrainStop - Trap Room/Lighting/BasicLightSwitch/LightSwitchPeckLogic",
     "Light Switch (Trap Room Train Stop)", "overworld"),
    ("TrainSystem/TrainStop - Upper Hub/Lighting/BasicLightSwitch/LightSwitchPeckLogic",
     "Light Switch (Upper Hub Train Stop)", "overworld"),
)

LAMP_KINDS: frozenset[str] = frozenset({"BuoyProp", "BuoyRedProp", "BuoyLight"})


def object_kind_location_name(label: str) -> str:
    return f"First {label} Pickup"


LIGHTS: tuple[Light, ...] = (
    *(Light(obj.guid, f"{obj.location_name} Lit", obj.zone, False) for obj in ISLAND_OBJECTS if obj.kind in LAMP_KINDS),
    *(Light(key, f"{name} On", zone, True) for key, name, zone in LIGHT_SWITCHES),
)
"""Every light of `light_sanity`, in id order: the island's lamps, then the wall switches."""


# Zone locks: overworld lamps and gadgets locked by the zone of the nearest coloured tower.
ZONE_LOCK_ID_OFFSET = 14_300
"""By position in ZONE_LOCKS."""


class ZoneLock(NamedTuple):
    key: str
    """The option value (`start_with_zone_locks`): `red_lamps`, `red_gadgets`..."""
    item_name: str
    zone: str
    """The tower's colour, as in OVERWORLD_ZONES."""
    lamps: bool
    """The lamps and the light switches; False: the other gadgets."""


ZONE_COLOURS: tuple[tuple[str, str], ...] = (
    ("red", "bigKeyRedZone"), ("green", "bigKeyGreenZone"), ("blue", "bigKeyBlueZone"), ("yellow", "bigKeyYellowZone"),
)
"""The four coloured towers, by colour and big key prop."""

ZONE_LOCKS: tuple[ZoneLock, ...] = tuple(
    ZoneLock(f"{colour}_{kind}", f"{colour.title()} Zone {kind.title()}", colour, kind == "lamps")
    for colour, _ in ZONE_COLOURS for kind in ("lamps", "gadgets"))

ZONE_LOCK_KEYS: frozenset[str] = frozenset(lock.key for lock in ZONE_LOCKS)

# Nearest tower centre (MonumentOrder.Centres) of each entry; fix by hand one on the wrong side.
OVERWORLD_ZONES: dict[str, str] = {
    "ed2f8967-b4c1-43d9-91b5-11679be10bc6": "blue",  # Binoculars (Peak Marker)
    "24d2f9a5-5376-4a7c-b08c-17e9c060855b": "blue",  # Lamp (Diffuser Peak)
    "12a7f64b-8175-4857-a371-77dc2989cd68": "blue",  # Lamp (near Cannonball 2)
    "59a73457-6368-4639-8ec7-335902797343": "blue",  # Lamp (near Coordinates)
    "dab5cfc8-f4d9-44f2-abb0-90bc42b3cade": "blue",  # Pedestal Lamp (Diffuser Peak)
    "52b0dfcb-4588-4ce7-a78c-bdb9cb800fe6": "blue",  # Pedestal Lamp (Peak Marker 1)
    "f84cb1c7-1702-4fa1-9ddb-f4c334041237": "blue",  # Pedestal Lamp (Peak Marker 2)
    "cd88c773-6375-42c7-9475-76b288bd1c91": "blue",  # Pedestal Lamp (Peak Marker 3)
    "b2f9b7af-39f5-431c-9908-c2ebf6d6453b": "blue",  # Pedestal Lamp (near Breadcrumb Loop 1)
    "accb5df0-1452-4b84-8607-9f01bd517103": "blue",  # Pedestal Lamp (near Observation Room 3)
    "a552b903-066f-45a8-8550-4cbf5652de74": "blue",  # Pedestal Lamp (near Poet and Priest)
    "ec17f547-5b1f-4fcf-828e-f2932dc2f620": "blue",  # Radio (Factory Music Garden)
    "ea8c71ac-810f-4426-84a1-17e0e3b0b81d": "blue",  # Radio (Peak Marker)
    "cee6114c-2dc0-4d02-a393-5686a2f2e9b8": "blue",  # Walkie-Talkie (Back Beach Hill, Yellow)
    "6efb6db4-76c1-4ffa-b249-8badc6d4bf82": "blue",  # Walkie-Talkie (Peak, Blue)
    "a5992995-191a-45b7-aa23-e9a64a4fede7": "blue",  # Walkie-Talkie (Peak, Yellow)
    "44015a0e-20ca-441a-b6e9-bbd28657b5f3": "green",  # Clock (Ring Room Train Stop)
    "e56da5eb-bac5-4c5e-b489-a8de3d204e7c": "green",  # Clock (South Valley Beach Train Stop)
    "f468d4dc-594e-4c62-80d4-3a85d9e25b3e": "green",  # Clock (Trap Room Train Stop)
    "087f1e73-309b-4db2-8e39-dee6c926ad59": "green",  # Lamp (near Invisible Ink)
    "f4819494-6b1d-48ef-823b-4b26d2ceda53": "green",  # Lamp (near Memory Bombs)
    "13956d25-97a1-4b5c-9992-00c187092476": "green",  # Lamp (near Musical Holiday)
    "982f4dbf-22bb-4553-aa9c-dd6588c19fda": "green",  # Lamp (near Optical Telegraph)
    "632fc258-f424-4b88-930a-a62c45044965": "green",  # Lamp (near Trap Room)
    "TrainSystem/TrainStop - Ring Room/Lighting/BasicLightSwitch/LightSwitchPeckLogic": "green",
    "TrainSystem/TrainStop - South Valley Beach/Lighting/BasicLightSwitch/LightSwitchPeckLogic": "green",
    "TrainSystem/TrainStop - Trap Room/Lighting/BasicLightSwitch/LightSwitchPeckLogic": "green",
    "7e2f3ba8-2124-4fe9-8707-1621be5a0926": "green",  # Megaphone (Pyramid Holder)
    "16990dcc-ebb8-453e-88fc-6478e3980728": "green",  # Pedestal Lamp (near Breadcrumb Loop 2)
    "b46c590f-7038-4033-b84d-e3fb664e095c": "green",  # Pedestal Lamp (near Musical Holiday 1)
    "d069a848-37f5-40e0-ac18-64516951f481": "green",  # Pedestal Lamp (near Musical Holiday 2)
    "c7141d1b-96bf-4c2c-8c05-3d2067e64a93": "green",  # Pedestal Lamp (near Musical Holiday 3)
    "0e8faaf2-b5fd-4906-aaab-788f8ccbf603": "green",  # Pedestal Lamp (near Perspective Counting)
    "7017169d-96f6-4578-8c1f-eb65f3549e32": "green",  # Pedestal Lamp (near Ring Room)
    "d2b218b4-279c-4e3e-9751-53a8cbfade87": "green",  # Pedestal Lamp (near Trap Room)
    "87dbbb84-5c58-43a4-96ba-e43c1c7308b9": "green",  # Radio (Greensteps Music Garden)
    "070cf8f2-6824-413e-9c2a-43a1027c24c5": "green",  # Radio (Musical Holiday)
    "00d7ed74-a4d9-4969-b8e2-bf8d4975ac8a": "green",  # Walkie-Talkie (Ocean Treasure, Blue)
    "cd159c6e-88aa-4158-a11f-2790cf581052": "green",  # Walkie-Talkie (Ocean Treasure, Yellow)
    "88942e8e-6fae-4d71-93f0-8500741a82d1": "red",  # Binoculars (Carousel)
    "209a7dc3-9dcb-425e-86c5-3a84aa9bd0ec": "red",  # Clock (Sports Valley Train Stop)
    "a08daa18-2fef-4a49-a062-2baafdf48686": "red",  # Clock (Upper Hub Train Stop)
    "5d10fec3-7a2d-4bfd-be0e-6196152d88f5": "red",  # Lamp (near Cannonball 1)
    "f7f2e9ed-14fd-4a54-a918-fc06b444a39e": "red",  # Lamp (near Easy Simultaneous Press)
    "cec1412b-4727-48c4-b6a1-5a5e7989af18": "red",  # Lamp (near High Button 1)
    "94876c24-50ba-41cb-8fdd-9bd026f3be32": "red",  # Lamp (near High Button 2)
    "df814b39-e550-4e17-ad44-e32028ab3c47": "red",  # Lamp (near Kick Up Pits)
    "17e22f02-4f01-4aa4-83fb-d2871acd97cd": "red",  # Lamp (near Pointer's Paradise)
    "d3e57e13-a91d-492c-bd46-2c59da8bef9e": "red",  # Lamp (near Telescope to Box)
    "5ee8fa69-8937-4841-b9a7-ee8a10c3a933": "red",  # Lamp (near Teller Window 1)
    "4b9cad35-ab44-4dfe-9e4b-390cb165aca5": "red",  # Lamp (near Teller Window 2)
    "LandmarksNonChallenge/Salon/Positioner/SalonBuilding/BasicLightSwitch - Salon Main/LightSwitchPeckLogic": "red",
    "LandmarksNonChallenge/Salon/Positioner/SalonBuilding/BasicLightSwitch - Mirror/LightSwitchPeckLogic": "red",
    "TrainSystem/TrainStop - Sports Valley/Lighting/BasicLightSwitch/LightSwitchPeckLogic": "red",
    "TrainSystem/TrainStop - Upper Hub/Lighting/BasicLightSwitch/LightSwitchPeckLogic": "red",
    "52fedcd9-caa6-4ec6-a285-f73e6ff40f50": "red",  # Paintbrush (Salon)
    "60412245-4b6c-47eb-a4e7-67eec146bf20": "red",  # Pedestal Lamp (near Basketball)
    "d0a1c556-d62f-42df-a36e-019636da7679": "red",  # Pedestal Lamp (near Fielding)
    "5efbf72b-9dff-4251-a536-f625ba8ff315": "red",  # Pedestal Lamp (near High Button 1)
    "fee806b8-8617-49f8-99af-bdea0c148ed0": "red",  # Pedestal Lamp (near High Button 2)
    "4ec6aca0-9961-4550-9c26-7cacb9917566": "red",  # Pedestal Lamp (near High Button 3)
    "38722a78-da82-4d6e-80c4-efd70e1be90f": "red",  # Pedestal Lamp (near Pointer's Paradise 1)
    "d368b4c7-8389-4f63-a20f-ba88ef17f0a5": "red",  # Pedestal Lamp (near Pointer's Paradise 2)
    "007a1599-17ad-426f-a68b-37718423e9f7": "red",  # Pedestal Lamp (Hub 1)
    "99d8a99a-89eb-4ad0-8736-104aafe1fdd7": "red",  # Pedestal Lamp (Hub 2)
    "fa653354-bdfa-431d-aa19-7453e5730e80": "red",  # Pedestal Lamp (Hub 3)
    "942347f0-cd23-445f-8019-d296e5c92ead": "red",  # Pedestal Lamp (Hub 4)
    "0abc8402-398d-4f0d-bf5e-fb3051a16e21": "red",  # Pedestal Lamp (Hub 5)
    "c2756ad3-4f4a-45e5-93a3-4c10e892ebdc": "red",  # Pedestal Lamp (near Teller Window 1)
    "226bdfbc-c2ad-4bda-b9ec-2d72c09f1502": "red",  # Pedestal Lamp (near Teller Window 2)
    "d467b790-8968-46b1-9be9-e36eac9ebe52": "red",  # Pedestal Lamp (near Teller Window 3)
    "2cb8963f-ac2f-4f2a-b720-11a701bc4b08": "red",  # Pedestal Lamp (near Teller Window 4)
    "41e7b481-2e10-4d62-9136-5e6e0ba9fa7d": "red",  # Radio (Lighthouse Music Garden)
    "ebdec567-58c0-48c7-a62a-bbd843bef36f": "red",  # Radio (Upper Hub)
    "044daaf7-fb90-4f73-a9bb-00b8a3df9c60": "red",  # Scanner (Fireworks Platform)
    "28cf2386-5e54-4b7e-99ba-283298aa3816": "red",  # Scanner (Upper Hub)
    "ac52bd4b-990f-4908-999d-5badeb8b6eda": "red",  # Torch (Tutorial Exit)
    "185e3864-0dcb-498b-baee-6da1a4b21a8e": "red",  # Walkie-Talkie (Red Cone Hill, Yellow)
    "f29614f5-0c70-4ed8-b152-9e57b371f20d": "red",  # Walkie-Talkie (Red Cone Valley, Blue)
    "1c9f8bc9-5850-4085-80f6-05c2821b505d": "yellow",  # Clock (Green Diffuser Train Stop)
    "80e38f3e-d054-4187-9675-66d6e70c8356": "yellow",  # Lamp (near Egg Hunt)
    "6a673617-992d-49ca-87f9-223aef4c9fe4": "yellow",  # Lamp (near Scout Bombs)
    "6beb48a3-60de-41e8-a9f6-d39772bcdfa4": "yellow",  # Lamp (near Singer and Selector)
    "TrainSystem/TrainStop - Green Diffuser/Lighting/BasicLightSwitch/LightSwitchPeckLogic": "yellow",
    "c6259d07-2499-4063-b033-b72cddd951c2": "yellow",  # Megaphone (Ramp Holder)
    "080554bb-bc16-41cd-bc96-4765cec96beb": "yellow",  # Pedestal Lamp (near Egg Hunt)
    "11c1d018-3dbf-46d4-b1de-f9aba4d29fc2": "yellow",  # Pedestal Lamp (near Observation Room 1)
    "bef6d32e-57a9-4ba1-b984-05fdec08800c": "yellow",  # Pedestal Lamp (near Observation Room 2)
    "4ee18fc7-2a43-44e0-9a2b-4508e9d2705d": "yellow",  # Pedestal Lamp (near Singer and Selector 1)
    "c716c86d-81a7-40cc-873f-0405821dbd72": "yellow",  # Pedestal Lamp (near Singer and Selector 2)
    "cef7ae31-a105-491d-8d0d-daab0253909c": "yellow",  # Pedestal Lamp (near Singer and Selector 3)
    "d8e70c55-9800-498e-af4e-7054093950e4": "yellow",  # Radio (Floorplan Music Garden)
    "56a7313e-9e55-485b-ac52-fcc6f8030133": "yellow",  # Walkie-Talkie (Beach, Blue)
}


def zone_of(key: str, zone: str) -> str | None:
    """The zone colour of a lamp, gadget or switch, or None when no zone lock applies."""
    if zone.startswith("tower:"):
        return next((colour for colour, prop in ZONE_COLOURS if zone == f"tower:{prop}"), None)
    if zone == "overworld":
        return OVERWORLD_ZONES.get(key)
    return None


def zone_lock_for(key: str, zone: str, lamps: bool) -> ZoneLock | None:
    colour = zone_of(key, zone)
    if colour is None:
        return None
    return next(lock for lock in ZONE_LOCKS if lock.zone == colour and lock.lamps == lamps)


def zone_lock_item_id(lock: ZoneLock) -> int:
    return BASE_ID + ZONE_LOCK_ID_OFFSET + ZONE_LOCKS.index(lock)


def light_event_name(light: Light) -> str:
    return f"{light.location_name} (event)"


def light_location_id(light: Light) -> int:
    return BASE_ID + LIGHT_ID_OFFSET + LIGHTS.index(light)


def object_location_id(obj: IslandObject) -> int:
    return BASE_ID + OBJECT_ID_OFFSET + ISLAND_OBJECTS.index(obj)


def object_kind_location_id(position: int) -> int:
    return BASE_ID + OBJECT_KIND_ID_OFFSET + position


MAP_ROOM_ITEM_NAME = "Map Room"


def tower_by_prop(prop_name: str) -> Tower:
    return next(tower for tower in TOWERS if tower.prop_name == prop_name)


def pickup_location_id(position: int) -> int:
    return BASE_ID + PICKUP_ID_OFFSET + position


def firework_location_id(position: int) -> int:
    return BASE_ID + FIREWORK_ID_OFFSET + position


def flare_gun_location_id(position: int) -> int:
    return BASE_ID + FLARE_GUN_ID_OFFSET + position


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


def puzzle_location_id(puzzle: Puzzle) -> int:
    return BASE_ID + puzzle.prop_value


def puzzle_need_item_id(need: PuzzleNeed) -> int:
    return BASE_ID + PUZZLE_NEED_ID_OFFSET + PUZZLE_NEEDS.index(need)


def key_deposit_location_id(tower: Tower) -> int:
    return BASE_ID + tower.prop_value


def feature_item_id(tower: Tower) -> int:
    """The feature the tower's key opens: map room, chairlift, train, tunnels..."""
    return BASE_ID + tower.prop_value


def key_item_name(tower: Tower) -> str:
    """The physical key, named after its tower like its own cut and deposit checks."""
    return f"{tower.key_name} Key"


def key_item_id(tower: Tower) -> int:
    return BASE_ID + KEY_ITEM_ID_OFFSET + tower.prop_value


def cut_location_name(tower: Tower, index: int) -> str:
    """Numbered from 1 for the player, while `index` counts from 0 like the mod."""
    return f"{tower.key_name} Key Cut {index + 1}"


def cut_location_id(tower: Tower, index: int) -> int:
    return BASE_ID + CUT_ID_OFFSET + tower.prop_value * CUT_STRIDE + index


def cut_locations(tower: Tower) -> tuple[str, ...]:
    return tuple(cut_location_name(tower, index) for index in range(tower.segments))


def radio_location_id(station: RadioStation) -> int:
    return BASE_ID + RADIO_ID_OFFSET + station.system_value


def radio_item_id(station: RadioStation) -> int:
    # Same id as the location: item and location ids are separate namespaces in Archipelago.
    return BASE_ID + RADIO_ID_OFFSET + station.system_value


# Arch doors: the first one and the Drawbridge lead out of the start; the other two are shortcuts.


class ArchDoor(NamedTuple):
    system_name: str
    """`SavableSystem` enum name, the SaveManager key the door's state lives under."""
    system_value: int
    """`SavableSystem` enum value; the item id is BASE_ID + ARCH_DOOR_ID_OFFSET + this."""
    item_name: str


FIRST_ARCH_DOOR = ArchDoor("SpawnHubGate", 11, "First Arch Door")
LEFT_ARCH_DOOR = ArchDoor("HubShortcutToSportsCreek", 13, "Left Arch Door")
RIGHT_ARCH_DOOR = ArchDoor("HubTunnel", 12, "Right Arch Door")
ARCH_DOORS: tuple[ArchDoor, ...] = (FIRST_ARCH_DOOR, LEFT_ARCH_DOOR, RIGHT_ARCH_DOOR)


# Both doors can be walked round; `require_arch_doors` only keeps a shortcut from coming too late.
TOWER_FAR_DOOR: dict[str, ArchDoor] = {
    "bigKeyGreenZone": RIGHT_ARCH_DOOR,
    "bigKeyBlueZone": LEFT_ARCH_DOOR,
    "bigKeyYellowZone": LEFT_ARCH_DOOR,
    "bigKeyBoss": LEFT_ARCH_DOOR,
    "bigKeyOverflow": LEFT_ARCH_DOOR,
}
"""The far door each tower lies past, by the tower's `prop_name`. The drawbridge has none."""

ZONE_FAR_DOOR: dict[str, ArchDoor | None] = {
    "chairlift": RIGHT_ARCH_DOOR,
    "tunnels": LEFT_ARCH_DOOR,
    # The ending can be walked to round the Left door, so it is never held behind it.
    "ending": None,
    "green_dome": LEFT_ARCH_DOOR,
}
"""The far door each zone behind a big key lies past (`regions.py` names the entrances)."""


def arch_door_item_id(door: ArchDoor) -> int:
    return BASE_ID + ARCH_DOOR_ID_OFFSET + door.system_value


def deposit_location_name(amount: int) -> str:
    return f"Gourd Deposit {amount}"


def deposit_location_id(amount: int) -> int:
    return BASE_ID + DEPOSIT_ID_OFFSET + amount


# Tunnel doors (`tunnel_sanity`): each GateLogic opens alone; the purple hole is SouthPeakTunnel.

TUNNEL_DOOR_ID_OFFSET = 14_400
"""By position in TUNNEL_DOORS."""


class TunnelDoor(NamedTuple):
    item_name: str
    gates: tuple[str, ...]
    """Paths of the `GateLogic` states the mod opens."""
    tower: str | None
    """`prop_name` of the tower the outlet sits by; None for the purple hole."""


PURPLE_TUNNEL_ITEM_NAME = "Purple Tunnel"

TUNNEL_DOORS: tuple[TunnelDoor, ...] = (
    *(TunnelDoor(f"{colour.title()} Tunnel Door", (f"TunnelSystem/TunnelOutlet - {colour.title()}/GateLogic",), prop)
      for colour, prop in ZONE_COLOURS),
    TunnelDoor(PURPLE_TUNNEL_ITEM_NAME, (
        "TunnelSystem/SouthPeakTunnel/TunnelOutlet_Simple/GateLogic",
        "TunnelSystem/SouthPeakTunnel/TunnelOutlet_Simple (1)/GateLogic",
    ), None),
)


def tunnel_door_item_id(door: TunnelDoor) -> int:
    return BASE_ID + TUNNEL_DOOR_ID_OFFSET + TUNNEL_DOORS.index(door)
