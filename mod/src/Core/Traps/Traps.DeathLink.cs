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
    }
}
