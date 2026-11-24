"""The gourd puzzles, their needs and their ids."""

from __future__ import annotations

from typing import NamedTuple

from .ids import BASE_ID, PUZZLE_NEED_ID_OFFSET


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


def puzzle_needs(puzzle: Puzzle) -> tuple[PuzzleNeed, ...]:
    """The needs of this puzzle, in PUZZLE_NEEDS order."""
    tags = PUZZLE_TAGS[puzzle.prop_name]
    return tuple(need for need in PUZZLE_NEEDS if need.key in tags)


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


def puzzle_location_id(puzzle: Puzzle) -> int:
    return BASE_ID + puzzle.prop_value


def puzzle_need_item_id(need: PuzzleNeed) -> int:
    return BASE_ID + PUZZLE_NEED_ID_OFFSET + PUZZLE_NEEDS.index(need)
