using System;
using System.Collections.Generic;
using BigWalkArchipelago.Core.Net;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Towers
{
    internal static partial class MonumentOrder
    {
        // A closed tower's buttons are hidden by their model only; the network state is left alone.

        private static readonly string[] ButtonColours = { null, "Red", "Green", "Blue", "Yellow", null, null };
        private static readonly List<Transform>[] ButtonModels = { new(), new(), new(), new(), new(), new(), new() };
        private static readonly Dictionary<Transform, Vector3>[] ButtonHidden =
            { new(), new(), new(), new(), new(), new(), new() };
        private static float _nextButtonSearch;

        private static void TickButtons(bool active)
        {
            for (var tower = 1; tower <= 5; tower++)
            {
                var closed = active && !IsOpen(tower);
                var hidden = ButtonHidden[tower];
                if (!closed && hidden.Count == 0)
                    continue;

                if (!closed)
                {
                    foreach (var pair in hidden)
                    {
                        if (pair.Key != null)
                            SetButton(pair.Key, pair.Value, false);
                    }

                    hidden.Clear();
                    Plugin.Log.LogInfo($"{Tag} The buttons of {Names[tower]} are back.");
                    continue;
                }

                // A tower that loads again brings new buttons, so the list is searched again.
                var models = ButtonModels[tower];
                if ((models.Count == 0 || models.Exists(model => model == null)) && Time.unscaledTime >= _nextButtonSearch)
                    FindButtons();

                var count = 0;
                foreach (var model in models)
                {
                    if (model == null || hidden.ContainsKey(model))
                        continue;

                    hidden[model] = model.localScale;
                    SetButton(model, Vector3.zero, true);
                    count++;
                }

                if (count > 0)
                    Plugin.Log.LogInfo($"{Tag} {count} button(s) of {Names[tower]} hidden.");
            }
        }

        // Scale first: the model's LODGroup turns its renderers back on every frame.
        private static void SetButton(Transform model, Vector3 scale, bool hide)
        {
            model.localScale = scale;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = !hide;
            foreach (var collider in model.GetComponentsInChildren<Collider>(true))
                collider.enabled = !hide;
        }

        private static void FindButtons()
        {
            _nextButtonSearch = Time.unscaledTime + 10f;
            foreach (var list in ButtonModels)
                list.RemoveAll(model => model == null);

            foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (transform == null)
                    continue;

                var isPoke = transform.name == "BasicPokeButton";
                var isHold = transform.name == "Button-nHold-Four";
                if (!isPoke && !isHold)
                    continue;

                var path = string.Empty;
                for (var up = transform; up != null; up = up.parent)
                    path = up.name + "/" + path;

                if (isHold)
                {
                    if (path.Contains("/BlackTower ", StringComparison.Ordinal) && path.Contains("/NHoldFullSet/", StringComparison.Ordinal)
                        && !ButtonModels[5].Contains(transform))
                        ButtonModels[5].Add(transform);
                    continue;
                }

                if (!path.Contains("/Lookouts/Lookout_", StringComparison.Ordinal))
                    continue;

                for (var tower = 1; tower <= 4; tower++)
                {
                    if (!path.Contains("Lookout_" + ButtonColours[tower], StringComparison.OrdinalIgnoreCase))
                        continue;

                    var model = transform.Find("BasicPokeButtonModel");
                    model = model != null ? model : transform;
                    if (!ButtonModels[tower].Contains(model))
                        ButtonModels[tower].Add(model);
                }
            }
        }

        // The game opens the Green Dome door after the Black Tower; the mod opens it at its turn.
        private static TrackedPeckState _greenDomeDoor;
        private static float _nextDoorSearch;

        private static void TickGreenDomeDoor()
        {
            if (!NetworkServer.active || (ApRuntime.SlotData == null && DebugMode == null) || !IsOpen(6))
                return;

            if (_greenDomeDoor == null)
            {
                if (Time.unscaledTime < _nextDoorSearch)
                    return;

                _nextDoorSearch = Time.unscaledTime + 10f;
                _greenDomeDoor = FindGreenDomeDoor();
                if (_greenDomeDoor == null)
                    return;
            }

            if (_greenDomeDoor.currentPeckContext.state == 1)
                return;

            // Logged each time: more than once per world would mean the game shuts it again.
            _greenDomeDoor.SetState(1);
            Plugin.Log.LogInfo($"{Tag} The Green Dome takes gourds: its door opened.");
        }

        private static TrackedPeckState FindGreenDomeDoor()
        {
            foreach (var state in UnityEngine.Object.FindObjectsByType<TrackedPeckState>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var parent = state != null ? state.transform.parent : null;
                if (parent != null && state.name == "BasicDoorPeckLogic" && parent.name == "BasicDoor - to overflow")
                    return state;
            }

            return null;
        }
    }
}
