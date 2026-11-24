"""Colors, names and custom tiles options."""

from __future__ import annotations

from Options import Choice, DefaultOnToggle, FreeText, OptionCounter, OptionSet, Range, Toggle

from .. import tiles


class GourdName(FreeText):
    """
    What Big Walk calls the gourds on screen for the run (24 characters max): the item feed, the goal
    line and the hub board, on every player's screen. The Gourd Carton follows: "<name> Carton".
    Archipelago, the text client and the other games keep calling the item "Gourd".
    Letters, digits and spaces, with ' - _ . , ! ? & + : ; ( ) @; anything else is dropped.
    """

    display_name = "Gourd Name"
    default = "Gourd"


class RandomColors(DefaultOnToggle):
    """
    The island's buoys (their body, halo and light), flare guns (and their shots), lasers (one color per
    laser, its beam included), gadgets (one color per object: binoculars, clocks, compasses, cowbells, radios,
    megaphones, torches and their light, scanners, walkie-talkies) and the firework launchers away from the towers (and their bursts) take colors
    drawn for the seed, between five and ten of them, the same on every player's screen. It paints objects of the map only:
    never the players, the gourds, the keys, or what a puzzle needs. Off: the game's own colors.
    The Rainbow Flare Gun, whose shots run through every color, is not affected by it.
    """

    display_name = "Random Colors"


class CustomTiles(Choice):
    """
    The nine basic peg tiles (the triangle, the asterisk, the G...) drawn for the seed among the game's own and the
    community's drawings (CC BY 4.0, by Jack5, Don Chalant, GameFraek and Kensufox). A tile only changes what it shows:
    every puzzle, every display and every player's screen follows, and the puzzles ask for the same tiles.

    - off: the game's tiles.
    - matching_games: the drawings of the games in the multiworld (Celeste, Zelda, Minecraft...) and Archipelago's own
      (pelly, in every multiworld) join the game's tiles, and nine are drawn among them.
    - matching_first: the same drawings, every one shown first (nine drawn among them if more); the game's tiles
      take the places left.
    - all: nine drawn among every tile.
    - custom_only: nine drawn among the drawings alone: none of the game's basic tiles stays.

    How often each one is drawn: Custom Tile Weights; tiles you want for sure: Custom Tile Choices. Your own
    drawings, from the host's folder: CUSTOM-TILES.md in the mod's repository.
    """

    display_name = "Custom Tiles"
    option_off = 0
    option_matching_games = 1
    option_all = 2
    option_custom_only = 3
    option_matching_first = 4
    default = option_matching_games


class CustomDrawings(Toggle):
    """
    The community's scribbles for the drawing panels (the Poet's and the Pontiff's puzzles): each one takes the place
    of one of the game's 45 scribbles, drawn for the seed. Like the custom tiles, only what a tile shows changes.
    None are in this version yet: until some are added, on and off play alike. How to add one: the README of the
    mod's mod/assets/tiles folder. Your own scribbles, from the host's folder: CUSTOM-TILES.md in the mod's repository.
    """

    display_name = "Custom Drawings"


class MirroredPoses(Range):
    """
    The pose panels (Indoor Semaphore, Optical Telegraph, Charades Rooms) drawn for the seed among the game's nine poses
    and the mirrors of the four that are not symmetric: one arm up and one level, one up and one down, and so on, the
    other way round. A mirror can be there beside its own pose: tell left from right. Only what a panel shows changes;
    the puzzles work as before.

    How often each mirror is drawn, against a game pose (100 = as often). 0, the default: the game's poses.
    """

    display_name = "Mirrored Poses"
    range_start = 0
    range_end = 100
    default = 0


class CustomTileChoices(OptionSet):
    """
    With Custom Tiles on, the tiles you want for sure, whatever the mode and the weights: nine at most (with more,
    nine are drawn among them). The rest is drawn as usual. Names: see Custom Tile Weights. For example
    [pokeball, heart, cat].
    """

    display_name = "Custom Tile Choices"
    valid_keys = frozenset(tiles.ALL_TILES)
    default = frozenset()


class CustomTileWeights(OptionCounter):
    """
    With Custom Tiles, how often each tile is drawn, against the others. 0 leaves a tile out (a basic tile left out
    still shows where fewer than nine remain to draw from).

    The game's tiles: triangle, asterisk, diagonal_line, G, dot_and_line, vertical_ellipsis, S, spiral, e.
    The drawings: pelly, jack, strawberry, triforce, semibot, star, smile, five, arrow, sword_in_stone, lodo,
    stones_above_banner, pokeball, tree, creeper, sigil, chicken, planets, monster, joker, rune, knight,
    blob_with_legs, gear, hat, heart, tunic, portal, cat.
    """

    display_name = "Custom Tile Weights"
    valid_keys = frozenset(tiles.ALL_TILES)
    min = 0
    max = 100
    default = {key: 1 for key in tiles.ALL_TILES}
