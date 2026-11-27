"""A UDP relay on 127.0.0.1 that delays, drops, duplicates and reorders packets, for loopback guests."""

from __future__ import annotations

import argparse
import asyncio
import json
import random
import sys
import threading
import time
from dataclasses import asdict, dataclass, field
from pathlib import Path

HERE = Path(__file__).resolve().parent
PRESETS_FILE = HERE / "netsim-presets.json"
LOG_DIR = HERE / "netsim-logs"

# A packet that would wait longer than this behind --rate is dropped, as a full router queue would.
MAX_QUEUE_SECONDS = 0.5

# Two packets in a row leave at least this far apart, so they leave in order.
TIE_SECONDS = 1e-9

# A guest silent this long is forgotten: a guest that comes back joins from a new port.
IDLE_SECONDS = 30.0

# Windows' Proactor loop stops reading a UDP socket after one error; the Selector loop does not.
LOOP_FACTORY = asyncio.SelectorEventLoop if sys.platform == "win32" else None


@dataclass
class Settings:
    latency: float = 0.0  # ms, one way
    jitter: float = 0.0  # ms, plus or minus
    loss: float = 0.0  # %
    dup: float = 0.0  # %
    reorder: float = 0.0  # %
    rate: float = 0.0  # kbit/s, 0 for no limit


@dataclass
class Stats:
    received: int = 0
    sent: int = 0
    lost: int = 0
    cut: int = 0
    queue_full: int = 0
    duplicated: int = 0
    delayed: int = 0
    reordered: int = 0

    def add(self, other: Stats) -> None:
        for name in self.__dataclass_fields__:
            setattr(self, name, getattr(self, name) + getattr(other, name))


class Link:
    """One direction of one guest: decides when each packet leaves, from its own seeded random."""

    def __init__(self, settings: Settings, seed: int) -> None:
        self.settings = settings
        self.random = random.Random(seed)
        self.stats = Stats()
        self._free_at = 0.0  # when --rate has sent the last packet queued
        self._last_departure = 0.0

    def schedule(self, size: int, now: float, cut: bool = False) -> list[float]:
        """Departure times for one packet: none if dropped, two if duplicated."""
        s = self.settings
        self.stats.received += 1
        # Drawn before any drop, so one seed gives the same pattern whatever is cut.
        lost = self.random.random() * 100 < s.loss
        duplicated = self.random.random() * 100 < s.dup
        reordered = self.random.random() * 100 < s.reorder
        jitter = self.random.uniform(-s.jitter, s.jitter) if s.jitter else 0.0
        if cut:
            self.stats.cut += 1
            return []
        if lost:
            self.stats.lost += 1
            return []

        start = now
        if s.rate > 0:
            begin = max(now, self._free_at)
            if begin - now > MAX_QUEUE_SECONDS:
                self.stats.queue_full += 1
                return []
            self._free_at = begin + size * 8 / (s.rate * 1000)
            start = self._free_at

        departure = start + max(0.0, s.latency + jitter) / 1000
        if reordered:
            # Held back past the next packets, which then overtake it.
            departure += max(s.jitter, 10.0) / 1000
            self.stats.reordered += 1
        else:
            # Jitter alone keeps the order; a tie would let asyncio's timer heap swap two packets.
            departure = max(departure, self._last_departure + TIE_SECONDS)
            self._last_departure = departure

        if departure > now:
            self.stats.delayed += 1
        if duplicated:
            self.stats.duplicated += 1
            copy = departure + TIE_SECONDS
            if not reordered:
                self._last_departure = copy
            return [departure, copy]
        return [departure]


class Relay:
    """Listens on one port and keeps one upstream socket per guest, so the host tells them apart."""

    def __init__(self, listen: tuple[str, int], target: tuple[str, int], settings: Settings,
                 seed: int, max_guests: int = 3, log=print) -> None:
        self.listen, self.target, self.settings, self.seed = listen, target, settings, seed
        self.max_guests = max_guests
        self.log = log
        self.cut = False
        self.guests: dict[tuple, Guest] = {}
        self.retired = (Stats(), Stats())
        self._front: asyncio.DatagramTransport | None = None
        self._count = 0
        self.socket_errors = 0

    def on_socket_error(self, error: Exception) -> None:
        self.socket_errors += 1
        if self.socket_errors == 1:
            self.log(f"socket error, still relaying (a guest gone?): {error!r}")

    @property
    def address(self) -> tuple[str, int]:
        return self._front.get_extra_info("sockname")[:2]

    async def start(self) -> None:
        loop = asyncio.get_running_loop()
        self._front, _ = await loop.create_datagram_endpoint(
            lambda: _Protocol(self._from_guest, self.on_socket_error), local_addr=self.listen)

    def close(self) -> None:
        for guest in list(self.guests.values()):
            guest.close()
        if self._front:
            self._front.close()

    def set_cut(self, on: bool) -> None:
        if on != self.cut:
            self.cut = on
            self.log(f"cut {'ON: every packet is dropped' if on else 'OFF: packets flow again'}")

    def totals(self) -> tuple[Stats, Stats]:
        up, down = Stats(), Stats()
        for guest in self.guests.values():
            up.add(guest.up.stats)
            down.add(guest.down.stats)
        up.add(self.retired[0])
        down.add(self.retired[1])
        return up, down

    def forget_idle(self, now: float) -> None:
        for source, guest in list(self.guests.items()):
            if now - guest.last_seen > IDLE_SECONDS:
                self.retired[0].add(guest.up.stats)
                self.retired[1].add(guest.down.stats)
                guest.close()
                del self.guests[source]
                self.log(f"guest from {source[0]}:{source[1]} silent for {IDLE_SECONDS:.0f} s, forgotten")

    def _from_guest(self, data: bytes, source: tuple) -> None:
        guest = self.guests.get(source)
        if guest is None:
            if len(self.guests) >= self.max_guests:
                self.log(f"refused {source[0]}:{source[1]}: already {self.max_guests} guests")
                return
            self._count += 1
            # Each guest's links get their own seeds, so one guest's traffic never shifts another's.
            guest = Guest(self, source, self.seed * 1000 + self._count * 2)
            self.guests[source] = guest
            asyncio.ensure_future(guest.open())
            self.log(f"guest {self._count} from {source[0]}:{source[1]}")
        guest.last_seen = asyncio.get_running_loop().time()
        guest.forward_up(data)

    def deliver_down(self, data: bytes, source: tuple) -> None:
        if self._front and not self._front.is_closing():
            self._front.sendto(data, source)


class Guest:
    def __init__(self, relay: Relay, source: tuple, seed: int) -> None:
        self.relay, self.source = relay, source
        self.up = Link(relay.settings, seed)
        self.down = Link(relay.settings, seed + 1)
        self.last_seen = 0.0
        self._back: asyncio.DatagramTransport | None = None
        self._waiting: list[bytes] = []

    async def open(self) -> None:
        loop = asyncio.get_running_loop()
        self._back, _ = await loop.create_datagram_endpoint(
            lambda: _Protocol(self._from_host, self.relay.on_socket_error),
            remote_addr=self.relay.target)
        for data in self._waiting:
            self._back.sendto(data)
        self._waiting.clear()

    def close(self) -> None:
        if self._back:
            self._back.close()

    def forward_up(self, data: bytes) -> None:
        self._send(self.up, data, self._to_host)

    def _from_host(self, data: bytes, _source: tuple) -> None:
        self._send(self.down, data, lambda d: self.relay.deliver_down(d, self.source))

    def _to_host(self, data: bytes) -> None:
        if self._back is None:
            self._waiting.append(data)
        elif not self._back.is_closing():
            self._back.sendto(data)

    def _send(self, link: Link, data: bytes, deliver) -> None:
        loop = asyncio.get_running_loop()
        now = loop.time()
        # Always through the timer: sent at once, a packet would pass the ones still waiting.
        for departure in link.schedule(len(data), now, self.relay.cut):
            link.stats.sent += 1
            loop.call_at(departure, deliver, data)


class _Protocol(asyncio.DatagramProtocol):
    def __init__(self, on_data, on_error) -> None:
        self.on_data = on_data
        self.on_error = on_error

    def datagram_received(self, data: bytes, addr) -> None:
        self.on_data(data, addr)

    def error_received(self, exc: Exception) -> None:
        # Windows reports a closed port as an error on the next receive: a guest gone, nothing more.
        self.on_error(exc)


def load_presets(path: Path = PRESETS_FILE) -> dict[str, Settings]:
    raw = json.loads(path.read_text(encoding="utf-8"))
    return {name: Settings(**values) for name, values in raw.items() if not name.startswith("_")}


def format_stats(up: Stats, down: Stats) -> str:
    lines = [f"{'':12}{'guest>host':>12}{'host>guest':>12}"]
    for name in Stats.__dataclass_fields__:
        lines.append(f"{name:12}{getattr(up, name):>12}{getattr(down, name):>12}")
    return "\n".join(lines)


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Relays UDP between loopback guests and the host on this PC, with a worse network in between.")
    parser.add_argument("--listen", type=int, default=7778, help="port the guests join (default 7778)")
    parser.add_argument("--target", type=int, default=7777, help="port the host listens on (default 7777)")
    parser.add_argument("--preset", help="a preset of netsim-presets.json; the options below override it")
    parser.add_argument("--latency", type=float, help="one-way delay, in ms")
    parser.add_argument("--jitter", type=float, help="random delay added or removed, in ms")
    parser.add_argument("--loss", type=float, help="packets dropped, in %%")
    parser.add_argument("--dup", type=float, help="packets sent twice, in %%")
    parser.add_argument("--reorder", type=float, help="packets held back behind the next ones, in %%")
    parser.add_argument("--rate", type=float, help="bandwidth each way, in kbit/s (0: no limit)")
    parser.add_argument("--seed", type=int, help="seed of the random draws (default: picked and logged)")
    parser.add_argument("--cut", type=float, default=0, help="length of each full cut, in seconds")
    parser.add_argument("--every", type=float, default=0, help="time between two cuts, in seconds")
    parser.add_argument("--duration", type=float, default=0, help="stop after this many seconds")
    parser.add_argument("--max-guests", type=int, default=3)
    parser.add_argument("--log", type=Path, help="log file (default: tools/netsim-logs/netsim-<time>.log)")
    parser.add_argument("--stop-file", type=Path, help="stop cleanly once this file exists")
    parser.add_argument("--cut-file", type=Path, help="cut the link while this file exists (for scripts)")
    parser.add_argument("--no-keys", action="store_true", help="no keyboard control, even in a console")
    parser.add_argument("--list-presets", action="store_true", help="print the presets and exit")
    args = parser.parse_args(argv)
    if (args.cut > 0) != (args.every > 0) or (args.cut > 0 and args.cut >= args.every):
        parser.error("--cut and --every go together, and the cut must be shorter than the period")
    return args


def settings_from(args: argparse.Namespace, presets: dict[str, Settings]) -> Settings:
    if args.preset and args.preset not in presets:
        raise SystemExit(f"No preset '{args.preset}'. Presets: {', '.join(presets)}")
    settings = Settings(**asdict(presets[args.preset])) if args.preset else Settings()
    for name in Settings.__dataclass_fields__:
        value = getattr(args, name)
        if value is not None:
            setattr(settings, name, value)
    return settings


class Logger:
    def __init__(self, path: Path) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        self.path = path
        self._file = path.open("a", encoding="utf-8")

    def __call__(self, message: str) -> None:
        line = f"{time.strftime('%H:%M:%S')} {message}"
        print(line, flush=True)
        self._file.write(line + "\n")
        self._file.flush()

    def close(self) -> None:
        self._file.close()


def read_keys(loop: asyncio.AbstractEventLoop, on_key) -> None:
    """Reads single keys in a thread: c cuts or restores, s prints the stats, q quits."""
    if sys.platform == "win32":
        import msvcrt
        while True:
            key = msvcrt.getwch().lower()
            loop.call_soon_threadsafe(on_key, key)
    else:
        import termios
        import tty
        fd = sys.stdin.fileno()
        old = termios.tcgetattr(fd)
        try:
            tty.setcbreak(fd)
            while True:
                loop.call_soon_threadsafe(on_key, sys.stdin.read(1).lower())
        finally:
            termios.tcsetattr(fd, termios.TCSADRAIN, old)


async def run(args: argparse.Namespace, relay: Relay, log) -> None:
    await relay.start()
    try:
        await _serve(args, relay, log)
    finally:
        relay.close()


async def _serve(args: argparse.Namespace, relay: Relay, log) -> None:
    stop = asyncio.Event()
    loop = asyncio.get_running_loop()
    started = loop.time()

    def on_key(key: str) -> None:
        if key == "c":
            relay.set_cut(not relay.cut)
        elif key == "s":
            log("stats so far:\n" + format_stats(*relay.totals()))
        elif key == "q":
            stop.set()

    if not args.no_keys and sys.stdin.isatty():
        threading.Thread(target=read_keys, args=(loop, on_key), daemon=True).start()
        log("keys: c cuts or restores the link, s prints the stats, q quits")

    while not stop.is_set():
        elapsed = loop.time() - started
        scheduled = args.every > 0 and elapsed % args.every >= args.every - args.cut
        if args.every > 0 or args.cut_file:
            relay.set_cut(scheduled or bool(args.cut_file and args.cut_file.exists()))
        relay.forget_idle(loop.time())
        if args.duration and elapsed >= args.duration:
            break
        if args.stop_file and args.stop_file.exists():
            log(f"stop file found: {args.stop_file}")
            args.stop_file.unlink(missing_ok=True)
            break
        try:
            await asyncio.wait_for(stop.wait(), 0.1)
        except asyncio.TimeoutError:
            pass


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    presets = load_presets()
    if args.list_presets:
        for name, preset in presets.items():
            print(f"{name:8} {preset}")
        return 0
    settings = settings_from(args, presets)
    seed = args.seed if args.seed is not None else random.randrange(1, 1_000_000)
    path = args.log or LOG_DIR / f"netsim-{time.strftime('%Y%m%d-%H%M%S')}.log"
    log = Logger(path)
    log(f"netsim: guests join 127.0.0.1:{args.listen}, relayed to 127.0.0.1:{args.target}")
    log(f"preset={args.preset or '-'} seed={seed} {settings}"
        + (f" cut={args.cut}s every={args.every}s" if args.every else ""))
    relay = Relay(("127.0.0.1", args.listen), ("127.0.0.1", args.target), settings, seed,
                  args.max_guests, log)
    try:
        asyncio.run(run(args, relay, log), loop_factory=LOOP_FACTORY)
    except KeyboardInterrupt:
        log("stopped with Ctrl+C")
    except OSError as error:
        log(f"could not listen on 127.0.0.1:{args.listen}: {error}")
        log.close()
        return 1
    log("final stats:\n" + format_stats(*relay.totals())
        + (f"\nsocket errors {relay.socket_errors}" if relay.socket_errors else ""))
    log(f"log written to {path}")
    log.close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
