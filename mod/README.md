# BigWalkArchipelago

BepInEx mod (IL2CPP, Harmony) for *Big Walk* (House House): it plugs an
Archipelago multiworld client into the game loop.

## Build

Requirements: the .NET SDK (tested with 10.0.301) and the local DLLs in
`lib/`: [lib/README.md](lib/README.md) says where each one comes from.

```
dotnet build BigWalkArchipelago.sln
```

Targets `net6.0` to match the BepInEx IL2CPP runtime shipped inside the game
(`.NET 6.0.7`, confirmed in `BepInEx/LogOutput.log`).

## Deployment

Copy **three** DLLs from `src/bin/Debug/net6.0/` into
`<game folder>/BepInEx/plugins/BigWalkArchipelago/`:

- `BigWalkArchipelago.dll`: the mod
- `Archipelago.MultiClient.Net.dll`: the official Archipelago client
- `Newtonsoft.Json.dll`: pulled in by the one above

The last two come from the `Archipelago.MultiClient.Net` NuGet package and
are copied into the build output automatically. Without either of them the
whole mod fails to load.

`tools/deploy-mod.ps1` copies them; `tools/package-mod.ps1` builds in Release
and produces the player zip (BepInEx included).

Launch the game through Steam: running the executable directly does not always
load BepInEx.

## In-game tests

`tests/ingame/scenarios.json` lists what is played before a release, on two machines with the same zip. Each
scenario names the mod folders it covers and the log lines that prove it.

```
python tests/ingame/check.py verify
python tests/ingame/check.py record --version <version> --host <logs> --guest <logs>
python tests/ingame/check.py release <zip>
```

`verify` runs on GitHub: it fails when a scenario names a log line the mod no longer writes, or when a folder has no
scenario. `record` ticks what the logs of the host and the guest prove; tick the lines "by eye" in
`tests/ingame/results/<version>.md`. `release` refuses a zip whose own build was not fully ticked. The game starts a
new log each time: keep a copy of each one.
