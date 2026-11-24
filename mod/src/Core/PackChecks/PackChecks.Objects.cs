using System;
using System.Collections.Generic;
using System.Linq;
using BigWalkArchipelago.Core.Net;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal static partial class PackChecks
    {
        // Kinds counted by `object_sanity_kinds`; the others leave the island while it is on.
        private static readonly HashSet<string> CountedKinds = new();
        private static readonly HashSet<string> MirroredKinds = new();

        internal static readonly string[] AllObjectKinds =
        {
            "WalkieTalkieProp", "MegaphoneProp", "FmRadioProp", "BinocularsProp", "LaserProp", "TorchProp",
            "XrayGogglesProp", "ClockProp", "CompassProp", "FoldingMapProp",
            "CowBellLargeProp", "CowBellMediumProp", "CowBellSmallProp", "SalonBrushProp", "BuoyProp", "BuoyRedProp", "BuoyLight",
        };

        // Lamps stay when their kind is left out: puzzles use them.
        private static readonly string[] StayWhenLeftOut = { "BuoyProp", "BuoyRedProp", "BuoyLight" };

        // Never taken off the island: the Salon's paintbrushes and the lamps.
        private static readonly string[] NeverTaken = { "SalonBrushProp", "BuoyProp", "BuoyRedProp", "BuoyLight" };

        private static HashSet<string> Counted => IsGuest ? MirroredKinds : CountedKinds;

        internal static IEnumerable<string> HostCountedKinds => CountedKinds;

        internal static string ObjectKindOf(string name)
        {
            foreach (var kind in AllObjectKinds)
            {
                if (name == kind || name.StartsWith(kind + " (", StringComparison.Ordinal))
                    return kind;
            }

            return null;
        }

        internal static bool KeepsObjectOnMap(string name)
        {
            var kind = ObjectKindOf(name);
            return KeepsObjectsOnMap && kind != null && Counted.Contains(kind);
        }

        internal static bool HidesObject(string name)
        {
            var kind = ObjectKindOf(name);
            return KeepsObjectsOnMap && kind != null && !Counted.Contains(kind) && Array.IndexOf(NeverTaken, kind) < 0
                   && Array.IndexOf(StayWhenLeftOut, kind) < 0;
        }

        private static byte ObjectMode => IsGuest ? (_mirrored ? _mirroredObjectMode : ObjectsOff) : _objectMode;

        internal static bool KeepsObjectsOnMap => ObjectMode != ObjectsOff;

        internal static byte HostObjectMode => _objectMode;

        // For the clocks and cowbells, which the hiding pass never hid.
        private static bool _leftOutHidden;

        private static void HideLeftOutKinds()
        {
            if (_leftOutHidden || !KeepsObjectsOnMap)
                return;
            _leftOutHidden = true;

            var hidden = 0;
            foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsSortMode.None))
            {
                if (prop == null || ReceivedItemSpawner.IsCosmeticClone(prop) || prop.gameObject.name.Contains("(AP", StringComparison.Ordinal)
                    || !HidesObject(prop.gameObject.name))
                    continue;
                GadgetItemSpawner.HidePack(prop.gameObject);
                hidden++;
            }

            if (hidden > 0)
                Plugin.Log.LogInfo($"{Tag} {hidden} object(s) of kinds object_sanity_kinds leaves out taken off the map.");
        }

        private static bool OnObjectPickedUp(Prop prop, string guid)
        {
            if (_objectMode == ObjectsEvery)
            {
                var kind = ObjectKindOf(prop.gameObject.name);
                if (Array.IndexOf(_objectGuids, guid) < 0 || kind == null || !CountedKinds.Contains(kind))
                    return false;
                Report(ApLocationIds.ObjectPrefix + guid, $"{prop.gameObject.name} picked up");
                if (Array.IndexOf(NeverTaken, kind) < 0 && Collected.Add(guid))
                {
                    _nextTick = 0f;
                    ModChannel.SendSnapshotNow();
                }
                return true;
            }

            var found = ObjectKindOf(prop.gameObject.name);
            if (found == null || !CountedKinds.Contains(found) || Array.IndexOf(_objectKinds, found) < 0)
                return false;
            Report(ApLocationIds.ObjectKindPrefix + found, $"first {found} picked up");
            return true;
        }
    }
}
