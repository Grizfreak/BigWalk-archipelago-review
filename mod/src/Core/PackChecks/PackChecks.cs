using System;
using System.Collections.Generic;
using System.Linq;
using BigWalkArchipelago.Core.Net;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Packs, fireworks, flare guns, gadgets and lights as checks. Pick-ups are caught on the host.
    internal static class PackChecks
    {
        private const string Tag = "[" + nameof(PackChecks) + "]";
        private const string LauncherName = "FireworkLauncher";

        internal const byte ModeOff = 0;
        internal const byte ModeOn = 1;
        internal const byte ModeVanilla = 2;
        internal const byte ModeSelf = 3;

        private static byte _mode;
        private static string[] _guids = Array.Empty<string>();
        private static bool _fireworkSanity;
        private static string[] _fireworkKeys = Array.Empty<string>();

        private static bool _flareGunSanity;
        private static string[] _flareGunGuids = Array.Empty<string>();

        // `every`: each gadget's first pick-up is a check; `first_pickup`: the first of each kind.
        internal const byte ObjectsOff = 0;
        internal const byte ObjectsFirstPickup = 1;
        internal const byte ObjectsEvery = 2;
        private static byte _objectMode;
        private static string[] _objectGuids = Array.Empty<string>();
        private static string[] _objectKinds = Array.Empty<string>();
        private static byte _mirroredObjectMode;

        // Kinds counted by `object_sanity_kinds`; the others leave the island while it is on.
        private static readonly HashSet<string> CountedKinds = new();
        private static readonly HashSet<string> MirroredKinds = new();

        internal static readonly string[] AllObjectKinds =
        {
            "WalkieTalkieProp", "MegaphoneProp", "FmRadioProp", "BinocularsProp", "LaserProp", "TorchProp",
            "XrayGogglesProp", "ClockProp", "CompassProp", "FoldingMapProp",
            "CowBellLargeProp", "CowBellMediumProp", "CowBellSmallProp", "SalonBrushProp", "BuoyProp", "BuoyRedProp", "BuoyLight",
        };

        // Lamps stay when their kind is left out: puzzles use them.
        private static readonly string[] StayWhenLeftOut = { "BuoyProp", "BuoyRedProp", "BuoyLight" };

        // Never taken off the island: the Salon's paintbrushes and the lamps.
        private static readonly string[] NeverTaken = { "SalonBrushProp", "BuoyProp", "BuoyRedProp", "BuoyLight" };

        private static HashSet<string> Counted => IsGuest ? MirroredKinds : CountedKinds;

        internal static IEnumerable<string> HostCountedKinds => CountedKinds;

        internal static string ObjectKindOf(string name)
        {
            foreach (var kind in AllObjectKinds)
            {
                if (name == kind || name.StartsWith(kind + " (", StringComparison.Ordinal))
                    return kind;
            }

            return null;
        }

        internal static bool KeepsObjectOnMap(string name)
        {
            var kind = ObjectKindOf(name);
            return KeepsObjectsOnMap && kind != null && Counted.Contains(kind);
        }

        internal static bool HidesObject(string name)
        {
            var kind = ObjectKindOf(name);
            return KeepsObjectsOnMap && kind != null && !Counted.Contains(kind) && Array.IndexOf(NeverTaken, kind) < 0
                   && Array.IndexOf(StayWhenLeftOut, kind) < 0;
        }

        private static byte ObjectMode => IsGuest ? (_mirrored ? _mirroredObjectMode : ObjectsOff) : _objectMode;

        internal static bool KeepsObjectsOnMap => ObjectMode != ObjectsOff;

        internal static byte HostObjectMode => _objectMode;

        private static readonly HashSet<string> Collected = new();

        private static bool _mirrored;
        private static byte _mirroredMode;

        internal static void Configure(ApSlotData slot)
        {
            _mode = slot.PackSanity switch
            {
                // Seeds older than the on/off toggle have no field; their packs stay hidden.
                "on" or "True" => ModeOn,
                "vanilla" or "False" => ModeVanilla,
                "self" => ModeSelf,
                _ => ModeOff,
            };
            _guids = slot.PickupGuids ?? Array.Empty<string>();
            _fireworkSanity = slot.FireworkSanity;
            _fireworkKeys = slot.FireworkKeys ?? Array.Empty<string>();
            _flareGunSanity = slot.FlareGunSanity;
            _flareGunGuids = slot.FlareGunGuids ?? Array.Empty<string>();
            _objectMode = slot.ObjectSanity switch
            {
                "every" => ObjectsEvery,
                "first_pickup" => ObjectsFirstPickup,
                _ => ObjectsOff,
            };
            _objectGuids = slot.ObjectGuids ?? Array.Empty<string>();
            _objectKinds = slot.ObjectKinds ?? Array.Empty<string>();
            CountedKinds.Clear();
            foreach (var kind in slot.ObjectSanityKinds ?? AllObjectKinds)
                CountedKinds.Add(kind);
            _leftOutHidden = false;

            Collected.Clear();
            if (_mode == ModeOn)
            {
                foreach (var guid in _guids)
                {
                    if (CheckTracker.IsReported(ApLocationIds.PickupPrefix + guid))
                        Collected.Add(guid);
                }
            }
            if (_objectMode == ObjectsEvery)
            {
                foreach (var guid in _objectGuids)
                {
                    if (CheckTracker.IsReported(ApLocationIds.ObjectPrefix + guid))
                        Collected.Add(guid);
                }
            }
            if (_flareGunSanity)
            {
                foreach (var guid in _flareGunGuids)
                {
                    if (CheckTracker.IsReported(ApLocationIds.FlareGunPrefix + guid))
                        Collected.Add(guid);
                }
            }

            Plugin.Log.LogInfo($"{Tag} Packs: mode {_mode}, {Collected.Count} of {_guids.Length} already collected. "
                               + $"Fireworks: {(_fireworkSanity ? _fireworkKeys.Length.ToString() : "off")}. "
                               + $"Objects: mode {_objectMode}.");
            ShowWhatStays();
        }

        private static bool IsGuest => NetworkClient.active && !NetworkServer.active;

        private static byte Mode => IsGuest ? (_mirrored ? _mirroredMode : ModeOff) : _mode;

        internal static bool KeepsPacksOnMap => Mode != ModeOff;

        internal static byte HostMode => _mode;

        internal static IEnumerable<string> HostCollected => Collected;

        // `self`: the pack is its own item and is already in the world.
        internal static bool IsPackItself(Archipelago.MultiClient.Net.Models.ItemInfo item) =>
            _mode == ModeSelf && item?.Player != null && ApRuntime.Current != null
            && item.Player.Slot == ApRuntime.Current.SlotNumber && ApLocationIds.IsPickupLocation(item.LocationId);

        internal static bool IsCollected(string guid) => Collected.Contains(guid);

        internal static void ApplyFromHost(byte mode, byte objectMode, List<string> kinds, List<string> collected)
        {
            _mirrored = true;
            _mirroredMode = mode;
            _mirroredObjectMode = objectMode;
            if (!MirroredKinds.SetEquals(kinds))
            {
                MirroredKinds.Clear();
                foreach (var kind in kinds)
                    MirroredKinds.Add(kind);
                _leftOutHidden = false;
            }
            Collected.Clear();
            foreach (var guid in collected)
                Collected.Add(guid);
            ShowWhatStays();
        }

        internal static void ForgetMirror()
        {
            _mirrored = false;
            _mirroredMode = ModeOff;
            _mirroredObjectMode = ObjectsOff;
            MirroredKinds.Clear();
            Collected.Clear();
        }

        private static void ShowWhatStays()
        {
            if (KeepsPacksOnMap)
                GadgetItemSpawner.ShowPacks(guid => !Collected.Contains(guid));
            if (KeepsObjectsOnMap)
                GadgetItemSpawner.ShowObjects(prop => !Collected.Contains(prop.savablePropGuid ?? string.Empty)
                                                      && KeepsObjectOnMap(prop.gameObject.name));
        }

        // A collected pack can show again after a world reload or a snapshot; hidden once a second.
        private static float _nextTick;
        private static Dictionary<string, Prop> _packs;

        internal static void Tick()
        {
            if (Time.unscaledTime < _nextTick)
                return;
            _nextTick = Time.unscaledTime + 1f;

            if (!WorldManager.isReadyForEffects)
            {
                _packs = null;
                _leftOutHidden = false;
                _worldReadySince = -1f;
                return;
            }

            if (_worldReadySince < 0f)
                _worldReadySince = Time.unscaledTime;

            HideLeftOutKinds();

            if (Collected.Count == 0)
                return;

            if (_packs != null)
            {
                foreach (var guid in Collected)
                {
                    if (!_packs.ContainsKey(guid))
                    {
                        _packs = null;
                        break;
                    }
                }
            }

            if (_packs == null)
            {
                _packs = GadgetItemSpawner.VanillaPacksByGuid();
                foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var id = prop != null ? prop.savablePropGuid : null;
                    if (!string.IsNullOrEmpty(id) && Collected.Contains(id) && !_packs.ContainsKey(id)
                        && !ReceivedItemSpawner.IsCosmeticClone(prop) && !prop.gameObject.name.Contains("(AP", StringComparison.Ordinal))
                        _packs[id] = prop;
                }
            }

            foreach (var guid in Collected)
            {
                if (!_packs.TryGetValue(guid, out var prop) || prop == null || !prop.gameObject.activeSelf)
                    continue;

                try
                {
                    ReceivedItemSpawner.ReleaseFromHands(prop);
                    GadgetItemSpawner.HidePack(prop.gameObject);
                    Plugin.Log.LogInfo($"{Tag} {prop.gameObject.name} {guid} collected: taken off the map.");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} Could not take {guid} off the map: {ex.Message}");
                }
            }
        }

        // For the clocks and cowbells, which the hiding pass never hid.
        private static bool _leftOutHidden;

        private static void HideLeftOutKinds()
        {
            if (_leftOutHidden || !KeepsObjectsOnMap)
                return;
            _leftOutHidden = true;

            var hidden = 0;
            foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsSortMode.None))
            {
                if (prop == null || ReceivedItemSpawner.IsCosmeticClone(prop) || prop.gameObject.name.Contains("(AP", StringComparison.Ordinal)
                    || !HidesObject(prop.gameObject.name))
                    continue;
                GadgetItemSpawner.HidePack(prop.gameObject);
                hidden++;
            }

            if (hidden > 0)
                Plugin.Log.LogInfo($"{Tag} {hidden} object(s) of kinds object_sanity_kinds leaves out taken off the map.");
        }


        internal static void OnPickedUp(Prop prop)
        {
            if (prop == null || !NetworkServer.active || _mode != ModeOn && _mode != ModeSelf && !_flareGunSanity
                && _objectMode == ObjectsOff)
                return;

            // Mod-spawned copies keep the island pack's guid and must not trigger its check.
            if (ReceivedItemSpawner.IsCosmeticClone(prop) || prop.gameObject.name.Contains("(AP", StringComparison.Ordinal))
                return;

            var guid = prop.savablePropGuid;
            if (string.IsNullOrEmpty(guid))
                return;

            if (_objectMode != ObjectsOff && OnObjectPickedUp(prop, guid))
                return;

            if (_mode == ModeSelf && Array.IndexOf(_guids, guid) >= 0)
            {
                Report(ApLocationIds.PickupPrefix + guid, $"{prop.gameObject.name} picked up, its own item");
                return;
            }

            if (_mode == ModeOn && Array.IndexOf(_guids, guid) >= 0)
                Report(ApLocationIds.PickupPrefix + guid, $"{prop.gameObject.name} picked up");
            else if (_flareGunSanity && Array.IndexOf(_flareGunGuids, guid) >= 0)
                Report(ApLocationIds.FlareGunPrefix + guid, $"{prop.gameObject.name} picked up");
            else
                return;

            // Hidden on the next tick, not inside the Command that is still handing it over.
            if (Collected.Add(guid))
            {
                _nextTick = 0f;
                ModChannel.SendSnapshotNow();
            }
        }

        private static bool OnObjectPickedUp(Prop prop, string guid)
        {
            if (_objectMode == ObjectsEvery)
            {
                var kind = ObjectKindOf(prop.gameObject.name);
                if (Array.IndexOf(_objectGuids, guid) < 0 || kind == null || !CountedKinds.Contains(kind))
                    return false;
                Report(ApLocationIds.ObjectPrefix + guid, $"{prop.gameObject.name} picked up");
                if (Array.IndexOf(NeverTaken, kind) < 0 && Collected.Add(guid))
                {
                    _nextTick = 0f;
                    ModChannel.SendSnapshotNow();
                }
                return true;
            }

            var found = ObjectKindOf(prop.gameObject.name);
            if (found == null || !CountedKinds.Contains(found) || Array.IndexOf(_objectKinds, found) < 0)
                return false;
            Report(ApLocationIds.ObjectKindPrefix + found, $"first {found} picked up");
            return true;
        }


        internal static void OnState(TrackedPeckState state, int value)
        {
            if (value == 0 || state == null || !NetworkServer.active)
                return;

            OnLight(state);

            var key = LauncherKey(state.transform);
            if (key == null)
                return;

            if (ModConfig.ArchipelagoEnabled.Value && SaveManager.GetIntValue(FiredPrefix + key, 0, false) == 0)
                SaveManager.SetIntValue(FiredPrefix + key, 1);

            if (_fireworkSanity && Array.IndexOf(_fireworkKeys, key) >= 0)
                Report(ApLocationIds.FireworkPrefix + key, $"firework launcher in {key} fired");
        }

        private static void OnLight(TrackedPeckState state)
        {
            var slot = ApRuntime.SlotData;
            if (slot == null || slot.LightSanity == "off" && slot.LightsCounted.Length == 0)
                return;

            // Loading a world resets every light to its default, which is not a player action.
            if (_worldReadySince < 0f || Time.unscaledTime - _worldReadySince < LightSettleSeconds)
                return;

            var prop = state.GetComponent<Prop>();
            string key = null;
            if (prop != null)
            {
                if (ReceivedItemSpawner.IsCosmeticClone(prop) || prop.gameObject.name.Contains("(AP", StringComparison.Ordinal))
                    return;
                key = prop.savablePropGuid;
            }
            else
            {
                key = state.transform.name;
                for (var t = state.transform.parent; t != null; t = t.parent)
                    key = t.name + "/" + key;
            }

            if (string.IsNullOrEmpty(key))
                return;

            if (Array.IndexOf(slot.LightsCounted, key) >= 0 && SaveManager.GetIntValue(LitPrefix + key, 0, false) == 0)
            {
                SaveManager.SetIntValue(LitPrefix + key, 1);
                Plugin.Log.LogInfo($"{Tag} light counted: {key}");
            }

            if (slot.LightSanity == "off" || Array.IndexOf(slot.LightKeys, key) < 0)
                return;
            if (slot.LightSanity == "lamps" && prop == null)
                return;

            Report(ApLocationIds.LightPrefix + key, $"{(prop != null ? prop.gameObject.name : "light switch")} switched on");
        }

        private const float LightSettleSeconds = 10f;
        private static float _worldReadySince = -1f;

        private const string LitPrefix = "ap_lit_";

        internal static int LightsOn()
        {
            var counted = ApRuntime.SlotData?.LightsCounted;
            if (counted == null || counted.Length == 0)
                return 0;
            var on = 0;
            foreach (var key in counted)
            {
                if (SaveManager.GetIntValue(LitPrefix + key, 0, false) != 0
                    || CheckTracker.IsReported(ApLocationIds.LightPrefix + key))
                    on++;
            }
            return on;
        }

        internal static void ClearLightLedger()
        {
            var data = SaveManager.instance != null ? SaveManager.instance.currentData : null;
            if (data?.entries == null)
                return;
            var lit = new List<string>();
            foreach (var entry in data.entries)
            {
                if (entry.value != 0 && entry.key != null && entry.key.StartsWith(LitPrefix, StringComparison.Ordinal))
                    lit.Add(entry.key);
            }
            foreach (var key in lit)
                SaveManager.SetIntValue(key, 0);
        }

        private const string FiredPrefix = "ap_firework_fired_";

        // Known to the mod too, so a launcher is recognised without a connection.
        private static readonly string[] Landmarks =
        {
            "Hub", "Lookout_RedCone", "Lookout_YellowZigzag", "Lookout_BlueTube", "Lookout_GreenHourglass",
            "PeakMarker", "SouthPeak", "FireworksPlatform_Intro",
        };

        internal static string LandmarkOf(Transform launcher)
        {
            for (var t = launcher != null ? launcher.parent : null; t != null; t = t.parent)
            {
                if (Array.IndexOf(Landmarks, t.name) >= 0)
                    return t.name;
            }

            return null;
        }

        internal static bool LauncherFired(string key)
        {
            return SaveManager.GetIntValue(FiredPrefix + key, 0, false) != 0
                   || CheckTracker.IsReported(ApLocationIds.FireworkPrefix + key);
        }

        internal static string LauncherKey(Transform transform)
        {
            var inLauncher = false;
            for (var t = transform; t != null; t = t.parent)
            {
                if (!inLauncher)
                {
                    inLauncher = t.name.StartsWith(LauncherName, StringComparison.Ordinal);
                    continue;
                }

                if (Array.IndexOf(Landmarks, t.name) >= 0)
                    return t.name;
            }

            return null;
        }

        private static void Report(string location, string what)
        {
            if (!CheckTracker.TryMarkReported(location))
                return;

            Plugin.Log.LogInfo($"{Tag} {what}: {location}.");
            Plugin.Reporter.ReportCheck(location);
        }
    }
}
