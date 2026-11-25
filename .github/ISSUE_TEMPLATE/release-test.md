---
name: Release test
about: What was played before a version is published (maintainer)
title: "Test of version "
labels: release-test
---

Played on two machines with the same zip, the one published. Tick a line once it was seen on both screens.
A line that fails stays unticked, with a comment on what happened.

- Version:
- SHA-256 of `BigWalkArchipelago.zip`:
- SHA-256 of `bigwalk.apworld`:
- Host:
- Guest:
- Date:

## Starting and joining

- [ ] Both machines: the title screen shows no error, Settings > Archipelago is there, and the two logs name the same build.
- [ ] Both machines: the `.cfg` of the last version keeps its values on the first start.
- [ ] Host: Test connection answers in words, and the corner of the screen shows the connection.
- [ ] Guest: joins, and sees the host's connection line.
- [ ] Guest: leaves and comes back; nothing is doubled or missing.
- [ ] A guest with another build, then a guest without the mod: both get a message, and neither game crashes.

## Checks and items

- [ ] The host solves a puzzle: the server lists the check once.
- [ ] The guest cuts a big key and places it: the server lists the cuts and the placement once each.
- [ ] A gourd received is in the same place on both screens, and either player can carry and deposit it.
- [ ] A gadget received is on both screens, and the island's own of that kind are gone.
- [ ] A big key and its door received: the key is tinted and the door open for both.
- [ ] Gourds deposited by both players: the deposit check goes out.

## Options

- [ ] `tower_mode: by_item`: only the towers opened so far take gourds.
- [ ] `lock_abilities`: locked actions are refused for both players, and work once received.
- [ ] `lock_puzzle_needs`: the buttons are missing, then back on both screens.
- [ ] A trap and a bonus: both players get them, and both end.
- [ ] The host's own tile and sound reach the guest.
- [ ] Hint sign, tracker board and map: the hinted check has a gold pin on both maps, and leaves once done.
- [ ] `random_colors`: the same colours on both screens.
- [ ] `big_climb`: the board in the hub ticks it.

## Losing the connection

- [ ] The server stops, a check is made, the server comes back: the check reaches it.
- [ ] The save is hosted again: nothing is received twice, and the gourds not deposited are back in the hub.
- [ ] The goal of a short seed: the server shows the slot as done.

## Logs

- [ ] No `[Error` line from the mod in any log, or each one explained in a comment.
- [ ] `LogOutput.log` of the host and of the guest attached, one per start.

## Generation

- [ ] The `fuzz` workflow ran on the published commit: link to the run, and its rate of fills without a place.
