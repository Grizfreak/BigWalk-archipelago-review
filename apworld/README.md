# apworld/

The Python Archipelago world for *Big Walk*. The code lives in
[`bigwalk/`](bigwalk/) and packages into `bigwalk.apworld`.

- [`bigwalk/docs/`](bigwalk/docs/): the player documentation as the
  Archipelago webhost renders it: the game's page and the setup guide.

## Build

From an Archipelago source checkout with the world linked (see below):

```
python Launcher.py "Build APWorlds" -- "Big Walk"
```

Produces `build/apworlds/bigwalk.apworld` in the checkout, to be copied into the
`custom_worlds/` folder of an Archipelago installation (0.6.7 or newer).

## Development and tests

To work with Archipelago's own tests, link the world's folder into a source
checkout of Archipelago rather than copying it:

```
mklink /J "<checkout>\worlds\bigwalk" "<here>\apworld\bigwalk"
```

Then, from the checkout:

```
set AP_TEST_WORLDS=bigwalk
python -m pytest worlds/bigwalk/test -q     # the world's own tests
python -m pytest test/general -q            # Archipelago conformance
```

## Trackers

**Universal Tracker**: supported, and no YAML is needed for it:
`interpret_slot_data` hands the seed's own settings back to UT, which rebuilds
the world from those instead of from whatever sits in the tracking player's
`Players` folder.

**Other trackers** (PopTracker packs): slot_data carries the whole logic as
data in `logic`: regions, exits and every rule, by item and location name.
Evaluating it keeps a tracker in step with the apworld without porting the
rules by hand. Built by [`bigwalk/logic_export.py`](bigwalk/logic_export.py):

| Key | Content |
|---|---|
| `origin` | the region the players start in |
| `rules` | every distinct rule once; index 0 is always `["true"]` |
| `exits` | `[from region, to region, rule index]` for every entrance |
| `locations` | `[name, id, region, rule index, event item]`; id 0 and an event item name for an event, `""` for a real check |

A rule is a nested list:

| Rule | True when |
|---|---|
| `["true"]` / `["false"]` | always / never |
| `["and", [rules]]` / `["or", [rules]]` | all / any of the rules |
| `["has", item, count]` | at least `count` of `item` received |
| `["all", [items]]` / `["any", [items]]` | one of each / one of any |
| `["from", [items], count]` | at least `count` items in total among the list |
| `["loc", location]` | that location is reachable (its region and its rule) |
| `["reg", region]` | that region is reachable |

Reachability is a sweep from `origin`: a region is reached through an exit
whose rule holds, a location when its region is reached and its rule holds.
An event location gives its event item once reached, which can open more,
so repeat until nothing changes.
