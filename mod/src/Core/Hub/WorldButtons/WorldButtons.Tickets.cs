using UnityEngine;

namespace BigWalkArchipelago.Core.Hub
{
    internal partial class WorldButtons
    {
        // The slots a table can have, so two slots never share a block of tickets.
        private const int MaxSlots = 40;

        // Skips blocks a scene object already holds; the scene is the same on every machine.
        private static int FreeTicketBlock(int slot)
        {
            var office = LobbyNetworking.TicketOffice.instance;
            for (var round = 0; round < 64; round++)
            {
                var first = FirstTicket + (slot + round * MaxSlots) * TicketsPerButton;
                if (first + TicketsPerButton > ushort.MaxValue)
                    break;

                var free = true;
                for (var i = 0; i < TicketsPerButton && free; i++)
                {
                    var ticket = (ushort)(first + i);
                    free = (office == null || !office.tickets.ContainsKey(ticket)) && !SceneTickets().Contains(ticket);
                }

                if (free)
                {
                    if (round > 0)
                        Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] Slot {slot}: tickets taken in the game, using block {round}.");
                    return first;
                }
            }

            return FirstTicket + slot * TicketsPerButton;
        }

        private static HashSet<ushort> _sceneTickets;
        private static float _sceneTicketsAt = -1f;

        // Disabled objects only register in OnEnable, so a hidden button's ticket looked free.
        internal static HashSet<ushort> SceneTickets()
        {
            if (_sceneTickets != null && Time.unscaledTime - _sceneTicketsAt < 5f)
                return _sceneTickets;

            var taken = new HashSet<ushort>();
            Collect<PeckSwitch>(taken, item => item.ticket);
            Collect<TrackedPeckState>(taken, item => item.ticket);
            Collect<PropHome>(taken, item => item.ticket);
            Collect<PlayerPose>(taken, item => item.ticket);
            Collect<StickyPlatform>(taken, item => item.ticket);
            _sceneTickets = taken;
            _sceneTicketsAt = Time.unscaledTime;
            return taken;
        }

        private static void Collect<T>(HashSet<ushort> taken, Func<T, ushort> ticketOf) where T : Component
        {
            foreach (var item in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // The mod's buttons hang under "AP ..." roots: a slot keeps its own block.
                if (item == null || item.transform.root.name.StartsWith("AP ", StringComparison.Ordinal))
                    continue;
                // Scene objects only, alike on every machine: run-time spawns may not be there yet.
                var identity = item.GetComponentInParent<Mirror.NetworkIdentity>(true);
                if (identity != null && identity.sceneId == 0)
                    continue;
                var ticket = ticketOf(item);
                if (ticket != 0)
                    taken.Add(ticket);
            }
        }
    }
}
