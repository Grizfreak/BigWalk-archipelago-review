using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Plays traps and bonuses on this machine's own player: the game is owner-authoritative.
    internal static class TrapEffects
    {
        internal const string Drop = "drop";
        internal const string Throw = "throw";
        internal const string Trip = "trip";
        internal const string Meeting = "meeting";
        internal const string Night = "night";
        internal const string Day = "day";
        internal const string Speed = "speed";
        internal const string Jump = "jump";
        internal const string Sleep = "sleep";
        internal const string Load = "load";

        // DeathLink received: what is held is dropped and the player is dazed as after a big fall.
        internal const string Knockout = "knockout";

        // The blindfold helmet on every unmasked player for the trap's duration (host only).
        internal const string Mask = "mask";

        // A flare in front of every player, in the Archipelago colours.
        internal const string Flare = "flare";

        // A puzzle's confetti and its sound around every player: filler, only a show.
        internal const string Confetti = "confetti";

        // A clock in every player's hands, on their own computer's time (host only).
        internal const string BigTime = "time";

        // Muted via CmdSetMute and DissonanceComms.IsMuted, held every frame against the mute key.
        internal const string Silence = "silence";
        private static float _silenceUntil = -1f;
        private static bool _silenceWasMuted;
        private static bool _silenceCommsWasMuted;
        private static Dissonance.DissonanceComms _comms;

        // While on, BigSlidePatch lets the velocity drift towards what the controls ask for.
        internal const string Slide = "slide";
        private static float _slideUntil = -1f;

        // Share of the asked-for velocity taken each fixed step (50 a second).
        internal const float SlideGrip = 0.025f;

        internal static bool SlideActive => _slideUntil >= 0f;

        // The Silent Gauntlet's white only; BigGhostPatch sets it after PlayerSpeechless.Update.
        internal const string Ghost = "ghost";
        private static float _ghostUntil = -1f;

        internal static bool GhostActive => _ghostUntil >= 0f;

        // Each player takes the next one's colours in netId order: every machine swaps alike.
        internal const string Paint = "paint";
        private static float _paintUntil = -1f;
        private static readonly List<(PlayerCharacter target, PlayerLookSet.LookColor head, PlayerLookSet.LookColor torso, PlayerLookSet.LookColor legs)> Painted = new();
        private static readonly List<(PlayerCharacter target, PlayerLookSet.LookColor head, PlayerLookSet.LookColor torso, PlayerLookSet.LookColor legs)> OwnColours = new();

        private static void StartPaint(int seconds)
        {
            var players = new List<PlayerCharacter>();
            foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None))
            {
                if (player != null && player.looks != null)
                    players.Add(player);
            }
            if (players.Count < 2)
            {
                Plugin.Log.LogInfo($"{Tag} Big Paint: alone, nothing to swap.");
                return;
            }
            players.Sort((a, b) => a.netId.CompareTo(b.netId));

            EndPaint();
            for (var i = 0; i < players.Count; i++)
            {
                var source = players[(i + 1) % players.Count].looks;
                Painted.Add((players[i], source.headColor, source.torsoColor, source.legsColor));
                var own = players[i].looks;
                OwnColours.Add((players[i], own.headColor, own.torsoColor, own.legsColor));
            }
            _paintUntil = Time.unscaledTime + seconds;
            HoldPaint();
        }

        internal static bool PaintActive => _paintUntil >= 0f;

        // URP render scale lowered, upscaled by nearest pixel; 0.1 is the lowest it takes.
        internal const string Pixels = "pixels";
        private const float PixelsScale = 0.1f;
        private static float _pixelsUntil = -1f;
        private static float _pixelsOwnScale;
        private static UnityEngine.Rendering.Universal.UpscalingFilterSelection _pixelsOwnFilter;

        private static UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset UrpAsset()
        {
            var asset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            return asset != null ? asset.TryCast<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>() : null;
        }

        private static void StartPixels(int seconds)
        {
            if (Gentle(Pixels))
                return;
            var asset = UrpAsset();
            if (asset == null)
            {
                Plugin.Log.LogWarning($"{Tag} Big Pixels: no URP asset.");
                return;
            }
            if (_pixelsUntil < 0f)
            {
                _pixelsOwnScale = asset.renderScale;
                _pixelsOwnFilter = asset.upscalingFilter;
            }
            _pixelsUntil = Mathf.Max(_pixelsUntil, Time.unscaledTime + seconds);
            HoldPixels(asset);
            Plugin.Log.LogInfo($"{Tag} Big Pixels: render scale {_pixelsOwnScale:F2} -> {PixelsScale:F2}, filter {_pixelsOwnFilter} -> Point.");
        }

        private static void HoldPixels(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset asset)
        {
            if (asset == null)
                return;
            if (asset.renderScale != PixelsScale)
                asset.renderScale = PixelsScale;
            if (asset.upscalingFilter != UnityEngine.Rendering.Universal.UpscalingFilterSelection.Point)
                asset.upscalingFilter = UnityEngine.Rendering.Universal.UpscalingFilterSelection.Point;
        }

        private static void EndPixels()
        {
            if (_pixelsUntil < 0f)
                return;
            _pixelsUntil = -1f;
            try
            {
                var asset = UrpAsset();
                if (asset != null)
                {
                    asset.renderScale = _pixelsOwnScale;
                    asset.upscalingFilter = _pixelsOwnFilter;
                }
                Plugin.Log.LogInfo($"{Tag} Big Pixels over.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Big Pixels end: {ex.Message}");
            }
        }

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

        // After the animation, which places the head again every frame.
        internal static void LateTick()
        {
            LateBrain();
            LateArms();
            if (_headUntil < 0f || _headFactor <= 1f)
                return;
            var lift = Vector3.up * (_headFactor - 1f) * HeadLift;
            foreach (var head in Heads.Values)
            {
                if (head.bone != null)
                    head.bone.position += lift;
            }
        }

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

        // Seen on the other players only: the first-person hands ignore their scale.
        internal const string Arms = "arms";
        private const float ArmsFactor = 2.5f;
        private const float ArmsEase = 1f;
        private static float _armsStart = -1f;
        private static float _armsUntil = -1f;
        private static readonly Dictionary<int, List<(Transform hand, Vector3 own)>> Hands = new();
        private static bool _handsLogged;

        private static List<(Transform hand, Vector3 own)> HandsOf(PlayerCharacter player)
        {
            var hands = new List<(Transform, Vector3)>();
            foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.name.IndexOf("hand", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                hands.Add((renderer.transform, renderer.transform.localScale));
            }
            if (!_handsLogged)
            {
                _handsLogged = true;
                var names = new List<string>();
                foreach (var (hand, _) in hands)
                    names.Add(hand.name);
                Plugin.Log.LogInfo($"{Tag} Big Arms: hands of {player.gameObject.name}: {(names.Count == 0 ? "none found" : string.Join(", ", names))}.");
            }
            return hands;
        }

        private static void StartArms(int seconds)
        {
            var now = Time.unscaledTime;
            if (_armsUntil < 0f)
                _armsStart = now;
            _armsUntil = Mathf.Max(_armsUntil, now + seconds);
        }

        private static void StepArms(float now)
        {
            float amount;
            if (now < _armsStart + ArmsEase)
                amount = (now - _armsStart) / ArmsEase;
            else if (now < _armsUntil)
                amount = 1f;
            else
                amount = 1f - (now - _armsUntil) / ArmsEase;
            if (amount <= 0f && now >= _armsUntil)
            {
                EndArms();
                return;
            }
            _armsFactor = Mathf.Lerp(1f, ArmsFactor, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(amount)));
            foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None))
            {
                if (player == null)
                    continue;
                var id = player.GetInstanceID();
                if (!Hands.ContainsKey(id))
                    Hands[id] = HandsOf(player);
            }
        }

        private static float _armsFactor = 1f;

        // After the game places the arms, since it sets them again every frame.
        private static void LateArms()
        {
            if (_armsUntil < 0f)
                return;
            foreach (var hands in Hands.Values)
                foreach (var (hand, own) in hands)
                {
                    if (hand != null)
                        hand.localScale = own * _armsFactor;
                }
        }

        private static void EndArms()
        {
            if (_armsUntil < 0f)
                return;
            _armsUntil = -1f;
            _armsStart = -1f;
            _armsFactor = 1f;
            foreach (var hands in Hands.Values)
                foreach (var (hand, own) in hands)
                {
                    if (hand != null)
                        hand.localScale = own;
                }
            Hands.Clear();
            Plugin.Log.LogInfo($"{Tag} Big Arms over.");
        }

        // Green cloud on every player; each machine pushes its own player away from the others.
        internal const string Smell = "smell";
        private const float SmellRadius = 5f;
        private const float SmellPush = 9f;
        // Added to the controls' velocity (BigSmellPatch); moving the body went through walls.
        internal static Vector3 SmellVelocity;
        private static float _smellUntil = -1f;
        private static readonly Dictionary<int, GameObject> Clouds = new();

        private static void StartSmell(int seconds)
        {
            _smellUntil = Mathf.Max(_smellUntil, Time.unscaledTime + seconds);
            Plugin.Log.LogInfo($"{Tag} Big Smell for {seconds}s.");
        }

        private static bool _smellLogged;

        private static ParticleSystem SmellTemplate()
        {
            foreach (var effect in UnityEngine.Object.FindObjectsByType<PeckEffectParticleNetworked>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (effect != null && effect.name == "FlareGunFlare" && effect.targetParticleSystem != null)
                    return effect.targetParticleSystem;
            }
            return null;
        }

        // The shot's own playing is off: its smoke system only gets the green puffs emitted here.
        private static GameObject MakeCloud(PlayerCharacter player)
        {
            var template = SmellTemplate();
            if (template == null)
                return null;
            var cloud = UnityEngine.Object.Instantiate(template.gameObject);
            cloud.name = "AP Big Smell";
            cloud.transform.SetParent(player.transform, false);
            cloud.transform.localPosition = Vector3.zero;
            var names = new List<string>();
            foreach (var system in cloud.GetComponentsInChildren<ParticleSystem>(true))
            {
                names.Add(system.name);
                system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            foreach (var light in cloud.GetComponentsInChildren<Light>(true))
                light.enabled = false;
            if (!_smellLogged)
            {
                _smellLogged = true;
                Plugin.Log.LogInfo($"{Tag} Big Smell: shot parts {string.Join(", ", names)}.");
            }
            return cloud;
        }

        private static ParticleSystem SmokeOf(GameObject cloud)
        {
            foreach (var system in cloud.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (system.name == "SmokeParticleSystem")
                    return system;
            }
            return null;
        }

        private const float SmellPuffsPerSecond = 40f;
        private static readonly Dictionary<int, float> PuffDebt = new();

        private static void AnimateCloud(GameObject cloud, float now)
        {
            if (cloud == null)
                return;
            var smoke = SmokeOf(cloud);
            if (smoke == null)
                return;
            var id = cloud.GetInstanceID();
            PuffDebt.TryGetValue(id, out var debt);
            debt += SmellPuffsPerSecond * Time.deltaTime;
            var centre = cloud.transform.position + Vector3.up * 1f;
            while (debt >= 1f)
            {
                debt -= 1f;
                var offset = UnityEngine.Random.insideUnitSphere;
                offset.y *= 0.8f;
                // The interop keeps only the raw fields of EmitParams and Particle.
                var life = UnityEngine.Random.Range(1.5f, 2.5f);
                var particle = new ParticleSystem.Particle();
                particle.m_Position = centre + offset * 1.2f;
                particle.m_Velocity = offset * 0.4f + Vector3.up * 0.25f;
                particle.m_StartSize = Vector3.one * UnityEngine.Random.Range(1f, 2f);
                particle.m_Lifetime = life;
                particle.m_StartLifetime = life;
                particle.m_StartColor = Color.Lerp(new Color(0.35f, 0.6f, 0.1f, 0.6f), new Color(0.6f, 0.8f, 0.3f, 0.5f), UnityEngine.Random.value);
                var emit = new ParticleSystem.EmitParams();
                emit.m_Particle = particle;
                emit.m_PositionSet = emit.m_VelocitySet = emit.m_StartSizeSet = emit.m_StartLifetimeSet = emit.m_StartColorSet = true;
                emit.m_ApplyShapeToPosition = false;
                smoke.Emit(emit, 1);
            }
            PuffDebt[id] = debt;
        }

        private static void StepSmell(float now)
        {
            if (now >= _smellUntil)
            {
                EndSmell();
                return;
            }
            PlayerCharacter own = null;
            var players = UnityEngine.Object.FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player == null)
                    continue;
                if (player.isLocalPlayer)
                    own = player;
                var id = player.GetInstanceID();
                // Gentle effects: no gas on this screen, the push stays.
                if (ModConfig.GentleEffects.Value)
                    continue;
                if (!Clouds.TryGetValue(id, out var cloud) || cloud == null)
                    Clouds[id] = cloud = MakeCloud(player);
                AnimateCloud(cloud, now);
            }
            SmellVelocity = Vector3.zero;
            if (own == null)
                return;
            var push = Vector3.zero;
            foreach (var player in players)
            {
                if (player == null || player == own)
                    continue;
                // The bodies, not the character roots, which do not follow them.
                var away = BodyPosition(own) - BodyPosition(player);
                away.y = 0f;
                var distance = away.magnitude;
                if (distance >= SmellRadius)
                    continue;
                var direction = distance > 0.01f ? away / distance : Vector3.forward;
                push += direction * (1f - distance / SmellRadius);
            }
            push = Vector3.ClampMagnitude(push, 1f) * SmellPush;
            // The controls' velocity is in the player's own frame, the way the camera faces.
            var camera = Camera.main;
            if (camera != null)
                push = Quaternion.Inverse(Quaternion.Euler(0f, camera.transform.eulerAngles.y, 0f)) * push;
            SmellVelocity = push;
        }

        private static Vector3 BodyPosition(PlayerCharacter player)
        {
            var body = player.GetComponentInChildren<Rigidbody>();
            return body != null ? body.position : player.transform.position;
        }

        private static void EndSmell()
        {
            if (_smellUntil < 0f)
                return;
            _smellUntil = -1f;
            SmellVelocity = Vector3.zero;
            foreach (var cloud in Clouds.Values)
            {
                if (cloud != null)
                    UnityEngine.Object.Destroy(cloud);
            }
            Clouds.Clear();
            PuffDebt.Clear();
            Plugin.Log.LogInfo($"{Tag} Big Smell over.");
        }

        // Scales each Rigidbody transform, so the collider, model and camera shrink together.
        internal const string Brother = "brother";
        private const float BrotherFactor = 0.4f;
        private const float BrotherEase = 2f;
        // Voices pitched up as the bodies shrink, through BigBrotherVoicePatch.
        private const float BrotherVoiceRaise = 1.5f;
        internal static float BrotherVoicePitch { get; private set; } = 1f;
        private static float _brotherStart = -1f;
        private static float _brotherUntil = -1f;
        private static readonly Dictionary<int, (Transform body, Vector3 own)> Brothers = new();

        private static void StartBrother(int seconds)
        {
            var now = Time.unscaledTime;
            if (_brotherUntil < 0f)
                _brotherStart = now;
            _brotherUntil = Mathf.Max(_brotherUntil, now + seconds);
            Plugin.Log.LogInfo($"{Tag} Big Brother for {seconds}s.");
        }

        private static void StepBrother(float now)
        {
            float amount;
            if (now < _brotherStart + BrotherEase)
                amount = (now - _brotherStart) / BrotherEase;
            else if (now < _brotherUntil)
                amount = 1f;
            else
                amount = 1f - (now - _brotherUntil) / BrotherEase;
            if (amount <= 0f && now >= _brotherUntil)
            {
                EndBrother();
                return;
            }
            var eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(amount));
            var factor = Mathf.Lerp(1f, BrotherFactor, eased);
            BrotherVoicePitch = Mathf.Lerp(1f, BrotherVoiceRaise, eased);
            foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None))
            {
                if (player == null)
                    continue;
                var id = player.GetInstanceID();
                if (!Brothers.TryGetValue(id, out var brother) || brother.body == null)
                {
                    var body = player.GetComponentInChildren<Rigidbody>();
                    if (body == null)
                        continue;
                    brother = (body.transform, body.transform.localScale);
                    Brothers[id] = brother;
                }
                brother.body.localScale = brother.own * factor;
            }
        }

        private static void EndBrother()
        {
            if (_brotherUntil < 0f)
                return;
            _brotherUntil = -1f;
            _brotherStart = -1f;
            BrotherVoicePitch = 1f;
            foreach (var brother in Brothers.Values)
            {
                if (brother.body != null)
                    brother.body.localScale = brother.own;
            }
            Brothers.Clear();
            Plugin.Log.LogInfo($"{Tag} Big Brother over.");
        }

        // The nearest puzzle still holding its gourd is reset (host only, Traps.FireReset).
        internal const string Reset = "reset";

        // The host spawns a chair by each player; each machine seats its player once it is there.
        internal const string Break = "break";
        private const float BreakWait = 5f;
        private static Vector3 _breakAt;
        private static float _breakUntil = -1f;

        private static void StartBreak(Vector3? chair)
        {
            if (chair == null)
                return;
            _breakAt = chair.Value;
            _breakUntil = Time.unscaledTime + BreakWait;
            Plugin.Log.LogInfo($"{Tag} Big Break: a chair at {_breakAt}.");
        }

        private static List<PlayerPose> BreakSeats()
        {
            var seats = new List<PlayerPose>();
            foreach (var pose in UnityEngine.Object.FindObjectsByType<PlayerPose>(FindObjectsSortMode.None))
            {
                var prop = pose != null ? pose.parentProp : null;
                if (prop != null && prop.gameObject.name.StartsWith("foldingChair", StringComparison.Ordinal) && ReceivedItemSpawner.IsCosmeticClone(prop))
                    seats.Add(pose);
            }
            return seats;
        }

        private static PlayerPose NearestSeat(List<PlayerPose> seats, Vector3 at, float within)
        {
            PlayerPose seat = null;
            foreach (var pose in seats)
            {
                var distance = Vector3.Distance(pose.transform.position, at);
                if (distance < within)
                {
                    within = distance;
                    seat = pose;
                }
            }
            return seat;
        }

        // Each machine also seats the others as it sees them, or a guest would not show them sat.
        private static readonly HashSet<int> BreakSat = new();

        private static void StepBreak(float now)
        {
            if (now >= _breakUntil)
            {
                _breakUntil = -1f;
                BreakSat.Clear();
                return;
            }
            var seats = BreakSeats();
            if (seats.Count == 0)
                return;
            foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None))
            {
                if (player == null || BreakSat.Contains(player.GetInstanceID()))
                    continue;
                var poser = player.poser;
                if (poser == null)
                    continue;
                var seat = player.isLocalPlayer ? NearestSeat(seats, _breakAt, 3f) : NearestSeat(seats, BodyPosition(player), 3f);
                if (seat == null)
                    continue;
                BreakSat.Add(player.GetInstanceID());
                if (poser.currentPose != null && poser.currentPose.Pointer == seat.Pointer)
                    continue;
                poser.EnterPose(seat);
                Plugin.Log.LogInfo($"{Tag} Big Break: {player.gameObject.name} sat here on ticket {seat.ticket}.");
            }
        }

        internal const string Goggles = "goggles";
        private const float GogglesFactor = 0.45f;
        private const float GogglesEase = 1.5f;
        private static float _gogglesStart = -1f;
        private static float _gogglesUntil = -1f;
        private static float _gogglesOwnFov;
        private static float _gogglesApplied;

        private static void StartGoggles(int seconds)
        {
            if (Gentle(Goggles))
                return;
            var now = Time.unscaledTime;
            if (_gogglesUntil < 0f)
            {
                _gogglesOwnFov = PlayerCameraMinder.fovFromSettings;
                _gogglesApplied = _gogglesOwnFov;
                _gogglesStart = now;
            }
            _gogglesUntil = Mathf.Max(_gogglesUntil, now + seconds);
            Plugin.Log.LogInfo($"{Tag} Big Goggles: field of view {_gogglesOwnFov:F0} towards {_gogglesOwnFov * GogglesFactor:F0}.");
        }

        private static void StepGoggles(float now)
        {
            // The player changed the setting meanwhile: that becomes their own.
            if (PlayerCameraMinder.fovFromSettings != _gogglesApplied)
                _gogglesOwnFov = PlayerCameraMinder.fovFromSettings / Mathf.Max(0.01f, _gogglesApplied / _gogglesOwnFov);

            float amount;
            if (now < _gogglesStart + GogglesEase)
                amount = (now - _gogglesStart) / GogglesEase;
            else if (now < _gogglesUntil)
                amount = 1f;
            else
                amount = 1f - (now - _gogglesUntil) / GogglesEase;
            if (amount <= 0f && now >= _gogglesUntil)
            {
                EndGoggles();
                return;
            }
            amount = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(amount));
            _gogglesApplied = _gogglesOwnFov * Mathf.Lerp(1f, GogglesFactor, amount);
            PlayerCameraMinder.fovFromSettings = _gogglesApplied;
        }

        private static void EndGoggles()
        {
            if (_gogglesUntil < 0f)
                return;
            _gogglesUntil = -1f;
            _gogglesStart = -1f;
            PlayerCameraMinder.fovFromSettings = _gogglesOwnFov;
            Plugin.Log.LogInfo($"{Tag} Big Goggles over: field of view {_gogglesOwnFov:F0}.");
        }

        // A timed tomato in the hands; when it pops the player is thrown high (no DeathLink).
        internal const string Air = "air";
        // The game caps a body near 60 m/s, so the push is held for a while instead.
        private const float ThrustSpeed = 60f;
        private const float ThrustSeconds = 5f;
        private const float CeilingRoom = 2f;
        private static bool _countdownLaunch;

        // Swapped in as the game sets it: setting it every frame from outside made it flicker.

        internal static bool TryPaintColour(PlayerLooks looks, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<PlayerLooks.LooksRenderer> part, out PlayerLookSet.LookColor colour)
        {
            foreach (var (target, head, torso, legs) in Painted)
            {
                if (target == null || target.looks == null || part == null || target.looks.Pointer != looks.Pointer)
                    continue;
                if (looks.headRenderers != null && part.Pointer == looks.headRenderers.Pointer) { colour = head; return true; }
                if (looks.torsoRenderers != null && part.Pointer == looks.torsoRenderers.Pointer) { colour = torso; return true; }
                if (looks.legsRenderers != null && part.Pointer == looks.legsRenderers.Pointer) { colour = legs; return true; }
            }
            colour = default;
            return false;
        }

        private static void HoldPaint()
        {
            foreach (var (target, head, torso, legs) in Painted)
            {
                var looks = target != null ? target.looks : null;
                if (looks == null)
                    continue;
                looks.SetColor(looks.headRenderers, head);
                looks.SetColor(looks.torsoRenderers, torso);
                looks.SetColor(looks.legsRenderers, legs);
            }
        }

        private static void EndPaint()
        {
            if (Painted.Count == 0)
            {
                _paintUntil = -1f;
                return;
            }
            // Off first, or the patch would swap out the colours put back below.
            _paintUntil = -1f;
            foreach (var (target, head, torso, legs) in OwnColours)
            {
                try
                {
                    var looks = target != null ? target.looks : null;
                    if (looks == null)
                        continue;
                    looks.SetColor(looks.headRenderers, head);
                    looks.SetColor(looks.torsoRenderers, torso);
                    looks.SetColor(looks.legsRenderers, legs);
                    looks.RefreshLook();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} Big Paint end: {ex.Message}");
                }
            }
            Painted.Clear();
            OwnColours.Clear();
            _paintUntil = -1f;
            Plugin.Log.LogInfo($"{Tag} Big Paint over.");
        }

        // The game writes whiteness only when it changes, so each one's own is set back at the end.
        private static void EndGhost()
        {
            if (_ghostUntil < 0f)
                return;
            _ghostUntil = -1f;
            try
            {
                foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None))
                {
                    if (player != null && player.looks != null)
                        player.looks.SetSpeechlessness(player.speechless != null ? player.speechless.speechlessness : 0f);
                }
                Plugin.Log.LogInfo($"{Tag} Big Ghost over.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Big Ghost end: {ex.Message}");
            }
        }

        // Bonuses get the item sound, not the trap alert.
        internal static bool IsBonus(string key) => key == Day || key == Speed || key == Jump || key == Confetti;

        // Every effect this build plays, so a key from a newer apworld is refused by name.
        internal static readonly string[] Known = { Drop, Throw, Trip, Meeting, Night, Sleep, Load, Mask, Flare, BigTime, Silence, Slide, Ghost, Paint, Air, Goggles, Pixels, Head, Brain, Arms, Smell, Brother, Reset, Break, Day, Speed, Jump, Confetti, Knockout };

        // Gentle effects: traps that are hard to look at are not shown on this screen.
        private static bool Gentle(string key)
        {
            if (!ModConfig.GentleEffects.Value)
                return false;
            Plugin.Log.LogInfo($"{Tag} Gentle effects on this machine: {DisplayName(key)} is not shown.");
            return true;
        }

        internal static string DisplayName(string key)
        {
            if (key == Knockout)
                return "Knock-out";
            return string.IsNullOrEmpty(key) ? "?" : "Big " + char.ToUpperInvariant(key[0]) + key.Substring(1);
        }

        private const float ThrowHoldSeconds = 1f;
        private const float SpeedFactor = 1.75f;
        private const float JumpFactor = 1.8f;
        private const float MidnightHour = 0f;
        private const float NoonHour = 12f;

        private const string Tag = "[" + nameof(TrapEffects) + "]";

        private static PlayerCharacter _throwPlayer;
        private static float _throwAt = -1f;
        private static double _throwWindUpStart;
        private static bool _cancelRefusedLogged;

        private static PlayerTunings _moveTunings;
        private static float[] _moveBase;
        private static float _speedUntil = -1f;
        private static float _jumpUntil = -1f;
        private static float _sleepUntil = -1f;
        private static float _sleepStart;
        private static PlayerCharacter _sleeper;

        // Big Speed, Big Jump or Big Sleep holds this machine's tunings (BigRunCompat).
        internal static bool MovementActive => _moveTunings != null;

        private const float AsleepSeconds = 2f;
        private const float AwakeSeconds = 5f;

        // Lasting effects received before this machine's player existed, and until when.
        private static readonly Dictionary<string, float> PendingMovement = new();

        private const float FastForwardSeconds = 4f;
        private static bool _skyRunning;
        private static float _skyFrom;
        private static float _skyTo;
        private static float _skySpan;
        private static float _skyStart;

        // Puzzle screen masks (binoculars, telescope, blindfold); masked players are not moved.
        private static readonly HashSet<int> ActiveMasks = new();

        internal static bool LocalMasked => ActiveMasks.Count > 0;

        internal static void NoteMask(PeckEffectMask mask, bool active)
        {
            if (mask == null)
                return;

            var id = mask.GetInstanceID();
            var changed = active ? ActiveMasks.Add(id) : ActiveMasks.Remove(id);
            if (changed)
                Plugin.Log.LogInfo($"{Tag} Screen mask {mask.maskType} {(active ? "on" : "off")} ('{mask.gameObject.name}'); {ActiveMasks.Count} on.");
        }

        internal static void Play(string key, int seconds, Vector3? destination, float yaw)
        {
            var player = LocalPlayer.Find();
            if (player == null)
            {
                // A guest told on joining, before its player exists: kept for when it does.
                if (key is Speed or Jump or Sleep)
                {
                    PendingMovement[key] = Time.unscaledTime + seconds;
                    Plugin.Log.LogInfo($"{Tag} {DisplayName(key)}: no local player yet; kept for {seconds}s.");
                }
                else
                {
                    Plugin.Log.LogInfo($"{Tag} {DisplayName(key)}: no local player, nothing played.");
                }

                return;
            }

            try
            {
                switch (key)
                {
                    case Drop:
                        DropHeld(player);
                        break;
                    case Slide:
                        StartSlide(seconds);
                        break;
                    case Ghost:
                        _ghostUntil = Time.unscaledTime + seconds;
                        break;
                    case Paint:
                        StartPaint(seconds);
                        break;
                    case Goggles:
                        StartGoggles(seconds);
                        break;
                    case Pixels:
                        StartPixels(seconds);
                        break;
                    case Head:
                        StartHead(seconds);
                        break;
                    case Brain:
                        StartBrain(seconds);
                        break;
                    case Arms:
                        StartArms(seconds);
                        break;
                    case Smell:
                        StartSmell(seconds);
                        break;
                    case Brother:
                        StartBrother(seconds);
                        break;
                    case Break:
                        StartBreak(destination);
                        break;
                    case Air:
                        StartCountdown(key, player, Vector3.zero, Quaternion.identity);
                        _countdownLaunch = true;
                        break;
                    case Silence:
                        StartSilence(player, seconds);
                        break;
                    case Flare:
                        BigFlare.Play();
                        break;
                    case Confetti:
                        BigConfetti.Play();
                        break;
                    case Knockout:
                        DropHeld(player);
                        if (player.faller != null)
                            player.faller.TriggerFall();
                        break;
                    case Throw:
                        StartThrow(player);
                        break;
                    case Trip:
                    case Meeting:
                        if (destination != null)
                            StartCountdown(key, player, destination.Value, Quaternion.Euler(0f, yaw, 0f));
                        else
                            Plugin.Log.LogInfo($"{Tag} {DisplayName(key)}: no destination for this player (spared).");
                        break;
                    case Night:
                        StartSky(MidnightHour); // the same hour for everyone
                        break;
                    case Day:
                        StartSky(NoonHour);
                        break;
                    case Speed:
                    case Jump:
                    case Sleep:
                        StartMovement(player, seconds, key);
                        break;
                    default:
                        Plugin.Log.LogWarning($"{Tag} Unknown effect '{key}', ignored (a newer apworld than this mod?).");
                        return;
                }

                Plugin.Log.LogInfo($"{Tag} {DisplayName(key)} played ({seconds}s).");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} {DisplayName(key)} failed: {ex.Message}");
            }
        }

        internal static void Tick()
        {
            var now = Time.unscaledTime;

            if (_throwAt >= 0f && now >= _throwAt)
                FinishThrow();
            else if (_throwAt >= 0f)
                HoldWindUp();

            if (PendingMovement.Count > 0)
                PlayPendingMovement(now);

            TickMovement(now);

            if (_slideUntil >= 0f && now >= _slideUntil)
                EndSlide();

            if (_ghostUntil >= 0f && now >= _ghostUntil)
                EndGhost();

            if (_paintUntil >= 0f && now >= _paintUntil)
                EndPaint();

            if (_gogglesUntil >= 0f)
                StepGoggles(now);

            if (_headUntil >= 0f)
                StepHead(now);

            if (_armsUntil >= 0f)
                StepArms(now);

            if (_smellUntil >= 0f)
                StepSmell(now);

            if (_brotherUntil >= 0f)
                StepBrother(now);

            if (_breakUntil >= 0f)
                StepBreak(now);

            if (_pixelsUntil >= 0f)
            {
                if (now >= _pixelsUntil)
                    EndPixels();
                else
                    HoldPixels(UrpAsset());
            }

            if (_silenceUntil >= 0f)
            {
                if (now >= _silenceUntil)
                    EndSilence();
                else
                    HoldSilence();
            }

            if (_skyRunning)
                StepSky();

            if (_countdownEnd >= 0f)
                StepCountdown();

            if (_launchUntil >= 0f)
                HoldLaunch();
        }

        // The world is going away: everything is put back while the objects still exist.
        internal static void EndAll()
        {
            _throwAt = -1f;
            _throwPlayer = null;
            _speedUntil = _jumpUntil = _sleepUntil = -1f;
            SetAsleep(false);
            RestoreMovement();
            EndSlide();
            EndSilence();
            EndGhost();
            EndPaint();
            EndGoggles();
            EndPixels();
            EndHead();
            EndBrain();
            EndArms();
            EndSmell();
            EndBrother();
            _countdownEnd = -1f;
            _countdownPlayer = null;

            if (_skyRunning)
            {
                // Never leave the clock frozen part way.
                _skyRunning = false;
                try { SkyManager.ClearFixedTime(); }
                catch (Exception) { }
            }

            ActiveMasks.Clear();
        }

        internal static string Describe()
        {
            var now = Time.unscaledTime;
            return $"masked={LocalMasked} ({ActiveMasks.Count}), "
                   + $"throw in {(_throwAt >= 0f ? (_throwAt - now).ToString("F1") + "s" : "-")}, "
                   + $"speed {(_speedUntil >= 0f ? (_speedUntil - now).ToString("F0") + "s left" : "off")}, "
                   + $"jump {(_jumpUntil >= 0f ? (_jumpUntil - now).ToString("F0") + "s left" : "off")}, "
                   + $"sleep {(_sleepUntil >= 0f ? (_sleepUntil - now).ToString("F0") + "s left" : "off")}, "
                   + $"sky {(_skyRunning ? $"fast forward to {_skyTo:F2}h" : "free")}, "
                   + $"hour {SafeHour():F2}";
        }

        private static float SafeHour()
        {
            try
            {
                return SkyManager.GetCurrentTime();
            }
            catch (Exception)
            {
                return -1f;
            }
        }

        // The teleport comes a second after the tomato pops, so the pop is heard where they stood.
        private const float CountdownSeconds = 0.3f + GadgetItemSpawner.TomatoSeconds + 1f;
        private static float _countdownEnd = -1f;
        private static string _countdownName;
        private static PlayerCharacter _countdownPlayer;
        private static Vector3 _countdownDestination;
        private static Quaternion _countdownRotation;

        private static void StartCountdown(string key, PlayerCharacter player, Vector3 destination, Quaternion rotation)
        {
            _countdownName = DisplayName(key);
            _countdownPlayer = player;
            _countdownDestination = destination;
            _countdownRotation = rotation;
            _countdownEnd = Time.unscaledTime + CountdownSeconds;
            _countdownLaunch = false;
        }

        private static void StepCountdown()
        {
            if (Time.unscaledTime < _countdownEnd)
                return;

            _countdownEnd = -1f;
            var player = _countdownPlayer;
            _countdownPlayer = null;
            if (_countdownLaunch)
            {
                _countdownLaunch = false;
                Launch(player);
                return;
            }
            try
            {
                WorldButtons.TeleportTo(player, _countdownDestination, _countdownRotation);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} {_countdownName}: the teleport failed: {ex.Message}");
            }
        }

        // The game holds a body's climb back, so the speed is set again every frame for a while.
        private static Rigidbody _launchBody;
        private static float _launchUntil = -1f;

        private static void Launch(PlayerCharacter player)
        {
            try
            {
                var body = player != null ? player.GetComponentInChildren<Rigidbody>() : null;
                if (body == null)
                {
                    Plugin.Log.LogWarning($"{Tag} Big Air: no body to throw.");
                    return;
                }
                var reach = ThrustSpeed * ThrustSeconds;
                var from = body.position + Vector3.up * 1.5f;
                foreach (var hit in Physics.RaycastAll(new Ray(from, Vector3.up), reach + CeilingRoom))
                {
                    if (hit.collider == null || hit.collider.attachedRigidbody == body || hit.collider.transform.IsChildOf(player.transform))
                        continue;
                    reach = Mathf.Min(reach, Mathf.Max(0f, hit.distance - CeilingRoom));
                }
                _launchBody = body;
                _launchUntil = Time.unscaledTime + reach / ThrustSpeed;
                Plugin.Log.LogInfo($"{Tag} Big Air: up {reach:F0} m ({(reach < ThrustSpeed * ThrustSeconds ? "under a ceiling" : "open sky")}).");
                HoldLaunch();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Big Air failed: {ex.Message}");
            }
        }

        private static void HoldLaunch()
        {
            if (_launchBody == null || Time.unscaledTime >= _launchUntil)
            {
                _launchBody = null;
                _launchUntil = -1f;
                return;
            }
            var velocity = _launchBody.velocity;
            _launchBody.velocity = new Vector3(velocity.x, ThrustSpeed, velocity.z);
        }

        private static void StartSilence(PlayerCharacter player, int seconds)
        {
            if (_silenceUntil < 0f)
            {
                var networking = player.GetComponent<PlayerNetworking>();
                _silenceWasMuted = networking != null && networking.isMuted;
                _comms = UnityEngine.Object.FindObjectOfType<Dissonance.DissonanceComms>();
                _silenceCommsWasMuted = _comms != null && _comms.IsMuted;
            }

            _silenceUntil = Mathf.Max(_silenceUntil, Time.unscaledTime + seconds);
            HoldSilence();
            Plugin.Log.LogInfo($"{Tag} Big Silence: muted for {seconds}s (was muted: {_silenceWasMuted}, mic {_silenceCommsWasMuted}).");
        }

        private static void HoldSilence()
        {
            try
            {
                var player = LocalPlayer.Find();
                var networking = player != null ? player.GetComponent<PlayerNetworking>() : null;
                if (networking != null && !networking.isMuted)
                    networking.CmdSetMute(true);
                if (_comms == null)
                    _comms = UnityEngine.Object.FindObjectOfType<Dissonance.DissonanceComms>();
                if (_comms != null && !_comms.IsMuted)
                    _comms.IsMuted = true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Big Silence: {ex.Message}");
            }
        }

        private static void EndSilence()
        {
            if (_silenceUntil < 0f)
                return;
            _silenceUntil = -1f;
            try
            {
                var player = LocalPlayer.Find();
                var networking = player != null ? player.GetComponent<PlayerNetworking>() : null;
                if (networking != null && networking.isMuted != _silenceWasMuted)
                    networking.CmdSetMute(_silenceWasMuted);
                if (_comms != null)
                    _comms.IsMuted = _silenceCommsWasMuted;
                Plugin.Log.LogInfo($"{Tag} Big Silence over.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Big Silence end: {ex.Message}");
            }
            _comms = null;
        }

        private static void StartSlide(int seconds)
        {
            _slideUntil = Mathf.Max(_slideUntil, Time.unscaledTime + seconds);
            Plugin.Log.LogInfo($"{Tag} Big Slide: the ground slips for {seconds}s (grip {SlideGrip}).");
        }

        private static void EndSlide()
        {
            if (_slideUntil < 0f)
                return;
            _slideUntil = -1f;
            Plugin.Log.LogInfo($"{Tag} Big Slide over.");
        }

        private static void DropHeld(PlayerCharacter player)
        {
            if (player.hands == null || !player.hands.isHoldingSomething)
            {
                Plugin.Log.LogInfo($"{Tag} Big Drop: hands already empty.");
                return;
            }

            player.decisions?.ClearWindUp();
            player.actions.ActionDropPropOrPlayer();
        }

        private static void StartThrow(PlayerCharacter player)
        {
            if (player.hands == null || !player.hands.isHoldingSomething)
            {
                Plugin.Log.LogInfo($"{Tag} Big Throw: nothing in the hands to throw.");
                return;
            }

            _throwWindUpStart = NetworkTime.time;
            TrapIsThrowing = true;
            try { player.decisions.SetWindUpStartTime(_throwWindUpStart); }
            finally { TrapIsThrowing = false; }
            _throwPlayer = player;
            _cancelRefusedLogged = false;
            var windUp = player.tunings != null ? Mathf.Max(0f, player.tunings.maxWindUpDuration) : 1f;
            _throwAt = Time.unscaledTime + windUp + ThrowHoldSeconds;
        }

        // The game's cancel clears the wind-up, so it is put back every frame until the throw.
        private static void HoldWindUp()
        {
            var player = _throwPlayer;
            try
            {
                if (player == null || player.decisions == null || player.hands == null || !player.hands.isHoldingSomething)
                    return;

                if (player.decisions.localWindUpStartTime == _throwWindUpStart)
                    return;

                TrapIsThrowing = true;
                try { player.decisions.SetWindUpStartTime(_throwWindUpStart); }
                finally { TrapIsThrowing = false; }
                if (!_cancelRefusedLogged)
                {
                    _cancelRefusedLogged = true;
                    Plugin.Log.LogInfo($"{Tag} Big Throw: the wind-up was cancelled or changed; put back.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogInfo($"{Tag} Big Throw: could not hold the wind-up ({ex.Message}).");
            }
        }

        // Lets the trap's own throw through the Throwing lock (AbilityLockPatches).
        internal static bool TrapIsThrowing { get; private set; }

        private static void FinishThrow()
        {
            var player = _throwPlayer;
            _throwAt = -1f;
            _throwPlayer = null;
            TrapIsThrowing = true;

            try
            {
                if (player == null || player.hands == null)
                    return;

                if (!player.hands.isHoldingSomething)
                {
                    player.decisions?.ClearWindUp();
                    Plugin.Log.LogInfo($"{Tag} Big Throw: the hands were empty by the end of the wind-up.");
                    return;
                }

                player.actions.ActionDropPropOrPlayer();
                Plugin.Log.LogInfo($"{Tag} Big Throw: thrown.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Big Throw failed at the end of the wind-up: {ex.Message}");
            }
            finally
            {
                TrapIsThrowing = false;
            }
        }

        // All three write the same tunings from the base values, so none can leave them changed.
        private static void StartMovement(PlayerCharacter player, int seconds, string key)
        {
            var tunings = player.tunings;
            if (tunings == null)
            {
                Plugin.Log.LogInfo($"{Tag} {DisplayName(key)}: the player has no tunings.");
                return;
            }

            if (_moveTunings != null && _moveTunings.Pointer != tunings.Pointer)
                RestoreMovement();

            if (_moveTunings == null)
            {
                _moveTunings = tunings;
                _moveBase = new[]
                {
                    tunings.forwardSpeed, tunings.forwardSprintSpeed, tunings.crouchForwardSpeed,
                    tunings.crouchForwardSprintSpeed, tunings.swimForwardSpeed, tunings.swimForwardSprintSpeed,
                    tunings.forwardSprintGhostSpeed, tunings.jumpForce, tunings.maxUpwardsVelocity,
                    tunings.mouseLookSpeed, tunings.stickLookSpeed,
                };
            }

            var until = Time.unscaledTime + seconds;
            switch (key)
            {
                case Speed:
                    _speedUntil = until;
                    break;
                case Jump:
                    _jumpUntil = until;
                    break;
                case Sleep:
                    if (_sleepUntil < 0f)
                        _sleepStart = Time.unscaledTime;
                    _sleepUntil = until;
                    _sleeper = player;
                    break;
            }

            ApplyMovement();
        }

        private static void PlayPendingMovement(float now)
        {
            var player = LocalPlayer.Find();
            if (player == null)
                return;

            foreach (var pair in new List<KeyValuePair<string, float>>(PendingMovement))
            {
                var left = Mathf.CeilToInt(pair.Value - now);
                if (left > 0)
                {
                    StartMovement(player, left, pair.Key);
                    Plugin.Log.LogInfo($"{Tag} {DisplayName(pair.Key)} played now that the player is here ({left}s left).");
                }
            }

            PendingMovement.Clear();
        }

        private static void TickMovement(float now)
        {
            if (_speedUntil >= 0f && now >= _speedUntil)
            {
                _speedUntil = -1f;
                Plugin.Log.LogInfo($"{Tag} Big Speed over.");
            }

            if (_jumpUntil >= 0f && now >= _jumpUntil)
            {
                _jumpUntil = -1f;
                Plugin.Log.LogInfo($"{Tag} Big Jump over.");
            }

            if (_sleepUntil >= 0f && now >= _sleepUntil)
            {
                _sleepUntil = -1f;
                SetAsleep(false);
                Plugin.Log.LogInfo($"{Tag} Big Sleep over.");
            }

            if (_moveTunings == null)
                return;

            if (_speedUntil < 0f && _jumpUntil < 0f && _sleepUntil < 0f)
                RestoreMovement();
            else
                ApplyMovement();
        }

        private static void ApplyMovement()
        {
            var t = _moveTunings;
            var b = _moveBase;
            if (t == null || b == null)
                return;

            try
            {
                var frozen = _sleepUntil >= 0f
                             && (Time.unscaledTime - _sleepStart) % (AsleepSeconds + AwakeSeconds) < AsleepSeconds;
                SetAsleep(frozen);
                var move = frozen ? 0f : _speedUntil >= 0f ? SpeedFactor : 1f;
                var jump = frozen ? 0f : _jumpUntil >= 0f ? JumpFactor : 1f;
                t.forwardSpeed = b[0] * move;
                t.forwardSprintSpeed = b[1] * move;
                t.crouchForwardSpeed = b[2] * move;
                t.crouchForwardSprintSpeed = b[3] * move;
                t.swimForwardSpeed = b[4] * move;
                t.swimForwardSprintSpeed = b[5] * move;
                t.forwardSprintGhostSpeed = b[6] * move;
                t.jumpForce = b[7] * jump;
                t.maxUpwardsVelocity = frozen ? b[8] : b[8] * jump;

                // The camera is frozen too: looking round woke the walker up.
                t.mouseLookSpeed = frozen ? 0f : b[9];
                t.stickLookSpeed = frozen ? 0f : b[10];
            }
            catch (Exception ex)
            {
                // The player object went with its world.
                Plugin.Log.LogInfo($"{Tag} Movement effects dropped (the player is gone: {ex.Message}).");
                _moveTunings = null;
                _moveBase = null;
            }
        }

        private static bool _asleep;

        // Big Sleep has the walker asleep now; TrapPatches blocks crouching meanwhile.
        internal static bool Asleep => _asleep;

        private static void SetAsleep(bool asleep)
        {
            if (_asleep == asleep)
                return;

            _asleep = asleep;
            try
            {
                if (_sleeper != null && _sleeper.sleeper != null)
                {
                    _sleeper.sleeper.forceSleeping = asleep;

                    // Not fully: the game only animates when sleepiness changes between frames.
                    if (asleep)
                        _sleeper.sleeper.smoothSleepiness = Mathf.Max(_sleeper.sleeper.smoothSleepiness, 0.85f);
                    else
                        // forceSleeping zeroed the idle clock, which would keep the walker asleep.
                        _sleeper.sleeper.RecordAction();
                }
            }
            catch (Exception)
            {
                // The player went with its world.
            }

            if (!asleep && _sleepUntil < 0f)
                _sleeper = null;
        }

        private static void RestoreMovement()
        {
            var t = _moveTunings;
            var b = _moveBase;
            _moveTunings = null;
            _moveBase = null;
            if (t == null || b == null)
                return;

            try
            {
                t.forwardSpeed = b[0];
                t.forwardSprintSpeed = b[1];
                t.crouchForwardSpeed = b[2];
                t.crouchForwardSprintSpeed = b[3];
                t.swimForwardSpeed = b[4];
                t.swimForwardSprintSpeed = b[5];
                t.forwardSprintGhostSpeed = b[6];
                t.jumpForce = b[7];
                t.maxUpwardsVelocity = b[8];
                t.mouseLookSpeed = b[9];
                t.stickLookSpeed = b[10];
            }
            catch (Exception)
            {
                // The player object went with its world: nothing left to put back.
            }
        }

        // The clock fast-forwards to the hour, never backwards, then runs on; nothing is put back.
        private static void StartSky(float hour)
        {
            _skyFrom = SkyManager.GetCurrentTime();
            _skyTo = hour;
            _skySpan = Mathf.Repeat(hour - _skyFrom, 24f);
            _skyStart = Time.unscaledTime;
            _skyRunning = true;
            Plugin.Log.LogInfo($"{Tag} Sky: fast forward from {_skyFrom:F2}h to {hour:F2}h.");
        }

        private static void StepSky()
        {
            var t = Mathf.Clamp01((Time.unscaledTime - _skyStart) / FastForwardSeconds);
            try
            {
                // Eased, so the sun speeds up and settles rather than jerking.
                var eased = t * t * (3f - 2f * t);
                SkyManager.SetFixedTime(Mathf.Repeat(_skyFrom + _skySpan * eased, 24f));
                if (t < 1f)
                    return;

                _skyRunning = false;
                SkyManager.ClearFixedTime();
                Plugin.Log.LogInfo($"{Tag} Sky at {_skyTo:F2}h; the clock runs on from there.");
            }
            catch (Exception ex)
            {
                _skyRunning = false;
                Plugin.Log.LogInfo($"{Tag} Sky fast forward stopped (no sky now: {ex.Message}).");
            }
        }
    }
}
