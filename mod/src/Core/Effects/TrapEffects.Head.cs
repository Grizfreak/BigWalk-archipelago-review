using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    internal static partial class TrapEffects
    {
        // Other players' heads grow; the own head is left alone, the camera may hang from it.
        internal const string Head = "head";
        private const float HeadFactor = 2.5f;
        private const float HeadEase = 1f;
        private static float _headStart = -1f;
        private static float _headUntil = -1f;
        private static readonly Dictionary<int, (Transform bone, Vector3 own, Vector3 ownPosition)> Heads = new();

        // Metres the head is raised per unit of growth, so it sits on the body instead of sinking.
        private const float HeadLift = 0.15f;

        private static Transform HeadBone(PlayerCharacter player)
        {
            var renderers = player.looks != null ? player.looks.headRenderers : null;
            if (renderers == null)
                return null;
            Transform common = null;
            foreach (var part in renderers)
            {
                var t = part != null && part.renderer != null ? part.renderer.transform : null;
                if (t == null)
                    continue;
                if (common == null)
                {
                    common = t.parent;
                    continue;
                }
                while (common != null && !t.IsChildOf(common))
                    common = common.parent;
            }
            return common != null && common != player.transform ? common : null;
        }

        private static void StartHead(int seconds)
        {
            var now = Time.unscaledTime;
            if (_headUntil < 0f)
                _headStart = now;
            _headUntil = Mathf.Max(_headUntil, now + seconds);
        }

        private static void StepHead(float now)
        {
            float amount;
            if (now < _headStart + HeadEase)
                amount = (now - _headStart) / HeadEase;
            else if (now < _headUntil)
                amount = 1f;
            else
                amount = 1f - (now - _headUntil) / HeadEase;
            if (amount <= 0f && now >= _headUntil)
            {
                EndHead();
                return;
            }
            var factor = Mathf.Lerp(1f, HeadFactor, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(amount)));
            _headFactor = factor;
            foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None))
            {
                if (player == null || player.isLocalPlayer)
                    continue;
                var id = player.GetInstanceID();
                if (!Heads.TryGetValue(id, out var head) || head.bone == null)
                {
                    var bone = HeadBone(player);
                    if (bone == null)
                        continue;
                    head = (bone, bone.localScale, bone.localPosition);
                    Heads[id] = head;
                }
                head.bone.localScale = head.own * factor;
            }
        }

        private static float _headFactor = 1f;

        private static void EndHead()
        {
            if (_headUntil < 0f)
                return;
            _headUntil = -1f;
            _headStart = -1f;
            _headFactor = 1f;
            foreach (var head in Heads.Values)
            {
                if (head.bone == null)
                    continue;
                head.bone.localScale = head.own;
            }
            Heads.Clear();
            Plugin.Log.LogInfo($"{Tag} Big Head over.");
        }

        // Other players' heads hidden and replaced by a copy of the island's buoy lamp.
        internal const string Brain = "brain";
        private const float BrainSize = 0.8f;
        private const float BrainLift = 0.1f;
        private const float BrainShine = 1.8f;
        private static readonly Color BrainGlow = new(1f, 0.8f, 0.45f, 1f);
        private static float _brainUntil = -1f;
        private static readonly Dictionary<int, (PlayerCharacter player, GameObject lamp)> Brains = new();

        private static void StartBrain(int seconds)
        {
            _brainUntil = Mathf.Max(_brainUntil, Time.unscaledTime + seconds);
            Plugin.Log.LogInfo($"{Tag} Big Brain for {seconds}s.");
        }

        // A lit lamp of the island to copy, if any.
        private static GameObject LampTemplate()
        {
            GameObject any = null;
            foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (prop == null || !prop.gameObject.name.StartsWith("BuoyProp", StringComparison.Ordinal) || ReceivedItemSpawner.IsCosmeticClone(prop))
                    continue;
                foreach (var light in prop.GetComponentsInChildren<Light>(false))
                {
                    if (light != null && light.enabled && light.name == "bouyLight")
                        return prop.gameObject;
                }
                any ??= prop.gameObject;
            }
            return any;
        }

        private static GameObject MakeBrain(Transform bone)
        {
            var template = LampTemplate();
            if (template == null)
                return null;
            var source = template.transform;
            var lamp = new GameObject("AP Big Brain");
            var model = new GameObject("model");
            model.transform.SetParent(lamp.transform, false);
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            var parts = new List<Transform>();
            foreach (var renderer in template.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer == null || (renderer.name != "buoy" && renderer.name != "LobbyFlareMesh"))
                    continue;
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    continue;
                var part = new GameObject(renderer.name);
                part.transform.SetParent(model.transform, false);
                var position = source.InverseTransformPoint(renderer.transform.position);
                var rotation = Quaternion.Inverse(source.rotation) * renderer.transform.rotation;
                var lossy = renderer.transform.lossyScale;
                var rootScale = source.lossyScale;
                var scale = new Vector3(lossy.x / rootScale.x, lossy.y / rootScale.y, lossy.z / rootScale.z);
                part.transform.localPosition = position;
                part.transform.localRotation = rotation;
                part.transform.localScale = scale;
                part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var copy = part.AddComponent<MeshRenderer>();
                copy.sharedMaterials = renderer.sharedMaterials;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                // The halo glows whatever the template's state.
                if (renderer.name == "LobbyFlareMesh")
                    block.SetColor("_MainColor", BrainGlow);
                // Tint pushed past white so the body shines: a lit lamp's colours can't be read.
                else
                    block.SetColor("_TintColor", block.GetColor("_TintColor") * BrainShine);
                copy.SetPropertyBlock(block);
                var bounds = filter.sharedMesh.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = position + rotation * Vector3.Scale(corner, scale);
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
                parts.Add(part.transform);
            }
            if (parts.Count == 0)
            {
                UnityEngine.Object.Destroy(lamp);
                return null;
            }
            var centre = (min + max) / 2f;
            foreach (var part in parts)
                part.localPosition -= centre;
            var extent = max - min;
            var size = Mathf.Max(extent.x, Mathf.Max(extent.y, extent.z));
            model.transform.localScale = Vector3.one * (size > 0.0001f ? BrainSize / size : 1f);

            // Gentle effects: no light from the lamp on this screen.
            if (!ModConfig.GentleEffects.Value)
            {
                var light = lamp.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 6f;
                light.intensity = 2f;
                light.color = BrainGlow;
            }

            lamp.transform.SetParent(bone, false);
            lamp.transform.position = bone.position + Vector3.up * BrainLift;
            lamp.transform.rotation = Quaternion.identity;
            var boneScale = bone.lossyScale;
            lamp.transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, boneScale.x), 1f / Mathf.Max(0.0001f, boneScale.y), 1f / Mathf.Max(0.0001f, boneScale.z));
            return lamp;
        }

        private static void SetHeadShown(PlayerCharacter player, bool shown)
        {
            var renderers = player != null && player.looks != null ? player.looks.headRenderers : null;
            if (renderers == null)
                return;
            foreach (var part in renderers)
            {
                if (part?.renderer != null && part.renderer.enabled != shown)
                    part.renderer.enabled = shown;
            }
        }

        private static void LateBrain()
        {
            if (_brainUntil < 0f)
                return;
            if (Time.unscaledTime >= _brainUntil)
            {
                EndBrain();
                return;
            }
            foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None))
            {
                if (player == null || player.isLocalPlayer)
                    continue;
                var id = player.GetInstanceID();
                if (!Brains.TryGetValue(id, out var brain) || brain.lamp == null)
                {
                    var bone = HeadBone(player);
                    var lamp = bone != null ? MakeBrain(bone) : null;
                    if (lamp == null)
                        continue;
                    brain = (player, lamp);
                    Brains[id] = brain;
                }
                SetHeadShown(player, false);
            }
        }

        private static void EndBrain()
        {
            if (_brainUntil < 0f)
                return;
            _brainUntil = -1f;
            foreach (var brain in Brains.Values)
            {
                if (brain.lamp != null)
                    UnityEngine.Object.Destroy(brain.lamp);
                SetHeadShown(brain.player, true);
            }
            Brains.Clear();
            Plugin.Log.LogInfo($"{Tag} Big Brain over.");
        }
    }
}
