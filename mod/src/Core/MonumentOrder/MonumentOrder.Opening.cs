using System;
using System.Collections.Generic;
using BigWalkArchipelago.Core.Net;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal static partial class MonumentOrder
    {
        // SavableSystem of each tower's opening button, by the towers above (-1: it has none).
        private static readonly string[] ButtonSystems =
        {
            null, "LookoutLightRed", "LookoutLightGreen", "LookoutLightBlue", "LookoutLightYellow", "BlackTowerInteriorDoor", null,
        };

        private static readonly SavableSystem[] ButtonEnums = ParseButtons();

        private static SavableSystem[] ParseButtons()
        {
            var result = new SavableSystem[ButtonSystems.Length];
            for (var i = 0; i < result.Length; i++)
                result[i] = ButtonSystems[i] != null && Enum.TryParse<SavableSystem>(ButtonSystems[i], out var parsed)
                    ? parsed
                    : SavableSystem.NotSavable;
            return result;
        }

        // The host's: one more monument opened by an item.
        internal static bool Grant()
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogWarning($"{Tag} Ignored: a Progressive Monument received while not host.");
                return false;
            }

            var granted = SaveManager.GetIntValue(GrantedKey, 0, false) + 1;
            SaveManager.SetIntValue(GrantedKey, granted);
            Plugin.Log.LogInfo($"{Tag} Progressive Monument {granted} received.");
            ApNotices.Post("Progressive Monument: another monument can open");
            return true;
        }

        internal static void ClearLedger() => SaveManager.SetIntValue(GrantedKey, 0);

        private static int Granted => SaveManager.GetIntValue(GrantedKey, 0, false);

        internal static bool IsFull(int tower) =>
            CosmeticMonumentFillTracker.FilledSlots(PrefixOf(tower)) >= SlotsOf(tower);

        // The host's own reckoning; a guest's is the mask it was told.
        internal static bool IsOpen(int tower)
        {
            if (!Active)
                return true;

            if (IsGuest)
                return (_mirroredOpen & (1 << tower)) != 0;

            var position = Array.IndexOf(Order, Props[tower]);
            if (position < 0)
                return true;

            for (var before = 0; before < position; before++)
            {
                var other = IndexOfProp(Order[before]);
                if (other >= 0 && !IsFull(other))
                    return false;
            }

            return Mode != "by_item" || Granted >= position + (FirstTowerOpen ? 0 : 1);
        }

        // A slot that already holds a gourd is never closed, so what a save restores goes through.
        internal static bool IsClosed(PropHome home)
        {
            if (!Active || home == null || !ReceivedItemSpawner.IsMonumentHome(home))
                return false;

            // Asked every frame while a gourd is held near the monument: remembered for a moment.
            var prefix = PrefixOfHome(home);
            var now = Time.unscaledTime;
            if (ClosedAt.TryGetValue(prefix, out var seen) && now - seen.at < 0.5f)
                return seen.closed;

            var tower = TowerOfPrefix(prefix);
            var closed = tower >= 0 && !IsOpen(tower) && CosmeticMonumentFillTracker.FilledSlots(prefix) == 0;
            ClosedAt[prefix] = (now, closed);
            return closed;
        }

        private static readonly Dictionary<string, (float at, bool closed)> ClosedAt = new();

        internal static bool HoldsButton(TrackedPeckState state)
        {
            if (state == null || !Active)
                return false;

            var system = state.savableSystem;
            for (var tower = 1; tower <= 5; tower++)
            {
                if (ButtonEnums[tower] == system)
                    return !IsOpen(tower);
            }

            return false;
        }

        internal static byte HostMask()
        {
            if (!Active)
                return 0;

            var mask = 0x80;
            for (var tower = 0; tower < Props.Length; tower++)
            {
                if (IsOpen(tower))
                    mask |= 1 << tower;
            }

            return (byte)mask;
        }

        internal static void ApplyFromHost(byte mask) => _mirroredOpen = (mask & 0x80) != 0 ? mask & 0x7F : -1;

        internal static void ForgetMirror() => _mirroredOpen = -1;

        // Null when all the monuments are open.
        internal static (string Prefix, int Slots)[] OrderedMonuments()
        {
            if (IsGuest || !Active)
                return null;

            var list = new List<(string, int)>();
            foreach (var prop in Order)
            {
                var tower = IndexOfProp(prop);
                if (tower >= 0)
                    list.Add((PrefixOf(tower), SlotsOf(tower)));
            }

            return list.ToArray();
        }

        // The first n towers of the order are full (the objective `big_towers`).
        internal static bool TowersFull(int count)
        {
            if (!Active || IsGuest)
                return false;

            for (var position = 0; position < count && position < Order.Length; position++)
            {
                var tower = IndexOfProp(Order[position]);
                if (tower < 0 || !IsFull(tower))
                    return false;
            }

            return count > 0;
        }

        internal static int FullCount()
        {
            var count = 0;
            if (!Active || IsGuest)
                return 0;

            foreach (var prop in Order)
            {
                var tower = IndexOfProp(prop);
                if (tower < 0 || !IsFull(tower))
                    break;
                count++;
            }

            return count;
        }
    }
}
