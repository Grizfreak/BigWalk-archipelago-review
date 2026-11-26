# Big Walk (Archipelago)

An [Archipelago](https://archipelago.gg) randomizer for *Big Walk* (House House).

## Download

The latest version:

| File | Who needs it |
| --- | --- |
| [BigWalkArchipelago.zip](https://github.com/Grizfreak/BigWalk-archipelago-review/releases/latest/download/BigWalkArchipelago.zip) | every player in the session, host and guests alike |
| [bigwalk.apworld](https://github.com/Grizfreak/BigWalk-archipelago-review/releases/latest/download/bigwalk.apworld) | whoever generates the seed (Archipelago 0.6.7 or newer) |

Every [release](https://github.com/Grizfreak/BigWalk-archipelago-review/releases) holds both files.
A mod and an apworld whose first two numbers match (1.1.0 and 1.1.3) play together; the third number of each moves on its
own.

## Getting started

1. Every player extracts the zip into the Big Walk folder and launches the
   game **through Steam**.
2. Whoever generates adds `bigwalk.apworld` to Archipelago, writes **one YAML
   per co-op group**, and hosts the room.
3. The host types the Archipelago address, the slot name and the password on
   the hosting screen, and presses Continue.

→ **[The setup guide](apworld/bigwalk/docs/setup_en.md)**, and [the mod's settings](SETUP.md)

→ **[Your own tile drawings and sounds](CUSTOM-TILES.md)**: drop PNG and WAV files in a
folder, no coding.

→ **[The game page](apworld/bigwalk/docs/en_Big%20Walk.md)**: what is randomized, the goals and a FAQ.

## Reporting a problem

Open an [issue](https://github.com/Grizfreak/BigWalk-archipelago-review/issues) and
attach `BepInEx/LogOutput.log` from the **host's** machine, and from any other
player whose screen showed the problem: in co-op, what goes wrong on a machine
is usually only visible in that machine's own log.

## For developers

The world is built with Archipelago's own "Build APWorlds" and the mod with `tools/package-mod.ps1`
(both documented in their own folder: [`apworld/README.md`](apworld/README.md),
[`mod/README.md`](mod/README.md)).

To propose a new tile to replace the basic ones: [`mod/assets/tiles/README.md`](mod/assets/tiles/README.md).

## License

[MIT](LICENSE). The mod zip also bundles BepInEx and its dependencies.

The custom tiles are under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/):
[who drew which](mod/assets/tiles/LICENSE.md).

## AI disclosure

How an LLM was used to make this project: [AI-DISCLOSURE.md](AI-DISCLOSURE.md).
