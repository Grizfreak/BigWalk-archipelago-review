using System;
using System.Collections.Generic;
using System.Linq;

namespace BigWalkArchipelago.Core.Net
{
    // The seed's `objectives`, all required in any order; a lone `goal` is a list of one.
    internal static class ApObjectives
    {
        private static readonly string[] Known =
        {
            "big_wall", "big_goodbye", "big_game", "big_collection", "big_keys", "big_radio", "big_towers",
            "big_climb", "big_party", "big_lights", "big_objects", "big_body", "big_doof", "big_makeover",
        };
        private static readonly string[] Endings = { "big_goodbye", "big_game", "big_wall" };

        // Set by the debug key, to see the board and the overlay without a server.
        internal static string[] DebugKeys;

        internal static IReadOnlyList<string> Keys => DebugKeys ?? FromSlot();

        private static ApSlotData _keysOf;
        private static string[] _keys = Array.Empty<string>();

        // The seed's list, or its goal alone.
        private static string[] FromSlot()
        {
            var slot = ApRuntime.SlotData;
            if (slot == null)
                return Array.Empty<string>();
            if (!ReferenceEquals(slot, _keysOf))
            {
                _keysOf = slot;
                var goal = slot.Goal switch
                {
                    "gauntlet" => "big_goodbye",
                    "ending" => "big_wall",
                    "second_ending" => "big_game",
                    "deposits" => "big_collection",
                    _ => null,
                };
                // big_fiesta: the party's name in a seed made by 1.0.2 or older.
                _keys = slot.Objectives is { Length: > 0 } listed
                    ? listed.Select(key => key == "big_fiesta" ? "big_party" : key).ToArray()
                    : goal != null ? new[] { goal } : Array.Empty<string>();
            }

            return _keys;
        }

        internal static bool Active => Keys.Count > 0;

        // An objective this build cannot detect would never finish, so the list is refused.
        internal static bool Detectable =>
            Active && Keys.All(key => Array.IndexOf(Known, key) >= 0 && (!ObjectiveSpots.IsSpot(key) || ObjectiveSpots.Known(key)));

        // The tracker keeps the game's names: a big key's prop and a station's SavableSystem.
        internal static int KeysPlaced() =>
            CheckTracker.GetReportedLocationNames().Count(name =>
                Enum.TryParse<SaveablePropName>(name, out var prop) && GourdRegistry.IsBigKey(prop));

        internal static int StationsOn() =>
            CheckTracker.GetReportedLocationNames().Count(name =>
                Enum.TryParse<SavableSystem>(name, out var system) && RadioStations.IsRealStation(system));

        // A made-up few when the debug key's fake list is on and no server has said anything.
        private static int GourdTarget => ApRuntime.SlotData != null ? ApRuntime.SlotData.DepositGoalAmount : DebugKeys != null ? 10 : 0;
        private static int TowersTarget => ApRuntime.SlotData != null ? ApRuntime.SlotData.TowersRequired : 0;
        private static int KeysTarget => ApRuntime.SlotData != null ? ApRuntime.SlotData.KeysRequired : DebugKeys != null ? 3 : int.MaxValue;
        private static int StationsTarget => ApRuntime.SlotData != null ? ApRuntime.SlotData.RadioStationsRequired : DebugKeys != null ? 3 : int.MaxValue;

        private static int ObjectsTarget => ApRuntime.SlotData != null ? ApRuntime.SlotData.ObjectsRequiredCount : DebugKeys != null ? 5 : 0;
        private static int BodyTarget => ApRuntime.SlotData != null ? ApRuntime.SlotData.BodyRequiredCount : DebugKeys != null ? 5 : 0;

        // Object Sanity checks sent: each object (every) or each kind (first_pickup).
        internal static int ObjectsPicked() =>
            CheckTracker.GetReportedLocationNames().Count(name =>
                name.StartsWith(ApLocationIds.ObjectPrefix, StringComparison.Ordinal)
                || name.StartsWith(ApLocationIds.ObjectKindPrefix, StringComparison.Ordinal));

        private static int LightsTarget => ApRuntime.SlotData != null ? ApRuntime.SlotData.LightsRequiredCount : DebugKeys != null ? 5 : 0;

        internal static bool IsEnding(string key) => Array.IndexOf(Endings, key) >= 0;

        internal static bool IsDone(string key)
        {
            switch (key)
            {
                case "big_wall":
                    return ApGoalFlags.IsLatched(SavableSystem.EndingGate);
                case "big_goodbye":
                    return ApGoalFlags.IsLatched(SavableSystem.GauntletComplete);
                case "big_game":
                    return ApSecondEnding.Reached;
                case "big_collection":
                    return GourdTarget > 0 && ApRuntime.DepositCount >= GourdTarget;
                case "big_towers":
                    return MonumentOrder.TowersFull(TowersTarget);
                case "big_climb":
                case "big_party":
                case "big_doof":
                case "big_makeover":
                    return ObjectiveSpots.IsDone(key);
                case "big_keys":
                    return KeysPlaced() >= KeysTarget;
                case "big_radio":
                    return StationsOn() >= StationsTarget;
                case "big_lights":
                    return LightsTarget > 0 && PackChecks.LightsOn() >= LightsTarget;
                case "big_objects":
                    return ObjectsTarget > 0 && ObjectsPicked() >= ObjectsTarget;
                case "big_body":
                    return BodyTarget > 0 && AbilityLocks.ItemsReceived >= BodyTarget;
                default:
                    return false;
            }
        }

        internal static bool AllDone => Active && Keys.All(IsDone);

        // The objectives other than this one: what an ending waits for with `lock_endings`.
        internal static bool OthersDone(string key) => Keys.Where(other => other != key).All(IsDone);

        internal static int DoneCount => Keys.Count(IsDone);

        internal static string Label(string key)
        {
            switch (key)
            {
                case "big_climb":
                    return "Big Climb (highest point)";
                case "big_party":
                    return "Big Party (on the beach)";
                case "big_doof":
                    return "Big Doof (light up the Doof Chamber)";
                case "big_makeover":
                    return "Big Makeover (painted at the Secret Salon)";
                case "big_towers":
                    return $"Big Towers ({MonumentOrder.FullCount()}/{TowersTarget} full)";
                case "big_wall":
                    return "Big Wall (break the bell)";
                case "big_keys":
                    return $"Big Keys ({KeysPlaced()}/{KeysTarget} placed)";
                case "big_radio":
                    return $"Big Radio ({StationsOn()}/{StationsTarget} stations)";
                case "big_lights":
                    return $"Big Lights ({PackChecks.LightsOn()}/{LightsTarget} lit)";
                case "big_objects":
                    return $"Big Objects ({ObjectsPicked()}/{ObjectsTarget} picked up)";
                case "big_body":
                    return $"Big Body ({AbilityLocks.ItemsReceived}/{BodyTarget} abilities)";
                case "big_goodbye":
                    return "Big Goodbye (the Gauntlet)";
                case "big_game":
                    return "Big Game (the secret ending)";
                case "big_collection":
                    return $"Big Collection ({ApRuntime.DepositCount}/{GourdTarget} {GourdNames.Plural})";
                default:
                    return key;
            }
        }

        // One line each, for the overlay, which splits on newlines.
        internal static string Summary() =>
            $"Objectives {DoneCount}/{Keys.Count}"
            + string.Concat(Keys.Select(key => $"\n   [{(IsDone(key) ? "x" : "  ")}]  {(IsEnding(key) ? $"<b>{Label(key)}</b>" : Label(key))}"));
    }
}
