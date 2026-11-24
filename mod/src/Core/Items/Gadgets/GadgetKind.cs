using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Items
{
    // Hand props used as filler items, in data.py's FILLER_ITEMS order (TryResolveGadgetItem).
    internal enum GadgetKind
    {
        Megaphone,
        WalkieTalkie,
        Backpack,
        Belt,
        FlareGun,
        Laser,
        Binoculars,
        Compass,
        FoldingMap,
        Radio,
        GourdCarton,
        Torch,
        Lamp,
        XrayGoggles,
        FlareGunBlue,
        FlareGunGreen,
        FlareGunYellow,

        // Not an item: the Big Trip and Big Meeting tomato. Last, so no asset id or ticket moves.
        Pomodoro,

        // A white FlareGunProp clone whose shots cycle every colour. Last, for the same reason.
        RainbowFlareGun,

        // Not an item: Big Mask's blindfold helmet. Last, as the tomato.
        Blindfold,

        // Not an item: Big Time's clock, its hands on the real time. Last, as the tomato.
        Clock,

        // Not an item: Big Break's folding chair. Last, as the tomato.
        Chair,
    }
}
