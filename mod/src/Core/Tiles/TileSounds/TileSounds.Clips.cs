using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BigWalkArchipelago.Core.Net;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Tiles
{
    internal static partial class TileSounds
    {
        private static void Apply(Dictionary<int, Sound> wanted)
        {
            var same = wanted.Count == Applied.Count && AllOursLoaded();
            foreach (var pair in wanted)
                same &= Applied.TryGetValue(pair.Key, out var hash) && hash == pair.Value.Hash;
            if (same)
                return;

            var changed = 0;
            var done = new HashSet<IntPtr>();
            var swaps = new Dictionary<IntPtr, AudioClip>();
            var dropped = new List<AudioClip>();
            foreach (var set in Resources.FindObjectsOfTypeAll<PegTileMusicSet>())
            {
                var group = set?.musicGroup;
                if (group?.SpeakerStems == null || set.mappings == null)
                    continue;
                foreach (var pair in set.mappings)
                {
                    var kind = KindIndex(pair.propGroup.ToString());
                    if (kind < 0 || pair.Index < 0 || pair.Index >= group.SpeakerStems.Length)
                        continue;
                    var cue = group.SpeakerStems[pair.Index];
                    if (cue == null || !done.Add(cue.Pointer))
                        continue;
                    if (!Originals.ContainsKey(cue.Pointer))
                        Originals[cue.Pointer] = cue._clip;

                    var original = Originals[cue.Pointer];
                    var target = wanted.TryGetValue(kind, out var sound) ? sound : null;
                    var current = Ours.TryGetValue(cue.Pointer, out var ours) ? ours : null;
                    if (target == null)
                    {
                        if (current == null)
                            continue;
                        cue._clip = original;
                        Ours.Remove(cue.Pointer);
                        swaps[current.Pointer] = original;
                        dropped.Add(current);
                    }
                    else
                    {
                        var seconds = original != null && original.frequency > 0 ? original.samples / (float)original.frequency : DefaultSeconds;
                        var clip = BuildClip($"PegTileA{kind + 1} (Archipelago)", target.Pcm, seconds);
                        cue._clip = clip;
                        Ours[cue.Pointer] = clip;
                        if (original != null)
                            swaps[original.Pointer] = clip;
                        if (current != null)
                        {
                            swaps[current.Pointer] = clip;
                            dropped.Add(current);
                        }
                    }
                    changed++;
                }
            }

            var players = Repoint(swaps);
            foreach (var clip in dropped)
            {
                Unpin(clip);
                UnityEngine.Object.Destroy(clip);
            }

            Applied.Clear();
            foreach (var pair in wanted)
                Applied[pair.Key] = pair.Value.Hash;
            Plugin.Log.LogInfo(wanted.Count == 0
                ? $"{Tag} The game's tile sounds back ({changed} stem(s), {players} speaker(s) switched)."
                : $"{Tag} Tile sounds replaced: {string.Join(", ", Names(wanted.Keys))} ({changed} stem(s), {players} speaker(s) switched).");
        }

        // Speakers loop their stem and are only muted: a live one gets the clip at its play head.
        private static int Repoint(Dictionary<IntPtr, AudioClip> swaps)
        {
            if (swaps.Count == 0)
                return 0;
            var switched = 0;
            foreach (var player in UnityEngine.Object.FindObjectsByType<MusicPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                try
                {
                    var controller = player?.ASC;
                    var held = controller?._clip;
                    if (held == null || !swaps.TryGetValue(held.Pointer, out var replacement) || replacement == null)
                        continue;
                    var source = controller._audioSource;
                    var playing = source != null && source.isPlaying;
                    var time = source != null ? source.time : 0f;
                    controller._clip = replacement;
                    if (source != null)
                    {
                        source.clip = replacement;
                        source.time = Mathf.Clamp(time, 0f, Math.Max(0f, replacement.length - 0.05f));
                        if (playing)
                            source.Play();
                    }
                    switched++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} A speaker kept its sound: {ex.Message}");
                }
            }
            return switched;
        }

        // The sound repeated to the stem's length, then silence: a stem loops on its length.
        private static AudioClip BuildClip(string name, short[] pcm, float seconds)
        {
            var length = Math.Max(1, (int)Math.Round(seconds * Rate));
            var samples = new float[length];
            var copies = pcm.Length == 0 ? 0 : Math.Max(1, length / pcm.Length);
            for (var copy = 0; copy < copies; copy++)
            {
                var start = copy * pcm.Length;
                for (var i = 0; i < pcm.Length && start + i < length; i++)
                    samples[start + i] = pcm[i] / 32768f;
            }

            var clip = AudioClip.Create(name, length, 1, Rate, false);
            clip.SetData((Il2CppStructArray<float>)samples, 0);
            clip.hideFlags = HideFlags.DontUnloadUnusedAsset;
            Pin(clip);
            return clip;
        }

        // The game unloads a clip left idle that no sound bank holds; a built clip cannot reload.
        private static void Pin(AudioClip clip)
        {
            try
            {
                var references = AudioManager.Instance?.AudioReferenceManager;
                if (references == null)
                    Plugin.Log.LogWarning($"{Tag} No audio manager yet: '{clip.name}' may get unloaded.");
                references?.IncreaseClipSoundBankCount(clip);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} A tile sound could not be kept loaded: {ex.Message}");
            }
        }

        private static void Unpin(AudioClip clip)
        {
            try
            {
                AudioManager.Instance?.AudioReferenceManager?.DecreaseClipSoundBankCount(clip);
            }
            catch (Exception)
            {
                // The clip is destroyed right after: a stale count only keeps a dead key.
            }
        }

        // Should one get unloaded anyway, the next tick builds it again.
        private static bool AllOursLoaded()
        {
            foreach (var clip in Ours.Values)
            {
                if (clip == null || clip.loadState != AudioDataLoadState.Loaded)
                {
                    Plugin.Log.LogInfo($"{Tag} A tile sound was unloaded by the game; built again.");
                    return false;
                }
            }
            return true;
        }
    }
}
