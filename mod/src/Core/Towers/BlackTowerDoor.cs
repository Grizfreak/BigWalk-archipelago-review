using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Towers
{
    // Holds the Black Tower's foot door open for `open_black_tower` or at its turn in `tower_mode`.
    internal static class BlackTowerDoor
    {
        internal static bool Enabled { get; private set; }

        // Held open from the start, or because the Black Tower takes gourds.
        internal static bool Held => Enabled || (MonumentOrder.Active && MonumentOrder.IsOpen(5));

        private static TrackedPeckState _door;

        internal static void Configure(bool open)
        {
            if (Enabled != open)
                Plugin.Log.LogInfo($"[{nameof(BlackTowerDoor)}] The Black Tower's door is {(open ? "open from the start" : "as in the game")}.");

            Enabled = open;
        }

        internal static bool IsTheDoor(TrackedPeckState state) => _door != null && state == _door;

        // Every couple of seconds: finds the door once its world is loaded and opens it.
        internal static void Enforce()
        {
            if (!NetworkServer.active || !Held)
                return;

            var door = FindDoor();
            if (door == null || door.currentPeckContext.state >= 1)
                return;

            door.SetState(1);
            Plugin.Log.LogInfo($"[{nameof(BlackTowerDoor)}] The Black Tower's door opened{(Enabled ? string.Empty : ": the Black Tower takes gourds")}.");
        }

        private static TrackedPeckState FindDoor()
        {
            if (_door != null)
                return _door;

            foreach (var state in UnityEngine.Object.FindObjectsByType<TrackedPeckState>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (state == null || state.name != "BasicDoorPeckLogic")
                    continue;

                var door = state.transform.parent;
                var positioner = door != null ? door.parent : null;
                var tower = positioner != null ? positioner.parent : null;
                if (door == null || door.name != "BasicDoor" || positioner == null || positioner.name != "Positioner"
                    || tower == null || !tower.name.StartsWith("BlackTower "))
                    continue;

                _door = state;
                Plugin.Log.LogInfo($"[{nameof(BlackTowerDoor)}] Door found under '{tower.name}'.");
                return state;
            }

            return null;
        }
    }
}
