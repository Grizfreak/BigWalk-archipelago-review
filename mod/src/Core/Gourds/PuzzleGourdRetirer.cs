using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Gourds
{
    // Retires a solved gourd a frame later: inside the pick-up Command it hands out a dead prop.
    internal class PuzzleGourdRetirer : MonoBehaviour
    {
        // Constructor required by Il2CppInterop for any type injected into IL2CPP.
        public PuzzleGourdRetirer(IntPtr ptr) : base(ptr)
        {
        }

        private static readonly Queue<(RewardGourd gourd, int frame)> Pending = new();

        internal static void Retire(RewardGourd gourd)
        {
            if (gourd != null)
                Pending.Enqueue((gourd, Time.frameCount));
        }

        private void Update()
        {
            while (Pending.Count > 0 && Pending.Peek().frame < Time.frameCount)
            {
                var (gourd, _) = Pending.Dequeue();
                try
                {
                    RetireNow(gourd);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(PuzzleGourdRetirer)}] Could not remove a solved gourd: {ex.Message}");
                }
            }
        }

        private static void RetireNow(RewardGourd gourd)
        {
            if (gourd == null || !NetworkServer.active)
                return;

            // Whoever the Command just gave it to lets go first, so nobody holds a vanished object.
            ReceivedItemSpawner.ReleaseFromHands(gourd.prop);

            NetworkServer.UnSpawn(gourd.gameObject);
            gourd.gameObject.SetActive(false);
        }
    }
}
