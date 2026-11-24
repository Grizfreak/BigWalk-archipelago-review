using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal partial class PuzzleNeedHider : MonoBehaviour
    {
        // Only components every puzzle part carries; walking every Transform would be far slower.
        private void Collect()
        {
            _objects.Clear();
            _inGauntlet.Clear();
            _skipNeeds.Clear();
            var found = new Dictionary<string, Dictionary<int, GameObject>>();
            foreach (var need in Rules.Select(r => r.Need).Concat(PanelNeeds))
                found[need] = new Dictionary<int, GameObject>();

            var started = Time.realtimeSinceStartup;
            var candidates = new Dictionary<int, Transform>();
            Gather<PeckSwitch>(candidates, c => c.transform);
            Gather<SimPressSwitch>(candidates, c => c.transform);
            Gather<TrackedPeckState>(candidates, c => c.transform);
            Gather<HouseLight>(candidates, c => c.transform);
            Gather<Prop>(candidates, c => c.transform);

            foreach (var start in candidates.Values)
            {
                var chain = new List<(Transform transform, string name)>();
                for (var t = start; t != null; t = t.parent)
                    chain.Add((t, t.name));

                var path = string.Join("/", Enumerable.Reverse(chain).Select(n => n.name));
                foreach (var rule in Rules)
                {
                    if (!Allowed(rule, path))
                        continue;

                    Transform top = null;
                    foreach (var (transform, name) in chain)
                    {
                        if (rule.Prefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)))
                            top = transform;
                    }

                    if (top == null)
                        continue;

                    found[rule.Need][top.gameObject.GetInstanceID()] = top.gameObject;
                    if (path.Contains(GauntletRoot))
                        _inGauntlet.Add(top.gameObject.GetInstanceID());

                    foreach (var extra in ListeningPartner(top))
                        found[rule.Need][extra.gameObject.GetInstanceID()] = extra.gameObject;
                }
            }

            CollectPanels(found, _inGauntlet);
            BuildSkipNeeds(found);

            _networked.Clear();
            foreach (var group in found.Values)
            {
                foreach (var go in group.Values)
                {
                    if (go.GetComponentInChildren<NetworkIdentity>(true) != null)
                        _networked.Add(go.GetInstanceID());
                }
            }

            foreach (var need in found.Keys.ToList())
            {
                var objects = found[need].Values.ToList();
                _objects[need] = objects;
                var kinds = objects.GroupBy(o => o.name.Split(' ')[0]).OrderBy(g => g.Key, StringComparer.Ordinal)
                    .Select(g => $"{g.Key} x{g.Count()}");
                var replicated = objects.Count(o => _networked.Contains(o.GetInstanceID()));
                Plugin.Log.LogInfo(
                    $"[{nameof(PuzzleNeedHider)}] {need}: {objects.Count} object(s), {replicated} replicated - {string.Join(", ", kinds)}");
            }

            LogGauntletParts(found);

            Plugin.Log.LogInfo(
                $"[{nameof(PuzzleNeedHider)}] Collected in {(Time.realtimeSinceStartup - started):F2}s from {candidates.Count} candidate(s).");
        }

        // A pole's needs are those of the parts in its puzzle folder; a split puzzle shows early.
        private void BuildSkipNeeds(Dictionary<string, Dictionary<int, GameObject>> found)
        {
            if (!found.TryGetValue(SkipAidNeed, out var poles) || poles.Count == 0)
                return;

            var parts = new List<(string Need, string Path)>();
            foreach (var group in found)
            {
                if (group.Key == SkipAidNeed || group.Key == GauntletStairways.ButtonsNeed)
                    continue;

                foreach (var go in group.Value.Values)
                    parts.Add((group.Key, PathOf(go.transform) + "/"));
            }

            var withNeeds = 0;
            foreach (var pole in poles.Values)
            {
                var folder = PuzzleFolderOf(pole.transform);
                var needs = parts.Where(p => p.Path.StartsWith(folder, StringComparison.Ordinal))
                    .Select(p => p.Need).Distinct().ToArray();
                _skipNeeds[pole.GetInstanceID()] = needs;
                if (needs.Length > 0)
                    withNeeds++;
            }

            Plugin.Log.LogInfo(
                $"[{nameof(PuzzleNeedHider)}] Skip aids: {poles.Count} found, {withNeeds} wait for the parts of their puzzle.");
        }

        private static string PuzzleFolderOf(Transform pole)
        {
            for (var t = pole.parent; t != null; t = t.parent)
            {
                if (t.parent != null && t.parent.name.StartsWith("Level", StringComparison.Ordinal))
                    return PathOf(t) + "/";

                if (t.name.IndexOf("ositioner", StringComparison.OrdinalIgnoreCase) >= 0)
                    return PathOf(t.parent != null ? t.parent : t) + "/";
            }

            return PathOf(pole.parent != null ? pole.parent : pole) + "/";
        }

        // One line a stage, to set against the apworld's GAUNTLET_STAGE_TAGS.
        private void LogGauntletParts(Dictionary<string, Dictionary<int, GameObject>> found)
        {
            var perStage = new SortedDictionary<int, SortedDictionary<string, int>>();
            foreach (var group in found)
            {
                foreach (var go in group.Value.Values)
                {
                    if (go == null || !_inGauntlet.Contains(go.GetInstanceID()))
                        continue;

                    var path = PathOf(go.transform);
                    var at = path.IndexOf("/Level", StringComparison.Ordinal);
                    if (at < 0 || at + 7 > path.Length || !int.TryParse(path.Substring(at + 6, 1), out var level))
                        continue;

                    if (!perStage.TryGetValue(level, out var needs))
                        perStage[level] = needs = new SortedDictionary<string, int>(StringComparer.Ordinal);

                    needs[group.Key] = needs.TryGetValue(group.Key, out var count) ? count + 1 : 1;
                }
            }

            foreach (var stage in perStage)
                Plugin.Log.LogInfo(
                    $"[{nameof(PuzzleNeedHider)}] Gauntlet stage {stage.Key + 1} (Level{stage.Key}) parts: "
                    + string.Join(", ", stage.Value.Select(n => $"{n.Key} x{n.Value}")));
        }

        private static string PathOf(Transform transform)
        {
            var parts = new List<string>();
            for (var t = transform; t != null; t = t.parent)
                parts.Add(t.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static void Gather<T>(Dictionary<int, Transform> into, Func<T, Transform> transformOf)
            where T : UnityEngine.Object
        {
            var all = FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all == null)
                return;

            foreach (var item in all)
            {
                if (item == null)
                    continue;

                var transform = transformOf(item);
                into[transform.GetInstanceID()] = transform;
            }
        }

        // A "(sending)" radio has a "(listening)" twin of the same number, with no component.
        private static IEnumerable<Transform> ListeningPartner(Transform sending)
        {
            var name = sending.name;
            if (sending.parent == null || !name.Contains("(sending)"))
                yield break;

            var partner = sending.parent.Find(name.Replace("(sending)", "(listening)"));
            if (partner != null)
                yield return partner;
        }
    }
}
