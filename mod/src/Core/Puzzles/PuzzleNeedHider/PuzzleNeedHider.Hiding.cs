using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Puzzles
{
    internal partial class PuzzleNeedHider : MonoBehaviour
    {
        private sealed class SoftState
        {
            internal Renderer[] Renderers;
            internal bool[] RendererWas;
            internal Collider[] Colliders;
            internal bool[] ColliderWas;
            internal Rigidbody[] Bodies;
            internal bool[] BodyWasKinematic;
            // Its home now, not when hidden: a moved prop takes the physics of where it is.
            internal Prop Prop;
            internal bool WasPinned;
        }

        internal static bool IsSoftHidden(GameObject go) =>
            go != null && _current != null && _current._soft.ContainsKey(go.GetInstanceID());

        private static bool IsLocked(string need) =>
            need == GauntletStairways.ButtonsNeed ? GauntletStairways.ButtonsHidden : PuzzleNeeds.IsLocked(need);

        private bool IsObjectLocked(string key, bool needLocked, int id, bool gauntletPartsLocked)
        {
            if (key == GauntletStairways.ButtonsNeed)
                return GauntletStairways.ButtonsHidden;

            if (_inGauntlet.Contains(id) && !gauntletPartsLocked)
                return false;

            if (key != SkipAidNeed)
                return needLocked;

            return _skipNeeds.TryGetValue(id, out var needs) && needs.Any(PuzzleNeeds.IsLocked);
        }

        private void Apply()
        {
            var gauntletPartsLocked = GauntletStairways.PartsHidden;
            foreach (var pair in _objects)
            {
                var needLocked = IsLocked(pair.Key);
                var locked = needLocked;
                var hid = 0;
                var showed = 0;
                foreach (var go in pair.Value)
                {
                    if (go == null)
                        continue;

                    var id = go.GetInstanceID();
                    var networked = _networked.Contains(id);

                    locked = IsObjectLocked(pair.Key, needLocked, id, gauntletPartsLocked);
                    if (locked)
                    {
                        if (networked)
                        {
                            if (SoftHide(go, id))
                                hid++;
                        }
                        else if (go.activeSelf)
                        {
                            _hiddenByUs.Add(id);
                            go.SetActive(false);
                            hid++;
                        }
                    }
                    else if (_hiddenByUs.Remove(id))
                    {
                        if (networked)
                            SoftShow(id);
                        else
                            go.SetActive(true);

                        showed++;
                    }
                }

                // What was done, not what the need says: poles follow their own puzzle's needs.
                if (hid > 0)
                    Plugin.Log.LogInfo(
                        $"[{nameof(PuzzleNeedHider)}] {Stamp()} {pair.Key}: hid {hid} of {pair.Value.Count}.");
                if (showed > 0)
                    Plugin.Log.LogInfo(
                        $"[{nameof(PuzzleNeedHider)}] {Stamp()} {pair.Key}: showed {showed} of {pair.Value.Count}.");
            }
        }

        // Run every second: the game's culling and scripts switch renderers back on.
        private bool SoftHide(GameObject go, int id)
        {
            var first = !_soft.TryGetValue(id, out var state);
            if (first)
            {
                state = new SoftState
                {
                    Renderers = go.GetComponentsInChildren<Renderer>(true),
                    Colliders = go.GetComponentsInChildren<Collider>(true),
                    Bodies = go.GetComponentsInChildren<Rigidbody>(true),
                };
                state.RendererWas = state.Renderers.Select(r => r != null && r.enabled).ToArray();
                state.ColliderWas = state.Colliders.Select(c => c != null && c.enabled).ToArray();
                state.BodyWasKinematic = state.Bodies.Select(b => b != null && b.isKinematic).ToArray();
                state.Prop = go.GetComponent<Prop>();
                state.WasPinned = state.Prop != null && state.Prop.currentHome != null;
                _soft[id] = state;
            }

            _hiddenByUs.Add(id);

            // A renderer the game switches on while hidden is one it means to show.
            for (var i = 0; i < state.Renderers.Length; i++)
            {
                var renderer = state.Renderers[i];
                if (renderer != null && renderer.enabled)
                {
                    if (!first)
                        state.RendererWas[i] = true;
                    renderer.enabled = false;
                }
            }

            for (var i = 0; i < state.Colliders.Length; i++)
            {
                var collider = state.Colliders[i];
                if (collider != null && collider.enabled)
                {
                    if (!first)
                        state.ColliderWas[i] = true;
                    collider.enabled = false;
                }
            }

            // Without a collider a loose tile would fall out of the world.
            foreach (var body in state.Bodies)
            {
                if (body != null && !body.isKinematic)
                    body.isKinematic = true;
            }

            return first;
        }

        private void SoftShow(int id)
        {
            if (!_soft.TryGetValue(id, out var state))
                return;

            for (var i = 0; i < state.Renderers.Length; i++)
            {
                if (state.Renderers[i] != null)
                    state.Renderers[i].enabled = state.RendererWas[i];
            }

            for (var i = 0; i < state.Colliders.Length; i++)
            {
                if (state.Colliders[i] != null)
                    state.Colliders[i].enabled = state.ColliderWas[i];
            }

            // Held if it hangs in a home now, free to fall if not.
            var pinnedNow = state.Prop != null && state.Prop.currentHome != null;
            var moved = state.Prop != null && pinnedNow != state.WasPinned;
            for (var i = 0; i < state.Bodies.Length; i++)
            {
                var body = state.Bodies[i];
                if (body == null)
                    continue;
                body.isKinematic = moved && state.Prop.rb != null && body.Pointer == state.Prop.rb.Pointer
                    ? pinnedNow
                    : state.BodyWasKinematic[i];
            }

            _soft.Remove(id);
        }
    }
}
