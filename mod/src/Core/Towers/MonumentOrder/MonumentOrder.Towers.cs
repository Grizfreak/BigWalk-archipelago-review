using System;
using System.Collections.Generic;
using BigWalkArchipelago.Core.Net;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Towers
{
    internal static partial class MonumentOrder
    {
        private static int IndexOfProp(string prop) => Array.IndexOf(Props, prop);

        // What loaded monuments (or the save) have taught of `monoument0..3`.
        private static bool TryKnownTower(string prefix, out int tower)
        {
            if (Learned.TryGetValue(prefix, out tower))
                return true;

            var saved = SaveManager.GetIntValue(MapPrefix + prefix, 0, false);
            if (saved > 0)
            {
                tower = saved - 1;
                Learned[prefix] = tower;
                return true;
            }

            tower = -1;
            return false;
        }

        // The Green Dome gives up the slots the seed's gourds cannot fill.
        internal static int SlotsOf(int tower) =>
            tower == 6 && ApRuntime.SlotData != null ? Math.Min(Slots[6], ApRuntime.SlotData.GreenDomeSlots) : Slots[tower];

        internal static string PrefixOf(int tower)
        {
            if (tower == 0 || tower >= 5)
                return GuessedPrefixes[tower];

            for (var candidate = 1; candidate <= 4; candidate++)
            {
                if (TryKnownTower(GuessedPrefixes[candidate], out var known) && known == tower)
                    return GuessedPrefixes[candidate];
            }

            return GuessedPrefixes[tower];
        }

        private static int TowerOfPrefix(string prefix)
        {
            var guessed = Array.IndexOf(GuessedPrefixes, prefix);
            if (guessed == 0 || guessed >= 5)
                return guessed;

            return TryKnownTower(prefix, out var known) ? known : guessed;
        }

        internal static string PrefixOfHome(PropHome home)
        {
            var name = home.saveableHomeName.ToString();
            var slot = name.LastIndexOf("Slot", StringComparison.Ordinal);
            return slot > 0 ? name.Substring(0, slot) : name;
        }

        // The four `monoument0..3` are told apart by the tower they stand nearest.
        internal static void Learn(PropHome home)
        {
            if (home == null || !ReceivedItemSpawner.IsMonumentHome(home))
                return;

            var prefix = PrefixOfHome(home);
            var guessed = Array.IndexOf(GuessedPrefixes, prefix);
            if (guessed < 1 || guessed > 4)
                return;

            var best = -1;
            var bestDistance = float.MaxValue;
            for (var tower = 1; tower <= 4; tower++)
            {
                var offset = home.transform.position - Centres[tower];
                offset.y = 0f;
                if (offset.sqrMagnitude < bestDistance)
                {
                    bestDistance = offset.sqrMagnitude;
                    best = tower;
                }
            }

            if (Learned.TryGetValue(prefix, out var known) && known == best)
                return;

            Learned[prefix] = best;
            SaveManager.SetIntValue(MapPrefix + prefix, best + 1);
            Plugin.Log.LogInfo(
                $"{Tag} {prefix} stands at {home.transform.position} nearest {Names[best]}"
                + (guessed == best ? " (as guessed)." : $" (not {Names[guessed]}, as guessed): corrected."));
        }

        // A tower's position in the apworld's TOWERS (the order above), or -1.
        internal static int TowerIndexOf(string prop) => IndexOfProp(prop);
    }
}
