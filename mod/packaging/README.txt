Big Walk - Archipelago mod
==========================

Installation
------------
1. Close the game.
2. Open the game's install folder. On Steam: right-click Big Walk >
   Manage > Browse local files. It is the folder that contains
   "Big Walk.exe".
3. Extract EVERYTHING in this zip into it, merging folders. Next to
   "Big Walk.exe" you should end up with:
     winhttp.dll, doorstop_config.ini, dotnet\ and BepInEx\
4. Launch the game THROUGH STEAM (not by double-clicking the executable:
   on some machines a direct launch does not load the mod).
5. The first launch is slow - a minute or two of black screen - while
   BepInEx downloads the Unity libraries it needs and builds its cache,
   so be online for it. The launches after it are normal.

Checking that it worked
-----------------------
BepInEx\LogOutput.log should contain a line reading
"Big Walk Archipelago v... loaded.".

Playing together
----------------
- Every player installs this zip, the same mod version and the same game
  version.
- Guests configure nothing: only the host types the Archipelago details on
  the hosting screen, and it is their save that holds the checks, the items
  and the deposits. Keep the same host for the whole game.

Your own tiles and sounds
-------------------------
The host can show their own drawings on the tiles and play their own sounds
on the sound puzzles, for everyone: PNG and WAV files named after the tile
(PegTileA4.png replaces the G) in BepInEx\config\BigWalkArchipelago\tiles\.
The guide: CUSTOM-TILES.md in the mod's GitHub repository.

Uninstalling
------------
Delete winhttp.dll, doorstop_config.ini, dotnet\, BepInEx\, README.txt and
THIRD-PARTY-NOTICES.txt from the game folder. The game goes back to being
perfectly vanilla - nothing in its own files is ever modified.

Credits
-------
The Big Walk Archipelago plugin is released under the MIT license
(BepInEx\licenses\BigWalkArchipelago.MIT.txt). This package also bundles BepInEx, Doorstop, Il2CppInterop, the .NET runtime and
their libraries, each under its own license: see THIRD-PARTY-NOTICES.txt and
BepInEx\licenses\.
