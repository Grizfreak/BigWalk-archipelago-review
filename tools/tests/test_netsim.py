"""Tests of tools/netsim.py against a local UDP echo, without the game."""

from __future__ import annotations

import asyncio
import sys
from pathlib import Path

import pytest

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

import netsim  # noqa: E402
from netsim import Link, Relay, Settings  # noqa: E402


class _Echo(asyncio.DatagramProtocol):
    def connection_made(self, transport) -> None:
        self.transport = transport
        self.sources: set = set()

    def datagram_received(self, data: bytes, addr) -> None:
        self.sources.add(addr)
        self.transport.sendto(b"echo:" + data, addr)


class _Client(asyncio.DatagramProtocol):
    def __init__(self) -> None:
        self.received: asyncio.Queue[bytes] = asyncio.Queue()

    def datagram_received(self, data: bytes, addr) -> None:
        self.received.put_nowait(data)


async def _setup(settings: Settings, seed: int = 1, guests: int = 1):
    loop = asyncio.get_running_loop()
    echo_transport, echo = await loop.create_datagram_endpoint(_Echo, local_addr=("127.0.0.1", 0))
    target = echo_transport.get_extra_info("sockname")[:2]
    relay = Relay(("127.0.0.1", 0), target, settings, seed, log=lambda _m: None)
    await relay.start()
    clients = []
    for _ in range(guests):
        transport, client = await loop.create_datagram_endpoint(_Client, remote_addr=relay.address)
        clients.append((transport, client))
    return echo_transport, echo, relay, clients


async def _receive(client: _Client, count: int, timeout: float = 2.0) -> list[bytes]:
    out = []
    try:
        while len(out) < count:
            out.append(await asyncio.wait_for(client.received.get(), timeout))
    except asyncio.TimeoutError:
        pass
    return out


def _close(echo_transport, relay, clients) -> None:
    for transport, _ in clients:
        transport.close()
    relay.close()
    echo_transport.close()


def test_relays_both_ways():
    async def scenario():
        echo_t, echo, relay, clients = await _setup(Settings())
        transport, client = clients[0]
        transport.sendto(b"hello")
        got = await _receive(client, 1)
        _close(echo_t, relay, clients)
        return got, relay

    got, relay = asyncio.run(scenario(), loop_factory=netsim.LOOP_FACTORY)
    assert got == [b"echo:hello"]
    up, down = relay.totals()
    assert (up.received, up.sent, down.received, down.sent) == (1, 1, 1, 1)


def test_each_guest_has_its_own_port_on_the_host_side():
    async def scenario():
        echo_t, echo, relay, clients = await _setup(Settings(), guests=3)
        for i, (transport, _) in enumerate(clients):
            transport.sendto(f"guest{i}".encode())
        answers = [await _receive(client, 1) for _, client in clients]
        _close(echo_t, relay, clients)
        return answers, echo.sources

    answers, sources = asyncio.run(scenario(), loop_factory=netsim.LOOP_FACTORY)
    assert answers == [[b"echo:guest0"], [b"echo:guest1"], [b"echo:guest2"]]
    assert len(sources) == 3


def test_jitter_alone_keeps_the_order():
    async def scenario():
        echo_t, _echo, relay, clients = await _setup(Settings(latency=5, jitter=5), seed=7)
        transport, client = clients[0]
        for i in range(60):
            transport.sendto(f"{i:03}".encode())
        got = await _receive(client, 60)
        _close(echo_t, relay, clients)
        return got

    got = asyncio.run(scenario(), loop_factory=netsim.LOOP_FACTORY)
    assert got == [f"echo:{i:03}".encode() for i in range(60)]


def test_latency_is_applied_each_way():
    async def scenario():
        echo_t, _echo, relay, clients = await _setup(Settings(latency=60))
        transport, client = clients[0]
        loop = asyncio.get_running_loop()
        start = loop.time()
        transport.sendto(b"ping")
        await _receive(client, 1)
        elapsed = loop.time() - start
        _close(echo_t, relay, clients)
        return elapsed

    # 60 ms out and 60 ms back.
    assert asyncio.run(scenario(), loop_factory=netsim.LOOP_FACTORY) >= 0.115


def _pattern(settings: Settings, seed: int, count: int = 2000) -> list[int]:
    link = Link(settings, seed)
    return [len(link.schedule(100, i * 0.001)) for i in range(count)]


def test_losses_follow_the_seed():
    settings = Settings(loss=10, dup=5)
    first, again, other = _pattern(settings, 42), _pattern(settings, 42), _pattern(settings, 43)
    assert first == again
    assert first != other
    lost = first.count(0) / len(first)
    assert 0.07 < lost < 0.13


def test_delays_follow_the_seed_and_stay_in_bounds():
    settings = Settings(latency=100, jitter=30)
    a = Link(settings, 5)
    b = Link(settings, 5)
    times_a = [a.schedule(100, 0.0)[0] for _ in range(500)]
    times_b = [b.schedule(100, 0.0)[0] for _ in range(500)]
    assert times_a == times_b
    assert all(0.070 - 1e-9 <= t <= 0.130 + 1e-9 for t in times_a)
    assert times_a == sorted(times_a)


def test_reorder_lets_the_next_packets_overtake():
    link = Link(Settings(latency=20, reorder=20), 3)
    departures = [link.schedule(100, i * 0.001)[0] for i in range(200)]
    assert departures != sorted(departures)
    assert link.stats.reordered > 0


def test_a_cut_drops_everything_and_keeps_the_seed():
    settings = Settings(loss=10)
    link = Link(settings, 9)
    cut = [link.schedule(100, 0.0, cut=True) for _ in range(100)]
    assert all(c == [] for c in cut) and link.stats.cut == 100
    # The draws are made during the cut too, so the pattern after it is the same as without it.
    reference = _pattern(settings, 9, 300)[100:]
    assert [len(link.schedule(100, 0.0)) for _ in range(200)] == reference


def test_rate_spaces_the_packets():
    link = Link(Settings(rate=80), 1)  # 80 kbit/s: 1000 bytes take 0.1 s
    departures = [link.schedule(1000, 0.0)[0] for _ in range(4)]
    assert departures == pytest.approx([0.1, 0.2, 0.3, 0.4])
    for _ in range(10):
        link.schedule(1000, 0.0)
    assert link.stats.queue_full > 0


def test_presets_load():
    presets = netsim.load_presets()
    assert {"good", "wifi", "bad", "mobile", "flaky"} <= set(presets)


def test_options_override_the_preset():
    args = netsim.parse_args(["--preset", "bad", "--loss", "0", "--seed", "3"])
    settings = netsim.settings_from(args, netsim.load_presets())
    assert settings.loss == 0 and settings.latency == netsim.load_presets()["bad"].latency


def test_cut_needs_a_period():
    with pytest.raises(SystemExit):
        netsim.parse_args(["--cut", "5"])


def test_a_cut_file_cuts_while_it_exists(tmp_path):
    cut = tmp_path / "cut"

    async def scenario():
        echo_t, _echo, relay, clients = await _setup(Settings())
        transport, client = clients[0]
        args = netsim.parse_args(["--cut-file", str(cut), "--duration", "1.2", "--no-keys"])
        serve = asyncio.ensure_future(netsim._serve(args, relay, lambda _m: None))
        await asyncio.sleep(0.2)
        transport.sendto(b"open")
        first = await _receive(client, 1, 0.5)
        cut.write_text("")
        await asyncio.sleep(0.2)
        transport.sendto(b"cut")
        second = await _receive(client, 1, 0.3)
        cut.unlink()
        await asyncio.sleep(0.2)
        transport.sendto(b"again")
        third = await _receive(client, 1, 0.5)
        await serve
        _close(echo_t, relay, clients)
        return first, second, third

    assert asyncio.run(scenario(), loop_factory=netsim.LOOP_FACTORY) == ([b"echo:open"], [], [b"echo:again"])


def test_a_guest_gone_does_not_stop_the_others():
    # Windows reports the closed port on the next receive; the relay must keep reading.
    async def scenario():
        echo_t, _echo, relay, clients = await _setup(Settings(), guests=1)
        gone, _ = clients[0]
        gone.sendto(b"first")
        await asyncio.sleep(0.1)
        gone.close()
        # The host answers the guest that left, to its closed port.
        for guest in relay.guests.values():
            guest._from_host(b"late", ("127.0.0.1", 0))
        await asyncio.sleep(0.2)
        loop = asyncio.get_running_loop()
        transport, client = await loop.create_datagram_endpoint(_Client, remote_addr=relay.address)
        transport.sendto(b"second")
        got = await _receive(client, 1)
        transport.close()
        _close(echo_t, relay, [])
        return got

    assert asyncio.run(scenario(), loop_factory=netsim.LOOP_FACTORY) == [b"echo:second"]


class _FailsOnce:
    """Stands for the relay's socket: its next receive fails as Windows does for a closed port."""

    def __init__(self, sock) -> None:
        self._sock, self._failed = sock, False

    def recvfrom(self, size):
        if not self._failed:
            self._failed = True
            raise ConnectionResetError(10054, "closed port")
        return self._sock.recvfrom(size)

    def __getattr__(self, name):
        return getattr(self._sock, name)


def test_the_relay_keeps_reading_after_a_socket_error():
    async def scenario():
        echo_t, _echo, relay, clients = await _setup(Settings())
        transport, client = clients[0]
        relay._front._sock = _FailsOnce(relay._front._sock)
        transport.sendto(b"lost")
        await asyncio.sleep(0.1)
        transport.sendto(b"still")
        got = await _receive(client, 2)
        _close(echo_t, relay, clients)
        return got, relay.socket_errors

    got, errors = asyncio.run(scenario(), loop_factory=netsim.LOOP_FACTORY)
    assert errors == 1
    assert got == [b"echo:lost", b"echo:still"]
