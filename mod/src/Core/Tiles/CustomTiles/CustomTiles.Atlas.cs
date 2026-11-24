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

namespace BigWalkArchipelago.Core.Tiles
{
    internal static partial class CustomTiles
    {
        private static bool Apply(List<string> wanted)
        {
            if (_set == null)
            {
                var sets = Resources.FindObjectsOfTypeAll<PegTileDataSet>();
                if (sets.Length == 0)
                    return false;
                _set = sets[0];
                if (sets.Length > 1)
                    Plugin.Log.LogWarning($"{Tag} {sets.Length} tile data sets; only '{_set.name}' is changed.");
                var cells = _set.indexDatas;
                _original = new PegTileDataSet.TileIndexData[cells.Length];
                for (var i = 0; i < cells.Length; i++)
                    _original[i] = cells[i];
            }

            var kinds = new Dictionary<PropGroup, int>();
            var shown = new List<string>();
            foreach (var entry in wanted)
            {
                var equals = entry.IndexOf('=');
                if (equals <= 0 || !Enum.TryParse<PropGroup>(entry.Substring(0, equals), out var kind)
                    || kinds.ContainsKey(kind))
                    continue;
                var name = entry.Substring(equals + 1);
                if (Drawing(name) == null)
                    continue;
                var cell = shown.IndexOf(name);
                if (cell < 0)
                {
                    cell = shown.Count;
                    shown.Add(name);
                }
                kinds[kind] = cell;
            }

            var grid = 1;
            while (grid * grid < shown.Count)
                grid *= 2;
            if (shown.Count > 0)
                BuildAtlas(shown, grid);

            var renderers = new List<PegTileRenderer>();
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<PegTileRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer != null && renderer.pegTileDataSet != null && renderer.pegTileDataSet.Pointer == _set.Pointer)
                    renderers.Add(renderer);
            }

            var datas = _set.indexDatas;

            // Tiles born on our cells keep their mesh's material: the game's cells make them swap.
            if (_newWorld)
            {
                _newWorld = false;
                for (var i = 0; i < datas.Length && i < _original.Length; i++)
                    datas[i] = _original[i];
                foreach (var renderer in renderers)
                    renderer.Refresh();
            }

            for (var i = 0; i < datas.Length && i < _original.Length; i++)
            {
                var cell = _original[i];
                if (kinds.TryGetValue(cell.propGroup, out var at))
                {
                    cell.textureIndex = _textureIndex;
                    cell.xIndex = at % grid;
                    cell.yIndex = at / grid;
                }
                datas[i] = cell;
            }

            foreach (var renderer in renderers)
                renderer.Refresh();
            var refreshed = renderers.Count;
            if (refreshed > 0)
                _probe = renderers[0];

            Plugin.Log.LogInfo(kinds.Count > 0
                ? $"{Tag} {kinds.Count} tile kind(s) shown as {shown.Count} drawing(s) ({string.Join(", ", shown)}); {refreshed} tile(s) refreshed."
                : $"{Tag} The game's tiles back; {refreshed} tile(s) refreshed.");
            return true;
        }

        private static byte[] Drawing(string name)
        {
            if (Drawings.TryGetValue(name, out var known))
                return known;

            if (name.StartsWith(MirrorPrefix, StringComparison.Ordinal))
                return Drawings[name] = Mirrored(name.Substring(MirrorPrefix.Length));

            byte[] pixels = null;
            try
            {
                if (IsGuest)
                {
                    if (Received.TryGetValue(name, out var received))
                        pixels = InkToPixels(received.Ink);
                    else if (!name.StartsWith(KindPrefix, StringComparison.Ordinal))
                        pixels = FromDll(name);
                }
                else if (LocalFiles.TryGetValue(name, out var path))
                {
                    if (new FileInfo(path).Length > MaxFileBytes)
                        throw new InvalidDataException($"more than {MaxFileBytes / (1024 * 1024)} MB");
                    using var file = File.OpenRead(path);
                    var rgba = PngReader.ReadRgba(file, out var width, out var height);
                    pixels = ToDrawing(rgba, width, height, Path.GetFileName(path));
                    Plugin.Log.LogInfo($"{Tag} '{Path.GetFileName(path)}' read from the tiles folder.");
                }
                else if (!name.StartsWith(KindPrefix, StringComparison.Ordinal))
                    pixels = FromDll(name);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} The drawing '{name}' could not be read ({ex.Message}); that tile stays as it is.");
                pixels = null;
            }

            Drawings[name] = pixels;
            return pixels;
        }

        private static byte[] FromDll(string name)
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("tiles." + name + ".png");
            if (stream == null)
            {
                Plugin.Log.LogWarning($"{Tag} No drawing '{name}' in this build nor in the tiles folder; that tile stays as it is.");
                return null;
            }
            var rgba = PngReader.ReadRgba(stream, out var width, out var height);
            return ToDrawing(rgba, width, height, name);
        }

        private static byte[] InkToPixels(byte[] ink)
        {
            var pixels = new byte[Cell * Cell * 4];
            for (var i = 0; i < ink.Length; i++)
            {
                pixels[i * 4] = 255;
                pixels[i * 4 + 1] = 255;
                pixels[i * 4 + 2] = 255;
                pixels[i * 4 + 3] = ink[i];
            }
            return pixels;
        }

        private static void BuildAtlas(List<string> shown, int grid)
        {
            // Past 16 drawings, half-size cells: a 4096 atlas with its mips costs hundreds of MB.
            var cell = grid > 4 ? Cell / 2 : Cell;
            var size = cell * grid;
            var top = new byte[size * size * 4];
            for (var index = 0; index < shown.Count; index++)
            {
                var pixels = Drawing(shown[index]);
                if (cell != Cell)
                    pixels = HalfSize(pixels, Cell);
                var cellX = index % grid;
                var cellY = index / grid;
                for (var row = 0; row < cell; row++)
                {
                    var textureRow = size - 1 - (cellY * cell + row);
                    Buffer.BlockCopy(pixels, row * cell * 4, top, (textureRow * size + cellX * cell) * 4, cell * 4);
                }
            }

            // The shader reads the colour, not only the alpha, so colour = ink alpha.
            for (var i = 0; i < top.Length; i += 4)
            {
                var ink = top[i + 3];
                top[i] = ink;
                top[i + 1] = ink;
                top[i + 2] = ink;
            }

            var chain = new List<byte[]> { top };
            var side = size;
            while (side > 1)
            {
                chain.Add(HalfSize(chain[chain.Count - 1], side));
                side /= 2;
            }
            var total = 0;
            foreach (var level in chain)
                total += level.Length;
            var raw = new byte[total];
            var at = 0;
            foreach (var level in chain)
            {
                Buffer.BlockCopy(level, 0, raw, at, level.Length);
                at += level.Length;
            }

            // One texture for good: tiles copy the material, a new texture left them on a dead one.
            var atlas = _atlas;
            if (atlas == null)
            {
                atlas = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = "pegtileatlas_archipelago" };
                var game = _set.materialPerTextureIndex[0]?.mainTexture;
                if (game != null)
                {
                    atlas.wrapMode = game.wrapMode;
                    atlas.filterMode = game.filterMode;
                    atlas.anisoLevel = game.anisoLevel;
                }
                UnityEngine.Object.DontDestroyOnLoad(atlas);
            }
            else if (atlas.width != size)
                atlas.Reinitialize(size, size, TextureFormat.RGBA32, true);
            atlas.LoadRawTextureData((Il2CppStructArray<byte>)raw);
            // Kept readable: it is filled again in place at the next change.
            atlas.Apply(false, false);

            // Unity null too: a destroyed material is made again, in the same slot.
            if (_material == null)
            {
                _material = new Material(_set.materialPerTextureIndex[0]) { name = "PegTile Archipelago" };
                UnityEngine.Object.DontDestroyOnLoad(_material);
            }

            if (_textureIndex >= 0)
            {
                _set.materialPerTextureIndex[_textureIndex] = _material;
            }
            else
            {
                var materials = _set.materialPerTextureIndex;
                var moreMaterials = new Il2CppReferenceArray<Material>(materials.Length + 1);
                for (var i = 0; i < materials.Length; i++)
                    moreMaterials[i] = materials[i];
                moreMaterials[materials.Length] = _material;
                _set.materialPerTextureIndex = moreMaterials;

                var sizes = _set.sizePerTextureIndex;
                var moreSizes = new Il2CppStructArray<PegTileDataSet.Int2>(sizes.Length + 1);
                for (var i = 0; i < sizes.Length; i++)
                    moreSizes[i] = sizes[i];
                _set.sizePerTextureIndex = moreSizes;

                _textureIndex = materials.Length;
            }

            _set.sizePerTextureIndex[_textureIndex] = new PegTileDataSet.Int2 { x = grid, y = grid };

            _material.mainTexture = atlas;
            _atlas = atlas;
        }

        private static byte[] HalfSize(byte[] pixels, int side)
        {
            var half = side / 2;
            var result = new byte[half * half * 4];
            for (var y = 0; y < half; y++)
            {
                for (var x = 0; x < half; x++)
                {
                    for (var c = 0; c < 4; c++)
                    {
                        var sum = pixels[((2 * y) * side + 2 * x) * 4 + c] + pixels[((2 * y) * side + 2 * x + 1) * 4 + c]
                                  + pixels[((2 * y + 1) * side + 2 * x) * 4 + c] + pixels[((2 * y + 1) * side + 2 * x + 1) * 4 + c];
                        result[(y * half + x) * 4 + c] = (byte)((sum + 2) / 4);
                    }
                }
            }
            return result;
        }
    }
}
