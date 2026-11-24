using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BigWalkArchipelago.Core.Net;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // PegTileA<n>.wav replaces stem n-1 of the sound tiles' music; guests get the host's samples.
    internal static partial class TileSounds
    {
        private const string Tag = "[" + nameof(TileSounds) + "]";
        internal const int Rate = 16000;
        private const float DefaultSeconds = 27.83f;
        // A stem lasts 27.83 s: anything past 28 s is never played, so never kept nor accepted.
        internal const int MaxSamples = Rate * 28;
        private const long MaxFileBytes = 32L * 1024 * 1024;

        private static readonly BlobChannel Channel = new(0, Tag, 1024 * 1024, MaxSamples * 2);

        private sealed class Sound
        {
            internal short[] Pcm;
            internal Blob Blob;
            internal ulong Hash => Blob.Hash;
        }

        // The host's own files, by tile kind index (0 = A1).
        private static readonly Dictionary<int, Sound> Local = new();
        private static string _localSignature = string.Empty;
        private static bool _guestFilesLogged;

        // A guest: what the host says each kind plays (its hash), and the sounds received so far.
        private static Dictionary<int, ulong> _manifest;
        private static readonly Dictionary<int, Sound> Received = new();

        // The game's clips, by cue, to put back; and what each kind plays now.
        private static readonly Dictionary<IntPtr, AudioClip> Originals = new();
        private static readonly Dictionary<IntPtr, AudioClip> Ours = new();
        private static readonly Dictionary<int, ulong> Applied = new();

        private static bool IsGuest => NetworkClient.active && !NetworkServer.active;

        // What the host's snapshot carries: each replaced kind and its sound's hash.
        internal static Dictionary<int, ulong> HostManifest()
        {
            var manifest = new Dictionary<int, ulong>();
            if (!ApRuntime.ShufflesPegTiles)
                return manifest;
            foreach (var pair in Local)
                manifest[pair.Key] = pair.Value.Hash;
            return manifest;
        }

        internal static void ApplyManifestFromHost(Dictionary<int, ulong> manifest)
        {
            _manifest = manifest;
            foreach (var kind in new List<int>(Received.Keys))
            {
                if (!manifest.TryGetValue(kind, out var hash) || hash != Received[kind].Hash)
                    Received.Remove(kind);
            }
        }

        internal static void ForgetMirror()
        {
            _manifest = null;
            Received.Clear();
            Channel.Forget();
        }

        internal static void Tick()
        {
            ScanLocal();
            var wanted = new Dictionary<int, Sound>();
            if (IsGuest)
            {
                if (_manifest != null)
                {
                    foreach (var pair in _manifest)
                    {
                        if (Received.TryGetValue(pair.Key, out var sound) && sound.Hash == pair.Value)
                            wanted[pair.Key] = sound;
                    }
                }
            }
            else if (ApRuntime.ShufflesPegTiles)
            {
                foreach (var pair in Local)
                    wanted[pair.Key] = pair.Value;
            }

            try
            {
                Apply(wanted);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Could not change the tile sounds, the game's stay: {ex.Message}");
            }
        }

        // Every host frame (ModChannel): the host's sounds to its guests, a few chunks at a time.
        internal static void PumpHost(IEnumerable<NetworkConnection> guests)
        {
            var blobs = new Dictionary<string, Blob>();
            if (ApRuntime.ShufflesPegTiles)
            {
                foreach (var pair in Local)
                    blobs[$"PegTileA{pair.Key + 1}"] = pair.Value.Blob;
            }
            Channel.Pump(guests, blobs);
        }

        // Runs in ModChannel's handler: nothing may throw, so every check returns quietly.
        internal static void ReceiveChunk(string key, ulong hash, int total, int offset, byte[] data)
        {
            var kind = KindIndex(key);
            if (kind < 0)
                return;
            var raw = Channel.Receive(key, hash, total, offset, data);
            if (raw == null)
                return;
            if (raw.Length % 2 != 0)
            {
                Plugin.Log.LogWarning($"{Tag} The host's sound for {key} was refused (odd length).");
                return;
            }
            var pcm = new short[raw.Length / 2];
            Buffer.BlockCopy(raw, 0, pcm, 0, raw.Length);
            Received[kind] = new Sound { Pcm = pcm, Blob = new Blob { Hash = hash } };
            Plugin.Log.LogInfo($"{Tag} The host's sound for {key} received ({pcm.Length / (float)Rate:0.00} s, {total / 1024} KB).");
        }

        private static int KindIndex(string name)
        {
            if (name.Length == 9 && name.StartsWith("PegTileA", StringComparison.OrdinalIgnoreCase)
                && name[8] >= '1' && name[8] <= '9')
                return name[8] - '1';
            return -1;
        }

        private static IEnumerable<string> Names(IEnumerable<int> kinds)
        {
            foreach (var kind in kinds)
                yield return $"PegTileA{kind + 1}";
        }
    }
}
