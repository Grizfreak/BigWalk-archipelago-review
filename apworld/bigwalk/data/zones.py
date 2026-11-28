"""Zone locks by the nearest coloured tower."""

from __future__ import annotations

from typing import NamedTuple

from .ids import BASE_ID


# Zone locks: overworld lamps and gadgets locked by the zone of the nearest coloured tower.
ZONE_LOCK_ID_OFFSET = 14_300
"""By position in ZONE_LOCKS."""


class ZoneLock(NamedTuple):
    key: str
    """The option value (`start_with_zone_locks`): `red_lamps`, `red_gadgets`..."""
    item_name: str
    zone: str
    """The tower's colour, as in OVERWORLD_ZONES."""
    lamps: bool
    """The lamps and the light switches; False: the other gadgets."""


ZONE_COLOURS: tuple[tuple[str, str], ...] = (
    ("red", "bigKeyRedZone"), ("green", "bigKeyGreenZone"), ("blue", "bigKeyBlueZone"), ("yellow", "bigKeyYellowZone"),
)
"""The four coloured towers, by colour and big key prop."""

ZONE_LOCKS: tuple[ZoneLock, ...] = tuple(
    ZoneLock(f"{colour}_{kind}", f"{colour.title()} Zone {kind.title()}", colour, kind == "lamps")
    for colour, _ in ZONE_COLOURS for kind in ("lamps", "gadgets"))

ZONE_LOCK_KEYS: frozenset[str] = frozenset(lock.key for lock in ZONE_LOCKS)

# Nearest tower centre (MonumentOrder.Centres) of each entry; fix by hand one on the wrong side.
OVERWORLD_ZONES: dict[str, str] = {
    "ed2f8967-b4c1-43d9-91b5-11679be10bc6": "blue",  # Binoculars (Peak Marker)
    "24d2f9a5-5376-4a7c-b08c-17e9c060855b": "blue",  # Pedestal Lamp (Diffuser Peak)
    "12a7f64b-8175-4857-a371-77dc2989cd68": "blue",  # Pedestal Lamp (near Cannonball and Backpack)
    "59a73457-6368-4639-8ec7-335902797343": "blue",  # Pedestal Lamp (near Coordinates)
    "dab5cfc8-f4d9-44f2-abb0-90bc42b3cade": "blue",  # Pedestal Lamp (Diffuser Peak 2)
    "52b0dfcb-4588-4ce7-a78c-bdb9cb800fe6": "blue",  # Pedestal Lamp (Mountain Top)
    "f84cb1c7-1702-4fa1-9ddb-f4c334041237": "blue",  # Pedestal Lamp (Mountain Top Entrance)
    "cd88c773-6375-42c7-9475-76b288bd1c91": "blue",  # Pedestal Lamp (Mountain Top Plateau)
    "b2f9b7af-39f5-431c-9908-c2ebf6d6453b": "blue",  # Pedestal Lamp (near Yellow Flare)
    "accb5df0-1452-4b84-8607-9f01bd517103": "blue",  # Pedestal Lamp (near Blue Tunnel Entrance)
    "a552b903-066f-45a8-8550-4cbf5652de74": "blue",  # Pedestal Lamp (near Poet and Priest)
    "ec17f547-5b1f-4fcf-828e-f2932dc2f620": "blue",  # Radio (Factory Music Garden)
    "ea8c71ac-810f-4426-84a1-17e0e3b0b81d": "blue",  # Radio (Peak Marker)
    "cee6114c-2dc0-4d02-a393-5686a2f2e9b8": "blue",  # Walkie-Talkie (Back Beach Hill, Yellow)
    "6efb6db4-76c1-4ffa-b249-8badc6d4bf82": "blue",  # Walkie-Talkie (Peak, Blue)
    "a5992995-191a-45b7-aa23-e9a64a4fede7": "blue",  # Walkie-Talkie (Peak, Yellow)
    "44015a0e-20ca-441a-b6e9-bbd28657b5f3": "green",  # Clock (Ring Room Train Stop)
    "e56da5eb-bac5-4c5e-b489-a8de3d204e7c": "green",  # Clock (South Valley Beach Train Stop)
    "f468d4dc-594e-4c62-80d4-3a85d9e25b3e": "green",  # Clock (Trap Room Train Stop)
    "087f1e73-309b-4db2-8e39-dee6c926ad59": "green",  # Pedestal Lamp (near Flare Run Answer)
    "f4819494-6b1d-48ef-823b-4b26d2ceda53": "green",  # Pedestal Lamp (near Optical Telegraph Hint)
    "13956d25-97a1-4b5c-9992-00c187092476": "green",  # Pedestal Lamp (near Musical Holiday 2)
    "982f4dbf-22bb-4553-aa9c-dd6588c19fda": "green",  # Pedestal Lamp (near Optical Telegraph)
    "632fc258-f424-4b88-930a-a62c45044965": "green",  # Pedestal Lamp (near Trap Room)
    "TrainSystem/TrainStop - Ring Room/Lighting/BasicLightSwitch/LightSwitchPeckLogic": "green",
    "TrainSystem/TrainStop - South Valley Beach/Lighting/BasicLightSwitch/LightSwitchPeckLogic": "green",
    "TrainSystem/TrainStop - Trap Room/Lighting/BasicLightSwitch/LightSwitchPeckLogic": "green",
    "7e2f3ba8-2124-4fe9-8707-1621be5a0926": "green",  # Megaphone (Pyramid Holder)
    "16990dcc-ebb8-453e-88fc-6478e3980728": "green",  # Pedestal Lamp (near Yellow Flare 2)
    "b46c590f-7038-4033-b84d-e3fb664e095c": "green",  # Pedestal Lamp (near Yellow Flare 3)
    "d069a848-37f5-40e0-ac18-64516951f481": "green",  # Pedestal Lamp (near Musical Holiday)
    "c7141d1b-96bf-4c2c-8c05-3d2067e64a93": "green",  # near Optical Telegraph Hint 2
    "0e8faaf2-b5fd-4906-aaab-788f8ccbf603": "green",  # Pedestal Lamp (near Perspective Counting)
    "7017169d-96f6-4578-8c1f-eb65f3549e32": "green",  # Pedestal Lamp (near Green Tower Megaphone)
    "d2b218b4-279c-4e3e-9751-53a8cbfade87": "green",  # Pedestal Lamp (near Big Fiesta Beach)
    "87dbbb84-5c58-43a4-96ba-e43c1c7308b9": "green",  # Radio (Greensteps Music Garden)
    "070cf8f2-6824-413e-9c2a-43a1027c24c5": "green",  # Radio (Musical Holiday)
    "00d7ed74-a4d9-4969-b8e2-bf8d4975ac8a": "green",  # Walkie-Talkie (Ocean Treasure, Blue)
    "cd159c6e-88aa-4158-a11f-2790cf581052": "green",  # Walkie-Talkie (Ocean Treasure, Yellow)
    "88942e8e-6fae-4d71-93f0-8500741a82d1": "red",  # Binoculars (Carousel)
    "209a7dc3-9dcb-425e-86c5-3a84aa9bd0ec": "red",  # Clock (Sports Valley Train Stop)
    "a08daa18-2fef-4a49-a062-2baafdf48686": "red",  # Clock (Upper Hub Train Stop)
    "5d10fec3-7a2d-4bfd-be0e-6196152d88f5": "red",  # Pedestal Lamp (Top of Red Tower Walkie-Talkie)
    "f7f2e9ed-14fd-4a54-a918-fc06b444a39e": "red",  # Pedestal Lamp (near Easy Simultaneous Press)
    "cec1412b-4727-48c4-b6a1-5a5e7989af18": "red",  # Pedestal Lamp (near Tutorial Area Firework 3)
    "94876c24-50ba-41cb-8fdd-9bd026f3be32": "red",  # Pedestal Lamp (near Tutorial Area Firework)
    "df814b39-e550-4e17-ad44-e32028ab3c47": "red",  # Pedestal Lamp (near Green Dome 2)
    "17e22f02-4f01-4aa4-83fb-d2871acd97cd": "red",  # Pedestal Lamp (near Red Tower 3)
    "d3e57e13-a91d-492c-bd46-2c59da8bef9e": "red",  # Pedestal Lamp (near Telescope to Box)
    "5ee8fa69-8937-4841-b9a7-ee8a10c3a933": "red",  # Pedestal Lamp (near Teller Window 5)
    "4b9cad35-ab44-4dfe-9e4b-390cb165aca5": "red",  # Pedestal Lamp (near Teller Window 6)
    "LandmarksNonChallenge/Salon/Positioner/SalonBuilding/BasicLightSwitch - Salon Main/LightSwitchPeckLogic": "red",
    "LandmarksNonChallenge/Salon/Positioner/SalonBuilding/BasicLightSwitch - Mirror/LightSwitchPeckLogic": "red",
    "TrainSystem/TrainStop - Sports Valley/Lighting/BasicLightSwitch/LightSwitchPeckLogic": "red",
    "TrainSystem/TrainStop - Upper Hub/Lighting/BasicLightSwitch/LightSwitchPeckLogic": "red",
    "52fedcd9-caa6-4ec6-a285-f73e6ff40f50": "red",  # Paintbrush (Salon)
    "60412245-4b6c-47eb-a4e7-67eec146bf20": "red",  # Pedestal Lamp (near Basketball)
    "d0a1c556-d62f-42df-a36e-019636da7679": "red",  # Pedestal Lamp (near Fielding)
    "5efbf72b-9dff-4251-a536-f625ba8ff315": "red",  # Pedestal Lamp (near Tutorial Exit)
    "fee806b8-8617-49f8-99af-bdea0c148ed0": "red",  # Pedestal Lamp (near Hub Firework)
    "4ec6aca0-9961-4550-9c26-7cacb9917566": "red",  # Pedestal Lamp (near Tutorial Area Firework 2)
    "38722a78-da82-4d6e-80c4-efd70e1be90f": "red",  # Pedestal Lamp (near Pointer's Paradise 3)
    "d368b4c7-8389-4f63-a20f-ba88ef17f0a5": "red",  # Pedestal Lamp (near Pointer's Paradise 2)
    "007a1599-17ad-426f-a68b-37718423e9f7": "red",  # Pedestal Lamp (Hub past Left Arch Door)
    "99d8a99a-89eb-4ad0-8736-104aafe1fdd7": "red",  # Pedestal Lamp (Hub Left Arch Door)
    "fa653354-bdfa-431d-aa19-7453e5730e80": "red",  # Pedestal Lamp (near Green Dome)
    "942347f0-cd23-445f-8019-d296e5c92ead": "red",  # Pedestal Lamp (Hub 4)
    "0abc8402-398d-4f0d-bf5e-fb3051a16e21": "red",  # Pedestal Lamp (near Hub Firework 2)
    "c2756ad3-4f4a-45e5-93a3-4c10e892ebdc": "red",  # Pedestal Lamp (near Teller Window 1)
    "226bdfbc-c2ad-4bda-b9ec-2d72c09f1502": "red",  # Pedestal Lamp (near Teller Window 2)
    "d467b790-8968-46b1-9be9-e36eac9ebe52": "red",  # Pedestal Lamp (near Teller Window 3)
    "2cb8963f-ac2f-4f2a-b720-11a701bc4b08": "red",  # Pedestal Lamp (near Teller Window 4)
    "41e7b481-2e10-4d62-9136-5e6e0ba9fa7d": "red",  # Radio (Lighthouse Music Garden)
    "ebdec567-58c0-48c7-a62a-bbd843bef36f": "red",  # Radio (Upper Hub)
    "044daaf7-fb90-4f73-a9bb-00b8a3df9c60": "red",  # Scanner (Fireworks Platform)
    "28cf2386-5e54-4b7e-99ba-283298aa3816": "red",  # Scanner (Upper Hub)
    "ac52bd4b-990f-4908-999d-5badeb8b6eda": "red",  # Torch (Tutorial Exit)
    "185e3864-0dcb-498b-baee-6da1a4b21a8e": "red",  # Walkie-Talkie (Red Cone Hill, Yellow)
    "f29614f5-0c70-4ed8-b152-9e57b371f20d": "red",  # Walkie-Talkie (Red Cone Valley, Blue)
    "1c9f8bc9-5850-4085-80f6-05c2821b505d": "yellow",  # Clock (Green Diffuser Train Stop)
    "80e38f3e-d054-4187-9675-66d6e70c8356": "yellow",  # Pedestal Lamp (near Egg Hunt)
    "6a673617-992d-49ca-87f9-223aef4c9fe4": "yellow",  # Pedestal Lamp (near Scout Bombs)
    "6beb48a3-60de-41e8-a9f6-d39772bcdfa4": "yellow",  # Pedestal Lamp (near Yellow Tower Megaphone)
    "TrainSystem/TrainStop - Green Diffuser/Lighting/BasicLightSwitch/LightSwitchPeckLogic": "yellow",
    "c6259d07-2499-4063-b033-b72cddd951c2": "yellow",  # Megaphone (Ramp Holder)
    "080554bb-bc16-41cd-bc96-4765cec96beb": "yellow",  # Pedestal Lamp (near Egg Hunt 2)
    "11c1d018-3dbf-46d4-b1de-f9aba4d29fc2": "yellow",  # Pedestal Lamp (near Green Flare)
    "bef6d32e-57a9-4ba1-b984-05fdec08800c": "yellow",  # Pedestal Lamp (near Black Tower)
    "4ee18fc7-2a43-44e0-9a2b-4508e9d2705d": "yellow",  # near Chairlift Yellow Tower Stop
    "c716c86d-81a7-40cc-873f-0405821dbd72": "yellow",  # near Chairlift Yellow Tower Beach
    "cef7ae31-a105-491d-8d0d-daab0253909c": "yellow",  # near Chairlift Yellow Tower Stop 2
    "d8e70c55-9800-498e-af4e-7054093950e4": "yellow",  # Radio (Floorplan Music Garden)
    "56a7313e-9e55-485b-ac52-fcc6f8030133": "yellow",  # Walkie-Talkie (Beach, Blue)
}


def zone_of(key: str, zone: str) -> str | None:
    """The zone colour of a lamp, gadget or switch, or None when no zone lock applies."""
    if zone.startswith("tower:"):
        return next((colour for colour, prop in ZONE_COLOURS if zone == f"tower:{prop}"), None)
    if zone == "overworld":
        return OVERWORLD_ZONES.get(key)
    return None


def zone_lock_for(key: str, zone: str, lamps: bool) -> ZoneLock | None:
    colour = zone_of(key, zone)
    if colour is None:
        return None
    return next(lock for lock in ZONE_LOCKS if lock.zone == colour and lock.lamps == lamps)


def zone_lock_item_id(lock: ZoneLock) -> int:
    return BASE_ID + ZONE_LOCK_ID_OFFSET + ZONE_LOCKS.index(lock)
