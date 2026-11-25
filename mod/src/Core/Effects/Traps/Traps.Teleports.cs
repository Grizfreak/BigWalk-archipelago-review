using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    internal static partial class Traps
    {
        private static void FireTrip(string key)
        {
            var walkers = Walkers();
            var spots = TripSpots.Candidates();
            if (spots.Count == 0)
            {
                Plugin.Log.LogInfo($"{Tag} Big Trip: no place to send anyone yet.");
                Net.ApNotices.Post("Big Trip: nowhere to go yet", warning: true);
                return;
            }

            // A different place for each player, none of them where that player already is.
            var shuffled = spots.OrderBy(_ => UnityEngine.Random.value).ToList();
            var used = new HashSet<int>();
            var sentAny = false;
            foreach (var walker in walkers)
            {
                if (IsSpared(walker, out var why))
                {
                    Plugin.Log.LogInfo($"{Tag} Big Trip spares {walker.Player.gameObject.name}: {why}.");
                    continue;
                }

                var pick = -1;
                for (var i = 0; i < shuffled.Count && pick < 0; i++)
                {
                    if (!used.Contains(i) && Vector3.Distance(shuffled[i].Point, walker.Position) > 40f)
                        pick = i;
                }

                for (var i = 0; i < shuffled.Count && pick < 0; i++)
                {
                    if (!used.Contains(i))
                        pick = i;
                }

                if (pick < 0)
                    pick = UnityEngine.Random.Range(0, shuffled.Count);

                used.Add(pick);
                Plugin.Log.LogInfo($"{Tag} Big Trip sends {walker.Player.gameObject.name} to {shuffled[pick].Label}.");
                Send(walker, key, shuffled[pick].Point, walker.Yaw);
                sentAny = true;
            }

            if (!sentAny)
                Net.ApNotices.Post("Big Trip: nobody free to move", warning: true);
        }

        private static void FireMeeting()
        {
            var walkers = Walkers();
            var free = walkers.Where(w => !IsSpared(w, out _)).ToList();
            if (walkers.Count < 2 || free.Count < 2)
            {
                Plugin.Log.LogInfo($"{Tag} Big Meeting: {walkers.Count} player(s), {free.Count} free to move; a Big Trip instead.");
                FireTrip(TrapEffects.Trip);
                return;
            }

            var target = free[UnityEngine.Random.Range(0, free.Count)];
            var movers = free.Where(w => w != target).ToList();
            Plugin.Log.LogInfo($"{Tag} Big Meeting around {target.Player.gameObject.name}, {movers.Count} player(s) gathered.");

            var taken = new List<Vector3>();
            for (var i = 0; i < movers.Count; i++)
            {
                var angle = target.Yaw + 360f * (i + 1) / (movers.Count + 1);

                // Nowhere to stand beside them: on them, which the game sorts out by pushing apart.
                var landing = SafeLanding(target.Position, MeetingRadius, angle, 2f, taken)
                              ?? target.Position + Vector3.up * 0.8f;

                // Facing the player they were gathered around.
                var look = target.Position - landing;
                var yaw = new Vector2(look.x, look.z).sqrMagnitude > 0.01f
                    ? Quaternion.LookRotation(new Vector3(look.x, 0f, look.z)).eulerAngles.y
                    : target.Yaw;
                Send(movers[i], TrapEffects.Meeting, landing, yaw);
            }
        }

        // A standable spot in sight of `center`, its floor within `maxRise`; null if none.
        internal static Vector3? SafeLanding(Vector3 center, float radius, float angle, float maxRise, List<Vector3> taken)
        {
            var eye = center + Vector3.up * 1.2f;
            foreach (var distance in new[] { radius, radius * 1.6f, radius * 0.6f })
            {
                for (var i = 0; i < 12; i++)
                {
                    var point = center + Quaternion.Euler(0f, angle + 30f * i, 0f) * Vector3.forward * distance;
                    if (Physics.Linecast(eye, point + Vector3.up * 1.2f, out _, ~0, QueryTriggerInteraction.Ignore))
                        continue;
                    if (!Physics.Raycast(point + Vector3.up * 2.5f, Vector3.down, out var hit, 5f, ~0, QueryTriggerInteraction.Ignore))
                        continue;
                    if (Mathf.Abs(hit.point.y - center.y) > maxRise)
                        continue;

                    var feet = hit.point;
                    if (Physics.CheckCapsule(feet + Vector3.up * 0.45f, feet + Vector3.up * 1.6f, 0.35f, ~0, QueryTriggerInteraction.Ignore))
                        continue;
                    if (taken != null && taken.Exists(other => Vector3.Distance(other, feet) < 1f))
                        continue;

                    taken?.Add(feet);
                    return feet + Vector3.up * 0.8f;
                }
            }

            return null;
        }
    }
}
