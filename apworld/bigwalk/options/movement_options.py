"""Movement locking options."""

from __future__ import annotations

from Options import Choice, OptionSet, Toggle


class LockAbilities(Choice):
    """
    The players' bodies behind items, received by every player at once. Legs: jumping and crouching,
    needed to leave the starting zone. Seat: sitting on a chair or on the floor. A Progressive Arm:
    raising it, then pointing, then extending it sideways. The gesture puzzles ask for them.

    Throwing and Carrying (another player) are items too, and Running with Lock Running.

    - disabled: every ability from the start.
    - handed: three Progressive Left Arm and three Progressive Right Arm.
    - ambidextrous: three Progressive Arms, both arms at once.
    """

    display_name = "Lock Abilities"
    option_disabled = 0
    option_handed = 1
    option_ambidextrous = 2
    default = option_disabled


class StartWithAbilities(OptionSet):
    """
    Which abilities you already have when Lock Abilities is on: their items are handed over at once, and leave the
    pool. Every ability not listed is an item to find.

    Abilities: legs, seat, arms, throwing, carrying. Running has its own option, Lock Running.
    """

    display_name = "Start With Abilities"
    valid_keys = frozenset({"legs", "seat", "arms", "throwing", "carrying"})
    default = frozenset()


class LockRunning(Choice):
    """
    With Lock Abilities on, running can be an item too (Running). No place asks for it: the island is only slower
    to cross until it arrives.

    - off: you run from the start.
    - anywhere: Running is an item, anywhere in the multiworld.
    - early: Running is an item, placed among the first checks anyone can reach (in a solo seed, your own).

    true and false still work: true is anywhere, false is off.
    """

    display_name = "Lock Running"
    option_off = 0
    option_anywhere = 1
    option_early = 2
    alias_false = 0
    alias_true = 1
    default = option_off


class RidesRequireSeat(Toggle):
    """
    With Lock Abilities on, the chairlift and the train are seats like the others: you cannot sit on them before
    Seat arrives, and the logic asks for Seat to use them. Off, they are never locked.
    """

    display_name = "Rides Require Seat"
