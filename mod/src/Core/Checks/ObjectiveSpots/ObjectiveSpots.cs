using System.Globalization;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Checks
{
    // The place and action objectives (big_climb, party, doof, makeover), noticed by the host.
    internal static partial class ObjectiveSpots
    {
        private const string Tag = "[" + nameof(ObjectiveSpots) + "]";
        private const string LatchPrefix = "ap_objective_";

        // The party keeps its pre-1.0.3 save name, big_fiesta, so older saves still have it done.
        private static string Latch(string key) => LatchPrefix + (key == "big_party" ? "big_fiesta" : key);

        private struct Spot
        {
            internal Vector3 Centre;
            internal float Radius;
        }

        internal static bool IsSpot(string key) => key is "big_climb" or "big_party" or "big_doof" or "big_makeover";

        private static Spot? Of(string key)
        {
            if (key != "big_climb")
                return null;

            var text = ModConfig.ClimbSpot.Value;
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var parts = text.Split(',');
            if (parts.Length != 4)
                return null;

            var numbers = new float[4];
            for (var i = 0; i < 4; i++)
            {
                if (!float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[i]))
                    return null;
            }

            return new Spot { Centre = new Vector3(numbers[0], numbers[1], numbers[2]), Radius = Mathf.Max(1f, numbers[3]) };
        }

        // For the map tracker: where big_climb is.
        internal static Vector3? ClimbCentre() => Of("big_climb")?.Centre;

        internal static bool Known(string key) => key is "big_party" or "big_doof" or "big_makeover" || Of(key) != null;

        internal static bool IsDone(string key) => SaveManager.GetIntValue(Latch(key), 0, false) != 0;

        internal static void Tick()
        {
            if (!NetworkServer.active || !WorldManager.isReadyForEffects || !ApObjectives.Active)
                return;

            foreach (var key in ApObjectives.Keys)
            {
                if (!IsSpot(key) || IsDone(key))
                    continue;

                if (key == "big_party")
                {
                    CheckTheParty();
                    continue;
                }

                if (key == "big_doof")
                {
                    CheckTheDoof();
                    continue;
                }

                if (key == "big_makeover")
                {
                    CheckTheMakeover();
                    continue;
                }

                var spot = Of(key);
                if (spot == null || !Gathered(spot.Value))
                {
                    _atTheTopSince = -1f;
                    continue;
                }

                // Not on the way past: ten seconds there.
                if (_atTheTopSince < 0f)
                    _atTheTopSince = Time.unscaledTime;
                if (Time.unscaledTime - _atTheTopSince < ClimbSeconds)
                    continue;

                SaveManager.SetIntValue(Latch(key), 1);
                _atTheTopSince = -1f;
                Plugin.Log.LogInfo($"{Tag} {key} is done: a player is at {spot.Value.Centre}.");
                ApNotices.Post(key == "big_climb" ? "Big Climb: the highest point is reached" : "Big Party: the party is reached");
            }
        }

        // When the radio went into the Speaker Stage dock, in unscaled seconds.
        private const float PartySeconds = 10f;
        private const float ClimbSeconds = 10f;
        private static float _atTheTopSince = -1f;
        private static float _dockedSince = -1f;
        private static bool _loggedDocked;
        private static string _lastSeen;

        // Whether the party's ten seconds are running: a radio in the dock and its music playing.
        private static bool _partyPlaying;

        // Ticks at a count's start and its last three seconds; a running count is checked often.
        private static int _climbTicks;
        private static int _partyTicks;
        private static int _doofTicks;
        private static float _nextLook;
        private const float LookWhileCounting = 0.25f;

        internal static void TickCountdown()
        {
            if (!NetworkServer.active)
                return;

            Count(_atTheTopSince, ClimbSeconds, ref _climbTicks);
            Count(_partyPlaying ? _dockedSince : -1f, PartySeconds, ref _partyTicks);
            Count(_doofSince, DoofSeconds, ref _doofTicks);

            if ((_atTheTopSince >= 0f || _partyPlaying || _doofSince >= 0f) && Time.unscaledTime >= _nextLook)
            {
                _nextLook = Time.unscaledTime + LookWhileCounting;
                Tick();
            }
        }

        // The ticks of one count: at its start, then three, two and one second before its end.
        private static void Count(float since, float seconds, ref int played)
        {
            if (since < 0f)
            {
                played = 0;
                return;
            }

            var elapsed = Time.unscaledTime - since;
            while (played < 4 && elapsed >= (played == 0 ? 0f : seconds - 4 + played))
            {
                played++;
                ApSounds.CountdownTick();
            }
        }

        // Looked for at most every ten seconds while missing: the walk goes over every Transform.
        private static readonly System.Collections.Generic.Dictionary<string, Transform> Found = new();
        private static float _nextFind;

        private static Transform Find(string path)
        {
            if (Found.TryGetValue(path, out var known) && known != null)
                return known;

            if (Time.unscaledTime < _nextFind)
                return null;

            var wanted = new System.Collections.Generic.HashSet<string>(DoofSwitches) { DoofRoot, SalonRoot, SmokeDown, SmokeUp, path };
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t == null)
                    continue;
                var full = t.name;
                for (var p = t.parent; p != null; p = p.parent)
                    full = p.name + "/" + full;
                if (wanted.Contains(full))
                    Found[full] = t;
            }

            if (!Found.TryGetValue(path, out known) || known == null)
            {
                _nextFind = Time.unscaledTime + 10f;
                return null;
            }

            return known;
        }

        // `everyone_gathers`: every player in the place, else one is enough.
        private static bool EveryoneGathers => ApRuntime.SlotData != null && ApRuntime.SlotData.EveryoneGathers;

        private static bool Gathered(Spot spot) => Gathered(spot.Centre, spot.Radius);

        private static bool Gathered(Vector3 centre, float radius)
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null)
                return false;

            var anyone = false;
            foreach (var player in players)
            {
                if (player == null)
                    continue;

                var there = Vector3.Distance(player.transform.position, centre) <= radius;
                if (there && !EveryoneGathers)
                    return true;
                if (!there && EveryoneGathers)
                    return false;
                anyone |= there;
            }

            return anyone;
        }
    }
}
