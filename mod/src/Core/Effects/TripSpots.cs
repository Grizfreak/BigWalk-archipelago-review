using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    // Big Trip only sends players to places already reached, so it never skips a lock.
    internal static class TripSpots
    {
        internal struct Spot
        {
            internal string Label;
            internal Vector3 Point;
        }

        // How far from a radio station (a launcher, a puzzle's gourd) a player lands.
        private const float StationDistance = 3.5f;

        // A puzzle with a screen mask this close to its gourd's home is a mask puzzle.
        private const float MaskPuzzleRadius = 60f;

        internal static List<Spot> Candidates()
        {
            var spots = new List<Spot>();
            try
            {
                spots.AddRange(TeleportButtons.TripLandings()
                    .Select(landing => new Spot { Label = landing.Key, Point = landing.Value }));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(TripSpots)}] Tower landings: {ex.Message}");
            }

            try
            {
                foreach (var station in UnityEngine.Object.FindObjectsByType<BroadcastStation>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (!RadioStations.TryGetSystem(station, out var system))
                        continue;
                    if (SaveManager.GetIntValue(system.ToString(), 0, false) == 0)
                        continue;

                    var landing = AroundStation(station.transform.position);
                    if (landing != null)
                        spots.Add(new Spot { Label = $"radio {system}", Point = landing.Value });
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(TripSpots)}] Radio stations: {ex.Message}");
            }

            try
            {
                AddPuzzles(spots);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(TripSpots)}] Puzzles: {ex.Message}");
            }

            try
            {
                foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (transform == null || !transform.name.StartsWith("FireworkLauncher", StringComparison.Ordinal))
                        continue;
                    var key = PackChecks.LandmarkOf(transform);
                    if (key == null || !PackChecks.LauncherFired(key))
                        continue;
                    var landing = AroundStation(transform.position);
                    if (landing != null)
                        spots.Add(new Spot { Label = $"fireworks {key}", Point = landing.Value });
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(TripSpots)}] Fireworks: {ex.Message}");
            }

            try
            {
                foreach (var pair in GadgetItemSpawner.VanillaPacksByGuid())
                {
                    if (!CheckTracker.IsReported(ApLocationIds.PickupPrefix + pair.Key))
                        continue;
                    var landing = AroundStation(pair.Value.transform.position);
                    if (landing != null)
                        spots.Add(new Spot { Label = $"pack {pair.Value.gameObject.name} {pair.Key}", Point = landing.Value });
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(TripSpots)}] Packs: {ex.Message}");
            }

            spots.RemoveAll(spot => Gauntlet.Contains(spot.Point));
            return spots;
        }

        // Solved puzzles, beside their gourd's home; never a mask puzzle.
        private static void AddPuzzles(List<Spot> spots)
        {
            var masks = new List<Vector3>();
            foreach (var mask in UnityEngine.Object.FindObjectsByType<PeckEffectMask>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mask != null)
                    masks.Add(mask.transform.position);
            }

            foreach (var gourd in UnityEngine.Object.FindObjectsByType<RewardGourd>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var prop = gourd != null ? gourd.prop : null;
                if (prop == null || !GourdRegistry.TryGetLocationId(prop.saveablePropName, out var location)
                    || !CheckTracker.IsReported(location))
                    continue;

                var home = GourdRegistry.TryGetHomeFor(prop.saveablePropName);
                if (home == null)
                    continue;

                var at = home.transform.position;
                if (masks.Exists(mask => Vector3.Distance(mask, at) < MaskPuzzleRadius))
                    continue;

                var landing = AroundStation(at);
                if (landing != null)
                    spots.Add(new Spot { Label = $"puzzle {prop.saveablePropName}", Point = landing.Value });
            }
        }

        // A patch of floor beside the station, near its own height, with room to stand.
        private static Vector3? AroundStation(Vector3 station)
        {
            return Traps.SafeLanding(station, StationDistance, 0f, 2.5f, null);
        }
    }
}
