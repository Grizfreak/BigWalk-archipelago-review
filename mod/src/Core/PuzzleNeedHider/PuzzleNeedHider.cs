using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Hides the objects of every locked puzzle need, found by prefab name; local on every machine.
    internal class PuzzleNeedHider : MonoBehaviour
    {
        // Required by Il2CppInterop for any injected type.
        public PuzzleNeedHider(IntPtr ptr) : base(ptr)
        {
        }

        private const float SettleSeconds = 4f;
        private const float RecheckIntervalSeconds = 1f;

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

        private readonly Dictionary<string, List<GameObject>> _objects = new Dictionary<string, List<GameObject>>();
        private readonly HashSet<int> _hiddenByUs = new HashSet<int>();

        private readonly HashSet<int> _inGauntlet = new HashSet<int>();

        private readonly Dictionary<int, string[]> _skipNeeds = new Dictionary<int, string[]>();

        // Never SetActive(false): Mirror only sends a joining guest the active objects.
        private readonly HashSet<int> _networked = new HashSet<int>();

        private sealed class SoftState
        {
            internal Renderer[] Renderers;
            internal bool[] RendererWas;
            internal Collider[] Colliders;
            internal bool[] ColliderWas;
            internal Rigidbody[] Bodies;
            internal bool[] BodyWasKinematic;
            // Its home now, not when hidden: a moved prop takes the physics of where it is.
            internal Prop Prop;
            internal bool WasPinned;
        }

        internal static bool IsSoftHidden(GameObject go) =>
            go != null && _current != null && _current._soft.ContainsKey(go.GetInstanceID());

        private readonly Dictionary<int, SoftState> _soft = new Dictionary<int, SoftState>();

        private const float StatusIntervalSeconds = 60f;

        private static PuzzleNeedHider _current;

        private float _nextStatus;
        private float _readySince = -1f;
        private bool _collected;
        private int _appliedVersion = -1;
        private float _nextRecheck;

        private void Awake()
        {
            _current = this;
        }

        private void Update()
        {
            if (!WorldManager.isReadyForEffects)
            {
                // Needs survive the world: the connection outlives a return to the menu.
                if (_collected)
                {
                    SessionJournal.Write("world", "gone", withPosition: false);
                    HandsJournal.WorldGone();
                }

                _readySince = -1f;
                _collected = false;
                _appliedVersion = -1;
                _objects.Clear();
                _inGauntlet.Clear();
                _skipNeeds.Clear();
                _networked.Clear();
                _soft.Clear();

                // _hiddenByUs is kept: the world can blink out of ready and come back the same.
                return;
            }

            if (_readySince < 0f)
                _readySince = Time.time;

            if (!_collected)
            {
                if (Time.time - _readySince < SettleSeconds)
                    return;

                Collect();
                _collected = true;
                SessionJournal.Write("world", "ready");
                HandsJournal.WorldReady();

                // A save hosted with Archipelago off must be vanilla.
                if (NetworkServer.active)
                {
                    if (ModConfig.ArchipelagoEnabled.Value)
                    {
                        PuzzleNeeds.Rearm();
                        if (AbilityLocks.Enabled)
                            AbilityLocks.Rearm();
                    }
                    else
                    {
                        PuzzleNeeds.Forget();
                        AbilityLocks.Forget();
                    }
                }
            }

            HandsJournal.Tick();

            if (_appliedVersion == PuzzleNeeds.Version && Time.time < _nextRecheck)
                return;

            _nextRecheck = Time.time + RecheckIntervalSeconds;
            Apply();
            _appliedVersion = PuzzleNeeds.Version;

            if (Time.time >= _nextStatus)
            {
                _nextStatus = Time.time + StatusIntervalSeconds;
                LogStatus(detailed: false);
            }
        }

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

        private static readonly string[] PanelNeeds =
        {
            PuzzleNeeds.IconPanels, PuzzleNeeds.DrawingPanels, PuzzleNeeds.PosePanels, PuzzleNeeds.SoundPanels,
            PuzzleNeeds.PointPanels,
        };

        // A tile's PropGroup family decides its need; speaker tiles are told apart by their rack.
        private static void CollectPanels(Dictionary<string, Dictionary<int, GameObject>> found, HashSet<int> gauntletIds)
        {
            var renderers = FindObjectsByType<PegTileRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (renderers == null)
                return;

            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.GetComponent<Prop>() == null)
                    continue;

                var path = PathOf(renderer.transform);
                if (!path.StartsWith(PuzzleRoot, StringComparison.Ordinal) || IsExcluded(path))
                    continue;

                var need = PanelNeed(renderer.propGroup.ToString(), path);
                if (need == null)
                    continue;

                found[need][renderer.gameObject.GetInstanceID()] = renderer.gameObject;
                if (path.Contains(GauntletRoot))
                    gauntletIds.Add(renderer.gameObject.GetInstanceID());
            }
        }

        private static string PanelNeed(string group, string path)
        {
            if (path.Contains("PegTileSetSpeaker"))
                return PuzzleNeeds.SoundPanels;
            if (group.StartsWith("PegTilePriest", StringComparison.Ordinal))
                return PuzzleNeeds.DrawingPanels;

            const string stem = "PegTile";
            if (group.Length <= stem.Length + 1 || !char.IsDigit(group[stem.Length + 1]))
                return null;

            switch (group[stem.Length])
            {
                case 'A': return PuzzleNeeds.IconPanels;
                case 'B': return PuzzleNeeds.PointPanels;
                case 'C': return PuzzleNeeds.DrawingPanels;
                case 'D': return PuzzleNeeds.PosePanels;
                default: return null;
            }
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

        private static bool Allowed(Rule rule, string path)
        {
            if (!rule.IgnoreExclusions && IsExcluded(path))
                return false;
            if (rule.InPuzzlesOnly && !path.StartsWith(PuzzleRoot, StringComparison.Ordinal))
                return false;
            return rule.PathMustContainOneOf == null || rule.PathMustContainOneOf.Any(path.Contains);
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

        internal static void LogStatus(bool detailed)
        {
            var self = _current;
            if (self == null || !self._collected)
            {
                Plugin.Log.LogInfo($"[{nameof(PuzzleNeedHider)}] {Stamp()} status: no world collected yet.");
                return;
            }

            var role = NetworkServer.active ? "host" : NetworkClient.active ? "guest" : "solo";
            var locked = PuzzleNeeds.LockedNeeds();
            var perNeed = self._objects.OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => (need: p.Key, total: p.Value.Count(o => o != null),
                    showing: p.Value.Count(o => o != null && self.IsShowing(o))))
                .ToList();

            var summary = $"{locked.Count} locked of {perNeed.Count}, "
                          + $"{perNeed.Sum(n => n.showing)} of {perNeed.Sum(n => n.total)} objects showing";
            Plugin.Log.LogInfo($"[{nameof(PuzzleNeedHider)}] {Stamp()} status ({role}): {summary}.");
            SessionJournal.Write(detailed ? "mark" : "status", $"{summary} | locked: {string.Join(",", locked)}");

            if (!detailed)
                return;

            foreach (var (need, total, showing) in perNeed)
                Plugin.Log.LogInfo(
                    $"[{nameof(PuzzleNeedHider)}]   {need}: {(IsLocked(need) ? "locked" : "open  ")} {showing}/{total} showing");
        }

        // A replicated object stays active, so ask what this hider made invisible.
        private bool IsShowing(GameObject go) =>
            go.activeSelf && !(_networked.Contains(go.GetInstanceID()) && _hiddenByUs.Contains(go.GetInstanceID()));

        internal static string Stamp() => DateTime.Now.ToString("HH:mm:ss");

        private static bool IsLocked(string need) =>
            need == GauntletStairways.ButtonsNeed ? GauntletStairways.ButtonsHidden : PuzzleNeeds.IsLocked(need);

        private bool IsObjectLocked(string key, bool needLocked, int id, bool gauntletPartsLocked)
        {
            if (key == GauntletStairways.ButtonsNeed)
                return GauntletStairways.ButtonsHidden;

            if (_inGauntlet.Contains(id) && !gauntletPartsLocked)
                return false;

            if (key != SkipAidNeed)
                return needLocked;

            return _skipNeeds.TryGetValue(id, out var needs) && needs.Any(PuzzleNeeds.IsLocked);
        }

        private void Apply()
        {
            var gauntletPartsLocked = GauntletStairways.PartsHidden;
            foreach (var pair in _objects)
            {
                var needLocked = IsLocked(pair.Key);
                var locked = needLocked;
                var hid = 0;
                var showed = 0;
                foreach (var go in pair.Value)
                {
                    if (go == null)
                        continue;

                    var id = go.GetInstanceID();
                    var networked = _networked.Contains(id);

                    locked = IsObjectLocked(pair.Key, needLocked, id, gauntletPartsLocked);
                    if (locked)
                    {
                        if (networked)
                        {
                            if (SoftHide(go, id))
                                hid++;
                        }
                        else if (go.activeSelf)
                        {
                            _hiddenByUs.Add(id);
                            go.SetActive(false);
                            hid++;
                        }
                    }
                    else if (_hiddenByUs.Remove(id))
                    {
                        if (networked)
                            SoftShow(id);
                        else
                            go.SetActive(true);

                        showed++;
                    }
                }

                // What was done, not what the need says: poles follow their own puzzle's needs.
                if (hid > 0)
                    Plugin.Log.LogInfo(
                        $"[{nameof(PuzzleNeedHider)}] {Stamp()} {pair.Key}: hid {hid} of {pair.Value.Count}.");
                if (showed > 0)
                    Plugin.Log.LogInfo(
                        $"[{nameof(PuzzleNeedHider)}] {Stamp()} {pair.Key}: showed {showed} of {pair.Value.Count}.");
            }
        }

        // Run every second: the game's culling and scripts switch renderers back on.
        private bool SoftHide(GameObject go, int id)
        {
            var first = !_soft.TryGetValue(id, out var state);
            if (first)
            {
                state = new SoftState
                {
                    Renderers = go.GetComponentsInChildren<Renderer>(true),
                    Colliders = go.GetComponentsInChildren<Collider>(true),
                    Bodies = go.GetComponentsInChildren<Rigidbody>(true),
                };
                state.RendererWas = state.Renderers.Select(r => r != null && r.enabled).ToArray();
                state.ColliderWas = state.Colliders.Select(c => c != null && c.enabled).ToArray();
                state.BodyWasKinematic = state.Bodies.Select(b => b != null && b.isKinematic).ToArray();
                state.Prop = go.GetComponent<Prop>();
                state.WasPinned = state.Prop != null && state.Prop.currentHome != null;
                _soft[id] = state;
            }

            _hiddenByUs.Add(id);

            // A renderer the game switches on while hidden is one it means to show.
            for (var i = 0; i < state.Renderers.Length; i++)
            {
                var renderer = state.Renderers[i];
                if (renderer != null && renderer.enabled)
                {
                    if (!first)
                        state.RendererWas[i] = true;
                    renderer.enabled = false;
                }
            }

            for (var i = 0; i < state.Colliders.Length; i++)
            {
                var collider = state.Colliders[i];
                if (collider != null && collider.enabled)
                {
                    if (!first)
                        state.ColliderWas[i] = true;
                    collider.enabled = false;
                }
            }

            // Without a collider a loose tile would fall out of the world.
            foreach (var body in state.Bodies)
            {
                if (body != null && !body.isKinematic)
                    body.isKinematic = true;
            }

            return first;
        }

        private void SoftShow(int id)
        {
            if (!_soft.TryGetValue(id, out var state))
                return;

            for (var i = 0; i < state.Renderers.Length; i++)
            {
                if (state.Renderers[i] != null)
                    state.Renderers[i].enabled = state.RendererWas[i];
            }

            for (var i = 0; i < state.Colliders.Length; i++)
            {
                if (state.Colliders[i] != null)
                    state.Colliders[i].enabled = state.ColliderWas[i];
            }

            // Held if it hangs in a home now, free to fall if not.
            var pinnedNow = state.Prop != null && state.Prop.currentHome != null;
            var moved = state.Prop != null && pinnedNow != state.WasPinned;
            for (var i = 0; i < state.Bodies.Length; i++)
            {
                var body = state.Bodies[i];
                if (body == null)
                    continue;
                body.isKinematic = moved && state.Prop.rb != null && body.Pointer == state.Prop.rb.Pointer
                    ? pinnedNow
                    : state.BodyWasKinematic[i];
            }

            _soft.Remove(id);
        }
    }
}
