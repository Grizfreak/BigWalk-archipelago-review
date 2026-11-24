using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Keeps the seven big keys locked in their stones until their Archipelago item arrives.
    internal static class KeyCustody
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

        // blockGrabbing does not stop a full monument launching its key, so the release is refused.
        private static readonly Dictionary<int, SaveablePropName> KeyByLogic = new();
        private static readonly HashSet<int> NotAReleaseLogic = new();

        internal static bool IsReleaseHeld(TrackedPeckState state, out SaveablePropName propName)
        {
            propName = SaveablePropName.notSavable;
            if (!KeysAreItems || state == null)
                return false;

            var id = state.GetInstanceID();
            if (!KeyByLogic.TryGetValue(id, out propName))
            {
                if (NotAReleaseLogic.Contains(id))
                    return false;

                var name = state.gameObject.name;
                if (name != "KeyStoneLogic" && name != "KeyScrewLogic")
                {
                    NotAReleaseLogic.Add(id);
                    return false;
                }

                // Not cached while its key is not loaded: asked again next time.
                if (!TryFindKeyOf(state, out propName))
                    return false;

                KeyByLogic[id] = propName;
            }

            return !IsGranted(propName);
        }

        // The key whose stone shares the logic's parent (BigKeyInStone, BigKeyInBox, BigKeyInScrew)
        private static bool TryFindKeyOf(TrackedPeckState logic, out SaveablePropName propName)
        {
            propName = SaveablePropName.notSavable;
            var parent = logic.transform.parent;
            if (parent == null)
                return false;

            foreach (var prop in GourdRegistry.LoadedBigKeys())
            {
                var stone = prop.startHome;
                if (stone != null && stone.transform.IsChildOf(parent))
                {
                    propName = prop.saveablePropName;
                    return true;
                }
            }

            return false;
        }

        internal static void ForgetReleaseLogics()
        {
            KeyByLogic.Clear();
            NotAReleaseLogic.Clear();
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

        // Returns true when nothing is left waiting.
        private static bool TickPending(bool toPlayer)
        {
            if (Pending.Count == 0)
                return true;

            // Copied first: delivery runs game code that may change the set.
            var attempts = new List<SaveablePropName>(Pending);
            foreach (var propName in attempts)
            {
                if (!TryDeliver(propName, toPlayer))
                    continue;

                Pending.Remove(propName);
                Plugin.Log.LogInfo($"[{nameof(KeyCustody)}] {propName} is now at the spawn point.");
            }

            if (Pending.Count > 0)
            {
                LogBlockedOnce();
                return false;
            }

            _pendingBlockedLogged = false;
            return true;
        }

        private static void LogBlockedOnce()
        {
            if (_pendingBlockedLogged)
                return;

            _pendingBlockedLogged = true;
            Plugin.Log.LogInfo(
                $"[{nameof(KeyCustody)}] {Pending.Count} key(s) waiting on a world that can take them. Still trying.");
        }

        // ServerSetUnpinned looks inlined; if it throws, the key stays grabbable in its stone.
        private static bool TryDeliver(SaveablePropName propName, bool toPlayer)
        {
            var prop = FindKey(propName);
            if (prop == null)
                return false;

            // Already in its plinth: taking it out would empty the slot and raise the drawbridge.
            var placed = prop.currentHome;
            if (placed != null && GourdRegistry.IsBigKeyPlinth(placed.saveableHomeName))
            {
                Plugin.Log.LogInfo($"[{nameof(KeyCustody)}] {propName} is already in its plinth; left there.");
                return true;
            }

            // During play the key goes into a player's hands, like a gourd.
            var recipient = toPlayer ? ItemRecipients.Choose() : null;
            var destination = ReceivedItemSpawner.ResolveSpawnPosition(toPlayer, recipient);
            if (destination == null)
            {
                ItemRecipients.Settled(recipient);
                return false;
            }

            // Unlocked first: this is what makes the key obtainable at all.
            var stone = prop.startHome;
            if (stone != null && !GourdRegistry.IsBigKeyPlinth(stone.saveableHomeName))
                stone.blockGrabbing = false;

            try
            {
                prop.ServerSetUnpinned();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(KeyCustody)}] {propName}: Prop.ServerSetUnpinned threw ({ex.Message}). The key is "
                    + "unlocked but stays in its stone at the tower, go and fetch it there.");
                ItemRecipients.Settled(recipient);
                return true;
            }

            try
            {
                prop.SetLoose();
                Teleport(prop, destination.Value);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(KeyCustody)}] {propName}: could not be moved to the spawn point ({ex.Message}). "
                    + "It is unpinned and grabbable wherever it currently is.");
            }

            Delivered.Add((propName, destination.Value, Time.time + RecheckDelaySeconds, 0));

            // One frame later: the spawn and the holding SyncVar must not share a frame.
            if (recipient != null)
                ReceivedItemSpawner.QueueHandover(prop, recipient);

            return true;
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

        private static void Teleport(Prop prop, Vector3 destination)
        {
            prop.transform.position = destination;
        }

        // Holds a delivered key in place for a moment and counts how often the game moved it.
        internal static void SettleDelivered()
        {
            using var activity = ModActivity.Enter("key settle");
            if (Delivered.Count == 0)
                return;

            for (var i = Delivered.Count - 1; i >= 0; i--)
            {
                var entry = Delivered[i];
                var prop = FindKey(entry.key);

                // A held key moves with its player; putting it back would take it from them.
                if (prop != null && IsHeld(prop))
                {
                    Delivered.RemoveAt(i);
                    Plugin.Log.LogInfo($"[{nameof(KeyCustody)}] {entry.key}: in somebody's hands, left alone.");
                    continue;
                }

                if (prop != null && Time.time < entry.at
                    && Vector3.Distance(prop.transform.position, entry.target) > StrayMetres)
                {
                    Teleport(prop, entry.target);
                    Delivered[i] = (entry.key, entry.target, entry.at, entry.corrections + 1);
                    continue;
                }

                if (Time.time < entry.at)
                    continue;

                Delivered.RemoveAt(i);
                Plugin.Log.LogInfo(
                    $"[{nameof(KeyCustody)}] {entry.key}: put back {entry.corrections} time(s) in "
                    + $"{RecheckDelaySeconds}s. "
                    + (entry.corrections == 0
                        ? "Nothing fetched it home."
                        : entry.corrections <= 3
                            ? "A few corrections: the game fetches it home once or twice after the "
                              + "unpin, and putting it back settles it."
                            : "Continuous: something holds it at its tower, and this is a tug of war "
                              + "that has to be won at the cause."));
                Report(entry.key, entry.target);
            }
        }

        private static bool IsHeld(Prop prop)
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null)
                return false;

            foreach (var pc in players)
            {
                if (pc != null && pc.hands != null && pc.hands.heldProp == prop)
                    return true;
            }

            return false;
        }

        private static void Report(SaveablePropName propName, Vector3 target)
        {
            var prop = FindKey(propName);
            if (prop == null)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(KeyCustody)}] {propName} {RecheckDelaySeconds}s later: GONE from Prop.allProps "
                    + "entirely \u2014 destroyed, not merely hidden.");
                return;
            }

            var go = prop.gameObject;
            var home = prop.currentHome;
            var homeName = home != null ? $"'{home.name}' ({home.saveableHomeName})" : "<none, still loose>";

            var netId = 0u;
            try
            {
                netId = prop.netId;
            }
            catch (Exception)
            {
                // A prop without its NetworkIdentity is one of the outcomes this log tells apart.
            }

            var renderer = go.GetComponentInChildren<Renderer>(true);
            var visible = renderer != null
                ? $"renderer.enabled={renderer.enabled} isVisible={renderer.isVisible}"
                : "no renderer";


            Plugin.Log.LogInfo(
                $"[{nameof(KeyCustody)}] {propName} {RecheckDelaySeconds}s later: "
                + $"activeSelf={go.activeSelf} activeInHierarchy={go.activeInHierarchy} netId={netId} | "
                + $"home {homeName} | put at {target}, now at {prop.transform.position} "
                + $"({Vector3.Distance(target, prop.transform.position):F1}m away) | {visible}");
        }

        private static Prop FindKey(SaveablePropName propName)
        {
            // Through GourdRegistry: Prop.allProps throws when there is no world.
            return GourdRegistry.FindProp(propName);
        }
    }
}
