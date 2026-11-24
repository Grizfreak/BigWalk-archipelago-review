using System;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Items
{
    // Frees this machine's hands when the server took what they hold: host Drop() won't replicate.
    internal class StaleHeldPropReleaser : MonoBehaviour
    {
        // Constructor required by Il2CppInterop for any type injected into IL2CPP.
        public StaleHeldPropReleaser(IntPtr ptr) : base(ptr)
        {
        }

        private const float CheckIntervalSeconds = 0.25f;

        // Not zero: a client attaches a prop before the server knows, so pickups look alike.
        private const float DisagreementGraceSeconds = 1f;

        private float _nextCheck;

        private static Prop _disputed;

        private static float _disputedSince;

        private void Update()
        {
            if (Time.time < _nextCheck)
                return;

            _nextCheck = Time.time + CheckIntervalSeconds;

            try
            {
                ReleaseIfStale();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(StaleHeldPropReleaser)}] Check failed, ignored: {ex.Message}");
            }
        }

        private static void ReleaseIfStale()
        {
            var hands = FindLocalHands();
            var prop = hands != null ? hands.heldProp : null;
            if (prop == null)
            {
                _disputed = null;
                return;
            }

            if (!IsStale(prop) && !ServerDisagrees(prop))
                return;

            Plugin.Log.LogInfo(
                $"[{nameof(StaleHeldPropReleaser)}] Holding a prop the server has taken away; letting go of it.");

            try
            {
                hands.Drop(new PlayerHeldInformation(prop));
            }
            catch (Exception ex)
            {
                // Better empty hands than hands pointing at nothing.
                Plugin.Log.LogWarning($"[{nameof(StaleHeldPropReleaser)}] Drop failed, clearing the hand directly: {ex.Message}");
                try { hands.heldProp = null; }
                catch (Exception inner) { Plugin.Log.LogWarning($"[{nameof(StaleHeldPropReleaser)}] ...and that failed too: {inner.Message}"); }
            }
        }

        // A key reclaimed by Ctrl+R is teleported, not unspawned: PlayerHeldInformation tells.
        private static bool ServerDisagrees(Prop prop)
        {
            var identity = prop.GetComponent<NetworkIdentity>();
            if (identity == null)
                return false;

            var networking = FindLocalNetworking();
            if (networking == null)
                return false;

            bool disagrees;
            try
            {
                var held = networking.playerHeldInformation;
                disagrees = held.identity == null || held.identity != identity;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(StaleHeldPropReleaser)}] Could not read our own held information: {ex.Message}");
                return false;
            }

            if (!disagrees)
            {
                _disputed = null;
                return false;
            }

            if (_disputed != prop)
            {
                _disputed = prop;
                _disputedSince = Time.time;
                return false;
            }

            if (Time.time - _disputedSince < DisagreementGraceSeconds)
                return false;

            Plugin.Log.LogInfo(
                $"[{nameof(StaleHeldPropReleaser)}] The server stopped saying we hold netId {identity.netId} "
                + $"{DisagreementGraceSeconds:0}s ago; letting go of it.");
            _disputed = null;
            return true;
        }

        private static PlayerNetworking FindLocalNetworking()
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null)
                return null;

            foreach (var pc in players)
            {
                if (pc != null && pc.isLocalPlayer)
                    return pc.playerNetworking;
            }

            return null;
        }

        private static bool IsStale(Prop prop)
        {
            var go = prop.gameObject;
            if (go == null || !go.activeInHierarchy)
                return true;

            // netId drops to 0 when the server unspawns; no NetworkIdentity means not networked.
            var identity = prop.GetComponent<NetworkIdentity>();
            return identity != null && identity.netId == 0;
        }

        private static PlayerHands FindLocalHands()
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null)
                return null;

            foreach (var pc in players)
            {
                if (pc != null && pc.isLocalPlayer)
                    return pc.hands;
            }

            return null;
        }
    }
}
