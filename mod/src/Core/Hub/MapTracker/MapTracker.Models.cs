using System;
using System.Collections.Generic;
using BigWalkArchipelago.Core.Net;
using UnityEngine;

namespace BigWalkArchipelago.Core.Hub
{
    internal static partial class MapTracker
    {
        private static readonly List<Vector3> _placed = new();

        // Markers on one spot spread on a spiral around it, each a little higher.
        private static void Place(string key, Vector3 world, Transform modelSource, bool fromPrefab)
        {
            var at = MapPoint(world);
            Surface(ref at);
            var neighbours = 0;
            foreach (var other in _placed)
            {
                if ((other - at).sqrMagnitude < Neighbour * Neighbour)
                    neighbours++;
            }
            var spot = at;
            if (neighbours > 0)
            {
                var angle = neighbours * 137.5f * Mathf.Deg2Rad;
                var radius = SpreadStep * Mathf.Sqrt(neighbours);
                at += new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Surface(ref at);
            }
            var marker = Make(at, modelSource, fromPrefab, StickHeight + neighbours * StackStep, StickColour);
            if (marker == null)
                return;
            _placed.Add(spot);
            Markers[key] = marker;
        }

        private static bool _noSurfaceLogged;

        private static bool Surface(ref Vector3 at)
        {
            var relief = _map.transform.parent;
            var terrain = Terrain(relief);
            var best = float.MinValue;
            var onTerrain = false;
            foreach (var hit in Physics.RaycastAll(new Ray(at + Vector3.up * 5f, Vector3.down), 10f))
            {
                if (hit.collider == null || relief == null || !hit.collider.transform.IsChildOf(relief) || hit.point.y <= best)
                    continue;
                best = hit.point.y;
                onTerrain = hit.collider == terrain;
            }
            if (best == float.MinValue)
            {
                if (!_noSurfaceLogged)
                {
                    _noSurfaceLogged = true;
                    Plugin.Log.LogInfo($"{Tag} No collider of the relief under a marker: markers stay at their own height.");
                }
                return false;
            }
            if (best <= at.y + 0.005f)
                return false;
            at.y = best;
            // Orange only when under the ground itself; inside a model it stands on the model.
            return onTerrain;
        }

        // The relief's ground: its largest collider.
        private static Collider _terrain;

        private static Collider Terrain(Transform relief)
        {
            if (_terrain != null || relief == null)
                return _terrain;
            var largest = 0f;
            foreach (var collider in relief.GetComponentsInChildren<Collider>(true))
            {
                var size = collider.bounds.size.x * collider.bounds.size.z;
                if (size > largest)
                {
                    largest = size;
                    _terrain = collider;
                }
            }
            if (_terrain != null)
                Plugin.Log.LogInfo($"{Tag} The relief's ground: {_terrain.name}.");
            return _terrain;
        }

        private static Vector3 MapPoint(Vector3 world) =>
            _map.transform.TransformPoint(Vector3.Scale(world + _map.worldScaleOffset, _map.scale));

        // `fromPrefab`: the source is the map's own (inactive) flag, taken whole.
        private static Marker Make(Vector3 at, Transform source, bool fromPrefab, float stickHeight, Color stickColour)
        {
            var marker = new Marker { Root = new GameObject("AP map marker " + source.name) };
            marker.Root.transform.SetParent(_map.transform, true);
            marker.Root.transform.position = at;

            var stick = WorldButtons.MakeSlab("stick", at + Vector3.up * (stickHeight / 2f), Quaternion.identity,
                new Vector3(0.012f, stickHeight, 0.012f), stickColour);
            stick.transform.SetParent(marker.Root.transform, true);
            marker.Stick = stick.GetComponent<Renderer>();
            marker.StickColour = stickColour;

            var model = new GameObject("model");
            model.transform.SetParent(marker.Root.transform, false);
            var parts = new List<GameObject>();
            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (var renderer in source.GetComponentsInChildren<MeshRenderer>(fromPrefab))
            {
                if (renderer == null || !fromPrefab && !renderer.enabled || IsFarLod(renderer.name))
                    continue;
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    continue;

                var part = new GameObject(renderer.name);
                part.transform.SetParent(model.transform, false);
                var localPosition = source.InverseTransformPoint(renderer.transform.position);
                var localRotation = Quaternion.Inverse(source.rotation) * renderer.transform.rotation;
                var scale = renderer.transform.lossyScale;
                var root = source.lossyScale;
                var localScale = new Vector3(scale.x / root.x, scale.y / root.y, scale.z / root.z);
                part.transform.localPosition = localPosition;
                part.transform.localRotation = localRotation;
                part.transform.localScale = localScale;
                part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var copy = part.AddComponent<MeshRenderer>();
                copy.sharedMaterials = renderer.sharedMaterials;

                var own = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(own);
                copy.SetPropertyBlock(own);
                var grey = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(grey);
                grey.SetColor("_TintColor", GreyTint);
                marker.Parts.Add((copy, own, grey));

                // A prefab's renderer has no bounds, so use the mesh's.
                var mesh = filter.sharedMesh.bounds;
                foreach (var corner in Corners(mesh))
                {
                    var p = localPosition + localRotation * Vector3.Scale(corner, localScale);
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
                parts.Add(part);
            }

            if (parts.Count == 0)
            {
                UnityEngine.Object.Destroy(marker.Root);
                return null;
            }

            var centre = (min + max) / 2f;
            foreach (var part in parts)
                part.transform.localPosition -= centre;
            var extent = max - min;
            var size = Mathf.Max(extent.x, Mathf.Max(extent.y, extent.z));
            var factor = size > 0.0001f ? ModelSize / size : 1f;
            model.transform.localScale = Vector3.one * factor;
            // Keep the part's turn in the flag, or the tower's mark stands askew.
            model.transform.rotation = fromPrefab && _map.flagPrefab != null
                ? Quaternion.Inverse(_map.flagPrefab.transform.rotation) * source.rotation
                : source.rotation;
            model.transform.position = at + Vector3.up * (stickHeight + ModelSize / 2f);
            return marker;
        }

        private static IEnumerable<Vector3> Corners(Bounds b)
        {
            for (var i = 0; i < 8; i++)
                yield return b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
        }

        private static bool IsFarLod(string name) =>
            name.IndexOf("LOD", StringComparison.OrdinalIgnoreCase) >= 0 && !name.EndsWith("LOD0", StringComparison.OrdinalIgnoreCase);
    }
}
