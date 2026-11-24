using Archipelago.MultiClient.Net.Enums;

namespace BigWalkArchipelago.Core.Net
{
    internal sealed partial class ApConnection
    {
        // Free hints for these locations of this slot; the server announces each once.
        internal void ScoutAsHints(long[] locationIds)
        {
            if (_status != ConnectionStatus.Connected || locationIds.Length == 0)
                return;

            try
            {
                _session.Locations.ScoutLocationsAsync(HintCreationPolicy.CreateAndAnnounceOnce, locationIds);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Failed to scout {locationIds.Length} location(s) as hints: {ex.Message}");
            }
        }

        // Until when a server message is taken as the answer to a hint asked at the sign.
        private DateTime _hintAnswerUntil = DateTime.MinValue;

        // Like the Text Client's `!hint`: it costs the slot's hint points.
        internal void AskHint(string text)
        {
            if (_session == null || _status != ConnectionStatus.Connected || string.IsNullOrWhiteSpace(text))
                return;
            try
            {
                _hintAnswerUntil = DateTime.UtcNow.AddSeconds(10);
                _session.Say("!hint " + text.Trim());
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Could not ask for a hint: {ex.Message}");
            }
        }

        // The slot's hint points and what a hint costs (-1, -1 when unknown).
        internal (int points, int cost) HintBudget()
        {
            try
            {
                return _session != null ? (_session.RoomState.HintPoints, _session.RoomState.HintCost) : (-1, -1);
            }
            catch (Exception)
            {
                return (-1, -1);
            }
        }

        // The locations of this slot that open hints point to (anyone's item, not found yet).
        internal HashSet<long> HintedLocations()
        {
            var result = new HashSet<long>();
            var session = _session;
            if (session == null)
                return result;
            try
            {
                var me = session.ConnectionInfo.Slot;
                foreach (var hint in _hints)
                {
                    if (hint != null && !hint.Found && hint.FindingPlayer == me)
                        result.Add(hint.LocationId);
                }
            }
            catch (Exception)
            {
                // Read again at the next count.
            }
            return result;
        }

        // Open hints for this slot's items, newest last, as "item: location (finder)".
        internal List<string> OpenHintLines()
        {
            var lines = new List<string>();
            var session = _session;
            if (session == null)
                return lines;

            try
            {
                var me = session.ConnectionInfo.Slot;
                foreach (var hint in _hints)
                {
                    if (hint == null || hint.Found || hint.ReceivingPlayer != me)
                        continue;
                    var finder = session.Players.GetPlayerInfo(hint.FindingPlayer);
                    var receiver = session.Players.GetPlayerInfo(hint.ReceivingPlayer);
                    var item = session.Items.GetItemName(hint.ItemId, receiver?.Game) ?? hint.ItemId.ToString();
                    var location = session.Locations.GetLocationNameFromId(hint.LocationId, finder?.Game) ?? hint.LocationId.ToString();
                    // The slot it is in, even this one.
                    var where = finder == null ? location : $"{location} ({finder.Name})";
                    lines.Add($"{item}: {where}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Could not read the hints: {ex.Message}");
            }

            return lines;
        }
    }
}
