"""Quality of life options, puzzle helpers included."""

from __future__ import annotations

from Options import Choice, DefaultOnToggle, Range, Toggle


class HintDiscoveredKeyLocations(Toggle):
    """
    When a tower is reached (its opening button pressed), hint what sits at its key cuts and its key deposit, so a player who
    walks up to a tower learns what is waiting there. Free, like the hints at the start.
    """

    display_name = "Hint Discovered Key Locations"


class HintDiscoveredDepositBoxes(Toggle):
    """
    With Tower Mode in_order or by_item: when a tower is reached, hint the gourd deposit checks that sit in its
    monument (the gourds placed there, counted from the towers before it). Free, like the hints at the start.
    Without a tower order the deposits belong to no tower, and nothing is hinted.
    """

    display_name = "Hint Discovered Deposit Boxes"


class FastGolf(Toggle):
    """
    The golf ball (the cannonball of the golf puzzles) is no longer too heavy, and is carried like any object.
    """

    display_name = "Fast Golf"


class TeleportBackToHub(Toggle):
    """
    With Teleport Buttons, a button back to the hub in each destination.
    """

    display_name = "Teleport Back to Hub"


class TrackerBoard(Toggle):
    """
    The right sign of the hub's notice board counts the checks not done yet, and names a few of those a hint
    points to. For what you can do now, use a tracker (Universal Tracker, PopTracker). Mod only.
    """

    display_name = "Tracker Board"


class MapTracker(Choice):
    """
    The big map of the Map Room tracks the seed: every check stands on it (puzzles, objects, lamps, switches,
    fireworks, radio stations, towers...), a small copy of its object on a pin, and leaves it once done. Mod only.

    - off: the game's map.
    - all: every check not done, those a hint points to on a gold pin.
    """

    display_name = "Map Tracker"
    option_off = 0
    option_all = 2
    alias_in_logic = 2
    default = 0


class HintBoard(DefaultOnToggle):
    """
    The centre sign of the hub's notice board shows your hint points and the cost of a hint, and lists the hints you
    asked for that are not found yet. Write an item's name on it to ask for a hint (it costs points, as `!hint`).
    Mod only.
    """

    display_name = "Hint Board"


class TowerResyncStations(Toggle):
    """
    A resync station in each of the five towers, at the Drawbridge and at the Green Dome Tower: a
    button that brings your gourds, keys and gadgets lying around back in front of it. The hub's
    station is always there.
    """

    display_name = "Tower Resync Stations"


class GuestsCanResync(Toggle):
    """
    The guests may use the resync stations too. Off, only the host can.
    """

    display_name = "Guests Can Resync"


class TeleportButtons(Choice):
    """
    Buttons in the hub that teleport you to the Red, Green, Blue, Yellow and Black towers, the
    Silent Gauntlet, the Drawbridge and the Green Dome Tower.

    - off: none.
    - free: all there from the start.
    - with_towers: once the place has been opened on foot (the tower's button, the chapel's), or
      for the Drawbridge and the Green Dome Tower, once a player has stood there.
    - items: the same, and each also needs its Teleporter item (8 items).
    """

    display_name = "Teleport Buttons"
    option_off = 0
    option_free = 1
    option_with_towers = 2
    option_items = 3
    default = option_off


class CabinFeverSeconds(Range):
    """
    The wait of the Cabin Fever puzzle, in seconds; 300 (5 minutes) is the game's own. Mod only; it
    changes no logic. For a wait drawn once per seed, Archipelago's own `random-range-60-300`.
    """

    display_name = "Cabin Fever Wait"
    range_start = 0
    range_end = 3600
    default = 300


class CabinFeverHelp(Toggle):
    """
    A hidden button in the Cabin Fever room that takes ten seconds off the wait each time it is
    pressed. Mod only.
    """

    display_name = "Cabin Fever Help Button"


class CabinFeverHideTimer(Toggle):
    """
    The Cabin Fever room's dial hides the time left: "??:??" while the wait runs, with now and then a digit
    flashing. The game's sound still plays over the last ten seconds. Mod only.
    """

    display_name = "Cabin Fever Hide Timer"


class CabinFeverLongSeconds(Range):
    """
    The wait of the Cabin Fever Long puzzle, in seconds; 1800 (30 minutes) is the game's own. Mod
    only; it changes no logic. For a wait drawn once per seed, `random-range-300-1800`.
    """

    display_name = "Cabin Fever Long Wait"
    range_start = 0
    range_end = 3600
    default = 1800


class CabinFeverLongHelp(Toggle):
    """
    A hidden button in the Cabin Fever Long room that takes ten seconds off the wait each time it is
    pressed. Mod only.
    """

    display_name = "Cabin Fever Long Help Button"


class CabinFeverLongHideTimer(Toggle):
    """
    The Cabin Fever Long room's dial hides the time left: "??:??" while the wait runs, with now and then a
    digit flashing. The game's sound still plays over the last ten seconds. Mod only.
    """

    display_name = "Cabin Fever Long Hide Timer"


class TileThief(Choice):
    """
    A button beside the Tile Thief puzzle that makes peg tiles in front of it.

    - vanilla: no button.
    - easy: the tiles the puzzle expects.
    - chaos: one tile of every kind the puzzle draws from; the answer is still yours to find.

    With Lock Puzzle Needs, it waits for the panels Tile Thief is built from.
    """

    display_name = "Tile Thief"
    option_vanilla = 0
    option_easy = 1
    option_chaos = 2
    default = option_vanilla
