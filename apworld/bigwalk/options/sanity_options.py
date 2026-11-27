"""Options that turn things in the world into locations."""

from __future__ import annotations

from Options import Choice, DefaultOnToggle, OptionSet, Range, Toggle

from .. import data


class GourdSanity(Choice):
    """
    Checks for placing gourds in the towers' slots, counted across all towers together.

    - off: none.
    - every_5: a check every 5 gourds.
    - every_gourd: a check for every gourd.
    """

    display_name = "Gourd Sanity"
    option_off = 0
    option_every_5 = 1
    option_every_gourd = 2
    # Pre-rename names, kept readable and still sent to the mod.
    alias_none = option_off
    alias_milestones = option_every_5
    alias_all = option_every_gourd
    # A bare `on` / `true` reads as a boolean: take it as the default rather than refuse it.
    alias_on = option_every_5
    alias_true = option_every_5
    default = 1


GOURD_SANITY_ON_THE_WIRE = {
    "off": "none",
    "every_5": "milestones",
    "every_gourd": "all",
}
"""The pre-rename strings slot_data keeps sending; see GOAL_ON_THE_WIRE."""


class RadioSanity(DefaultOnToggle):
    """
    Switching on each of the seven radio stations is a check.
    """

    display_name = "Radio Sanity"


class PackSanity(Choice):
    """
    The island's backpacks, belts and gourd carton (12 in all) as checks.

    - on: picking each one up for the first time is a check (12 locations); the pack then
      disappears from the map, so the packs a player can wear are still the items of the pool.
    - junk: the same checks, holding only filler or a trap.
    - vanilla: each pack is a check that gives itself: it stays in the hands of whoever picked it up, and no
      other location gives a pack (backpacks_in_pool, belts_in_pool and gourd_cartons_in_pool do nothing).
    - off: as in the game, free to take on the map, and no location.
    """

    display_name = "Pack Sanity"
    option_off = 0
    option_on = 1
    option_junk = 2
    option_vanilla = 3
    # Old toggle values still read; `self` is what slot_data sends for vanilla.
    alias_true = option_on
    alias_false = option_off
    alias_self = option_vanilla
    default = option_on


class FireworkSanity(DefaultOnToggle):
    """
    Firing each of the eight firework launchers (the button at its foot) for the first time is a
    check: 8 locations.
    """

    display_name = "Firework Sanity"


class ObjectSanity(Choice):
    """
    The island's gadgets (walkie-talkies, radios, megaphones, binoculars, lasers, torches, scanners, clocks, the
    compass, the map, the cowbells) stay on the map, and picking them up sends checks.

    - off: no check, and the gadgets the pool holds are taken off the map as before.
    - first_pickup: the first one of each kind picked up (16 checks by default); the objects stay.
    - every: each one, the first time it is picked up (144 checks by default); it then leaves the map, like a pack.
    """

    display_name = "Object Sanity"
    option_off = 0
    option_first_pickup = 1
    option_every = 2
    default = option_off


class ObjectSanityKinds(OptionSet):
    """
    Which kinds of gadget Object Sanity counts. A kind left out is no check, and it leaves the island while
    Object Sanity is on.

    Kinds: walkie_talkie, radio, megaphone, binoculars, laser, torch, scanner, clock, compass, map, cowbell,
    lamp, paintbrush. The paintbrushes are left out by default, and they never leave the island (the Salon needs
    them). A lamp left out stays where it is (puzzles use them). With Light Sanity on, the lamps are never counted
    here: switching them on is their check.
    """

    display_name = "Object Sanity Kinds"
    valid_keys = frozenset(data.OBJECT_KIND_KEYS.values())
    default = frozenset(data.OBJECT_KIND_KEYS.values()) - data.OBJECT_KINDS_LEFT_OUT_BY_DEFAULT


class ObjectSanityItems(Choice):
    """
    What Object Sanity's checks may hold.

    - any: anything, from any world, except the share `object_sanity_junk_percentage` keeps for junk.
    - local: only this world's own items, of those nothing needs (filler, traps, useful ones).
    - junk: only filler, never anything a world needs.
    """

    display_name = "Object Sanity Items"
    option_any = 0
    option_local = 1
    option_junk = 2
    default = option_any


class LightSanity(Choice):
    """
    A check the first time each light of the island is switched on. The puzzles'
    own lights are not counted.

    - off: no check.
    - lamps: the lamps (the ones Object Sanity knows, on their pedestals, in the towers, in the tutorial).
    - lamps_and_switches: the lamps, and the wall switches of the Doof Chamber, the salons, the tutorial, the
      train and its stops.
    """

    display_name = "Light Sanity"
    option_off = 0
    option_lamps = 1
    option_lamps_and_switches = 2
    default = option_off


class LightSanityItems(Choice):
    """
    What Light Sanity's checks may hold.

    - any: anything, from any world, except the share `light_sanity_junk_percentage` keeps for junk.
    - local: only this world's own items, of those nothing needs (filler, traps, useful ones).
    - junk: only filler, never anything a world needs.
    """

    display_name = "Light Sanity Items"
    option_any = 0
    option_local = 1
    option_junk = 2
    default = option_any


class ObjectSanityJunkPercentage(Range):
    """
    With `object_sanity_items: any`, the share of Object Sanity's checks, drawn at random, that hold only junk
    (filler or a trap). The rest may hold anything. 100 is `junk`; 0 lets every check hold anything.
    """

    display_name = "Object Sanity Junk Percentage"
    range_start = 0
    range_end = 100
    default = 75


class LightSanityJunkPercentage(Range):
    """
    With `light_sanity_items: any`, the share of Light Sanity's checks, drawn at random, that hold only junk
    (filler or a trap). The rest may hold anything. 100 is `junk`; 0 lets every check hold anything.
    """

    display_name = "Light Sanity Junk Percentage"
    range_start = 0
    range_end = 100
    default = 75


class ZoneLocks(Toggle):
    """
    The island's lamps and gadgets locked by zone, the zone of the nearest of the four coloured towers: 8 items,
    `Red Zone Lamps`, `Red Zone Gadgets` and so on for green, blue and yellow. Only with Object Sanity or Light
    Sanity on, for what they count.

    - Gadgets: a zone's gadgets are off the map until its item arrives.
    - Lamps and light switches: they stay usable, but they are out of logic until the item arrives (lighting one
      early still sends its check).

    The tutorial's, the Map Room's, the chairlift's, the tunnels', the Big Wall's and the train's keep the lock
    they have.
    """

    display_name = "Zone Locks"


class StartWithZoneLocks(OptionSet):
    """
    With Zone Locks on, the zones you already have.

    Keys: red_lamps, red_gadgets, green_lamps, green_gadgets, blue_lamps, blue_gadgets, yellow_lamps,
    yellow_gadgets.
    """

    display_name = "Start With Zone Locks"
    valid_keys = data.ZONE_LOCK_KEYS
    default = frozenset()


class ZoneLocksLocal(Toggle):
    """
    With Zone Locks on, keep the zone items in your own world.
    """

    display_name = "Zone Locks Local"


class TunnelSanity(Toggle):
    """
    The tunnels open door by door: the Tunnels item gives way to 5 items, `Red Tunnel Door`, `Green Tunnel Door`,
    `Blue Tunnel Door`, `Yellow Tunnel Door` and `Purple Tunnel`.

    - Each colored door opens the tunnels' outlet by its tower; you get in through any door you have.
    - The Purple Tunnel opens the purple hole past the chairlift, the way to the seven purple puzzles.

    Off with Linear Keys, where the Yellow key opens every door at once, as in the game.
    """

    display_name = "Tunnel Sanity"


class FlareGunSanity(Choice):
    """
    The island's four flare guns as checks.

    - on: picking up each one for the first time is a check (4 locations). The four flare guns are
      then items in the pool (how many of each: `island_object_limits`; with `random_colors`, painted
      from the seed's colors).
    - junk: the same, the four checks holding only filler or a trap.
    - off: the flare guns stay on the island and none is an item.

    The Rainbow Flare Gun is an item either way (`island_object_limits`).
    """

    display_name = "Flare Gun Sanity"
    option_off = 0
    option_on = 1
    option_junk = 2
    # Old toggle values still read.
    alias_true = option_on
    alias_false = option_off
    default = option_on


class TowerSanity(DefaultOnToggle):
    """
    A check for reaching each of the seven towers: the Red, Green, Blue, Yellow and Black ones when the tower's
    opening button is pressed (the two buttons at the top of the Black Tower held together), the drawbridge and
    the Green Dome when a player gets there.
    """

    display_name = "Tower Sanity"


class ShuffleRadioMusic(DefaultOnToggle):
    """
    If on, each station's music is an item, and a station stays silent until
    its music is found.

    - On: switching a station on is still a check, but its music only plays
      once its Radio Music item arrives. 7 items, replacing filler.
    - Off: vanilla, a station plays as soon as it is switched on.
    """

    display_name = "Shuffle Radio Music"


class MonumentSanity(DefaultOnToggle):
    """
    A check for each monument filled to its last slot: the Drawbridge's, the five towers' and the Green Dome's.
    Linear Towers all_deposit_boxes puts each tower's key there; with one_deposit_box the key has a check of its own
    (the "Monument Key"), and these stay free for anything. On by default.
    """

    display_name = "Monument Sanity"


class CutKeySanity(Choice):
    """
    The 25 cuts of the big keys.

    - on: each cut is a check.
    - junk: each cut is a check that holds only filler.
    - off: no check for the cuts.
    """

    display_name = "Cut Key Sanity"
    option_on = 0
    option_junk = 1
    option_off = 2
    default = option_on
