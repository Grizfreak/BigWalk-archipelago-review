using System;
using BigWalkArchipelago.Core;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BigWalkArchipelago.Patches.Menus
{
    // Each setting line: what it shows and what its arrows change.
    internal static partial class SettingsMenuArchipelagoRows
    {
        private static void BuildSizeRow(SettingsRow template, Transform parent)
        {
            var row = BuildArrayRow(template, parent, "Archipelago text", "ARCHIPELAGO TEXT :", hostOnly: false,
                () => $"{Sizes[Current()].label} ({Sizes[Current()].size})",
                delta =>
                {
                    var next = (Current() + delta + Sizes.Length) % Sizes.Length;
                    ModConfig.ShowConnectionStatus.Value = true;
                    ModConfig.StatusFontSize.Value = Sizes[next].size;
                });
            _sizeRow = row.Row;
            Plugin.Log.LogInfo($"{Tag} Archipelago text size line added (copied from '{template.gameObject.name}').");
        }

        private static readonly int[] Amnesties = { -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        private static void BuildFlareRow(SettingsRow template, Transform parent)
        {
            BuildArrayRow(template, parent, "Gentle effects", "GENTLE EFFECTS :", hostOnly: false,
                () => ModConfig.GentleEffects.Value ? "On" : "Off",
                _ => ModConfig.GentleEffects.Value = !ModConfig.GentleEffects.Value);
        }

        private static readonly string[] Alerts = { "Text + sound", "Sound only", "Text only", "Off" };

        private static int CurrentAlerts() =>
            (ModConfig.ShowItemFeed.Value ? 0 : 2) + (ModConfig.ApSounds.Value ? 0 : 1);

        private static void BuildMessagesRows(SettingsRow template, Transform parent)
        {
            BuildArrayRow(template, parent, "Archipelago alerts", "ARCHIPELAGO ALERTS :", hostOnly: false,
                () => Alerts[CurrentAlerts()],
                delta =>
                {
                    var next = (CurrentAlerts() + delta + Alerts.Length) % Alerts.Length;
                    ModConfig.ShowItemFeed.Value = next < 2;
                    ModConfig.ApSounds.Value = next % 2 == 0;
                });

            BuildArrayRow(template, parent, "Objectives on screen", "OBJECTIVES ON SCREEN :", hostOnly: false,
                () => ModConfig.ShowObjectives.Value ? "Shown" : "Hidden",
                _ => ModConfig.ShowObjectives.Value = !ModConfig.ShowObjectives.Value);
        }

        // Position 0 is off, 1 the seed's amnesty, 2 and on a number of its own.
        private static void BuildDeathLinkRows(SettingsRow template, Transform parent)
        {
            int Position() => !ModConfig.DeathLinkEnabled.Value
                ? 0
                : 1 + Math.Max(0, Array.IndexOf(Amnesties, ModConfig.DeathLinkAmnesty.Value));

            BuildArrayRow(template, parent, "DeathLink", "DEATHLINK :", hostOnly: true,
                () =>
                {
                    var notInSeed = !Traps.SeedHasDeathLink && Core.Net.ApRuntime.ShufflesPegTiles ? " (not in this seed)" : string.Empty;
                    if (!ModConfig.DeathLinkEnabled.Value)
                        return "Off";
                    return ModConfig.DeathLinkAmnesty.Value < 0
                        ? (Core.Net.ApRuntime.ShufflesPegTiles ? $"On, seed's amnesty ({Traps.SeedAmnesty})" : "On, seed's amnesty") + notInSeed
                        : $"On, amnesty {ModConfig.DeathLinkAmnesty.Value}" + notInSeed;
                },
                delta =>
                {
                    var next = (Position() + delta + Amnesties.Length + 1) % (Amnesties.Length + 1);
                    var wasOn = ModConfig.DeathLinkEnabled.Value;
                    ModConfig.DeathLinkEnabled.Value = next > 0;
                    if (next > 0)
                        ModConfig.DeathLinkAmnesty.Value = Amnesties[next - 1];
                    if (wasOn != ModConfig.DeathLinkEnabled.Value)
                        Core.Net.ApRuntime.ApplyDeathLinkSetting();
                    Plugin.Log.LogInfo($"{Tag} DeathLink: {(next == 0 ? "off" : ModConfig.DeathLinkAmnesty.Value < 0 ? "on, the seed's amnesty" : $"on, amnesty {ModConfig.DeathLinkAmnesty.Value}")}.");
                });
        }

        private static void BuildHintRow(SettingsRow template, Transform parent)
        {
            BuildArrayRow(template, parent, "Guests ask hints", "GUESTS ASK HINTS :", hostOnly: true,
                () => ModConfig.GuestsCanAskHints.Value ? "Yes" : "No",
                _ =>
                {
                    ModConfig.GuestsCanAskHints.Value = !ModConfig.GuestsCanAskHints.Value;
                    Plugin.Log.LogInfo($"{Tag} Guests can ask hints: {ModConfig.GuestsCanAskHints.Value}.");
                });
        }

        private static readonly string[] PoolModes = { "Over the seed", "Game's tiles only", "Off" };

        private static void BuildTilePoolRow(SettingsRow template, Transform parent)
        {
            BuildArrayRow(template, parent, "Tile pool", "TILE POOL :", hostOnly: true,
                () => PoolModes[Math.Clamp(ModConfig.TilePool.Value, 0, PoolModes.Length - 1)],
                delta =>
                {
                    ModConfig.TilePool.Value = (ModConfig.TilePool.Value + delta + PoolModes.Length) % PoolModes.Length;
                    Plugin.Log.LogInfo($"{Tag} Tile pool: {PoolModes[ModConfig.TilePool.Value]}.");
                });
        }

        private static int Current()
        {
            var size = ModConfig.StatusFontSize.Value;
            var best = 0;
            for (var i = 1; i < Sizes.Length; i++)
            {
                if (Math.Abs(Sizes[i].size - size) < Math.Abs(Sizes[best].size - size))
                    best = i;
            }

            return best;
        }
    }
}
