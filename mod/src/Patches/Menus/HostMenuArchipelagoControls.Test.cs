using System;
using BigWalkArchipelago.Core;
using BigWalkArchipelago.Core.Net;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BigWalkArchipelago.Patches
{
    internal static partial class HostMenuArchipelagoControls
    {
        // While a probe is out the line says so, with dots that keep moving.
        internal static void ShowTesting(HostMenuConfirm menu, string text)
        {
            _testingText = text;
            _lastDots = -1;
            SetResult(menu, text + "...");
        }

        private static void OnTest(HostMenuConfirm menu)
        {
            if (!ModConfig.ArchipelagoEnabled.Value)
            {
                SetResult(menu, "Archipelago is off for this session");
                return;
            }

            var slotName = menu.gameNameField != null ? menu.gameNameField.text : string.Empty;
            var password = menu.passwordField != null ? menu.passwordField.text : string.Empty;

            if (!ApEndpoint.TryResolveFromFields(ApEndpoint.CurrentHostAndPort(), slotName, password,
                                                 out var endpoint, out var problem))
            {
                SetResult(menu, string.IsNullOrEmpty(problem) ? "Fill in the host and the slot name" : problem);
                return;
            }

            _testedSlotName = endpoint.SlotName;
            Owner = ProbeOwner.TestButton;
            ApConnectionTest.Reset();
            ApConnectionTest.Start(endpoint);

            ShowTesting(menu, $"Testing {endpoint.Host}:{endpoint.Port}");
            Plugin.Log.LogInfo(
                $"[{nameof(HostMenuArchipelagoControls)}] Testing the Archipelago connection to "
                + $"{endpoint.Host}:{endpoint.Port} as '{endpoint.SlotName}' (test button).");
        }

        // Polled from the Update postfix: the probe answers on its own thread.
        internal static void Tick(HostMenuConfirm menu)
        {
            // Three steps a second, and the label only rewritten when they change.
            if (_testingText != null)
            {
                if (ApConnectionTest.Status == ApConnectionTest.TestStatus.Testing)
                {
                    var dots = (int)(Time.unscaledTime * 3f) % 4;
                    if (dots != _lastDots)
                    {
                        _lastDots = dots;
                        SetResult(menu, _testingText + new string('.', dots));
                    }
                }
                else
                {
                    _testingText = null;
                }
            }

            if (Owner != ProbeOwner.TestButton)
                return;

            switch (ApConnectionTest.Status)
            {
                case ApConnectionTest.TestStatus.Ok:
                    Owner = ProbeOwner.None;
                    SetResult(menu, $"Connected as '{_testedSlotName}'");
                    Plugin.Log.LogInfo(
                        $"[{nameof(HostMenuArchipelagoControls)}] Archipelago test succeeded as '{_testedSlotName}'.");
                    break;

                case ApConnectionTest.TestStatus.Failed:
                    Owner = ProbeOwner.None;
                    SetResult(menu, $"Failed: {Explain(ApConnectionTest.LastError)}");
                    Plugin.Log.LogWarning(
                        $"[{nameof(HostMenuArchipelagoControls)}] Archipelago test failed: {ApConnectionTest.LastError}");
                    break;
            }
        }

        // Turns the server's codes into the three mistakes worth telling apart; others pass.
        private static string Explain(string error)
        {
            if (string.IsNullOrEmpty(error))
                return "no reason given";

            // Matches the library's sentences as well as the server's codes.
            var badSlot = error.IndexOf("Slot", StringComparison.OrdinalIgnoreCase) >= 0;
            var badPassword = error.IndexOf("Password", StringComparison.OrdinalIgnoreCase) >= 0;

            // Not a Big Walk room means the wrong address, usually the port, not a wrong name.
            if ((badSlot || badPassword) && !RoomPlaysBigWalk())
                return "not a Big Walk room - check the port" + PortNote();

            if (badSlot && badPassword)
                return $"no slot called '{_testedSlotName}' here, and the password is wrong too";

            if (badSlot)
                return $"no slot '{_testedSlotName}' in this room - check the host and port, then the name";

            if (badPassword)
                return "wrong Archipelago password";

            // Our 15s giveup and the socket's timeouts all mean the server was not reached.
            if (error.IndexOf("no answer", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("refused", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("unreachable", StringComparison.OrdinalIgnoreCase) >= 0)
                return "no server answered - check the host and the port" + PortNote();

            return error + PortNote();
        }

        // True when the room runs Big Walk or there is no way to know.
        private static bool RoomPlaysBigWalk()
        {
            var games = ApConnectionTest.LastRoomGames;
            if (games == null || games.Length == 0)
                return true;

            foreach (var game in games)
            {
                if (string.Equals(game, "Big Walk", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        // Only when no port was given: archipelago.gg rooms each have their own.
        private static string PortNote()
        {
            var raw = ApEndpoint.CurrentHostAndPort();
            if (ApEndpoint.HasExplicitPort(raw))
                return string.Empty;

            return ApEndpoint.IsArchipelagoGg(raw)
                ? " (no port given - each archipelago.gg room has its own)"
                : $" (no port given, tried {ApEndpoint.DefaultPortForDisplay})";
        }

        // For HostMenuConfirmStartPatch, which has things to say on this same line.
        internal static void ShowResult(HostMenuConfirm menu, string text)
        {
            SetResult(menu, text);
        }

        // Continue's own probe result goes here too, where the player is looking.
        internal static void NoteTested(string slotName)
        {
            _testedSlotName = slotName ?? string.Empty;
        }

        internal static void ShowLastFailure(HostMenuConfirm menu)
        {
            SetResult(menu, $"Failed: {Explain(ApConnectionTest.LastError)}");
        }
    }
}
