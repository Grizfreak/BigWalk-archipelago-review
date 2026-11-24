using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Hand props used as filler items, in data.py's FILLER_ITEMS order (TryResolveGadgetItem).
    internal enum GadgetKind
    {
        Megaphone,
        WalkieTalkie,
        Backpack,
        Belt,
        FlareGun,
        Laser,
        Binoculars,
        Compass,
        FoldingMap,
        Radio,
        GourdCarton,
        Torch,
        Lamp,
        XrayGoggles,
        FlareGunBlue,
        FlareGunGreen,
        FlareGunYellow,

        // Not an item: the Big Trip and Big Meeting tomato. Last, so no asset id or ticket moves.
        Pomodoro,

        // A white FlareGunProp clone whose shots cycle every colour. Last, for the same reason.
        RainbowFlareGun,

        // Not an item: Big Mask's blindfold helmet. Last, as the tomato.
        Blindfold,

        // Not an item: Big Time's clock, its hands on the real time. Last, as the tomato.
        Clock,

        // Not an item: Big Break's folding chair. Last, as the tomato.
        Chair,
    }

    // Clones the island's hand props as filler items; each machine hides its own vanilla copies.
    internal static class GadgetItemSpawner
    {
        private static readonly Dictionary<GadgetKind, string> PrefabNames = new()
        {
            { GadgetKind.Megaphone, "MegaphoneProp" },
            { GadgetKind.WalkieTalkie, "WalkieTalkieProp" },
            { GadgetKind.Backpack, "BackpackProp" },
            { GadgetKind.Belt, "HolsterProp" },
            { GadgetKind.FlareGun, "FlareGunProp" },
            { GadgetKind.Laser, "LaserProp" },
            { GadgetKind.Binoculars, "BinocularsProp" },
            { GadgetKind.Compass, "CompassProp" },
            { GadgetKind.FoldingMap, "FoldingMapProp" },
            { GadgetKind.Radio, "FmRadioProp" },
            { GadgetKind.GourdCarton, "GourdCartonProp" },
            { GadgetKind.Torch, "TorchProp" },
            // Left in the world (KeptInWorld): a buoy light is scenery worth keeping lit.
            { GadgetKind.Lamp, "BuoyLight" },
            { GadgetKind.XrayGoggles, "XrayGogglesProp" },
            // Distinct prefabs, not a colour variant of the base FlareGunProp.
            { GadgetKind.FlareGunBlue, "FlareGunPropBlue" },
            { GadgetKind.FlareGunGreen, "FlareGunPropGreen" },
            { GadgetKind.FlareGunYellow, "FlareGunPropYellow" },
            { GadgetKind.Pomodoro, "Pomodoro" },

            // After FlareGun, so that MatchKind still names a vanilla FlareGunProp a FlareGun.
            { GadgetKind.RainbowFlareGun, "FlareGunProp" },
            { GadgetKind.Blindfold, "BlindfoldProp" },
            { GadgetKind.Clock, "ClockProp" },
            { GadgetKind.Chair, "foldingChair" },
        };

        // Round lamps are three prefabs; only BuoyProp has a yellow light, so it lends its look.
        private static readonly Dictionary<GadgetKind, string[]> AlternatePrefabNames = new()
        {
            { GadgetKind.Lamp, new[] { "BuoyProp", "BuoyRedProp" } },
        };

        private static bool MatchesKind(GadgetKind kind, string name)
        {
            if (NameMatches(name, PrefabNames[kind]))
                return true;

            if (AlternatePrefabNames.TryGetValue(kind, out var alternates))
            {
                foreach (var alternate in alternates)
                {
                    if (NameMatches(name, alternate))
                        return true;
                }
            }

            return false;
        }

        private static readonly HashSet<GadgetKind> KeptInWorld = new()
        {
            GadgetKind.Lamp,

            // The puzzles' tomatoes stay where they are: a clone is a trap's, never one of theirs.
            GadgetKind.Pomodoro,

            // The blindfold puzzles' helmets too, for the same reason.
            GadgetKind.Blindfold,

            GadgetKind.Clock,

            GadgetKind.Chair,
        };

        internal const float TomatoSeconds = 3f;

        // Packs, flare guns and these lend no tickets, so host and guests agree before the options.
        private static readonly HashSet<GadgetKind> IslandObjects = new()
        {
            GadgetKind.Megaphone, GadgetKind.WalkieTalkie, GadgetKind.Laser, GadgetKind.Binoculars,
            GadgetKind.Compass, GadgetKind.FoldingMap, GadgetKind.Radio, GadgetKind.Torch, GadgetKind.XrayGoggles,
        };

        private static readonly HashSet<GadgetKind> Packs = new()
        {
            GadgetKind.Backpack,
            GadgetKind.Belt,
            GadgetKind.GourdCarton,
        };

        // The flare guns stay on the island whatever the options; no flare gun is filler any more.
        private static readonly HashSet<GadgetKind> FlareGuns = new()
        {
            GadgetKind.FlareGun,
            GadgetKind.FlareGunBlue,
            GadgetKind.FlareGunGreen,
            GadgetKind.FlareGunYellow,
            GadgetKind.RainbowFlareGun,
        };

        // A clone borrowing a not-yet-registered prop's ticket ends with two props on one ticket.
        private static bool LendsNoTickets(GadgetKind kind)
        {
            return KeptInWorld.Contains(kind) || Packs.Contains(kind) || FlareGuns.Contains(kind)
                || IslandObjects.Contains(kind);
        }

        // Mirror's SpawnDelegate has no payload, so the asset id encodes the kind and the instance.
        private const uint InstanceAssetIdBase = 0xB16_9C00;

        // Room for 20 kinds in one block: 0xB16_9C00 to 0xB16_9E7F.
        internal const int MaxInstancesPerKind = 32;

        // Extra slots invent tickets from the top of the range, computed alike on every machine.
        private const int MaxTicketsPerClone = 8;

        private const int ExtraTicketTop = 65535;

        private static ushort ExtraTicket(GadgetKind kind, int index, int rank)
        {
            return (ushort)(ExtraTicketTop - ((((int)kind * MaxInstancesPerKind) + index) * MaxTicketsPerClone + rank));
        }

        internal static uint AssetIdFor(GadgetKind kind, int index)
        {
            return InstanceAssetIdBase + (uint)((int)kind * MaxInstancesPerKind + index);
        }

        internal static bool TryDecodeAssetId(uint assetId, out GadgetKind kind, out int index)
        {
            kind = default;
            index = 0;
            if (assetId < InstanceAssetIdBase)
                return false;

            var offset = assetId - InstanceAssetIdBase;
            var kindValue = (int)(offset / MaxInstancesPerKind);
            if (!Enum.IsDefined(typeof(GadgetKind), kindValue))
                return false;

            kind = (GadgetKind)kindValue;
            index = (int)(offset % MaxInstancesPerKind);
            return true;
        }

        internal static IEnumerable<uint> AllAssetIds()
        {
            foreach (GadgetKind kind in Enum.GetValues(typeof(GadgetKind)))
            {
                for (var index = 0; index < MaxInstancesPerKind; index++)
                    yield return AssetIdFor(kind, index);
            }
        }

        // Templates are never spawned over the network, so they get a suffix of their own.
        private const string TemplateNameSuffix = "(AP template)";
        private const string PlaceholderNameSuffix = "(AP placeholder)";

        // In savablePropGuid order, so indices line up on every machine.
        private static readonly Dictionary<GadgetKind, List<Prop>> _templates = new();

        private static bool _hiddenThisWorld;

        // Kept so ReHideVanillaInstances can switch off again what Mirror switches on.
        private static readonly List<GameObject> _hidden = new();

        // Every machine, once per world load: each clones its own templates from its own scene.
        internal static void CaptureTemplates()
        {
            foreach (var kind in PrefabNames.Keys)
                CaptureTemplate(kind);
        }

        private static void CaptureTemplate(GadgetKind kind)
        {
            var prefabName = PrefabNames[kind];

            // Inactive ones count: on a guest the hidden props are never switched on.
            var all = UnityEngine.Object.FindObjectsByType<Prop>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all == null)
                return;

            var candidates = new List<Prop>();
            foreach (var candidate in all)
            {
                if (candidate == null || candidate.gameObject == null)
                    continue;

                if (!MatchesKind(kind, candidate.gameObject.name) || IsOurs(candidate.gameObject.name))
                    continue;

                candidates.Add(candidate);
            }

            if (candidates.Count == 0)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(GadgetItemSpawner)}] No {kind} ({prefabName}) found in the scene to capture as a template; a received one will have nothing to clone from.");
                return;
            }

            // Sorted by guid: FindObjectsByType has no order and host and clients must index alike.
            candidates.Sort(CompareByGuid);
            if (candidates.Count > MaxInstancesPerKind)
                candidates.RemoveRange(MaxInstancesPerKind, candidates.Count - MaxInstancesPerKind);

            var templates = new List<Prop>();
            foreach (var candidate in candidates)
            {
                var template = BuildTemplate(candidate, prefabName);
                if (template != null)
                    templates.Add(template);
            }

            // Re-armed on every world load, so the previous world's templates would leak.
            if (_templates.TryGetValue(kind, out var previous) && previous != null)
            {
                foreach (var old in previous)
                {
                    if (old != null && old.gameObject != null)
                        UnityEngine.Object.Destroy(old.gameObject);
                }
            }

            _templates[kind] = templates;
            Plugin.Log.LogInfo(
                $"[{nameof(GadgetItemSpawner)}] {kind}: {templates.Count} template(s) captured, one per vanilla "
                + $"'{prefabName}'{(AlternatePrefabNames.ContainsKey(kind) ? " or its alternates" : string.Empty)}.");
        }

        private static int CompareByGuid(Prop a, Prop b)
        {
            var byGuid = string.CompareOrdinal(a.savablePropGuid ?? string.Empty, b.savablePropGuid ?? string.Empty);
            return byGuid != 0 ? byGuid : string.CompareOrdinal(a.gameObject.name, b.gameObject.name);
        }

        private static Prop BuildTemplate(Prop candidate, string prefabName)
        {
            // Instantiate drops the MaterialPropertyBlock; without it the template turns pink.
            var propertyBlocks = ReceivedItemSpawner.CapturePropertyBlocks(candidate.gameObject);

            // Inactive before Instantiate, so the clone's Awake and OnEnable never run.
            var templateWasActive = candidate.gameObject.activeSelf;
            GameObject clone;
            try
            {
                candidate.gameObject.SetActive(false);
                clone = UnityEngine.Object.Instantiate(candidate.gameObject);
            }
            finally
            {
                if (templateWasActive)
                    candidate.gameObject.SetActive(true);
            }

            clone.name = $"{prefabName} {TemplateNameSuffix}";
            ReceivedItemSpawner.ApplyPropertyBlocks(clone, propertyBlocks);
            ReceivedItemSpawner.ClearLightmapReferences(clone);
            ReceivedItemSpawner.FixMaterialessRenderers(clone);
            ReceivedItemSpawner.RefreshPropertyBlockHelpers(clone);
            clone.SetActive(false);

            var identity = clone.GetComponent<NetworkIdentity>();
            if (identity != null)
                identity.sceneId = 0;

            return clone.GetComponent<Prop>();
        }

        private static int VanillaCount(GadgetKind kind)
        {
            return _templates.TryGetValue(kind, out var list) && list != null ? list.Count : 0;
        }

        // None for a kind the island keeps: its vanilla tickets are always taken.
        private static int VanillaUsable(GadgetKind kind)
        {
            return LendsNoTickets(kind) ? 0 : VanillaCount(kind);
        }

        // An extra slot walks the templates in turn, so received lamps get the island's colours.
        private static Prop ResolveTemplate(GadgetKind kind, int index, out bool extra)
        {
            var count = VanillaCount(kind);
            var usable = VanillaUsable(kind);
            extra = count > 0 && index >= usable;
            return GetTemplate(kind, extra ? (index - usable) % count : index);
        }

        // Random, so the look is drawn in the island's proportions; only the host draws.
        private static readonly System.Random _draw = new();

        private static int PickAtRandom(List<int> candidates)
        {
            return candidates[_draw.Next(candidates.Count)];
        }

        private static Prop GetTemplate(GadgetKind kind, int index = 0)
        {
            if (!_templates.TryGetValue(kind, out var list) || list == null || list.Count == 0)
                return null;

            if (index < 0 || index >= list.Count)
            {
                // Not expected, every machine loads the same scene; wrong tickets beat no clone.
                Plugin.Log.LogWarning(
                    $"[{nameof(GadgetItemSpawner)}] Asked for {kind} instance {index}, but only {list.Count} were captured here; using the first.");
                index = 0;
            }

            var template = list[index];
            return template != null ? template : null;
        }

        // Host only: reads this machine's ticket office; the answer travels in the asset id.
        private static int ChooseInstance(GadgetKind kind)
        {
            if (!_templates.TryGetValue(kind, out var list) || list == null || list.Count == 0 || list[0] == null)
                return 0;

            // Asked of every template: a kind can mix prefabs whose components differ.
            var needed = Math.Min(TicketsOf(list[0].gameObject).Count, MaxTicketsPerClone);
            var anyTicketed = list.Exists(template => template != null && TicketsOf(template.gameObject).Count > 0);
            if (!anyTicketed)
            {
                var all = new List<int>();
                for (var index = 0; index < list.Count; index++)
                    all.Add(index);
                return PickAtRandom(all);
            }

            var usable = VanillaUsable(kind);
            var candidates = new List<int>();
            for (var index = 0; index < usable; index++)
            {
                var template = list[index];
                if (template != null && TicketsOf(template.gameObject).TrueForAll(IsTicketFree))
                    candidates.Add(index);
            }

            if (candidates.Count > 0)
                return PickAtRandom(candidates);

            for (var index = usable; index < MaxInstancesPerKind; index++)
            {
                // This slot's own template: the lamp mixes three prefabs in one list.
                var template = list[(index - usable) % list.Count];
                var slotNeeds = template != null
                    ? Math.Min(TicketsOf(template.gameObject).Count, MaxTicketsPerClone)
                    : needed;

                var free = true;
                for (var rank = 0; rank < slotNeeds && free; rank++)
                    free = IsInventedTicketFree(ExtraTicket(kind, index, rank));

                if (free)
                    candidates.Add(index);
            }

            if (candidates.Count > 0)
                return PickAtRandom(candidates);

            Plugin.Log.LogWarning(
                $"[{nameof(GadgetItemSpawner)}] All {MaxInstancesPerKind} {kind} slot(s) are held by live clones; this one "
                + "will look right and hold nothing.");
            return usable < MaxInstancesPerKind ? usable : 0;
        }

        // Called while the clone is inactive, so its components register the new tickets.
        private static void AssignExtraTickets(GameObject clone, GadgetKind kind, int index)
        {
            var rank = 0;
            var skipped = 0;

            foreach (var home in clone.GetComponentsInChildren<PropHome>(true))
            {
                if (home == null || home.ticket == 0)
                    continue;

                if (rank < MaxTicketsPerClone)
                    home.ticket = ExtraTicket(kind, index, rank);
                else
                    skipped++;
                rank++;
            }

            foreach (var peck in clone.GetComponentsInChildren<PeckSwitch>(true))
            {
                if (peck == null || peck.ticket == 0)
                    continue;

                if (rank < MaxTicketsPerClone)
                    peck.ticket = ExtraTicket(kind, index, rank);
                else
                    skipped++;
                rank++;
            }

            foreach (var state in clone.GetComponentsInChildren<TrackedPeckState>(true))
            {
                if (state == null || state.ticket == 0)
                    continue;

                if (rank < MaxTicketsPerClone)
                    state.ticket = ExtraTicket(kind, index, rank);
                else
                    skipped++;
                rank++;
            }

            // Seats too: with the template's ticket, sitting on the island's chair moved you to it.
            foreach (var pose in clone.GetComponentsInChildren<PlayerPose>(true))
            {
                if (pose == null || pose.ticket == 0)
                    continue;

                if (rank < MaxTicketsPerClone)
                {
                    pose.ticket = ExtraTicket(kind, index, rank);
                    // CmdEnterPose carries a ticket of its own, the template's unless set.
                    pose.shellReference = new(pose.ticket);
                }
                else
                    skipped++;
                rank++;
            }

            var why = LendsNoTickets(kind)
                ? "a kind whose vanilla instances keep their tickets, so it never borrows one"
                : $"past the {VanillaCount(kind)} vanilla instance(s)";
            Plugin.Log.LogInfo(
                $"[{nameof(GadgetItemSpawner)}] {kind} #{index} is an extra slot ({why}): gave it "
                + $"{Math.Min(rank, MaxTicketsPerClone)} invented ticket(s) from {ExtraTicket(kind, index, 0)} down.");

            if (skipped > 0)
                Plugin.Log.LogWarning(
                    $"[{nameof(GadgetItemSpawner)}] {kind} #{index} has {skipped} more ticketed component(s) than the "
                    + $"{MaxTicketsPerClone} a slot reserves; those keep the template's ticket and will not work.");
        }

        // Ticket 0 has no identity to protect; counting it would make every such kind look taken.
        private static List<ushort> TicketsOf(GameObject root)
        {
            var tickets = new List<ushort>();

            foreach (var home in root.GetComponentsInChildren<PropHome>(true))
            {
                if (home != null && home.ticket != 0)
                    tickets.Add(home.ticket);
            }

            foreach (var peck in root.GetComponentsInChildren<PeckSwitch>(true))
            {
                if (peck != null && peck.ticket != 0)
                    tickets.Add(peck.ticket);
            }

            foreach (var state in root.GetComponentsInChildren<TrackedPeckState>(true))
            {
                if (state != null && state.ticket != 0)
                    tickets.Add(state.ticket);
            }

            foreach (var pose in root.GetComponentsInChildren<PlayerPose>(true))
            {
                if (pose != null && pose.ticket != 0)
                    tickets.Add(pose.ticket);
            }

            return tickets;
        }

        // A switched-off scene object (a hidden Memory Bombs mine) holds its ticket off the office.
        private static bool IsInventedTicketFree(ushort ticket)
        {
            return IsTicketFree(ticket) && !WorldButtons.SceneTickets().Contains(ticket);
        }

        private static bool IsTicketFree(ushort ticket)
        {
            try
            {
                var office = LobbyNetworking.TicketOffice.instance;
                return office == null || office.tickets == null || !office.tickets.ContainsKey(ticket);
            }
            catch (Exception)
            {
                return true;
            }
        }

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

        // Our copies of each kind in this world, oldest first, for ModConfig.GadgetCopiesKept.
        private static readonly Dictionary<GadgetKind, List<Prop>> Spawned = new();

        // A copy past the limit replaces the oldest one lying loose. Packs are left alone.
        private static void KeepUnderLimit(GadgetKind kind, Prop added)
        {
            if (!Spawned.TryGetValue(kind, out var list))
                Spawned[kind] = list = new List<Prop>();
            list.RemoveAll(prop => prop == null);
            list.Add(added);

            var limit = ModConfig.GadgetCopiesKept.Value;
            if (limit <= 0 || Packs.Contains(kind))
                return;

            var removed = 0;
            for (var i = 0; i < list.Count - 1 && list.Count > limit;)
            {
                var prop = list[i];
                if (prop.currentHome != null || IsHeldByAnyone(prop))
                {
                    i++;
                    continue;
                }

                list.RemoveAt(i);
                try
                {
                    NetworkServer.Destroy(prop.gameObject);
                    removed++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(GadgetItemSpawner)}] Could not remove an old {kind}: {ex.Message}");
                }
            }

            if (removed > 0)
                Plugin.Log.LogInfo(
                    $"[{nameof(GadgetItemSpawner)}] {removed} old {kind}(s) removed to keep {limit} in the world.");
        }

        private static bool IsHeldByAnyone(Prop prop)
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null)
                return false;

            foreach (var pc in players)
            {
                if (pc != null && pc.hands != null && pc.hands.heldProp == prop)
                    return true;
            }

            return false;
        }

        private static float _nextLateCapture;

        private const float LateCaptureCooldownSeconds = 1f;

        // Captures every kind at once: a spawn wave comes in one burst, so one scan serves it.
        private static void TryCaptureLate(GadgetKind kind)
        {
            if (Time.time < _nextLateCapture)
                return;

            _nextLateCapture = Time.time + LateCaptureCooldownSeconds;

            Plugin.Log.LogInfo(
                $"[{nameof(GadgetItemSpawner)}] An incoming {kind} arrived before the world-ready sweep; capturing "
                + "the templates now.");
            CaptureTemplates();
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

        // Counted so the rebuild does not restock what the sweep left standing.
        private static void CountKept(Dictionary<GadgetKind, int> kept, Prop prop)
        {
            var kind = MatchKind(prop.gameObject.name);
            if (kind == null)
                return;

            kept[kind.Value] = kept.TryGetValue(kind.Value, out var count) ? count + 1 : 1;
        }

        private static GadgetKind? MatchKind(string name)
        {
            foreach (var pair in PrefabNames)
            {
                if (NameMatches(name, pair.Value))
                    return pair.Key;
            }

            return null;
        }

        // "FlareGunProp" must not also match "FlareGunPropBlue".
        private static bool NameMatches(string name, string exact)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return name == exact || name.StartsWith(exact + " ", StringComparison.Ordinal);
        }

        private static bool IsOurs(string name)
        {
            return name.Contains(TemplateNameSuffix, StringComparison.Ordinal)
                || name.Contains(ReceivedItemSpawner.CosmeticNameSuffix, StringComparison.Ordinal);
        }

        // Only loose props go: anything in a home (worn, stowed) or holding one stays.
        internal static int DestroyLooseCosmeticGadgets(out Dictionary<GadgetKind, int> kept)
        {
            kept = new Dictionary<GadgetKind, int>();
            var destroyed = 0;
            var seen = 0;
            var placed = 0;

            // Inactive included: a picked-up prop can leave the active hierarchy.
            var all = UnityEngine.Object.FindObjectsByType<Prop>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (all == null)
                return 0;

            var candidates = new List<Prop>();
            var occupiedHosts = new HashSet<int>();

            foreach (var prop in all)
            {
                if (prop == null || !ReceivedItemSpawner.IsCosmeticClone(prop))
                    continue;

                // A gourd is not swept here, but it can keep its backpack or carton.
                if (prop.GetComponent<RewardGourd>() != null)
                {
                    var gourdHome = prop.currentHome;
                    if (gourdHome != null && gourdHome.parentProp != null)
                        occupiedHosts.Add(gourdHome.parentProp.GetInstanceID());
                    continue;
                }

                seen++;

                var home = prop.currentHome;
                if (home == null)
                {
                    candidates.Add(prop);
                    continue;
                }

                placed++;
                CountKept(kept, prop);

                // Its holder stays too, or its contents would be homed in a destroyed object.
                if (home.parentProp != null)
                    occupiedHosts.Add(home.parentProp.GetInstanceID());
            }

            foreach (var prop in candidates)
            {
                if (occupiedHosts.Contains(prop.GetInstanceID()))
                {
                    placed++;
                    CountKept(kept, prop);
                    continue;
                }

                // Tidying the hands is best effort; removing the prop is not.
                ReceivedItemSpawner.ReleaseFromHands(prop);

                try
                {
                    NetworkServer.Destroy(prop.gameObject);
                    destroyed++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(GadgetItemSpawner)}] Could not remove a cosmetic gadget: {ex.Message}");
                }
            }

            if (placed > 0)
                Plugin.Log.LogInfo(
                    $"[{nameof(GadgetItemSpawner)}] {placed} cosmetic gadget(s) left alone: worn, stowed or holding "
                    + "something that is.");

            seen -= placed;

            if (destroyed != seen)
                Plugin.Log.LogWarning(
                    $"[{nameof(GadgetItemSpawner)}] {seen} loose cosmetic gadget(s) found but only {destroyed} removed; "
                    + "the rebuild will put back fewer than are owed.");

            return destroyed;
        }

        // Host entry point; toPlayer is false for the session-start replay, stocked at the hub.
        internal static GameObject SpawnCosmeticPickup(GadgetKind kind, bool toPlayer)
        {
            using var activity = ModActivity.Enter("cosmetic gadget spawn");
            if (!NetworkServer.active)
                return null;

            try
            {
                var recipient = toPlayer ? ItemRecipients.Choose() : null;
                var resolved = ReceivedItemSpawner.ResolveSpawnPosition(toPlayer, recipient);
                if (resolved == null)
                {
                    ItemRecipients.Settled(recipient);
                    Plugin.Log.LogInfo(
                        $"[{nameof(GadgetItemSpawner)}] Nowhere to put a {kind} yet (no player and no InventorySpawn loaded), cosmetic spawn skipped.");
                    return null;
                }

                EnsureVanillaHidden();
                if (GetTemplate(kind) == null)
                    TryCaptureLate(kind);

                var index = ChooseInstance(kind);
                var prop = BuildNeutralizedClone(kind, resolved.Value, Quaternion.identity, index);
                if (prop == null)
                {
                    ItemRecipients.Settled(recipient);
                    return null;
                }

                NetworkServer.Spawn(prop.gameObject, AssetIdFor(kind, index));

                // Otherwise the clone stays fixed in its original resting pose.
                prop.SetLoose();
                PublishLooseStates(prop.gameObject);

                ReceivedItemSpawner.QueueHandover(prop, recipient);
                KeepUnderLimit(kind, prop);

                var identity = prop.GetComponent<NetworkIdentity>();
                ColourPainter.PaintCopy(prop.gameObject, identity != null ? identity.netId : 0);
                Plugin.Log.LogInfo(
                    $"[{nameof(GadgetItemSpawner)}] Cosmetic {kind} #{index} spawned at {prop.transform.position} (netId {(identity != null ? identity.netId : 0)}).");
                return prop.gameObject;
            }
            catch (Exception ex)
            {
                // Must never make the item reception fail.
                Plugin.Log.LogWarning($"[{nameof(GadgetItemSpawner)}] Cosmetic spawn failed, ignored: {ex}");
                return null;
            }
        }

        // Points the tomato's tick and pop at the clone's own state before the clone wakes.
        private static void WireTomatoEffects(GameObject clone)
        {
            var own = clone.GetComponent<TrackedPeckState>();
            if (own == null)
                return;

            var notes = new List<string>();
            foreach (var audio in clone.GetComponentsInChildren<PeckEffectAudio>(true))
            {
                var reference = audio.systemReference;
                notes.Add($"audio listened to {(reference.peckSystem == null ? "nothing" : reference.peckSystem == own ? "the clone" : reference.peckSystem.gameObject.name)}");
                reference.peckSystem = own;
                audio.systemReference = reference;

                // Played at the tomato: a guest gets the state change without the context's prop.
                var where = audio.transformReference;
                where.referenceType = PeckTransformReference.ReferenceType.Custom;
                where.customTransform = clone.transform;
                audio.transformReference = where;
            }

            foreach (var particle in clone.GetComponentsInChildren<PeckEffectParticleNetworked>(true))
            {
                var reference = particle.systemReference;
                notes.Add($"pop listened to {(reference.peckSystem == null ? "nothing" : reference.peckSystem == own ? "the clone" : reference.peckSystem.gameObject.name)}");
                reference.peckSystem = own;
                particle.systemReference = reference;
            }

            Plugin.Log.LogInfo($"[{nameof(GadgetItemSpawner)}] Tomato clone: {string.Join(", ", notes)}; all on the clone now.");
        }

        // Host: one of `kind` set down upright at a place (Big Break's chair). Null on failure.
        internal static Prop SpawnAt(GadgetKind kind, Vector3 position, Quaternion rotation)
        {
            if (!NetworkServer.active)
                return null;

            try
            {
                if (GetTemplate(kind) == null)
                    TryCaptureLate(kind);

                var index = ChooseInstance(kind);
                var prop = BuildNeutralizedClone(kind, position, rotation, index);
                if (prop == null)
                    return null;

                // Lowest point on the spot, not the pivot; meshes answer while inactive.
                var lowest = float.MaxValue;
                foreach (var filter in prop.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = filter != null ? filter.sharedMesh : null;
                    if (mesh == null)
                        continue;
                    var bounds = mesh.bounds;
                    for (var i = 0; i < 8; i++)
                    {
                        var corner = bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                        lowest = Mathf.Min(lowest, filter.transform.TransformPoint(corner).y);
                    }
                }
                if (lowest < float.MaxValue)
                    prop.transform.position += Vector3.up * (position.y - lowest + 0.02f);

                NetworkServer.Spawn(prop.gameObject, AssetIdFor(kind, index));
                prop.SetLoose();
                PublishLooseStates(prop.gameObject);
                return prop;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(GadgetItemSpawner)}] Could not set down a {kind}: {ex.Message}");
                return null;
            }
        }

        internal static Prop SpawnFor(GadgetKind kind, PlayerCharacter player, bool toHands = true)
        {
            if (!NetworkServer.active || player == null)
                return null;

            try
            {
                if (GetTemplate(kind) == null)
                    TryCaptureLate(kind);

                var t = player.transform;
                var position = t.position + t.forward * 1.2f + Vector3.up * 0.8f;
                var index = ChooseInstance(kind);
                var prop = BuildNeutralizedClone(kind, position, Quaternion.identity, index);
                if (prop == null)
                    return null;

                NetworkServer.Spawn(prop.gameObject, AssetIdFor(kind, index));
                prop.SetLoose();
                PublishLooseStates(prop.gameObject);

                if (toHands && player.hands != null && !player.hands.isHoldingSomething)
                    ReceivedItemSpawner.QueueHandover(prop, player);

                return prop;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(GadgetItemSpawner)}] Could not give {player.gameObject.name} a {kind}: {ex.Message}");
                return null;
            }
        }

        // Builds the clone without spawning it, so GadgetSpawnHandler can do the same on a client.
        internal static Prop BuildNeutralizedClone(GadgetKind kind, Vector3 position, Quaternion rotation, int index = 0)
        {
            var template = ResolveTemplate(kind, index, out var extra);
            if (template == null)
            {
                // A guest gets Mirror's spawn wave before the world is ready, so capture now.
                TryCaptureLate(kind);
                template = ResolveTemplate(kind, index, out extra);
            }

            if (template == null)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(GadgetItemSpawner)}] No {kind} template captured yet, cosmetic spawn skipped.");
                return null;
            }

            // Instantiate does not carry a MaterialPropertyBlock over.
            var templatePropertyBlocks = ReceivedItemSpawner.CapturePropertyBlocks(template.gameObject);

            var clone = UnityEngine.Object.Instantiate(template.gameObject, position, rotation);
            clone.name = $"{PrefabNames[kind]} {ReceivedItemSpawner.CosmeticNameSuffix}";
            ReceivedItemSpawner.ApplyPropertyBlocks(clone, templatePropertyBlocks);

            // Before activation: tickets register in OnEnable, and a refused one never asks again.
            if (extra)
                AssignExtraTickets(clone, kind, index);

            var cloneIdentity = clone.GetComponent<NetworkIdentity>();
            if (cloneIdentity != null)
            {
                cloneIdentity.sceneId = 0;
                ReceivedItemSpawner.GetPrivatePropertySetter<NetworkIdentity>("hasSpawned")
                    ?.Invoke(cloneIdentity, new object[] { false });
            }

            var prop = clone.GetComponent<Prop>();
            if (prop == null)
            {
                Plugin.Log.LogWarning($"[{nameof(GadgetItemSpawner)}] Clone without a Prop, destroying and aborting.");
                UnityEngine.Object.Destroy(clone);
                return null;
            }

            ReceivedItemSpawner.NeutralizeProgression(prop);
            StartLooseNotHung(clone);

            // Big Mask's helmet cannot be taken off by anyone.
            if (kind == GadgetKind.Blindfold)
                prop.blockRemovingFromHomes = true;

            // The tomato timer is local (PeckEffectTimer), so every machine's clone is set here.
            if (kind == GadgetKind.Pomodoro)
            {
                foreach (var timer in clone.GetComponentsInChildren<PeckEffectTimer>(true))
                    timer.duration = TomatoSeconds;
                WireTomatoEffects(clone);
            }

            clone.SetActive(true);

            if (cloneIdentity != null)
                ReceivedItemSpawner.GetPrivatePropertySetter<NetworkIdentity>(nameof(NetworkIdentity.SpawnedFromInstantiate))
                    ?.Invoke(cloneIdentity, new object[] { false });

            // After activation: something re-enables the materialless renderer on wake.
            ReceivedItemSpawner.ClearLightmapReferences(clone);
            ReceivedItemSpawner.FixMaterialessRenderers(clone);
            ReceivedItemSpawner.RefreshPropertyBlockHelpers(clone);
            SettleOffTheHanger(clone);

            // After the helpers' refresh, which would put a buoy's tint back.
            if (kind == GadgetKind.Lamp || FlareGuns.Contains(kind))
                ColourPainter.PaintClone(kind, clone, kind == GadgetKind.RainbowFlareGun);

            return prop;
        }

        // A clone copies a template still on its hanger, so its states are reset to lying loose.
        private static readonly Dictionary<string, int> LooseStates = new()
        {
            { "placed on hanger", 0 },
            { "backpack not worn", 1 },
            { "backpackWornSystem", 0 },
        };

        // Save identity removed, or the clone reads and writes the vanilla prop's saved states.
        private static void StartLooseNotHung(GameObject clone)
        {
            foreach (var state in clone.GetComponentsInChildren<TrackedPeckState>(true))
            {
                if (state == null)
                    continue;

                state.saveIdentity = null;

                if (state.label == null || !LooseStates.TryGetValue(state.label, out var value))
                    continue;

                try
                {
                    var context = state.currentPeckContext;
                    context.state = value;
                    state.currentPeckContext = context;

                    var predicted = state.peckContextForPrediction;
                    predicted.state = value;
                    state.peckContextForPrediction = predicted;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(GadgetItemSpawner)}] Could not set '{state.label}' on a cloned {clone.name}: {ex.Message}");
                }
            }
        }

        // The server holds the borrowed ticket's last state, so the host publishes the loose ones.
        private static void PublishLooseStates(GameObject clone)
        {
            if (!NetworkServer.active)
                return;

            foreach (var state in clone.GetComponentsInChildren<TrackedPeckState>(true))
            {
                if (state == null || state.label == null || !LooseStates.TryGetValue(state.label, out var value))
                    continue;

                try
                {
                    state.SetState(value);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(GadgetItemSpawner)}] Could not publish '{state.label}' for {clone.name}: {ex.Message}");
                }
            }
        }

        private static void SettleOffTheHanger(GameObject clone)
        {
            foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true))
            {
                var hung = renderer != null ? renderer.gameObject : null;
                if (hung == null || !hung.name.EndsWith("_hung", StringComparison.Ordinal) || hung.transform.parent == null)
                    continue;

                var closedName = hung.name.Substring(0, hung.name.Length - "_hung".Length) + "_closed";
                var closed = hung.transform.parent.Find(closedName);
                if (closed == null)
                    continue;

                hung.SetActive(false);
                closed.gameObject.SetActive(true);
            }
        }

        // Never null: an unresolved netId breaks that player's network state for the session.
        internal static Prop BuildStandInClone(GadgetKind wanted, Vector3 position, Quaternion rotation)
        {
            foreach (var kind in StandInOrder())
            {
                if (kind == wanted || GetTemplate(kind) == null)
                    continue;

                var prop = BuildNeutralizedClone(kind, position, rotation);
                if (prop == null)
                    continue;

                Plugin.Log.LogWarning(
                    $"[{nameof(GadgetItemSpawner)}] Nothing to build an incoming {wanted} from; stood a {kind} in "
                    + "its place so the spawn resolves. It will look wrong for this player.");
                return prop;
            }

            return null;
        }

        private static IEnumerable<GadgetKind> StandInOrder()
        {
            // The Lamp first: the one kind the hiding pass spares.
            yield return GadgetKind.Lamp;

            foreach (var kind in PrefabNames.Keys)
                yield return kind;
        }

        // Just a NetworkIdentity, so Mirror has a netId to resolve.
        internal static GameObject BuildResolvablePlaceholder(GadgetKind wanted, Vector3 position)
        {
            try
            {
                var placeholder = new GameObject($"{wanted} {PlaceholderNameSuffix}");
                placeholder.transform.position = position;
                placeholder.AddComponent<NetworkIdentity>();

                Plugin.Log.LogWarning(
                    $"[{nameof(GadgetItemSpawner)}] Nothing at all to build an incoming {wanted} from, not even a "
                    + "stand-in; spawned an empty placeholder so this client's network state stays readable.");
                return placeholder;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(GadgetItemSpawner)}] Could not even place a placeholder for an incoming {wanted}: {ex.Message}");
                return null;
            }
        }
    }
}
