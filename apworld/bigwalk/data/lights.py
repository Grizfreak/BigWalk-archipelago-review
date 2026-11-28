"""Lamps and wall switches, checks of `light_sanity`."""

from __future__ import annotations

from typing import NamedTuple

from .ids import BASE_ID
from .objects import ISLAND_OBJECTS


class Light(NamedTuple):
    """A light whose first switching on is a check with `light_sanity`."""

    key: str
    """A lamp's `savablePropGuid`, or a wall switch's TrackedPeckState path."""
    location_name: str
    zone: str
    """As IslandObject.zone, plus "train" (the train's headlights: the Train item)."""
    switch: bool
    """A wall switch (`lamps_and_switches` only), not a lamp."""


LIGHT_ID_OFFSET = 14_000
"""`light_sanity`: by position in LIGHTS, up to 14_199."""

LIGHT_EVENT_ITEM_NAME = "Light"
"""The event every light holds beside its check, so the logic can count them for `big_lights`."""


LIGHT_SWITCHES: tuple[tuple[str, str, str], ...] = (
    # Wall switches outside the puzzles, as (path, name, zone); the puzzles' own come with `lights`.
    ("LandmarksNonChallenge/DoofChamber/Positioner/FXControls/BasicLightSwitchHouse/LightSwitchPeckLogic",
     "Light Switch (Doof Chamber, House)", "tunnels"),
    ("LandmarksNonChallenge/DoofChamber/Positioner/FXControls/BasicLightSwitchRing/LightSwitchPeckLogic",
     "Light Switch (Doof Chamber, Ring)", "tunnels"),
    ("LandmarksNonChallenge/DoofChamber/Positioner/FXControls/ColouredSpotSwitches/BasicLightSwitchPurple/LightSwitchPeckLogic",
     "Light Switch (Doof Chamber, Purple)", "tunnels"),
    ("LandmarksNonChallenge/DoofChamber/Positioner/FXControls/ColouredSpotSwitches/BasicLightSwitchReds/LightSwitchPeckLogic",
     "Light Switch (Doof Chamber, Reds)", "tunnels"),
    ("LandmarksNonChallenge/DoofChamber/Positioner/FXControls/ColouredSpotSwitches/BasicLightSwitchWhiteSpots/LightSwitchPeckLogic",
     "Light Switch (Doof Chamber, White Spots)", "tunnels"),
    ("LandmarksNonChallenge/Salon/Positioner/SalonBuilding/BasicLightSwitch - Salon Main/LightSwitchPeckLogic",
     "Light Switch (Tutorial Salon, Main)", "overworld"),
    ("LandmarksNonChallenge/Salon/Positioner/SalonBuilding/BasicLightSwitch - Mirror/LightSwitchPeckLogic",
     "Light Switch (Tutorial Salon, Mirror)", "overworld"),
    ("LandmarksNonChallenge/Salon_Secret/Positioner/SalonBuilding/BasicLightSwitch - Salon Main/LightSwitchPeckLogic",
     "Light Switch (Secret Salon, Main)", "tunnels"),
    ("LandmarksNonChallenge/Salon_Secret/Positioner/SalonBuilding/BasicLightSwitch - Mirror/LightSwitchPeckLogic",
     "Light Switch (Secret Salon, Mirror)", "tunnels"),
    ("LandmarksNonChallenge/SpawnCourtyardandHubPlatform/Positioner/TeachingArea/Positioner/ObjectsTeaching/Switch/LightSwitch/BasicLightSwitch/LightSwitchPeckLogic",
     "Light Switch (Tutorial)", "start"),
    # Three trains, each switch on its engine's dashboard, named by the engine's tint.
    ("TrainSystem/TrainNew/TrainCarNew/CarriageBody/CarriageContentsFront/BasicLightSwitch - headlight/LightSwitchPeckLogic",
     "Light Switch (Red Train)", "train"),
    ("TrainSystem/TrainNew/TrainCarNew B(5)/CarriageBody/CarriageContentsFront/BasicLightSwitch - headlight/LightSwitchPeckLogic",
     "Light Switch (Green Train)", "train"),
    ("TrainSystem/TrainNew/TrainCarNew C(10)/CarriageBody/CarriageContentsFront/BasicLightSwitch - headlight/LightSwitchPeckLogic",
     "Light Switch (Yellow Train)", "train"),
    ("TrainSystem/TrainStop - Green Diffuser/Lighting/BasicLightSwitch/LightSwitchPeckLogic",
     "Light Switch (Green Flare Train Stop)", "overworld"),
    ("TrainSystem/TrainStop - Ring Room/Lighting/BasicLightSwitch/LightSwitchPeckLogic",
     "Light Switch (near Green Tower Train Stop)", "overworld"),
    ("TrainSystem/TrainStop - South Valley Beach/Lighting/BasicLightSwitch/LightSwitchPeckLogic",
     "Light Switch (Big Fiesta Beach Train Stop)", "overworld"),
    ("TrainSystem/TrainStop - Sports Valley/Lighting/BasicLightSwitch/LightSwitchPeckLogic",
     "Light Switch (Sports Valley Train Stop)", "overworld"),
    ("TrainSystem/TrainStop - Trap Room/Lighting/BasicLightSwitch/LightSwitchPeckLogic",
     "Light Switch (Trap Room Train Stop)", "overworld"),
    ("TrainSystem/TrainStop - Upper Hub/Lighting/BasicLightSwitch/LightSwitchPeckLogic",
     "Light Switch (Green Dome Train Stop)", "overworld"),
)

LAMP_KINDS: frozenset[str] = frozenset({"BuoyProp", "BuoyRedProp", "BuoyLight"})


LIGHTS: tuple[Light, ...] = (
    *(Light(obj.guid, f"{obj.location_name} Lit", obj.zone, False) for obj in ISLAND_OBJECTS if obj.kind in LAMP_KINDS),
    *(Light(key, f"{name} On", zone, True) for key, name, zone in LIGHT_SWITCHES),
)
"""Every light of `light_sanity`, in id order: the island's lamps, then the wall switches."""


def light_event_name(light: Light) -> str:
    return f"{light.location_name} (event)"


def light_location_id(light: Light) -> int:
    return BASE_ID + LIGHT_ID_OFFSET + LIGHTS.index(light)
