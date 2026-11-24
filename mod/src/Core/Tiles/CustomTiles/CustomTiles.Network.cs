using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Mirror;

namespace BigWalkArchipelago.Core.Tiles
{
    internal static partial class CustomTiles
    {
        internal static Dictionary<string, ulong> HostManifest()
        {
            var manifest = new Dictionary<string, ulong>();
            foreach (var pair in HostBlobs())
                manifest[pair.Key] = pair.Value.Hash;
            return manifest;
        }

        internal static void PumpHost(IEnumerable<NetworkConnection> guests) => Channel.Pump(guests, HostBlobs());

        private static Dictionary<string, Blob> HostBlobs()
        {
            if (IsGuest || !ApRuntime.ShufflesPegTiles)
                return new Dictionary<string, Blob>();
            if (_hostBlobsSignature == _localSignature)
                return _hostBlobs;

            _hostBlobsSignature = _localSignature;
            _hostBlobs = new Dictionary<string, Blob>();
            foreach (var name in LocalFiles.Keys)
            {
                var pixels = Drawing(name);
                if (pixels == null)
                    continue;
                var ink = new byte[Cell * Cell];
                for (var i = 0; i < ink.Length; i++)
                    ink[i] = pixels[i * 4 + 3];
                _hostBlobs[name] = Blob.Make(ink);
            }
            return _hostBlobs;
        }

        internal static void ApplyDrawingsFromHost(Dictionary<string, ulong> drawings)
        {
            _hostDrawings = drawings;
            foreach (var name in new List<string>(Received.Keys))
            {
                if (!drawings.TryGetValue(name, out var hash) || hash != Received[name].Hash)
                {
                    Received.Remove(name);
                    Drawings.Remove(name);
                }
            }
        }

        // Runs in ModChannel's handler: nothing may throw, so every check returns quietly.
        internal static void ReceiveChunk(string name, ulong hash, int total, int offset, byte[] data)
        {
            var ink = Channel.Receive(name, hash, total, offset, data);
            if (ink == null)
                return;
            if (ink.Length != Cell * Cell)
            {
                Plugin.Log.LogWarning($"{Tag} The host's drawing '{name}' was refused (not {Cell} x {Cell}).");
                return;
            }
            Received[name] = (hash, ink);
            Drawings.Remove(name);
            Plugin.Log.LogInfo($"{Tag} The host's drawing '{name}' received ({total / 1024} KB).");
        }

        private static string ReceivedSignature()
        {
            var parts = new List<string>();
            foreach (var pair in Received)
                parts.Add($"{pair.Key}:{pair.Value.Hash}");
            parts.Sort(StringComparer.Ordinal);
            return string.Join(";", parts);
        }
    }
}
