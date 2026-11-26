# Big Walk

## Where is the options page?

The [player options page](../player-options) lets you configure your personal options and export a config file from
them. Every option is described there and in the YAML template. This page says what they change in the game.

## What does randomization do to this game?

- **Gourds**: solving a puzzle sends a check instead of giving its gourd. Your gourds come from the multiworld, as
  props you can carry. Any gourd fits any monument slot, and deposits are checks.
- **Big keys**: each one is an item, tinted so you can tell them apart. You still cut it along its trail and place it:
  each cut and each placement is a check. Placing a key opens nothing.
- **What the keys opened** are items of their own: the drawbridge, the map room, the chairlift, the train, the tunnels,
  the Big Wall Door and the Spawn Secret Door.
- **Radio**: switching on a station is a check, and its music plays once its Radio Music item arrives. Stations are
  numbered by their light on the dial, from the left.
- **Puzzle parts** (`lock_puzzle_needs`, on by default): the 17 things puzzles are built from (buttons, panels,
  speakers...) are items. A part is on the map for everyone once its item arrives. You start with one that solves a
  tutorial puzzle.
- **Arch doors**: the hub doors not listed in `start_with_arch_doors_open` are items. With `require_arch_doors`, what
  lies past the left door (Yellow, Blue and Black towers, Green Dome) or the right door (Green Tower, chairlift) needs
  that door in logic, though you can walk around it.
- **Purple gourds** are on the map from the start, and the seal on the true ending's path is gone.

## What is the goal of Big Walk when randomized?

- **Big Wall**: break the bell inside the Big Wall.
- **Big Goodbye** (default): complete the Silent Gauntlet behind it.
- **Big Game**: reach the secret ending behind the Spawn Secret Door.
- **Big Collection**: place a set number of gourds in the towers' slots.

`objectives` adds things to do first, in any order, and `random_objectives` draws some of them:

| Objective | What to do |
|---|---|
| `big_collection` | place `gourds_required` gourds |
| `big_keys` | place `keys_required` big keys |
| `big_radio` | switch on `radio_stations_required` stations |
| `big_climb` | stand on the highest point of the island |
| `big_party` | plug a radio into the beach's big speakers with music playing |
| `big_doof` | in the Doof Chamber, the five lights on and the smoke button held |
| `big_makeover` | get painted at the Secret Salon |
| `big_lights` | switch on `lights_required` percent of the lights |
| `big_objects` | send `objects_required` percent of the object checks |
| `big_body` | receive `body_required` percent of the ability items |
| `big_towers` | fill `towers_required` monuments, in the seed's order |

With `everyone_gathers` (on), big_climb, big_party, big_doof and big_makeover want every player there. The objectives
are on a board in the hub. With `lock_endings_until_objectives` (on), the ending stays shut until the others are done.

```yaml
Big Walk:
  goal: big_wall
  objectives: [big_keys, big_radio, big_climb, big_party]
  random_objectives: 2   # two of these four, drawn per seed
```

`tower_mode` sets how the monuments take gourds: `all_open`, `in_order` (one at a time, in an order the seed draws) or
`by_item` (the same, and each next one waits for a Progressive Monument).

## What items and locations can get shuffled?

Locations on the default options:

- the 45 puzzles, and the 7 radio stations
- the 25 key cuts and the 7 key placements
- gourd deposits, counted over all the towers
- the 7 towers reached, and the 7 monuments full
- the 12 packs and the 4 flare guns picked up, and the 8 firework launchers fired

Off by default: picking up the island's objects (`object_sanity`), switching on its lights (`light_sanity`) and the
Silent Gauntlet's seven stages (`gauntlet_mode: locked_stages`, where each way up is an item).

Items:

- 45 Gourds, the 7 big keys, the 7 things they used to open, the Radio Music items, the arch doors and the puzzle parts
- Backpacks, Belts, a Gourd Carton, the flare guns and the Rainbow Flare Gun
- filler: the island's own hand props, whose copies on the island are removed; traps and bonuses take part of it
- with their options: abilities, Progressive Monuments, Gauntlet doors, zone items, tunnel doors, Teleporters

`zone_locks` locks objects and lamps by zone. `tunnel_sanity` opens the tunnels door by door. The seven purple puzzles
also need the Tunnels (or the Purple Tunnel).

### Abilities

With `lock_abilities` (`handed` or `ambidextrous`), what players can do becomes items, shared by everyone: **Legs**
(jump and crouch, needed to leave the starting zone), **Seat**, three **Progressive Arms** (raise, point, extend; for
each arm with `handed`), **Throwing** and **Carrying**. `lock_running` adds **Running**, never required.

What the logic asks for:

- **Seat and both arms extended**: Concert, Indoor Semaphore, Optical Telegraph, Charades Rooms, Pointer's Paradise,
  Dancer and Selector, the Gauntlet's last stage
- **Both arms raised**: Invisible Ink
- **Seat**: the Carousel and its binoculars, Cannonball Commute, and with `rides_require_seat` the chairlift and train
- **Throwing**: Fielding, Window Labyrinth, Basketball, Kick Up Pits
- **Carrying**: High Button, the two obbies, Pointer's Paradise, the Megaphones

## Closer to the game, or easier

- `linear_towers`, `linear_keys`, `linear_puzzles`, `linear_arch_doors` bring back the game's own order.
- `start_with_map_room_open`, `start_with_drawbridge_open`, `open_black_tower`, `fast_golf`, `tile_thief`, the Cabin
  Fever options, `teleport_buttons` and `tower_resync_stations` make things easier.
- For keys hinted from the start, add `Big Keys` to Archipelago's `start_hints`. To keep `Zone Locks` or
  `Gauntlet Doors` in your own world, add them to `local_items`.

In the hub, `hint_board` (on) shows your hints and takes new ones, `tracker_board` counts the checks left, and
`map_tracker` puts them on the Map Room's map. None of them works out the logic: for that, use Universal Tracker or a
PopTracker pack.

## Traps and bonuses

They hit every player of the session and replace part of the filler: none of it traps by default, a quarter bonuses.
Effects that last take `trap_duration` seconds. `hard_traps` off leaves the hard ones out.

| Trap | What it does |
|---|---|
| **Big Drop** | everyone drops what they're holding |
| **Big Throw** (hard) | everyone throws what they're holding |
| **Big Trip** (hard) | everyone is sent to a different place already opened |
| **Big Meeting** (hard) | everyone is sent around a random player |
| **Big Night** | the clock jumps to midnight |
| **Big Sleep** (hard) | everyone falls asleep, two seconds out of seven |
| **Big Load** (hard) | every pack comes off and spills |
| **Big Mask** (hard) | everyone wears the blindfold helmet |
| **Big Flare** | a flare shot lands on every player |
| **Big Silence** | everyone is muted |
| **Big Slide** | the ground is slippery |
| **Big Time** | every player gets a clock on their computer's time |
| **Big Ghost** | every player turns white |
| **Big Paint** | players swap colors |
| **Big Air** (hard) | everyone shoots up into the air |
| **Big Goggles** | the field of view narrows |
| **Big Pixels** | the island turns into big pixels |
| **Big Head** | the other players' heads grow |
| **Big Brain** | the other players' heads become lit lamps |
| **Big Arms** | the players' hands grow |
| **Big Smell** | a green gas pushes players apart |
| **Big Brother** | everyone shrinks |
| **Big Reset** (hard) | the nearest unsolved puzzle restarts |
| **Big Break** | everyone sits on a folding chair |

| Bonus | What it does |
|---|---|
| **Big Speed** | everyone walks and runs faster |
| **Big Jump** | everyone jumps higher |
| **Big Day** | the clock jumps to noon |
| **Big Confetti** | confetti bursts around every player |

**DeathLink** (off by default): sent when a puzzle is failed, and after a big fall if you ask for it. Received, it
knocks everyone out, only makes them drop what they hold, or plays a trap from a roulette, for everyone or for one
player. **TrapLink** (`trap_link`, off): traps are shared with the other games that have it.

## Looks

- `random_colors` (on) paints the island's buoys, flare guns, lasers and gadgets, the same on every screen.
- `custom_tiles` (on) shows drawings on the nine basic peg tiles, those of the games in your multiworld first. The
  puzzles ask for the same tiles. The drawings are by **Jack5**, **Don Chalant**, **GameFraek** and **Kensufox**, under
  [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/); the games belong to their owners.
- `mirrored_poses` adds mirrored poses to the pose panels.
- The host names the gourds in game: Settings > Archipelago. Archipelago still says "Gourd".

Flashing effects can be softened on your own screen: Settings > Archipelago > Gentle effects.

## Which items can be in another player's world?

Any of them.

## What does another world's item look like in Big Walk?

There's nothing to see in the world. The overlay names each check as it's sent, what it gave and to whom.

## When the player receives an item, what happens?

Gourds, keys and gadgets go to a player with free hands, or wait at the hub. Doors, rides, music and puzzle parts work
right away, for everyone. Traps and bonuses play on every player, one at a time.

The resync station in the hub brings back what got stuck out of reach. Placed keys and deposited gourds stay put.
Unlocks are applied again each time the world loads, so give a door a second after loading.

## FAQ

### Can it be played solo?

No. Big Walk is co-op, and so is this: both bells and every monument deposit need two players.

### Does everyone need a slot?

No. A whole co-op group shares one Archipelago slot, and only the host types the Archipelago details. Every player
installs the same build of the mod, though: see the setup guide.

### Is there a client to run while playing?

No. Checks go out and items come in from inside the game.
