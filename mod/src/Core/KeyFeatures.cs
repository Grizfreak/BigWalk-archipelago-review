using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Grants a big key's door as an item by driving the plinth's state; kept in a save ledger.
    internal static class KeyFeatures
    {
        // No game enum name has this prefix, so SaveValuePatch never takes it for a key placement.
        private const string KeyPrefix = "ap_feature_";

        // Not cleared when the connection drops: a network hiccup must not reopen every door.
        internal static bool FeaturesInPlay { get; private set; }

        // Granted features still waiting for a world able to take them.
        private static readonly HashSet<SaveablePropName> Pending = new();

        private static bool _pendingBlockedLogged;

        internal static void Configure(bool featuresInPlay)
        {
            if (FeaturesInPlay == featuresInPlay)
                return;

            FeaturesInPlay = featuresInPlay;
            Plugin.Log.LogInfo(
                $"[{nameof(KeyFeatures)}] Big key features are "
                + (featuresInPlay
                    ? "Archipelago items: placing a key no longer opens its door."
                    : "left vanilla: placing a key opens its door on the spot."));
        }

        internal static bool IsGranted(SaveablePropName propName)
        {
            return SaveManager.GetIntValue(KeyPrefix + propName, 0, false) != 0;
        }

        // Persisted first: the ledger survives the world reload, the live unlock does not.
        internal static bool Grant(SaveablePropName propName)
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(KeyFeatures)}] Ignored: {propName} received while not host.");
                return false;
            }

            if (!GourdRegistry.IsBigKey(propName))
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(KeyFeatures)}] {propName} is not a big key; item ignored.");
                return false;
            }

            SaveManager.SetIntValue(KeyPrefix + propName, 1);
            Pending.Add(propName);

            var opened = TickPending();
            Plugin.Log.LogInfo(
                $"[{nameof(KeyFeatures)}] {propName} granted"
                + (opened ? " and open." : "; it will open once its plinth is loaded."));

            return true;
        }

        // Cannot close a door already open in the running world; that needs a reload.
        internal static void ClearLedger()
        {
            var cleared = 0;
            foreach (var propName in GourdRegistry.BigKeys)
            {
                if (!IsGranted(propName))
                    continue;

                SaveManager.SetIntValue(KeyPrefix + propName, 0);
                cleared++;
            }

            Pending.Clear();
            _pendingBlockedLogged = false;

            if (cleared > 0)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(KeyFeatures)}] {cleared} feature grant(s) cleared: this save is being replayed "
                    + "against a different seed or slot. Reload the world to close the doors.");
            }
        }

        // Also on connecting with a world loaded: the item cursor skips the server's replay.
        internal static void RearmFromLedger()
        {
            Pending.Clear();
            _pendingBlockedLogged = false;

            if (!FeaturesInPlay)
                return;

            foreach (var propName in GourdRegistry.BigKeys)
            {
                if (IsGranted(propName))
                    Pending.Add(propName);
            }

            if (Pending.Count > 0)
                TickPending();
        }

        // A plinth may not be loaded yet when a world becomes ready. True once nothing is pending.
        internal static bool TickPending()
        {
            if (Pending.Count == 0)
                return true;

            // Copied first: SetState runs the game's listeners, which may touch this set.
            var attempts = new List<SaveablePropName>(Pending);
            foreach (var propName in attempts)
            {
                if (!TryOpen(propName))
                    continue;

                Pending.Remove(propName);
                Plugin.Log.LogInfo($"[{nameof(KeyFeatures)}] {propName} is now open.");
            }

            if (Pending.Count > 0)
            {
                LogBlockedOnce();
                return false;
            }

            _pendingBlockedLogged = false;
            return true;
        }

        // Once per stuck spell, not once per poll tick.
        private static void LogBlockedOnce()
        {
            if (_pendingBlockedLogged)
                return;

            _pendingBlockedLogged = true;

            foreach (var propName in Pending)
                Plugin.Log.LogInfo($"[{nameof(KeyFeatures)}] {propName} waiting: {WhyNoDoor(propName)}");

            Plugin.Log.LogInfo(
                $"[{nameof(KeyFeatures)}] {Pending.Count} feature(s) waiting. Still trying.");
        }

        private static string WhyNoDoor(SaveablePropName propName)
        {
            if (!GourdRegistry.TryGetHomeName(propName, out var homeName))
                return "no plinth is mapped to this key.";

            var home = GourdRegistry.TryGetHome(homeName);
            if (home == null)
                return $"plinth {homeName} is not loaded.";

            return $"plinth {homeName} is loaded but carries no pinDirectControlSystem, which every one of "
                   + "them had when this was measured. Run Ctrl+K.";
        }

        private static bool TryOpen(SaveablePropName propName)
        {
            var door = FindDoor(propName);
            if (door == null)
                return false;

            try
            {
                door.SetState(1);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[{nameof(KeyFeatures)}] SetState(1) failed for {propName}: {ex}");
                return false;
            }
        }

        // The plinth's own `pinDirectControlSystem`, what the game drives when a key goes in.
        internal static TrackedPeckState FindDoor(SaveablePropName propName)
        {
            var home = GourdRegistry.TryGetHomeFor(propName);
            return home != null ? home.pinDirectControlSystem : null;
        }
    }
}
