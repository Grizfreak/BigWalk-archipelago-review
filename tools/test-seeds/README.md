# Test seeds

The seeds the "Release test" issue asks for. Each file is one test: generate it on its own, then play it as
`mod/README.md` says (seed settings on one PC with a loopback guest, what happens between players on two PCs).

```
python Generate.py --player_files_path <folder with one of these files> --outputpath <out>
```

| File | What to look at in game |
|---|---|
| `t1-narrow-start.yaml` | There is something to do in the first five minutes. Legs, Seat, Carrying and Running do block the carousel, the seats and the megaphones. The zone locks are clear on screen. |
| `t2-start-overlap.yaml` | Legs, the Drawbridge and the First Arch Door are there once each: nothing doubled in the pool (see the spoiler), nothing missing, and neither the `.cfg` nor the save gives the item again on the next start. |
| `t3-all-sanity.yaml` | On the host: no regular stutter, each check sent once (server log), objects switched off or destroyed break nothing, no error repeating in a loop (ErrorFloodWatch). |
| `t4-chaos.yaml` | With three players: a trap during the Gauntlet, a trap during Big Run, a guest who leaves and comes back in the middle of a trap. No false DeathLink from a light puzzle reset, a timed tomato or a door closing again. The `one_player` roulette target does change player. |
| `t5-towers.yaml` | The Black Tower drawn before a monument, with no monument taking gourds yet. A tower's roof waits for its turn; the Black Tower door opens in its turn and stays open for the host and the guest. |
| `t6-objectives.yaml` | With two or three players: the ending stays locked while an objective is missing and opens at the right time. Each "everyone" objective really needs everyone (one player away = not done). |
| `t7-tiles.yaml` | A guest who joins during the game sees the same tiles and hears the same sounds as the host (from the host's folder). Mirrored poses can be played. `tile_thief: chaos` blocks no puzzle. |
| `t8-multiworld.yaml` | Two Big Walk slots and an APQuest slot (a game Archipelago ships, so the CI can generate it too). Ids right on both sides, local and remote items, automatic hints for discovered places, the hint board when points are short, then `!release` and `!collect` at the end. |
| `t9-near-vanilla.yaml` | The mod breaks nothing when it has almost nothing to do: no error message, no ghost item, a normal ending. |
