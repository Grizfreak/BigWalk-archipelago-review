using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Puzzles
{
    internal static partial class PuzzleFailures
    {
        // A Gauntlet stage may be no location of this seed, so its chamber is read from the save.
        private static bool Solved(string location)
        {
            if (location.StartsWith(ChamberPrefix, StringComparison.Ordinal)
                && int.TryParse(location.Substring(ChamberPrefix.Length), out var level))
                return GauntletStairways.IsStageSolved(level);

            return !string.IsNullOrEmpty(location) && CheckTracker.IsReported(location);
        }

        private static void Fail(int id, string puzzle, World world)
        {
            if (Time.unscaledTime < world.ReadyAt)
                return;
            if (world.StageOf.TryGetValue(id, out var stage) && GauntletStairways.IsStageSolved(stage))
            {
                Plugin.Log.LogInfo($"{Tag} Not failed: {puzzle}, its Gauntlet stage is solved already.");
                return;
            }
            if (LastFired.TryGetValue(id, out var at) && Time.unscaledTime - at < Cooldown)
                return;
            LastFired[id] = Time.unscaledTime;

            Plugin.Log.LogInfo($"{Tag} Failed: {puzzle}.");
            SessionJournal.Write("puzzle failed", puzzle);
            Traps.Trigger("puzzle_failed", Describe(puzzle));
        }

        // `puzzle` is "gourdX (Kind)" or "gourdX (what happened)"; the slot name goes in front.
        private static readonly Dictionary<string, string> Hows = new()
        {
            { "PressInOrder", "buttons pressed out of order" },
            { "CountingController", "a wrong count" },
            { "ValidatorDisplay", "the validator screen went red" },
        };

        internal static string Describe(string puzzle)
        {
            var space = puzzle.IndexOf(' ');
            var key = space > 0 ? puzzle.Substring(0, space) : puzzle;
            var open = puzzle.LastIndexOf('(');
            var how = open > 0 ? puzzle.Substring(open + 1).TrimEnd(')') : null;
            if (how != null && Hows.TryGetValue(how, out var said))
                how = said;

            var name = key;
            if (Core.Net.ApLocationIds.TryResolveLocation(key, out var locationId))
                name = Core.Net.ApRuntime.LocationName(locationId);
            // The server has no name for a stage that is no location of this seed.
            if (key.StartsWith(ChamberPrefix, StringComparison.Ordinal) && (name == key || name == locationId.ToString())
                && int.TryParse(key.Substring(ChamberPrefix.Length), out var level))
                name = $"Silent Gauntlet's stage {level + 1}";

            return how == null ? $"failed the {name}" : $"failed the {name} ({how})";
        }
    }
}
