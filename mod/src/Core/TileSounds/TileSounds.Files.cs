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
    internal static partial class TileSounds
    {
        private static void ScanLocal()
        {
            FileInfo[] files;
            try
            {
                var folder = new DirectoryInfo(CustomTiles.LocalFolder);
                if (!folder.Exists)
                    return;
                files = folder.GetFiles("*.wav");
            }
            catch (Exception)
            {
                return;
            }

            Array.Sort(files, (a, b) => string.CompareOrdinal(a.Name, b.Name));
            var signature = new StringBuilder();
            foreach (var file in files)
                signature.Append(file.Name).Append(':').Append(file.Length).Append(':').Append(file.LastWriteTimeUtc.Ticks).Append(';');
            if (signature.ToString() == _localSignature)
                return;
            _localSignature = signature.ToString();
            _guestFilesLogged = false;

            Local.Clear();
            foreach (var file in files)
            {
                var kind = KindIndex(Path.GetFileNameWithoutExtension(file.Name));
                if (kind < 0)
                {
                    Plugin.Log.LogWarning($"{Tag} '{file.Name}': only PegTileA1.wav to PegTileA9.wav have a sound; left out.");
                    continue;
                }
                try
                {
                    if (file.Length > MaxFileBytes)
                        throw new InvalidDataException($"more than {MaxFileBytes / (1024 * 1024)} MB");
                    using var stream = file.OpenRead();
                    var samples = WavReader.ReadMono(stream, out var frequency);
                    var pcm = Prepare(samples, frequency);
                    Local[kind] = new Sound { Pcm = pcm, Blob = Blob.Make(PcmBytes(pcm)) };
                    Plugin.Log.LogInfo($"{Tag} '{file.Name}' read: {samples.Length / (float)frequency:0.00} s at {frequency} Hz"
                                       + (samples.Length / (float)frequency > MaxSamples / (float)Rate ? ", cut at 28 s." : "."));
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} '{file.Name}' could not be read ({ex.Message}); that tile keeps its sound.");
                }
            }

            if (IsGuest && Local.Count > 0 && !_guestFilesLogged)
            {
                _guestFilesLogged = true;
                Plugin.Log.LogInfo($"{Tag} Your own tile sounds are not used in another player's world: everyone hears the host's.");
            }
        }

        // To 16 kHz mono 16-bit, cut at 28 s, with 5 ms fades so the repeats do not click.
        private static short[] Prepare(float[] samples, int frequency)
        {
            var length = (int)Math.Min(MaxSamples, (long)samples.Length * Rate / frequency);
            var pcm = new short[length];
            var fade = Rate / 200;
            for (var i = 0; i < length; i++)
            {
                var at = (double)i * frequency / Rate;
                var index = (int)at;
                var next = Math.Min(index + 1, samples.Length - 1);
                var value = samples[index] + (samples[next] - samples[index]) * (float)(at - index);
                var gain = Math.Min(1f, Math.Min(i, length - 1 - i) / (float)fade);
                pcm[i] = (short)Math.Clamp(value * gain * 32767f, short.MinValue, short.MaxValue);
            }
            return pcm;
        }

        private static byte[] PcmBytes(short[] pcm)
        {
            var bytes = new byte[pcm.Length * 2];
            Buffer.BlockCopy(pcm, 0, bytes, 0, bytes.Length);
            return bytes;
        }
    }
}
