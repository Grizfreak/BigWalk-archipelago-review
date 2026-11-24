"""DeathLink options."""

from __future__ import annotations

from Options import Choice, DefaultOnToggle, OptionCounter, OptionSet, Range, Toggle

from .. import data


class BigWalkDeathLink(Choice):
    """
    DeathLink: a death in one game is a death in every game that has it on.

    - off: nothing sent, nothing received. The host can also switch DeathLink off for a session, and
      replace the amnesty, in the game's Settings > Archipelago, whatever the YAML says.
    - send: Big Walk sends one when something in `death_link_triggers` happens, and ignores those it
      receives.
    - receive: Big Walk plays `death_link_effect` when one arrives, and sends none.
    - both: both.
    """

    display_name = "DeathLink"
    option_off = 0
    option_send = 1
    option_receive = 2
    option_both = 3
    default = option_off


class DeathLinkTriggers(OptionSet):
    """
    What sends a DeathLink, with `death_link` on send or both.

    - puzzle_failed: a puzzle failed, as the game itself tells it: a wrong tile validated, buttons
      pressed out of order, a count gone wrong, a timer run out.
    - big_fall: a player falls far enough to land dazed. Not by default. A daze a received
      DeathLink causes (`death_link_effect: knockout`) sends nothing back.
    """

    display_name = "DeathLink Triggers"
    valid_keys = frozenset(data.DEATH_LINK_TRIGGERS)
    default = frozenset({"puzzle_failed"})


class DeathLinkAmnesty(Range):
    """How many triggers (failed puzzles, big falls) are forgiven before one sends a DeathLink: 0 sends on the first."""

    display_name = "DeathLink Amnesty"
    range_start = 0
    range_end = 10
    default = 0


class DeathLinkEffect(Choice):
    """
    What a DeathLink received does, with `death_link` on receive or both.

    - knockout: everyone drops what they hold, takes off their backpacks and belts (their contents
      fall out), and is dazed as after a big fall.
    - drop: everyone drops what they hold.
    - roulette: a trap drawn at random, by `death_link_trap_weights`.
    """

    display_name = "DeathLink Effect"
    option_drop = 0
    option_roulette = 1
    option_knockout = 2
    default = option_knockout


class DeathLinkTrapWeights(OptionCounter):
    """
    The traps the DeathLink roulette draws from, and how often each, against the others; 0 leaves
    one out. Apart from `trap_weights`, which is the item pool's.
    """

    display_name = "DeathLink Trap Weights"
    valid_keys = frozenset(trap.item_name for trap in data.TRAPS)
    min = 0
    max = 100
    default = {trap.item_name: 10 for trap in data.TRAPS}


class DeathLinkTarget(Choice):
    """
    Who a DeathLink received hits, with `death_link` on receive or both.

    - everyone: every player of the session.
    - one_player: one player only, the one hit the fewest times so far this session (ties at
      random), as received items go to the player given the fewest. With `death_link_effect:
      roulette`, the traps that need several players or hit the whole session are left out of the
      draw, whatever `death_link_trap_weights` says: Big Meeting (it gathers everyone around one
      player) and Big Night (the clock is the same for everyone). The other traps play on the
      one player: Big Drop, Big Throw, Big Trip, Big Sleep, Big Load, Big Mask and Big Flare.
    """

    display_name = "DeathLink Target"
    option_everyone = 0
    option_one_player = 1
    default = option_everyone


class DeathLinkTrapOnSend(Toggle):
    """
    With `death_link` on send or both: when Big Walk sends a DeathLink, its own players take the
    effect of a death too (`death_link_effect`, on `death_link_target`). Normally a game only
    receives the link of the others' deaths; this makes the one who dies pay for it as well.
    Nothing happens with `death_link` on receive or off: nothing is sent.
    """

    display_name = "DeathLink Trap on Send"


class DeathLinkRouletteHardTraps(DefaultOnToggle):
    """
    Lets the roulette draw the hard traps (Big Throw, Big Trip, Big Meeting, Big Sleep, Big Load, Big Mask, Big Air,
    Big Reset). Off keeps them out of it.
    """

    display_name = "DeathLink Roulette Hard Traps"
