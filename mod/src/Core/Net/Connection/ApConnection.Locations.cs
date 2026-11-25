namespace BigWalkArchipelago.Core.Net
{
    internal sealed partial class ApConnection
    {
        // This slot's number in the multiworld; -1 before a login.
        internal int SlotNumber => _session?.ConnectionInfo?.Slot ?? -1;

        // Snapshotted at login: a slot's locations are fixed at generation.
        internal bool BelongsToSlot(long locationId)
        {
            return _slotLocations.Contains(locationId);
        }

        // The datapackage name for a location; falls back to the number when the lookup fails.
        internal string LocationName(long locationId)
        {
            try
            {
                var name = _session?.Locations.GetLocationNameFromId(locationId);
                return string.IsNullOrEmpty(name) ? locationId.ToString() : name;
            }
            catch (Exception)
            {
                return locationId.ToString();
            }
        }

        internal void SendChecks(long[] locationIds)
        {
            if (_status != ConnectionStatus.Connected || locationIds.Length == 0)
                return;

            try
            {
                _session.Locations.CompleteLocationChecks(locationIds);
            }
            catch (Exception ex)
            {
                // Not lost: reported checks are saved and the whole set is resent on reconnect.
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Failed to send {locationIds.Length} check(s), will resend on reconnect: {ex.Message}");
            }
        }

        // Whether the server took it, so the caller can keep the goal armed on a dead socket.
        internal bool SendGoal()
        {
            if (_status != ConnectionStatus.Connected)
                return false;

            try
            {
                _session.SetGoalAchieved();
                Plugin.Log.LogInfo($"[{nameof(ApConnection)}] Goal reported to the server.");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Failed to report the goal: {ex.Message}");
                return false;
            }
        }

        // The locations of this slot the server knows as checked (a copy; empty if offline).
        internal HashSet<long> CheckedLocations()
        {
            try
            {
                return _session != null ? new HashSet<long>(_session.Locations.AllLocationsChecked) : new HashSet<long>();
            }
            catch (Exception)
            {
                // The socket's thread was adding one: the next look will have it.
                return new HashSet<long>();
            }
        }

        // How many of this slot's locations are not checked yet.
        internal int UncheckedCount(HashSet<long> checkedIds)
        {
            var count = 0;
            foreach (var id in _slotLocations)
            {
                if (!checkedIds.Contains(id))
                    count++;
            }
            return count;
        }
    }
}
