using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Opens a big-key door without the key by driving its plinth's pinDirectControlSystem.
    internal static class DebugBigKeyDoorForce
    {
        internal static void ForceOne(string nameFilter)
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(DebugBigKeyDoorForce)}] Not the host: nothing done. Every write in this mod is host-side.");
                return;
            }

            var candidates = Collect();
            if (candidates.Count == 0)
            {
                // Never silent: "found nothing" and "never registered" look alike.
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugBigKeyDoorForce)}] No big key prop found at all. Nothing to drive.");
                return;
            }

            Plugin.Log.LogInfo(
                $"[{nameof(DebugBigKeyDoorForce)}] {candidates.Count} big key(s), nearest plinth first:");
            foreach (var candidate in candidates)
                Plugin.Log.LogInfo($"[{nameof(DebugBigKeyDoorForce)}]   {candidate.Describe()}");

            var chosen = Choose(candidates, nameFilter);
            if (chosen == null)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugBigKeyDoorForce)}] Nothing driven: "
                    + (string.IsNullOrEmpty(nameFilter)
                        ? "not one of them has both its plinth loaded and a matching taggedPinSystems entry. "
                          + "Walk to a tower and press again."
                        : $"no big key matches '{nameFilter}' with its plinth loaded. Clear "
                          + $"{nameof(ModConfig.BigKeyDoorName)} to take the nearest one instead."));
                return;
            }

            Drive(chosen);
        }

        // Hands over a key item through KeyCustody.Grant, the path a real item takes.
        internal static void GrantOne(string nameFilter)
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(DebugBigKeyDoorForce)}] Not the host: nothing done.");
                return;
            }

            if (!KeyCustody.KeysAreItems)
            {
                // With the flag off the keys are vanilla: a silent no-op looks like a lost key.
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugBigKeyDoorForce)}] This slot does not have big keys as items "
                    + "(slot_data big_key_items is off), so there is nothing to grant.");
                return;
            }

            var candidates = Collect();
            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrEmpty(nameFilter)
                    && !candidate.PropName.ToString().Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                Plugin.Log.LogInfo(
                    $"[{nameof(DebugBigKeyDoorForce)}] Granting the KEY {candidate.PropName} "
                    + $"(plinth {candidate.HomeName}); its door is a separate item.");
                KeyCustody.Grant(candidate.PropName, toPlayer: true);
                return;
            }

            Plugin.Log.LogInfo(
                $"[{nameof(DebugBigKeyDoorForce)}] No big key matches '{nameFilter}'. "
                + $"Clear {nameof(ModConfig.BigKeyDoorName)} to take the nearest one.");
        }

        // Grants the feature via KeyFeatures.Grant, so the ledger is written and survives a reload.
        internal static void GrantFeature(string nameFilter)
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(DebugBigKeyDoorForce)}] Not the host: nothing done.");
                return;
            }

            if (!KeyFeatures.FeaturesInPlay)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugBigKeyDoorForce)}] This slot does not have big key features as items "
                    + "(slot_data big_key_features is off), so there is nothing to grant.");
                return;
            }

            foreach (var candidate in Collect())
            {
                if (!string.IsNullOrEmpty(nameFilter)
                    && !candidate.PropName.ToString().Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                Plugin.Log.LogInfo(
                    $"[{nameof(DebugBigKeyDoorForce)}] Granting the FEATURE {candidate.PropName} opens "
                    + $"(plinth {candidate.HomeName}); its key is a separate item.");
                KeyFeatures.Grant(candidate.PropName);
                return;
            }

            Plugin.Log.LogInfo(
                $"[{nameof(DebugBigKeyDoorForce)}] No big key matches '{nameFilter}'. "
                + $"Clear {nameof(ModConfig.BigKeyDoorName)} to take the nearest one.");
        }

        // Collected even when incomplete: the table shows why nothing was driven.
        private static List<Candidate> Collect()
        {
            var localPlayer = Core.LocalPlayer.Find();
            Vector3? origin = localPlayer != null ? localPlayer.transform.position : (Vector3?)null;

            var candidates = new List<Candidate>();

            foreach (var prop in Prop.allProps)
            {
                if (prop == null || !GourdRegistry.IsBigKey(prop.saveablePropName))
                    continue;

                var candidate = new Candidate { Prop = prop, PropName = prop.saveablePropName };

                if (GourdRegistry.TryGetHomeName(candidate.PropName, out var homeName))
                {
                    candidate.HomeName = homeName;
                    candidate.Home = GourdRegistry.TryGetHome(homeName);
                }

                if (candidate.Home != null)
                {
                    candidate.Door = FindTaggedSystem(prop, candidate.Home.pinGroup);
                    candidate.Channel = "taggedPinSystems";

                    // Big keys have no pin state of their own: only the plinth's is left.
                    if (candidate.Door == null)
                    {
                        candidate.Door = candidate.Home.pinDirectControlSystem;
                        candidate.Channel = "home.pinDirectControlSystem (fallback)";
                    }
                }

                // Distance to the plinth, where the door is; the key's is printed too.
                candidate.PlinthDistance = Distance(origin, candidate.Home);
                candidate.KeyDistance = Distance(origin, prop);

                candidates.Add(candidate);
            }

            candidates.Sort((a, b) => Sortable(a.PlinthDistance).CompareTo(Sortable(b.PlinthDistance)));
            return candidates;
        }

        private static Candidate Choose(List<Candidate> candidates, string nameFilter)
        {
            foreach (var candidate in candidates)
            {
                if (candidate.Door == null)
                    continue;

                if (!string.IsNullOrEmpty(nameFilter)
                    && !candidate.PropName.ToString().Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                return candidate;
            }

            return null;
        }

        // The tagged pin system whose group its plinth accepts, as the game's loop tests.
        private static TrackedPeckState FindTaggedSystem(Prop prop, PropGroup pinGroup)
        {
            var tagged = prop.taggedPinSystems;
            if (tagged == null)
                return null;

            for (var i = 0; i < tagged.Length; i++)
            {
                var pair = tagged[i];
                if (pair.propGroup == pinGroup && pair.peckSystem != null)
                    return pair.peckSystem;
            }

            return null;
        }

        private static void Drive(Candidate candidate)
        {
            var door = candidate.Door;
            var saveKey = SaveKeyOf(door);

            // Before and after: a state already at 1 and a no-op look the same on screen.
            Plugin.Log.LogInfo(
                $"[{nameof(DebugBigKeyDoorForce)}] Driving {candidate.PropName} via {candidate.Channel} -> "
                + $"'{door.label}' on '{door.name}' (savableSystem {door.savableSystem}); "
                + $"before: {ReadSaved(saveKey)}. {DescribeListeners(door)}");

            try
            {
                door.SetState(1);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[{nameof(DebugBigKeyDoorForce)}] SetState(1) threw: {ex}");
                return;
            }

            Plugin.Log.LogInfo(
                $"[{nameof(DebugBigKeyDoorForce)}] SetState(1) returned; after: {ReadSaved(saveKey)}. "
                + $"Now look at what {candidate.HomeName} is supposed to open.");

            // NotSavable: the unlock lives in memory only and must be re-applied on each load.
            if (door.savableSystem == SavableSystem.NotSavable && string.IsNullOrEmpty(saveKey))
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugBigKeyDoorForce)}] This state persists nothing: if the door opened, the mod "
                    + "will have to keep its own ledger and re-apply on every world load.");
            }
        }

        // Zero listeners means driving it cannot do anything visible.
        private static string DescribeListeners(TrackedPeckState state)
        {
            try
            {
                var refs = state.systemRefences;
                return refs == null
                    ? "systemRefences is null, nothing listens."
                    : $"{refs.Count} effect(s) registered on it.";
            }
            catch (Exception ex)
            {
                return $"could not read systemRefences ({ex.Message}).";
            }
        }

        private static string SaveKeyOf(TrackedPeckState state)
        {
            if (state.savableSystem != SavableSystem.NotSavable)
                return state.savableSystem.ToString();

            return state.saveIdentity != null ? state.saveIdentity.saveGuid : null;
        }

        private static string ReadSaved(string saveKey)
        {
            return string.IsNullOrEmpty(saveKey)
                ? "nothing saved under any key"
                : $"SaveManager['{saveKey}']={SaveManager.GetIntValue(saveKey, -12345, false)}";
        }

        private static float Distance(Vector3? origin, UnityEngine.Component component)
        {
            if (!origin.HasValue || component == null)
                return float.NaN;

            return Vector3.Distance(origin.Value, component.transform.position);
        }

        // NaN sorts last: an unloaded plinth is the least interesting row.
        private static float Sortable(float distance)
        {
            return float.IsNaN(distance) ? float.MaxValue : distance;
        }

        private sealed class Candidate
        {
            internal Prop Prop;
            internal SaveablePropName PropName;
            internal SaveableHomeName HomeName;
            internal PropHome Home;
            internal TrackedPeckState Door;
            internal string Channel;
            internal float PlinthDistance;
            internal float KeyDistance;

            internal string Describe()
            {
                var plinth = Home != null
                    ? $"{HomeName} at {Format(PlinthDistance)} (accepts {Home.pinGroup})"
                    : $"{HomeName} NOT LOADED";

                var door = Door != null
                    ? $"door '{Door.label}' on '{Door.name}' via {Channel}"
                    : "nothing to drive: no taggedPinSystems entry AND no pinDirectControlSystem";

                return $"{PropName} | plinth {plinth} | key at {Format(KeyDistance)} | {door}";
            }

            private static string Format(float distance)
            {
                return float.IsNaN(distance) ? "<no player>" : $"{distance:F1}m";
            }
        }
    }
}
