"""Goal, objectives and the towers' options."""

from __future__ import annotations

from Options import Choice, DefaultOnToggle, OptionSet, Range

from .. import data


class Goal(Choice):
    """
    What you need to do to win. No goal can be done solo.

    - big_wall: break the bell inside the Big Wall. Needs the Big Wall
      Door.
    - big_goodbye: complete the Silent Gauntlet, behind the Big Wall. Needs
      the Big Wall Door. Saying farewell afterwards is up to you.
    - big_game: reach the secret ending behind the Spawn Secret Door. Needs
      that item, and nothing else: the sphere that normally seals it until
      you have finished the game is gone from the start.
    - big_collection: place a number of gourds in the towers' slots (see
      Gourds Required).
    """

    display_name = "Goal"
    # Named after the game's achievements, in order of effort; big_collection is Archipelago's.
    option_big_wall = 0
    option_big_goodbye = 1
    option_big_game = 2
    option_big_collection = 3
    # Older names, so an older YAML still reads; the first ones are also GOAL_ON_THE_WIRE's.
    alias_ending = option_big_wall
    alias_gauntlet = option_big_goodbye
    alias_second_ending = option_big_game
    alias_secret_ending = option_big_game
    alias_deposits = option_big_collection
    alias_gourds = option_big_collection
    default = option_big_goodbye


GOAL_ON_THE_WIRE = {
    "big_wall": "ending",
    "big_goodbye": "gauntlet",
    "big_game": "second_ending",
    "big_collection": "deposits",
}
"""The string slot_data sends for each goal. Frozen: a released mod must keep reading it."""


OBJECTIVE_ENDINGS = ("big_goodbye", "big_game", "big_wall")
"""What can end a list: the Gauntlet's bell, the secret ending, or the chapel's bell."""


EXCLUSIVE_ENDINGS = ("big_goodbye", "big_game")
"""The endings a save plays only one of; the chapel's bell can go beside either."""


OBJECTIVE_KEYS = (
    "big_wall", "big_goodbye", "big_game", "big_collection", "big_keys", "big_radio", "big_towers",
    "big_climb", "big_party", "big_lights", "big_objects", "big_body", "big_doof", "big_makeover",
)
"""Every objective, in the order the slot data lists them. Frozen: the mod reads these strings."""


OBJECTIVE_ALIASES = {
    # The Doof Chamber was first meant to be a club.
    "big_club": "big_doof",
    # The party's old name; the mod still reads it in an older seed.
    "big_fiesta": "big_party",
}
"""An objective's older names, still read in a YAML, and what each one is now."""


class Objectives(OptionSet):
    """
    What a seed asks for besides its Goal, in any order: the Goal (the ending) is then won once every objective listed
    is done too. Empty (the default): the Goal alone, as before. With Random Objectives, a few of these are drawn instead.

    - big_collection: place a number of gourds in the towers' slots (see Gourds Required).
    - big_keys: place a number of big keys in their plinths, the drawbridge's included (see Keys Required).
    - big_radio: switch on a number of radio stations (see Radio Stations Required; needs Radio Sanity).
    - big_towers: complete a number of towers (see Towers Required; needs Tower Mode in_order or by_item).
    - big_lights: switch on a share of the island's lights, each at least once (see Lights Required). Without Light
      Sanity the lamps count, with no check of their own.
    - big_objects: pick up a share of the island's objects (see Objects Required; needs Object Sanity).
    - big_body: get back a share of the body's abilities (see Body Required; needs Lock Abilities).
    - big_climb: stand on the highest point of the island.
    - big_party (also read as big_fiesta): the party on the beach, a radio plugged into the big speakers with a music playing. Needs a Radio,
      and with Shuffle Radio Music one of the stations, which become progression.
    - big_doof (also read as big_club): the Doof Chamber, the music room in the tunnels: its five light switches on
      and its smoke machine's button held, ten seconds.
    - big_makeover: be painted at the Secret Salon, in the tunnels (with Everyone Gathers, every player, each in turn).
    With Everyone Gathers, big_climb, big_party, big_doof and big_makeover want every player there.

    The endings (big_wall, big_goodbye, big_game) are not listed here: they are the Goal.
    """

    display_name = "Objectives"
    # world._check_the_objectives refuses the endings clearly and maps the old names.
    valid_keys = frozenset(OBJECTIVE_KEYS) | set(OBJECTIVE_ALIASES)
    default = frozenset()


class RandomObjectives(Range):
    """
    Draws this many objectives at random, from the Objectives listed or, if none are, from all of those the other options
    allow. 0 (the default): the Objectives listed are all asked for, none drawn.
    """

    display_name = "Random Objectives"
    range_start = 0
    range_end = len(OBJECTIVE_KEYS) - len(OBJECTIVE_ENDINGS)
    default = 0


class LockEndingsUntilObjectives(DefaultOnToggle):
    """
    With Objectives: the ending's entry point (the chapel's bell, the Gauntlet's bell door, the secret room's
    printer) stays shut on every screen until the other objectives are done, so an ending,
    which plays once per save, is never played too early. On by default (0.6). Off: nothing is shut, and the ending
    can be played in any order.
    """

    display_name = "Lock Endings Until Objectives"


class KeysRequired(Range):
    """
    Only relevant if the Objectives hold "big_keys".

    How many big keys must be placed in their plinths, the drawbridge's and the six towers'.
    """

    display_name = "Keys Required"
    range_start = 1
    range_end = len(data.TOWERS)
    default = len(data.TOWERS)


class LightsRequired(Range):
    """
    Only relevant if the Objectives hold "big_lights".

    The share of the island's lights (those Light Sanity counts, or the lamps without it) that must have been
    switched on at least once, in percent.
    """

    display_name = "Lights Required"
    range_start = 1
    range_end = 100
    default = 50


class ObjectsRequired(Range):
    """
    Only relevant if the Objectives hold "big_objects".

    The share of Object Sanity's checks (each object with every, each kind with first_pickup) that must be sent, in
    percent.
    """

    display_name = "Objects Required"
    range_start = 1
    range_end = 100
    default = 50


class BodyRequired(Range):
    """
    Only relevant if the Objectives hold "big_body".

    The share of the ability items (Legs, Seat, the arms, Throwing, Carrying; not Running) that must have been
    received, in percent.
    """

    display_name = "Body Required"
    range_start = 1
    range_end = 100
    default = 100


class RadioStationsRequired(Range):
    """
    Only relevant if the Objectives hold "big_radio".

    How many radio stations must be switched on. Needs Radio Sanity, whose checks are the stations.
    """

    display_name = "Radio Stations Required"
    range_start = 1
    range_end = len(data.RADIO_STATIONS)
    default = len(data.RADIO_STATIONS)


class GourdsRequired(Range):
    """
    Only relevant if the Goal, or the Objectives, hold "big_collection".

    How many gourds must be placed in the towers' slots to win.
    """

    display_name = "Gourds Required"
    range_start = 5
    range_end = data.MAX_MONUMENT_SLOTS
    default = 30


class TowerMode(Choice):
    """
    How the towers' monuments open for the gourds.

    - all_open: every monument takes gourds from the start, in any order. The way it has always been.
    - in_order: one monument at a time, in an order the seed draws: the next opens once the one before is
      full, and a monument that is not the next refuses gourds, and its tower's button stays still.
    - by_item: the same order, and the next monument also waits for a Progressive Monument item, the first
      monument being open from the start.
    """

    display_name = "Tower Mode"
    option_all_open = 0
    option_in_order = 1
    option_by_item = 2
    default = option_all_open


class FirstTowerOpen(DefaultOnToggle):
    """
    With Tower Mode by_item: the first monument takes gourds from the start (the default). Off: it waits for a
    Progressive Monument like the others, and the seed starts with no monument open. Needs by_item: nothing could
    open the first monument in in_order.
    """

    display_name = "First Tower Open"


class EveryoneGathers(DefaultOnToggle):
    """
    Big Climb and Big Party count their ten seconds only while every player is there: at the island's highest point for
    the climb, near the big speakers on the beach for the party (the default). Off: one player is enough.
    """

    display_name = "Everyone Gathers"


class TowersRequired(Range):
    """
    Only relevant if the Objectives hold "big_towers" (which needs Tower Mode in_order or by_item).

    How many towers must be completed, a tower being complete when its monument is full, in the seed's order.
    """

    display_name = "Towers Required"
    range_start = 1
    range_end = len(data.TOWERS)
    default = len(data.TOWERS)
