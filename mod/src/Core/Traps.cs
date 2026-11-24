using System;
using System.Collections.Generic;
using System.Linq;
using BigWalkArchipelago.Core.Net;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Traps, bonuses and DeathLink: the host queues them and tells each machine what to play.
    internal static class Traps
    {
        private const string Tag = "[" + nameof(Traps) + "]";

        // Spacing between queued effects, and a cap so a returning slot gets no burst of traps.
        private const float Spacing = 4f;
        private const int MaxPending = 10;

        // How far from its target a player gathered by Big Meeting lands.
        private const float MeetingRadius = 2.2f;

        // DeathLink: never two sent closer than this, nor two from one player's falls.
        private const float DeathLinkCooldown = 10f;

        private static Dictionary<long, string> _items = new();
        private static int _duration = 30;
        private static bool _spareGauntlet = true;
        private static bool _sendsDeathLink;
        private static bool _receivesDeathLink;
        private static HashSet<string> _triggers = new();
        private static int _tolerance;
        private static string _deathLinkEffect = "drop";
        private static Dictionary<string, int> _roulette = new();
        private static bool _trapOnSend;
        private static bool _trapLink;

        internal static int Duration => _duration;

        internal static void Configure(ApSlotData slot)
        {
            _items = new Dictionary<long, string>(slot.EffectItems);
            _duration = Mathf.Clamp(slot.TrapDuration, 5, 600);
            _spareGauntlet = slot.TrapsSpareTheGauntlet;
            _sendsDeathLink = slot.SendsDeathLink;
            _receivesDeathLink = slot.ReceivesDeathLink;
            _triggers = new HashSet<string>(slot.DeathLinkTriggers);
            _tolerance = Math.Max(0, slot.DeathLinkTolerance);
            _deathLinkEffect = slot.DeathLinkEffect;
            _trapOnSend = slot.DeathLinkTrapOnSend;
            _trapLink = slot.TrapLink;
            _roulette = new Dictionary<string, int>();
            foreach (var pair in slot.DeathLinkRoulette)
            {
                if (Array.IndexOf(TrapEffects.Known, pair.Key) >= 0)
                    _roulette[pair.Key] = pair.Value;
            }
            _missed = 0;
            _deathLinkOnePlayer = slot.DeathLinkTarget == "one_player";
        }

        // The host's Settings > Archipelago can turn DeathLink off or override the amnesty.
        internal static bool DeathLinkOn => ModConfig.DeathLinkEnabled.Value;
        internal static bool ReceivesDeathLink => _receivesDeathLink && DeathLinkOn;
        internal static bool SeedHasDeathLink => _sendsDeathLink || _receivesDeathLink;
        internal static int SeedAmnesty => _tolerance;
        internal static int Amnesty => ModConfig.DeathLinkAmnesty.Value >= 0 ? ModConfig.DeathLinkAmnesty.Value : _tolerance;

        // Says once what the host's Settings change from the seed's, so nobody wonders.
        internal static void AnnounceOverrides()
        {
            if (!SeedHasDeathLink)
                return;

            if (!DeathLinkOn)
                Net.ApNotices.Post("DeathLink is off in the Settings (the seed has it on)", warning: true);
            else if (ModConfig.DeathLinkAmnesty.Value >= 0 && ModConfig.DeathLinkAmnesty.Value != _tolerance)
                Net.ApNotices.Post($"DeathLink amnesty {Amnesty} in the Settings (the seed says {_tolerance})", warning: true);
        }

        internal static bool TryResolveItem(long itemId, out string key)
        {
            return _items.TryGetValue(itemId - ApLocationIds.Base, out key);
        }

        private static readonly Queue<string> Pending = new();

        // Whether each pending effect hits one player only (`death_link_target: one_player`).
        private static readonly Queue<bool> PendingSingle = new();

        private static bool _deathLinkOnePlayer;

        // DeathLinks taken per player: the next single one goes to the one with the fewest.
        private static readonly Dictionary<string, int> DeathLinkHits = new();

        // During Fire: the one player it hits, or null for everyone.
        private static PlayerCharacter _onlyPlayer;
        private static float _nextFire;

        // Pending effects are kept in the save, as the item cursor has already counted them.
        private const string PendingPrefix = "ap_trap_pending_";

        // After the world is ready or a player joins, Big Trip and Big Meeting are dropped.
        private const float JoinGrace = 20f;
        private static float _graceUntil;
        private static int _playersSeen;

        // The host's world is ready: the queue as the save has it, and the grace begins.
        internal static void OnWorldReady()
        {
            _graceUntil = Time.unscaledTime + JoinGrace;
            _playersSeen = PlayerCount();
            if (!ModConfig.ArchipelagoEnabled.Value)
                return;

            Pending.Clear();
            PendingSingle.Clear();
            foreach (var key in TrapEffects.Known)
            {
                var count = SaveManager.GetIntValue(PendingPrefix + key, 0, false);
                for (var i = 0; i < count && Pending.Count < MaxPending; i++)
                {
                    Pending.Enqueue(key);
                    PendingSingle.Enqueue(false);
                }
            }

            if (Pending.Count > 0)
                Plugin.Log.LogInfo($"{Tag} {Pending.Count} effect(s) still waiting from the last session.");
        }

        // A save bound to another seed: the effects that seed's items queued are not this one's.
        internal static void ClearLedger()
        {
            Pending.Clear();
            PendingSingle.Clear();
            foreach (var key in TrapEffects.Known)
                SaveManager.SetIntValue(PendingPrefix + key, 0);
        }

        private static int PlayerCount()
        {
            var players = PlayerCharacter.allPlayerCharacters;
            return players != null ? players.Count : 0;
        }

        private static void CountPending(string key, int change)
        {
            if (!ModConfig.ArchipelagoEnabled.Value || !NetworkServer.active)
                return;

            var name = PendingPrefix + key;
            SaveManager.SetIntValue(name, Math.Max(0, SaveManager.GetIntValue(name, 0, false) + change));
        }

        internal static bool Enqueue(string key, string why, bool single = false)
        {
            if (Array.IndexOf(TrapEffects.Known, key) < 0)
            {
                Plugin.Log.LogWarning($"{Tag} '{key}' ({why}) is not an effect this build plays; ignored.");
                return false;
            }

            if (Pending.Count >= MaxPending)
            {
                Plugin.Log.LogInfo($"{Tag} {TrapEffects.DisplayName(key)} ({why}) let go: {MaxPending} already waiting.");
                return false;
            }

            Pending.Enqueue(key);
            PendingSingle.Enqueue(single);
            CountPending(key, +1);
            Plugin.Log.LogInfo($"{Tag} {TrapEffects.DisplayName(key)} queued ({why}); {Pending.Count} waiting.");
            return true;
        }

        internal static void OnDeathLinkReceived(string cause)
        {
            if (!ReceivesDeathLink)
                return;

            ApSounds.DeathLinkAlert();

            ApplyDeathEffect($"received ({cause})", $"DeathLink from {cause}", $"DeathLink from {cause}");
        }

        // `death_link_effect` on `death_link_target`; never sent on as a TrapLink.
        private static void ApplyDeathEffect(string journal, string why, string notice = null)
        {
            var single = _deathLinkOnePlayer;
            var key = _deathLinkEffect switch
            {
                "roulette" => Roulette(single ? SessionWide : null),
                "drop" => TrapEffects.Drop,
                _ => TrapEffects.Knockout,
            };
            SessionJournal.Write("deathlink", $"{journal} -> {TrapEffects.DisplayName(key)}");
            if (notice != null)
                Net.ApNotices.Post($"{notice}: {TrapEffects.DisplayName(key)}");
            Enqueue(key, why + (single ? ", one player" : string.Empty), single);
        }

        // Traps that cannot hit one player: left out of a one-player DeathLink's roulette.
        private static readonly string[] SessionWide = { TrapEffects.Meeting, TrapEffects.Night };

        private static string Roulette(string[] except = null)
        {
            var total = 0;
            foreach (var pair in _roulette)
            {
                if (except == null || Array.IndexOf(except, pair.Key) < 0)
                    total += pair.Value;
            }
            if (total <= 0)
                return TrapEffects.Drop;

            var roll = UnityEngine.Random.Range(0, total);
            foreach (var pair in _roulette)
            {
                if (except != null && Array.IndexOf(except, pair.Key) >= 0)
                    continue;
                roll -= pair.Value;
                if (roll < 0)
                    return pair.Key;
            }

            return TrapEffects.Drop;
        }

        // The traps of the pool, as opposed to the bonuses and the DeathLink knock-out.
        private static readonly string[] PoolTraps =
        {
            TrapEffects.Drop, TrapEffects.Throw, TrapEffects.Trip, TrapEffects.Meeting, TrapEffects.Night,
            TrapEffects.Sleep, TrapEffects.Load, TrapEffects.Mask, TrapEffects.Flare, TrapEffects.BigTime, TrapEffects.Silence, TrapEffects.Slide,
        };

        // Only traps from items are sent, so a trap made by a DeathLink or TrapLink never loops.
        internal static void SendTrapLink(string key, string itemName)
        {
            if (!_trapLink || !NetworkServer.active || Array.IndexOf(PoolTraps, key) < 0)
                return;
            if (ApRuntime.SendTrapLink(itemName))
                Plugin.Log.LogInfo($"{Tag} TrapLink sent: {itemName}.");
        }

        // Other games' trap words, by the nearest Big Walk trap; no match draws from the roulette.
        private static readonly (string key, string[] words)[] TrapLinkWords =
        {
            (TrapEffects.Sleep, new[] { "freeze", "ice", "paraly", "stun", "sleep", "slow", "stop" }),
            (TrapEffects.Drop, new[] { "drop", "fumble", "butterfinger", "disarm", "empty", "damage", "bonk" }),
            (TrapEffects.Throw, new[] { "throw", "launch", "spring", "bounce", "banana", "slip" }),
            (TrapEffects.Trip, new[] { "teleport", "warp", "trip", "tp", "reverse", "random" }),
            (TrapEffects.Night, new[] { "dark", "night", "fog", "blackout", "blind" }),
            (TrapEffects.Mask, new[] { "mask", "screen", "blur", "tiny", "zoom" }),
            (TrapEffects.Flare, new[] { "flare", "flash", "bomb", "explo", "fire", "light" }),
            (TrapEffects.BigTime, new[] { "time", "clock", "timer", "alarm" }),
            (TrapEffects.Silence, new[] { "silence", "mute", "quiet", "deaf", "comms", "voice" }),
            (TrapEffects.Slide, new[] { "slide", "slipp", "skate", "oil", "grease" }),
            (TrapEffects.Load, new[] { "strip", "unequip", "lose", "inventory", "rupee", "item" }),
        };

        internal static void OnTrapLinkReceived(string source, string trapName)
        {
            if (!_trapLink || !NetworkServer.active)
                return;

            var name = trapName.ToLowerInvariant();
            string key = null;
            foreach (var trap in PoolTraps)
            {
                if (name == "big " + trap)
                    key = trap;
            }

            if (key == null)
            {
                foreach (var (candidate, words) in TrapLinkWords)
                {
                    if (Array.Exists(words, word => name.Contains(word)))
                    {
                        key = candidate;
                        break;
                    }
                }
            }

            if (key == null || !_roulette.TryGetValue(key, out var weight) || weight <= 0)
                key = Roulette();

            ApSounds.DeathLinkAlert();
            SessionJournal.Write("traplink", $"received ({source}: {trapName}) -> {TrapEffects.DisplayName(key)}");
            Enqueue(key, $"TrapLink from {source}: {trapName}");
        }

        // Host, every frame. `settled`: the session's startup burst is through (ApRuntime).
        internal static void TickHost(bool settled)
        {
            if (DoorsToReopen.Count > 0)
                TickDoors();
            if (BreakSeats.Count > 0)
                TickBreakSeats();
            if (TomatoStarts.Count > 0)
                StartDueTomatoes();
            if (Tomatoes.Count > 0)
                ClearTomatoes();
            if (Helmets.Count > 0)
                TickHelmets();
            if (RetiringHelmets.Count > 0)
                RetireHelmets();

            var players = PlayerCount();
            if (players > _playersSeen)
            {
                _graceUntil = Time.unscaledTime + JoinGrace;
                Plugin.Log.LogInfo($"{Tag} A player joined: any Big Trip or Big Meeting in the next {JoinGrace:0}s is dropped.");
            }

            _playersSeen = players;

            if (Pending.Count == 0 || !settled || !WorldManager.isReadyForEffects || Time.unscaledTime < _nextFire)
                return;

            if (LocalPlayer.Find() == null)
                return;

            var key = Pending.Dequeue();
            var single = PendingSingle.Count > 0 && PendingSingle.Dequeue();
            CountPending(key, -1);
            if (Time.unscaledTime < _graceUntil && (key == TrapEffects.Trip || key == TrapEffects.Meeting))
            {
                Plugin.Log.LogInfo($"{Tag} {TrapEffects.DisplayName(key)} dropped: players are still arriving.");
                return;
            }

            _nextFire = Time.unscaledTime + Spacing;
            if (single && Array.IndexOf(SessionWide, key) < 0)
            {
                _onlyPlayer = LeastHit();
                try
                {
                    Fire(key);
                }
                finally
                {
                    _onlyPlayer = null;
                }
            }
            else
            {
                Fire(key);
            }
        }

        // The player the fewest DeathLinks have hit this session, ties at random; counted as hit.
        private static PlayerCharacter LeastHit()
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null || players.Count == 0)
                return null;

            PlayerCharacter chosen = null;
            var fewest = int.MaxValue;
            var ties = 0;
            foreach (var player in players)
            {
                if (player == null)
                    continue;
                var hits = DeathLinkHits.TryGetValue(player.gameObject.name, out var h) ? h : 0;
                if (hits < fewest)
                {
                    fewest = hits;
                    chosen = player;
                    ties = 1;
                }
                else if (hits == fewest && UnityEngine.Random.Range(0, ++ties) == 0)
                {
                    chosen = player;
                }
            }

            if (chosen != null)
            {
                DeathLinkHits[chosen.gameObject.name] = fewest + 1;
                Plugin.Log.LogInfo($"{Tag} This DeathLink hits {chosen.gameObject.name} only ({fewest + 1} so far).");
            }
            return chosen;
        }

        internal static void Fire(string key)
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogInfo($"{Tag} Only the host fires an effect.");
                return;
            }

            SessionJournal.Write("trap", TrapEffects.DisplayName(key));
            switch (key)
            {
                case TrapEffects.Trip:
                    FireTrip(TrapEffects.Trip);
                    break;
                case TrapEffects.Meeting:
                    FireMeeting();
                    break;
                case TrapEffects.Load:
                    FireLoad();
                    break;
                case TrapEffects.Mask:
                    FireMask();
                    break;
                case TrapEffects.Reset:
                    FireReset();
                    break;
                case TrapEffects.Break:
                    // A folding chair before each player; each machine sits its own player on it.
                    foreach (var walker in Walkers())
                    {
                        // On the ground, upright: dropped from chest height, the chairs fell over.
                        var body = walker.Player.GetComponentInChildren<Rigidbody>();
                        var at = body != null ? body.position : walker.Position;
                        var yaw = body != null ? body.rotation.eulerAngles.y : walker.Yaw;
                        var ground = float.MaxValue;
                        var from = at + Vector3.up * 1f;
                        foreach (var hit in Physics.RaycastAll(from, Vector3.down, 6f))
                        {
                            if (hit.collider == null || hit.collider.isTrigger || hit.collider.transform.IsChildOf(walker.Player.transform)
                                || hit.distance >= ground)
                                continue;
                            ground = hit.distance;
                            at = hit.point;
                        }
                        var chair = GadgetItemSpawner.SpawnAt(GadgetKind.Chair, at, Quaternion.Euler(0f, yaw, 0f));
                        if (chair == null)
                        {
                            Plugin.Log.LogInfo($"{Tag} Big Break: no chair for {walker.Player.gameObject.name}.");
                            continue;
                        }
                        Send(walker, key, chair.transform.position, 0f);
                        BreakSeats.Add((walker.Player, chair, Time.unscaledTime + BreakServerDelay));
                    }
                    break;
                case TrapEffects.BigTime:
                    // A clock for everyone: in the hands, or at their feet when they hold one.
                    foreach (var walker in Walkers())
                    {
                        if (GadgetItemSpawner.SpawnFor(GadgetKind.Clock, walker.Player) != null)
                            Plugin.Log.LogInfo($"{Tag} Big Time: a clock for {walker.Player.gameObject.name}.");
                    }
                    break;
                case TrapEffects.Flare:
                {
                    // Every machine readies its colours, then the host's shots follow.
                    var walkers = Walkers();
                    foreach (var walker in walkers)
                        Send(walker, key, null, 0f);
                    BigFlare.FireOn(walkers.ConvertAll(w => w.Player));
                    break;
                }
                case TrapEffects.Air:
                    // A tomato for everyone, then up in the air; those falls send no DeathLink.
                    _knockoutUntil = Time.unscaledTime + GadgetItemSpawner.TomatoSeconds + AirQuiet;
                    foreach (var walker in Walkers())
                    {
                        GiveTomato(walker);
                        Send(walker, key, null, 0f);
                    }
                    break;
                case TrapEffects.Knockout:
                    // Packs off on the host, then each machine drops its player: no DeathLink.
                    _knockoutUntil = Time.unscaledTime + KnockoutQuiet;
                    FireLoad();
                    foreach (var walker in Walkers())
                        Send(walker, key, null, 0f);
                    break;
                default:
                    foreach (var walker in Walkers())
                        Send(walker, key, null, 0f);
                    if (Array.IndexOf(Lasting, key) >= 0 && _onlyPlayer == null)
                        ActiveUntil[key] = Time.unscaledTime + _duration;
                    break;
            }
        }

        // Big Reset: per group of players, the nearest unsolved puzzle's door is shut and reopened.
        private const float ResetGroup = 25f;
        private const float ResetRadius = 40f;

        // Resetting every nearby state freed the gourds through their vices: only the door cycles.
        private const float ResetShutSeconds = 0.4f;

        private static bool UnderGourdHolder(Transform t)
        {
            for (; t != null; t = t.parent)
            {
                if (t.name.IndexOf("GourdVice", StringComparison.Ordinal) >= 0 || t.name.IndexOf("GourdValet", StringComparison.Ordinal) >= 0)
                    return true;
            }
            return false;
        }
        private static readonly List<(TrackedPeckState state, int was, float at)> DoorsToReopen = new();

        private static void CycleDoor(string puzzle, Vector3 gourd, float best, int players)
        {
            LobbyDoor door = null;
            var nearest = ResetRadius;
            foreach (var candidate in UnityEngine.Object.FindObjectsByType<LobbyDoor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                // The gourd's vice is a LobbyDoor too: shut, it lets the gourd go.
                if (candidate == null || candidate.peckSystem.peckSystem == null || UnderGourdHolder(candidate.transform))
                    continue;
                var distance = Vector3.Distance(candidate.transform.position, gourd);
                if (distance < nearest)
                {
                    nearest = distance;
                    door = candidate;
                }
            }
            if (door == null)
            {
                Plugin.Log.LogInfo($"{Tag} Big Reset: {puzzle} ({best:F0} m from {players} player(s)) has no door within {ResetRadius:F0} m.");
                return;
            }
            var state = door.peckSystem.peckSystem.TryCast<TrackedPeckState>();
            if (state == null)
            {
                Plugin.Log.LogInfo($"{Tag} Big Reset: {puzzle}'s door {door.name} has no state of its own.");
                return;
            }
            var was = state.currentPeckContext.state;
            var shut = door.peckAndDoorStatesAreInverse ? 1 : 0;
            Plugin.Log.LogInfo($"{Tag} Big Reset: {puzzle} ({best:F0} m from {players} player(s)), door {door.name} at {nearest:F0} m, state {was}, shut at {shut}.");
            state.SetState(shut);
            DoorsToReopen.Add((state, was, Time.unscaledTime + ResetShutSeconds));
        }

        private static void TickDoors()
        {
            for (var i = DoorsToReopen.Count - 1; i >= 0; i--)
            {
                var (state, was, at) = DoorsToReopen[i];
                if (Time.unscaledTime < at)
                    continue;
                DoorsToReopen.RemoveAt(i);
                if (state != null && state.currentPeckContext.state != was)
                    state.SetState(was);
            }
        }

        private static void FireReset()
        {
            var gourds = new List<(Vector3 at, string name)>();
            foreach (var gourd in UnityEngine.Object.FindObjectsByType<RewardGourd>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var prop = gourd != null ? gourd.prop : null;
                if (prop == null || !GourdRegistry.TryGetLocationId(prop.saveablePropName, out var location) || CheckTracker.IsReported(location))
                    continue;
                gourds.Add((gourd.transform.position, prop.saveablePropName.ToString()));
            }
            if (gourds.Count == 0)
            {
                Plugin.Log.LogInfo($"{Tag} Big Reset: no puzzle still holds its gourd.");
                return;
            }

            var left = Walkers().ConvertAll(w => w.Position);
            var done = new HashSet<string>();
            while (left.Count > 0)
            {
                var group = new List<Vector3> { left[0] };
                left.RemoveAt(0);
                for (var grew = true; grew;)
                {
                    grew = false;
                    for (var i = left.Count - 1; i >= 0; i--)
                    {
                        if (!group.Exists(p => Vector3.Distance(p, left[i]) < ResetGroup))
                            continue;
                        group.Add(left[i]);
                        left.RemoveAt(i);
                        grew = true;
                    }
                }

                var nearest = gourds[0];
                var best = float.MaxValue;
                foreach (var gourd in gourds)
                {
                    foreach (var player in group)
                    {
                        var distance = Vector3.Distance(player, gourd.at);
                        if (distance < best)
                        {
                            best = distance;
                            nearest = gourd;
                        }
                    }
                }
                if (!done.Add(nearest.name))
                    continue;

                CycleDoor(nearest.name, nearest.at, best, group.Count);
            }
        }

        // The server seats each player, as a player's own action seats them on their machine only.
        private const float BreakServerDelay = 0.6f;
        private static readonly List<(PlayerCharacter player, Prop chair, float at)> BreakSeats = new();

        private static void TickBreakSeats()
        {
            for (var i = BreakSeats.Count - 1; i >= 0; i--)
            {
                var (player, chair, at) = BreakSeats[i];
                if (Time.unscaledTime < at)
                    continue;
                BreakSeats.RemoveAt(i);
                try
                {
                    var pose = chair != null ? chair.GetComponentInChildren<PlayerPose>() : null;
                    var networking = player != null ? player.playerNetworking : null;
                    if (pose == null || networking == null)
                        continue;
                    networking.ServerEnterPoseAuto(pose.shellReference);
                    Plugin.Log.LogInfo($"{Tag} Big Break: {player.gameObject.name} sat by the server (ticket {pose.ticket}).");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} Big Break: {ex.Message}");
                }
            }
        }

        // Effects that last, so a player who joins while one runs gets it for the time left.
        private static readonly string[] Lasting = { TrapEffects.Speed, TrapEffects.Jump, TrapEffects.Sleep, TrapEffects.Slide, TrapEffects.Silence };
        private static readonly Dictionary<string, float> ActiveUntil = new();

        // From the ModChannel, when a guest says hello.
        internal static void OnGuestJoined(NetworkConnection connection)
        {
            foreach (var pair in ActiveUntil)
            {
                var left = Mathf.CeilToInt(pair.Value - Time.unscaledTime);
                if (left < 1)
                    continue;

                Plugin.Log.LogInfo($"{Tag} Guest #{connection.connectionId} joins during {TrapEffects.DisplayName(pair.Key)}: {left}s of it.");
                ModChannel.SendEffect(connection, pair.Key, left, null, 0f);
            }
        }

        private sealed class Walker
        {
            internal PlayerCharacter Player;
            internal NetworkConnectionToClient Connection;
            internal bool Local;
            internal Vector3 Position;
            internal float Yaw;
        }

        // Every player with the connection it plays through; the host's own is connection 0.
        private static List<Walker> Walkers()
        {
            var all = AllWalkers();
            if (_onlyPlayer != null)
                all.RemoveAll(walker => walker.Player != _onlyPlayer);
            return all;
        }

        private static List<Walker> AllWalkers()
        {
            var walkers = new List<Walker>();
            foreach (var connection in NetworkServer.connections.Values)
            {
                var identity = connection?.identity;
                var player = identity != null ? identity.GetComponent<PlayerCharacter>() : null;
                if (player == null)
                    continue;

                walkers.Add(new Walker
                {
                    Player = player,
                    Connection = connection,
                    Local = connection.connectionId == NetworkConnection.LocalConnectionId,
                    Position = player.transform.position,
                    Yaw = player.transform.eulerAngles.y,
                });
            }

            return walkers;
        }

        // The tomato leaves the world this long after its pop, as the sound dies with it.
        private const float TomatoLinger = 0.1f;
        private const float TomatoSoundSeconds = 1.5f;
        private static readonly List<KeyValuePair<Prop, float>> Tomatoes = new();
        private const float TomatoStartDelay = 0.3f;
        private static readonly List<(Prop tomato, PlayerCharacter player, float at)> TomatoStarts = new();

        private static void StartDueTomatoes()
        {
            for (var i = TomatoStarts.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime < TomatoStarts[i].at)
                    continue;

                var start = TomatoStarts[i];
                TomatoStarts.RemoveAt(i);
                if (start.tomato != null && start.tomato.gameObject != null)
                    StartTomato(start.tomato, start.player);
            }
        }

        private static void GiveTomato(Walker walker)
        {
            var tomato = GadgetItemSpawner.SpawnFor(GadgetKind.Pomodoro, walker.Player);
            if (tomato == null)
                return;

            // Set going once in the hands: the hand-over after the spawn would cut its tick.
            var startAt = Time.unscaledTime + TomatoStartDelay;
            TomatoStarts.Add((tomato, walker.Player, startAt));
            Tomatoes.Add(new KeyValuePair<Prop, float>(tomato, startAt + GadgetItemSpawner.TomatoSeconds + TomatoLinger));
        }

        // With the tomato as the context's prop, or its tick and pop have nowhere to play.
        private static void StartTomato(Prop tomato, PlayerCharacter player)
        {
            foreach (var peckSwitch in tomato.GetComponentsInChildren<PeckSwitch>(true))
            {
                if (peckSwitch == null || peckSwitch.gameObject.name != "OnSetSwitch")
                    continue;

                var context = new PeckContext(0)
                {
                    propIdentity = tomato.GetComponent<NetworkIdentity>(),
                    playerIdentity = player != null ? player.netIdentity : null,
                };
                peckSwitch.Peck(context);
                Plugin.Log.LogInfo($"{Tag} Tomato set going for {GadgetItemSpawner.TomatoSeconds:0}s.");
                return;
            }

            Plugin.Log.LogWarning($"{Tag} The tomato has no OnSetSwitch; it will not pop.");
        }

        // Host, every frame: the tomatoes that have popped, out of hands and out of the world.
        private static readonly HashSet<int> Released = new();

        private static void ClearTomatoes()
        {
            for (var i = Tomatoes.Count - 1; i >= 0; i--)
            {
                if (Time.unscaledTime < Tomatoes[i].Value)
                    continue;

                var tomato = Tomatoes[i].Key;
                Tomatoes.RemoveAt(i);
                try
                {
                    if (tomato == null || tomato.gameObject == null)
                        continue;

                    // First time due: out of the hands, back in the list for when its pop is over.
                    if (!Released.Contains(tomato.GetInstanceID()))
                    {
                        Released.Add(tomato.GetInstanceID());
                        ReceivedItemSpawner.ReleaseFromHands(tomato);
                        Tomatoes.Add(new KeyValuePair<Prop, float>(tomato, Time.unscaledTime + TomatoSoundSeconds));
                        continue;
                    }

                    Released.Remove(tomato.GetInstanceID());
                    NetworkServer.Destroy(tomato.gameObject);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} Could not take a tomato away: {ex.Message}");
                }
            }
        }

        private static void Send(Walker walker, string key, Vector3? destination, float yaw)
        {
            if (destination != null && (key == TrapEffects.Trip || key == TrapEffects.Meeting))
                GiveTomato(walker);

            if (walker.Local)
                TrapEffects.Play(key, _duration, destination, yaw);
            else
                ModChannel.SendEffect(walker.Connection, key, _duration, destination, yaw);
        }

        // Guests say whether a puzzle's mask is on their screen (ModChannel, KindStatus).
        private static readonly Dictionary<int, bool> GuestMasked = new();

        internal static void NoteGuestMasked(int connectionId, bool masked)
        {
            if (GuestMasked.TryGetValue(connectionId, out var before) && before == masked)
                return;

            GuestMasked[connectionId] = masked;
            Plugin.Log.LogInfo($"{Tag} Guest #{connectionId}: screen mask {(masked ? "on" : "off")}.");
        }

        private static bool IsMasked(Walker walker)
        {
            return walker.Local
                ? TrapEffects.LocalMasked
                : GuestMasked.TryGetValue(walker.Connection.connectionId, out var masked) && masked;
        }

        // Whether Big Trip and Big Meeting leave this player where they are.
        private static bool IsSpared(Walker walker, out string why)
        {
            why = null;
            if (IsMasked(walker))
                why = "a puzzle's mask is on their screen";
            else if (_spareGauntlet && Gauntlet.Contains(walker.Position))
                why = "in the Silent Gauntlet";
            return why != null;
        }

        private static void FireTrip(string key)
        {
            var walkers = Walkers();
            var spots = TripSpots.Candidates();
            if (spots.Count == 0)
            {
                Plugin.Log.LogInfo($"{Tag} Big Trip: no place to send anyone yet.");
                Net.ApNotices.Post("Big Trip: nowhere to go yet", warning: true);
                return;
            }

            // A different place for each player, none of them where that player already is.
            var shuffled = spots.OrderBy(_ => UnityEngine.Random.value).ToList();
            var used = new HashSet<int>();
            var sentAny = false;
            foreach (var walker in walkers)
            {
                if (IsSpared(walker, out var why))
                {
                    Plugin.Log.LogInfo($"{Tag} Big Trip spares {walker.Player.gameObject.name}: {why}.");
                    continue;
                }

                var pick = -1;
                for (var i = 0; i < shuffled.Count && pick < 0; i++)
                {
                    if (!used.Contains(i) && Vector3.Distance(shuffled[i].Point, walker.Position) > 40f)
                        pick = i;
                }

                for (var i = 0; i < shuffled.Count && pick < 0; i++)
                {
                    if (!used.Contains(i))
                        pick = i;
                }

                if (pick < 0)
                    pick = UnityEngine.Random.Range(0, shuffled.Count);

                used.Add(pick);
                Plugin.Log.LogInfo($"{Tag} Big Trip sends {walker.Player.gameObject.name} to {shuffled[pick].Label}.");
                Send(walker, key, shuffled[pick].Point, walker.Yaw);
                sentAny = true;
            }

            if (!sentAny)
                Net.ApNotices.Post("Big Trip: nobody free to move", warning: true);
        }

        private static void FireMeeting()
        {
            var walkers = Walkers();
            var free = walkers.Where(w => !IsSpared(w, out _)).ToList();
            if (walkers.Count < 2 || free.Count < 2)
            {
                Plugin.Log.LogInfo($"{Tag} Big Meeting: {walkers.Count} player(s), {free.Count} free to move; a Big Trip instead.");
                FireTrip(TrapEffects.Trip);
                return;
            }

            var target = free[UnityEngine.Random.Range(0, free.Count)];
            var movers = free.Where(w => w != target).ToList();
            Plugin.Log.LogInfo($"{Tag} Big Meeting around {target.Player.gameObject.name}, {movers.Count} player(s) gathered.");

            var taken = new List<Vector3>();
            for (var i = 0; i < movers.Count; i++)
            {
                var angle = target.Yaw + 360f * (i + 1) / (movers.Count + 1);

                // Nowhere to stand beside them: on them, which the game sorts out by pushing apart.
                var landing = SafeLanding(target.Position, MeetingRadius, angle, 2f, taken)
                              ?? target.Position + Vector3.up * 0.8f;

                // Facing the player they were gathered around.
                var look = target.Position - landing;
                var yaw = new Vector2(look.x, look.z).sqrMagnitude > 0.01f
                    ? Quaternion.LookRotation(new Vector3(look.x, 0f, look.z)).eulerAngles.y
                    : target.Yaw;
                Send(movers[i], TrapEffects.Meeting, landing, yaw);
            }
        }

        // A standable spot in sight of `center`, its floor within `maxRise`; null if none.
        internal static Vector3? SafeLanding(Vector3 center, float radius, float angle, float maxRise, List<Vector3> taken)
        {
            var eye = center + Vector3.up * 1.2f;
            foreach (var distance in new[] { radius, radius * 1.6f, radius * 0.6f })
            {
                for (var i = 0; i < 12; i++)
                {
                    var point = center + Quaternion.Euler(0f, angle + 30f * i, 0f) * Vector3.forward * distance;
                    if (Physics.Linecast(eye, point + Vector3.up * 1.2f, out _, ~0, QueryTriggerInteraction.Ignore))
                        continue;
                    if (!Physics.Raycast(point + Vector3.up * 2.5f, Vector3.down, out var hit, 5f, ~0, QueryTriggerInteraction.Ignore))
                        continue;
                    if (Mathf.Abs(hit.point.y - center.y) > maxRise)
                        continue;

                    var feet = hit.point;
                    if (Physics.CheckCapsule(feet + Vector3.up * 0.45f, feet + Vector3.up * 1.6f, 0.35f, ~0, QueryTriggerInteraction.Ignore))
                        continue;
                    if (taken != null && taken.Exists(other => Vector3.Distance(other, feet) < 1f))
                        continue;

                    taken?.Add(feet);
                    return feet + Vector3.up * 0.8f;
                }
            }

            return null;
        }

        private static readonly string[] PackNames = { "BackpackProp", "HolsterProp", "GourdCartonProp" };

        // A clone of the blindfold helmet, locked to each player's head home until the trap ends.
        private static readonly List<(Prop helmet, PropHome home, float until)> Helmets = new();

        // Kept a moment once off, or a guest's screen stays blurred.
        private const float HelmetLinger = 1.5f;
        private static readonly List<(Prop helmet, float at)> RetiringHelmets = new();

        // Debug: Big Mask fired by hand spares the host, who can then watch the guest wear it.
        internal static bool DebugMaskSparesHost;

        private static void FireMask()
        {
            foreach (var walker in Walkers())
            {
                if (DebugMaskSparesHost && walker.Local)
                    continue;
                if (IsMasked(walker))
                {
                    Plugin.Log.LogInfo($"{Tag} Big Mask spares {walker.Player.gameObject.name}: a mask is already on their screen.");
                    continue;
                }

                PropHome home = null;
                foreach (var candidate in walker.Player.GetComponentsInChildren<PropHome>(true))
                {
                    if (candidate != null && candidate.gameObject.name == "blindfoldPropHome")
                    {
                        home = candidate;
                        break;
                    }
                }

                if (home == null || home.pinnedProp != null)
                {
                    Plugin.Log.LogInfo($"{Tag} Big Mask spares {walker.Player.gameObject.name}: {(home == null ? "no head home" : "something is on their head")}.");
                    continue;
                }

                var helmet = GadgetItemSpawner.SpawnFor(GadgetKind.Blindfold, walker.Player, toHands: false);
                if (helmet == null)
                    continue;

                try
                {
                    helmet.ServerSetPinned(home);
                    Helmets.Add((helmet, home, Time.unscaledTime + _duration));
                    Plugin.Log.LogInfo($"{Tag} Big Mask on {walker.Player.gameObject.name} for {_duration}s.");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} Could not put the helmet on {walker.Player.gameObject.name}: {ex.Message}");
                    NetworkServer.Destroy(helmet.gameObject);
                }
            }
        }

        // Host, every frame: a knocked-off helmet goes back on; one whose time is up leaves.
        private static void TickHelmets()
        {
            for (var i = Helmets.Count - 1; i >= 0; i--)
            {
                var (helmet, home, until) = Helmets[i];
                try
                {
                    if (helmet == null || helmet.gameObject == null || home == null)
                    {
                        Helmets.RemoveAt(i);
                        continue;
                    }

                    if (Time.unscaledTime >= until)
                    {
                        Helmets.RemoveAt(i);
                        helmet.blockRemovingFromHomes = false;
                        helmet.ServerSetUnpinned();
                        RetiringHelmets.Add((helmet, Time.unscaledTime + HelmetLinger));
                        Plugin.Log.LogInfo($"{Tag} Big Mask over for one player.");
                        continue;
                    }

                    if (helmet.currentHome != home)
                        helmet.ServerSetPinned(home);
                }
                catch (Exception ex)
                {
                    Helmets.RemoveAt(i);
                    Plugin.Log.LogWarning($"{Tag} A Big Mask helmet went wrong: {ex.Message}");
                }
            }
        }

        private static void RetireHelmets()
        {
            for (var i = RetiringHelmets.Count - 1; i >= 0; i--)
            {
                var (helmet, at) = RetiringHelmets[i];
                if (Time.unscaledTime < at)
                    continue;
                RetiringHelmets.RemoveAt(i);
                try
                {
                    if (helmet != null && helmet.gameObject != null)
                        NetworkServer.Destroy(helmet.gameObject);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} Could not take a Big Mask helmet out of the world: {ex.Message}");
                }
            }
        }

        private static void FireLoad()
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null)
                return;

            List<Prop> contents = null;
            foreach (var player in players)
            {
                if (player == null || _onlyPlayer != null && player != _onlyPlayer)
                    continue;

                var behind = player.transform.position - player.transform.forward * 0.8f + Vector3.up * 0.6f;
                foreach (var home in player.GetComponentsInChildren<PropHome>(true))
                {
                    var pack = home != null ? home.pinnedProp : null;
                    if (pack == null || !Array.Exists(PackNames, n => pack.gameObject.name.StartsWith(n, StringComparison.Ordinal)))
                        continue;

                    // A worn pack's Kernal moves under the bones: the home's parentProp names it.
                    var spilled = 0;
                    foreach (var content in contents ??= AllProps())
                    {
                        var at = content != null ? content.currentHome : null;
                        if (at == null || content == pack || at.parentProp != pack)
                            continue;
                        if (Unpin(content, behind + UnityEngine.Random.insideUnitSphere * 0.4f))
                            spilled++;
                    }

                    if (Unpin(pack, behind))
                        Plugin.Log.LogInfo($"{Tag} Big Load: {player.gameObject.name} lost {pack.gameObject.name}, {spilled} thing(s) spilled.");
                }
            }
        }

        private static List<Prop> AllProps()
        {
            return new List<Prop>(UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
        }

        private static bool Unpin(Prop prop, Vector3 at)
        {
            try
            {
                prop.ServerSetUnpinned();
                prop.SetLoose();
                prop.transform.position = at;
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Big Load could not take {prop.gameObject.name} off: {ex.Message}");
                return false;
            }
        }

        // The floor under a point, looked for from a little above it.
        internal static Vector3 Ground(Vector3 point)
        {
            if (Physics.Raycast(point + Vector3.up * 2.5f, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 0.8f;
            return point + Vector3.up * 0.5f;
        }

        // A failed puzzle calls Trigger (Core/PuzzleFailures).
        private static int _missed;
        private static float _lastSent = -100f;

        // `big_fall`: the host sees its own falls in TriggerFall and guests' in ShowFall.
        private const float FallCooldown = 5f;
        private const float KnockoutQuiet = 8f;
        private const float AirQuiet = 15f;
        private static readonly Dictionary<int, float> LastFall = new();
        private static float _knockoutUntil = -100f;

        internal static void OnFall(PlayerCharacter player)
        {
            if (!NetworkServer.active || player == null || Time.unscaledTime < _knockoutUntil)
                return;

            var id = player.GetInstanceID();
            if (LastFall.TryGetValue(id, out var at) && Time.unscaledTime - at < FallCooldown)
                return;

            LastFall[id] = Time.unscaledTime;
            Plugin.Log.LogInfo($"{Tag} {player.gameObject.name} took a big fall.");
            Trigger("big_fall", "took a big fall");
        }

        internal static void Trigger(string trigger, string what)
        {
            if (!_sendsDeathLink || !DeathLinkOn || !_triggers.Contains(trigger))
                return;

            if (_missed < Amnesty)
            {
                _missed++;
                Plugin.Log.LogInfo($"{Tag} DeathLink forgiven ({trigger}): {_missed}/{Amnesty} of the amnesty.");
                Net.ApNotices.Post($"DeathLink forgiven {_missed}/{Amnesty}: {what}");
                return;
            }

            if (Time.unscaledTime - _lastSent < DeathLinkCooldown)
            {
                Net.ApNotices.Post($"DeathLink not sent, one was sent a moment ago: {what}");
                return;
            }

            _missed = 0;
            _lastSent = Time.unscaledTime;
            ApRuntime.SendDeathLink(what, announce: true);

            // `death_link_trap_on_send`: the players who sent it take the effect of a death too.
            if (_trapOnSend)
                ApplyDeathEffect("sent", "own DeathLink sent");
        }

        internal static void ForgetWorld()
        {
            Gauntlet.Forget();
            PuzzleFailures.ForgetWorld();
            Tomatoes.Clear();
            Helmets.Clear();
            RetiringHelmets.Clear();
            TomatoStarts.Clear();
            Released.Clear();
            ActiveUntil.Clear();
        }

        internal static void ForgetGuests()
        {
            GuestMasked.Clear();
        }

        internal static string Describe()
        {
            var lines = new List<string>
            {
                $"{Pending.Count} waiting, duration {_duration}s, spare gauntlet {_spareGauntlet}, "
                + $"deathlink on={DeathLinkOn} send={_sendsDeathLink} [{string.Join(",", _triggers)}] {_missed}/{Amnesty} receive={_receivesDeathLink} ({_deathLinkEffect})",
            };

            if (NetworkServer.active)
            {
                foreach (var walker in Walkers())
                {
                    IsSpared(walker, out var why);
                    lines.Add($"  {walker.Player.gameObject.name}{(walker.Local ? " (host)" : string.Empty)} at {walker.Position:F0}"
                              + $", in gauntlet={Gauntlet.Contains(walker.Position)}, masked={IsMasked(walker)}"
                              + (why != null ? $" -> spared: {why}" : string.Empty));
                }

                foreach (var spot in TripSpots.Candidates())
                    lines.Add($"  trip spot: {spot.Label} at {spot.Point:F1}");
            }

            return string.Join(Environment.NewLine, lines);
        }
    }

    // The Silent Gauntlet's extent: boxes around the renderers under `SilentGauntlet <n>Player`.
    internal static class Gauntlet
    {
        private const string RootPrefix = "SilentGauntlet";
        private const float Margin = 6f;
        private const float MaxBoxSize = 200f;

        // The Gauntlet's parts are spread over the island, so it gets a box per part.
        private static readonly System.Collections.Generic.List<Bounds> _boxes = new();
        private static bool _looked;

        internal static bool Contains(Vector3 point)
        {
            if (!_looked)
                Find();
            foreach (var box in _boxes)
            {
                if (box.Contains(point))
                    return true;
            }

            return false;
        }

        internal static void Forget()
        {
            _boxes.Clear();
            _looked = false;
        }

        // A particle system's bounds sit at the origin until it has played: not part of it.
        private static bool IsParticle(Renderer renderer) =>
            renderer.GetIl2CppType().Name == "ParticleSystemRenderer";

        private static bool Measure(Transform branch, out Bounds bounds)
        {
            bounds = default;
            var any = false;
            foreach (var renderer in branch.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || IsParticle(renderer))
                    continue;
                if (!any)
                    bounds = renderer.bounds;
                else
                    bounds.Encapsulate(renderer.bounds);
                any = true;
            }

            return any;
        }

        private static float Widest(Bounds bounds) =>
            Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));

        private static void Collect(Transform branch)
        {
            if (!Measure(branch, out var bounds))
                return;
            if (Widest(bounds) <= MaxBoxSize)
            {
                bounds.Expand(Margin * 2f);
                _boxes.Add(bounds);
                Plugin.Log.LogInfo($"[{nameof(Gauntlet)}]   box '{branch.name}': {bounds.size.x:F0} x {bounds.size.y:F0} x {bounds.size.z:F0} m around {bounds.center}.");
                return;
            }

            Plugin.Log.LogInfo($"[{nameof(Gauntlet)}]   '{branch.name}' is {bounds.size.x:F0} x {bounds.size.y:F0} x {bounds.size.z:F0} m: split into its children.");
            if (branch.childCount == 0)
            {
                Plugin.Log.LogInfo($"[{nameof(Gauntlet)}]   dropped '{branch.name}', a single renderer too wide.");
                return;
            }

            // Its own renderers (not a child's) count one by one.
            foreach (var renderer in branch.GetComponents<Renderer>())
            {
                if (IsParticle(renderer))
                    continue;
                var own = renderer.bounds;
                if (Widest(own) <= MaxBoxSize)
                {
                    own.Expand(Margin * 2f);
                    _boxes.Add(own);
                }
                else
                    Plugin.Log.LogInfo($"[{nameof(Gauntlet)}]   dropped the renderer of '{branch.name}', too wide.");
            }

            for (var i = 0; i < branch.childCount; i++)
                Collect(branch.GetChild(i));
        }

        private static void Find()
        {
            if (!WorldManager.isReadyForEffects)
                return;

            _looked = true;
            try
            {
                Transform root = null;
                foreach (var state in UnityEngine.Object.FindObjectsByType<TrackedPeckState>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    for (var t = state != null ? state.transform : null; t != null; t = t.parent)
                    {
                        if (t.name.StartsWith(RootPrefix, StringComparison.Ordinal))
                        {
                            root = t;
                            break;
                        }
                    }

                    if (root != null)
                        break;
                }

                if (root == null)
                {
                    Plugin.Log.LogInfo($"[{nameof(Gauntlet)}] No '{RootPrefix}' object in this world.");
                    return;
                }

                _boxes.Clear();
                Plugin.Log.LogInfo($"[{nameof(Gauntlet)}] '{root.name}':");
                Collect(root);
                Plugin.Log.LogInfo($"[{nameof(Gauntlet)}] {_boxes.Count} box(es) in all.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(Gauntlet)}] Could not measure the Gauntlet: {ex.Message}");
            }
        }
    }

    // Big Trip only sends players to places already reached, so it never skips a lock.
    internal static class TripSpots
    {
        internal struct Spot
        {
            internal string Label;
            internal Vector3 Point;
        }

        // How far from a radio station (a launcher, a puzzle's gourd) a player lands.
        private const float StationDistance = 3.5f;

        // A puzzle with a screen mask this close to its gourd's home is a mask puzzle.
        private const float MaskPuzzleRadius = 60f;

        internal static List<Spot> Candidates()
        {
            var spots = new List<Spot>();
            try
            {
                spots.AddRange(TeleportButtons.TripLandings()
                    .Select(landing => new Spot { Label = landing.Key, Point = landing.Value }));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(TripSpots)}] Tower landings: {ex.Message}");
            }

            try
            {
                foreach (var station in UnityEngine.Object.FindObjectsByType<BroadcastStation>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (!RadioStations.TryGetSystem(station, out var system))
                        continue;
                    if (SaveManager.GetIntValue(system.ToString(), 0, false) == 0)
                        continue;

                    var landing = AroundStation(station.transform.position);
                    if (landing != null)
                        spots.Add(new Spot { Label = $"radio {system}", Point = landing.Value });
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(TripSpots)}] Radio stations: {ex.Message}");
            }

            try
            {
                AddPuzzles(spots);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(TripSpots)}] Puzzles: {ex.Message}");
            }

            try
            {
                foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (transform == null || !transform.name.StartsWith("FireworkLauncher", StringComparison.Ordinal))
                        continue;
                    var key = PackChecks.LandmarkOf(transform);
                    if (key == null || !PackChecks.LauncherFired(key))
                        continue;
                    var landing = AroundStation(transform.position);
                    if (landing != null)
                        spots.Add(new Spot { Label = $"fireworks {key}", Point = landing.Value });
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(TripSpots)}] Fireworks: {ex.Message}");
            }

            try
            {
                foreach (var pair in GadgetItemSpawner.VanillaPacksByGuid())
                {
                    if (!CheckTracker.IsReported(ApLocationIds.PickupPrefix + pair.Key))
                        continue;
                    var landing = AroundStation(pair.Value.transform.position);
                    if (landing != null)
                        spots.Add(new Spot { Label = $"pack {pair.Value.gameObject.name} {pair.Key}", Point = landing.Value });
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(TripSpots)}] Packs: {ex.Message}");
            }

            spots.RemoveAll(spot => Gauntlet.Contains(spot.Point));
            return spots;
        }

        // Solved puzzles, beside their gourd's home; never a mask puzzle.
        private static void AddPuzzles(List<Spot> spots)
        {
            var masks = new List<Vector3>();
            foreach (var mask in UnityEngine.Object.FindObjectsByType<PeckEffectMask>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mask != null)
                    masks.Add(mask.transform.position);
            }

            foreach (var gourd in UnityEngine.Object.FindObjectsByType<RewardGourd>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var prop = gourd != null ? gourd.prop : null;
                if (prop == null || !GourdRegistry.TryGetLocationId(prop.saveablePropName, out var location)
                    || !CheckTracker.IsReported(location))
                    continue;

                var home = GourdRegistry.TryGetHomeFor(prop.saveablePropName);
                if (home == null)
                    continue;

                var at = home.transform.position;
                if (masks.Exists(mask => Vector3.Distance(mask, at) < MaskPuzzleRadius))
                    continue;

                var landing = AroundStation(at);
                if (landing != null)
                    spots.Add(new Spot { Label = $"puzzle {prop.saveablePropName}", Point = landing.Value });
            }
        }

        // A patch of floor beside the station, near its own height, with room to stand.
        private static Vector3? AroundStation(Vector3 station)
        {
            return Traps.SafeLanding(station, StationDistance, 0f, 2.5f, null);
        }
    }

    // Runs the effect clocks, the host's queue, DeathLink triggers and the guests' mask status.
    internal sealed class TrapRunner : MonoBehaviour
    {
        public TrapRunner(IntPtr ptr) : base(ptr)
        {
        }

        private static bool _worldWasReady;
        private static float _nextStatus;
        private static bool _lastMaskedSent;

        // After the animation has placed the bodies: what must win over it (Big Head's lift).
        private void LateUpdate()
        {
            try
            {
                TrapEffects.LateTick();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(TrapRunner)}] {ex.Message}");
            }
        }

        private void Update()
        {
            try
            {
                var ready = WorldManager.isReadyForEffects;
                if (!ready && _worldWasReady)
                {
                    TrapEffects.EndAll();
                    Traps.ForgetWorld();
                }

                if (ready && !_worldWasReady && NetworkServer.active)
                    Traps.OnWorldReady();

                _worldWasReady = ready;
                TrapEffects.Tick();
                PackChecks.Tick();
                ZoneLocks.Tick();
                MonumentOrder.TickFullChecks();
                FastGolf.Tick();
                ColourPainter.Tick();
                HintBoards.Tick();
                MapTracker.Tick();
                BigFlare.Tick();
                Patches.SettingsMenuArchipelagoRows.KeepPlaced();

                if (NetworkServer.active)
                {
                    ObjectiveSpots.TickCountdown();
                    Traps.TickHost(ApRuntime.SessionSettled);
                }
                else
                {
                    Traps.ForgetGuests();
                    if (NetworkClient.active)
                        TellHostAboutMask();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(TrapRunner)}] {ex.Message}");
            }
        }

        // On a change, and again every few seconds in case one was missed.
        private static void TellHostAboutMask()
        {
            var masked = TrapEffects.LocalMasked;
            if (masked == _lastMaskedSent && Time.unscaledTime < _nextStatus)
                return;

            if (ModChannel.SendStatus(masked))
            {
                _lastMaskedSent = masked;
                _nextStatus = Time.unscaledTime + 5f;
            }
        }
    }
}
