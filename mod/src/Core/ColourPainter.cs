using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Paints buoys, guns, gadgets, lasers and fireworks in the seed's colours, on each machine.
    internal static class ColourPainter
    {
        private const string Tag = "[" + nameof(ColourPainter) + "]";
        internal const string RainbowMarker = " rainbow";

        private static readonly string[] BuoyNames = { "BuoyLight", "BuoyProp", "BuoyRedProp" };
        private static readonly string[] GunNames = { "FlareGunPropBlue", "FlareGunPropGreen", "FlareGunPropYellow", "FlareGunProp" };

        // The gadgets painted (a laser is painted on its own first, PaintIfLaser).
        private static readonly string[][] GadgetKinds =
        {
            new[] { "BinocularsProp" }, new[] { "ClockProp" }, new[] { "CompassProp" },
            new[] { "CowBellLargeProp", "CowBellMediumProp", "CowBellSmallProp" }, new[] { "FmRadioProp" },
            new[] { "LaserProp" }, new[] { "MegaphoneProp" }, new[] { "TorchProp" }, new[] { "XrayGogglesProp" },
            new[] { "WalkieTalkieProp" },
        };
        private const string LauncherName = "FireworkLauncher";

        private static bool _dirty = true;
        private static bool _wasReady;
        private static readonly List<ParticleSystem> RainbowShots = new();
        private static Color _rainbowShown;

        internal static void Repaint() => _dirty = true;

        // Switching a lamp on resets its light and halo, so they are put back twice a second.
        private static readonly List<(Light light, Renderer halo, Color colour, Renderer body, Color bodyColour)> Lamps = new();
        private static float _nextLampCheck;

        private static void KeepTheLamps()
        {
            if (Lamps.Count == 0 || Time.unscaledTime < _nextLampCheck)
                return;

            _nextLampCheck = Time.unscaledTime + 0.5f;
            var fixedCount = 0;
            for (var i = Lamps.Count - 1; i >= 0; i--)
            {
                var (light, halo, colour, body, bodyColour) = Lamps[i];
                if (light == null && halo == null && body == null)
                {
                    Lamps.RemoveAt(i);
                    continue;
                }

                if (light != null && light.enabled && light.color != colour)
                {
                    light.color = colour;
                    fixedCount++;
                }

                if (halo != null)
                {
                    var block = new MaterialPropertyBlock();
                    halo.GetPropertyBlock(block);
                    // An off lamp hides its halo; a block the game rewrote reads black.
                    if (halo.gameObject.activeInHierarchy && block.GetColor("_MainColor") != colour)
                    {
                        block.SetColor("_MainColor", colour);
                        halo.SetPropertyBlock(block);
                        fixedCount++;
                    }
                }

                // The buoy's PropertyBlockHelper rewrites its block untinted on a switch.
                if (body != null)
                {
                    var block = new MaterialPropertyBlock();
                    body.GetPropertyBlock(block);
                    if (block.GetColor("_TintColor") != bodyColour)
                    {
                        block.SetColor("_TintColor", bodyColour);
                        body.SetPropertyBlock(block);
                        fixedCount++;
                    }
                }
            }

            if (fixedCount > 0 && !_lampFixLogged)
            {
                _lampFixLogged = true;
                Plugin.Log.LogInfo($"{Tag} A lamp's light or halo had been changed by the game: put back to the seed's colour.");
            }
        }

        private static bool _lampFixLogged;

        // Every frame: the world once ready or recoloured, and the rainbow guns' shots.
        internal static void Tick()
        {
            var ready = WorldManager.isReadyForEffects;
            if (ready && !_wasReady)
                _dirty = true;
            if (!ready)
            {
                RainbowShots.Clear();
                Lamps.Clear();
            }
            _wasReady = ready;

            if (ready && _dirty)
            {
                _dirty = false;
                PaintWorld();
            }

            TickRainbow();
            if (ready)
                KeepTheLamps();
        }

        private static void PaintWorld()
        {
            int buoys = 0, guns = 0, gadgets = 0;
            foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (prop == null)
                    continue;
                try
                {
                    var key = KeyOf(prop);
                    if (PaintIfBuoy(prop.gameObject, key))
                        buoys++;
                    else if (PaintIfGun(prop.gameObject))
                        guns++;
                    else if (PaintIfLaser(prop.gameObject, key))
                        gadgets++;
                    else if (PaintIfGadget(prop.gameObject, key))
                        gadgets++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} Could not paint {prop.gameObject.name}: {ex.Message}");
                }
            }

            var fireworks = PaintFireworks();

            if (Palette.Active || RainbowShots.Count > 0)
                Plugin.Log.LogInfo($"{Tag} Painted {buoys} buoy(s), {guns} flare gun(s), {gadgets} gadget(s) and {fireworks} firework launcher(s){(Palette.Active ? string.Empty : " (rainbow only)")}.");
        }

        // A clone just built (GadgetItemSpawner), on every machine.
        internal static void PaintClone(GadgetKind kind, GameObject clone, bool rainbow)
        {
            try
            {
                if (rainbow)
                {
                    clone.name += RainbowMarker;
                    PaintIfGun(clone);
                    return;
                }

                var prop = clone.GetComponent<Prop>();
                if (kind == GadgetKind.Lamp)
                    PaintIfBuoy(clone, prop != null ? prop.savablePropGuid : clone.name);
                else if (kind == GadgetKind.Laser)
                    PaintIfLaser(clone, prop != null ? prop.savablePropGuid : clone.name);
                else if (!PaintIfGun(clone))
                    PaintIfGadget(clone, prop != null ? prop.savablePropGuid : clone.name);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Could not paint a {kind} clone: {ex.Message}");
            }
        }

        // A received copy by its netId, the same everywhere: its own colour, not its template's.
        internal static void PaintCopy(GameObject copy, uint netId)
        {
            if (copy == null || netId == 0 || copy.name.Contains(RainbowMarker))
                return;
            try
            {
                var key = CopyKey(netId);
                if (!PaintIfBuoy(copy, key) && !PaintIfLaser(copy, key))
                    PaintIfGadget(copy, key);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Could not paint copy {netId}: {ex.Message}");
            }
        }

        private static string CopyKey(uint netId) => "copy " + netId;

        // A world repaint keeps each copy's own colour (a guest repaints after copies arrive).
        private static string KeyOf(Prop prop)
        {
            if (!ReceivedItemSpawner.IsCosmeticClone(prop))
                return prop.savablePropGuid;
            var identity = prop.GetComponent<Mirror.NetworkIdentity>();
            return identity != null && identity.netId != 0 ? CopyKey(identity.netId) : prop.savablePropGuid;
        }

        private static bool StartsWithAny(string name, string[] names)
        {
            foreach (var candidate in names)
            {
                if (name.StartsWith(candidate, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static bool PaintIfBuoy(GameObject root, string key)
        {
            if (!StartsWithAny(root.name, BuoyNames))
                return false;
            if (!Palette.Active)
                return true;

            var colour = Palette.For(string.IsNullOrEmpty(key) ? root.name : key);
            if (root.name.StartsWith("BuoyLight", StringComparison.Ordinal))
                colour = ("Orange", Color.white, PedestalLampHalo);
            Renderer haloRenderer = null;
            Renderer bodyRenderer = null;
            Light lampLight = null;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name == "buoy")
                {
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    block.SetColor("_TintColor", colour.body);
                    renderer.SetPropertyBlock(block);
                    bodyRenderer = renderer;
                }
                else if (renderer.name == "LobbyFlareMesh" && UnderBuoyLight(renderer.transform))
                {
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    block.SetColor("_MainColor", colour.halo);
                    renderer.SetPropertyBlock(block);
                    haloRenderer = renderer;
                }
            }

            foreach (var light in root.GetComponentsInChildren<Light>(true))
            {
                if (light.name == "bouyLight")
                {
                    light.color = colour.halo;
                    lampLight = light;
                }
            }

            if (lampLight != null || haloRenderer != null || bodyRenderer != null)
            {
                Lamps.RemoveAll(entry => entry.light == lampLight && entry.halo == haloRenderer && entry.body == bodyRenderer);
                Lamps.Add((lampLight, haloRenderer, colour.halo, bodyRenderer, colour.body));
            }

            return true;
        }

        // A pedestal lamp's sphere is orange in the model and takes no tint: its light matches.
        private static readonly Color PedestalLampHalo = new Color(1.00f, 0.40f, 0.00f);

        // The halo sits at bouyLight/LobbyFlare/LobbyFlareMesh, two levels under the light.
        private static bool UnderBuoyLight(Transform transform)
        {
            for (var t = transform.parent; t != null; t = t.parent)
            {
                if (t.name == "bouyLight")
                    return true;
            }
            return false;
        }

        // One colour per object, as the lamps, the same on every machine.
        private static bool PaintIfGadget(GameObject root, string key)
        {
            var row = Array.FindIndex(GadgetKinds, names => StartsWithAny(root.name, names));
            if (row < 0)
                return false;
            if (!Palette.Active)
                return true;

            var colour = Palette.For(string.IsNullOrEmpty(key) ? root.name : key).body;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer != null ? renderer.sharedMaterial : null;
                if (material == null || !material.name.StartsWith("hhVertexColors", StringComparison.Ordinal)
                    || material.name.StartsWith("hhVertexColorsEmissive", StringComparison.Ordinal))
                    continue;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_TintColor", colour);
                renderer.SetPropertyBlock(block);
            }

            // A helper would write its own block over ours (WalkieColour): its tints are set too.
            foreach (var helper in root.GetComponentsInChildren<PropertyBlockHelper>(true))
            {
                var settings = helper != null ? helper.colorSettings : null;
                if (settings == null)
                    continue;
                var changed = false;
                for (var i = 0; i < settings.Length; i++)
                {
                    // An element of an IL2CPP struct array comes back as a copy.
                    var setting = settings[i];
                    if (setting.propertyName != "_TintColor" && setting.propertyName != "_Tint")
                        continue;
                    setting.color = colour;
                    settings[i] = setting;
                    changed = true;
                }
                if (changed)
                    helper.Refresh();
            }

            // A torch's light takes its body's colour, kept like a lamp's.
            if (root.name.StartsWith("TorchProp", StringComparison.Ordinal))
            {
                foreach (var light in root.GetComponentsInChildren<Light>(true))
                {
                    if (light == null)
                        continue;
                    light.color = colour;
                    Lamps.RemoveAll(entry => entry.light == light);
                    Lamps.Add((light, null, colour, null, colour));
                }
            }
            return true;
        }

        private const string LaserName = "LaserProp";
        private static readonly string[] GlowColours = { "_TintColor", "_MainColor", "_BaseColor", "_Color", "_EmissionColor" };

        private static bool PaintIfLaser(GameObject root, string key)
        {
            if (!root.name.StartsWith(LaserName, StringComparison.Ordinal))
                return false;
            if (!Palette.Active)
                return true;

            var colour = Palette.For(string.IsNullOrEmpty(key) ? root.name : key);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer != null ? renderer.sharedMaterial : null;
                if (material == null)
                    continue;

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                if (material.name.StartsWith("hhVertexColors", StringComparison.Ordinal)
                    && !material.name.StartsWith("hhVertexColorsEmissive", StringComparison.Ordinal))
                {
                    block.SetColor("_TintColor", colour.body);
                }
                else if (renderer.name != "laser")
                {
                    // Not the tip: its vertex colours are set, and a tint on top would darken it.
                    foreach (var property in GlowColours)
                    {
                        if (material.HasProperty(property))
                            block.SetColor(property, colour.halo);
                    }
                }
                renderer.SetPropertyBlock(block);

                var line = renderer.TryCast<LineRenderer>();
                if (line != null)
                {
                    line.startColor = colour.halo;
                    line.endColor = colour.halo;
                }

                // The lasers' red is in their meshes' vertex colours, not in a shader property.
                if (renderer.name == "Beam" || renderer.name == "laser")
                    RecolourLaserPart(renderer, colour.halo);
            }

            foreach (var light in root.GetComponentsInChildren<Light>(true))
                light.color = colour.halo;

            foreach (var system in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = system.main;
                main.startColor = new ParticleSystem.MinMaxGradient(colour.halo);
            }

            return true;
        }

        private static bool _laserDescribed;

        // One tube per colour, shared by the lasers that wear it.
        private static readonly Dictionary<uint, Mesh> Tubes = new();
        private const int TubeSides = 6;
        private const int TubeRings = 69;

        // The game's red is about 0.8 of a full channel, so the colour is brought to that level.
        private static Mesh BeamTube(Bounds bounds, Color colour)
        {
            var max = Mathf.Max(colour.r, Mathf.Max(colour.g, colour.b));
            var scaled = max > 0f ? colour * (0.8f / max) : colour;
            var key = (Color32)new Color(scaled.r, scaled.g, scaled.b, 1f);
            var id = (uint)key.r << 16 | (uint)key.g << 8 | key.b;
            if (Tubes.TryGetValue(id, out var known) && known != null)
                return known;

            var radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            var top = bounds.max.y;
            var bottom = bounds.min.y;
            var centre = bounds.center;
            var columns = TubeSides + 1;
            var vertices = new Vector3[TubeRings * columns];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var colours = new Color32[vertices.Length];
            for (var ring = 0; ring < TubeRings; ring++)
            {
                var t = ring / (float)(TubeRings - 1);
                var y = Mathf.Lerp(top, bottom, t);
                for (var side = 0; side < columns; side++)
                {
                    // The game's tube has a corner on +Z: start there.
                    var angle = Mathf.PI / 2f - side * 2f * Mathf.PI / TubeSides;
                    var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    var at = ring * columns + side;
                    vertices[at] = new Vector3(centre.x, y, centre.z) + outward * radius;
                    normals[at] = outward;
                    uvs[at] = new Vector2(side / (float)TubeSides, t);
                    colours[at] = key;
                }
            }

            var triangles = new int[(TubeRings - 1) * TubeSides * 6];
            var index = 0;
            for (var ring = 0; ring < TubeRings - 1; ring++)
            {
                for (var side = 0; side < TubeSides; side++)
                {
                    var a = ring * columns + side;
                    var b = a + 1;
                    var c = a + columns;
                    var d = c + 1;
                    triangles[index++] = a;
                    triangles[index++] = c;
                    triangles[index++] = b;
                    triangles[index++] = b;
                    triangles[index++] = c;
                    triangles[index++] = d;
                }
            }

            var tube = new Mesh { name = "laser (AP colour)" };
            tube.vertices = vertices;
            tube.normals = normals;
            tube.uv = uvs;
            tube.colors32 = colours;
            tube.triangles = triangles;
            tube.bounds = bounds;
            UnityEngine.Object.DontDestroyOnLoad(tube);
            Tubes[id] = tube;
            return tube;
        }
        private static Shader _plainShader;
        private static bool _plainLooked;

        private static void RecolourLaserPart(Renderer renderer, Color colour)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            var mesh = filter != null ? filter.sharedMesh : null;
            var material = renderer.sharedMaterial;

            // Once per launch, what the part is made of, for the next measurement.
            if (!_laserDescribed && renderer.name == "Beam")
            {
                _laserDescribed = true;
                var shader = material != null ? material.shader : null;
                var properties = new List<string>();
                if (shader != null)
                {
                    for (var i = 0; i < shader.GetPropertyCount(); i++)
                        properties.Add($"{shader.GetPropertyName(i)}({shader.GetPropertyType(i)})");
                }
                var sample = mesh != null && mesh.isReadable && mesh.colors32 != null && mesh.colors32.Length > 0
                    ? mesh.colors32[0].ToString()
                    : "none";
                Plugin.Log.LogInfo($"{Tag} Laser beam: shader {(shader != null ? shader.name : "?")} [{string.Join(", ", properties)}], "
                                   + $"texture {(material != null && material.mainTexture != null ? material.mainTexture.name : "none")}, "
                                   + $"mesh {(mesh != null ? mesh.name : "?")} readable={(mesh != null && mesh.isReadable)} "
                                   + $"vertices={(mesh != null ? mesh.vertexCount : 0)} first colour={sample}.");
            }

            if (mesh != null && mesh.isReadable)
            {
                try
                {
                    // An instance of its own: the mesh is the same for every laser.
                    var own = filter.mesh;
                    var colours = own.colors32;
                    var count = own.vertexCount;
                    var painted = new Color32[count];
                    var target = (Color32)colour;
                    for (var i = 0; i < count; i++)
                    {
                        var alpha = colours != null && colours.Length == count ? colours[i].a : (byte)255;
                        painted[i] = new Color32(target.r, target.g, target.b, alpha);
                    }
                    own.colors32 = painted;
                    return;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} A laser's {renderer.name} kept its colours: {ex.Message}");
                }
            }

            // Keeps househouse/laser, which stretches the beam; it vanishes under other shaders.
            if (renderer.name == "Beam")
            {
                if (filter != null && mesh != null)
                    filter.sharedMesh = BeamTube(mesh.bounds, colour);
                return;
            }

            // Unreadable mesh: Standard ignores the red vertex colours, Particles Unlit does not.
            if (!_plainLooked)
            {
                _plainLooked = true;
                _plainShader = Shader.Find("Standard");
                if (_plainShader == null)
                    Plugin.Log.LogWarning($"{Tag} No Standard shader to colour the lasers' beams with; they stay red.");
            }
            if (_plainShader == null)
                return;

            var plain = new Material(_plainShader) { name = "laser (AP colour)" };
            plain.SetColor("_Color", colour);
            plain.EnableKeyword("_EMISSION");
            plain.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            plain.SetColor("_EmissionColor", colour * 2f);
            renderer.sharedMaterial = plain;
        }

        // The towers' launchers keep their own colours, and the other launchers never take those.
        private static readonly string[] TowerColours = { "Red", "Yellow", "Blue", "Green" };

        private static (string name, Color body, Color halo) FireworkColour(string landmark)
        {
            var free = new List<(string name, Color body, Color halo)>();
            foreach (var index in Palette.Current)
            {
                if (Array.IndexOf(TowerColours, Palette.Colours[index].name) < 0)
                    free.Add(Palette.Colours[index]);
            }
            if (free.Count == 0)
            {
                foreach (var entry in Palette.Colours)
                {
                    if (Array.IndexOf(TowerColours, entry.name) < 0)
                        free.Add(entry);
                }
            }

            unchecked
            {
                var hash = 2166136261u;
                foreach (var c in "firework " + landmark)
                    hash = (hash ^ c) * 16777619u;
                return free[(int)(hash % (uint)free.Count)];
            }
        }

        private static int PaintFireworks()
        {
            if (!Palette.Active)
                return 0;

            var count = 0;
            foreach (var launcher in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (launcher == null || !launcher.name.StartsWith(LauncherName, StringComparison.Ordinal))
                    continue;
                var landmark = PackChecks.LandmarkOf(launcher);
                if (string.IsNullOrEmpty(landmark) || landmark.StartsWith("Lookout_", StringComparison.Ordinal))
                    continue;

                try
                {
                    var colour = FireworkColour(landmark);
                    foreach (var helper in launcher.GetComponentsInChildren<PropertyBlockHelper>(true))
                    {
                        if (helper == null || helper.colorSettings == null)
                            continue;
                        var property = helper.gameObject.name switch
                        {
                            "fireworkLauncherModel" => "_TintColor",
                            "HangingSpark" => "_EmissionColor",
                            _ => null,
                        };
                        if (property == null)
                            continue;
                        var settings = helper.colorSettings;
                        for (var i = 0; i < settings.Length; i++)
                        {
                            // An element of an IL2CPP struct array comes back as a copy.
                            var setting = settings[i];
                            if (setting.propertyName != property)
                                continue;
                            setting.color = property == "_TintColor" ? colour.body : colour.halo;
                            settings[i] = setting;
                        }
                        helper.Refresh();
                    }

                    foreach (var system in launcher.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        if (system.name != "HangingSpark" && system.name != "SparkParticleSystem")
                            continue;
                        var main = system.main;
                        main.startColor = new ParticleSystem.MinMaxGradient(colour.halo);
                    }
                    count++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} Could not paint the firework in {landmark}: {ex.Message}");
                }
            }
            return count;
        }

        // A gun's place in the seed's draw: base 0, Blue 1, Green 2, Yellow 3.
        private static int FlareGunSlot(string name)
        {
            if (name.StartsWith("FlareGunPropBlue", StringComparison.Ordinal))
                return 1;
            if (name.StartsWith("FlareGunPropGreen", StringComparison.Ordinal))
                return 2;
            if (name.StartsWith("FlareGunPropYellow", StringComparison.Ordinal))
                return 3;
            return 0;
        }

        private static bool PaintIfGun(GameObject root)
        {
            if (!StartsWithAny(root.name, GunNames))
                return false;

            var rainbow = root.name.Contains(RainbowMarker, StringComparison.Ordinal);
            if (!rainbow && !Palette.Active)
                return true;

            var colour = rainbow ? default : Palette.At(FlareGunSlot(root.name));
            var body = rainbow ? Palette.RainbowBody : colour.body;

            foreach (var helper in root.GetComponentsInChildren<PropertyBlockHelper>(true))
            {
                var target = helper != null ? helper.targetRenderer : null;
                if (target == null || helper.targetSubmesh != 0 || target.name != "flaregun" && target.name != "flaregunbarrel")
                    continue;
                var settings = helper.colorSettings;
                if (settings == null || settings.Length == 0)
                    continue;

                // An element of an IL2CPP struct array comes back as a copy.
                var setting = settings[0];
                setting.color = body;
                settings[0] = setting;
                helper.Refresh();
            }

            foreach (var effect in root.GetComponentsInChildren<PeckEffectParticleNetworked>(true))
            {
                if (effect == null || effect.name != "FlareGunFlare" || effect.targetParticleSystem == null)
                    continue;
                if (rainbow)
                {
                    if (!RainbowShots.Contains(effect.targetParticleSystem))
                        RainbowShots.Add(effect.targetParticleSystem);
                }
                else
                {
                    FlareColours.Apply(effect.targetParticleSystem, colour.halo);
                }
            }

            return true;
        }

        private static void TickRainbow()
        {
            if (RainbowShots.Count == 0)
                return;
            var colour = Palette.RainbowNow();
            if (colour == _rainbowShown)
                return;
            _rainbowShown = colour;

            for (var i = RainbowShots.Count - 1; i >= 0; i--)
            {
                var shot = RainbowShots[i];
                if (shot == null)
                {
                    RainbowShots.RemoveAt(i);
                    continue;
                }
                FlareColours.Apply(shot, colour);
            }
        }
    }
}
