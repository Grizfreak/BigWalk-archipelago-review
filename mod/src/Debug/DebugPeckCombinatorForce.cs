using System;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Writes a nearby combinator's watched states directly: N-hold buttons need two players.
    internal static class DebugPeckCombinatorForce
    {
        internal static void ForceNearby(float radius)
        {
            var localPlayer = Core.LocalPlayer.Find();
            if (localPlayer == null)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugPeckCombinatorForce)}] Local player not found.");
                return;
            }

            var origin = localPlayer.transform.position;
            var sqrRadius = radius * radius;

            var all = UnityEngine.Object.FindObjectsByType<PeckCombinator>(FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugPeckCombinatorForce)}] No PeckCombinator found in the current area.");
                return;
            }

            // Log what is around first, or an out-of-range combinator looks like a dead key.
            var nearestName = string.Empty;
            var nearestDistance = float.MaxValue;
            var inRange = 0;

            foreach (var combinator in all)
            {
                if (combinator == null)
                    continue;

                float sqrDistance;
                try
                {
                    sqrDistance = (combinator.transform.position - origin).sqrMagnitude;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(DebugPeckCombinatorForce)}] Unreadable PeckCombinator (position): {ex.Message}");
                    continue;
                }

                var distance = Mathf.Sqrt(sqrDistance);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestName = combinator.gameObject.name;
                }

                if (sqrDistance > sqrRadius)
                    continue;

                inRange++;
                Plugin.Log.LogInfo($"[{nameof(DebugPeckCombinatorForce)}] {combinator.gameObject.name} (PeckCombinator), starting force.");

                try
                {
                    ForceCombinator(combinator);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(DebugPeckCombinatorForce)}] {combinator.gameObject.name}, exception while forcing, block skipped: {ex}");
                }
            }

            if (inRange == 0)
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugPeckCombinatorForce)}] {all.Length} PeckCombinator(s) in the scene, none within {radius:0}m."
                    + (nearestName.Length > 0 ? $" Nearest: '{nearestName}' at {nearestDistance:0.0}m." : string.Empty));
        }

        private static void ForceCombinator(PeckCombinator combinator)
        {
            var rules = combinator.rules;
            if (rules == null || rules.Length == 0)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugPeckCombinatorForce)}]   (no rule)");
                return;
            }

            for (var i = 0; i < rules.Length; i++)
            {
                try
                {
                    var rule = rules[i];
                    Plugin.Log.LogInfo($"[{nameof(DebugPeckCombinatorForce)}]   rule[{i}], target desiredState={rule.desiredState}");

                    ForceState(rule.systems, rule.desiredState);

                    var block = rule.block;
                    if (block != null)
                        ForceState(block.systems, rule.desiredState);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(DebugPeckCombinatorForce)}]   rule[{i}], exception, ignored: {ex.Message}");
                }
            }
        }

        private static void ForceState(TrackedPeckState[] systems, int desiredState)
        {
            if (systems == null)
                return;

            for (var s = 0; s < systems.Length; s++)
            {
                var state = systems[s];
                if (state == null)
                    continue;

                try
                {
                    state.SetState(desiredState);
                    Plugin.Log.LogInfo($"[{nameof(DebugPeckCombinatorForce)}]     {state.gameObject.name}.SetState({desiredState}) called.");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(DebugPeckCombinatorForce)}]     SetState exception, ignored: {ex.Message}");
                }
            }
        }
    }
}
