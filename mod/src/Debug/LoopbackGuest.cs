using HarmonyLib;
using kcp2k;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // A second local instance that joins the first as a real Kcp guest on 127.0.0.1 (Debug only).
    internal static class LoopbackGuest
    {
        internal const string GuestArgument = "--bwap-guest";

        // Join as soon as the title menu is up, instead of on Ctrl+L.
        internal const string JoinArgument = "--bwap-join";

        // A guest without the mod: Plugin.Load then only sets up the join and guest patches.
        internal const string VanillaArgument = "--bwap-vanilla";

        // Which guest this is, 1 to 3.
        internal const string IndexArgument = "--bwap-guest-index";

        // The port this guest joins instead of 7777, such as tools/netsim.py's relay.
        internal const string PortArgument = "--bwap-port";

        internal const string HostAddress = "127.0.0.1";

        private const string Tag = "[" + nameof(LoopbackGuest) + "]";

        internal static bool IsGuest { get; private set; }

        internal static bool IsVanilla { get; private set; }

        internal static bool AutoJoin { get; private set; }

        internal static int Index { get; private set; } = 1;

        // 0: the game's own port.
        internal static ushort Port { get; private set; }

        internal static string RoleName =>
            IsVanilla ? "loopback guest without the mod" : IsGuest ? "loopback guest" : "host or solo";

        internal static void DetectRole()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                if (string.Equals(argument, IndexArgument, StringComparison.OrdinalIgnoreCase)
                    && i + 1 < arguments.Length && int.TryParse(arguments[i + 1], out var index) && index >= 1)
                    Index = index;
                else if (string.Equals(argument, PortArgument, StringComparison.OrdinalIgnoreCase)
                    && i + 1 < arguments.Length && ushort.TryParse(arguments[i + 1], out var port) && port > 0)
                    Port = port;
                else if (string.Equals(argument, GuestArgument, StringComparison.OrdinalIgnoreCase))
                    IsGuest = true;
                else if (string.Equals(argument, VanillaArgument, StringComparison.OrdinalIgnoreCase))
                    IsGuest = IsVanilla = true;
                else if (string.Equals(argument, JoinArgument, StringComparison.OrdinalIgnoreCase))
                    AutoJoin = true;
            }

            AutoJoin &= IsGuest;

            Plugin.Log.LogInfo(IsGuest
                ? $"{Tag} Role: {RoleName} {Index} ({(IsVanilla ? VanillaArgument : GuestArgument)} is on the command line); "
                  + (AutoJoin ? $"joins {HostAddress} as soon as the title menu is up." : "press Ctrl+L to join.")
                  + (Port > 0 ? $" Joins port {Port} ({PortArgument})." : "")
                : $"{Tag} Role: host or solo (no {GuestArgument} on the command line).");
        }

        // Guest only, each in its own try so one failed bind does not take the plugin down.
        internal static void TryApply(Harmony harmony)
        {
            if (!IsGuest)
                return;

            // The first guest reports no identifier; the others one of their own.
            TryPatch(harmony, "the player identifier",
                () => harmony.Patch(
                    AccessTools.Method(typeof(HouseSteamManager), nameof(HouseSteamManager.TryGetLocalUserIdentifier)),
                    postfix: new HarmonyMethod(typeof(LoopbackGuest),
                        Index > 1 ? nameof(NumberedIdentifierPostfix) : nameof(IdentifierPostfix))));

        }

        private static void TryPatch(Harmony harmony, string what, Action patch)
        {
            try
            {
                patch();
                Plugin.Log.LogInfo($"{Tag} Patched {what}.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Could not patch {what}: {ex.Message}");
            }
        }

        // Both instances share one SteamID, so the guest reports none and falls back on deviceName.
        private static void IdentifierPostfix(ref bool __result)
        {
            __result = false;
        }

        // Two guests would share one deviceName, so each from the second on says its own.
        private static void NumberedIdentifierPostfix(ref bool __result, ref string __0)
        {
            __0 = $"loopback-guest-{Index}";
            __result = true;
        }

        // As JoinFriendCard.ActionJoin does it; the connecting menu matters to the authenticator.
        internal static void Join()
        {
            if (!IsGuest)
            {
                Plugin.Log.LogInfo($"{Tag} Only the loopback guest joins (tools/launch-guest.ps1 starts it).");
                return;
            }

            if (NetworkServer.active || NetworkClient.active)
            {
                Plugin.Log.LogInfo(
                    $"{Tag} Not joining: already {(NetworkServer.active ? "hosting" : "connected or connecting")}.");
                return;
            }

            SetGuestPort();
            Plugin.Log.LogInfo($"{Tag} Joining {HostAddress} over Kcp (NetworkMinder.SetTransportAndConnect)...");
            NetworkMinder.SetTransportAndConnect(HostAddress);
            ShowConnectingMenu();
        }

        // Only in this guest's process: the host's own KcpTransport keeps listening on its port.
        private static void SetGuestPort()
        {
            if (Port == 0)
                return;

            var manager = NetworkManager.singleton;
            if (manager == null)
            {
                Plugin.Log.LogWarning($"{Tag} No NetworkManager: {PortArgument} {Port} not applied.");
                return;
            }

            // The KcpTransport sits inside the MultiplexTransport, as DebugNetworkLookup shows.
            var found = new HashSet<IntPtr>();
            var kcps = new List<KcpTransport>();
            foreach (var transport in manager.GetComponentsInChildren<Transport>(true))
                AddKcp(transport, found, kcps);
            var multiplex = Transport.active?.TryCast<MultiplexTransport>();
            if (multiplex?.transports != null)
                foreach (var inner in multiplex.transports)
                    AddKcp(inner, found, kcps);

            foreach (var kcp in kcps)
            {
                var before = kcp.port;
                kcp.port = Port;
                Plugin.Log.LogInfo($"{Tag} KcpTransport on '{kcp.gameObject.name}': port {before} -> {Port} (timeout {kcp.Timeout} ms).");
            }
            if (kcps.Count == 0)
                Plugin.Log.LogWarning($"{Tag} No KcpTransport found: {PortArgument} {Port} not applied.");
        }

        private static void AddKcp(Transport transport, HashSet<IntPtr> found, List<KcpTransport> kcps)
        {
            var kcp = transport?.TryCast<KcpTransport>();
            if (kcp != null && found.Add(kcp.Pointer))
                kcps.Add(kcp);
        }

        // Beside the host, not inside them.
        private const float SummonSideOffset = 1.5f;
        private const float SummonLift = 0.5f;

        internal static void SummonGuest()
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogInfo($"{Tag} Only the host calls the guest over.");
                return;
            }

            var host = Core.LocalPlayer.Find();
            if (host == null)
            {
                Plugin.Log.LogInfo($"{Tag} No local player to call the guest to.");
                return;
            }

            var sent = BigWalkArchipelago.Core.Net.ModChannel.SendSummon(host.transform.position, host.transform.rotation);
            Plugin.Log.LogInfo(sent > 0
                ? $"{Tag} Called {sent} loopback guest(s) over to {host.transform.position}."
                : $"{Tag} No loopback guest to call: none connected from this PC has said hello.");
        }

        // The game's own player teleport; the guest owns its position, so the host sees it too.
        internal static void OnSummoned(Vector3 hostPosition, Quaternion hostRotation)
        {
            if (!IsGuest)
                return;

            var me = Core.LocalPlayer.Find();
            if (me == null || me.grease == null)
            {
                Plugin.Log.LogInfo($"{Tag} Called over by the host, but there is no local player to move yet.");
                return;
            }

            var destination = hostPosition + hostRotation * Vector3.right * SummonSideOffset + Vector3.up * SummonLift;
            var marker = new GameObject("Loopback summon point");
            try
            {
                marker.transform.SetPositionAndRotation(destination, hostRotation);
                me.grease.Teleport(marker.transform);
                Plugin.Log.LogInfo($"{Tag} Called over by the host; teleported to {destination}.");
            }
            finally
            {
                UnityEngine.Object.Destroy(marker);
            }
        }

        private static void ShowConnectingMenu()
        {
            try
            {
                var menus = MainMenuManager.instance;
                if (menus == null)
                    return;
                if (menus.titleMenu != null)
                    menus.titleMenu.gameObject.SetActive(false);
                if (menus.connectingMenu != null)
                    menus.connectingMenu.gameObject.SetActive(true);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Could not show the connecting menu: {ex.Message}");
            }
        }
    }
}
