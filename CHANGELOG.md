# Changelog

The mod and the apworld move together: `Plugin.PluginVersion`, the `.csproj`,
`archipelago.json` and `WORLD_VERSION` carry the same number. A seed made by
an older apworld plays with a newer mod (ids never change); the other way
round, an older mod ignores what it does not know and can make a seed
unwinnable, which is why every player of a session should run the same build.

## Unreleased

### Changed

- **`gourd_name` only renames the gourds in Big Walk**: the item feed, the goal line and the hub board show the
  name, as before. The apworld no longer rewrites the data package, so the server, the Text Client, the other
  games and the spoiler log say "Gourd", and two Big Walk slots may now pick different names.
- **Three options gone, Archipelago has them**: instead of `hint_keys`, put `Big Keys` in `start_hints`; instead of
  `zone_locks_local` and `gauntlet_stages_local`, put `Zone Locks` or `Gauntlet Doors` in `local_items`. The Gauntlet's
  stage items were local by default and no longer are. A YAML that still lists them only gets a warning.
- **The mod no longer works out the logic**: `map_tracker` shows every check not done, hinted ones on a gold pin
  (`in_logic` is read as `all`), and `tracker_board` counts the checks left and names the hinted ones. Universal
  Tracker and PopTracker packs still get the logic.
- **The hint sign no longer suggests item names** while you write: ask with the name, as in the Text Client.
- **Start items are decided before the fill**: the search that gave a second item away on very tight seeds is gone.
- **Five settings gone from the .cfg** (`KeyColorProperty`, `GourdRestoreInterval`, `ClimbSpot`, `ReceivedSound`,
  `CheckSound`): they now use their default. The release build no longer writes a debug file.
- Every player of a session needs this version (protocol v27).

### Fixed

- **South Peak Fireworks were in logic without the chairlift** (Tyrope): the launcher sits past it, like the Maroon
  Backpack and the Blue Belt, and now needs the Chairlift item too.

## 1.0.15

### Moving from 1.0.14 to 1.0.15

- Only the apworld changed (protocol v26): the 1.0.14 mod plays 1.0.15 seeds, and YAMLs work as they are.
  Regenerate to get the fix.

### Fixed

- **Three lamps past the purple hole were in logic without it** (caitlin): the 1.0.7 fix only covered the seven
  purple puzzles. Lamp and Pedestal Lamp (South Peak 1) and Pedestal Lamp (near Cabin Fever Long) now need the
  Tunnels item too (the Purple Tunnel with `tunnel_sanity`). Lamp (near Cabin Fever Long), on the coast, no longer
  needs the chairlift.

## 1.0.14

### Moving from 1.0.13 to 1.0.14

- The mod and the apworld both changed, but nothing on the wire (protocol v26): 1.0.13 seeds and YAMLs work as they
  are. Update the mod to see its version in game; new seeds get the train switches' new names.

### Added

- **The mod's version in game** (Tyrope): it showed bottom-right on the title and loading screens only; it now also
  shows there while the pause or settings menu is open in the world.

### Changed

- **The train light switches are named after their trains** (Tyrope): there are three separate trains, red, green
  and yellow, each with a headlight switch on its engine's dashboard, right of the horn and bell. "Light Switch
  (Train, Front / Middle / Back Car)" become "Light Switch (Red / Green / Yellow Train)". Their ids do not change.

### Docs

- **The `slot_data["logic"]` format is in the apworld README** (Tyrope): it pointed to a file that is not published.

## 1.0.13

### Moving from 1.0.12 to 1.0.13

- Only the apworld changed: the mod is the same as 1.0.12's (protocol v26), so a 1.0.13 seed plays with the 1.0.12
  mod, and 1.0.12 YAMLs generate as they are.

### Changed

- **The logic travels with every seed**: slot_data's `logic` (regions, exits and every rule, as data) used to be sent
  only with `tracker_board` or `map_tracker`. It is now always there, so an outside tracker such as a PopTracker pack
  can evaluate the apworld's own rules instead of porting them by hand. Format in `apworld/README.md`.

## 1.0.12

### Moving from 1.0.11 to 1.0.12

- Only the apworld changed: the mod is the same as 1.0.11's (protocol v26), so a 1.0.12 seed plays with the 1.0.11
  mod, and 1.0.11 YAMLs generate as they are. Regenerate a seed to get the fix.

### Fixed

- **Binoculars (Carousel) out of reach** (Morgan): with `lock_abilities` and `object_sanity`, the logic could ask for
  the Carousel's binoculars before the Seat, though they are only reached by riding the Carousel's swings. They now
  need the Seat.

## 1.0.11

### Moving from 1.0.10 to 1.0.11

- Only the mod changed: the apworld is the same as 1.0.10's (protocol v26), so 1.0.10 seeds and YAMLs work as they
  are. Every player should still update the mod, the host first: the fix runs on each machine.

### Fixed

- **Custom tile sounds going silent** (Blunty, and two other players): after walking away from the sound tiles'
  speakers for a while, the host's `PegTileA<n>.wav` sounds stayed silent on coming back. The game unloads idle clips
  from memory, and a sound built from a file could not be loaded back; the mod now keeps them loaded.

## 1.0.10

### Moving from 1.0.9 to 1.0.10

- Only the apworld changed: the mod is the same as 1.0.9's (protocol v26), so a 1.0.10 seed plays with the 1.0.9
  mod, and the 1.0.10 mod plays a 1.0.9 seed.
- A 1.0.9 YAML generates as it did: `lock_running: true` reads as `anywhere`, `false` as `off`.

### Added

- **`lock_running: early`** (EnvyOmega): Running stays an item, placed among the first checks anyone can reach (in a
  solo seed, in the starting zone). `lock_running` is now `off`, `anywhere` or `early`.

## 1.0.9

### Moving from 1.0.8 to 1.0.9

- Everyone in a session needs this version: the mod's protocol is now v26.
- A 1.0.8 YAML generates as it did; only a YAML naming the two lamps near Centurion Song in `exclude_locations` or
  `priority_locations` needs their new names (see below). A seed generated before keeps its old logic for them.

### Added

- **Your own tiles** (Vidyo): the host's PNGs in `BepInEx/config/BigWalkArchipelago/tiles/` are shown on the peg
  tiles, for every player of the session (the host sends them; a guest's own files are not used). Named after a tile
  kind (`PegTileA4.png`), one replaces that icon or scribble; named after one of the mod's drawings (`pelly.png`), it
  stands in for it. Any PNG that is not interlaced, any size up to 4096 pixels, with or without transparency; the
  folder is read again when it changes. How to, step by step: `CUSTOM-TILES.md`.
- **A pool of your own drawings**: any number of PNGs in the tiles folder's `pool`, drawn for the icon tiles by the
  seed and slot (the same on every reconnection). Settings > Archipelago > Tile pool, on the host: over the seed's
  own draw, only on the icons the seed left as the game's, or off.
- **Your own tile sounds**: `PegTileA1.wav` to `PegTileA9.wav` in the same folder replace the icon tiles' parts of
  the music on the speakers of Singer and Selector, Dancer and Selector and Tile Thief. The sound is their clue, so the
  host's sounds are sent to every guest (samples only, checked by size and hash) and a guest's own files are not used.

### Fixed

- **The two lamps behind the Big Wall were in logic without the Big Wall Door** (Morgan): they now sit behind it,
  as objects and as lights, and no longer count as yellow-zone lamps for `zone_locks`. They are renamed
  `Lamp (Big Wall)` and `Pedestal Lamp (Big Wall)` (they were "near Centurion Song", the nearest puzzle as the crow
  flies); their ids do not change, but a YAML naming them in `exclude_locations` or `priority_locations` needs the
  new names.

## 1.0.8

### Moving from 1.0.7 to 1.0.8

- Everyone in a session needs this version: the mod's protocol is now v25.
- A 1.0.7 YAML generates as it did, except for two more Teleporters with `teleport_buttons: items`.

### Added

- **Teleport buttons to the Drawbridge and the Green Dome Tower** (Jay0): one button in the hub for each and one back
  in each. With `with_towers` they open once a player has stood there. With `items` they also need their Teleporter,
  so a seed generated before 1.0.8 never opens them. The hub's row moved two places to the left to make room for them on the red wall.
- **Resync stations at the Drawbridge and the Green Dome Tower**, with `tower_resync_stations`.

### Fixed

- **The Silent Gauntlet sent DeathLinks named after Scout Bombs** (Tyrope): opening a stage's door to enter it or to
  leave it after solving it could count as a failure. The door fix of 1.0.7 now covers every "buttons in order" puzzle,
  not just Pointer's Paradise, whichever comes first, the door or the reset. A stage already solved no longer fails,
  a timed tomato looks at its own stage instead of Scout Bombs' check, and the Gauntlet's failures carry their stage's
  name.
- **Small regular hitches**: two of the mod's checks searched the whole scene every few seconds, for about 10 to 20 ms
  each time. They now search when something changed, with a slower check as a safety net.

## 1.0.7

### Moving from 1.0.6 to 1.0.7

- Update the apworld and the mod together: a seed with `tunnel_sanity` needs this mod to open its doors.
- A 1.0.6 YAML generates as it did, except that the purple puzzles now wait for the Tunnels.

### Added

- **`tunnel_sanity`** (off by default): the tunnels open door by door. The Tunnels item gives way to five items,
  Red, Green, Blue and Yellow Tunnel Door (each the outlet by its tower; any one lets you in) and Purple Tunnel.

### Fixed

- **The seven purple puzzles were in logic without the purple hole** (MasterProZ_NL): the hole that leads to them past
  the chairlift only opens with the Tunnels. They now need the Tunnels item (the Purple Tunnel with `tunnel_sanity`).
- **A timed tomato sent a DeathLink on a solved puzzle** (Tyrope, Window Labyrinth): when it ran out while the gourd's
  clamp was still opening, the check had not gone yet. The mod now waits a few seconds for the check before counting a
  failure.
- **Closing the door of Pointer's Paradise sent a DeathLink** (Tyrope): 1.0.2 only spotted the reset when nobody closed
  the room; the game gives it the closer's name. A failure that turns the puzzle off in the same moment is a reset now.

## 1.0.6

### Moving from 1.0.5 to 1.0.6

- **Everyone needs this build:** some of the mod's buttons take other tickets, and a guest on 1.0.5 would press
  nothing with them.
- A 1.0.5 YAML generates as it did: the apworld only changes its version.

### Fixed

- **Sounds, door and clamp animations stopped for good** (Morgan, Blunty): a gourd cleared by a resync, or any rolling
  prop that went away, stayed in the game's audio loop, which then failed every frame (the flood of errors in the log).
  The mod now takes such a prop out of the loop as it goes.
- **A Memory Bombs button could not be pressed** (Jay0): a button of the mod could take the ticket of a game button that
  was switched off at the time (puzzle needs locked), and the press then reached the mod's button. Tickets held by
  switched-off objects are now avoided, for the mod's buttons and for the extra gadgets.

## 1.0.5

### Moving from 1.0.4 to 1.0.5

- Update the apworld to generate: a 1.0.4 YAML generates as it did, except that the Hub Fireworks now wait for the way
  out of the starting zone when the First Arch Door is locked.
- The mod's fixes are each player's own: a session still plays with mixed 1.0.4 and newer builds.

### Fixed

- **A seed could be impossible from the start** (Blunty, Morgan): the Hub Fireworks were in logic from the starting
  zone, but they stand in the hub, past the drawbridge or the First Arch Door. With both locked, the generator could put
  the Drawbridge there.
- **The custom tiles showed pieces of the game's own symbols after rejoining** (Blunty, Morgan): a world loaded again in
  the same game kept its tiles on the game's sheet. They are drawn again on each new world.
- **Big Run's key during Big Speed, Big Jump or Big Sleep left you fast, or frozen, for good** (Morgan): with the Big Run
  mod, its key now waits until the trap ends, and says so.

### Added

- **A report when Unity floods the log with errors** (Blunty): twenty errors a second for three seconds and the mod
  writes once what is broken (network objects, held objects, homes). Unity's own `Player.log` names the code at fault.

## 1.0.4

### Moving from 1.0.3 to 1.0.4

- **Everyone needs this build** with `lock_abilities`: the lock of the chairlift and the train travels to the guests with the others.
- A 1.0.3 YAML generates as it did, except with `lock_abilities` and `object_sanity`: the Megaphones now ask for
  Carrying.

### Fixed

- **The Carousel's swings could be sat on without Seat** (Tyrope): a seat that drives the puzzle was never locked. The
  logic already asked for Seat; the mod now does too.
- **A deck chair picked up first could be sat on without Seat** (naf): a chair you can carry is now always furniture,
  even in the hands of the player who holds it.
- **The Megaphones were in logic without Carrying** (Tyrope): each one hangs above one player's reach.

### Added

- **`rides_require_seat`** (off by default): with `lock_abilities`, the chairlift and the train are locked like any
  seat until Seat arrives, and the logic asks for Seat to use them.

## 1.0.3

### Moving from 1.0.2 to 1.0.3

- **Everyone needs this build:** the copies of received gadgets are painted from their network id, and a guest on 1.0.2
  would paint them another way than its host.
- A 1.0.2 YAML generates as it did. With `tower_mode`, a tower's roof (its gadgets and fireworks) and "Reached the
  tower" now wait in logic for the tower's turn, so such a seed may place things differently. `big_fiesta` is now
  `big_party`; the old name is still read, in a YAML, a seed and a save.

### Fixed

- **The host's game hitched every couple of seconds**, worse the longer the session ran. Each received gadget made the
  mod look through every prop of the world again, and a few checks ran all in the same frame every two seconds. The
  look happens once per world now, and the checks one per frame.
- **A guest's big fall never sent a DeathLink** (`big_fall`): only the host's falls were seen. The host now sees every
  player's.
- **With `tower_mode`, the trackers showed a tower's roof in logic before the tower opened**: its gadgets, its
  fireworks and "Reached the tower" now wait for its turn, as the button at its foot does. The lamps around a tower are
  at its foot and do not wait.

### Added

- **`GadgetCopiesKept`** (in the mod's `.cfg`, 10 by default): at most that many copies of each received gadget in the
  world. Loading a save puts back no more, and past it a new one takes the place of the oldest left lying around. A
  long session no longer piles up hundreds of them.
- With `random_colors`, every gadget takes its own color, each copy received too, and a torch's light takes its color.
- **Big Confetti**, a bonus (`bonus_weights`): two bursts of a puzzle's confetti around every player, with the sound of
  a puzzle won.
- `big_fiesta` is now **`big_party`** ("Big Party (on the beach)"), and `big_club` reads as `big_doof`.

## 1.0.2

### Moving from 1.0.1 to 1.0.2

- **Everyone needs this build:** the gadgets the multiworld sends are built differently, and a guest on 1.0.1 would
  build them another way than its host.
- A 1.0.1 YAML generates as it did. With `open_black_tower` off, the Black Tower's button and monument now wait in
  logic for the door at its foot, so such a seed may place things differently.

### Fixed

- **Gadgets received with `object_sanity` could break between players:** a scanner nobody else could pick up, then
  stuck on a guest's face, a torch that stayed on, a walkie-talkie that gave a guest their own voice back. A received
  gadget could borrow what identifies one of the island's own, which stays on the map with `object_sanity`.
- **Opening the door of a light puzzle sent a DeathLink** (Pointer's Paradise and the others whose lights must be
  pressed in order): the game puts the lights out when the room closes, the same way it does for a wrong press. Only a
  press counts now.
- **Peg tiles were shuffled across puzzles:** in the Green Tower, the Ring Room, the Silent Gauntlet and the rooms
  around them, tiles were strewn about, doubled in one puzzle and missing in another, invisible ink in a plain puzzle
  and plain tiles in the ink one. Tiles now swap only within their own rack and their own kind, a few seconds after the world loads, and the same
  way every time the save is loaded.
- **A key's cutting flags on the map followed the key around:** a key carried near another trail showed its flags on
  that trail's stations. Each key's flags stay on its own trail. (A key can be cut along any trail, as in the game.)
- **With `open_black_tower` off, the Black Tower's button and monument were in logic behind a shut door.** They now
  wait for it: the monuments of the drawbridge and the four coloured towers full, or with `tower_mode` the Black
  Tower's turn, when the mod opens it.
- Generation is much faster in large multiworlds: the world's pre-fill no longer walks every other world (27 s down
  to under a tenth of a second per Big Walk among 150 slots).

### Added

- **`mirrored_poses`** (0 to 100, off by default): the pose panels (Indoor Semaphore, Optical Telegraph, Charades
  Rooms) are drawn among the game's poses and the mirrors of the four that are not symmetric, a pose and its mirror
  possibly side by side. The value is how often a mirror is drawn against a game pose. The puzzles work as before.
- **Two objectives in the tunnels:** **`big_doof`**, the Doof Chamber's five lights on and its smoke machine's button held for ten seconds, and
  **`big_makeover`**, painted at the Secret Salon (every player, each in turn, with `everyone_gathers`). With `everyone_gathers`, every player there. The Doof
  Chamber's switches and radio are now in the tunnels for the logic and for `zone_locks`.
- Big Climb's, Big Fiesta's and Big Doof's ten seconds tick, with Cabin Fever's sound: once when they start, then on
  each of the last three, for every player.
- With `random_colors`, each laser takes its own color, its tip and its beam included.
- With `tower_mode: by_item`, the towers' line says which Progressive Monument it waits for.
- The host is told on screen when the seed was made with a newer apworld than the mod: the mod ignores what it does not
  know, which can leave a seed unwinnable. A seed from an older apworld plays as it was generated, without the newer
  options (custom tiles among them), and is not warned about.
- The session journal (`BepInEx/session-journal.tsv`) notes what each player holds, for reports of a gourd that cannot
  be picked up.
- The setup guide covers Linux and the Steam Deck (Proton needs a launch option).

## 1.0.1

### Moving from 1.0.0 to 1.0.1

- **A fix for the mod only.** Nothing changed in the world: a 1.0.0 seed plays as it is, and so does a seed already
  started (a seed stuck at a shut Green Dome opens again once the host runs 1.0.1). Every player of a session should
  still run the same build.

### Fixed

- **The Green Dome's door stayed shut** while its monument was the one taking gourds: the game opens it once the
  Black Tower is complete, which the world never asked for. With `tower_mode` drawing the Green Dome before the Black
  Tower, no monument took gourds at all. The mod now opens it at the Green Dome's turn (from the start with
  `all_open`).
- **A player who fell could not get up** with Legs and Seat both locked (`lock_abilities`): jump and sit, the only
  ways up, stay on while seated.
- A raised arm that pointed, with pointing locked, still showed a T-pose and its sound on the player's own screen.

## 1.0.0

### Moving from 0.7 to 1.0

- **Everyone needs this build:** the host's channel to its guests changed (protocol v22).
- **A 0.7 YAML generates as it did, with one change you can see:** `custom_tiles` is on by default
  (`matching_games`): when a game of the multiworld has a drawing of its own, some of the puzzles' basic tiles show it.
  The puzzles ask for the same tiles; `custom_tiles: off` keeps the game's.
- `pack_sanity`, `flare_gun_sanity` and `linear_towers` were on/off and have more values now; `true` and `false`
  still mean what they meant.

### Added

- **Custom tiles**, drawn by the community (Jack5, Don Chalant, GameFraek, Kensufox; CC BY 4.0): the nine basic peg
  tiles shown as other drawings, on every tile, split-flap and display, the same on every player's screen.
  **`custom_tiles`** (off / matching_games, the default: the drawings of the games in the multiworld join the game's
  tiles / matching_first: every one of them shown, the game's tiles in the places left / all / custom_only),
  **`custom_tile_choices`** (the tiles you want for sure), **`custom_tile_weights`**. A game brings its drawing in
  when its name holds the drawing's game, whatever the capitals (`Super Mario 64`, `TUNIC`, a Manual's
  `Manual_SuperMarioBros3_...`); pelly, Archipelago's own, is in every multiworld.
  **`custom_drawings`** for the drawing panels' scribbles, none yet. How to propose a tile:
  `mod/assets/tiles/README.md`.
- **`pack_sanity: junk`** and **`flare_gun_sanity: junk`**: the same checks, holding only filler or a trap.
- **`pack_sanity: vanilla`**: each pack's check gives the pack itself, which stays in your hands; no other location
  gives one.
- **`linear_towers: one_deposit_box`**: a tower's key comes once its monument holds a number of gourds drawn for each
  tower, at a check of its own, the "Monument Key".

### Fixed

- The peg tiles a puzzle part hid could fall off their racks when their item came.
- The seed's colors kept the order they were drawn in (the flare guns took the lowest ones).
- `junk` options leave the progression enough room: a tight seed could fail to fill.

## Before 1.0

The full notes of these versions are in the git history of this file.

- **0.7.1**: `zone_locks`, and a share of junk for the object and light checks.
- **0.7.0**: the hub's hint and tracker boards, the map tracker, more traps.
- **0.6.1**: seeds the fuzzer could not make or win (apworld only).
- **0.6.0**: abilities, objects and lights as checks, monuments full, the game's own order back as options, three traps.
- **0.5.0**: objectives, towers in order, towers reached as checks, the gourds' name.
- **0.4.0**: packs, fireworks and flare guns as checks, colors, traps and bonuses, DeathLink, TrapLink, settings in game.
- **0.3.0**: the Silent Gauntlet, arch doors in logic, teleport buttons, Tile Thief's button, peg tiles shuffled.
- **0.1.2**: puzzle parts as items.
- **0.1.0, 0.1.1**: the first alphas.
