using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal static partial class GadgetItemSpawner
    {
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

        // Random, so the look is drawn in the island's proportions; only the host draws.
        private static readonly System.Random _draw = new();

        private static int PickAtRandom(List<int> candidates)
        {
            return candidates[_draw.Next(candidates.Count)];
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
    }
}
