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
    internal static partial class CustomTiles
    {
        // A pose read from the game's atlas and flipped; no game picture ships in the DLL.
        private const string MirrorPrefix = "mirror_";

        private static byte[] Mirrored(string kindName)
        {
            if (_set == null || _original == null || !Enum.TryParse<PropGroup>(kindName, out var kind))
                return null;

            var found = false;
            var cell = default(PegTileDataSet.TileIndexData);
            foreach (var data in _original)
            {
                if (data.propGroup == kind)
                {
                    cell = data;
                    found = true;
                    break;
                }
            }

            var materials = _set.materialPerTextureIndex;
            var texture = found && cell.textureIndex >= 0 && cell.textureIndex < materials.Length && materials[cell.textureIndex] != null
                ? materials[cell.textureIndex].mainTexture
                : null;
            if (texture == null)
            {
                Plugin.Log.LogWarning($"{Tag} No cell of {kindName} to mirror; that tile stays as it is.");
                return null;
            }

            var grid = _set.sizePerTextureIndex[cell.textureIndex];
            var cellWidth = texture.width / (float)Math.Max(1, grid.x);
            var cellHeight = texture.height / (float)Math.Max(1, grid.y);
            var left = Mathf.FloorToInt(cell.xIndex * cellWidth);
            var width = Mathf.FloorToInt((cell.xIndex + 1) * cellWidth) - left;
            // Row 0 of the grid is the texture's top; ReadPixels counts from the bottom.
            var top = Mathf.FloorToInt(cell.yIndex * cellHeight);
            var height = Mathf.FloorToInt((cell.yIndex + 1) * cellHeight) - top;
            var bottom = texture.height - top - height;

            var target = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            Texture2D copy = null;
            try
            {
                Graphics.Blit(texture, target);
                RenderTexture.active = target;
                copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(left, bottom, width, height), 0, 0);
                copy.Apply();
                var source = copy.GetPixels32();

                var pixels = new byte[Cell * Cell * 4];
                for (var row = 0; row < Cell; row++)
                {
                    var y = height - 1 - Math.Min(height - 1, (int)((row + 0.5f) * height / Cell));
                    for (var column = 0; column < Cell; column++)
                    {
                        var x = width - 1 - Math.Min(width - 1, (int)((column + 0.5f) * width / Cell));
                        var pixel = source[y * width + x];
                        var bright = Math.Max(pixel.r, Math.Max(pixel.g, pixel.b));
                        var at = (row * Cell + column) * 4;
                        pixels[at] = 255;
                        pixels[at + 1] = 255;
                        pixels[at + 2] = 255;
                        pixels[at + 3] = (byte)(pixel.a * bright / 255);
                    }
                }

                Plugin.Log.LogInfo($"{Tag} {kindName} mirrored from its cell ({left}, {top}, {width} x {height}) of '{texture.name}'.");
                return pixels;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Could not mirror {kindName}, it stays as it is: {ex.Message}");
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                if (copy != null)
                    UnityEngine.Object.Destroy(copy);
            }
        }
    }
}
