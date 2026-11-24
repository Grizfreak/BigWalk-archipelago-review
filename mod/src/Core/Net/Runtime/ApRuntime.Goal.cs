using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    internal partial class ApRuntime : MonoBehaviour
    {
        // Uses the cached count: this runs every frame and the tracker walks the whole save.
        private static void RefreshGoalLine()
        {
            RefreshGoalLineCore();

            var towers = MonumentOrder.HudLine();
            if (towers != null)
                GoalLine = string.IsNullOrEmpty(GoalLine) ? towers : GoalLine + "\n" + towers;
        }

        private static void RefreshGoalLineCore()
        {
            // SlotData is null until login, and this runs from the first hosting frame.
            var slotData = Connection.SlotData;
            if (slotData == null || !ModConfig.ArchipelagoEnabled.Value)
            {
                GoalLine = null;
                return;
            }

            if (ApObjectives.Active)
            {
                GoalLine = ModConfig.ShowObjectives.Value ? ApObjectives.Summary() : null;
                GoalLineIsWarning = !ApObjectives.Detectable;
                return;
            }

            string goal;
            switch (slotData.Goal)
            {
                case "gauntlet":
                    goal = "Big Goodbye, complete the Silent Gauntlet behind the Big Wall";
                    break;

                case "ending":
                    goal = "Big Wall, break the bell inside the Big Wall";
                    break;

                case "second_ending":
                    goal = "Big Game, reach the secret ending behind the Spawn Secret Door";
                    break;

                case "deposits":
                    var target = slotData.DepositGoalAmount;
                    goal = $"Big Collection, place {target} {GourdNames.Plural} in the towers' slots ({_depositCount}/{target})";
                    break;

                default:
                    GoalLine = $"Goal: {slotData.Goal} - this build cannot detect it";
                    GoalLineIsWarning = true;
                    return;
            }

            // "reached" means reported to the server, not merely met.
            GoalLine = _goalSent ? $"Goal: {goal} - reached" : $"Goal: {goal}";
            GoalLineIsWarning = false;
        }

        // Called when the second ending's gourd comes loose; needs no world, only a socket.
        internal static void ReportGoalIfReached()
        {
            CheckGoal();
        }

        private static void CheckGoal()
        {
            if (_goalSent || !IsGoalReached())
                return;

            // Marked sent only once the socket took it; the next tick or connection retries.
            if (Connection.SendGoal())
                _goalSent = true;
        }

        private static bool IsGoalReached()
        {
            // Called from a Harmony patch whether or not this session ever connected.
            if (Connection.SlotData == null)
                return false;

            if (ApObjectives.Active)
                return ApObjectives.AllDone;

            switch (Connection.SlotData.Goal)
            {
                case "gauntlet":
                    return ApGoalFlags.IsLatched(SavableSystem.GauntletComplete);

                case "ending":
                    return ApGoalFlags.IsLatched(SavableSystem.EndingGate);

                case "deposits":
                    var target = Connection.SlotData.DepositGoalAmount;
                    return target > 0 && _depositCount >= target;

                case "second_ending":
                    return ApSecondEnding.Reached;

                default:
                    // Unreachable: OnJustConnected refuses an unknown goal.
                    return false;
            }
        }
    }
}
