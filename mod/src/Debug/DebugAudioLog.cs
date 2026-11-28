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

        // Applied by Plugin.Load only with Debug.Enabled, never by PatchAll.
        internal static void TryApply(Harmony harmony)
        {
            try
            {
                harmony.Patch(AccessTools.Method(typeof(AudioPlayHelper), nameof(AudioPlayHelper.Play)),
                    prefix: new HarmonyMethod(typeof(DebugAudioLog), nameof(PlayPrefix)));
                harmony.Patch(AccessTools.Method(typeof(AudioPlayHelper), nameof(AudioPlayHelper.CreateEventAndPlay)),
                    prefix: new HarmonyMethod(typeof(DebugAudioLog), nameof(CreateEventPrefix)));
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(DebugAudioLog)}] Sounds not logged: {ex.Message}");
            }
        }

        private static void PlayPrefix(AudioAsset audioAsset, Vector3 worldPosition, UnityEngine.Object owner) =>
            Write("Play", audioAsset, worldPosition, owner);

        private static void CreateEventPrefix(AudioAsset audioAsset, Vector3 worldPosition, UnityEngine.Object owner) =>
            Write("CreateEventAndPlay", audioAsset, worldPosition, owner);

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
}
