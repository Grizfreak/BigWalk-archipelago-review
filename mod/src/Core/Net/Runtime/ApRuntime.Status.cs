using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    internal partial class ApRuntime : MonoBehaviour
    {
        private static void RefreshStatusMessage()
        {
            // A non-host never connects by design, so a warning would be a lie.
            if (!NetworkServer.active)
            {
                StatusMessage = null;
                GoalLine = null;
                return;
            }

            RefreshGoalLine();

            // Otherwise a session played with the switch off shows no sign of it.
            if (!ModConfig.ArchipelagoEnabled.Value)
            {
                StatusMessage = "Archipelago: off";
                StatusIsWarning = false;
                return;
            }

            switch (Connection.Status)
            {
                case ApConnection.ConnectionStatus.Connecting:
                    StatusMessage = "Archipelago: connecting...";
                    StatusIsWarning = false;
                    break;

                case ApConnection.ConnectionStatus.Connected:
                    StatusMessage = "Archipelago: connected";
                    StatusIsWarning = false;
                    break;

                default:
                    StatusMessage = string.IsNullOrEmpty(Connection.LastError)
                        ? "Archipelago: not connected"
                        : $"Archipelago: disconnected - {Connection.LastError} (retrying)";
                    StatusIsWarning = true;
                    break;
            }
        }
    }
}
