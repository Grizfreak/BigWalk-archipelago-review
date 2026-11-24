using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Item and check sounds, the game's own clips. One per burst, so forty items are one sound.
    internal static class ApSounds
    {
        // Both can be changed in the .cfg.
        private const string ReceivedDefault = "sfx_prop_cowbell_small_clang_random";
        private const string CheckDefault = "sfx_gourd_plinth_open_end_random";

        private static string ReceivedClip => Clip(ModConfig.ReceivedSound.Value, ReceivedDefault);
        private static string CheckClip => Clip(ModConfig.CheckSound.Value, CheckDefault);

        private static string Clip(string configured, string fallback) =>
            string.IsNullOrWhiteSpace(configured) ? fallback : configured.Trim();

        private const float BurstSeconds = 15f;

        private static readonly Dictionary<string, AudioAsset> Found = new();
        private static float _lastReceived = -100f;
        private static float _lastCheck = -100f;

        // For the debug key: the next sound plays whatever just played.
        internal static void ForgetBursts()
        {
            _lastReceived = -100f;
            _lastCheck = -100f;
            _lastDeathLinkSound = -100f;
        }

        internal static bool PlayNamed(string clip)
        {
            var player = LocalPlayer.Find();
            return player != null && PlayNamedAt(clip, player.transform.position);
        }

        // Not held to the sounds' setting: it is an effect's own sound.
        internal static bool PlayNamedAt(string clip, Vector3 position)
        {
            try
            {
                var asset = Find(clip);
                if (asset == null)
                    return false;
                AudioPlayHelper.Play(asset, position);
                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApSounds)}] {clip} did not play: {ex.Message}");
                return false;
            }
        }

        // Not held to a burst: each is rare.
        internal static void Confetti()
        {
            Counts[2]++;
            PlayAlways("sfx_challengeWin_confetti_random");
        }

        private static float _lastDeathLinkSound = -100f;

        internal static void DeathLinkAlert()
        {
            Counts[3]++;
            if (Time.unscaledTime - _lastDeathLinkSound < 2f)
                return;

            _lastDeathLinkSound = Time.unscaledTime;
            PlayAlways("sfx_prop_cowbell_large_clang_random");
        }

        private static void PlayAlways(string clip)
        {
            if (!ModConfig.ApSounds.Value)
            {
                Plugin.Log.LogInfo($"[{nameof(ApSounds)}] {clip} not played: the sounds are off in the settings.");
                return;
            }

            var played = PlayNamed(clip);
            Plugin.Log.LogInfo($"[{nameof(ApSounds)}] {clip}: {(played ? "played" : "NOT played")}.");
        }

        // Not held to a burst: four ticks, a second apart.
        private const string CountdownClip = "sfx_cabinFever_timer_run_random";

        internal static void CountdownTick()
        {
            Counts[4]++;
            PlayAlways(CountdownClip);
            Net.ModChannel.SendSnapshotNow();
        }

        // Counted per kind for the snapshot; a guest plays a sound when a count moves.
        internal static readonly byte[] Counts = new byte[5];

        internal static void PlayForGuest(int kind)
        {
            switch (kind)
            {
                case 0:
                    Play(ReceivedClip, ref _lastReceived);
                    break;
                case 1:
                    Play(CheckClip, ref _lastCheck);
                    break;
                case 2:
                    PlayAlways("sfx_challengeWin_confetti_random");
                    break;
                case 4:
                    PlayAlways(CountdownClip);
                    break;
                default:
                    if (Time.unscaledTime - _lastDeathLinkSound >= 2f)
                    {
                        _lastDeathLinkSound = Time.unscaledTime;
                        PlayAlways("sfx_prop_cowbell_large_clang_random");
                    }

                    break;
            }
        }

        internal static void Received()
        {
            Counts[0]++;
            Play(ReceivedClip, ref _lastReceived);
        }

        // A puzzle's own check already plays a sound; only other checks call this.
        internal static void CheckSent()
        {
            Counts[1]++;
            Play(CheckClip, ref _lastCheck);
        }

        private static void Play(string clip, ref float last)
        {
            if (!ModConfig.ApSounds.Value || Time.unscaledTime - last < BurstSeconds)
                return;

            try
            {
                var player = LocalPlayer.Find();
                var asset = Find(clip);
                if (player == null || asset == null)
                    return;

                last = Time.unscaledTime;
                AudioPlayHelper.Play(asset, player.transform.position);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApSounds)}] {clip} did not play: {ex.Message}");
            }
        }

        private static AudioAsset Find(string clip)
        {
            if (Found.TryGetValue(clip, out var known) && known != null)
                return known;

            foreach (var asset in Resources.FindObjectsOfTypeAll<AudioAsset>())
            {
                if (asset != null && asset.name == clip)
                {
                    Found[clip] = asset;
                    return asset;
                }
            }

            Plugin.Log.LogWarning($"[{nameof(ApSounds)}] No audio asset called '{clip}' in memory.");
            return null;
        }
    }
}
