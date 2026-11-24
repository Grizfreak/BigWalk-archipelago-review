using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Loopback guest only: joins on --bwap-join and logs every step of the connection.
    internal class LoopbackGuestMonitor : MonoBehaviour
    {
        // Required by Il2CppInterop for any type injected into IL2CPP.
        public LoopbackGuestMonitor(IntPtr ptr) : base(ptr)
        {
        }

        private const string Tag = "[" + nameof(LoopbackGuestMonitor) + "]";

        private bool _autoJoinDone;
        private string _lastState;
        private float _stateSince;
        private bool _errorLogged;

        private void Update()
        {
            try
            {
                if (LoopbackGuest.AutoJoin && !_autoJoinDone && TitleMenuIsUp())
                {
                    _autoJoinDone = true;
                    Plugin.Log.LogInfo($"{Tag} Title menu is up; joining on my own ({LoopbackGuest.JoinArgument}).");
                    LoopbackGuest.Join();
                }

                var state = CurrentState();
                if (state != _lastState)
                {
                    var now = Time.realtimeSinceStartup;
                    var after = _lastState == null ? "" : $" (after {now - _stateSince:0.0}s as '{_lastState}')";
                    Plugin.Log.LogInfo($"{Tag} Connection: {state}{after}.");
                    _lastState = state;
                    _stateSince = now;
                }
            }
            catch (Exception ex)
            {
                // Once: an Update that throws does so every frame.
                if (!_errorLogged)
                {
                    _errorLogged = true;
                    Plugin.Log.LogWarning($"{Tag} Update threw (logged once): {ex.Message}");
                }
            }
        }

        // Throws before the menu scene exists; catch all, the game's NRE is an Il2CppException.
        private static bool TitleMenuIsUp()
        {
            try
            {
                var menus = MainMenuManager.instance;
                return menus != null && menus.titleMenu != null && menus.titleMenu.gameObject.activeInHierarchy;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string CurrentState()
        {
            if (NetworkServer.active)
                return "hosting (this is not what a loopback guest is for)";
            if (!NetworkClient.active)
                return "offline";
            if (!NetworkClient.isConnected)
                return "connecting";

            var connection = NetworkClient.connection;
            if (connection == null || !connection.isAuthenticated)
                return "connected, not authenticated yet";
            if (!NetworkClient.ready)
                return "authenticated, not ready yet";
            if (NetworkClient.localPlayer == null)
                return "ready, waiting for the player object";
            return $"in the world (local player netId {NetworkClient.localPlayer.netId})";
        }
    }
}
