using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Items
{
    // Builds the host's cosmetic gourd clones on guests: a clone of a scene object has no prefab.
    internal class CosmeticGourdSpawnHandler : MonoBehaviour
    {
        // Required by Il2CppInterop for any injected type.
        public CosmeticGourdSpawnHandler(IntPtr ptr) : base(ptr)
        {
        }

        private bool _registeredForThisClient;
        private bool _registrationImpossible;
        private float _nextCheck;

        private const float CheckIntervalSeconds = 1f;

        // Static because the handler Mirror calls is static.
        private static readonly List<RewardGourd> PendingSpawned = new();

        private static readonly Dictionary<int, float> SettleDeadline = new();

        // Covers the hold SyncVar's round trip; too early snatches a gourd from someone's hands.
        private const float SettleSeconds = 3f;

        private void Update()
        {
            DrainPendingLoose();

            // NetworkClient is rebuilt each session, so the handler must be re-armed every time.
            if (_registrationImpossible)
                return;

            var clientActive = NetworkClient.active;
            if (!clientActive)
            {
                _registeredForThisClient = false;
                return;
            }

            if (Time.time < _nextCheck)
                return;

            _nextCheck = Time.time + CheckIntervalSeconds;

            // Ask Mirror's table, not a flag: NetworkClient.ClearSpawners() empties it.
            if (IsStillRegistered())
                return;

            if (_registeredForThisClient)
                Plugin.Log.LogInfo(
                    $"[{nameof(CosmeticGourdSpawnHandler)}] Spawn handler was cleared from under us; registering again.");

            _registeredForThisClient = true;

            try
            {
                // The SpawnDelegate overload: Il2CppInterop cannot marshal the SpawnMessage one.
                NetworkClient.RegisterSpawnHandler(
                    ReceivedItemSpawner.CosmeticAssetId,
                    (SpawnDelegate)Spawn,
                    (UnSpawnDelegate)UnSpawn);

                Plugin.Log.LogInfo(
                    $"[{nameof(CosmeticGourdSpawnHandler)}] Cosmetic gourd spawn handler registered (assetId {ReceivedItemSpawner.CosmeticAssetId}).");
            }
            catch (Exception ex)
            {
                // Said once: this failure belongs to the build, not to the moment.
                _registrationImpossible = true;
                Plugin.Log.LogWarning(
                    $"[{nameof(CosmeticGourdSpawnHandler)}] Could not register the spawn handler; cosmetic gourds will not appear for this player, and this will not be retried: {ex.Message}");
            }
        }

        // False when the lookup fails: re-registering needlessly is harmless.
        private static bool IsStillRegistered()
        {
            try
            {
                var handlers = NetworkClient.spawnHandlers;
                return handlers != null && handlers.ContainsKey(ReceivedItemSpawner.CosmeticAssetId);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(CosmeticGourdSpawnHandler)}] Could not read Mirror's spawn handler table ({ex.Message}); registering anyway.");
                return false;
            }
        }

        // A clone keeps the template's fixed physics; SetLoose a frame later, after the spawn.
        [HideFromIl2Cpp]
        private static void DrainPendingLoose()
        {
            if (PendingSpawned.Count == 0)
                return;

            var now = Time.time;
            var doneWith = new List<RewardGourd>();

            foreach (var rewardGourd in PendingSpawned)
            {
                if (rewardGourd == null)
                {
                    doneWith.Add(rewardGourd);
                    continue;
                }

                var prop = rewardGourd.prop;
                if (prop == null)
                    continue;

                var id = rewardGourd.GetInstanceID();
                if (!SettleDeadline.TryGetValue(id, out var deadline))
                {
                    deadline = now + SettleSeconds;
                    SettleDeadline[id] = deadline;

                    // Once: Mirror's spawn payload ran after the handler and lost the colour.
                    ReceivedItemSpawner.ReapplyCosmeticLook(rewardGourd);
                }

                // Claimed: leave it alone, but first make the claim true in the local hands.
                TryAttachToLocalHands(prop);

                var holder = FindHolderName(prop);
                if (holder != null)
                {
                    var identity = rewardGourd.GetComponent<Mirror.NetworkIdentity>();
                    Plugin.Log.LogInfo(
                        $"[{nameof(CosmeticGourdSpawnHandler)}] netId {(identity != null ? identity.netId : 0)} claimed by {holder}; leaving its physics alone.");
                    doneWith.Add(rewardGourd);
                    continue;
                }

                var homedIn = FindHomeName(prop);
                if (homedIn != null)
                {
                    var identity = rewardGourd.GetComponent<Mirror.NetworkIdentity>();
                    Plugin.Log.LogInfo(
                        $"[{nameof(CosmeticGourdSpawnHandler)}] netId {(identity != null ? identity.netId : 0)} sits in {homedIn}; leaving it there.");
                    doneWith.Add(rewardGourd);
                    continue;
                }

                if (now < deadline)
                    continue;

                var timedOutIdentity = rewardGourd.GetComponent<Mirror.NetworkIdentity>();
                Plugin.Log.LogInfo(
                    $"[{nameof(CosmeticGourdSpawnHandler)}] netId {(timedOutIdentity != null ? timedOutIdentity.netId : 0)} unclaimed after {SettleSeconds:0}s; dropping it.");

                try
                {
                    prop.SetLoose();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(CosmeticGourdSpawnHandler)}] Could not let go of a cosmetic gourd physically: {ex.Message}");
                }

                doneWith.Add(rewardGourd);
            }

            foreach (var done in doneWith)
            {
                PendingSpawned.Remove(done);
                if (done != null)
                    SettleDeadline.Remove(done.GetInstanceID());
            }
        }

        // Asks hands.heldProp and the networked SyncVar: on a guest only the latter may be set.
        [HideFromIl2Cpp]
        // A hand-over decided on the host never ran PickUp on the receiving guest, so run it here.
        internal static bool TryAttachToLocalHands(Prop prop)
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null || prop == null)
                return false;

            var propIdentity = prop.GetComponent<Mirror.NetworkIdentity>();
            if (propIdentity == null)
                return false;

            foreach (var pc in players)
            {
                if (pc == null || !pc.isLocalPlayer)
                    continue;

                var hands = pc.hands;
                if (hands == null || hands.heldProp == prop)
                    return false;

                var networking = pc.playerNetworking;
                if (networking == null)
                    return false;

                try
                {
                    var held = networking.playerHeldInformation;
                    if (held.identity == null || held.identity != propIdentity)
                        return false;

                    hands.PickUp(prop);
                    Plugin.Log.LogInfo(
                        $"[{nameof(CosmeticGourdSpawnHandler)}] netId {propIdentity.netId} was handed to this player "
                        + "by the server but never reached their hands; picking it up locally.");
                    return true;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(CosmeticGourdSpawnHandler)}] Could not pick up a prop the server says we hold: {ex.Message}");
                    return false;
                }
            }

            return false;
        }

        // A homed prop is left alone: SetLoose would take it out of its home on a reconnection.
        internal static string FindHomeName(Prop prop)
        {
            try
            {
                var home = prop != null ? prop.currentHome : null;
                return home != null ? $"'{home.gameObject.name}' ({home.saveableHomeName})" : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        internal static string FindHolderName(Prop prop)
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null || prop == null)
                return null;

            var propIdentity = prop.GetComponent<Mirror.NetworkIdentity>();

            foreach (var pc in players)
            {
                if (pc == null)
                    continue;

                if (pc.hands != null && pc.hands.heldProp == prop)
                    return pc.gameObject.name;

                var networking = pc.playerNetworking;
                if (networking == null || propIdentity == null)
                    continue;

                try
                {
                    var held = networking.playerHeldInformation;
                    if (held.identity != null && held.identity == propIdentity)
                        return $"{pc.gameObject.name} (via SyncVar)";
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(CosmeticGourdSpawnHandler)}] Could not read a player's held information: {ex.Message}");
                }
            }

            return null;
        }

        [HideFromIl2Cpp]
        private static bool IsHeldByAnyone(Prop prop)
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

        private static GameObject Spawn(Vector3 position, uint assetId)
        {
            Net.ModChannel.NoteHostHasMod("a gourd of ours was spawned");

            try
            {
                var rewardGourd = ReceivedItemSpawner.BuildNeutralizedClone(position, Quaternion.identity);
                if (rewardGourd != null)
                {
                    // Logged to tell the spawn order from a held SyncVar resolved too early.
                    Plugin.Log.LogInfo(
                        $"[{nameof(CosmeticGourdSpawnHandler)}] Cosmetic gourd built at {position}.");

                    PendingSpawned.Add(rewardGourd);

                    return rewardGourd.gameObject;
                }

                Plugin.Log.LogWarning(
                    $"[{nameof(CosmeticGourdSpawnHandler)}] Nothing to clone from for an incoming cosmetic gourd; it will be missing for this player.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(CosmeticGourdSpawnHandler)}] Failed to build an incoming cosmetic gourd: {ex.Message}");
            }

            // Null tells Mirror the handler failed; it logs and keeps the connection.
            return null;
        }

        private static void UnSpawn(GameObject spawned)
        {
            if (spawned == null)
                return;

            Plugin.Log.LogInfo($"[{nameof(CosmeticGourdSpawnHandler)}] Cosmetic gourd removed by the server.");
            UnityEngine.Object.Destroy(spawned);
        }
    }
}
