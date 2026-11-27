---
name: Release test
about: What was played before a version is published (maintainer)
title: "Test of version "
labels: release-test
---

Co-op is played on a Debug build of the release commit (loopback instances on one PC, with or without netsim);
the published zip itself is smoke-tested as host.

- **Loopback** (one PC, one copy of the game): the Debug build of the release commit. The guests are more instances
  of the same Steam install (`tools/launch-guest.ps1`), joining the host over Kcp on 127.0.0.1 as a real guest joins
  through Mirror. With `-NetSim <preset>`, they join through `tools/netsim.py`, which delays, drops and cuts packets.
- **Smoke test** (same PC): the published `BigWalkArchipelago.zip` in a clean install, played solo as host.

How to set it up: [mod/README.md, "Testing on one PC"](https://github.com/Grizfreak/BigWalk-archipelago-review/blob/master/mod/README.md#testing-on-one-pc).
Tick a line once it was seen. A line that fails stays unticked, with a comment on what happened.

- Version:
- Commit:
- OS tested: Windows (Linux and the Steam Deck are not tested by the maintainer)
- Date:

Loopback, Debug build:
- SHA-256 of the Debug `BigWalkArchipelago.dll`:
- Fingerprint in the logs (`loaded (build ...)`), host and guests:
- Host's `Debug.Enabled`: on / off (off needs the guest's own `BepInEx-guest` folder)
- netsim: preset, seed and options (or "not used"):
- netsim log attached: yes / no

Published files:
- SHA-256 of `BigWalkArchipelago.zip`:
- SHA-256 of `bigwalk.apworld`:
- Fingerprint of the zip's DLL in the smoke test log (`loaded (build ...)`):

## What changed since the last version

- [ ] apworld
- [ ] mod

In a few lines:

## What I tested, and what I skipped

Tested:

Not tested, and why:
- The game's online transport (EpicTransport: EOS lobbies, relays, packet sizes): loopback guests join over Kcp.
- Two different Steam accounts: the instances share one SteamID (see `mod/src/Debug/LoopbackGuest.cs`).
- Two real machines (their own clocks, frame rates, NAT and drivers).
- Co-op with the Release binary: only the Debug build of the same commit plays co-op.
- (anything else: unchanged since vX, needs another OS, ...)

## If the apworld changed

- [ ] The `fuzz` workflow ran on the published commit: link to the run, and its rate of fills without a place.
- [ ] The seeds of `tools/test-seeds/` generate, or the ones that fail are named with the reason.
- [ ] Each check whose logic changed was reached in game as the logic says (logic tour or by hand): list them.
- [ ] A seed made with this apworld and the previous mod: the mod warns about the newer apworld.

## Smoke test of the published zip

- [ ] The zip is extracted into a clean install (no `BepInEx` folder left from a Debug build).
- [ ] The game starts through Steam.
- [ ] The log shows the expected version and fingerprint (`Big Walk Archipelago v... loaded (build ...)`).
- [ ] Settings > Archipelago is there.
- [ ] Test connection answers.
- [ ] The host joins a room.
- [ ] A few checks go out, and the server lists each one once.
- [ ] An item received shows up in the world.
- [ ] No `[Debug]` line in the log.

## If the mod changed (loopback)

### Starting and joining

- [ ] Every instance: the title screen shows no error, Settings > Archipelago is there, and the logs name the same build.
- [ ] Every instance: the `.cfg` of the last version keeps its values on the first start.
- [ ] Host: Test connection answers in words, and the corner of the screen shows the connection.
- [ ] Guest: joins, and sees the host's connection line.
- [ ] Guest: leaves and comes back; nothing is doubled or missing.
- [ ] A guest with another build, then a guest without the mod (`-Vanilla`): both get a message, and neither game crashes.

### Checks and items

- [ ] The host solves a puzzle: the server lists the check once.
- [ ] The guest cuts a big key and places it: the server lists the cuts and the placement once each.
- [ ] A gourd received is in the same place on both screens, and either player can carry and deposit it.
- [ ] A gadget received is on both screens, and the island's own of that kind are gone.
- [ ] A big key and its door received: the key is tinted and the door open for both.
- [ ] Gourds deposited by both players: the deposit check goes out.

### Options

- [ ] `tower_mode: by_item`: only the towers opened so far take gourds.
- [ ] `lock_abilities`: locked actions are refused for both players, and work once received.
- [ ] `lock_puzzle_needs`: the buttons are missing, then back on both screens.
- [ ] A trap and a bonus: both players get them, and both end.
- [ ] The host's own tile and sound reach the guest.
- [ ] Hint sign, tracker board and map: the hinted check has a gold pin on both maps, and leaves once done.
- [ ] `random_colors`: the same colours on both screens.
- [ ] `big_climb`: the board in the hub ticks it.

### Losing the connection

- [ ] The server stops, a check is made, the server comes back: the check reaches it.
- [ ] The save is hosted again: nothing is received twice, and the gourds not deposited are back in the hub.
- [ ] The goal of a short seed: the server shows the slot as done.

## Degraded network (netsim)

Preset and seed above. A cut is `c` in the netsim window, or `--cut <s> --every <s>`.

- [ ] The guest leaves and comes back during a cut (the cut long enough for the guest to drop).
- [ ] Items are received while the guest is cut off: they are on the guest's screen once the link is back.
- [ ] Checks made by the guest under latency or losses: the server lists each one once.
- [ ] The host loses the Archipelago server, then gets it back.
- [ ] After the resync, nothing is doubled or missing on any screen.

## Logs

- [ ] No `[Error` line from the mod in any log, or each one explained in a comment.
- [ ] `LogOutput.log` of the host and of each guest attached, one per start.
- [ ] `LogOutput.log` of the smoke test attached.
- [ ] The netsim log attached (`tools/netsim-logs/`), if netsim was used.
- [ ] The session journal of the session attached.
- [ ] The Archipelago server log of the room attached.

## Outside this test (optional)

- [ ] A session with a remote player who owns the game (real network, Release zip): who, and what was seen.
- [ ] Linux/Proton or Steam Deck, by a community tester: who, and what was seen.
