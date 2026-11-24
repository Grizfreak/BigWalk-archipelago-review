using System;
using System.Collections.Generic;
using BigWalkArchipelago.Core.Net;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal static partial class MonumentOrder
    {
        // The debug key's own mode and order, to see the monuments hold without a server.
        internal static string DebugMode;
        // Drawn again at each start of the debug key's cycle: a seed draws its own.
        internal static void ShuffleDebugOrder()
        {
            var random = new System.Random();
            for (var i = DebugOrder.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (DebugOrder[i], DebugOrder[j]) = (DebugOrder[j], DebugOrder[i]);
            }
        }

        internal static readonly string[] DebugOrder =
        {
            "bigKeyRedZone", "bigKeyIntro", "bigKeyGreenZone", "bigKeyBlueZone", "bigKeyYellowZone", "bigKeyBoss", "bigKeyOverflow",
        };

        // Debug key (Alt + 3): fills the first monument of the order that is not full, in the save.
        internal static void DebugFillNext()
        {
            if (!Active || IsGuest)
            {
                ApNotices.Post("Debug: no monument order to fill (Ctrl + 9)");
                return;
            }

            foreach (var prop in Order)
            {
                var tower = IndexOfProp(prop);
                if (tower < 0 || IsFull(tower))
                    continue;

                var missing = SlotsOf(tower) - CosmeticMonumentFillTracker.FilledSlots(PrefixOf(tower));
                var marked = CosmeticMonumentFillTracker.PutDepositsBack(missing);
                Plugin.Log.LogInfo($"{Tag} Debug: {marked} slot(s) of {Names[tower]} marked filled.");
                ApNotices.Post($"Debug: {Names[tower]} filled ({marked} slots)");
                return;
            }

            ApNotices.Post("Debug: every monument is full");
        }
    }
}
