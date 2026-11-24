using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Swaps the island's peg tiles among their own spots, per seed, within one rack and family.
    internal static class PegTileShuffler
    {
        private const string Tag = "[" + nameof(PegTileShuffler) + "]";

        private static bool _enabled;
        private static string _seed = string.Empty;
        private static float _at = -1f;
        private static float _nextScan;

        // Sets already drawn here: a tile set loose may leave its rack and must not be drawn twice.
        private static readonly HashSet<string> _done = new();
        private static readonly HashSet<int> _moved = new();

        // A few seconds after the world is ready, so its tiles have been pinned by the game first.
        private const float Delay = 5f;

        // How often the tiles are looked for again: a far part of the island comes in later.
        private const float ScanSeconds = 10f;

        // A prop that comes back into the world starts no second time: looked for now and then.
        private const float QuietScanSeconds = 60f;
        private static bool _dirty = true;
        private static float _nextQuietScan;

        internal static void MarkDirty() => _dirty = true;

        // Close enough for the ground to be loaded, far enough for the swaps to be out of sight.
        private const float NearMetres = 150f;

        internal static void Configure(string seed)
        {
            // A reconnection to the same seed changes nothing.
            if ((seed ?? string.Empty) != _seed)
            {
                _done.Clear();
                _moved.Clear();
            }

            _enabled = true;
            _seed = seed ?? string.Empty;
        }

        internal static void Tick()
        {
            if (!_enabled || !Net.ApRuntime.ShufflesPegTiles || !NetworkServer.active)
                return;

            if (_at < 0f)
            {
                _at = Time.unscaledTime + Delay;
                return;
            }

            if (Time.unscaledTime < _at || Time.unscaledTime < _nextScan)
                return;

            _nextScan = Time.unscaledTime + ScanSeconds;
            ReleaseNear();
            if (!_dirty && Time.unscaledTime < _nextQuietScan)
                return;

            _dirty = false;
            _nextQuietScan = Time.unscaledTime + QuietScanSeconds;
            Shuffle();
        }

        // A world went away: the next one is shuffled again, from its own starting places.
        internal static void ForgetWorld()
        {
            _done.Clear();
            _moved.Clear();
            Frozen.Clear();
            _at = -1f;
            _nextScan = 0f;
            _dirty = true;
            _nextQuietScan = 0f;
        }

        // The home a tile is pinned to, or, for a tile simply lying there (most), its place.
        private struct Spot
        {
            internal PropHome Home;
            internal Vector3 Position;
            internal Vector3 Local;
        }

        private sealed class Set
        {
            internal Transform Root;
            internal readonly List<Prop> Tiles = new();
        }

        private static void Shuffle()
        {
            var sets = new Dictionary<string, Set>();
            foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsSortMode.None))
            {
                if (prop == null || prop.propGroups == null || !prop.propGroups.Contains(PropGroup.PegTile)
                    || prop.name.Contains("(AP", StringComparison.Ordinal) || _moved.Contains(prop.GetInstanceID())
                    || IsHeld(prop))
                    continue;

                // A tile in a puzzle's slot (display or answer) or stowed in a pack: left alone.
                var home = prop.currentHome;
                if (home != null && (home.name.StartsWith("PegSlot", StringComparison.Ordinal)
                                     || home.GetComponentInParent<Prop>() != null))
                    continue;

                var root = SetRoot(prop.transform);
                if (root == null)
                    continue;

                var key = Path(root) + "|" + FamilyOf(prop) + (prop.name.Contains("Speaker", StringComparison.Ordinal) ? "|speaker" : string.Empty);
                if (_done.Contains(key))
                    continue;

                if (!sets.TryGetValue(key, out var set))
                    sets[key] = set = new Set { Root = root };
                set.Tiles.Add(prop);
            }

            foreach (var pair in sets)
            {
                if (pair.Value.Tiles.Count < 2)
                {
                    _done.Add(pair.Key);
                    continue;
                }

                _done.Add(pair.Key);
                Draw(pair.Key, pair.Value);
            }
        }

        private static void Draw(string key, Set set)
        {
            var tiles = set.Tiles;
            tiles.Sort((a, b) => string.CompareOrdinal(IdOf(a), IdOf(b)));

            var spots = new List<Spot>();
            foreach (var tile in tiles)
            {
                spots.Add(new Spot
                {
                    Home = tile.currentHome,
                    Position = tile.transform.position,
                    Local = set.Root.InverseTransformPoint(tile.transform.position),
                });
            }

            // The spots in an order that does not depend on which tile stands on each.
            spots.Sort(CompareSpots);

            var random = new System.Random(StableHash(_seed + "|" + key));
            for (var i = spots.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (spots[i], spots[j]) = (spots[j], spots[i]);
            }

            // Out of every home first, so no home is asked to hold two.
            foreach (var tile in tiles)
            {
                _moved.Add(tile.GetInstanceID());
                if (tile.currentHome != null)
                    tile.ServerSetUnpinned();
            }

            var moved = 0;
            for (var i = 0; i < tiles.Count; i++)
            {
                try
                {
                    var tile = tiles[i];
                    var spot = spots[i];
                    if (spot.Home != null)
                    {
                        tile.ServerSetPinned(spot.Home);
                    }
                    else
                    {
                        // The place only: each tile keeps its own turn.
                        tile.SetLoose();
                        tile.transform.position = spot.Position;

                        // Held still until a player is near enough for the ground to be there.
                        if (tile.rb != null)
                        {
                            tile.rb.isKinematic = true;
                            Frozen.Add(tile);
                        }
                    }

                    // A soft-hidden tile has no collider and would fall through: held until shown.
                    if (PuzzleNeedHider.IsSoftHidden(tile.gameObject) && tile.rb != null)
                        tile.rb.isKinematic = true;

                    moved++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} '{tiles[i].name}' could not be placed: {ex.Message}");
                }
            }

            Plugin.Log.LogInfo($"{Tag} {moved} tile(s) swapped about in {key}.");
        }

        // Its nearest parent named PegTileRack... or PegTileSet..., else its own parent.
        private static Transform SetRoot(Transform tile)
        {
            for (var parent = tile.parent; parent != null; parent = parent.parent)
            {
                var name = parent.name;
                if (name.StartsWith("PegTileRack", StringComparison.Ordinal)
                    || name.StartsWith("PegTileSet", StringComparison.Ordinal))
                    return parent;
            }

            return tile.parent;
        }

        // The symbols' family (PegTileA, PegTilePriestB...): a tile only swaps with its own kind.
        private static string FamilyOf(Prop prop)
        {
            foreach (var group in prop.propGroups)
            {
                if (group != PropGroup.PegTile && group.ToString().StartsWith("PegTile", StringComparison.Ordinal))
                    return PegTileCloner.Family(group);
            }

            return "PegTile";
        }

        private static string IdOf(Prop prop)
        {
            return string.IsNullOrEmpty(prop.savablePropGuid) ? Path(prop.transform) : prop.savablePropGuid;
        }

        // By the home's place in the scene, else by the spot's place in the set, to the decimetre.
        private static int CompareSpots(Spot a, Spot b)
        {
            if (a.Home != null || b.Home != null)
            {
                if (a.Home == null)
                    return 1;
                if (b.Home == null)
                    return -1;
                return string.CompareOrdinal(Path(a.Home.transform), Path(b.Home.transform));
            }

            var byX = Mathf.RoundToInt(a.Local.x * 10f).CompareTo(Mathf.RoundToInt(b.Local.x * 10f));
            if (byX != 0)
                return byX;
            var byZ = Mathf.RoundToInt(a.Local.z * 10f).CompareTo(Mathf.RoundToInt(b.Local.z * 10f));
            if (byZ != 0)
                return byZ;
            return Mathf.RoundToInt(a.Local.y * 10f).CompareTo(Mathf.RoundToInt(b.Local.y * 10f));
        }

        private static readonly List<Prop> Frozen = new();

        private static void ReleaseNear()
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null || Frozen.Count == 0)
                return;

            for (var i = Frozen.Count - 1; i >= 0; i--)
            {
                var tile = Frozen[i];
                if (tile == null)
                {
                    Frozen.RemoveAt(i);
                    continue;
                }

                var near = false;
                foreach (var pc in players)
                {
                    if (pc != null && (pc.transform.position - tile.transform.position).sqrMagnitude < NearMetres * NearMetres)
                    {
                        near = true;
                        break;
                    }
                }

                if (!near)
                    continue;

                Frozen.RemoveAt(i);
                // A tile its puzzle need hides stays held (PuzzleNeedHider.SoftShow lets it go).
                if (tile.rb != null && !PuzzleNeedHider.IsSoftHidden(tile.gameObject))
                    tile.rb.isKinematic = false;
            }
        }

        private static bool PlayerNear(List<Prop> tiles)
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null)
                return false;

            var centre = Vector3.zero;
            foreach (var tile in tiles)
                centre += tile.transform.position;
            centre /= tiles.Count;

            foreach (var pc in players)
            {
                if (pc != null && (pc.transform.position - centre).sqrMagnitude < NearMetres * NearMetres)
                    return true;
            }

            return false;
        }

        private static bool IsHeld(Prop prop)
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null)
                return false;

            foreach (var pc in players)
            {
                if (pc != null && pc.hands != null && pc.hands.heldProp == prop)
                    return true;
            }

            return false;
        }

        private static string Path(Transform t)
        {
            var path = t.name;
            for (var parent = t.parent; parent != null; parent = parent.parent)
                path = parent.name + "/" + path;
            return path;
        }

        // The same number on every run and every machine (string.GetHashCode is not).
        private static int StableHash(string text)
        {
            unchecked
            {
                var hash = (int)2166136261;
                foreach (var c in text)
                    hash = (hash ^ c) * 16777619;
                return hash;
            }
        }
    }
}
