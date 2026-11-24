using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BigWalkArchipelago.Core;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Writes every pack, belt and carton to BepInEx/pickup-census.tsv (Ctrl+Keypad 1).
    internal static class DebugPickupCensus
    {
        private const string Tag = "[" + nameof(DebugPickupCensus) + "]";

        private static readonly string[] PackPrefabs = { "BackpackProp", "HolsterProp", "GourdCartonProp", "FlareGunProp" };

        // The game's class list names no firework launcher, so object names are all there is.
        private static readonly string[] FireworkHints = { "firework", "rocket", "launcher", "pyro", "flare station", "flarestation" };

        internal static void Dump()
        {
            try
            {
                var versions = VersionsByObject();
                var text = new StringBuilder();
                text.AppendLine($"# pickup census, map version now: {PlayerCountSwapper.playerCount}");
                text.AppendLine("kind\tname\tguid\tx\ty\tz\tactive\tversions\tnearest puzzle\tdistance\tpath");

                var packs = 0;
                var colours = new StringBuilder();
                colours.AppendLine("# the colours each pack carries: every PropertyBlockHelper colour (path:property=#RRGGBB), then each renderer's material");
                foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (prop == null || ReceivedItemSpawner.IsCosmeticClone(prop))
                        continue;

                    var name = prop.gameObject.name;
                    var prefab = Array.Find(PackPrefabs, p => name.StartsWith(p, StringComparison.Ordinal));
                    if (prefab == null || name.Contains("(AP", StringComparison.Ordinal))
                        continue;

                    packs++;
                    text.AppendLine(Row(prefab, prop.transform, prop.savablePropGuid, versions));
                    colours.AppendLine($"{prefab}	{prop.savablePropGuid}	{ColoursOf(prop.gameObject)}");
                }

                var fireworks = 0;
                var seen = new HashSet<int>();
                foreach (var launch in UnityEngine.Object.FindObjectsByType<PeckEffectLaunchProp>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (launch == null || !seen.Add(launch.gameObject.GetInstanceID()))
                        continue;
                    fireworks++;
                    var state = launch.trackedStateSystem;
                    text.AppendLine(Row("LaunchProp", launch.transform,
                        state != null ? $"state {state.gameObject.name} ({state.savableSystem})={state.currentPeckContext.state} saveIdentity={(state.saveIdentity != null)}" : "no state", versions));
                }

                foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (transform == null || !seen.Add(transform.gameObject.GetInstanceID()))
                        continue;

                    var lower = transform.name.ToLowerInvariant();
                    if (Array.Exists(FireworkHints, hint => lower.Contains(hint)))
                    {
                        fireworks++;
                        text.AppendLine(Row("NameHint", transform, Components(transform.gameObject), versions));
                    }
                }

                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(BepInEx.Paths.BepInExRootPath, "pickup-colours.tsv"), colours.ToString());

                var path = System.IO.Path.Combine(BepInEx.Paths.BepInExRootPath, "pickup-census.tsv");
                System.IO.File.WriteAllText(path, text.ToString());
                Plugin.Log.LogInfo($"{Tag} {packs} pack(s) and {fireworks} firework candidate(s) written to {path}.");
                Core.Net.ApNotices.Post($"Census: {packs} packs, {fireworks} firework candidates");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Census failed: {ex}");
            }
        }

        // Every island object a player can pick up, to BepInEx/object-census.tsv (Ctrl+Shift+5).
        internal static void DumpObjects()
        {
            try
            {
                var versions = VersionsByObject();
                var text = new StringBuilder();
                text.AppendLine($"# object census, map version now: {PlayerCountSwapper.playerCount}");
                text.AppendLine("kind	name	guid	x	y	z	active	versions	nearest puzzle	distance	path");
                var count = 0;
                foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (prop == null || ReceivedItemSpawner.IsCosmeticClone(prop) || prop.gameObject.name.Contains("(AP", StringComparison.Ordinal))
                        continue;
                    var name = prop.gameObject.name;
                    var cut = name.IndexOf(" (", StringComparison.Ordinal);
                    var kind = cut > 0 ? name.Substring(0, cut) : name;
                    text.AppendLine(Row(kind, prop.transform, prop.savablePropGuid, versions));
                    count++;
                }

                // Every lamp pedestal, full or empty: an empty PropHome has no prop row.
                var pedestals = 0;
                foreach (var home in UnityEngine.Object.FindObjectsByType<PropHome>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (home == null)
                        continue;
                    var homePath = PathOf(home.transform);
                    if (homePath.IndexOf("pedestal", StringComparison.OrdinalIgnoreCase) < 0
                        && homePath.IndexOf("pedistal", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    pedestals++;
                    var pinned = home.pinnedProp != null ? home.pinnedProp.gameObject.name : "EMPTY";
                    text.AppendLine(Row("Pedestal", home.transform, $"holds {pinned}", versions));
                }
                Plugin.Log.LogInfo($"{Tag} {pedestals} lamp pedestal(s) listed (kind Pedestal).");

                // The lamp nearest the player: its body block and light, against the seed colours.
                var local = Mirror.NetworkClient.localPlayer;
                Vector3? here = local != null ? local.transform.position : Camera.main != null ? Camera.main.transform.position : null;
                Prop nearest = null;
                var best = float.MaxValue;
                foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsSortMode.None))
                {
                    if (prop == null || here == null || !(prop.gameObject.name.StartsWith("BuoyLight", StringComparison.Ordinal)
                                                         || prop.gameObject.name.StartsWith("BuoyProp", StringComparison.Ordinal)
                                                         || prop.gameObject.name.StartsWith("BuoyRedProp", StringComparison.Ordinal)))
                        continue;
                    var d = (prop.transform.position - here.Value).sqrMagnitude;
                    if (d < best)
                    {
                        best = d;
                        nearest = prop;
                    }
                }
                if (nearest != null)
                {
                    var expected = Palette.For(string.IsNullOrEmpty(nearest.savablePropGuid) ? nearest.gameObject.name : nearest.savablePropGuid);
                    Plugin.Log.LogInfo($"{Tag} nearest lamp {nearest.gameObject.name} {nearest.savablePropGuid} at {Mathf.Sqrt(best):0.0} m; "
                        + $"seed wants body {expected.body} halo {expected.halo} ({expected.name})");
                    foreach (var renderer in nearest.GetComponentsInChildren<Renderer>(true))
                    {
                        var block = new MaterialPropertyBlock();
                        renderer.GetPropertyBlock(block);
                        Plugin.Log.LogInfo($"{Tag}   renderer {renderer.name}: _TintColor={block.GetColor("_TintColor")} _MainColor={block.GetColor("_MainColor")} "
                            + $"mat={(renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "-")} active={renderer.gameObject.activeInHierarchy}");
                    }
                    foreach (var light in nearest.GetComponentsInChildren<Light>(true))
                        Plugin.Log.LogInfo($"{Tag}   light {light.name}: {light.color} enabled={light.enabled}");
                    LogBodyShader(nearest.gameObject);
                }
                else
                {
                    Plugin.Log.LogInfo($"{Tag} nearest lamp: none found (player position known: {here != null}).");
                }

                // A BuoyProp too, whose body the tint does reach, to compare the materials.
                foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsSortMode.None))
                {
                    if (prop != null && prop.gameObject.name == "BuoyProp")
                    {
                        Plugin.Log.LogInfo($"{Tag} compare with BuoyProp {prop.savablePropGuid}:");
                        LogBodyShader(prop.gameObject);
                        break;
                    }
                }

                // The parts of one lamp of each model, to compare with what ColourPainter paints.
                foreach (var model in new[] { "BuoyLight", "BuoyProp" })
                {
                    foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        if (prop == null || prop.gameObject.name != model)
                            continue;
                        foreach (var t in prop.GetComponentsInChildren<Transform>(true))
                        {
                            var light = t.GetComponent<Light>();
                            Plugin.Log.LogInfo($"{Tag} lamp part {model} '{PathOf(t)}' [{Components(t.gameObject)}]"
                                + (light != null ? $" light={light.color}" : ""));
                            foreach (var helper in t.GetComponents<PropertyBlockHelper>())
                            {
                                if (helper?.colorSettings == null)
                                    continue;
                                foreach (var setting in helper.colorSettings)
                                    Plugin.Log.LogInfo($"{Tag}   helper colour {setting.propertyName} = {setting.color}");
                            }
                        }
                        break;
                    }
                }

                // What makes a clock's hands turn.
                foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (prop == null || !prop.gameObject.name.StartsWith("ClockProp", StringComparison.Ordinal))
                        continue;
                    foreach (var t in prop.GetComponentsInChildren<Transform>(true))
                        Plugin.Log.LogInfo($"{Tag} clock part '{PathOf(t)}' rot={t.localEulerAngles} [{Components(t.gameObject)}]");
                    break;
                }

                var path = System.IO.Path.Combine(BepInEx.Paths.BepInExRootPath, "object-census.tsv");
                System.IO.File.WriteAllText(path, text.ToString());
                Plugin.Log.LogInfo($"{Tag} {count} object(s) written to {path}.");
                Core.Net.ApNotices.Post($"Census: {count} objects");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Object census failed: {ex}");
            }
        }

        // Every colour and float property of a lamp body's shader, from the material and the block.
        private static void LogBodyShader(GameObject lamp)
        {
            foreach (var renderer in lamp.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.name != "buoy" || renderer.sharedMaterial == null)
                    continue;
                var material = renderer.sharedMaterial;
                var shader = material.shader;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Plugin.Log.LogInfo($"{Tag}   body material {material.name}, shader {shader.name}, {shader.GetPropertyCount()} properties");
                for (var i = 0; i < shader.GetPropertyCount(); i++)
                {
                    var name = shader.GetPropertyName(i);
                    var type = shader.GetPropertyType(i);
                    if (type == UnityEngine.Rendering.ShaderPropertyType.Color || type == UnityEngine.Rendering.ShaderPropertyType.Vector)
                        Plugin.Log.LogInfo($"{Tag}     {name} ({type}) material={material.GetColor(name)} block={block.GetColor(name)}");
                    else if (type == UnityEngine.Rendering.ShaderPropertyType.Float || type == UnityEngine.Rendering.ShaderPropertyType.Range)
                        Plugin.Log.LogInfo($"{Tag}     {name} ({type}) material={material.GetFloat(name)} block={block.GetFloat(name)}");
                }
            }
        }

        private static int _nextSpot;

        // Ctrl+F3: teleports the local player beside the next pack or firework launcher.
        internal static void TeleportNext()
        {
            var player = Core.LocalPlayer.Find();
            if (player == null)
                return;

            var spots = new List<KeyValuePair<string, Vector3>>();
            foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (prop == null || ReceivedItemSpawner.IsCosmeticClone(prop))
                    continue;
                var name = prop.gameObject.name;
                if (name.Contains("(AP", StringComparison.Ordinal)
                    || !Array.Exists(PackPrefabs, p => name.StartsWith(p, StringComparison.Ordinal)))
                    continue;
                spots.Add(new KeyValuePair<string, Vector3>($"{name} {prop.savablePropGuid}", prop.transform.position));
            }

            foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (transform != null && transform.name.StartsWith("FireworkLauncher", StringComparison.Ordinal))
                    spots.Add(new KeyValuePair<string, Vector3>(
                        $"FireworkLauncher in {transform.parent?.name}/{transform.parent?.parent?.name}", transform.position));
            }

            if (spots.Count == 0)
                return;

            spots.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            var spot = spots[_nextSpot % spots.Count];
            _nextSpot++;
            var landing = Traps.Ground(spot.Value + Vector3.back * 2f);
            BigWalkArchipelago.Core.WorldButtons.TeleportTo(player, landing, Quaternion.identity);
            Plugin.Log.LogInfo($"{Tag} Teleported to {spot.Key} ({_nextSpot}/{spots.Count}) at {spot.Value.ToString("F1")}.");
            Core.Net.ApNotices.Post($"Teleport {_nextSpot}/{spots.Count}: {spot.Key.Split(' ')[0]}");
        }

        // An object under no PlayerCountSwapper is in every version.
        private static Dictionary<int, string> VersionsByObject()
        {
            var result = new Dictionary<int, string>();
            foreach (var swapper in UnityEngine.Object.FindObjectsByType<PlayerCountSwapper>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (swapper == null)
                    continue;
                Mark(result, swapper.target2, "2");
                Mark(result, swapper.target3, "3");
                Mark(result, swapper.target4, "4");
            }

            return result;
        }

        private static void Mark(Dictionary<int, string> result, GameObject target, string version)
        {
            if (target == null)
                return;

            foreach (var transform in target.GetComponentsInChildren<Transform>(true))
            {
                var id = transform.gameObject.GetInstanceID();
                result[id] = result.TryGetValue(id, out var before) && !before.Contains(version) ? before + "," + version : version;
            }
        }

        private static string VersionsOf(Transform transform, Dictionary<int, string> versions)
        {
            return versions.TryGetValue(transform.gameObject.GetInstanceID(), out var found) ? found : "2,3,4";
        }

        private static string Row(string kind, Transform transform, string detail, Dictionary<int, string> versions)
        {
            var p = transform.position;
            var nearest = NearestPuzzle(p, out var distance);
            return string.Join("\t",
                kind,
                transform.name,
                detail ?? string.Empty,
                p.x.ToString("F1", CultureInfo.InvariantCulture),
                p.y.ToString("F1", CultureInfo.InvariantCulture),
                p.z.ToString("F1", CultureInfo.InvariantCulture),
                transform.gameObject.activeInHierarchy ? "active" : "inactive",
                VersionsOf(transform, versions),
                nearest ?? "-",
                distance.ToString("F0", CultureInfo.InvariantCulture),
                PathOf(transform));
        }

        internal static string ColoursOf(GameObject root)
        {
            var parts = new List<string>();
            foreach (var helper in root.GetComponentsInChildren<PropertyBlockHelper>(true))
            {
                if (helper == null || helper.colorSettings == null)
                    continue;

                foreach (var setting in helper.colorSettings)
                    parts.Add($"{helper.gameObject.name}:{setting.propertyName}=#{ColorUtility.ToHtmlStringRGB(setting.color)}");
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                var material = renderer.sharedMaterial;
                parts.Add($"{renderer.gameObject.name}[{(renderer.gameObject.activeSelf ? "on" : "off")}]:{(material != null ? material.name : "-")}");
            }

            return string.Join("  ", parts);
        }

        private static string Components(GameObject gameObject)
        {
            var names = new List<string>();
            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (component != null)
                    names.Add(component.GetIl2CppType().Name);
            }

            return string.Join(",", names);
        }

        private static string PathOf(Transform transform)
        {
            var parts = new List<string>();
            for (var t = transform; t != null; t = t.parent)
                parts.Insert(0, t.name);
            return string.Join("/", parts);
        }

        private static string NearestPuzzle(Vector3 position, out float nearestDistance)
        {
            string nearest = null;
            nearestDistance = float.MaxValue;
            foreach (var gourd in UnityEngine.Object.FindObjectsByType<RewardGourd>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var prop = gourd != null ? gourd.prop : null;
                if (prop == null || !GourdRegistry.TryGetLocationId(prop.saveablePropName, out _))
                    continue;

                var distance = Vector3.Distance(position, prop.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = prop.saveablePropName.ToString();
                }
            }

            return nearest;
        }
    }
}
