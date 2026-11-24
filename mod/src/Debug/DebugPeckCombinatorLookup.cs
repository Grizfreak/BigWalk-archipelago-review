using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Read-only dump of nearby PeckCombinators; each reference in its own try against a bad object.
    internal static class DebugPeckCombinatorLookup
    {
        internal static void DumpNearby(float radius)
        {
            var localPlayer = Core.LocalPlayer.Find();
            if (localPlayer == null)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugPeckCombinatorLookup)}] Local player not found.");
                return;
            }

            var origin = localPlayer.transform.position;
            var sqrRadius = radius * radius;

            var all = UnityEngine.Object.FindObjectsByType<PeckCombinator>(FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugPeckCombinatorLookup)}] No PeckCombinator found in the current area.");
                return;
            }

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
                    Plugin.Log.LogWarning($"[{nameof(DebugPeckCombinatorLookup)}] Unreadable PeckCombinator (position): {ex.Message}");
                    continue;
                }

                if (sqrDistance > sqrRadius)
                    continue;

                // Logged before reading any field, with the path: several share the same name.
                Plugin.Log.LogInfo($"[{nameof(DebugPeckCombinatorLookup)}] {combinator.gameObject.name} (PeckCombinator) at "
                                   + $"{Mathf.Sqrt(sqrDistance):0.0}m, {PathOf(combinator.transform)}, starting inspection.");

                try
                {
                    DescribeCombinator(combinator);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(DebugPeckCombinatorLookup)}] {combinator.gameObject.name}, exception during inspection, block skipped: {ex}");
                }
            }
        }

        private static void DescribeCombinator(PeckCombinator combinator)
        {
            DescribeTrackedPeckState("  directControlSystem", combinator.directControlSystem);
            DescribeSwitch("  onConditionMet", combinator.onConditionMet);
            DescribeSwitch("  onConditionStop", combinator.onConditionStop);

            var rules = combinator.rules;
            if (rules == null || rules.Length == 0)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugPeckCombinatorLookup)}]   (no rule)");
                return;
            }

            for (var i = 0; i < rules.Length; i++)
            {
                try
                {
                    var rule = rules[i];
                    var maxDesc = rule.hasMaximum ? rule.maximumMatches.ToString() : "unlimited";
                    Plugin.Log.LogInfo(
                        $"[{nameof(DebugPeckCombinatorLookup)}]   rule[{i}], minimumMatches={rule.minimumMatches}, maximumMatches={maxDesc}, desiredState={rule.desiredState}");

                    var systems = rule.systems;
                    if (systems != null)
                    {
                        for (var s = 0; s < systems.Length; s++)
                            DescribeTrackedPeckState($"    systems[{s}]", systems[s]);
                    }

                    // N-hold rules group their buttons in rule.block, with an empty systems[].
                    DescribeBlock("    rule.block", rule.block);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(DebugPeckCombinatorLookup)}]   rule[{i}], exception, ignored: {ex.Message}");
                }
            }
        }

        private static void DescribeTrackedPeckState(string label, TrackedPeckState state)
        {
            try
            {
                if (state == null)
                {
                    Plugin.Log.LogInfo($"[{nameof(DebugPeckCombinatorLookup)}] {label}=<null>");
                    return;
                }

                var saveIdentity = state.saveIdentity;
                var saveGuid = saveIdentity != null ? saveIdentity.saveGuid : "<no SaveIdentity>";
                var savableSystem = state.savableSystem;
                var key = savableSystem != SavableSystem.NotSavable ? savableSystem.ToString() : saveGuid;
                var currentValue = SaveManager.GetIntValue(key, -12345, false);

                Plugin.Log.LogInfo(
                    $"[{nameof(DebugPeckCombinatorLookup)}] {label}={state.gameObject.name}, live state={state.currentPeckContext.compressedState}, "
                    + $"savableSystem={savableSystem}, saveGuid={saveGuid}, actual key='{key}', SaveManager[{key}]={currentValue}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(DebugPeckCombinatorLookup)}] {label}, exception while reading, ignored: {ex.Message}");
            }
        }

        private static void DescribeBlock(string label, PeckSystemBlock block)
        {
            try
            {
                if (block == null)
                {
                    Plugin.Log.LogInfo($"[{nameof(DebugPeckCombinatorLookup)}] {label}=<null>");
                    return;
                }

                Plugin.Log.LogInfo($"[{nameof(DebugPeckCombinatorLookup)}] {label}={block.gameObject.name} (PeckSystemBlock)");

                var systems = block.systems;
                if (systems == null || systems.Length == 0)
                {
                    Plugin.Log.LogInfo($"[{nameof(DebugPeckCombinatorLookup)}] {label}, (no system in the block)");
                    return;
                }

                for (var s = 0; s < systems.Length; s++)
                    DescribeTrackedPeckState($"{label}.systems[{s}]", systems[s]);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(DebugPeckCombinatorLookup)}] {label}, exception while reading, ignored: {ex.Message}");
            }
        }

        private static void DescribeSwitch(string label, PeckSwitch sw)
        {
            try
            {
                if (sw == null)
                {
                    Plugin.Log.LogInfo($"[{nameof(DebugPeckCombinatorLookup)}] {label}=<null>");
                    return;
                }

                var tracked = sw.trackedStateSystem;
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugPeckCombinatorLookup)}] {label}={sw.gameObject.name} (PeckSwitch, specificState={sw.specificState}"
                    + (tracked != null ? $", its state's live value={tracked.currentPeckContext.compressedState}" : "") + ")");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(DebugPeckCombinatorLookup)}] {label}, exception while reading, ignored: {ex.Message}");
                return;
            }

            DescribeTrackedPeckState($"{label}.trackedStateSystem", sw.trackedStateSystem);
        }

        private static string PathOf(Transform t)
        {
            var path = t.name;
            for (var p = t.parent; p != null; p = p.parent)
                path = p.name + "/" + path;
            return path;
        }
    }
}
