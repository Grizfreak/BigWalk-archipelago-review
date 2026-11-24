using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal static partial class TrapEffects
    {
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
    }
}
