using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Keeps the seven big keys locked in their stones until their Archipelago item arrives.
    internal static partial class KeyCustody
    {
        // No game enum name has this prefix, so SaveValuePatch never reads these as a key placed.
        private const string KeyPrefix = "ap_key_";

        // False when slot_data lacks the field: the keys then behave as in the vanilla game.
        internal static bool KeysAreItems { get; private set; }

        // Granted but not delivered yet: a loading world has no spawn point.
        private static readonly HashSet<SaveablePropName> Pending = new();

        private static bool _pendingBlockedLogged;

        // Delivered keys, rechecked a moment later in case the game takes them back.
        private static readonly List<(SaveablePropName key, Vector3 target, float at, int corrections)> Delivered = new();

        private const float RecheckDelaySeconds = 2f;

        // Beyond the 1.3m drop from the spawn point, so a key that just fell is not a stray.
        private const float StrayMetres = 5f;

        internal static void Configure(bool keysAreItems)
        {
            if (KeysAreItems == keysAreItems)
                return;

            KeysAreItems = keysAreItems;
            Plugin.Log.LogInfo(
                $"[{nameof(KeyCustody)}] Big keys are "
                + (keysAreItems
                    ? "Archipelago items: filling a monument no longer releases one."
                    : "left vanilla: a full monument releases its own key."));
        }

        internal static bool IsGranted(SaveablePropName propName)
        {
            return SaveManager.GetIntValue(KeyPrefix + propName, 0, false) != 0;
        }

        // toPlayer is false for the startup replay, so those keys land at the hub with the rest.
        internal static bool Grant(SaveablePropName propName, bool toPlayer)
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(KeyCustody)}] Ignored: {propName} received while not host.");
                return false;
            }

            if (!GourdRegistry.IsBigKey(propName))
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(KeyCustody)}] {propName} is not a big key; item ignored.");
                return false;
            }

            SaveManager.SetIntValue(KeyPrefix + propName, 1);
            Pending.Add(propName);

            var delivered = TickPending(toPlayer);
            Plugin.Log.LogInfo(
                $"[{nameof(KeyCustody)}] {propName} granted"
                + (delivered ? " and delivered." : "; it will be delivered once the world is ready."));

            return true;
        }

        // A key already lying in the world stays there; Tick() re-locks its home after a reload.
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
                    $"[{nameof(KeyCustody)}] {cleared} key grant(s) cleared: this save is being replayed against "
                    + "a different seed or slot.");
            }
        }

        // A key already out of its stone keeps its place: the game saves prop positions itself.
        internal static void RearmFromLedger()
        {
            Pending.Clear();
            _pendingBlockedLogged = false;

            // A new world rebuilds every renderer, so the keys need painting again.
            KeyColours.Rearm();

            if (!KeysAreItems)
                return;

            foreach (var propName in GourdRegistry.BigKeys)
            {
                if (IsGranted(propName) && IsStillInItsStone(propName))
                    Pending.Add(propName);
            }

            if (Pending.Count > 0)
                TickPending(toPlayer: false);
        }

        // Every frame: a player can grab a released key well inside the 1s poll.
        internal static void EnforceCustodyTick()
        {
            if (!KeysAreItems || !NetworkServer.active)
                return;

            EnforceCustody();
        }

        internal static void Tick()
        {
            if (!KeysAreItems || !NetworkServer.active)
                return;

            TickPending(toPlayer: false);
            KeyColours.PaintOnce();
        }

        // Keyed on startHome: currentHome becomes the plinth once the key is placed.
        private static void EnforceCustody()
        {
            foreach (var prop in GourdRegistry.LoadedBigKeys())
            {
                var stone = prop.startHome;
                if (stone == null)
                    continue;

                // Guard in case startHome is ever a plinth.
                if (GourdRegistry.IsBigKeyPlinth(stone.saveableHomeName))
                    continue;

                var shouldBeLocked = !IsGranted(prop.saveablePropName);
                if (stone.blockGrabbing != shouldBeLocked)
                    stone.blockGrabbing = shouldBeLocked;
            }
        }

        private static bool IsStillInItsStone(SaveablePropName propName)
        {
            var prop = FindKey(propName);
            if (prop == null)
                return false;

            var home = prop.currentHome;
            return home != null && prop.startHome != null && home.Pointer == prop.startHome.Pointer;
        }

        // Ctrl+R for keys; a key already in its plinth is left alone.
        internal static int ResyncToSpawn()
        {
            using var activity = ModActivity.Enter("key resync");
            if (!KeysAreItems || !NetworkServer.active)
                return 0;

            var moved = 0;
            foreach (var propName in GourdRegistry.BigKeys)
            {
                if (!IsGranted(propName))
                    continue;

                var prop = FindKey(propName);
                if (prop == null)
                    continue;

                var home = prop.currentHome;
                if (home != null && GourdRegistry.IsBigKeyPlinth(home.saveableHomeName))
                    continue;

                // Dropped first, or the hands keep holding a key that has moved away.
                ReceivedItemSpawner.ReleaseFromHands(prop);

                Pending.Add(propName);
                moved++;
            }

            if (moved > 0)
                TickPending(toPlayer: false);

            return moved;
        }

        private static Prop FindKey(SaveablePropName propName)
        {
            // Through GourdRegistry: Prop.allProps throws when there is no world.
            return GourdRegistry.FindProp(propName);
        }
    }
}
