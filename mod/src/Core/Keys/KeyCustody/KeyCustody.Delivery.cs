using UnityEngine;

namespace BigWalkArchipelago.Core.Keys
{
    internal static partial class KeyCustody
    {
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
                if (prop != null && SceneObjects.IsHeld(prop))
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

        private static void Report(SaveablePropName propName, Vector3 target)
        {
            var prop = FindKey(propName);
            if (prop == null)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(KeyCustody)}] {propName} {RecheckDelaySeconds}s later: GONE from Prop.allProps "
                    + "entirely, destroyed, not merely hidden.");
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
    }
}
