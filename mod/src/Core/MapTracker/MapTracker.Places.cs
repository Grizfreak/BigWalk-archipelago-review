using System;
using System.Collections.Generic;
using BigWalkArchipelago.Core.Net;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal static partial class MapTracker
    {
        private sealed class Scene
        {
            internal readonly Dictionary<string, Prop> Props = new();
            internal readonly Dictionary<string, Transform> Launchers = new();
            internal readonly Dictionary<string, Transform> Stations = new();
            internal readonly Dictionary<SaveablePropName, Transform> Keys = new();
            internal readonly Dictionary<SaveableHomeName, Transform> Homes = new();
            internal Dictionary<int, Vector3> Monuments;
            internal Transform TowerMark;
            internal Transform GourdMark;
            internal Transform Gauntlet;
        }

        private static Scene _scene;

        private static Scene Island()
        {
            if (_scene != null)
                return _scene;
            var scene = new Scene();
            foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (prop == null || ReceivedItemSpawner.IsCosmeticClone(prop) || prop.gameObject.name.Contains("(AP", StringComparison.Ordinal))
                    continue;
                if (!string.IsNullOrEmpty(prop.savablePropGuid))
                    scene.Props[prop.savablePropGuid] = prop;
            }
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t == null || !t.name.StartsWith("FireworkLauncher", StringComparison.Ordinal))
                    continue;
                var landmark = PackChecks.LandmarkOf(t);
                if (!string.IsNullOrEmpty(landmark))
                    scene.Launchers[landmark] = t;
            }
            foreach (var station in UnityEngine.Object.FindObjectsByType<BroadcastStation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (station != null && station.radioPeckSystem != null)
                    scene.Stations[station.radioPeckSystem.savableSystem.ToString()] = station.transform;
            }
            foreach (var blank in UnityEngine.Object.FindObjectsByType<KeyBlank>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (blank != null && blank.prop != null && !ReceivedItemSpawner.IsCosmeticClone(blank.prop))
                    scene.Keys[blank.prop.saveablePropName] = blank.prop.transform;
            }
            foreach (var home in UnityEngine.Object.FindObjectsByType<PropHome>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (home != null)
                    scene.Homes[home.saveableHomeName] = home.transform;
            }
            scene.Monuments = MonumentPlaces();
            var flag = _map.flagPrefab != null ? _map.flagPrefab.transform : null;
            scene.TowerMark = flag != null ? FindChild(flag, "tower_flag") : null;
            scene.GourdMark = flag != null ? FindChild(flag, "gourd_stashed") : null;
            scene.Gauntlet = ActiveNamed("SilentGauntlet");
            return _scene = scene;
        }

        private static void Build(string key)
        {
            var scene = Island();
            var colon = key.IndexOf(':');
            var kind = key.Substring(0, colon);
            var rest = key.Substring(colon + 1);
            switch (kind)
            {
                case "prop":
                    if (scene.Props.TryGetValue(rest, out var prop))
                        Place(key, prop.transform.position, prop.transform, false);
                    break;
                case "switch":
                {
                    var state = FindByPath(rest);
                    if (state != null && state.parent != null)
                        Place(key, state.parent.position, state.parent, false);
                    break;
                }
                case "firework":
                    if (scene.Launchers.TryGetValue(rest, out var launcher))
                    {
                        var model = launcher.Find("fireworkLauncherModel");
                        Place(key, launcher.position, model != null ? model : launcher, false);
                    }
                    break;
                case "radio":
                    // The station itself, not the radio that tunes it.
                    if (scene.Stations.TryGetValue(rest, out var station))
                        Place(key, station.position, station, false);
                    break;
                case "tower":
                case "monument":
                {
                    var tower = MonumentOrder.TowerIndexOf(rest);
                    if (tower < 0)
                        break;
                    var hasMonument = scene.Monuments.TryGetValue(tower, out var monument);
                    if (kind == "tower" && scene.TowerMark != null)
                    {
                        var centre = MonumentOrder.CentreOf(tower);
                        if (centre != Vector3.zero)
                            Place(key, centre, scene.TowerMark, true);
                        else if (hasMonument)
                            Place(key, monument, scene.TowerMark, true);
                    }
                    else if (kind == "monument" && hasMonument && scene.GourdMark != null)
                        Place(key, monument, scene.GourdMark, true);
                    break;
                }
                case "monumentkey":
                {
                    var tower = MonumentOrder.TowerIndexOf(rest);
                    if (tower >= 0 && scene.Monuments.TryGetValue(tower, out var monument)
                        && Enum.TryParse<SaveablePropName>(rest, out var keyName) && scene.Keys.TryGetValue(keyName, out var bigKey))
                        Place(key, monument, bigKey, false);
                    break;
                }
                case "gauntlet":
                    if (scene.Gauntlet != null && scene.GourdMark != null)
                        Place(key, scene.Gauntlet.position, scene.GourdMark, true);
                    break;
                case "cut":
                {
                    var last = rest.LastIndexOf(':');
                    if (last < 0 || !Enum.TryParse<SaveablePropName>(rest.Substring(0, last), out var name)
                        || !int.TryParse(rest.Substring(last + 1), out var index) || !scene.Keys.TryGetValue(name, out var bigKey))
                        break;
                    var trail = TrailOf(name);
                    if (trail?.stations == null)
                        break;
                    foreach (var cutStation in trail.stations)
                    {
                        if (cutStation != null && cutStation.stationIndex == index)
                            Place(key, cutStation.transform.position, bigKey, false);
                    }
                    break;
                }
                case "deposit":
                    if (Enum.TryParse<SaveablePropName>(rest, out var deposited) && scene.Keys.TryGetValue(deposited, out var depositKey)
                        && GourdRegistry.TryGetBigKeyHome(deposited, out var plinth) && scene.Homes.TryGetValue(plinth, out var plinthPlace))
                        Place(key, plinthPlace.position, depositKey, false);
                    break;
                case "objective":
                {
                    var place = ObjectivePlace(rest);
                    if (place == null || scene.TowerMark == null)
                        break;
                    var at = MapPoint(place.Value);
                    Surface(ref at);
                    var marker = Make(at, scene.TowerMark, true, ObjectiveStick, ObjectiveColour);
                    if (marker != null)
                        Markers[key] = marker;
                    break;
                }
            }
        }

        // By trail name, not nearest trail: a carried key would take another key's trail.
        private static readonly Dictionary<SaveablePropName, string> TrailNames = new()
        {
            { SaveablePropName.bigKeyIntro, "CuttingTrailBoardwalk" },
            { SaveablePropName.bigKeyRedZone, "CuttingTrailRedTower" },
            { SaveablePropName.bigKeyGreenZone, "CuttingTrailGreenTower" },
            { SaveablePropName.bigKeyBlueZone, "CuttingTrailBlueTower" },
            { SaveablePropName.bigKeyYellowZone, "CuttingTrailYellowTower" },
        };

        internal static UnlockTrail TrailOf(SaveablePropName key)
        {
            if (!TrailNames.TryGetValue(key, out var trailName))
                return null;

            foreach (var trail in UnityEngine.Object.FindObjectsByType<UnlockTrail>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (trail != null && trail.gameObject.name == trailName)
                    return trail;
            }

            return null;
        }

        private static readonly Dictionary<string, Marker> Objectives = new();
        private const float ObjectiveStick = 0.6f;
        private const float OpenStick = 1.6f;
        private static readonly Color ObjectiveColour = new(1.2f, 1.2f, 1.2f, 1f);

        private static Vector3? ObjectivePlace(string key)
        {
            switch (key)
            {
                case "big_climb":
                    return ObjectiveSpots.ClimbCentre();
                case "big_party":
                    foreach (var home in UnityEngine.Object.FindObjectsByType<PropHome>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        if (home != null && home.name == "FmRadioDockPropHome")
                            return home.transform.position;
                    }
                    return null;
                case "big_doof":
                    return ObjectiveSpots.DoofCentre();
                case "big_makeover":
                    return ObjectiveSpots.SalonCentre();
                case "big_wall":
                    return ActiveNamed("GoodbyeChapel")?.position;
                case "big_game":
                    return ActiveNamed("GoodbyePrinter")?.position;
                case "big_goodbye":
                {
                    var gauntlet = ActiveNamed("SilentGauntlet");
                    var last = gauntlet != null ? FindChild(gauntlet, "Level6") : null;
                    return (last ?? gauntlet)?.position;
                }
                default:
                    return null;
            }
        }

        // The active object of this name (the one of the map version loaded), or any.
        private static Transform ActiveNamed(string prefix)
        {
            Transform any = null;
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t == null || !t.name.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                if (t.gameObject.activeInHierarchy)
                    return t;
                any ??= t;
            }
            return any;
        }

        private static Dictionary<int, Vector3> MonumentPlaces()
        {
            var sums = new Dictionary<int, (Vector3 sum, int count)>();
            foreach (var home in UnityEngine.Object.FindObjectsByType<PropHome>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (home == null || !ReceivedItemSpawner.IsMonumentHome(home))
                    continue;
                var prefix = MonumentOrder.PrefixOfHome(home);
                for (var tower = 0; tower < MonumentOrder.TowerCount; tower++)
                {
                    if (MonumentOrder.PrefixOf(tower) != prefix)
                        continue;
                    var entry = sums.TryGetValue(tower, out var known) ? known : (Vector3.zero, 0);
                    sums[tower] = (entry.Item1 + home.transform.position, entry.Item2 + 1);
                    break;
                }
            }
            var places = new Dictionary<int, Vector3>();
            foreach (var pair in sums)
                places[pair.Key] = pair.Value.sum / pair.Value.count;
            return places;
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && child.name == name)
                    return child;
            }
            return null;
        }

        private static Transform FindByPath(string path)
        {
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t != null && t.name == path.Substring(path.LastIndexOf('/') + 1) && PathOf(t) == path)
                    return t;
            }
            return null;
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
