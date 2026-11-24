using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Puzzles
{
    internal partial class PuzzleNeedHider : MonoBehaviour
    {
        // Hiding too much can softlock, so non-puzzle zones that share puzzle prefabs stay out.
        private const string PuzzleRoot = "Landmarks";
        private static readonly string[] Excluded =
        {
            "Gauntlet", "Credits",
            "EndingGate", "BlackTower", "SecondGoodbye", "GoodbyeGate", "OverflowMonument",
            "HubGate", "Lookouts", "MusicGarden", "PeakMarker", "SouthPeak", "FireworksPlatform",
            "IntroPavilion", "SpawnCourtyard",
            // Radio stations are checks the logic asks nothing for; two stand in puzzle folders.
            "BroadcastStation",
        };

        // Only the seven stages' parts, and only while `lock_gauntlet_needs` counts on them.
        private const string GauntletRoot = "SilentGauntlet";

        // Not one of PuzzleNeeds': each pole waits for the needs of its own puzzle.
        private const string SkipAidNeed = "skip_aid";
        private static readonly string[] GauntletSpared = { "Entrance", "NHoldFullSet", "CliffBlockingWalls" };

        private static bool IsGauntletStagePart(string path)
        {
            if (!path.Contains(GauntletRoot) || path.Contains("/Level7") || GauntletSpared.Any(s => path.Contains(s)))
                return false;

            return Enumerable.Range(0, 7).Any(level => path.Contains($"/Level{level}/"));
        }

        private static bool IsExcluded(string path)
        {
            foreach (var excluded in Excluded)
            {
                if (!path.Contains(excluded))
                    continue;

                if (excluded == "Gauntlet" && IsGauntletStagePart(path))
                    continue;

                return true;
            }

            return false;
        }

        // The highest ancestor whose name matches is hidden.
        private sealed class Rule
        {
            internal string Need;
            internal string[] Prefixes;
            internal bool InPuzzlesOnly = true;
            internal string[] PathMustContainOneOf;
            internal bool IgnoreExclusions;
        }

        private static readonly Rule[] Rules =
        {
            // BasicKnob is a door handle: hiding it could shut players in.
            new Rule { Need = PuzzleNeeds.Buttons, Prefixes = new[] { "BasicPokeButton", "BasicPushButton", "ButtonNHold" } },

            new Rule { Need = PuzzleNeeds.SyncButtons, Prefixes = new[] { "SimPressSwitch", "BreadcrumbStation", "MediumSimPressStation" } },

            // Only the red bulbs that carry information, not the room switches.
            new Rule { Need = PuzzleNeeds.Lights, Prefixes = new[] { "Light_Table_Red" }, InPuzzlesOnly = false },

            new Rule { Need = PuzzleNeeds.Speakers, Prefixes = new[] { "FixedRadio", "Intercom" } },
            new Rule { Need = PuzzleNeeds.Teapots, Prefixes = new[] { "KettleProp" } },
            new Rule { Need = PuzzleNeeds.Counter, Prefixes = new[] { "counter" } },
            new Rule { Need = PuzzleNeeds.CoordinatesComputer, Prefixes = new[] { "CoordinateTrackerProp" } },
            new Rule { Need = PuzzleNeeds.Eggs, Prefixes = new[] { "PegTileRoundProp" } },
            new Rule { Need = PuzzleNeeds.InkViewer, Prefixes = new[] { "FixedTelescope" },
                PathMustContainOneOf = new[] { "InvisibleInk", "GauntletChamberInvisi" } },
            new Rule { Need = PuzzleNeeds.BigHead, Prefixes = new[] { "BlindfoldProp" } },
            new Rule { Need = PuzzleNeeds.GolfBall, Prefixes = new[] { "cannonballProp" } },
            new Rule { Need = PuzzleNeeds.TimedTomato, Prefixes = new[] { "Pomodoro" } },

            // Skip-aid poles: shown once all needs of their own puzzle are in.
            new Rule
            {
                Need = SkipAidNeed, Prefixes = new[] { "SkipAid" }, InPuzzlesOnly = false, IgnoreExclusions = true,
            },

            // Stairway buttons: `locked_stages` replaces them with items.
            new Rule
            {
                Need = GauntletStairways.ButtonsNeed, Prefixes = new[] { "NHoldFullSet" }, InPuzzlesOnly = false,
                PathMustContainOneOf = new[] { "GourdTower_Stairs" }, IgnoreExclusions = true,
            },
        };

        private static bool Allowed(Rule rule, string path)
        {
            if (!rule.IgnoreExclusions && IsExcluded(path))
                return false;
            if (rule.InPuzzlesOnly && !path.StartsWith(PuzzleRoot, StringComparison.Ordinal))
                return false;
            return rule.PathMustContainOneOf == null || rule.PathMustContainOneOf.Any(path.Contains);
        }
    }
}
