using HarmonyLib;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Logs every sound the game plays to BepInEx/audio-played.tsv, each asset once per half second.
    internal static class DebugAudioLog
    {
        // Set while the cycling keys play a clip, so the test is not logged as a game sound.
        internal static bool Mute;

        private static readonly Dictionary<string, float> Last = new();
        private static string _file;

        internal static void Write(string source, AudioAsset asset, Vector3 position, UnityEngine.Object owner)
        {
            if (Mute || asset == null || !ModConfig.DebugModeEnabled.Value)
                return;
            try
            {
                DebugAudioProbe.Remember(asset);
                var now = Time.unscaledTime;
                if (Last.TryGetValue(asset.name, out var before) && now - before < 0.5f)
                    return;
                Last[asset.name] = now;

                _file ??= Path.Combine(BepInEx.Paths.BepInExRootPath, "audio-played.tsv");
                var who = owner != null ? owner.name : "";
                File.AppendAllText(_file,
                    $"{now:0.0}\t{source}\t{asset.name}\t{who}\t{position.x:0.#},{position.y:0.#},{position.z:0.#}\n");
            }
            catch (System.Exception)
            {
                // A diagnostic: never let it break a sound.
            }
        }
    }

    [HarmonyPatch(typeof(AudioPlayHelper), nameof(AudioPlayHelper.Play))]
    internal static class DebugAudioPlayPatch
    {
        private static void Prefix(AudioAsset audioAsset, Vector3 worldPosition, UnityEngine.Object owner) =>
            DebugAudioLog.Write("Play", audioAsset, worldPosition, owner);
    }

    [HarmonyPatch(typeof(AudioPlayHelper), nameof(AudioPlayHelper.CreateEventAndPlay))]
    internal static class DebugAudioCreateEventPatch
    {
        private static void Prefix(AudioAsset audioAsset, Vector3 worldPosition, UnityEngine.Object owner) =>
            DebugAudioLog.Write("CreateEventAndPlay", audioAsset, worldPosition, owner);
    }
}
