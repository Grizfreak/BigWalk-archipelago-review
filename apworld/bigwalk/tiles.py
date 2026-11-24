"""Custom tiles drawn for the seed: basic peg tiles, drawing panels and pose panels."""

from __future__ import annotations

from random import Random
from typing import Iterable, Mapping, NamedTuple


class CustomTile(NamedTuple):
    key: str
    """Its file in mod/assets/tiles and its key in `custom_tile_weights`."""
    games: tuple[str, ...]
    """Shown when a game in the multiworld has one of these in its name; with none, always."""
    artist: str


class CustomDrawing(NamedTuple):
    key: str
    """Its file in mod/assets/tiles/drawings; file names are unique across categories."""
    artist: str


VANILLA_TILES: tuple[str, ...] = (
    "triangle", "asterisk", "diagonal_line", "G", "dot_and_line", "vertical_ellipsis", "S", "spiral", "e",
)
"""PegTileA1 to PegTileA9, by the community document's names."""

CUSTOM_TILES: tuple[CustomTile, ...] = (
    CustomTile("pelly", (), "Jack5"),
    CustomTile("jack", ("The Witness",), "Jack5"),
    CustomTile("strawberry", ("Celeste",), "Jack5"),
    CustomTile("triforce", ("Zelda", "Link to the Past", "Links Awakening", "Link's Awakening", "Ocarina of Time",
                            "Majora's Mask", "Wind Waker", "Minish Cap", "Twilight Princess", "Skyward Sword",
                            "SMZ3"), "Jack5"),
    CustomTile("semibot", ("R.E.P.O.",), "Jack5"),
    CustomTile("star", ("Mario", "Luigi", "Yoshi"), "Jack5"),
    CustomTile("smile", ("Lingo",), "Jack5"),
    CustomTile("five", ("Dice",), "Jack5"),
    CustomTile("arrow", ("Grapple Dog",), "Jack5"),
    CustomTile("sword_in_stone", ("Dark Souls",), "Don Chalant"),
    CustomTile("lodo", ("StarCraft",), "Don Chalant"),
    CustomTile("stones_above_banner", ("RuneScape",), "Don Chalant"),
    CustomTile("pokeball", ("Pokémon", "Pokemon"), "Don Chalant"),
    CustomTile("tree", ("Terraria",), "Don Chalant"),
    CustomTile("creeper", ("Minecraft",), "Don Chalant"),
    CustomTile("sigil", ("Blasphemous",), "Don Chalant"),
    CustomTile("chicken", ("Stardew Valley",), "Don Chalant"),
    CustomTile("planets", ("Risk of Rain",), "Don Chalant"),
    CustomTile("monster", ("Slay the Spire",), "Don Chalant"),
    CustomTile("joker", ("Balatro",), "Don Chalant"),
    CustomTile("rune", ("DOOM",), "Don Chalant"),
    CustomTile("knight", ("Hollow Knight",), "GameFraek"),
    CustomTile("blob_with_legs", ("Animal Well",), "GameFraek"),
    CustomTile("gear", ("Factorio",), "GameFraek"),
    CustomTile("hat", ("A Hat in Time",), "GameFraek"),
    CustomTile("heart", ("Undertale", "Deltarune"), "GameFraek"),
    CustomTile("tunic", ("Tunic",), "GameFraek"),
    CustomTile("portal", ("Portal",), "GameFraek"),
    CustomTile("cat", ("Rain World",), "Kensufox"),
)

ALL_TILES: tuple[str, ...] = (*VANILLA_TILES, *(tile.key for tile in CUSTOM_TILES))

CUSTOM_DRAWINGS: tuple[CustomDrawing, ...] = ()

DRAWING_PLACES: tuple[str, ...] = (
    *(f"PegTileC{i}" for i in range(1, 10)),
    *(f"PegTilePriest{letter}{i}" for letter in "ABCD" for i in range(1, 10)),
)
"""Drawing panel tile kinds (the mod's PropGroup names): C1..C9 and Priest A1..D9."""


POSE_PLACES: tuple[str, ...] = tuple(f"PegTileD{i}" for i in range(1, 10))
"""Pose panel tile kinds (set D: Indoor Semaphore, Optical Telegraph, Charades)."""

ASYMMETRIC_POSES: tuple[str, ...] = ("PegTileD4", "PegTileD5", "PegTileD6", "PegTileD9")
"""Poses whose mirror is a new pose; the other five are symmetric."""

KEPT_POSES: tuple[str, ...] = ("PegTileD3",)
"""Never replaced: D3 is also drawn on the pulse flashers, which are not pose puzzles."""

POSE_WEIGHT = 100
"""A game pose's weight in the draw, against each mirror's `mirrored_poses` weight."""


def mirror_name(pose: str) -> str:
    """The drawing the mod builds by flipping the pose's cell of the game's atlas."""
    return f"mirror_{pose}"


def draw_poses(random: Random, mirror_weight: int) -> dict[str, str]:
    """Place -> drawing for the pose panels that change; a drawn game pose keeps its place."""
    if mirror_weight <= 0:
        return {}
    weights = {pose: POSE_WEIGHT for pose in POSE_PLACES if pose not in KEPT_POSES}
    weights.update({mirror_name(pose): mirror_weight for pose in ASYMMETRIC_POSES})
    pool = list(weights)
    drawn: list[str] = list(KEPT_POSES)
    while pool and len(drawn) < len(POSE_PLACES):
        pick = random.choices(pool, weights=[weights[key] for key in pool])[0]
        pool.remove(pick)
        drawn.append(pick)

    free = [place for place in POSE_PLACES if place not in drawn]
    random.shuffle(free)
    return dict(zip(free, [key for key in drawn if key not in POSE_PLACES]))


def matching(games: Iterable[str]) -> list[str]:
    """Drawings whose game is in the multiworld (case-insensitive substring), plus generic ones."""
    names = [name.casefold() for name in games]
    return [tile.key for tile in CUSTOM_TILES
            if not tile.games or any(part.casefold() in name for part in tile.games for name in names)]


def draw(random: Random, candidates: Iterable[str], weights: Mapping[str, int],
         chosen: Iterable[str] = (), preferred: Iterable[str] = ()) -> tuple[str, ...]:
    """Drawings for PegTileA1 to A9: chosen ones first, then preferred, then drawn by weight."""
    drawn = list(dict.fromkeys(key for key in chosen if key in ALL_TILES))
    if len(drawn) > len(VANILLA_TILES):
        drawn = random.sample(drawn, len(VANILLA_TILES))
    for keys in (preferred, candidates):
        pool = [key for key in dict.fromkeys(keys) if key in ALL_TILES and weights.get(key, 1) > 0 and key not in drawn]
        while pool and len(drawn) < len(VANILLA_TILES):
            pick = random.choices(pool, weights=[weights.get(key, 1) for key in pool])[0]
            pool.remove(pick)
            drawn.append(pick)

    result = list(VANILLA_TILES)
    free = [i for i, key in enumerate(VANILLA_TILES) if key not in drawn]
    random.shuffle(free)
    for key, place in zip([key for key in drawn if key not in VANILLA_TILES], free):
        result[place] = key
    return tuple(result)


def draw_drawings(random: Random, drawings: Iterable[str]) -> dict[str, str]:
    """Each drawing given replaces a scribble at a random place among DRAWING_PLACES."""
    names = list(dict.fromkeys(drawings))
    random.shuffle(names)
    places = random.sample(DRAWING_PLACES, min(len(names), len(DRAWING_PLACES)))
    return dict(zip(places, names))
