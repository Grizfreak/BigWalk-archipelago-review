# Big Walk Setup Guide

## Required Software

- Big Walk on Steam (Windows, or Linux and the Steam Deck through Proton). Every player needs their own copy.
- The Big Walk Archipelago mod, from
  [its releases](https://github.com/Grizfreak/BigWalk-archipelago/releases).
  It is an all-in-one zip: BepInEx is inside it, so there is nothing to
  install separately.
- The Big Walk apworld, `bigwalk.apworld`, from the same release.
- Archipelago 0.6.7 or newer.

There is no separate client to run while you play: the mod talks to
Archipelago from inside the game.

## About BepInEx

The mod is a BepInEx plugin, so the game needs BepInEx to load it, and not
just any BepInEx. It needs **BepInEx 6, the Unity IL2CPP x64 build**, and
specifically **`6.0.0-be.781`**, the build the mod is compiled and tested
against.

The release zip contains that exact build. If the game folder already has
another BepInEx in it, replace it with the one from the zip: builds are not
interchangeable.

The build is published at
[builds.bepinex.dev](https://builds.bepinex.dev/projects/bepinex_be) as
`BepInEx-Unity.IL2CPP-win-x64` under build 781, if you ever need it on its
own.

## Installing the mod

**Everyone playing installs it, not just the host.** See "Playing together"
below for why.

1. Close the game.
2. Open the game's install folder. On Steam: right-click **Big Walk** →
   **Manage** → **Browse local files**. It is the folder containing
   `Big Walk.exe`.
3. Extract the whole zip in there, merging folders. Next to `Big Walk.exe`
   you should end up with `winhttp.dll`, `doorstop_config.ini`, `dotnet/` and
   `BepInEx/`.
4. Launch the game **through Steam**. Double-clicking the executable does not
   always load BepInEx.
5. The first launch is slow, a minute or two of black screen while BepInEx
   downloads the Unity libraries it needs and builds its cache, so be online
   for it.

To check it worked, `BepInEx/LogOutput.log` should contain a line reading
`Big Walk Archipelago v<version> loaded (build …).`, the version being the zip's, the build is a short code that
tells two copies of the mod apart.

**On Linux or the Steam Deck (Proton):** Proton loads its own `winhttp.dll` rather than BepInEx's, and the mod
never starts. In Steam, right-click **Big Walk** → **Properties** → **General**, and set the **Launch options** to
`WINEDLLOVERRIDES="winhttp=n,b" %command%`. The zip holds the Windows build of BepInEx, so the game must run
through Proton. The install folder and `BepInEx/LogOutput.log` are where they are on Windows.

Everyone must run the same build of the mod and the same game version: a
guest on another build ignores its host. To uninstall, delete the four things
you added; nothing in the game's own files is ever modified.

## Installing the APWorld

Place `bigwalk.apworld` in the `custom_worlds` folder of your Archipelago
installation. Only whoever generates the seed needs it, and only one copy, in
`custom_worlds`.

## Configuring your YAML file

### What is a YAML file and why do I need one?

Your YAML file contains a set of configuration options which provide the
generator with information about how it should generate your game. Each
player of a multiworld will provide their own YAML file. This setup allows
each player to enjoy an experience customized for their taste, and different
players in the same multiworld can all have different options.

### Where do I get a YAML file?

Once the apworld is installed, you can generate one with the **Generate
Template Options** button in the Archipelago Launcher. It appears in
`Players/Templates` as `Big Walk.yaml`, with every option documented in the
comments. Copy it into `Players`, not `Templates`, and edit it there.

If the file is missing from `Players/Templates`, the apworld is not where
Archipelago is looking. Check `custom_worlds` again.

### One YAML per co-op group

A co-op group shares one Archipelago slot, so they fill in **one** YAML
between them, with one slot name.

## Joining a MultiWorld Game

### Generating a game

Put every player's `.yaml` into the `Players` folder and run
`ArchipelagoGenerate.exe`, or press **Generate** in the Launcher. The result
lands in `output` as a `.zip`.

### Hosting a game

Either run `ArchipelagoServer.exe` on that zip and host it yourself, or
upload it to [archipelago.gg/uploads](https://archipelago.gg/uploads), which
gives you a room and a port. The website does not need the apworld installed
to host a game that is already generated.

### Connecting from the game

Only the host of the co-op session connects to Archipelago.

1. From the main menu, start hosting a game.
2. The mod adds one field to the hosting screen and relabels two:
   - **Slot name**: your slot name in the multiworld, exactly as in the
     YAML. It is also the save's own name, so it can only be typed when the
     save is created: once it exists the field is locked, because renaming it
     would point the save at a different Archipelago slot.
   - **Archipelago password**: the room's password, or leave it empty: the
     mod makes the field optional.
   - **Archipelago host**: the server, as `host:port`, for example
     `archipelago.gg:38281`. It is prefilled with `archipelago.gg:` the first
     time and remembers what you typed afterwards.
3. Press **Test connection**. It logs in once, throws the session away and
   says what happened: connected under your slot name, or why not, an
   unknown slot name, a wrong password and a server that never answered are
   told apart. It never stops you continuing.
4. Continue. Your co-op partners join your session the usual way, with
   nothing to fill in.

An **AP : ON / AP : OFF** switch sits beside Continue. Switched off, the host
field greys out and the two fields above go back to being the game's own save
name and session password, which is what they are when nothing is
connecting.

The host's screen carries a line in the corner: the Archipelago connection,
for the whole session, with the last few checks sent and items received
underneath it. It reads `Archipelago: off` when the switch is off.

What it cannot show is the rest of the multiworld, who received what you
sent, or what anyone else is doing. Keep Archipelago's Text Client open
beside the game for that.

### Universal Tracker

Big Walk supports Universal Tracker, which lists what is in logic right now
rather than only what you have already sent. Put its `tracker.apworld` in
`custom_worlds` beside `bigwalk.apworld` and connect it to the room with your
slot name.

You do not need your YAML for it, and you do not need to be the person who
generated the seed: the tracker asks the server for this slot's settings and
rebuilds the world from those.

## Playing together

Big Walk is co-op, and **a whole co-op group shares one Archipelago slot**:
you share one world and one save, so you share one set of checks.

**You cannot play solo.** Depositing a gourd in a monument and breaking either
bell both need two players acting at once.

**Two people cannot take two separate slots in the same game either.** They
would need two separate sessions, and then neither could deposit or ring
anything.

### Who does what

- **The host** fills in the three fields on the hosting screen and is the
  only one who talks to Archipelago. Their save owns everything: the checks,
  the items received, the filled monuments.
- **Everyone else** joins the session the normal way. No address, no slot
  name, no password.

### Why the others need the mod too

Some of what the mod does reaches everyone by itself: doors and shortcuts
replicate like any other game state, a check is reported by the host whoever
made it, and the vanilla props removed for the filler items are removed for
everybody.

Most of what a player *sees*, though, their own copy of the mod has to build.
A player without it:

- sees none of the gourds or gadgets the multiworld sends, they exist for
  everyone else and are simply not there on that player's screen, and sees
  the big keys all alike, untinted;
- gets no Archipelago overlay and hears no radio music, even once it has been
  granted;
- does not see the purple postgame gourds on the map, and walks into the
  sphere on the way to the true ending that the host walks straight through.

### The mod's settings, in the game

**Settings > Archipelago**, last in the game's Settings, from the main menu or the pause menu: the
overlay's text size, the host's DeathLink switch and amnesty, **Gentle effects** (hides the traps'
flashes on your screen, for photosensitive players) and the **gourd name** (the host names the gourds
for everyone). The whole list, and who can change what, is on the game's page, "In-game settings".
They live in the mod's own file, `BepInEx/config/com.grizfreak.bigwalk.archipelago.cfg`, which can be
edited by hand with the game closed.

### Always keep the same host

The save lives on the host's machine, and with it the checks, the received
items and the monuments you have filled. If someone else hosts later, that
save has none of it: the mod would reconnect to the same slot with a blank
save, replay the whole item history, and the deposits you had already made
would be gone. Pick a host at the start and stay with them.

## If you get stuck somewhere unreachable

The **resync station** in the hub (and in each tower with
`tower_resync_stations`) brings everything Archipelago has given you that is
loose back in front of it. Deposited gourds and placed keys are never touched.
Only the host can press it, unless `guests_can_resync` is on.

If a save is lost altogether, start a new one and reconnect to the same slot.
The server, not the save file, owns the list of items you have received, so
reconnecting replays all of them. The one thing that does not come back
automatically is gourds you had already deposited: those reappear at the hub
and you have to walk them back to a monument. Since deposits are counted
globally, it does not matter which one.
