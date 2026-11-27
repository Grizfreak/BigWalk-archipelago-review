---
name: Release test
about: What was played before a version is published (maintainer)
title: "Test of version "
labels: release-test
---

Played with the apworld that is published, as a host and a guest, in two setups:

- **Loopback** (one PC): the Debug build of the same commit, with a loopback guest (`tools/launch-guest.ps1`)
  that joins over 127.0.0.1 through Mirror like a real guest. Used for the seed settings (options, logic).
- **Real network** (two PCs on the same local network): the published zip, the guest joining through the game's
  own matchmaking (EOS). Used for what happens between players (joining, items, checks, leaving and coming back).

Each section says which setup it was played in. Tick a line once it was seen on both screens.
A line that fails stays unticked, with a comment on what happened.

- Version:
- Commit:
- SHA-256 of `BigWalkArchipelago.zip`:
- SHA-256 of `bigwalk.apworld`:
- Host PC:
- Guest PC (same local network):
- Loopback (Debug build, one PC) used for the options: yes / no
- OS tested: Windows (Linux and the Steam Deck are not tested by the maintainer)
- Date:

## What changed since the last version

- [ ] apworld
- [ ] mod

In a few lines:

## What I tested, and what I skipped

Tested:

Not tested, and why: (unchanged since vX, needs another OS, needs a real network, ...)

## If the apworld changed

- [ ] The `fuzz` workflow ran on the published commit: link to the run, and its rate of fills without a place.
- [ ] The seeds of `tools/test-seeds/` generate, or the ones that fail are named with the reason.
- [ ] Each check whose logic changed was reached in game as the logic says (logic tour or by hand): list them.
- [ ] A seed made with this apworld and the previous mod: the mod warns about the newer apworld.

## If the mod changed

### Starting and joining (real network)

- [ ] Both instances: the title screen shows no error, Settings > Archipelago is there, and the two logs name the same build.
- [ ] Both instances: the `.cfg` of the last version keeps its values on the first start.
- [ ] Host: Test connection answers in words, and the corner of the screen shows the connection.
- [ ] Guest: joins, and sees the host's connection line.
- [ ] Guest: leaves and comes back; nothing is doubled or missing.
- [ ] A guest with another build, then a guest without the mod: both get a message, and neither game crashes.

### Checks and items (real network)

- [ ] The host solves a puzzle: the server lists the check once.
- [ ] The guest cuts a big key and places it: the server lists the cuts and the placement once each.
- [ ] A gourd received is in the same place on both screens, and either player can carry and deposit it.
- [ ] A gadget received is on both screens, and the island's own of that kind are gone.
- [ ] A big key and its door received: the key is tinted and the door open for both.
- [ ] Gourds deposited by both players: the deposit check goes out.

### Options (loopback)

- [ ] `tower_mode: by_item`: only the towers opened so far take gourds.
- [ ] `lock_abilities`: locked actions are refused for both players, and work once received.
- [ ] `lock_puzzle_needs`: the buttons are missing, then back on both screens.
- [ ] A trap and a bonus: both players get them, and both end.
- [ ] The host's own tile and sound reach the guest.
- [ ] Hint sign, tracker board and map: the hinted check has a gold pin on both maps, and leaves once done.
- [ ] `random_colors`: the same colours on both screens.
- [ ] `big_climb`: the board in the hub ticks it.

### Losing the connection (real network)

- [ ] The server stops, a check is made, the server comes back: the check reaches it.
- [ ] The save is hosted again: nothing is received twice, and the gourds not deposited are back in the hub.
- [ ] The goal of a short seed: the server shows the slot as done.

## Logs

- [ ] No `[Error` line from the mod in any log, or each one explained in a comment.
- [ ] `LogOutput.log` of the host and of the guest attached, one per start.
- [ ] The session journal of the session attached.
- [ ] The Archipelago server log of the room attached.

## Outside this test (optional)

- [ ] A session with a remote player (real network): who, and what was seen.
- [ ] Linux/Proton or Steam Deck, by a community tester: who, and what was seen.
