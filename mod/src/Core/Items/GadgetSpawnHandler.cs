using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Items
{
    // Builds gadget clones on clients: they are neither scene objects nor registered prefabs.
    internal class GadgetSpawnHandler : MonoBehaviour
    {
        // Required by Il2CppInterop for any type injected into IL2CPP.
        public GadgetSpawnHandler(IntPtr ptr) : base(ptr)
        {
        }

        private bool _registeredForThisClient;
        private bool _registrationImpossible;
        private float _nextCheck;

        private const float CheckIntervalSeconds = 1f;

        private static readonly List<Prop> PendingSpawned = new();
        private static readonly Dictionary<int, float> SettleDeadline = new();
        private static readonly HashSet<int> Painted = new();
        private const float SettleSeconds = 3f;

        private void Update()
        {
            DrainPendingLoose();

            if (_registrationImpossible)
                return;

            if (!NetworkClient.active)
            {
                _registeredForThisClient = false;
                return;
            }

            if (Time.time < _nextCheck)
                return;

            _nextCheck = Time.time + CheckIntervalSeconds;

            if (IsStillRegistered())
                return;

            if (_registeredForThisClient)
                Plugin.Log.LogInfo(
                    $"[{nameof(GadgetSpawnHandler)}] Spawn handlers were cleared from under us; registering again.");

            _registeredForThisClient = true;

            try
            {
                var spawn = (SpawnDelegate)SpawnFromAssetId;
                var unspawn = (UnSpawnDelegate)UnSpawn;
                var registered = 0;
                foreach (var assetId in GadgetItemSpawner.AllAssetIds())
                {
                    NetworkClient.RegisterSpawnHandler(assetId, spawn, unspawn);
                    registered++;
                }

                Plugin.Log.LogInfo(
                    $"[{nameof(GadgetSpawnHandler)}] Gadget spawn handlers registered ({registered}, one per kind and vanilla instance).");
            }
            catch (Exception ex)
            {
                _registrationImpossible = true;
                Plugin.Log.LogWarning(
                    $"[{nameof(GadgetSpawnHandler)}] Could not register the spawn handlers; gadget items will not appear for this player, and this will not be retried: {ex.Message}");
            }
        }

        private static bool IsStillRegistered()
        {
            try
            {
                var handlers = NetworkClient.spawnHandlers;
                if (handlers == null)
                    return false;

                foreach (var assetId in GadgetItemSpawner.AllAssetIds())
                {
                    if (!handlers.ContainsKey(assetId))
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(GadgetSpawnHandler)}] Could not read Mirror's spawn handler table ({ex.Message}); registering anyway.");
                return false;
            }
        }

        // Deferred a frame: run at once, it would beat Mirror applying the spawn payload.
        [HideFromIl2Cpp]
        private static void DrainPendingLoose()
        {
            if (PendingSpawned.Count == 0)
                return;

            var now = Time.time;
            var doneWith = new List<Prop>();

            foreach (var prop in PendingSpawned)
            {
                if (prop == null)
                {
                    doneWith.Add(prop);
                    continue;
                }

                var id = prop.GetInstanceID();
                // Painted once its netId has arrived, which the spawn handler is called without.
                var netIdentity = prop.GetComponent<NetworkIdentity>();
                if (netIdentity != null && netIdentity.netId != 0 && Painted.Add(id))
                    ColourPainter.PaintCopy(prop.gameObject, netIdentity.netId);

                if (!SettleDeadline.TryGetValue(id, out var deadline))
                {
                    deadline = now + SettleSeconds;
                    SettleDeadline[id] = deadline;
                }

                CosmeticGourdSpawnHandler.TryAttachToLocalHands(prop);

                var holder = CosmeticGourdSpawnHandler.FindHolderName(prop);
                if (holder != null)
                {
                    var identity = prop.GetComponent<NetworkIdentity>();
                    Plugin.Log.LogInfo(
                        $"[{nameof(GadgetSpawnHandler)}] netId {(identity != null ? identity.netId : 0)} claimed by {holder}; leaving its physics alone.");
                    doneWith.Add(prop);
                    continue;
                }

                // Worn or stowed: making it loose would pull a backpack off somebody's back.
                var homedIn = CosmeticGourdSpawnHandler.FindHomeName(prop);
                if (homedIn != null)
                {
                    var identity = prop.GetComponent<NetworkIdentity>();
                    Plugin.Log.LogInfo(
                        $"[{nameof(GadgetSpawnHandler)}] netId {(identity != null ? identity.netId : 0)} sits in {homedIn}; leaving it there.");
                    doneWith.Add(prop);
                    continue;
                }

                if (now < deadline)
                    continue;

                var timedOutIdentity = prop.GetComponent<NetworkIdentity>();
                Plugin.Log.LogInfo(
                    $"[{nameof(GadgetSpawnHandler)}] netId {(timedOutIdentity != null ? timedOutIdentity.netId : 0)} unclaimed after {SettleSeconds:0}s; dropping it.");

                try
                {
                    prop.SetLoose();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(GadgetSpawnHandler)}] Could not let go of a cosmetic gadget physically: {ex.Message}");
                }

                doneWith.Add(prop);
            }

            foreach (var done in doneWith)
            {
                PendingSpawned.Remove(done);
                if (done != null)
                {
                    SettleDeadline.Remove(done.GetInstanceID());
                    Painted.Remove(done.GetInstanceID());
                }
            }
        }

        private static GameObject SpawnFromAssetId(Vector3 position, uint assetId)
        {
            Net.ModChannel.NoteHostHasMod("a gadget of ours was spawned");

            if (!GadgetItemSpawner.TryDecodeAssetId(assetId, out var kind, out var index))
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(GadgetSpawnHandler)}] Asset id {assetId:X} names no gadget; placing an empty stand-in.");
                return GadgetItemSpawner.BuildResolvablePlaceholder(GadgetKind.Megaphone, position);
            }

            return Spawn(kind, index, position, assetId);
        }

        // Never return null: Mirror would break that player's network state for the whole session.
        private static GameObject Spawn(GadgetKind kind, int index, Vector3 position, uint assetId)
        {
            try
            {
                var prop = GadgetItemSpawner.BuildNeutralizedClone(kind, position, Quaternion.identity, index);
                if (prop != null)
                {
                    Plugin.Log.LogInfo($"[{nameof(GadgetSpawnHandler)}] Cosmetic {kind} #{index} built at {position}.");
                    PendingSpawned.Add(prop);
                    return prop.gameObject;
                }

                var standIn = GadgetItemSpawner.BuildStandInClone(kind, position, Quaternion.identity);
                if (standIn != null)
                {
                    PendingSpawned.Add(standIn);
                    return standIn.gameObject;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(GadgetSpawnHandler)}] Failed to build an incoming {kind}: {ex.Message}");
            }

            return GadgetItemSpawner.BuildResolvablePlaceholder(kind, position);
        }

        private static void UnSpawn(GameObject spawned)
        {
            if (spawned == null)
                return;

            Plugin.Log.LogInfo($"[{nameof(GadgetSpawnHandler)}] Cosmetic gadget removed by the server.");
            UnityEngine.Object.Destroy(spawned);
        }
    }
}
