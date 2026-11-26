# Mod settings

How to install, generate and connect: [the setup guide](apworld/bigwalk/docs/setup_en.md).

## Settings in the game

The mod adds a category to the game's own Settings, last in the list: **Settings > Archipelago**, from
the main menu or the pause menu. Nothing there is in the YAML.

- **Archipelago text** (each player): the size of the corner overlay's text, Small to Huge.
- **DeathLink** and **DeathLink amnesty** (the host): On follows the YAML; Off sends and receives
  none for this session, whatever the YAML says. The amnesty is the seed's, or 0 to 10 of your own.
- **Gentle effects** (each player, off by default): the traps' flashes are not shown on your screen,
  for players who find them hard to look at. Today that is Big Flare's. Everyone else keeps theirs, and
  nothing else changes, the clock included.
- **Tile pool** (the host): how the drawings of the tiles folder's `pool` are drawn for the icon
  tiles, over the seed's own draw, on the game's own icons only, or off (see
  [CUSTOM-TILES.md](CUSTOM-TILES.md)).
- **Gourd name** (the host): free text, up to 24 characters, applied with Enter or by leaving the
  field. The overlay says it instead of "Gourd" on every player's screen; a guest sees the host's,
  greyed. The server, the Text Client and the other games keep saying "Gourd".

On a guest, the host's settings are greyed ("Set by the host").

## Mod settings

The mod writes `BepInEx/config/com.grizfreak.bigwalk.archipelago.cfg` on its
first launch. Everything in it has a comment; the ones worth knowing are in
the `[Archipelago]` section:

- `Enabled`: turn the connection off entirely. The mod's other features stay
  on and checks are only written to the log. Useful for playing vanilla-ish
  with the shortcuts open.
- `HostPort`: the address remembered from the hosting screen.
- `ShowConnectionStatus`: the whole corner display; `StatusFontSize`,
  `ShowItemFeed` and `NoticeSeconds` tune what it contains.
- `SpawnGourdAtPlayer`, `PutGourdInHands`, who a gourd or gadget received
  mid-game goes to, and whether it lands in their hands or at their feet. It
  goes to a player with free hands, the one given the fewest items this
  session first. The batch rebuilt at the start of a session always goes to
  the hub.
- `GadgetCopiesKept`: how many copies of each received gadget the world keeps
  (10 by default, 0 for no limit). Past that, a reload puts back no more, and a
  new one takes the place of the oldest lying around, never one held, worn or
  stowed. Backpacks, belts and cartons are only limited on reload.
- `ColorBigKeys`: whether received keys are tinted so they stand out from
  the ones sitting at their towers. Received gourds always come in one of six
  colors.

- `GentleEffects`, `DeathLinkEnabled`, `DeathLinkAmnesty`, `GourdName`, what Settings >
  Archipelago above writes; they can be edited by hand with the game closed.

## Your own tiles

The host can put their own drawings on the peg tiles and their own sounds on the sound puzzles,
for everyone in the session, by dropping PNG and WAV files in
`BepInEx/config/BigWalkArchipelago/tiles/`. How to, step by step: [CUSTOM-TILES.md](CUSTOM-TILES.md).
