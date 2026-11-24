using System;
using System.Collections.Generic;
using System.Linq;
using BigWalkArchipelago.Core.Net;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal static partial class Traps
    {
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
    }
}
