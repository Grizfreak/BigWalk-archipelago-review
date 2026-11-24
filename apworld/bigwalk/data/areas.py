"""What sits behind a big key, and the start zone's footholds."""

from __future__ import annotations

from .puzzles import PUZZLE_NEEDS, PUZZLES, PuzzleNeed, puzzle_needs
from .towers import RADIO_STATIONS


def tutorial_footholds(started: frozenset[str] | set[str]) -> tuple[PuzzleNeed, ...]:
    """Needs that, added to `started`, open a tutorial puzzle; empty if one is already open."""
    def opens(have: set[str]) -> bool:
        return any(all(need.key in have for need in puzzle_needs(puzzle))
                   for puzzle in PUZZLES if puzzle.prop_name in START_ZONE_PUZZLES)

    if opens(set(started)):
        return ()
    return tuple(need for need in PUZZLE_NEEDS if opens(set(started) | {need.key}))


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
