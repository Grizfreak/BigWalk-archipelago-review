using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Core.Puzzles
{
    // Hides the secret room printer's cards while `lock_endings_until_objectives` holds.
    internal static class PrinterHold
    {
        private const string Tag = "[" + nameof(PrinterHold) + "]";

        private static Transform _printer;
        private static float _nextSearch;
        private static bool _hidden;
        private static readonly Dictionary<int, Vector3> Scales = new();

        internal static void Tick()
        {
            var locked = ObjectiveBoard.HoldsTheLastStairway && ObjectiveBoard.Locked("big_game");
            if (!locked && !_hidden)
                return;

            if (_printer == null)
            {
                // A new world has fresh, visible cards whatever the last one's state was.
                _hidden = false;
                Scales.Clear();
                if (!locked)
                    return;

                if (Time.unscaledTime < _nextSearch)
                    return;

                Find();
                if (_printer == null)
                    return;
            }

            if (locked == _hidden)
                return;

            foreach (var prop in _printer.GetComponentsInChildren<Prop>(true))
            {
                if (prop == null || !prop.name.StartsWith("CueCard", StringComparison.Ordinal))
                    continue;

                var card = prop.transform;
                var id = card.GetInstanceID();
                if (locked)
                {
                    Scales[id] = card.localScale;
                    card.localScale = Vector3.zero;
                }
                else if (Scales.TryGetValue(id, out var scale))
                {
                    card.localScale = scale;
                }
            }

            foreach (var home in _printer.GetComponentsInChildren<PropHome>(true))
            {
                if (home != null && home.name == "PrinterPropHome")
                    home.blockGrabbing = locked;
            }

            _hidden = locked;
            if (!locked)
                Scales.Clear();
            Plugin.Log.LogInfo($"{Tag} The printer's cards are {(locked ? "hidden" : "back")}.");
        }

        private static void Find()
        {
            _nextSearch = Time.unscaledTime + 10f;
            foreach (var transform in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (transform != null && transform.name == "GoodbyePrinter")
                {
                    _printer = transform;
                    return;
                }
            }
        }
    }
}
