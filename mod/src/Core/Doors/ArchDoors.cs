using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Doors
{
    // The hub's three arch doors as items (`lock_arch_doors`); granted doors are kept in the save.
    internal static class ArchDoors
    {
        private const string KeyPrefix = "ap_archdoor_";

        internal static readonly SavableSystem[] All =
        {
            SavableSystem.SpawnHubGate,
            SavableSystem.HubShortcutToSportsCreek,
            SavableSystem.HubTunnel,
        };

        private static readonly HashSet<SavableSystem> Locked = new();

        // False until slot_data has said which doors are locked.
        internal static bool Configured { get; private set; }

        internal static bool IsArchDoor(SavableSystem system)
        {
            return Array.IndexOf(All, system) >= 0;
        }

        // `linear_arch_doors`: the doors are the game's own; the mod neither opens nor holds any.
        internal static bool Vanilla { get; private set; }

        internal static void Configure(IEnumerable<string> lockedSystemNames, bool vanilla = false)
        {
            Vanilla = vanilla;
            Locked.Clear();
            foreach (var name in lockedSystemNames ?? Array.Empty<string>())
            {
                if (Enum.TryParse<SavableSystem>(name, out var system) && IsArchDoor(system))
                    Locked.Add(system);
                else
                    Plugin.Log.LogWarning($"[{nameof(ArchDoors)}] '{name}' is not an arch door this build knows; ignored.");
            }

            Configured = true;
            Plugin.Log.LogInfo(vanilla
                ? $"[{nameof(ArchDoors)}] The arch doors are the game's: their buttons open them."
                : $"[{nameof(ArchDoors)}] Held closed until their item arrives: "
                  + (Locked.Count == 0 ? "none." : string.Join(", ", Locked) + "."));

            Enforce();
        }

        // Without Archipelago there is no list to wait for: every door opens.
        internal static void ConfigureWithoutArchipelago()
        {
            Vanilla = false;
            Locked.Clear();
            Configured = true;
        }

        internal static void Forget()
        {
            Locked.Clear();
            Configured = false;
        }

        internal static bool IsHeldClosed(SavableSystem system)
        {
            return Locked.Contains(system) && !IsGranted(system);
        }

        private static bool IsGranted(SavableSystem system)
        {
            return SaveManager.GetIntValue(KeyPrefix + system, 0, false) != 0;
        }

        internal static bool Grant(SavableSystem system)
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogWarning($"[{nameof(ArchDoors)}] Ignored: {system} received while not host.");
                return false;
            }

            SaveManager.SetIntValue(KeyPrefix + system, 1);
            Plugin.Log.LogInfo($"[{nameof(ArchDoors)}] {system} granted.");
            SetDoor(system, open: true);
            return true;
        }

        // A save rebound to another seed replays every item, so doors granted before go with it.
        internal static void ClearLedger()
        {
            foreach (var system in All)
                SaveManager.SetIntValue(KeyPrefix + system, 0);
        }

        // Run when slot_data arrives and whenever a world becomes ready: either can come first.
        internal static void Enforce()
        {
            if (!Configured || Vanilla || !NetworkServer.active || !WorldManager.isReadyForEffects)
                return;

            foreach (var system in All)
                SetDoor(system, open: !IsHeldClosed(system));
        }

        // Live when the door is loaded, else the save alone; the hold patch only refuses a raise.
        private static void SetDoor(SavableSystem system, bool open)
        {
            var target = FindState(system);
            var wanted = open ? 1 : 0;

            if (target != null)
            {
                if (target.currentPeckContext.state == wanted)
                    return;

                target.SetState(wanted);
                Plugin.Log.LogInfo($"[{nameof(ArchDoors)}] {system} {(open ? "opened" : "closed")}.");
                return;
            }

            if (SaveManager.GetIntValue(system.ToString(), 0, false) != wanted)
                SaveManager.SetIntValue(system.ToString(), wanted);
        }

        private static TrackedPeckState FindState(SavableSystem system)
        {
            var loaded = UnityEngine.Object.FindObjectsByType<TrackedPeckState>(FindObjectsSortMode.None);
            if (loaded == null)
                return null;

            foreach (var state in loaded)
            {
                if (state != null && state.savableSystem == system)
                    return state;
            }

            return null;
        }
    }
}
