using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    internal static partial class Traps
    {
        // Big Reset: per group of players, the nearest unsolved puzzle's door is shut and reopened.
        private const float ResetGroup = 25f;
        private const float ResetRadius = 40f;

        // Resetting every nearby state freed the gourds through their vices: only the door cycles.
        private const float ResetShutSeconds = 0.4f;

        private static bool UnderGourdHolder(Transform t)
        {
            for (; t != null; t = t.parent)
            {
                if (t.name.IndexOf("GourdVice", StringComparison.Ordinal) >= 0 || t.name.IndexOf("GourdValet", StringComparison.Ordinal) >= 0)
                    return true;
            }
            return false;
        }
        private static readonly List<(TrackedPeckState state, int was, float at)> DoorsToReopen = new();

        private static void CycleDoor(string puzzle, Vector3 gourd, float best, int players)
        {
            LobbyDoor door = null;
            var nearest = ResetRadius;
            foreach (var candidate in UnityEngine.Object.FindObjectsByType<LobbyDoor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                // The gourd's vice is a LobbyDoor too: shut, it lets the gourd go.
                if (candidate == null || candidate.peckSystem.peckSystem == null || UnderGourdHolder(candidate.transform))
                    continue;
                var distance = Vector3.Distance(candidate.transform.position, gourd);
                if (distance < nearest)
                {
                    nearest = distance;
                    door = candidate;
                }
            }
            if (door == null)
            {
                Plugin.Log.LogInfo($"{Tag} Big Reset: {puzzle} ({best:F0} m from {players} player(s)) has no door within {ResetRadius:F0} m.");
                return;
            }
            var state = door.peckSystem.peckSystem.TryCast<TrackedPeckState>();
            if (state == null)
            {
                Plugin.Log.LogInfo($"{Tag} Big Reset: {puzzle}'s door {door.name} has no state of its own.");
                return;
            }
            var was = state.currentPeckContext.state;
            var shut = door.peckAndDoorStatesAreInverse ? 1 : 0;
            Plugin.Log.LogInfo($"{Tag} Big Reset: {puzzle} ({best:F0} m from {players} player(s)), door {door.name} at {nearest:F0} m, state {was}, shut at {shut}.");
            state.SetState(shut);
            DoorsToReopen.Add((state, was, Time.unscaledTime + ResetShutSeconds));
        }

        private static void TickDoors()
        {
            for (var i = DoorsToReopen.Count - 1; i >= 0; i--)
            {
                var (state, was, at) = DoorsToReopen[i];
                if (Time.unscaledTime < at)
                    continue;
                DoorsToReopen.RemoveAt(i);
                if (state != null && state.currentPeckContext.state != was)
                    state.SetState(was);
            }
        }

        private static void FireReset()
        {
            var gourds = new List<(Vector3 at, string name)>();
            foreach (var gourd in UnityEngine.Object.FindObjectsByType<RewardGourd>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                var prop = gourd != null ? gourd.prop : null;
                if (prop == null || !GourdRegistry.TryGetLocationId(prop.saveablePropName, out var location) || CheckTracker.IsReported(location))
                    continue;
                gourds.Add((gourd.transform.position, prop.saveablePropName.ToString()));
            }
            if (gourds.Count == 0)
            {
                Plugin.Log.LogInfo($"{Tag} Big Reset: no puzzle still holds its gourd.");
                return;
            }

            var left = Walkers().ConvertAll(w => w.Position);
            var done = new HashSet<string>();
            while (left.Count > 0)
            {
                var group = new List<Vector3> { left[0] };
                left.RemoveAt(0);
                for (var grew = true; grew;)
                {
                    grew = false;
                    for (var i = left.Count - 1; i >= 0; i--)
                    {
                        if (!group.Exists(p => Vector3.Distance(p, left[i]) < ResetGroup))
                            continue;
                        group.Add(left[i]);
                        left.RemoveAt(i);
                        grew = true;
                    }
                }

                var nearest = gourds[0];
                var best = float.MaxValue;
                foreach (var gourd in gourds)
                {
                    foreach (var player in group)
                    {
                        var distance = Vector3.Distance(player, gourd.at);
                        if (distance < best)
                        {
                            best = distance;
                            nearest = gourd;
                        }
                    }
                }
                if (!done.Add(nearest.name))
                    continue;

                CycleDoor(nearest.name, nearest.at, best, group.Count);
            }
        }
    }
}
