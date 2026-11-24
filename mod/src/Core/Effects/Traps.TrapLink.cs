using System;
using System.Collections.Generic;
using System.Linq;
using BigWalkArchipelago.Core.Net;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    internal static partial class Traps
    {
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
    }
}
