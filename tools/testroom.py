#!/usr/bin/env python3
"""Rebuilds the apworld, generates a seed from tools/players/ and hosts it for a game test."""

from __future__ import annotations

import argparse
import re
import shutil
import socket
import subprocess
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
PLAYERS = REPO / "tools" / "players"
OUTPUT = REPO / "tools" / "out"

DEFAULT_PORT = 38281


# Lets tools/deathlink.py send server commands through `!admin`: a background room takes no input.
ADMIN_PASSWORD = "bwtest"


# A resumed room keeps its .apsave options, the server password included.
def keep_admin_password(archipelago: Path, apsave: Path) -> None:
    if not apsave.is_file():
        return
    script = (
        "import sys, zlib, pickle\n"
        "path, password = sys.argv[1], sys.argv[2]\n"
        "data = pickle.loads(zlib.decompress(open(path, 'rb').read()))\n"
        "options = data.get('game_options')\n"
        "if options is not None and options.get('server_password') != password:\n"
        "    options['server_password'] = password\n"
        "    open(path, 'wb').write(zlib.compress(pickle.dumps(data)))\n"
        "    print('admin password written into the room save')\n"
    )
    subprocess.run([sys.executable, "-c", script, str(apsave), ADMIN_PASSWORD], cwd=archipelago, check=True)


def find_archipelago(explicit: str | None) -> Path:
    """Finds a source checkout or an installed build: --archipelago, then beside the repo."""
    candidates = []
    if explicit:
        candidates.append(Path(explicit))
    candidates += [REPO.parent / "Archipelago", REPO.parent.parent / "Archipelago"]

    for candidate in candidates:
        if (candidate / "Generate.py").is_file() or (candidate / "ArchipelagoGenerate.exe").is_file():
            return candidate

    sys.exit(
        "Could not find Archipelago. Pass --archipelago <path> to a source checkout "
        "or an installed Archipelago folder."
    )


def run(command: list[str], cwd: Path) -> None:
    print(f"$ {' '.join(str(part) for part in command)}")
    result = subprocess.run(command, cwd=cwd)
    if result.returncode != 0:
        sys.exit(f"Command failed with exit code {result.returncode}.")


def check_apworld(archipelago: Path) -> None:
    linked = archipelago / "worlds" / "bigwalk"
    packaged = archipelago / "custom_worlds" / "bigwalk.apworld"
    for path in (linked, packaged):
        if path.exists():
            print(f"using the world at {path}")
            return
    sys.exit("No Big Walk world in this Archipelago: link or install it first (apworld/README.md).")


def generate(archipelago: Path, yaml_name: str, seed: int | None) -> Path:
    # One YAML per run: these files are alternative setups for one slot, not a multiworld.
    yaml_path = PLAYERS / yaml_name
    if not yaml_path.is_file():
        sys.exit(f"No such player file: {yaml_path}")

    # Keeps tools/out: each seed's .apsave holds the room state a reconnecting save replays.
    OUTPUT.mkdir(parents=True, exist_ok=True)

    staging = OUTPUT / "players"
    if staging.exists():
        shutil.rmtree(staging)
    staging.mkdir()
    shutil.copy2(yaml_path, staging / yaml_path.name)

    command = generator_command(archipelago) + [
        "--player_files_path", str(staging),
        "--outputpath", str(OUTPUT),
    ]
    if seed is not None:
        command += ["--seed", str(seed)]

    # The new zip is the one not there before: the names carry a random seed, not a date.
    before = {path.name for path in OUTPUT.glob("AP_*.zip")}
    run(command, cwd=archipelago)

    produced = [path for path in OUTPUT.glob("AP_*.zip") if path.name not in before]
    if not produced:
        sys.exit("Generation reported success but produced no new archive.")
    if len(produced) > 1:
        # Never expected with one YAML, but picking blind would host the wrong seed.
        sys.exit("Generation produced several archives: " + ", ".join(p.name for p in produced))
    return produced[0]


def generator_command(archipelago: Path) -> list[str]:
    if (archipelago / "Generate.py").is_file():
        return [sys.executable, str(archipelago / "Generate.py")]
    return [str(archipelago / "ArchipelagoGenerate.exe")]


def server_command(archipelago: Path) -> list[str]:
    if (archipelago / "MultiServer.py").is_file():
        return [sys.executable, str(archipelago / "MultiServer.py")]
    return [str(archipelago / "ArchipelagoServer.exe")]


def read_slot_name(yaml_name: str) -> str:
    # The slot name is the YAML's `name:`, which the in-game save must match exactly.
    for line in (PLAYERS / yaml_name).read_text(encoding="utf-8").splitlines():
        if line.startswith("name:"):
            return line.split(":", 1)[1].strip()
    sys.exit(f"No `name:` found in {yaml_name}; cannot tell what to call the save.")


def world_version() -> str:
    """WORLD_VERSION as world.py declares it, so the cheat sheet never goes stale."""
    source = (REPO / "apworld" / "bigwalk" / "world.py").read_text(encoding="utf-8")
    match = re.search(r'^WORLD_VERSION\s*=\s*"([^"]+)"', source, re.MULTILINE)
    return match.group(1) if match else "?"


def port_in_use(port: int) -> bool:
    """Whether something already answers on this port, on either loopback."""
    for family, host in ((socket.AF_INET, "127.0.0.1"), (socket.AF_INET6, "::1")):
        with socket.socket(family, socket.SOCK_STREAM) as probe:
            probe.settimeout(0.5)
            try:
                if probe.connect_ex((host, port)) == 0:
                    return True
            except OSError:
                continue
    return False


def print_cheat_sheet(yaml_name: str, port: int) -> None:
    slot = read_slot_name(yaml_name)
    print()
    print("=" * 70)
    print("In the game's hosting screen:")
    print(f"    SLOT NAME              {slot}   <- the save must have this exact name")
    print("    ARCHIPELAGO PASSWORD   (leave empty)")
    print(f"    HOST ARCHIPELAGO       127.0.0.1:{port}")
    print()
    print("Expected in BepInEx/LogOutput.log:")
    print(f"    [ApRuntime] Connecting to 127.0.0.1:{port} as '{slot}'...")
    print(f"    [ApRuntime] Connected. apworld v{world_version()}, goal=...")
    print()
    print("Useful commands in this server console:")
    print(f"    /send {slot} Gourd                 spawn a gourd at the hub")
    # A feature opens something; its key opens nothing and carries six checks.
    print(f"    /send {slot} Drawbridge            the door: opens live")
    print(f"    /send {slot} Drawbridge Key        the key: opens nothing, carries checks")
    print(f"    /send_location {slot} Cabin Fever Puzzle   mark a check server-side")
    print("    /players                           who is connected")
    print()
    print("The test that matters most: quit the game, relaunch, reconnect.")
    print("no gourd may be spawned twice. That is the item cursor working.")
    print(f"({yaml_name}; Ctrl+C here stops the room.)")
    print("=" * 70)
    print()


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--yaml", default="solo-smoke.yaml",
                        help="player file in tools/players (default: solo-smoke.yaml)")
    parser.add_argument("--port", type=int, default=DEFAULT_PORT)
    parser.add_argument("--seed", type=int, default=None)
    parser.add_argument("--archipelago", default=None,
                        help="path to an Archipelago checkout or install")
    parser.add_argument("--no-host", action="store_true", help="generate but do not host")
    parser.add_argument("--resume", action="store_true",
                        help="re-host the most recent seed instead of generating a new one, "
                             "keeping the room's history so a save can reconnect to it")
    args = parser.parse_args()

    if not args.no_host and port_in_use(args.port):
        sys.exit(f"Port {args.port} is already taken, probably by another room. "
                 f"Pick a free one with --port (and type that port in the game).")

    archipelago = find_archipelago(args.archipelago)
    print(f"Archipelago: {archipelago}")

    if args.resume:
        # A save reconnects only to its own room: its item history lives in the .apsave.
        existing = sorted(OUTPUT.glob("AP_*.zip"), key=lambda p: p.stat().st_mtime)
        if not existing:
            sys.exit(f"Nothing to resume: no seed in {OUTPUT}.")
        archive = existing[-1]
        print(f"resuming: {archive}")
        keep_admin_password(archipelago, archive.with_suffix(".apsave"))
    else:
        check_apworld(archipelago)
        archive = generate(archipelago, args.yaml, args.seed)
        print(f"seed: {archive}")

    if args.no_host:
        return

    print_cheat_sheet(args.yaml, args.port)

    try:
        subprocess.run(
            server_command(archipelago) + [str(archive), "--port", str(args.port),
                                           "--server_password", ADMIN_PASSWORD],
            cwd=archipelago,
        )
    except KeyboardInterrupt:
        print("\nRoom stopped.")


if __name__ == "__main__":
    main()
