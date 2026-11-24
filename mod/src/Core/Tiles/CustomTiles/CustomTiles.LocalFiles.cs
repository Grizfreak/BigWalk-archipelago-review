using System.Text;

namespace BigWalkArchipelago.Core.Tiles
{
    internal static partial class CustomTiles
    {
        // Scanned every tick: a file added, removed or saved again makes every drawing read anew.
        private static void ScanLocal()
        {
            if (_folderFailed)
                return;

            FileInfo[] files;
            try
            {
                var folder = new DirectoryInfo(LocalFolder);
                if (!folder.Exists)
                {
                    folder.Create();
                    Plugin.Log.LogInfo($"{Tag} Your own tiles go in {folder.FullName}.");
                }
                files = folder.GetFiles();
            }
            catch (Exception ex)
            {
                _folderFailed = true;
                Plugin.Log.LogWarning($"{Tag} The tiles folder cannot be read, only the mod's drawings are shown: {ex.Message}");
                return;
            }

            Array.Sort(files, (a, b) => string.CompareOrdinal(a.Name, b.Name));
            var pool = PoolFiles();
            var draw = PoolDrawKey();
            var signature = new StringBuilder();
            foreach (var file in files)
                signature.Append(file.Name).Append(':').Append(file.Length).Append(':').Append(file.LastWriteTimeUtc.Ticks).Append(';');
            signature.Append("|pool:");
            foreach (var file in pool)
                signature.Append(file.Name).Append(':').Append(file.Length).Append(':').Append(file.LastWriteTimeUtc.Ticks).Append(';');
            signature.Append("|draw:").Append(draw).Append("|mode:").Append(ModConfig.TilePool.Value);
            if (signature.ToString() == _localSignature)
                return;

            _localSignature = signature.ToString();
            _guestFilesLogged = false;
            LocalFiles.Clear();
            LocalKinds.Clear();
            Drawings.Clear();
            var names = new List<string>();
            foreach (var file in files)
            {
                // The sounds are TileSounds' to read.
                if (string.Equals(file.Extension, ".wav", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!string.Equals(file.Extension, ".png", StringComparison.OrdinalIgnoreCase))
                {
                    Plugin.Log.LogWarning($"{Tag} '{file.Name}' in the tiles folder is not a .png; left out.");
                    continue;
                }

                var stem = Path.GetFileNameWithoutExtension(file.Name);
                if (stem.StartsWith("PegTile", StringComparison.OrdinalIgnoreCase) && Enum.TryParse<PropGroup>(stem, true, out var kind))
                {
                    var kindName = kind.ToString();
                    if (!ReplaceableKind.IsMatch(kindName))
                    {
                        Plugin.Log.LogWarning($"{Tag} '{file.Name}': {kindName} is a pose or point tile, which its puzzle reads; left out.");
                        continue;
                    }
                    LocalFiles[KindPrefix + kindName] = file.FullName;
                    if (!LocalKinds.Contains(kindName))
                        LocalKinds.Add(kindName);
                }
                else
                    LocalFiles[stem] = file.FullName;
                names.Add(file.Name);
            }

            Plugin.Log.LogInfo(names.Count == 0
                ? $"{Tag} No drawing of your own in {LocalFolder}."
                : $"{Tag} Your own drawings in {LocalFolder}: {string.Join(", ", names)}.");
            DrawPool(pool, draw);
        }

        private const int MaxPoolFiles = 1000;
        private const int PoolOnGameTiles = 1;
        private const int PoolOff = 2;

        private static FileInfo[] PoolFiles()
        {
            try
            {
                var folder = new DirectoryInfo(Path.Combine(LocalFolder, "pool"));
                if (!folder.Exists)
                {
                    folder.Create();
                    return Array.Empty<FileInfo>();
                }
                var files = new List<FileInfo>();
                foreach (var file in folder.GetFiles("*.png"))
                {
                    if (files.Count == MaxPoolFiles)
                        break;
                    files.Add(file);
                }
                files.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
                return files.ToArray();
            }
            catch (Exception)
            {
                return Array.Empty<FileInfo>();
            }
        }

        // Keyed on seed and slot: the same draw on every reconnection, another next seed.
        private static string PoolDrawKey()
        {
            if (IsGuest || !ApRuntime.ShufflesPegTiles || ApRuntime.Current == null)
                return string.Empty;
            return $"{ApRuntime.Current.SeedName}/{ApRuntime.Current.SlotName}";
        }

        private static void DrawPool(FileInfo[] pool, string draw)
        {
            var mode = ModConfig.TilePool.Value;
            if (pool.Length == 0 || draw.Length == 0 || mode == PoolOff)
                return;

            var seeded = new HashSet<string>();
            if (mode == PoolOnGameTiles)
            {
                foreach (var entry in _seed)
                {
                    var equals = entry.IndexOf('=');
                    if (equals > 0)
                        seeded.Add(entry.Substring(0, equals));
                }
            }

            var free = new List<string>();
            for (var i = 1; i <= Vanilla.Length; i++)
            {
                var kind = $"PegTileA{i}";
                if (!LocalKinds.Contains(kind) && !seeded.Contains(kind))
                    free.Add(kind);
            }
            // A hash of the key, not System.Random: the same order on every machine.
            free.Sort((a, b) => Rank(draw, a).CompareTo(Rank(draw, b)));
            var picks = new List<FileInfo>(pool);
            picks.Sort((a, b) => Rank(draw, a.Name.ToLowerInvariant()).CompareTo(Rank(draw, b.Name.ToLowerInvariant())));

            var drawn = new List<string>();
            for (var i = 0; i < free.Count && i < picks.Count; i++)
            {
                LocalFiles[KindPrefix + free[i]] = picks[i].FullName;
                LocalKinds.Add(free[i]);
                drawn.Add($"{free[i]}={picks[i].Name}");
            }
            drawn.Sort(StringComparer.Ordinal);
            Plugin.Log.LogInfo($"{Tag} {pool.Length} drawing(s) in the pool, {drawn.Count} drawn for this seed"
                               + (mode == PoolOnGameTiles ? " on the game's own icons" : string.Empty) + $": {string.Join(", ", drawn)}."); 
        }

        private static ulong Rank(string draw, string name) => Blob.Fnv(Encoding.UTF8.GetBytes(draw + "|" + name));

        // Ink is the alpha; with no transparency, the distance from the border's brightness.
        private static byte[] ToDrawing(byte[] rgba, int width, int height, string name)
        {
            var count = width * height;
            var ink = new byte[count];
            var transparent = false;
            for (var i = 0; i < count && !transparent; i++)
                transparent = rgba[i * 4 + 3] < 255;

            if (transparent)
            {
                for (var i = 0; i < count; i++)
                    ink[i] = rgba[i * 4 + 3];
            }
            else
            {
                long sum = 0;
                var border = 0;
                for (var x = 0; x < width; x++)
                {
                    sum += Brightness(rgba, x) + Brightness(rgba, (height - 1) * width + x);
                    border += 2;
                }
                for (var y = 0; y < height; y++)
                {
                    sum += Brightness(rgba, y * width) + Brightness(rgba, y * width + width - 1);
                    border += 2;
                }
                var background = (int)(sum / border);
                var range = Math.Max(1, Math.Max(background, 255 - background));
                for (var i = 0; i < count; i++)
                    ink[i] = (byte)Math.Min(255, Math.Abs(Brightness(rgba, i) - background) * 255 / range);
                Plugin.Log.LogInfo($"{Tag} '{name}' has no transparency: its ink is what stands out from its "
                                   + (background < 128 ? "dark" : "light") + " background.");
            }

            var pixels = new byte[Cell * Cell * 4];
            for (var i = 0; i < pixels.Length; i += 4)
            {
                pixels[i] = 255;
                pixels[i + 1] = 255;
                pixels[i + 2] = 255;
            }

            if (width == Cell && height == Cell)
            {
                for (var i = 0; i < count; i++)
                    pixels[i * 4 + 3] = ink[i];
                return pixels;
            }

            // Box average; a picture smaller than a tile is scaled up blocky.
            var side = Math.Max(width, height);
            var fitWidth = Math.Max(1, (int)((long)width * Cell / side));
            var fitHeight = Math.Max(1, (int)((long)height * Cell / side));
            var left = (Cell - fitWidth) / 2;
            var top = (Cell - fitHeight) / 2;
            for (var ty = 0; ty < fitHeight; ty++)
            {
                var y0 = (int)((long)ty * height / fitHeight);
                var y1 = Math.Max(y0 + 1, (int)((long)(ty + 1) * height / fitHeight));
                for (var tx = 0; tx < fitWidth; tx++)
                {
                    var x0 = (int)((long)tx * width / fitWidth);
                    var x1 = Math.Max(x0 + 1, (int)((long)(tx + 1) * width / fitWidth));
                    long total = 0;
                    for (var y = y0; y < y1; y++)
                        for (var x = x0; x < x1; x++)
                            total += ink[y * width + x];
                    pixels[((top + ty) * Cell + left + tx) * 4 + 3] = (byte)(total / ((y1 - y0) * (x1 - x0)));
                }
            }

            Plugin.Log.LogInfo($"{Tag} '{name}' is {width}x{height}: fitted into {Cell}x{Cell}"
                               + (width != height ? ", centred." : "."));
            return pixels;
        }

        private static int Brightness(byte[] rgba, int pixel)
        {
            var at = pixel * 4;
            return (rgba[at] * 299 + rgba[at + 1] * 587 + rgba[at + 2] * 114) / 1000;
        }
    }
}
