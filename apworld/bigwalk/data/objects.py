"""The island's gadgets, checks of `object_sanity`."""

from __future__ import annotations

from typing import NamedTuple

from .ids import BASE_ID


class IslandObject(NamedTuple):
    """A gadget of the island whose pick-up is a check with `object_sanity`."""

    guid: str
    """The prop's `savablePropGuid`, the same on every machine."""
    kind: str
    """The prop's name in the game, which the mod matches a pick-up by (`first_pickup`)."""
    location_name: str
    zone: str
    """start, overworld, chairlift, purple, tunnels, big_wall, map_room or tower:<big key prop>."""


OBJECT_ID_OFFSET = 13_500
"""`object_sanity: every`: by position in ISLAND_OBJECTS."""
OBJECT_KIND_ID_OFFSET = 13_700
"""`object_sanity: first_pickup`: by position in OBJECT_KINDS."""

ISLAND_OBJECTS: tuple[IslandObject, ...] = (
    # Packs, flare guns and the puzzles' own tools have their own checks or none.
    IslandObject("6364d493-5c48-4480-a02d-ae086ef06825", "BinocularsProp", "Binoculars (Blue Castle Tower)", "tower:bigKeyBlueZone"),
    IslandObject("88942e8e-6fae-4d71-93f0-8500741a82d1", "BinocularsProp", "Binoculars (Carousel)", "overworld"),
    IslandObject("16e1e4a2-aadf-43e1-a67b-f2513dd63303", "BinocularsProp", "Binoculars (Green Cup Tower)", "tower:bigKeyGreenZone"),
    IslandObject("ed2f8967-b4c1-43d9-91b5-11679be10bc6", "BinocularsProp", "Binoculars (Peak Marker)", "overworld"),
    IslandObject("f40709b7-b15b-4131-ab88-ae08571224ac", "BinocularsProp", "Binoculars (Red Funnel Tower)", "tower:bigKeyRedZone"),
    IslandObject("eed83af3-1958-4718-a634-5280bdc6b3cb", "BinocularsProp", "Binoculars (South Peak)", "chairlift"),
    IslandObject("6debc10d-8e02-40d5-a5be-bfc992b89a9a", "BinocularsProp", "Binoculars (Yellow Twist Tower)", "tower:bigKeyYellowZone"),
    IslandObject("1c9f8bc9-5850-4085-80f6-05c2821b505d", "ClockProp", "Clock (Green Diffuser Train Stop)", "overworld"),
    IslandObject("44015a0e-20ca-441a-b6e9-bbd28657b5f3", "ClockProp", "Clock (Ring Room Train Stop)", "overworld"),
    IslandObject("e56da5eb-bac5-4c5e-b489-a8de3d204e7c", "ClockProp", "Clock (South Valley Beach Train Stop)", "overworld"),
    IslandObject("209a7dc3-9dcb-425e-86c5-3a84aa9bd0ec", "ClockProp", "Clock (Sports Valley Train Stop)", "overworld"),
    IslandObject("f468d4dc-594e-4c62-80d4-3a85d9e25b3e", "ClockProp", "Clock (Trap Room Train Stop)", "overworld"),
    IslandObject("a08daa18-2fef-4a49-a062-2baafdf48686", "ClockProp", "Clock (Upper Hub Train Stop)", "overworld"),
    IslandObject("d793e0fe-a145-496f-a348-2d8a65add9e8", "CompassProp", "Compass (Map Room)", "map_room"),
    IslandObject("672cd949-1e5b-465a-a1cb-3860594f9e9f", "FoldingMapProp", "Folding Map (Map Room)", "map_room"),
    IslandObject("cf0fd97f-0f39-4b0c-9474-54c665aeb2e9", "CowBellLargeProp", "Large Cowbell (Tutorial)", "start"),
    IslandObject("f68a6dbc-0b73-47b6-b9e6-fd28e016606e", "LaserProp", "Laser (Blue Castle Tower)", "tower:bigKeyBlueZone"),
    IslandObject("b2913ee0-2c3f-4582-a2f5-6c9f33b40b3b", "LaserProp", "Laser (Green Cup Tower)", "tower:bigKeyGreenZone"),
    IslandObject("c7ba38c6-f0fe-45fd-ad4a-6d1907b408ae", "LaserProp", "Laser (Map Room 1)", "map_room"),
    IslandObject("cb8326bc-26c2-4214-8dc4-3ebafe0b5603", "LaserProp", "Laser (Map Room 2)", "map_room"),
    IslandObject("7c788690-d373-4b31-9be9-31bbb3ec1f88", "LaserProp", "Laser (Map Room 3)", "map_room"),
    IslandObject("8fbe1de2-1ea0-4da4-8ef5-282f5d19b492", "LaserProp", "Laser (Red Funnel Tower)", "tower:bigKeyRedZone"),
    IslandObject("e69463b0-e27c-4247-93e4-50df64cd12ad", "LaserProp", "Laser (Yellow Twist Tower)", "tower:bigKeyYellowZone"),
    IslandObject("a418a7cd-0945-44c1-9f7b-14eac4584cf1", "CowBellMediumProp", "Medium Cowbell (Tutorial)", "start"),
    IslandObject("7e2f3ba8-2124-4fe9-8707-1621be5a0926", "MegaphoneProp", "Megaphone (Pyramid Holder)", "overworld"),
    IslandObject("c6259d07-2499-4063-b033-b72cddd951c2", "MegaphoneProp", "Megaphone (Ramp Holder)", "overworld"),
    IslandObject("48da7ac6-988f-4f4b-a376-3686fe550f69", "MegaphoneProp", "Megaphone (Spiral Holder)", "chairlift"),
    IslandObject("2835fac9-81b1-4312-a576-cc977e6e8c85", "FmRadioProp", "Radio (Abbey Music Garden)", "chairlift"),
    IslandObject("21ab205e-db90-452b-b620-8f64ebcabd63", "FmRadioProp", "Radio (Doof Chamber)", "tunnels"),
    IslandObject("ec17f547-5b1f-4fcf-828e-f2932dc2f620", "FmRadioProp", "Radio (Factory Music Garden)", "overworld"),
    IslandObject("d8e70c55-9800-498e-af4e-7054093950e4", "FmRadioProp", "Radio (Floorplan Music Garden)", "overworld"),
    IslandObject("87dbbb84-5c58-43a4-96ba-e43c1c7308b9", "FmRadioProp", "Radio (Greensteps Music Garden)", "overworld"),
    IslandObject("41e7b481-2e10-4d62-9136-5e6e0ba9fa7d", "FmRadioProp", "Radio (Lighthouse Music Garden)", "overworld"),
    IslandObject("070cf8f2-6824-413e-9c2a-43a1027c24c5", "FmRadioProp", "Radio (Musical Holiday)", "overworld"),
    IslandObject("ea8c71ac-810f-4426-84a1-17e0e3b0b81d", "FmRadioProp", "Radio (Peak Marker)", "overworld"),
    IslandObject("ebdec567-58c0-48c7-a62a-bbd843bef36f", "FmRadioProp", "Radio (Upper Hub)", "overworld"),
    IslandObject("b4dbfae3-89e9-45c0-9dd0-3fd815cd3904", "XrayGogglesProp", "Scanner (Blue Castle Tower)", "tower:bigKeyBlueZone"),
    IslandObject("044daaf7-fb90-4f73-a9bb-00b8a3df9c60", "XrayGogglesProp", "Scanner (Fireworks Platform)", "overworld"),
    IslandObject("ed7b657e-5949-4a1e-8b82-e50ad2a8d785", "XrayGogglesProp", "Scanner (Green Cup Tower)", "tower:bigKeyGreenZone"),
    IslandObject("0132c70b-9a0c-4b5a-be06-b9bb077a6e5a", "XrayGogglesProp", "Scanner (Red Funnel Tower)", "tower:bigKeyRedZone"),
    IslandObject("c420401c-ba23-4e0c-aeac-2ea3ebcafa26", "XrayGogglesProp", "Scanner (South Peak)", "chairlift"),
    IslandObject("28cf2386-5e54-4b7e-99ba-283298aa3816", "XrayGogglesProp", "Scanner (Upper Hub)", "overworld"),
    IslandObject("9fadd6c5-af79-437f-80dd-ebb74d8526d4", "XrayGogglesProp", "Scanner (Yellow Twist Tower)", "tower:bigKeyYellowZone"),
    IslandObject("307d526d-780f-4fe2-b3fd-95f74b86319c", "CowBellSmallProp", "Small Cowbell (Tutorial)", "start"),
    IslandObject("381abbdb-da55-4243-a5df-692acd3fb2c0", "TorchProp", "Torch (Blue Castle Tower)", "tower:bigKeyBlueZone"),
    IslandObject("cf899912-898f-4486-86ae-91ed78025a9d", "TorchProp", "Torch (Blue Tunnel Outlet)", "tunnels"),
    IslandObject("b25443e0-ec2d-4b55-b8e9-01ac48604ba5", "TorchProp", "Torch (Green Cup Tower)", "tower:bigKeyGreenZone"),
    IslandObject("57632bfc-688b-40e4-aabd-d020f489b291", "TorchProp", "Torch (Green Tunnel Outlet)", "tunnels"),
    IslandObject("d6270f5f-1ab7-4526-9e64-cccebde9389d", "TorchProp", "Torch (Red Funnel Tower)", "tower:bigKeyRedZone"),
    IslandObject("cbbf5ba7-0026-4737-9547-2e69187a2845", "TorchProp", "Torch (Red Tunnel Outlet)", "tunnels"),
    IslandObject("ac52bd4b-990f-4908-999d-5badeb8b6eda", "TorchProp", "Torch (Tutorial Exit)", "overworld"),
    IslandObject("56a5d535-d1a6-4ada-a1b3-cf6951bb2e66", "TorchProp", "Torch (Yellow Tunnel Outlet)", "tunnels"),
    IslandObject("b459af97-5a07-4024-8950-02f752c5292b", "TorchProp", "Torch (Yellow Twist Tower)", "tower:bigKeyYellowZone"),
    IslandObject("cee6114c-2dc0-4d02-a393-5686a2f2e9b8", "WalkieTalkieProp", "Walkie-Talkie (Back Beach Hill, Yellow)", "overworld"),
    IslandObject("56a7313e-9e55-485b-ac52-fcc6f8030133", "WalkieTalkieProp", "Walkie-Talkie (Beach, Blue)", "overworld"),
    IslandObject("cd159c6e-88aa-4158-a11f-2790cf581052", "WalkieTalkieProp", "Walkie-Talkie (Ocean Treasure, Yellow)", "overworld"),
    IslandObject("00d7ed74-a4d9-4969-b8e2-bf8d4975ac8a", "WalkieTalkieProp", "Walkie-Talkie (Ocean Treasure, Blue)", "overworld"),
    IslandObject("6efb6db4-76c1-4ffa-b249-8badc6d4bf82", "WalkieTalkieProp", "Walkie-Talkie (Peak, Blue)", "overworld"),
    IslandObject("a5992995-191a-45b7-aa23-e9a64a4fede7", "WalkieTalkieProp", "Walkie-Talkie (Peak, Yellow)", "overworld"),
    IslandObject("185e3864-0dcb-498b-baee-6da1a4b21a8e", "WalkieTalkieProp", "Walkie-Talkie (Red Cone Hill, Yellow)", "overworld"),
    IslandObject("f29614f5-0c70-4ed8-b152-9e57b371f20d", "WalkieTalkieProp", "Walkie-Talkie (Red Cone Valley, Blue)", "overworld"),
    # Paintbrushes are left out by default and never taken off the island: the Salon needs them.
    IslandObject("52fedcd9-caa6-4ec6-a285-f73e6ff40f50", "SalonBrushProp", "Paintbrush (Salon)", "overworld"),
    IslandObject("64d92f9a-e44c-4218-ab98-4ea48d0c9b33", "SalonBrushProp", "Paintbrush (Secret Salon)", "tunnels"),
    # Lamps outside the puzzles, each placed in the zone of the puzzle it stands nearest to.
    IslandObject("1cdcd9a1-0343-4234-b1d9-7be646dec4ae", "BuoyProp", "Pedestal Lamp (Blue Castle Tower 2)", "tower:bigKeyBlueZone"),
    IslandObject("24d2f9a5-5376-4a7c-b08c-17e9c060855b", "BuoyProp", "Pedestal Lamp (Diffuser Peak)", "overworld"),
    IslandObject("fcda7d44-81af-4526-be44-f16a90e5b0ec", "BuoyProp", "Pedestal Lamp (Green Cup Tower)", "tower:bigKeyGreenZone"),
    IslandObject("98c2cd14-bbcf-4066-b58c-d7d23d2757f1", "BuoyProp", "Pedestal Lamp (near Purple Tunnel Exit 2)", "purple"),
    IslandObject("d252953b-ebd1-484a-a555-953bccb904c0", "BuoyProp", "Pedestal Lamp (near Purple Peak Firework 2)", "chairlift"),
    IslandObject("2141d27e-647c-401f-9a0c-a3c1aa029434", "BuoyProp", "Lamp (Tutorial 1)", "start"),
    IslandObject("0cb88fcc-6d33-4b08-9e5e-124a34f7f15b", "BuoyProp", "Lamp (Tutorial 2)", "start"),
    IslandObject("0460483f-1636-4ee6-8635-2168a9acaf34", "BuoyProp", "Pedestal Lamp (Tutorial Reunion)", "start"),
    IslandObject("4df70c57-8d1b-447e-bad5-5aa66c1169f4", "BuoyProp", "Pedestal Lamp (Hub)", "overworld"),
    IslandObject("1ff0231c-4933-4bec-b878-4e6f1fe88856", "BuoyProp", "Lamp (Yellow Twist Tower)", "tower:bigKeyYellowZone"),
    IslandObject("c5e792c4-080b-4377-8d68-f09c00debc91", "BuoyProp", "Pedestal Lamp (near Big Fiesta Beach 2)", "overworld"),
    IslandObject("b5461e87-1a09-4f2c-b9b5-158f195adbf3", "BuoyProp", "Pedestal Lamp (near Chairlift Purple Area)", "chairlift"),
    IslandObject("5d10fec3-7a2d-4bfd-be0e-6196152d88f5", "BuoyProp", "Pedestal Lamp (Top of Red Tower Walkie-Talkie)", "overworld"),
    IslandObject("12a7f64b-8175-4857-a371-77dc2989cd68", "BuoyProp", "Pedestal Lamp (near Cannonball and Backpack)", "overworld"),
    # Behind the Big Wall, on the far side from the Centurion Song.
    IslandObject("b95078fd-d8f2-4fd9-b2aa-ecf8b84fdad7", "BuoyProp", "Pedestal Lamp (past Big Wall 2)", "big_wall"),
    IslandObject("59a73457-6368-4639-8ec7-335902797343", "BuoyProp", "Pedestal Lamp (near Coordinates)", "overworld"),
    IslandObject("f7f2e9ed-14fd-4a54-a918-fc06b444a39e", "BuoyProp", "Pedestal Lamp (near Easy Simultaneous Press)", "overworld"),
    IslandObject("80e38f3e-d054-4187-9675-66d6e70c8356", "BuoyProp", "Pedestal Lamp (near Egg Hunt)", "overworld"),
    IslandObject("cec1412b-4727-48c4-b6a1-5a5e7989af18", "BuoyProp", "Pedestal Lamp (near Tutorial Area Firework 3)", "overworld"),
    IslandObject("94876c24-50ba-41cb-8fdd-9bd026f3be32", "BuoyProp", "Pedestal Lamp (near Tutorial Area Firework)", "overworld"),
    IslandObject("087f1e73-309b-4db2-8e39-dee6c926ad59", "BuoyProp", "Pedestal Lamp (near Flare Run Answer)", "overworld"),
    IslandObject("df814b39-e550-4e17-ad44-e32028ab3c47", "BuoyProp", "Pedestal Lamp (near Green Dome 2)", "overworld"),
    IslandObject("f4819494-6b1d-48ef-823b-4b26d2ceda53", "BuoyProp", "Pedestal Lamp (near Optical Telegraph Hint)", "overworld"),
    IslandObject("13956d25-97a1-4b5c-9992-00c187092476", "BuoyProp", "Pedestal Lamp (near Musical Holiday 2)", "overworld"),
    IslandObject("982f4dbf-22bb-4553-aa9c-dd6588c19fda", "BuoyProp", "Pedestal Lamp (near Optical Telegraph)", "overworld"),
    IslandObject("3bfaaa40-7101-4f52-ad72-a283a8e39cac", "BuoyProp", "Pedestal Lamp (near Purple Area Megaphone)", "chairlift"),
    IslandObject("17e22f02-4f01-4aa4-83fb-d2871acd97cd", "BuoyProp", "Pedestal Lamp (near Red Tower 3)", "overworld"),
    IslandObject("6a673617-992d-49ca-87f9-223aef4c9fe4", "BuoyProp", "Pedestal Lamp (near Scout Bombs)", "overworld"),
    IslandObject("6beb48a3-60de-41e8-a9f6-d39772bcdfa4", "BuoyProp", "Pedestal Lamp (near Yellow Tower Megaphone)", "overworld"),
    IslandObject("d3e57e13-a91d-492c-bd46-2c59da8bef9e", "BuoyProp", "Pedestal Lamp (near Telescope to Box)", "overworld"),
    IslandObject("5ee8fa69-8937-4841-b9a7-ee8a10c3a933", "BuoyProp", "Pedestal Lamp (near Teller Window 5)", "overworld"),
    IslandObject("4b9cad35-ab44-4dfe-9e4b-390cb165aca5", "BuoyProp", "Pedestal Lamp (near Teller Window 6)", "overworld"),
    IslandObject("632fc258-f424-4b88-930a-a62c45044965", "BuoyProp", "Pedestal Lamp (near Trap Room)", "overworld"),
    IslandObject("d9f93a0a-19e3-4702-b5be-7b79e39d32c8", "BuoyRedProp", "Red Lamp (Tutorial 1)", "start"),
    IslandObject("1a014620-29dd-4517-8990-3854045129bd", "BuoyRedProp", "Red Lamp (Tutorial 2)", "start"),
    IslandObject("abc5a86c-4618-4bf6-bdba-e97e488ef81e", "BuoyRedProp", "Red Lamp (Tutorial 3)", "start"),
    # The pedestal lamps: a second lamp model, under the same rules.
    IslandObject("0537b69a-6e49-406a-b93c-d6bc7f6cc4b3", "BuoyLight", "Pedestal Lamp (Blue Castle Tower)", "tower:bigKeyBlueZone"),
    IslandObject("dab5cfc8-f4d9-44f2-abb0-90bc42b3cade", "BuoyLight", "Pedestal Lamp (Diffuser Peak 2)", "overworld"),
    IslandObject("eaa97883-12ad-4e8e-9f6d-cdf7f1a3a061", "BuoyLight", "Pedestal Lamp (Green Cup Tower 1)", "tower:bigKeyGreenZone"),
    IslandObject("68287599-1f13-42f0-9424-cabd187e8a55", "BuoyLight", "Pedestal Lamp (Green Cup Tower 2)", "tower:bigKeyGreenZone"),
    IslandObject("52b0dfcb-4588-4ce7-a78c-bdb9cb800fe6", "BuoyLight", "Pedestal Lamp (Mountain Top)", "overworld"),
    IslandObject("f84cb1c7-1702-4fa1-9ddb-f4c334041237", "BuoyLight", "Pedestal Lamp (Mountain Top Entrance)", "overworld"),
    IslandObject("cd88c773-6375-42c7-9475-76b288bd1c91", "BuoyLight", "Pedestal Lamp (Mountain Top Plateau)", "overworld"),
    IslandObject("1b047167-6e68-49c3-abf2-e15fbbeca0a5", "BuoyLight", "Pedestal Lamp (Red Funnel Tower 1)", "tower:bigKeyRedZone"),
    IslandObject("d27fe6ce-2b92-42bd-8d60-8885e368877c", "BuoyLight", "Pedestal Lamp (Red Funnel Tower 2)", "tower:bigKeyRedZone"),
    IslandObject("b28ac861-6c5d-4702-8b11-ef38df1eac22", "BuoyLight", "Pedestal Lamp (near Purple Tunnel Exit)", "purple"),
    IslandObject("4cd9c5fc-7414-4bfc-b4a2-6fb4fa0312c5", "BuoyLight", "Pedestal Lamp (near Purple Peak Firework)", "chairlift"),
    IslandObject("3d09137d-fbbd-4cb8-a17a-05de7676f61c", "BuoyLight", "Pedestal Lamp (Yellow Twist Tower 1)", "tower:bigKeyYellowZone"),
    IslandObject("d0f4305f-b577-41f7-a879-18be53553509", "BuoyLight", "Pedestal Lamp (Yellow Twist Tower 2)", "tower:bigKeyYellowZone"),
    IslandObject("60412245-4b6c-47eb-a4e7-67eec146bf20", "BuoyLight", "Pedestal Lamp (near Basketball)", "overworld"),
    IslandObject("b2f9b7af-39f5-431c-9908-c2ebf6d6453b", "BuoyLight", "Pedestal Lamp (near Yellow Flare)", "overworld"),
    IslandObject("16990dcc-ebb8-453e-88fc-6478e3980728", "BuoyLight", "Pedestal Lamp (near Yellow Flare 2)", "overworld"),
    IslandObject("db6fc3f6-366c-4cf9-81f1-9c45cbdceef4", "BuoyLight", "Pedestal Lamp (near Cabin Fever Long)", "purple"),
    IslandObject("cf2e3191-4c77-4ce9-8c56-f43a73d6e531", "BuoyLight", "Pedestal Lamp (past Big Wall)", "big_wall"),
    IslandObject("080554bb-bc16-41cd-bc96-4765cec96beb", "BuoyLight", "Pedestal Lamp (near Egg Hunt 2)", "overworld"),
    IslandObject("d0a1c556-d62f-42df-a36e-019636da7679", "BuoyLight", "Pedestal Lamp (near Fielding)", "overworld"),
    IslandObject("5efbf72b-9dff-4251-a536-f625ba8ff315", "BuoyLight", "Pedestal Lamp (near Tutorial Exit)", "overworld"),
    IslandObject("fee806b8-8617-49f8-99af-bdea0c148ed0", "BuoyLight", "Pedestal Lamp (near Hub Firework)", "overworld"),
    IslandObject("4ec6aca0-9961-4550-9c26-7cacb9917566", "BuoyLight", "Pedestal Lamp (near Tutorial Area Firework 2)", "overworld"),
    IslandObject("b46c590f-7038-4033-b84d-e3fb664e095c", "BuoyLight", "Pedestal Lamp (near Yellow Flare 3)", "overworld"),
    IslandObject("d069a848-37f5-40e0-ac18-64516951f481", "BuoyLight", "Pedestal Lamp (near Musical Holiday)", "overworld"),
    IslandObject("c7141d1b-96bf-4c2c-8c05-3d2067e64a93", "BuoyLight", "Pedestal Lamp (near Optical Telegraph Hint 2)", "overworld"),
    IslandObject("11c1d018-3dbf-46d4-b1de-f9aba4d29fc2", "BuoyLight", "Pedestal Lamp (near Green Flare)", "overworld"),
    IslandObject("bef6d32e-57a9-4ba1-b984-05fdec08800c", "BuoyLight", "Pedestal Lamp (near Black Tower)", "overworld"),
    IslandObject("accb5df0-1452-4b84-8607-9f01bd517103", "BuoyLight", "Pedestal Lamp (near Blue Tunnel Entrance)", "overworld"),
    IslandObject("0e8faaf2-b5fd-4906-aaab-788f8ccbf603", "BuoyLight", "Pedestal Lamp (near Perspective Counting)", "overworld"),
    IslandObject("a552b903-066f-45a8-8550-4cbf5652de74", "BuoyLight", "Pedestal Lamp (near Poet and Priest)", "overworld"),
    IslandObject("38722a78-da82-4d6e-80c4-efd70e1be90f", "BuoyLight", "Pedestal Lamp (near Pointer's Paradise 3)", "overworld"),
    IslandObject("d368b4c7-8389-4f63-a20f-ba88ef17f0a5", "BuoyLight", "Pedestal Lamp (near Pointer's Paradise 2)", "overworld"),
    IslandObject("7017169d-96f6-4578-8c1f-eb65f3549e32", "BuoyLight", "Pedestal Lamp (near Green Tower Megaphone)", "overworld"),
    IslandObject("4ee18fc7-2a43-44e0-9a2b-4508e9d2705d", "BuoyLight", "Pedestal Lamp (near Chairlift Yellow Tower Stop)", "overworld"),
    IslandObject("c716c86d-81a7-40cc-873f-0405821dbd72", "BuoyLight", "Pedestal Lamp (near Chairlift Yellow Tower Beach)", "overworld"),
    IslandObject("cef7ae31-a105-491d-8d0d-daab0253909c", "BuoyLight", "Pedestal Lamp (near Chairlift Yellow Tower Stop 2)", "overworld"),
    IslandObject("007a1599-17ad-426f-a68b-37718423e9f7", "BuoyLight", "Pedestal Lamp (Hub past Left Arch Door)", "overworld"),
    IslandObject("99d8a99a-89eb-4ad0-8736-104aafe1fdd7", "BuoyLight", "Pedestal Lamp (Hub Left Arch Door)", "overworld"),
    IslandObject("fa653354-bdfa-431d-aa19-7453e5730e80", "BuoyLight", "Pedestal Lamp (near Green Dome)", "overworld"),
    IslandObject("942347f0-cd23-445f-8019-d296e5c92ead", "BuoyLight", "Pedestal Lamp (Hub 4)", "overworld"),
    IslandObject("0abc8402-398d-4f0d-bf5e-fb3051a16e21", "BuoyLight", "Pedestal Lamp (near Hub Firework 2)", "overworld"),
    IslandObject("c2756ad3-4f4a-45e5-93a3-4c10e892ebdc", "BuoyLight", "Pedestal Lamp (near Teller Window 1)", "overworld"),
    IslandObject("226bdfbc-c2ad-4bda-b9ec-2d72c09f1502", "BuoyLight", "Pedestal Lamp (near Teller Window 2)", "overworld"),
    IslandObject("d467b790-8968-46b1-9be9-e36eac9ebe52", "BuoyLight", "Pedestal Lamp (near Teller Window 3)", "overworld"),
    IslandObject("2cb8963f-ac2f-4f2a-b720-11a701bc4b08", "BuoyLight", "Pedestal Lamp (near Teller Window 4)", "overworld"),
    IslandObject("d2b218b4-279c-4e3e-9751-53a8cbfade87", "BuoyLight", "Pedestal Lamp (near Big Fiesta Beach)", "overworld"),
)

OBJECT_KINDS: tuple[tuple[str, str], ...] = (
    ("WalkieTalkieProp", "Walkie-Talkie"), ("MegaphoneProp", "Megaphone"), ("FmRadioProp", "Radio"),
    ("BinocularsProp", "Binoculars"), ("LaserProp", "Laser"), ("TorchProp", "Torch"), ("XrayGogglesProp", "Scanner"),
    ("ClockProp", "Clock"), ("CompassProp", "Compass"), ("FoldingMapProp", "Folding Map"),
    ("CowBellLargeProp", "Large Cowbell"), ("CowBellMediumProp", "Medium Cowbell"),
    ("CowBellSmallProp", "Small Cowbell"), ("SalonBrushProp", "Paintbrush"),
    ("BuoyProp", "Lamp"), ("BuoyRedProp", "Red Lamp"), ("BuoyLight", "Pedestal Lamp"),
)
"""The kinds of ISLAND_OBJECTS, in id order: (the game's name, the location's)."""

OBJECT_KIND_KEYS: dict[str, str] = {
    "WalkieTalkieProp": "walkie_talkie", "MegaphoneProp": "megaphone", "FmRadioProp": "radio",
    "BinocularsProp": "binoculars", "LaserProp": "laser", "TorchProp": "torch", "XrayGogglesProp": "scanner",
    "ClockProp": "clock", "CompassProp": "compass", "FoldingMapProp": "map",
    "CowBellLargeProp": "cowbell", "CowBellMediumProp": "cowbell", "CowBellSmallProp": "cowbell",
    "SalonBrushProp": "paintbrush",
    "BuoyProp": "lamp", "BuoyRedProp": "lamp", "BuoyLight": "lamp",
}
"""The `object_sanity_kinds` key of each kind (the three cowbells share one)."""

OBJECT_KINDS_LEFT_OUT_BY_DEFAULT: frozenset[str] = frozenset({"paintbrush"})
OBJECT_KINDS_NEVER_TAKEN: frozenset[str] = frozenset({"SalonBrushProp"})
"""Kinds the mod never takes off the island, picked up or left out."""


OBJECT_EVENT_ITEM_NAME = "Object"
"""The event each object check holds beside it, so the logic can count them for `big_objects`."""


def object_event_name(location_name: str) -> str:
    return f"{location_name} (event)"


def object_kind_location_name(label: str) -> str:
    return f"First {label} Pickup"


def object_location_id(obj: IslandObject) -> int:
    return BASE_ID + OBJECT_ID_OFFSET + ISLAND_OBJECTS.index(obj)


def object_kind_location_id(position: int) -> int:
    return BASE_ID + OBJECT_KIND_ID_OFFSET + position
