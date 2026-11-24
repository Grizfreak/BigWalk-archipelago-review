using System;
using System.Collections.Generic;
using System.Linq;
using BigWalkArchipelago.Core.Net;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal static partial class Traps
    {
        private static readonly string[] PackNames = { "BackpackProp", "HolsterProp", "GourdCartonProp" };

        private static void FireLoad()
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null)
                return;

            List<Prop> contents = null;
            foreach (var player in players)
            {
                if (player == null || _onlyPlayer != null && player != _onlyPlayer)
                    continue;

                var behind = player.transform.position - player.transform.forward * 0.8f + Vector3.up * 0.6f;
                foreach (var home in player.GetComponentsInChildren<PropHome>(true))
                {
                    var pack = home != null ? home.pinnedProp : null;
                    if (pack == null || !Array.Exists(PackNames, n => pack.gameObject.name.StartsWith(n, StringComparison.Ordinal)))
                        continue;

                    // A worn pack's Kernal moves under the bones: the home's parentProp names it.
                    var spilled = 0;
                    foreach (var content in contents ??= AllProps())
                    {
                        var at = content != null ? content.currentHome : null;
                        if (at == null || content == pack || at.parentProp != pack)
                            continue;
                        if (Unpin(content, behind + UnityEngine.Random.insideUnitSphere * 0.4f))
                            spilled++;
                    }

                    if (Unpin(pack, behind))
                        Plugin.Log.LogInfo($"{Tag} Big Load: {player.gameObject.name} lost {pack.gameObject.name}, {spilled} thing(s) spilled.");
                }
            }
        }

        private static List<Prop> AllProps()
        {
            return new List<Prop>(UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
        }

        private static bool Unpin(Prop prop, Vector3 at)
        {
            try
            {
                prop.ServerSetUnpinned();
                prop.SetLoose();
                prop.transform.position = at;
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Big Load could not take {prop.gameObject.name} off: {ex.Message}");
                return false;
            }
        }

        // The floor under a point, looked for from a little above it.
        internal static Vector3 Ground(Vector3 point)
        {
            if (Physics.Raycast(point + Vector3.up * 2.5f, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 0.8f;
            return point + Vector3.up * 0.5f;
        }
    }
}
