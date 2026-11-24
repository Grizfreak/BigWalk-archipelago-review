using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    internal static partial class Traps
    {
        // A clone of the blindfold helmet, locked to each player's head home until the trap ends.
        private static readonly List<(Prop helmet, PropHome home, float until)> Helmets = new();

        // Kept a moment once off, or a guest's screen stays blurred.
        private const float HelmetLinger = 1.5f;
        private static readonly List<(Prop helmet, float at)> RetiringHelmets = new();

        // Debug: Big Mask fired by hand spares the host, who can then watch the guest wear it.
        internal static bool DebugMaskSparesHost;

        private static void FireMask()
        {
            foreach (var walker in Walkers())
            {
                if (DebugMaskSparesHost && walker.Local)
                    continue;
                if (IsMasked(walker))
                {
                    Plugin.Log.LogInfo($"{Tag} Big Mask spares {walker.Player.gameObject.name}: a mask is already on their screen.");
                    continue;
                }

                PropHome home = null;
                foreach (var candidate in walker.Player.GetComponentsInChildren<PropHome>(true))
                {
                    if (candidate != null && candidate.gameObject.name == "blindfoldPropHome")
                    {
                        home = candidate;
                        break;
                    }
                }

                if (home == null || home.pinnedProp != null)
                {
                    Plugin.Log.LogInfo($"{Tag} Big Mask spares {walker.Player.gameObject.name}: {(home == null ? "no head home" : "something is on their head")}.");
                    continue;
                }

                var helmet = GadgetItemSpawner.SpawnFor(GadgetKind.Blindfold, walker.Player, toHands: false);
                if (helmet == null)
                    continue;

                try
                {
                    helmet.ServerSetPinned(home);
                    Helmets.Add((helmet, home, Time.unscaledTime + _duration));
                    Plugin.Log.LogInfo($"{Tag} Big Mask on {walker.Player.gameObject.name} for {_duration}s.");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} Could not put the helmet on {walker.Player.gameObject.name}: {ex.Message}");
                    NetworkServer.Destroy(helmet.gameObject);
                }
            }
        }

        // Host, every frame: a knocked-off helmet goes back on; one whose time is up leaves.
        private static void TickHelmets()
        {
            for (var i = Helmets.Count - 1; i >= 0; i--)
            {
                var (helmet, home, until) = Helmets[i];
                try
                {
                    if (helmet == null || helmet.gameObject == null || home == null)
                    {
                        Helmets.RemoveAt(i);
                        continue;
                    }

                    if (Time.unscaledTime >= until)
                    {
                        Helmets.RemoveAt(i);
                        helmet.blockRemovingFromHomes = false;
                        helmet.ServerSetUnpinned();
                        RetiringHelmets.Add((helmet, Time.unscaledTime + HelmetLinger));
                        Plugin.Log.LogInfo($"{Tag} Big Mask over for one player.");
                        continue;
                    }

                    if (helmet.currentHome != home)
                        helmet.ServerSetPinned(home);
                }
                catch (Exception ex)
                {
                    Helmets.RemoveAt(i);
                    Plugin.Log.LogWarning($"{Tag} A Big Mask helmet went wrong: {ex.Message}");
                }
            }
        }

        private static void RetireHelmets()
        {
            for (var i = RetiringHelmets.Count - 1; i >= 0; i--)
            {
                var (helmet, at) = RetiringHelmets[i];
                if (Time.unscaledTime < at)
                    continue;
                RetiringHelmets.RemoveAt(i);
                try
                {
                    if (helmet != null && helmet.gameObject != null)
                        NetworkServer.Destroy(helmet.gameObject);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} Could not take a Big Mask helmet out of the world: {ex.Message}");
                }
            }
        }
    }
}
