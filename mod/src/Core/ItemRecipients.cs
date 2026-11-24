using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Picks who gets a received item: free hands first, then fewest items this session. Host only.
    internal static class ItemRecipients
    {
        private static readonly Dictionary<uint, int> GivenThisSession = new();

        // Promised but not yet delivered counts as busy, so a burst spreads over several players.
        private static readonly HashSet<uint> Promised = new();

        internal static PlayerCharacter Choose()
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null)
                return null;

            var everyone = new List<PlayerCharacter>();
            var free = new List<PlayerCharacter>();
            foreach (var pc in players)
            {
                if (pc == null || pc.hands == null)
                    continue;

                everyone.Add(pc);
                if (!pc.hands.isHoldingSomething && !Promised.Contains(pc.netId))
                    free.Add(pc);
            }

            var pool = free.Count > 0 ? free : everyone;
            if (pool.Count == 0)
                return null;

            var fewest = int.MaxValue;
            foreach (var pc in pool)
                fewest = Mathf.Min(fewest, Given(pc));

            var tied = pool.FindAll(pc => Given(pc) == fewest);
            var chosen = tied[UnityEngine.Random.Range(0, tied.Count)];

            GivenThisSession[chosen.netId] = Given(chosen) + 1;
            Promised.Add(chosen.netId);
            return chosen;
        }

        // The item has reached their hands, or never will.
        internal static void Settled(PlayerCharacter pc)
        {
            if (pc != null)
                Promised.Remove(pc.netId);
        }

        internal static void Forget()
        {
            GivenThisSession.Clear();
            Promised.Clear();
        }

        private static int Given(PlayerCharacter pc)
        {
            return GivenThisSession.TryGetValue(pc.netId, out var count) ? count : 0;
        }
    }
}
