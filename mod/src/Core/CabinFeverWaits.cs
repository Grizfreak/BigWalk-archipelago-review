using System;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // The Cabin Fever waits from the slot, set on every machine since clients draw from `duration`.
    internal static class CabinFeverWaits
    {
        private sealed class Puzzle
        {
            internal string Key;
            internal string HousePath;
            internal float Vanilla;
            internal Vector3 HelpPoint;
            internal Vector3 HelpNormal;
            internal int HelpSlot;

            // 0 keeps the game's wait.
            internal int Seconds;
            internal bool Help;
            internal bool HideTimer;

            internal PeckEffectTimerNetworked Timer;
            internal float AppliedDuration = -1f;
        }

        // One help button inside each room.
        private static readonly Puzzle[] Puzzles =
        {
            new Puzzle
            {
                Key = "cabin_fever", HousePath = "CabinFever", Vanilla = 300f, HelpSlot = 18,
                HelpPoint = new Vector3(300.71f, 24.06f, -110.36f), HelpNormal = new Vector3(-0.73f, 0f, -0.68f).normalized,
            },
            new Puzzle
            {
                Key = "cabin_fever_long", HousePath = "CabinFeverLong", Vanilla = 1800f, HelpSlot = 19,
                HelpPoint = new Vector3(525.14f, 77.96f, -864.88f), HelpNormal = new Vector3(-0.07f, 0f, -1.00f).normalized,
            },
        };

        private const float HelpSeconds = 10f;
        private const float HelpSink = 0.25f;

        // A zero-length timer may not be one the game expects.
        private const float ShortestWait = 1f;

        internal static void Configure(string mode, int seconds, bool help, bool hide, string longMode, int longSeconds,
            bool longHelp, bool longHide)
        {
            Set(Puzzles[0], Resolve(mode, seconds, Puzzles[0].Vanilla), help, hide);
            Set(Puzzles[1], Resolve(longMode, longSeconds, Puzzles[1].Vanilla), longHelp, longHide);
        }

        internal static void ApplyFromHost(int seconds, int longSeconds, bool help, bool longHelp, bool hide, bool longHide)
        {
            Set(Puzzles[0], seconds, help, hide);
            Set(Puzzles[1], longSeconds, longHelp, longHide);
        }

        internal static void ForgetMirror()
        {
            Set(Puzzles[0], 0, false, false);
            Set(Puzzles[1], 0, false, false);
        }

        internal static int Seconds(int puzzle) => Puzzles[puzzle].Seconds;
        internal static bool Help(int puzzle) => Puzzles[puzzle].Help;

        internal static bool HideTimer(int puzzle) => Puzzles[puzzle].HideTimer;

        private static void Set(Puzzle puzzle, int seconds, bool help, bool hide)
        {
            if (puzzle.Seconds != seconds || puzzle.Help != help || puzzle.HideTimer != hide)
                Plugin.Log.LogInfo(
                    $"[{nameof(CabinFeverWaits)}] {puzzle.Key}: wait {(seconds > 0 ? seconds + " s" : "as in the game")}, help button {(help ? "on" : "off")}, timer {(hide ? "hidden" : "shown")}.");

            puzzle.Seconds = seconds;
            puzzle.Help = help;
            puzzle.HideTimer = hide;
        }

        // `random_between` was already drawn by the apworld, so it is a number like `fixed`.
        private static int Resolve(string mode, int seconds, float vanilla)
        {
            switch (mode)
            {
                case "reduced":
                    return (int)Math.Min(seconds, vanilla);
                case "random_between":
                case "fixed":
                    return seconds;
                default:
                    return 0;
            }
        }

        internal static void Tick()
        {
            foreach (var puzzle in Puzzles)
            {
                ApplyDuration(puzzle);
                SyncHelpButton(puzzle);
            }
        }

        private static void ApplyDuration(Puzzle puzzle)
        {
            var timer = FindTimer(puzzle);
            if (timer == null)
                return;

            var wanted = puzzle.Seconds > 0 ? Math.Max(puzzle.Seconds, ShortestWait) : puzzle.Vanilla;
            if (Mathf.Approximately(timer.duration, wanted) && Mathf.Approximately(puzzle.AppliedDuration, wanted))
                return;

            timer.duration = wanted;
            puzzle.AppliedDuration = wanted;
            Plugin.Log.LogInfo($"[{nameof(CabinFeverWaits)}] {puzzle.Key}: timer set to {wanted:F0} s.");
        }

        private static PeckEffectTimerNetworked FindTimer(Puzzle puzzle)
        {
            if (puzzle.Timer != null)
                return puzzle.Timer;

            foreach (var timer in UnityEngine.Object.FindObjectsByType<PeckEffectTimerNetworked>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (timer == null || timer.name != "TimerSystem")
                    continue;

                var parent = timer.transform.parent;
                var path = string.Empty;
                for (var depth = 0; depth < 4 && parent != null; depth++, parent = parent.parent)
                    path = parent.name + "/" + path;

                // The game loads the house for the chosen player count (PlayerCountSwapper).
                if (!IsHouse(path, puzzle.HousePath))
                    continue;

                puzzle.Timer = timer;
                puzzle.AppliedDuration = -1f;
                Plugin.Log.LogInfo($"[{nameof(CabinFeverWaits)}] {puzzle.Key}: timer found ({timer.duration:F0} s in the game).");
                return timer;
            }

            return null;
        }

        private static bool IsHouse(string path, string house)
        {
            var at = path.IndexOf("/" + house, StringComparison.Ordinal);
            if (at < 0)
                return false;

            // "CabinFever 2Player/" for two and three players, "CabinFever/" for four.
            var rest = path.Substring(at + 1 + house.Length);
            if (rest.StartsWith("/", StringComparison.Ordinal))
                return true;

            return rest.Length > 2 && rest[0] == ' ' && char.IsDigit(rest[1])
                   && rest.Substring(2).StartsWith("Player/", StringComparison.Ordinal);
        }

        private static void SyncHelpButton(Puzzle puzzle)
        {
            // No wait worth helping with: the button would do nothing.
            var wanted = puzzle.Help && (puzzle.Seconds == 0 || puzzle.Seconds > HelpSeconds);
            if (!wanted)
            {
                WorldButtons.Remove(puzzle.HelpSlot);
                return;
            }

            if (WorldButtons.Has(puzzle.HelpSlot))
                return;

            WorldButtons.Add(puzzle.HelpSlot, "Cabin Fever help", puzzle.HelpPoint - puzzle.HelpNormal * HelpSink,
                Quaternion.LookRotation(puzzle.HelpNormal, Vector3.up), presser => TakeTimeOff(puzzle),
                icon: null, tint: "hub", hostSide: true);
        }

        // Host only: the timer's end is a SyncVar.
        private static void TakeTimeOff(Puzzle puzzle)
        {
            if (!NetworkServer.active)
                return;

            var timer = FindTimer(puzzle);
            if (timer == null || !timer.timerActive)
            {
                Plugin.Log.LogInfo($"[{nameof(CabinFeverWaits)}] {puzzle.Key}: help pressed, but no wait is running.");
                return;
            }

            var remaining = timer.GetTimeRemaining();
            var taken = Math.Min(HelpSeconds, Math.Max(0f, remaining - 0.5f));
            timer.NetworkendTime = timer.EndTime - taken;
            Plugin.Log.LogInfo(
                $"[{nameof(CabinFeverWaits)}] {puzzle.Key}: help pressed, {taken:F0} s taken off, {remaining - taken:F0} s left.");
        }
    }
}
