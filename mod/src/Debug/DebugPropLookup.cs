using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Lists the island's props (guid, walkie-talkie, useHeldSwitch) to find item candidates.
    internal static class DebugPropLookup
    {
        // Props named the same are the same prefab; only a couple of each is printed in full.
        private const int DetailPerName = 2;

        // Lamps look like scenery to the filter below, so these names widen it.
        private static readonly string[] ExtraNameHints = { "lamp", "ray", "torch", "light" };

        internal static void Dump()
        {
            Plugin.Log.LogInfo($"[{nameof(DebugPropLookup)}] === prop inventory ===");

            var props = Prop.allProps;
            if (props == null)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugPropLookup)}] Prop.allProps is null, no world loaded.");
                return;
            }

            var counts = new Dictionary<string, int>();
            var printed = new Dictionary<string, int>();
            var candidates = 0;

            // Pass one: how many of each, so the summary says what the detail is a sample of.
            for (var i = 0; i < props.Count; i++)
            {
                var prop = props[i];
                if (prop == null)
                    continue;

                var name = prop.name;
                counts[name] = counts.TryGetValue(name, out var n) ? n + 1 : 1;
            }

            Plugin.Log.LogInfo(
                $"[{nameof(DebugPropLookup)}] {props.Count} prop(s) loaded, {counts.Count} distinct name(s).");

            // Pass two: the detail; gourds and big keys are left out.
            for (var i = 0; i < props.Count; i++)
            {
                var prop = props[i];
                if (prop == null || prop.saveablePropName != SaveablePropName.notSavable)
                    continue;

                var isWalkieTalkie = prop.radioVoiceAssigner != null;
                var hasGuid = !string.IsNullOrEmpty(prop.savablePropGuid);
                var isUsable = prop.useHeldSwitch != null;
                var nameHints = ContainsAnyHint(prop.name);

                // No use, no identity, no radio and no telling name: scenery.
                if (!isWalkieTalkie && !hasGuid && !isUsable && !nameHints)
                    continue;

                candidates++;

                var name = prop.name;
                var shown = printed.TryGetValue(name, out var s) ? s : 0;
                if (shown >= DetailPerName)
                {
                    printed[name] = shown + 1;
                    continue;
                }

                printed[name] = shown + 1;

                var groups = "<none>";
                var list = prop.propGroups;
                if (list != null && list.Count > 0)
                {
                    var parts = new string[list.Count];
                    for (var j = 0; j < list.Count; j++)
                        parts[j] = list[j].ToString();
                    groups = string.Join(", ", parts);
                }

                var inInventory = hasGuid && SaveManager.GetIsInInventory(prop.savablePropGuid);

                Plugin.Log.LogInfo(
                    $"[{nameof(DebugPropLookup)}]   '{name}' (x{counts[name]}) | guid="
                    + (hasGuid ? prop.savablePropGuid : "<none>")
                    + $" | inInventory={inInventory} | walkieTalkie={isWalkieTalkie} | usableHeld={isUsable}"
                    + $" | saveType={prop.propSaveType} | groups [{groups}]");
            }

            // Said even at zero: "no candidates" and "the tool did not run" must never look alike.
            Plugin.Log.LogInfo(
                $"[{nameof(DebugPropLookup)}] {candidates} candidate prop(s), carrying a guid, a walkie-talkie or a"
                + " use-while-held switch. Anything else loaded is scenery."
                + (candidates == 0 ? " Stand somewhere with objects around and press again." : string.Empty));

            Plugin.Log.LogInfo($"[{nameof(DebugPropLookup)}] === end of prop inventory ===");
        }

        private static bool ContainsAnyHint(string name)
        {
            foreach (var hint in ExtraNameHints)
            {
                if (name.Contains(hint, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        // Each clone's homes and tickets beside its vanilla twin: a clone's ticket may clash.
        private static void DumpHomeTickets(Prop prop, string label)
        {
            var homes = prop.gameObject.GetComponentsInChildren<PropHome>(true);
            if (homes == null || homes.Length == 0)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugPropLookup)}]     {label}: no PropHome at all.");
                return;
            }

            Plugin.Log.LogInfo($"[{nameof(DebugPropLookup)}]     {label}: {homes.Length} home(s)");
            foreach (var home in homes)
            {
                if (home == null)
                    continue;

                var ticket = home.ticket;
                var known = "no";
                try
                {
                    var office = LobbyNetworking.TicketOffice.instance;
                    if (office != null && office.tickets != null && office.tickets.ContainsKey(ticket))
                        known = office.tickets[ticket] == null
                            ? "yes (null)"
                            : (office.tickets[ticket].Pointer == home.Pointer ? "yes, US" : "yes, SOMEBODY ELSE");
                }
                catch (Exception ex)
                {
                    known = $"unreadable ({ex.Message})";
                }

                var wornBy = home.parentCharacter != null ? home.parentCharacter.gameObject.name : "<nobody>";
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugPropLookup)}]       '{home.gameObject.name}' ticket={ticket} inOffice={known} "
                    + $"| saveableHomeName={home.saveableHomeName} isInventory={home.isInventory} "
                    + $"blockPlacing={home.blockPlacing} blockGrabbing={home.blockGrabbing} "
                    + $"pinGroup={home.pinGroup} "
                    + $"parentCharacter={wornBy}");
            }
        }

        // Inactive since the hiding pass, hence the Include.
        private static void DumpVanillaCounterpart(string cosmeticName)
        {
            var prefabName = cosmeticName.Replace(
                ReceivedItemSpawner.CosmeticNameSuffix, string.Empty).Trim();

            var all = UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all == null)
                return;

            foreach (var candidate in all)
            {
                if (candidate == null || candidate.gameObject == null)
                    continue;

                var name = candidate.gameObject.name;
                if (name.Contains(ReceivedItemSpawner.CosmeticNameSuffix, System.StringComparison.Ordinal)
                    || name.Contains("(AP template)", System.StringComparison.Ordinal)
                    || !name.StartsWith(prefabName, System.StringComparison.Ordinal))
                    continue;

                DumpHomeTickets(candidate, $"VANILLA '{name}' (active={candidate.gameObject.activeInHierarchy})");
                return;
            }

            Plugin.Log.LogInfo($"[{nameof(DebugPropLookup)}]     no vanilla '{prefabName}' left in the scene to compare against.");
        }

        // Every buoy and lamp prop grouped by name, material and light colour.
        private static void DumpLampCensus()
        {
            Plugin.Log.LogInfo($"[{nameof(DebugPropLookup)}] === lamp census ===");

            var all = UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all == null)
                return;

            var groups = new SortedDictionary<string, int>(System.StringComparer.Ordinal);
            var total = 0;

            foreach (var prop in all)
            {
                if (prop == null || prop.gameObject == null)
                    continue;

                var name = prop.gameObject.name;
                if (name.IndexOf("Buoy", System.StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("Lamp", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                total++;

                var role = name.Contains("(AP template)", System.StringComparison.Ordinal) ? "TEMPLATE"
                    : name.Contains(ReceivedItemSpawner.CosmeticNameSuffix, System.StringComparison.Ordinal) ? "CLONE"
                    : "vanilla";

                var baseName = name;
                var paren = baseName.IndexOf(" (", System.StringComparison.Ordinal);
                if (paren > 0)
                    baseName = baseName.Substring(0, paren);

                var key = $"{role,-8} {baseName} | active={prop.gameObject.activeInHierarchy} | {DescribeLook(prop.gameObject)}";
                groups[key] = groups.TryGetValue(key, out var count) ? count + 1 : 1;
            }

            Plugin.Log.LogInfo($"[{nameof(DebugPropLookup)}]   {total} prop(s) mention a buoy or a lamp, in {groups.Count} group(s):");
            foreach (var pair in groups)
                Plugin.Log.LogInfo($"[{nameof(DebugPropLookup)}]   {pair.Value.ToString().PadLeft(3)} x {pair.Key}");
        }

        // The emitted light, the first renderer's material, and its property block colours.
        private static string DescribeLook(GameObject root)
        {
            var parts = new List<string>();

            try
            {
                var light = root.GetComponentInChildren<Light>(true);
                parts.Add(light != null ? $"light=#{ColorUtility.ToHtmlStringRGB(light.color)}" : "light=<none>");
            }
            catch (System.Exception ex)
            {
                parts.Add($"light=<{ex.Message}>");
            }

            try
            {
                var renderers = root.GetComponentsInChildren<Renderer>(true);
                if (renderers == null || renderers.Length == 0)
                {
                    parts.Add("renderer=<none>");
                    return string.Join(" | ", parts);
                }

                var renderer = renderers[0];
                var material = renderer.sharedMaterial;
                parts.Add($"material={(material != null ? material.name : "<none>")}");

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                foreach (var property in new[] { "_Color", "_BaseColor", "_TintColor", "_EmissionColor", "_RColor" })
                {
                    if (!block.isEmpty && block.HasColor(property))
                        parts.Add($"block{property}=#{ColorUtility.ToHtmlStringRGB(block.GetColor(property))}");
                    else if (material != null && material.HasProperty(property))
                        parts.Add($"mat{property}=#{ColorUtility.ToHtmlStringRGB(material.GetColor(property))}");
                }
            }
            catch (System.Exception ex)
            {
                parts.Add($"renderer=<{ex.Message}>");
            }

            return string.Join(" | ", parts);
        }

        internal static void DumpCosmeticGadgets()
        {
            Plugin.Log.LogInfo($"[{nameof(DebugPropLookup)}] === cosmetic gadget clones ===");

            var all = UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all == null)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugPropLookup)}] No props loaded.");
                return;
            }

            var found = 0;
            foreach (var prop in all)
            {
                if (prop == null || prop.gameObject == null
                    || !prop.gameObject.name.Contains(ReceivedItemSpawner.CosmeticNameSuffix, System.StringComparison.Ordinal))
                    continue;

                found++;
                Plugin.Log.LogInfo($"[{nameof(DebugPropLookup)}]   '{prop.gameObject.name}' | active={prop.gameObject.activeInHierarchy}");

                DumpHomeTickets(prop, "CLONE");
                DumpVanillaCounterpart(prop.gameObject.name);

                var renderers = prop.gameObject.GetComponentsInChildren<Renderer>(true);
                Plugin.Log.LogInfo($"[{nameof(DebugPropLookup)}]     {renderers.Length} renderer(s):");
                foreach (var renderer in renderers)
                {
                    if (renderer == null)
                        continue;

                    var sharedMaterial = renderer.sharedMaterial;
                    var materialName = sharedMaterial != null ? sharedMaterial.name : "<null>";
                    var shaderName = sharedMaterial != null && sharedMaterial.shader != null
                        ? sharedMaterial.shader.name
                        : "<no shader>";

                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);

                    Plugin.Log.LogInfo(
                        $"[{nameof(DebugPropLookup)}]       '{renderer.gameObject.name}' | material={materialName} | shader={shaderName}"
                        + $" | propertyBlockEmpty={block.isEmpty} | lightmapIndex={renderer.lightmapIndex} | enabled={renderer.enabled}");
                }
            }

            Plugin.Log.LogInfo($"[{nameof(DebugPropLookup)}] {found} cosmetic clone(s) found.");

            DumpLampCensus();
            Plugin.Log.LogInfo($"[{nameof(DebugPropLookup)}] === end of cosmetic gadget clones ===");
        }
    }
}
