using System.Text;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Lists nearby objects whose name or a component matches a keyword, with path and components.
    internal static class DebugComponentLookup
    {
        private static readonly string[] DefaultKeywords = { "arch", "door", "switch", "button", "gate", "peck", "shortcut", "bell", "chime", "gong", "cowbell", "sphere", "orb", "void", "block", "barrier", "wall", "unlock", "skip", "proven", "reward", "postgame", "complet", "second", "ending", "cosmetic", "paper", "dispens", "ticket", "goodbye", "print", "hold", "tray", "slot", "speaker", "plug", "radio", "party", "club", "antenna", "cable", "stereo", "boombox" };

        internal static void DumpNearbyByKeyword(float radius)
        {
            var localPlayer = Core.LocalPlayer.Find();
            if (localPlayer == null)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugComponentLookup)}] Local player not found.");
                return;
            }

            var origin = localPlayer.transform.position;
            var sqrRadius = radius * radius;

            var allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            var entries = new List<(GameObject go, float sqrDistance, string components)>();

            foreach (var t in allTransforms)
            {
                if (t == null)
                    continue;

                var sqrDistance = (t.position - origin).sqrMagnitude;
                if (sqrDistance > sqrRadius)
                    continue;

                var go = t.gameObject;
                var components = DescribeComponents(go);
                if (!Matches(go.name, components))
                    continue;

                entries.Add((go, sqrDistance, components));
            }

            entries.Sort((a, b) => a.sqrDistance.CompareTo(b.sqrDistance));

            Plugin.Log.LogInfo($"[{nameof(DebugComponentLookup)}] {entries.Count} matching object(s) within a radius of {radius}m:");
            foreach (var entry in entries)
            {
                var distance = Math.Sqrt(entry.sqrDistance).ToString("F1");
                var path = DescribePath(entry.go.transform);
                Plugin.Log.LogInfo($"[{nameof(DebugComponentLookup)}]   {entry.go.name}, distance={distance}m, path={path}, components: {entry.components}");

                var trackedState = entry.go.GetComponent<TrackedPeckState>();
                if (trackedState != null)
                    DescribeTrackedPeckState(trackedState);
            }
        }

        internal static string DescribePath(Transform t)
        {
            var names = new List<string>();
            var current = t.parent;
            var guard = 0;
            while (current != null && guard++ < 20)
            {
                names.Insert(0, current.name);
                current = current.parent;
            }

            return names.Count == 0 ? "<root>" : string.Join("/", names);
        }

        private static void DescribeTrackedPeckState(TrackedPeckState state)
        {
            try
            {
                var saveIdentity = state.saveIdentity;
                var saveGuid = saveIdentity != null ? saveIdentity.saveGuid : "<no SaveIdentity>";
                var savableSystem = state.savableSystem;
                var key = savableSystem != SavableSystem.NotSavable ? savableSystem.ToString() : saveGuid;
                var currentValue = SaveManager.GetIntValue(key, -12345, false);

                Plugin.Log.LogInfo(
                    $"[{nameof(DebugComponentLookup)}]     TrackedPeckState, label='{state.label}', savableSystem={savableSystem}, " +
                    $"saveGuid={saveGuid}, actual key='{key}', SaveManager[{key}]={currentValue}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(DebugComponentLookup)}]     TrackedPeckState, exception while reading, ignored: {ex.Message}");
            }
        }

        internal static string DescribeComponents(GameObject go)
        {
            var components = go.GetComponents<Component>();
            var names = new StringBuilder();
            foreach (var c in components)
            {
                if (c == null)
                    continue;
                if (names.Length > 0)
                    names.Append(", ");
                names.Append(c.GetIl2CppType().Name);
            }

            return names.ToString();
        }

        private static bool Matches(string goName, string components)
        {
            foreach (var kw in DefaultKeywords)
            {
                if (goName.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (components.Contains(kw, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
