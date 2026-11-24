using UnityEngine;

namespace BigWalkArchipelago.Core.Puzzles
{
    internal static partial class PuzzleFailures
    {
        private sealed class World
        {
            internal readonly Dictionary<int, string> FailureSwitches = new();

            // The failure switches of the PressInOrder puzzles, whose reset pecks them too.
            internal readonly HashSet<int> ResetBySetOff = new();

            // A PressInOrder's active state, by instance: its failure switch.
            internal readonly Dictionary<int, int> ActiveOf = new();

            // A failure peck waiting a few frames for the door that may turn the puzzle off.
            internal readonly Dictionary<int, int> PendingFrame = new();

            // When a PressInOrder last went off, by its failure switch.
            internal readonly Dictionary<int, int> OffFrame = new();

            // The Gauntlet stage of each id that Fail can receive.
            internal readonly Dictionary<int, int> StageOf = new();
            internal readonly Dictionary<int, string> Screens = new();

            // A synchronised button's state, by instance: the controller it belongs to.
            internal readonly Dictionary<int, int> SyncButtons = new();
            internal readonly Dictionary<int, string> SyncControllers = new();

            // A controller's success switch, by instance: the controller.
            internal readonly Dictionary<int, int> SyncSuccess = new();
            internal readonly HashSet<int> SyncDone = new();
            internal readonly Dictionary<int, int> LastState = new();

            // A tomato's state, by instance: its puzzle's name and location (the check name).
            internal readonly Dictionary<int, KeyValuePair<string, string>> Tomatoes = new();

            // A tomato that ran out, by instance: when to look whether its check went after all.
            internal readonly Dictionary<int, float> TomatoesOut = new();
            internal float ReadyAt;

            // One line per thing watched, for BepInEx/puzzle-failures.tsv.
            internal readonly List<string> Rows = new();
        }

        private static bool InDispenser(Transform transform)
        {
            for (var t = transform; t != null; t = t.parent)
            {
                if (t.name.StartsWith("PomodoroDispenser", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static World Build()
        {
            var world = new World { ReadyAt = Time.unscaledTime + WorldSettleSeconds };
            try
            {
                var gourds = new List<(string name, string location, Vector3 at)>();
                foreach (var gourd in UnityEngine.Object.FindObjectsByType<RewardGourd>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var prop = gourd != null ? gourd.prop : null;
                    if (prop != null && GourdRegistry.TryGetLocationId(prop.saveablePropName, out var location))
                        gourds.Add((prop.saveablePropName.ToString(), location, prop.transform.position));
                }

                (string name, string location) PuzzleOf(Transform at, int id)
                {
                    if (GauntletStairways.TryStageOf(at, out var stage))
                    {
                        world.StageOf[id] = stage;
                        var chamber = ChamberPrefix + stage;
                        return (chamber, chamber);
                    }

                    return Nearest(at.position);
                }

                (string name, string location) Nearest(Vector3 at)
                {
                    var nearest = ("a puzzle", string.Empty);
                    var best = float.MaxValue;
                    foreach (var gourd in gourds)
                    {
                        var distance = Vector3.Distance(gourd.at, at);
                        if (distance < best)
                        {
                            best = distance;
                            nearest = (gourd.name, gourd.location);
                        }
                    }

                    return nearest;
                }

                // `id` is the one Fail will get, so a solved stage is let off.
                string Name(Component owner, int id)
                {
                    var puzzle = PuzzleOf(owner.transform, id).name;
                    world.Rows.Add($"{owner.GetIl2CppType().Name}	{puzzle}	{SceneObjects.PathOf(owner.transform)}");
                    return $"{puzzle} ({owner.GetIl2CppType().Name})";
                }

                foreach (var screen in UnityEngine.Object.FindObjectsByType<ValidatorDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var state = screen != null ? screen.peckSystemReference.peckSystem : null;
                    if (state != null)
                        world.Screens[state.GetInstanceID()] = Name(screen, state.GetInstanceID());
                }

                foreach (var p in UnityEngine.Object.FindObjectsByType<PressInOrder>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (p != null && p.failureSwitch != null)
                    {
                        world.FailureSwitches[p.failureSwitch.GetInstanceID()] = Name(p, p.failureSwitch.GetInstanceID());
                        world.ResetBySetOff.Add(p.failureSwitch.GetInstanceID());
                        if (p.isActiveSystem.peckSystem != null)
                            world.ActiveOf[p.isActiveSystem.peckSystem.GetInstanceID()] = p.failureSwitch.GetInstanceID();
                    }
                }

                foreach (var c in UnityEngine.Object.FindObjectsByType<CountingController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (c != null && c.failureSwitch != null)
                        world.FailureSwitches[c.failureSwitch.GetInstanceID()] = Name(c, c.failureSwitch.GetInstanceID());
                }

                foreach (var sim in UnityEngine.Object.FindObjectsByType<SimPressController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (sim == null || sim.switches == null)
                        continue;

                    var controller = sim.GetInstanceID();
                    world.SyncControllers[controller] = Name(sim, controller);
                    if (sim.onSucess != null)
                        world.SyncSuccess[sim.onSucess.GetInstanceID()] = controller;
                    foreach (var button in sim.switches)
                    {
                        var state = button != null ? button.stateSystem : null;
                        if (state != null)
                        {
                            world.SyncButtons[state.GetInstanceID()] = controller;
                            if (world.StageOf.TryGetValue(controller, out var stage))
                                world.StageOf[state.GetInstanceID()] = stage;
                        }
                    }
                }

                // The tomatoes: the prop's own state, where its dispenser stands.
                foreach (var state in UnityEngine.Object.FindObjectsByType<TrackedPeckState>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (state == null || state.gameObject.name != "Pomodoro" || !InDispenser(state.transform))
                        continue;

                    var puzzle = PuzzleOf(state.transform, state.GetInstanceID());
                    world.Tomatoes[state.GetInstanceID()] = new KeyValuePair<string, string>(puzzle.name, puzzle.location);
                    world.Rows.Add($"Pomodoro	{puzzle.name}	{SceneObjects.PathOf(state.transform)}");
                }

                Plugin.Log.LogInfo(
                    $"{Tag} Watching {world.Screens.Count} validator screen(s), {world.FailureSwitches.Count} failure switch(es), "
                    + $"{world.SyncButtons.Count} synchronised button(s) of {world.SyncControllers.Count} puzzle(s), "
                    + $"{world.Tomatoes.Count} timed tomato(es).");

                var rows = new List<string>(world.Rows);
                rows.Sort((a, b) => string.CompareOrdinal(a.Split('	')[1], b.Split('	')[1]));
                rows.Insert(0, "kind	nearest puzzle	path");
                System.IO.File.WriteAllLines(System.IO.Path.Combine(BepInEx.Paths.BepInExRootPath, "puzzle-failures.tsv"), rows);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Could not list what a failure looks like: {ex.Message}");
            }

            return world;
        }
    }
}
