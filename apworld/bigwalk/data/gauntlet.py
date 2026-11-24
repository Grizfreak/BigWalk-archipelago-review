"""The Silent Gauntlet's stages and their ids."""

from __future__ import annotations

from typing import NamedTuple

from .ids import BASE_ID, GAUNTLET_ID_OFFSET
from .puzzles import PUZZLE_NEEDS, PuzzleNeed


GAUNTLET_STAGE_TAGS: tuple[frozenset[str], ...] = (
    frozenset({"coop", "icon_panels", "buttons"}),
    frozenset({"coop", "buttons"}),
    frozenset({"coop", "timed_tomato"}),
    frozenset({"coop", "sync_buttons"}),
    frozenset({"coop", "teapots", "buttons"}),
    frozenset({"coop", "icon_panels", "buttons", "ink_viewer"}),
    frozenset({"coop", "buttons"}),
)
"""The needs of the Silent Gauntlet's seven stages, in order."""


class GauntletStage(NamedTuple):
    index: int
    """0..6: the game's `Level<index>` under `SilentGauntlet 2Player/Positioner`."""
    system_name: str
    """`SavableSystem` enum name, written 0 -> 1 when the stage's puzzle is solved."""
    system_value: int
    """`SavableSystem` enum value; both ids are BASE_ID + GAUNTLET_ID_OFFSET + this."""
    chamber: str
    """The stage's puzzle, in the words of the game's chamber names."""

    @property
    def number(self) -> int:
        return self.index + 1

    @property
    def location_name(self) -> str:
        return f"Gauntlet Stage {self.number}: {self.chamber}"

    @property
    def item_name(self) -> str:
        """Opens the stairway out of this stage, the door `GourdTower_Gate All Hold`."""
        return f"Gauntlet Stage {self.number} Door"


GAUNTLET_STAGES: tuple[GauntletStage, ...] = (
    GauntletStage(0, "GauntletChamber0", 50, "Peg Board"),
    GauntletStage(1, "GauntletChamber1", 51, "Pointers"),
    GauntletStage(2, "GauntletChamber2", 52, "Kick To Kick"),
    GauntletStage(3, "GauntletChamber3", 53, "Sim Press"),
    GauntletStage(4, "GauntletChamber4", 54, "Volley Ball"),
    GauntletStage(5, "GauntletChamber5", 55, "Invisible"),
    GauntletStage(6, "GauntletChamber6", 56, "Sculptures"),
)


PROGRESSIVE_GAUNTLET_ITEM_NAME = "Progressive Gauntlet Door"
PROGRESSIVE_GAUNTLET_VALUE = 60
"""Past the stage values (50..56); each copy opens the lowest stage still shut."""


def progressive_gauntlet_id() -> int:
    return BASE_ID + GAUNTLET_ID_OFFSET + PROGRESSIVE_GAUNTLET_VALUE


def gauntlet_stage_id(stage: GauntletStage) -> int:
    return BASE_ID + GAUNTLET_ID_OFFSET + stage.system_value


def gauntlet_needs_through(stage: GauntletStage) -> tuple[PuzzleNeed, ...]:
    """The needs of this stage and of every stage below it."""
    tags = frozenset().union(*GAUNTLET_STAGE_TAGS[:stage.index + 1])
    return tuple(need for need in PUZZLE_NEEDS if need.key in tags)


def gauntlet_needs_of(stage: GauntletStage) -> tuple[PuzzleNeed, ...]:
    """The needs of this stage alone, for when the stages below it can be skipped."""
    tags = GAUNTLET_STAGE_TAGS[stage.index]
    return tuple(need for need in PUZZLE_NEEDS if need.key in tags)
