using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    internal static partial class TrapEffects
    {
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
    }
}
