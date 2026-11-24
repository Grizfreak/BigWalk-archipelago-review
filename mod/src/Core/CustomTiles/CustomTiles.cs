using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BigWalkArchipelago.Core.Net;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Shows the seed's drawings on peg tiles by pointing tile kinds at a mod atlas's cells.
    internal static partial class CustomTiles
    {
        private const string Tag = "[" + nameof(CustomTiles) + "]";

        // PegTileA1 to PegTileA9, by the apworld's names (tiles.VANILLA_TILES).
        private static readonly string[] Vanilla =
            { "triangle", "asterisk", "diagonal_line", "G", "dot_and_line", "vertical_ellipsis", "S", "spiral", "e" };

        // Cells of 512 on a square power-of-two grid, filled from the top left as the game does.
        private const int Cell = 512;

        private static string[] _seed = Array.Empty<string>();
        private static string[] _mirrored;
        private static string _applied = string.Empty;

        private static PegTileDataSet _set;
        private static PegTileDataSet.TileIndexData[] _original;
        private static int _textureIndex = -1;
        private static Material _material;
        private static Texture2D _atlas;
        private static PegTileRenderer _probe;
        private static bool _newWorld;
        private static readonly Dictionary<string, byte[]> Drawings = new(StringComparer.OrdinalIgnoreCase);

        // In the player's folder, "@<tile kind>" names a kind's own file.
        private const string KindPrefix = "@";
        private const long MaxFileBytes = 16 * 1024 * 1024;
        private static readonly Dictionary<string, string> LocalFiles = new(StringComparer.OrdinalIgnoreCase);
        private static readonly List<string> LocalKinds = new();
        private static string _localSignature = string.Empty;
        private static bool _folderFailed;

        // Only icons and scribbles: puzzles read the poses and the points.
        private static readonly Regex ReplaceableKind = new("^PegTile(A[1-9]|C[1-9]|Priest[A-D][1-9])$");

        private static readonly BlobChannel Channel = new(1, Tag, 1024 * 1024, Cell * Cell);
        private static Dictionary<string, Blob> _hostBlobs = new();
        private static string _hostBlobsSignature;
        private static Dictionary<string, ulong> _hostDrawings;
        private static readonly Dictionary<string, (ulong Hash, byte[] Ink)> Received = new(StringComparer.OrdinalIgnoreCase);
        private static bool _guestFilesLogged;

        internal static string LocalFolder => Path.Combine(BepInEx.Paths.ConfigPath, "BigWalkArchipelago", "tiles");

        internal static Texture2D Atlas => _atlas;

        internal static void Configure(ApSlotData slot)
        {
            var wanted = new List<string>();
            if (slot.PegTiles.Length == Vanilla.Length)
            {
                for (var place = 0; place < Vanilla.Length; place++)
                {
                    if (slot.PegTiles[place] != Vanilla[place])
                        wanted.Add($"PegTileA{place + 1}={slot.PegTiles[place]}");
                }
            }
            wanted.AddRange(slot.PegDrawings);
            _seed = wanted.ToArray();
            Plugin.Log.LogInfo(_seed.Length == 0
                ? $"{Tag} The game's tiles."
                : $"{Tag} Tiles shown as: {string.Join(", ", _seed)}.");
        }

        private static bool IsGuest => NetworkClient.active && !NetworkServer.active;

        internal static string[] HostTiles => ApRuntime.ShufflesPegTiles ? _seed : Array.Empty<string>();

        internal static void ApplyFromHost(List<string> tiles) => _mirrored = tiles.ToArray();

        internal static void ForgetMirror()
        {
            _mirrored = null;
            _hostDrawings = null;
            Received.Clear();
            Drawings.Clear();
            Channel.Forget();
        }

        internal static void Tick()
        {
            // Unity null with a live reference: the tile is gone, so this is another world.
            if (!ReferenceEquals(_probe, null) && _probe == null)
                ForgetSet();

            ScanLocal();
            var guest = IsGuest;
            var inWorld = guest ? _mirrored != null : Net.ApRuntime.ShufflesPegTiles;
            var seed = guest ? _mirrored ?? Array.Empty<string>() : HostTiles;
            // A guest shows the host's own kinds, never its own folder's.
            var overridden = guest ? HostKinds() : LocalKinds;
            if (guest && LocalFiles.Count > 0 && !_guestFilesLogged)
            {
                _guestFilesLogged = true;
                Plugin.Log.LogInfo($"{Tag} Your own drawings are not used in another player's world: everyone sees the host's.");
            }
            var wanted = new List<string>();
            foreach (var entry in seed)
            {
                var equals = entry.IndexOf('=');
                if (!inWorld || equals <= 0 || !overridden.Contains(entry.Substring(0, equals)))
                    wanted.Add(entry);
            }
            if (inWorld)
            {
                foreach (var kind in overridden)
                    wanted.Add($"{kind}={KindPrefix}{kind}");
            }
            wanted.Sort(StringComparer.Ordinal);
            var key = string.Join(",", wanted) + "|" + (guest ? ReceivedSignature() : _localSignature);
            if (key == _applied)
                return;

            try
            {
                if (Apply(wanted))
                    _applied = key;
            }
            catch (Exception ex)
            {
                // Once: the game's tiles stay, and the same draw is not tried again.
                _applied = key;
                Plugin.Log.LogWarning($"{Tag} Could not show the custom tiles, the game's stay: {ex}");
            }
        }

        private static void ForgetSet()
        {
            _probe = null;
            _applied = null;
            _newWorld = true;
            var sets = Resources.FindObjectsOfTypeAll<PegTileDataSet>();
            if (_set != null && sets.Length > 0 && sets[0].Pointer == _set.Pointer)
                return;

            Plugin.Log.LogInfo($"{Tag} A new tile data set came with this world; the drawings go on it again.");
            _set = null;
            _original = null;
            _textureIndex = -1;
            if (_material != null)
                UnityEngine.Object.Destroy(_material);
            _material = null;
        }

        private static List<string> HostKinds()
        {
            var kinds = new List<string>();
            if (_hostDrawings == null)
                return kinds;
            foreach (var name in _hostDrawings.Keys)
            {
                if (name.StartsWith(KindPrefix, StringComparison.Ordinal) && ReplaceableKind.IsMatch(name.Substring(1)))
                    kinds.Add(name.Substring(1));
            }
            return kinds;
        }
    }
}
