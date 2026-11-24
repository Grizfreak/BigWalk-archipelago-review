using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    // Traps, bonuses and DeathLink: the host queues them and tells each machine what to play.
    internal static partial class Traps
    {
        private const string Tag = "[" + nameof(Traps) + "]";

        // Spacing between queued effects, and a cap so a returning slot gets no burst of traps.
        private const float Spacing = 4f;
        private const int MaxPending = 10;

        // How far from its target a player gathered by Big Meeting lands.
        private const float MeetingRadius = 2.2f;

        // DeathLink: never two sent closer than this, nor two from one player's falls.
        private const float DeathLinkCooldown = 10f;

        private static Dictionary<long, string> _items = new();
        private static int _duration = 30;
        private static bool _spareGauntlet = true;
        private static bool _sendsDeathLink;
        private static bool _receivesDeathLink;
        private static HashSet<string> _triggers = new();
        private static int _tolerance;
        private static string _deathLinkEffect = "drop";
        private static Dictionary<string, int> _roulette = new();
        private static bool _trapOnSend;
        private static bool _trapLink;

        internal static int Duration => _duration;

        internal static void Configure(ApSlotData slot)
        {
            _items = new Dictionary<long, string>(slot.EffectItems);
            _duration = Mathf.Clamp(slot.TrapDuration, 5, 600);
            _spareGauntlet = slot.TrapsSpareTheGauntlet;
            _sendsDeathLink = slot.SendsDeathLink;
            _receivesDeathLink = slot.ReceivesDeathLink;
            _triggers = new HashSet<string>(slot.DeathLinkTriggers);
            _tolerance = Math.Max(0, slot.DeathLinkTolerance);
            _deathLinkEffect = slot.DeathLinkEffect;
            _trapOnSend = slot.DeathLinkTrapOnSend;
            _trapLink = slot.TrapLink;
            _roulette = new Dictionary<string, int>();
            foreach (var pair in slot.DeathLinkRoulette)
            {
                if (Array.IndexOf(TrapEffects.Known, pair.Key) >= 0)
                    _roulette[pair.Key] = pair.Value;
            }
            _missed = 0;
            _deathLinkOnePlayer = slot.DeathLinkTarget == "one_player";
        }

        // The host's Settings > Archipelago can turn DeathLink off or override the amnesty.
        internal static bool DeathLinkOn => ModConfig.DeathLinkEnabled.Value;
        internal static bool ReceivesDeathLink => _receivesDeathLink && DeathLinkOn;
        internal static bool SeedHasDeathLink => _sendsDeathLink || _receivesDeathLink;
        internal static int SeedAmnesty => _tolerance;
        internal static int Amnesty => ModConfig.DeathLinkAmnesty.Value >= 0 ? ModConfig.DeathLinkAmnesty.Value : _tolerance;

        // Says once what the host's Settings change from the seed's, so nobody wonders.
        internal static void AnnounceOverrides()
        {
            if (!SeedHasDeathLink)
                return;

            if (!DeathLinkOn)
                Net.ApNotices.Post("DeathLink is off in the Settings (the seed has it on)", warning: true);
            else if (ModConfig.DeathLinkAmnesty.Value >= 0 && ModConfig.DeathLinkAmnesty.Value != _tolerance)
                Net.ApNotices.Post($"DeathLink amnesty {Amnesty} in the Settings (the seed says {_tolerance})", warning: true);
        }

        internal static bool TryResolveItem(long itemId, out string key)
        {
            return _items.TryGetValue(itemId - ApLocationIds.Base, out key);
        }

        internal static void ForgetWorld()
        {
            Gauntlet.Forget();
            PuzzleFailures.ForgetWorld();
            Tomatoes.Clear();
            Helmets.Clear();
            RetiringHelmets.Clear();
            TomatoStarts.Clear();
            Released.Clear();
            ActiveUntil.Clear();
        }

        internal static void ForgetGuests()
        {
            GuestMasked.Clear();
        }

        internal static string Describe()
        {
            var lines = new List<string>
            {
                $"{Pending.Count} waiting, duration {_duration}s, spare gauntlet {_spareGauntlet}, "
                + $"deathlink on={DeathLinkOn} send={_sendsDeathLink} [{string.Join(",", _triggers)}] {_missed}/{Amnesty} receive={_receivesDeathLink} ({_deathLinkEffect})",
            };

            if (NetworkServer.active)
            {
                foreach (var walker in Walkers())
                {
                    IsSpared(walker, out var why);
                    lines.Add($"  {walker.Player.gameObject.name}{(walker.Local ? " (host)" : string.Empty)} at {walker.Position:F0}"
                              + $", in gauntlet={Gauntlet.Contains(walker.Position)}, masked={IsMasked(walker)}"
                              + (why != null ? $" -> spared: {why}" : string.Empty));
                }

                foreach (var spot in TripSpots.Candidates())
                    lines.Add($"  trip spot: {spot.Label} at {spot.Point:F1}");
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}
