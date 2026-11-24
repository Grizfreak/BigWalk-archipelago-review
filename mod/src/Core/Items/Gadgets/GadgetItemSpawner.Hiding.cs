using UnityEngine;

namespace BigWalkArchipelago.Core.Items
{
    internal static partial class GadgetItemSpawner
    {
        private static bool _hiddenThisWorld;

        // Kept so ReHideVanillaInstances can switch off again what Mirror switches on.
        private static readonly List<GameObject> _hidden = new();

        // Items replayed on connecting can come before the sweep, while vanilla props hold tickets.
        private static void EnsureVanillaHidden()
        {
            if (_hiddenThisWorld || !ModConfig.ArchipelagoEnabled.Value)
                return;

            HideVanillaInstances();
        }

        internal static void ForgetWorld()
        {
            _hiddenThisWorld = false;
            Spawned.Clear();
        }

        // Local on every machine: SetActive(false) keeps the network identity alive.
        internal static int HideVanillaInstances()
        {
            var hidden = 0;
            _hidden.Clear();

            // Inactive ones too: on a guest they arrive switched off and must stay that way.
            var all = UnityEngine.Object.FindObjectsByType<Prop>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all == null)
                return 0;

            foreach (var prop in all)
            {
                if (prop == null || prop.gameObject == null || IsOurs(prop.gameObject.name))
                    continue;

                var matched = MatchKind(prop.gameObject.name);
                if (matched == null || KeptInWorld.Contains(matched.Value))
                    continue;

                if (Packs.Contains(matched.Value) && PackChecks.KeepsPacksOnMap)
                    continue;

                if (IslandObjects.Contains(matched.Value) && PackChecks.KeepsObjectOnMap(prop.gameObject.name))
                    continue;

                if (FlareGuns.Contains(matched.Value))
                    continue;

                try
                {
                    var wasActive = prop.gameObject.activeSelf;
                    if (wasActive)
                        prop.gameObject.SetActive(false);

                    _hidden.Add(prop.gameObject);
                    if (wasActive)
                        hidden++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(GadgetItemSpawner)}] Could not hide a vanilla gadget prop: {ex.Message}");
                }
            }

            Plugin.Log.LogInfo(
                $"[{nameof(GadgetItemSpawner)}] Hid {hidden} vanilla gadget prop(s) from this machine's map, and is "
                + $"keeping {_hidden.Count - hidden} that were already off.");

            // An empty sweep counts once the world is loaded, or it reruns on every gadget.
            _hiddenThisWorld = _hidden.Count > 0 || WorldManager.isReadyForEffects;

            return hidden;
        }

        // The packs hidden before this machine knew they were checks, back on the map.
        internal static void ShowPacks(Func<string, bool> stays)
        {
            var shown = 0;
            for (var i = _hidden.Count - 1; i >= 0; i--)
            {
                var hidden = _hidden[i];
                var matched = hidden != null ? MatchKind(hidden.name) : null;
                if (matched == null || !Packs.Contains(matched.Value))
                    continue;

                var prop = hidden.GetComponent<Prop>();
                if (prop == null || !stays(prop.savablePropGuid ?? string.Empty))
                    continue;

                try
                {
                    hidden.SetActive(true);
                    _hidden.RemoveAt(i);
                    shown++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(GadgetItemSpawner)}] Could not show a pack: {ex.Message}");
                }
            }

            if (shown > 0)
                Plugin.Log.LogInfo($"[{nameof(GadgetItemSpawner)}] {shown} pack(s) back on the map: they are checks.");
        }

        // Gadgets hidden before this machine knew `object_sanity` keeps them, back on the map.
        internal static void ShowObjects(Func<Prop, bool> stays)
        {
            var shown = 0;
            for (var i = _hidden.Count - 1; i >= 0; i--)
            {
                var hidden = _hidden[i];
                var matched = hidden != null ? MatchKind(hidden.name) : null;
                if (matched == null || !IslandObjects.Contains(matched.Value))
                    continue;

                var prop = hidden.GetComponent<Prop>();
                if (prop == null || !stays(prop))
                    continue;

                try
                {
                    hidden.SetActive(true);
                    _hidden.RemoveAt(i);
                    shown++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(GadgetItemSpawner)}] Could not show an object: {ex.Message}");
                }
            }

            if (shown > 0)
                Plugin.Log.LogInfo($"[{nameof(GadgetItemSpawner)}] {shown} object(s) back on the map: they are checks.");
        }

        // A pack collected for its check: off this machine's map, and kept off.
        internal static void HidePack(GameObject pack)
        {
            if (pack == null)
                return;

            pack.SetActive(false);
            if (!_hidden.Contains(pack))
                _hidden.Add(pack);
        }

        // The island's own packs and flare guns (not a clone, not a template), by guid.
        internal static Dictionary<string, Prop> VanillaPacksByGuid()
        {
            var packs = new Dictionary<string, Prop>();
            foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (prop == null || prop.gameObject == null || IsOurs(prop.gameObject.name))
                    continue;

                var matched = MatchKind(prop.gameObject.name);
                var guid = prop.savablePropGuid;
                if (matched != null && (Packs.Contains(matched.Value) || FlareGuns.Contains(matched.Value)) && !string.IsNullOrEmpty(guid))
                    packs[guid] = prop;
            }

            return packs;
        }

        internal static int ReHideVanillaInstances()
        {
            var again = 0;

            foreach (var hidden in _hidden)
            {
                if (hidden == null)
                    continue;

                try
                {
                    if (!hidden.activeSelf)
                        continue;

                    hidden.SetActive(false);
                    again++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(GadgetItemSpawner)}] Could not hide a vanilla gadget prop again: {ex.Message}");
                }
            }

            if (again > 0)
                Plugin.Log.LogInfo(
                    $"[{nameof(GadgetItemSpawner)}] {again} vanilla gadget prop(s) came back; hidden again.");

            return again;
        }
    }
}
