"""Item pool, traps and bonuses options."""

from __future__ import annotations

from Options import DefaultOnToggle, OptionCounter, Range, Toggle

from .. import data


class BackpacksInPool(Range):
    """
    How many Backpacks the item pool holds. Each one takes the place of a filler item; a pool
    without that much room holds fewer. None with Pack Sanity vanilla, where each pack is its own check's item.
    """

    display_name = "Backpacks in Pool"
    range_start = 0
    range_end = 8
    default = 2


class BeltsInPool(Range):
    """
    How many Belts the item pool holds. Each one takes the place of a filler item; a pool
    without that much room holds fewer. None with Pack Sanity vanilla, where each pack is its own check's item.
    """

    display_name = "Belts in Pool"
    range_start = 0
    range_end = 8
    default = 2


class GourdCartonsInPool(Range):
    """
    How many Gourd Cartons the item pool holds. Each one takes the place of a filler item; a
    pool without that much room holds fewer. None with Pack Sanity vanilla, where each pack is its own check's item.
    """

    display_name = "Gourd Cartons in Pool"
    range_start = 0
    range_end = 4
    default = 1


class TrapFillPercentage(Range):
    """
    Percentage of filler items replaced by traps. A trap hits every player of the session, for
    `trap_duration` seconds when it lasts. Which traps, and how often each: `trap_weights`.
    """

    display_name = "Trap Fill Percentage"
    range_start = 0
    range_end = 100
    default = 0


class TrapWeights(OptionCounter):
    """
    How often each trap comes, against the others. 0 takes a trap out of the pool.

    - Big Drop: everyone drops what they hold.
    - Big Throw (hard): everyone throws what they hold, after a five-second wind-up.
    - Big Trip (hard): everyone is sent somewhere else on the map, each to a different place.
    - Big Meeting (hard): everyone is sent next to one player drawn at random.
    - Big Night: the clock fast-forwards to midnight, and runs on from there.
    - Big Sleep (hard): for `trap_duration` seconds, everyone falls asleep where they stand for two
      seconds, then wakes for five, again and again.
    - Big Load (hard): every pack worn comes off, and what it holds falls out.
    - Big Mask (hard): for `trap_duration` seconds, everyone wears the blindfold helmet of the
      blindfold puzzles, and nobody can take it off; a player already wearing a mask is spared.
    - Big Flare: a flare gun shot lands on every player, in the Archipelago colors.
      Each player can hide it on their own screen (Settings > Archipelago > Gentle effects).
    - Big Time: every player gets a clock to keep, showing the real time of their own computer.
    - Big Silence: for `trap_duration` seconds, every player is muted, and the mute key cannot lift it.
    - Big Slide: for `trap_duration` seconds, the ground is slippery: starting, stopping and turning
      slide.
    - Big Ghost: for `trap_duration` seconds, every player is white, as in the Silent Gauntlet (the
      color, not the silence).
    - Big Paint: for `trap_duration` seconds, the players swap colors (two swap, more go round).
    - Big Air (hard): a timed tomato ticks in every player's hands, then everyone shoots up into the
      air (stopping under a ceiling).
    - Big Goggles: for `trap_duration` seconds, the field of view narrows.
    - Big Pixels: for `trap_duration` seconds, the island turns to big square pixels.
    - Big Head: for `trap_duration` seconds, the other players' heads grow.
    - Big Brain: for `trap_duration` seconds, the other players' heads are a lit lamp.
    - Big Arms: for `trap_duration` seconds, the players' hands grow (seen on the others).
    - Big Smell: for `trap_duration` seconds, every player gives off a green gas and the players are
      pushed apart.
    - Big Brother: for `trap_duration` seconds, every player is small: body, view and collisions.
    - Big Reset (hard): the puzzle still holding its gourd nearest to each group of players starts
      again (its room's door shuts and opens).
    - Big Break: a folding chair appears under every player, who is sat on it.
    Gentle effects also spares Big Goggles, Big Pixels, Big Smell's gas and Big Brain's light.
    """

    display_name = "Trap Weights"
    valid_keys = frozenset(trap.item_name for trap in data.TRAPS)
    min = 0
    max = 100
    default = {trap.item_name: 10 for trap in data.TRAPS}


class TrapLink(Toggle):
    """
    TrapLink: a trap received in one game plays in every game that has it on (the "TrapLink" tag).

    With it on, every trap of this world's item pool that reaches you is also sent to the other
    games, and a trap sent by another game plays here: as the Big Walk trap of the same name when
    it is one, otherwise as the nearest one by its name (a freeze is Big Sleep, a bomb is Big Flare,
    ...), and as one drawn from the DeathLink roulette (`death_link_trap_weights`) when none comes
    near. Bonuses are never sent. A trap that comes from a DeathLink (`death_link_effect:
    roulette`) or from another game's TrapLink is not sent on, so none loops.
    """

    display_name = "TrapLink"


class HardTraps(DefaultOnToggle):
    """
    Lets the hard traps into the pool: those that cost time or progress (Big Throw, Big Trip,
    Big Meeting, Big Sleep, Big Load, Big Mask, Big Air, Big Reset). Off keeps only the standard ones.
    """

    display_name = "Hard Traps"


class BonusFillPercentage(Range):
    """
    Percentage of filler items replaced by bonuses, a boost for every player. Which bonuses, and how
    often each: `bonus_weights`.
    """

    display_name = "Bonus Fill Percentage"
    range_start = 0
    range_end = 100
    default = 25


class BonusWeights(OptionCounter):
    """
    How often each bonus comes, against the others. 0 takes a bonus out of the pool.

    - Big Speed: everyone walks and runs faster for `trap_duration` seconds.
    - Big Jump: everyone jumps higher for `trap_duration` seconds.
    - Big Day: the clock fast-forwards to noon, and runs on from there.
    - Big Confetti: a burst or two of confetti around every player, with the sound of a puzzle won. Filler, not a
      boost.
    """

    display_name = "Bonus Weights"
    valid_keys = frozenset(bonus.item_name for bonus in data.BONUSES)
    min = 0
    max = 100
    default = {bonus.item_name: 10 for bonus in data.BONUSES}


class IslandObjectLimits(OptionCounter):
    """
    How many of each of the island's objects the filler draws first; 0 leaves one out. Once every
    object is at its limit, the rest of the filler goes on with the objects not left out, the one
    the pool holds fewest of each time (a big pool, with Object or Light Sanity, has room for many).

    - Most objects are random filler, drawn up to their limit. They are props the item feed names and
      that land beside you, with no effect and nothing in logic: none is ever needed.
    - The Walkie-Talkie is one of them, and 8 is how many the island itself has. Lower it, or take
      it to 0, for a pool with fewer of them.
    - The four flare guns (`Flare Gun`, `Blue Flare Gun`, `Green Flare Gun`, `Yellow Flare Gun`) are
      not random filler: with `flare_gun_sanity` the pool holds exactly this many of each, and
      without it none. The `Rainbow Flare Gun` is always in the pool, this many times (0 leaves
      it out).
    - Backpacks, belts and gourd cartons are set apart: `backpacks_in_pool` and the like.
    """

    display_name = "Island Object Limits"
    # "X-Ray Goggles" is the Scanner's old name, still read (items.island_object_limits).
    valid_keys = frozenset(name for name, _ in data.FILLER_ITEMS if name not in ("Backpack", "Belt", "Gourd Carton")) | {
        "X-Ray Goggles"}
    min = 0
    max = 50
    default = {
        "Lamp": 2,
        "Flare Gun": 1,
        "Blue Flare Gun": 1,
        "Green Flare Gun": 1,
        "Yellow Flare Gun": 1,
        "Rainbow Flare Gun": 1,
        "Folding Map": 2,
        "Walkie-Talkie": 8,
        "Laser": 3,
        "Torch": 3,
        "Binoculars": 3,
        "Megaphone": 3,
        "Compass": 3,
        "Radio": 3,
        "Scanner": 3,
    }


class TrapDuration(Range):
    """How long Big Speed, Big Jump, Big Sleep and Big Mask last, in seconds."""

    display_name = "Trap Duration"
    range_start = 10
    range_end = 120
    default = 20


class TrapsSpareTheGauntlet(DefaultOnToggle):
    """
    Big Trip and Big Meeting leave alone the players inside the Silent Gauntlet, and Big Meeting
    never sends anyone into it.
    """

    display_name = "Traps Spare the Gauntlet"
