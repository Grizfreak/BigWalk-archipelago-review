"""Logic options: doors, puzzle needs, the Gauntlet and the vanilla behaviours."""

from __future__ import annotations

from Options import Choice, DefaultOnToggle, OptionSet, Toggle

from .. import data


# Off by default: the mod opens the hub's arch doors, so the drawbridge is not the only way out.
class StartWithDrawbridgeOpen(Toggle):
    """
    If on, you start with the Drawbridge open instead of finding it in the
    multiworld.

    - The hub's arch doors follow Start With Arch Doors Open, not this.
    - The Drawbridge Key Deposit stays a check either way.
    """

    display_name = "Start With Drawbridge Open"


class StartWithMapRoomOpen(Toggle):
    """
    If on, you start with the Map Room open instead of finding it in the multiworld. The Red Funnel
    Tower Key Deposit stays a check either way.
    """

    display_name = "Start With Map Room Open"


class LinearKeys(Toggle):
    """
    If on, a big key placed in its tower opens what it opens in the game (the drawbridge, the Map Room, the
    chairlift, the train, the tunnels, the Big Wall Door, the Spawn Secret Door) instead of that being an item to
    find. The key deposits stay checks; each one holds its own door.
    """

    display_name = "Linear Keys"


class LinearArchDoors(Toggle):
    """
    If on, the hub's three arch doors are as in the game: no items, each opened by its push button on its far side
    (the First Arch Door's outside the hub; the Left one coming from the train or the Green Dome, the Right one from
    the tunnels). The way out of the starting zone is then the Drawbridge. Start With Arch Doors Open and Require
    Arch Doors have nothing to do.
    """

    display_name = "Linear Arch Doors"


class LinearTowers(Choice):
    """
    Whether a tower's monument gives its key, as in the game, instead of the key being an item to find. The key
    still arrives as an item, beside the player.

    - off: the keys are items of the multiworld.
    - all_deposit_boxes: the key comes the moment the monument is full. The "Monument Full" checks exist with it,
      Monument Sanity or not.
    - one_deposit_box: the key comes once the monument holds a number of gourds drawn for each tower, between one
      and full (the "Monument Key" location).
    """

    display_name = "Linear Towers"
    option_off = 0
    option_all_deposit_boxes = 1
    option_one_deposit_box = 2
    # Old toggle values still read; `disabled` is the community document's word.
    alias_true = option_all_deposit_boxes
    alias_on = option_all_deposit_boxes
    alias_false = option_off
    alias_disabled = option_off
    default = option_off


class LinearPuzzles(Toggle):
    """
    If on, every puzzle holds a gourd, as in the game, instead of anything from the multiworld (not the seven purple
    ones past the chairlift, which stay checks like any other). The
    gourd still arrives as an item, beside the player.
    """

    display_name = "Linear Puzzles"


class IncludeBigGamePuzzles(Choice):
    """
    The seven purple gourds' puzzles, past the chairlift.

    - from_start: checks, and shown on the map from the start.
    - when_unlocked: checks, but as in the game: not on the map, and in logic only once the ending is reached.
    - disabled: not checks at all.
    """

    display_name = "Include Big Game Puzzles"
    option_from_start = 0
    option_when_unlocked = 1
    option_disabled = 2
    default = option_from_start


# The first door is the starting zone's other way out; the two far ones are only shortcuts.
class StartWithArchDoorsOpen(OptionSet):
    """
    The hub's arch doors open from the start; the others are items to find.

    - First Arch Door: closed, it makes a real early game.
    - Left and Right Arch Doors: shortcuts; the island stays reachable without them.
    """

    display_name = "Start With Arch Doors Open"
    valid_keys = frozenset(door.item_name for door in data.ARCH_DOORS)
    default = frozenset({data.FIRST_ARCH_DOOR.item_name})


class RequireArchDoors(DefaultOnToggle):
    """
    What lies past a closed Left or Right Arch Door needs that door in logic, so you are never
    sent the shortcut after the walk.

    - Left: the Yellow, Blue and Black towers, the Green Dome. Never the chapel: the end of the
      game needs only the Big Wall Door.
    - Right: the Green Tower, past the chairlift, the purple gourds.

    The doors are shortcuts, not walls: the island can be walked round them. But until a door
    arrives, what lies past it is out of logic: its checks are never expected of you, and a
    tracker shows them, and the goal, as out of reach even once you have walked there.
    """

    display_name = "Require Arch Doors"


class LockPuzzleNeeds(DefaultOnToggle):
    """
    What puzzles are built from (buttons, panels, speakers, timers...) are items to find first.
    The parts missing are hidden in the world; Start With Puzzle Needs lists those you already have.
    """

    display_name = "Lock Puzzle Needs"


class StartWithPuzzleNeeds(OptionSet):
    """
    Which puzzle needs you already have when Lock Puzzle Needs is on. Every
    need not listed is an item to find.

    Needs: buttons, sync_buttons, icon_panels, drawing_panels, pose_panels,
    sound_panels, point_panels, speakers, lights, teapots, timed_tomato,
    golf_ball, big_head, ink_viewer, counter, coordinates_computer, eggs.
    """

    display_name = "Start With Puzzle Needs"
    valid_keys = data.PUZZLE_NEED_KEYS
    default = frozenset()


class StartWithRandomPuzzleNeed(DefaultOnToggle):
    """
    With Lock Puzzle Needs on, start with one random need, so that at least
    one of the tutorial's puzzles is doable from the first minute. Nothing is
    added if Start With Puzzle Needs already allows one.

    The tutorial's puzzles need buttons or synchronized buttons, so it is one
    of those two. Turning this off is only possible when Start With Puzzle
    Needs lists one of them: a seed with nothing to do at the start cannot be
    generated.
    """

    display_name = "Start With Random Puzzle Need"


class GauntletMode(Choice):
    """
    What the Silent Gauntlet is, behind the Big Wall.

    - vanilla: nothing changes, and it has no checks. Holding the stairway
      buttons together opens the way up, as in the game.
    - locked_stages: the stairway buttons are gone and each stage's way up is
      an item. Solving a stage's puzzle is a check and still opens the wall
      inside the stage, but you only leave it with that stage's item. 7 checks,
      7 items.

    Not with the goal big_wall, which ends the run before the Gauntlet. With Big
    Goodbye the seven items are needed to win. With big_game the Gauntlet is extra: its checks sit behind the chapel, so with
    the usual accessibility (full) the Black Monolith Key and the seven items
    are always findable, but the goal itself asks for none of them.
    """

    display_name = "Gauntlet Mode"
    option_vanilla = 0
    option_locked_stages = 1
    default = option_vanilla


class GauntletPuzzlesRequired(DefaultOnToggle):
    """
    With Gauntlet Mode locked_stages: whether a stage's puzzle has to be solved
    to leave the stage.

    - On: solving the puzzle opens the wall inside the stage and is a check;
      the stage's item opens the stairway. You leave a stage by doing both.
    - Off: the stage's item opens the wall and the stairway at once. You may
      do the puzzles in any order, or none: with all seven items you can walk
      straight to the top. The puzzles are still checks.
    """

    display_name = "Gauntlet Puzzles Required"


class LockGauntletNeeds(DefaultOnToggle):
    """
    With Lock Puzzle Needs: whether the parts of the seven stages' puzzles in the
    Silent Gauntlet are items too, in either Gauntlet Mode.

    - On: the buttons, panels and so on inside the Gauntlet's stages are not on
      the map until their item arrives, and the Big Goodbye goal, which means
      finishing the Gauntlet, asks for them. With Gauntlet Mode locked_stages the
      stages' checks ask for them too. The finale and the entrance are left alone.
    - Off: the Gauntlet's parts stay on the map and nothing is asked for them.
    """

    display_name = "Lock Gauntlet Needs"


class GauntletStageItems(Choice):
    """
    With Gauntlet Mode locked_stages, what the seven stage items are.

    - progressive: seven Progressive Gauntlet Doors; each one opens the next stage up.
    - individual: one item per stage, Gauntlet Stage 1 Door to Stage 7 Door, found in any order.
    """

    display_name = "Gauntlet Stage Items"
    option_progressive = 0
    option_individual = 1
    default = option_progressive


class OpenBlackTower(DefaultOnToggle):
    """
    The Black Tower's door is open from the start (the default). Off: it opens as in the game, once the monuments of
    the drawbridge and the four coloured towers are full, or with Tower Mode in_order or by_item at the Black Tower's
    turn; its button and its monument wait for it in logic.
    """

    display_name = "Open Black Tower"
