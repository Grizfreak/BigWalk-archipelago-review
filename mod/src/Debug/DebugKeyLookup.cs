using System;
using System.Collections.Generic;
using BigWalkArchipelago.Core;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Dumps the big keys' blanks, plinths, wiring, custody and cutting stations.
    internal static class DebugKeyLookup
    {
        internal static void Dump()
        {
            Plugin.Log.LogInfo($"[{nameof(DebugKeyLookup)}] === big key dump ===");

            DumpKeyBlanks();
            DumpPlinths();
            DumpKeyedSwitches();
            DumpKeyProps();
            DumpKeyCustody();
            DumpCuttingStations();

            Plugin.Log.LogInfo($"[{nameof(DebugKeyLookup)}] === end of big key dump ===");
        }

        private static void DumpKeyBlanks()
        {
            var blanks = UnityEngine.Object.FindObjectsByType<KeyBlank>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            // Said even when zero: silence would look like a key that never registered.
            Plugin.Log.LogInfo(
                $"[{nameof(DebugKeyLookup)}] KeyBlank instances loaded: {blanks.Length}"
                + (blanks.Length == 0
                    ? " (they stream in with their tower; walk nearer one, or fly with F2)"
                    : string.Empty));

            foreach (var blank in blanks)
            {
                if (blank == null)
                    continue;

                var prop = blank.prop;
                var propName = prop != null ? prop.saveablePropName.ToString() : "<no prop>";

                var segments = "<null>";
                var cut = 0;
                var cuts = blank.cuts;
                if (cuts != null)
                {
                    var total = cuts.Count;
                    var flags = new char[total];
                    for (var i = 0; i < total; i++)
                    {
                        flags[i] = cuts[i] ? 'X' : '.';
                        if (cuts[i])
                            cut++;
                    }

                    segments = total > 0 ? new string(flags) : "<empty>";
                }

                var finished = blank.finishedPropGroup;
                var alreadyFinished = prop != null && HasGroup(prop, finished);

                Plugin.Log.LogInfo(
                    $"[{nameof(DebugKeyLookup)}]   '{blank.name}' | key {propName} "
                    + $"| segments [{segments}] ({cut} cut) | finishedPropGroup {finished} "
                    + $"| already in propGroups={alreadyFinished} | startFinished={blank.startFinished} "
                    + $"| covers={(blank.covers != null ? blank.covers.Length : 0)}");
            }
        }

        private static void DumpPlinths()
        {
            var blocks = UnityEngine.Object.FindObjectsByType<PropHomeBlock>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (SaveablePropName propName in Enum.GetValues(typeof(SaveablePropName)))
            {
                if (!GourdRegistry.IsBigKey(propName)
                    || !GourdRegistry.TryGetHomeName(propName, out var homeName))
                    continue;

                var home = GourdRegistry.TryGetHome(homeName);
                if (home == null)
                {
                    Plugin.Log.LogInfo(
                        $"[{nameof(DebugKeyLookup)}]   plinth {homeName} ({propName}): not loaded.");
                    continue;
                }

                var pinSystem = Describe(home.pinDirectControlSystem);

                var blockSystem = "<no PropHomeBlock found>";
                foreach (var block in blocks)
                {
                    if (block == null || block.homes == null)
                        continue;

                    var owns = false;
                    for (var i = 0; i < block.homes.Length && !owns; i++)
                        owns = block.homes[i] != null && block.homes[i].Pointer == home.Pointer;

                    if (!owns)
                        continue;

                    blockSystem = $"{Describe(block.isFullDirectControlSystem)} (block '{block.name}', "
                                  + $"{block.homes.Length} home(s))";
                    break;
                }

                Plugin.Log.LogInfo(
                    $"[{nameof(DebugKeyLookup)}]   plinth {homeName} ({propName}) | pinGroup {home.pinGroup} "
                    + $"| pinDirectControlSystem {pinSystem} | isFull -> {blockSystem}");

                Plugin.Log.LogInfo(
                    $"[{nameof(DebugKeyLookup)}]       onPin {DescribeSwitch(home.onPin)} "
                    + $"| onUnpin {DescribeSwitch(home.onUnpin)}");

                DumpPlinthWiring(home);
            }
        }

        // A TrackedPeckState does nothing alone: list what sits on, above and below it.
        private static void DumpPlinthWiring(PropHome home)
        {
            var state = home.pinDirectControlSystem;
            if (state == null)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugKeyLookup)}]       pinDirectControlSystem is null, this plinth drives "
                    + "nothing at all on a pin.");
                return;
            }

            try
            {
                var refs = state.systemRefences;
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugKeyLookup)}]       pin state '{state.name}': "
                    + (refs == null
                        ? "systemRefences null, NOTHING listens, so driving it cannot open anything."
                        : $"{refs.Count} effect(s) registered."));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugKeyLookup)}]       pin state '{state.name}': systemRefences unreadable "
                    + $"({ex.Message}).");
            }

            var go = state.gameObject;
            Plugin.Log.LogInfo(
                $"[{nameof(DebugKeyLookup)}]       path {DebugComponentLookup.DescribePath(go.transform)}/{go.name}");
            Plugin.Log.LogInfo(
                $"[{nameof(DebugKeyLookup)}]       on it: {DebugComponentLookup.DescribeComponents(go)}");

            var parent = go.transform.parent;
            if (parent != null)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugKeyLookup)}]       parent '{parent.name}': "
                    + DebugComponentLookup.DescribeComponents(parent.gameObject));
            }

            // Siblings too: the effect that opens a door may hang beside the state.
            var scope = parent != null ? parent : go.transform;
            for (var i = 0; i < scope.childCount; i++)
            {
                var child = scope.GetChild(i);
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugKeyLookup)}]         '{child.name}': "
                    + DebugComponentLookup.DescribeComponents(child.gameObject));
            }
        }

        private static void DumpKeyedSwitches()
        {
            var switches = UnityEngine.Object.FindObjectsByType<PeckSwitch>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            var found = 0;
            foreach (var peckSwitch in switches)
            {
                if (peckSwitch == null || !peckSwitch.needsKey)
                    continue;

                found++;
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugKeyLookup)}]   keyed switch '{peckSwitch.name}' | keyType {peckSwitch.keyType} "
                    + $"| drives {Describe(peckSwitch.trackedStateSystem)} "
                    + $"| mode {peckSwitch.stateMode} specific={peckSwitch.specificState}");
            }

            Plugin.Log.LogInfo(
                $"[{nameof(DebugKeyLookup)}] {found} switch(es) requiring a key, out of {switches.Length} loaded."
                + (found == 0 ? " None here, the doors may be plinth-driven after all." : string.Empty));
        }

        private static string DescribeSwitch(PeckSwitch peckSwitch)
        {
            if (peckSwitch == null)
                return "<none>";

            return $"'{peckSwitch.name}' -> {Describe(peckSwitch.trackedStateSystem)}";
        }

        // The door lives in the key's `taggedPinSystems`, on the entry matching the plinth.
        private static void DumpKeyProps()
        {
            foreach (var prop in Prop.allProps)
            {
                if (prop == null || !GourdRegistry.IsBigKey(prop.saveablePropName))
                    continue;

                var groups = "<none>";
                var list = prop.propGroups;
                if (list != null && list.Count > 0)
                {
                    var parts = new string[list.Count];
                    for (var i = 0; i < list.Count; i++)
                        parts[i] = list[i].ToString();
                    groups = string.Join(", ", parts);
                }

                Plugin.Log.LogInfo(
                    $"[{nameof(DebugKeyLookup)}]   key prop '{prop.name}' ({prop.saveablePropName}) "
                    + $"| propGroups [{groups}] "
                    + $"| SaveManager value={SaveManager.GetIntValue(prop.saveablePropName.ToString(), 0, false)}");

                DumpTaggedPinSystems(prop);
            }
        }

        private static void DumpTaggedPinSystems(Prop prop)
        {
            Plugin.Log.LogInfo(
                $"[{nameof(DebugKeyLookup)}]       own pinDirectControlSystem {Describe(prop.pinDirectControlSystem)}");

            // Null when the tower is not loaded: print every pair unmarked then.
            var pinGroup = (PropGroup?)null;
            var keyHome = GourdRegistry.TryGetHomeFor(prop.saveablePropName);
            if (keyHome != null)
                pinGroup = keyHome.pinGroup;

            var tagged = prop.taggedPinSystems;
            if (tagged == null || tagged.Length == 0)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugKeyLookup)}]       taggedPinSystems: NONE, this key drives no feature on pin, "
                    + "and the door is something else entirely.");
                return;
            }

            for (var i = 0; i < tagged.Length; i++)
            {
                var pair = tagged[i];
                var matches = pinGroup.HasValue && pair.propGroup == pinGroup.Value;
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugKeyLookup)}]       taggedPinSystems[{i}] on {pair.propGroup} -> "
                    + $"{Describe(pair.peckSystem)}{(matches ? "   <== MATCHES PLINTH" : string.Empty)}");
            }

            if (pinGroup.HasValue)
                return;

            Plugin.Log.LogInfo(
                $"[{nameof(DebugKeyLookup)}]       (plinth not loaded, so no pinGroup to match against, "
                + "walk to the tower to see which pair is the door.)");
        }

        // OnPeck and Apply are inlined, so the release is read from PeckEffectPropHomeSettings.
        private static void DumpKeyCustody()
        {
            var effects = UnityEngine.Object.FindObjectsByType<PeckEffectPropHomeSettings>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            Plugin.Log.LogInfo(
                $"[{nameof(DebugKeyLookup)}] key custody, {effects.Length} PeckEffectPropHomeSettings loaded:");

            foreach (var prop in Prop.allProps)
            {
                if (prop == null || !GourdRegistry.IsBigKey(prop.saveablePropName))
                    continue;

                var home = prop.currentHome;
                if (home == null)
                {
                    Plugin.Log.LogInfo(
                        $"[{nameof(DebugKeyLookup)}]   {prop.saveablePropName}: in no home at all, loose "
                        + "already, so nothing is holding it.");
                    continue;
                }

                Plugin.Log.LogInfo(
                    $"[{nameof(DebugKeyLookup)}]   {prop.saveablePropName} sits in '{home.name}' "
                    + $"({home.saveableHomeName}) | blockGrabbing={home.blockGrabbing} "
                    + $"blockPlacing={home.blockPlacing} | pinGroup {home.pinGroup}");

                // Compare both: an effect names a home by `propHome` or by `propHomeBlock`.
                var found = 0;
                foreach (var effect in effects)
                {
                    if (effect == null)
                        continue;

                    var via = (string)null;
                    if (effect.propHome != null && effect.propHome.Pointer == home.Pointer)
                        via = "propHome";
                    else if (BlockOwns(effect.propHomeBlock, home))
                        via = $"propHomeBlock '{effect.propHomeBlock.name}'";

                    if (via == null)
                        continue;

                    found++;
                    Plugin.Log.LogInfo(
                        $"[{nameof(DebugKeyLookup)}]       released by '{effect.name}' (via {via}) driven from "
                        + $"{Describe(effect.systemReference.peckSystem)} | "
                        + DescribeSettings(effect.settingsPerState));
                }

                if (found == 0)
                {
                    Plugin.Log.LogInfo(
                        $"[{nameof(DebugKeyLookup)}]       no PeckEffectPropHomeSettings names this home, by "
                        + "propHome or by propHomeBlock. Something else frees this one.");
                }

                DumpKeyAppearance(prop);
            }
        }

        private static bool BlockOwns(PropHomeBlock block, PropHome home)
        {
            if (block == null || block.homes == null)
                return false;

            for (var i = 0; i < block.homes.Length; i++)
            {
                if (block.homes[i] != null && block.homes[i].Pointer == home.Pointer)
                    return true;
            }

            return false;
        }

        // Big keys have no RewardGourd, so look for a PropertyBlockHelper or the materials.
        private static void DumpKeyAppearance(Prop prop)
        {
            var helper = prop.GetComponentInChildren<PropertyBlockHelper>(true);
            if (helper == null)
            {
                var renderers = prop.GetComponentsInChildren<Renderer>(true);
                var names = new List<string>();
                foreach (var renderer in renderers)
                {
                    if (renderer == null)
                        continue;

                    var material = renderer.sharedMaterial;
                    var materialName = material != null ? material.name : "<no material>";
                    names.Add($"'{renderer.name}'({materialName})");
                }

                var joined = string.Join(", ", names);
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugKeyLookup)}]       colour: no PropertyBlockHelper. "
                    + $"{renderers.Length} renderer(s): {joined}");
                return;
            }

            var settings = helper.colorSettings;
            if (settings == null || settings.Length == 0)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugKeyLookup)}]       colour: PropertyBlockHelper on '{helper.name}' but it "
                    + "exposes no colorSettings.");
                return;
            }

            for (var i = 0; i < settings.Length; i++)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugKeyLookup)}]       colour: '{helper.name}'.colorSettings[{i}] "
                    + $"'{settings[i].propertyName}' = {settings[i].color}");
            }
        }

        private static string DescribeSettings(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<PeckEffectPropHomeSettings.PropHomeSetting> settings)
        {
            if (settings == null || settings.Length == 0)
                return "settingsPerState is empty";

            var parts = new List<string>();
            for (var i = 0; i < settings.Length; i++)
            {
                var setting = settings[i];
                var grab = setting.blockGrabbingMask ? $"blockGrabbing:={setting.blockGrabbingValue}" : "grab untouched";
                var place = setting.blockPlacingMask ? $"blockPlacing:={setting.blockPlacingValue}" : "place untouched";
                parts.Add($"state {i} -> {grab}, {place}");
            }

            return string.Join(" | ", parts);
        }

        // Sorted by distance, to see if a key's cutting station sits behind another key's door.
        private static void DumpCuttingStations()
        {
            var localPlayer = Core.LocalPlayer.Find();
            Vector3? origin = localPlayer != null ? localPlayer.transform.position : (Vector3?)null;

            var trails = UnityEngine.Object.FindObjectsByType<UnlockTrail>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            Plugin.Log.LogInfo(
                $"[{nameof(DebugKeyLookup)}] {trails.Length} UnlockTrail(s) loaded"
                + (origin.HasValue ? ", stations sorted by distance:" : " (no local player, so no distances):"));

            foreach (var trail in trails)
            {
                if (trail == null)
                    continue;

                var stations = trail.stations;
                var finalTarget = trail.finalTarget != null ? trail.finalTarget.name : "<none>";

                Plugin.Log.LogInfo(
                    $"[{nameof(DebugKeyLookup)}]   trail '{trail.name}' | {(stations != null ? stations.Count : 0)} station(s) "
                    + $"| finalTarget '{finalTarget}'");

                if (stations == null)
                    continue;

                var rows = new List<(float distance, string line)>();
                for (var i = 0; i < stations.Count; i++)
                {
                    var station = stations[i];
                    if (station == null)
                        continue;

                    var distance = origin.HasValue
                        ? Vector3.Distance(origin.Value, station.transform.position)
                        : float.NaN;

                    var home = station.cuttingHome;
                    var homeName = home != null ? $"{home.saveableHomeName} (accepts {home.pinGroup})" : "<no cuttingHome>";

                    rows.Add((float.IsNaN(distance) ? float.MaxValue : distance,
                        $"      station index {station.stationIndex} '{station.name}' "
                        + $"| {(float.IsNaN(distance) ? "<no player>" : $"{distance:F1}m")} "
                        + $"| cuttingHome {homeName} | cutSystem {Describe(station.cutSystem.peckSystem)}"));
                }

                rows.Sort((a, b) => a.distance.CompareTo(b.distance));
                foreach (var row in rows)
                    Plugin.Log.LogInfo($"[{nameof(DebugKeyLookup)}] {row.line}");
            }
        }

        private static bool HasGroup(Prop prop, PropGroup group)
        {
            var list = prop.propGroups;
            if (list == null)
                return false;

            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == group)
                    return true;
            }

            return false;
        }

        // Falls back to the SaveIdentity guid, which SetState writes with no savableSystem.
        private static string Describe(TrackedPeckState state)
        {
            if (state == null)
                return "<none>";

            var saveKey = state.savableSystem != SavableSystem.NotSavable
                ? state.savableSystem.ToString()
                : (state.saveIdentity != null ? state.saveIdentity.saveGuid : null);

            var saved = string.IsNullOrEmpty(saveKey)
                ? "<nothing to save under>"
                : $"SaveManager['{saveKey}']={SaveManager.GetIntValue(saveKey, -12345, false)}";

            return $"'{state.label}' on '{state.name}' (savableSystem {state.savableSystem}, {saved})";
        }
    }
}
