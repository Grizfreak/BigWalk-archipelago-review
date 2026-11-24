using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    internal partial class ApRuntime : MonoBehaviour
    {
        // A trap from this world's pool, sent to the other games (TrapLink).
        internal static bool SendTrapLink(string trapName) => Connection.SendTrapLink(trapName);

        internal static void ApplyDeathLinkSetting()
        {
            Connection.ListenForDeathLinks(Traps.ReceivesDeathLink);
            Plugin.Log.LogInfo($"[{nameof(ApRuntime)}] DeathLink {(Traps.DeathLinkOn ? "on (as the seed says)" : "off")} in the settings.");
        }

        private static float _lastDeathLink = -10f;

        internal static void SendDeathLink()
        {
            if (Time.unscaledTime - _lastDeathLink < 3f)
                return;

            _lastDeathLink = Time.unscaledTime;
            SendDeathLink("pressed the DeathLink key", announce: true);
        }

        internal static void SendDeathLink(string what, bool announce = false)
        {
            var cause = $"{Connection.SlotName} {what}";
            var sent = Connection.SendDeathLink(cause);
            if (sent)
                ApSounds.DeathLinkAlert();
            if (announce)
                ApNotices.Post(sent ? $"DeathLink sent: {what}" : "DeathLink not sent: not connected", warning: !sent);
            Plugin.Log.LogInfo($"[{nameof(ApRuntime)}] DeathLink {(sent ? "sent" : "NOT sent (not connected)")}: {cause}.");
            SessionJournal.Write("deathlink", $"{(sent ? "sent" : "not sent")} ({cause})");
        }
    }
}
