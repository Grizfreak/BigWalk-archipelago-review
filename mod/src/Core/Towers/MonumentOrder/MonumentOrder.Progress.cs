using System;
using System.Collections.Generic;
using BigWalkArchipelago.Core.Net;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Towers
{
    internal static partial class MonumentOrder
    {
        // `monument_full_keys`: a monument sends its check once full, if it has slots.
        private static float _nextFullCheck;

        internal static void TickFullChecks()
        {
            if (!NetworkServer.active || Time.unscaledTime < _nextFullCheck || !WorldManager.isReadyForEffects)
                return;
            _nextFullCheck = Time.unscaledTime + 1f;

            var slot = ApRuntime.SlotData;
            if (slot == null)
                return;

            foreach (var prop in slot.MonumentFullKeys)
            {
                var tower = IndexOfProp(prop);
                if (tower < 0 || SlotsOf(tower) <= 0 || !IsFull(tower))
                    continue;
                var location = ApLocationIds.MonumentPrefix + prop;
                if (!CheckTracker.TryMarkReported(location))
                    continue;
                Plugin.Log.LogInfo($"{Tag} {Names[tower]}'s monument is full: {location}.");
                Plugin.Reporter.ReportCheck(location);
            }

            // `linear_towers: one_deposit_box`: the key once the monument holds its drawn gourds.
            for (var i = 0; i < slot.MonumentKeyKeys.Length && i < slot.MonumentKeyGourds.Length; i++)
            {
                var prop = slot.MonumentKeyKeys[i];
                var tower = IndexOfProp(prop);
                if (tower < 0 || CosmeticMonumentFillTracker.FilledSlots(PrefixOf(tower)) < slot.MonumentKeyGourds[i])
                    continue;
                var location = ApLocationIds.MonumentKeyPrefix + prop;
                if (!CheckTracker.TryMarkReported(location))
                    continue;
                Plugin.Log.LogInfo($"{Tag} {Names[tower]}'s monument holds {slot.MonumentKeyGourds[i]} gourd(s): {location}.");
                Plugin.Reporter.ReportCheck(location);
            }
        }

        // Read each frame and the fill walks the save, so the line is remembered for a second.
        private static string _hudLine;
        private static float _hudAt = -10f;

        internal static string HudLine()
        {
            if (!Active || IsGuest)
                return null;

            var now = Time.unscaledTime;
            if (now - _hudAt < 1f)
                return _hudLine;

            _hudAt = now;
            _hudLine = null;
            foreach (var prop in Order)
            {
                var tower = IndexOfProp(prop);
                if (tower < 0 || IsFull(tower))
                    continue;

                if (!IsOpen(tower))
                {
                    // By number, not by name: the order is a surprise.
                    var needed = Array.IndexOf(Order, prop) + (FirstTowerOpen ? 0 : 1);
                    _hudLine = Mode == "by_item" && Granted < needed
                        ? $"Towers: none open, waiting for Progressive Monument {needed} ({Granted} received)"
                        : "Towers: none open";
                    break;
                }

                _hudLine = $"Tower open: {Names[tower].Replace("the ", string.Empty)} ({CosmeticMonumentFillTracker.FilledSlots(PrefixOf(tower))}/{SlotsOf(tower)})";
                break;
            }

            return _hudLine;
        }

        // The board's lines: the order, each tower full, open or waiting.
        internal static string BoardText()
        {
            if (!Active || IsGuest)
                return string.Empty;

            // The order is a surprise: only full and open towers are named, unless ShowTowerOrder.
            var text = "<b>MONUMENTS</b>";
            var hidden = 0;
            foreach (var prop in Order)
            {
                var tower = IndexOfProp(prop);
                if (tower < 0)
                    continue;

                var full = IsFull(tower);
                var open = IsOpen(tower);
                if (!full && !open && !ModConfig.ShowTowerOrder.Value)
                {
                    hidden++;
                    continue;
                }

                var state = full ? "[x]" : open ? " >" : "[  ]";
                text += $"\n{state} {Names[tower].Replace("the ", string.Empty)} ({CosmeticMonumentFillTracker.FilledSlots(PrefixOf(tower))}/{SlotsOf(tower)})";
            }

            if (hidden > 0)
                text += $"\n[  ] ??? ({hidden} more)";

            return text;
        }
    }
}
