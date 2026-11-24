using UnityEngine;

namespace BigWalkArchipelago.Core.Hub
{
    internal static partial class MapTracker
    {
        private static void BuildHostKeys()
        {
            _hostKeysBuilt = true;
            HostKeys.Clear();
            var slot = ApRuntime.SlotData;
            var connection = ApRuntime.Current;

            void Add(string key, string location)
            {
                if (ApLocationIds.TryResolveLocation(location, out var id) && connection.BelongsToSlot(id))
                    HostKeys.Add((key, id));
            }

            foreach (var guid in slot.ObjectGuids)
                Add("prop:" + guid, ApLocationIds.ObjectPrefix + guid);
            foreach (var key in slot.LightKeys)
                Add((key.Contains("/") ? "switch:" : "prop:") + key, ApLocationIds.LightPrefix + key);
            foreach (var guid in slot.PickupGuids)
                Add("prop:" + guid, ApLocationIds.PickupPrefix + guid);
            foreach (var guid in slot.FlareGunGuids)
                Add("prop:" + guid, ApLocationIds.FlareGunPrefix + guid);
            foreach (var landmark in slot.FireworkKeys)
                Add("firework:" + landmark, ApLocationIds.FireworkPrefix + landmark);
            foreach (var system in RadioStations.All)
                Add("radio:" + system, system.ToString());
            for (var tower = 0; tower < MonumentOrder.TowerCount; tower++)
            {
                var prop = MonumentOrder.PropOfTower(tower);
                Add("tower:" + prop, ApLocationIds.TowerPrefix + prop);
                Add("monument:" + prop, ApLocationIds.MonumentPrefix + prop);
                Add("monumentkey:" + prop, ApLocationIds.MonumentKeyPrefix + prop);
            }
            for (var chamber = 0; chamber < 7; chamber++)
                Add("gauntlet:" + chamber, "GauntletChamber" + chamber);
            foreach (var key in GourdRegistry.BigKeyProps)
            {
                for (var index = 0; index < 5; index++)
                    Add("cut:" + key + ":" + index, ApLocationIds.CutLocationName(key, index));
                if (GourdRegistry.TryGetLocationId(key, out var deposit))
                    Add("deposit:" + key, deposit);
            }
            foreach (var flag in _map.GetComponentsInChildren<GourdFlag>(true))
            {
                if (flag != null && GourdRegistry.TryGetLocationId(flag.saveablePropName, out var puzzle))
                    Add("puzzle:" + flag.saveablePropName, puzzle);
            }
        }

        private static Dictionary<string, byte> HostStates()
        {
            if (!_hostKeysBuilt)
                BuildHostKeys();
            var logic = ApRuntime.SlotData?.Logic;
            var states = new Dictionary<string, byte>();
            foreach (var (key, id) in HostKeys)
            {
                var done = TrackerState.Checked.Contains(id);
                var inLogic = TrackerState.InLogic.Contains(id);
                var hinted = TrackerState.Hinted.Contains(id);
                var shown = !done && (inLogic || hinted || !OnlyInLogic);
                var region = logic?.RegionOf(id);
                var tunnel = region != null && region.IndexOf("Tunnel", StringComparison.OrdinalIgnoreCase) >= 0;
                states[key] = (byte)((shown ? Visible : 0) | (inLogic ? 0 : GreyBit) | (hinted ? HintBit : 0) | (tunnel ? TunnelBit : 0));
            }

            var objectives = new List<string>(ApObjectives.Keys);
            var goal = ApRuntime.SlotData?.Goal switch
            {
                "gauntlet" => "big_goodbye",
                "ending" => "big_wall",
                "second_ending" => "big_game",
                _ => null,
            };
            if (goal != null && !objectives.Contains(goal))
                objectives.Add(goal);
            foreach (var key in objectives)
            {
                if (!HasPlace(key))
                    continue;
                var done = ApObjectives.IsDone(key);
                var ending = ApObjectives.IsEnding(key);
                var open = ending && ApObjectives.OthersDone(key);
                var locked = ending && ObjectiveBoard.Locked(key);
                states["objective:" + key] = (byte)((done ? 0 : Visible) | (locked ? GreyBit : 0) | (open ? OpenBit : 0));
            }
            return states;
        }

        private static bool HasPlace(string objective) =>
            objective is "big_climb" or "big_party" or "big_doof" or "big_makeover" or "big_wall" or "big_game" or "big_goodbye";

        private static void SendToGuests()
        {
            var lines = new List<string>();
            foreach (var pair in _states)
                lines.Add(pair.Key + "\t" + pair.Value);
            lines.Sort(StringComparer.Ordinal);
            var text = string.Join("\n", lines);
            if (text == _sent && Time.unscaledTime < _sentAt + ResendSeconds)
                return;
            _sent = text;
            _sentAt = Time.unscaledTime;
            ModChannel.SendMap(text);
        }
    }
}
