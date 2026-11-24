using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Towers
{
    // `tower_mode`: monuments take gourds one at a time, in the seed's order; the host decides.
    internal static partial class MonumentOrder
    {
        private const string Tag = "[" + nameof(MonumentOrder) + "]";
        private const string GrantedKey = "ap_monuments_granted";
        private const string MapPrefix = "ap_monument_tower_";

        // The apworld's data.TOWERS order, which the guests' mask follows too.
        private static readonly string[] Props =
        {
            "bigKeyIntro", "bigKeyRedZone", "bigKeyGreenZone", "bigKeyBlueZone", "bigKeyYellowZone", "bigKeyBoss", "bigKeyOverflow",
        };

        private static readonly string[] Names =
        {
            "the Drawbridge", "the Red Funnel Tower", "the Green Cup Tower", "the Blue Castle Tower", "the Yellow Twist Tower",
            "the Black Tower", "the Green Dome",
        };

        private static readonly int[] Slots = { 4, 5, 5, 5, 5, 6, 15 };

        // Which `monoument0..3` is which tower is guessed, then corrected by where each one stands.
        private static readonly string[] GuessedPrefixes =
        {
            "monoumentIntro", "monoument0", "monoument1", "monoument2", "monoument3", "monoumentFinal", "monoumentOverflow",
        };

        // Inside each of the four towers, for telling the four apart.
        private static readonly Vector3[] Centres =
        {
            Vector3.zero,
            new Vector3(-88.75f, 87.11f, -608.67f),
            new Vector3(454.01f, 102.51f, -447.57f),
            new Vector3(139.55f, 102.38f, -201.39f),
            new Vector3(-229.89f, 121.50f, -239.87f),
            Vector3.zero,
            Vector3.zero,
        };

        internal static int TowerCount => Props.Length;
        internal static string PropOfTower(int tower) => Props[tower];
        internal static Vector3 CentreOf(int tower) => tower < Centres.Length ? Centres[tower] : Vector3.zero;

        internal static IEnumerable<(string prop, Vector3 centre)> TowerCentres()
        {
            for (var tower = 0; tower < Centres.Length; tower++)
            {
                if (Centres[tower] != Vector3.zero)
                    yield return (Props[tower], Centres[tower]);
            }
        }

        // On a guest: the towers open, one bit each (the order above), as the host last said.
        private static int _mirroredOpen = -1;

        private static readonly Dictionary<string, int> Learned = new();
        private static readonly HashSet<int> Blocked = new();
        private static int _lastActive = -2;

        private static bool IsGuest => NetworkClient.active && !NetworkServer.active;

        private static string Mode => DebugMode ?? (ApRuntime.SlotData != null ? ApRuntime.SlotData.TowerMode : "all_open");
        // `first_tower_open` off: with by_item, the first monument waits for an item as well.
        internal static bool DebugFirstClosed;
        private static bool FirstTowerOpen => DebugMode != null ? !DebugFirstClosed : ApRuntime.SlotData == null || ApRuntime.SlotData.FirstTowerOpen;

        private static string[] Order => DebugMode != null ? DebugOrder : ApRuntime.SlotData != null ? ApRuntime.SlotData.TowerOrder : Array.Empty<string>();

        internal static bool Active => IsGuest ? _mirroredOpen >= 0 : Mode != "all_open" && Order.Length == Props.Length;

        internal static bool IsItem(long itemId) =>
            ApRuntime.SlotData != null && ApRuntime.SlotData.MonumentItemId != 0 && itemId == ApRuntime.SlotData.MonumentItemId;

        // The monuments' homes, cached: the scene holds hundreds of homes, more with every prop.
        private static readonly List<PropHome> Monuments = new();
        private static float _monumentsFoundAt = -100f;
        private const float RefindSeconds = 30f;

        private static List<PropHome> MonumentHomes()
        {
            if (Monuments.Count > 0 && !Monuments.Exists(home => home == null)
                                    && Time.unscaledTime - _monumentsFoundAt < RefindSeconds)
                return Monuments;

            Monuments.Clear();
            _monumentsFoundAt = Time.unscaledTime;
            // Not `PropHome.allPropHomes`: the monuments are not in that list.
            foreach (var home in UnityEngine.Object.FindObjectsByType<PropHome>(FindObjectsSortMode.None))
            {
                if (ReceivedItemSpawner.IsMonumentHome(home))
                    Monuments.Add(home);
            }
            return Monuments;
        }

        internal static void Tick()
        {
            if (!WorldManager.isReadyForEffects)
                return;

            var homes = MonumentHomes();

            var active = Active;
            TickButtons(active);
            TickGreenDomeDoor();
            foreach (var home in homes)
            {
                if (home == null)
                    continue;

                var id = home.GetInstanceID();
                var closed = active && IsClosed(home);
                if (closed && !home.blockPlacing)
                {
                    home.blockPlacing = true;
                    Blocked.Add(id);
                    Plugin.Log.LogInfo($"{Tag} {home.saveableHomeName} is closed: blockPlacing on.");
                }
                else if (!closed && Blocked.Remove(id))
                {
                    home.blockPlacing = false;
                }
            }

            if (!active || IsGuest || !NetworkServer.active)
                return;

            // Said when the monument that takes gourds changes.
            var now = -1;
            foreach (var prop in Order)
            {
                var tower = IndexOfProp(prop);
                if (tower >= 0 && IsOpen(tower) && !IsFull(tower))
                {
                    now = tower;
                    break;
                }
            }

            if (now != _lastActive)
            {
                _lastActive = now;
                if (now >= 0)
                    ApNotices.Post($"The monument of {Names[now]} takes gourds now");
            }
        }
    }
}
