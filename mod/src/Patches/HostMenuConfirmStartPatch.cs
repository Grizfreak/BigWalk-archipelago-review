using System;
using BigWalkArchipelago.Core.Net;
using HarmonyLib;

namespace BigWalkArchipelago.Patches
{
    // Tests the Archipelago details on Continue; only a new save is held until the test passes.
    internal static class HostMenuConfirmStartPatch
    {
        private static bool _bypass;
        private static bool _autoContinuePending;

        // Kept across OnEnable, which runs again after Continue, for a few minutes only.
        private static string _verifiedKey;
        private static DateTime _verifiedAt;
        private static readonly TimeSpan VerifiedFor = TimeSpan.FromMinutes(10);

        private static string KeyOf(ApEndpoint endpoint)
        {
            return $"{endpoint.Host}:{endpoint.Port}|{endpoint.SlotName}|{endpoint.Password}";
        }

        private static bool RecentlyVerified(string key)
        {
            return _verifiedKey != null
                && (key == null || key == _verifiedKey)
                && DateTime.UtcNow - _verifiedAt < VerifiedFor;
        }

        internal static void RememberVerified(ApEndpoint endpoint)
        {
            _verifiedKey = KeyOf(endpoint);
            _verifiedAt = DateTime.UtcNow;
        }

        [HarmonyPatch(typeof(HostMenuConfirm), nameof(HostMenuConfirm.ActionStart))]
        internal static class ActionStart
        {
            private static bool Prefix(HostMenuConfirm __instance)
            {
                if (_bypass || !ModConfig.ArchipelagoEnabled.Value)
                    return true;

                var slotName = __instance.gameNameField != null ? __instance.gameNameField.text : string.Empty;
                var password = __instance.passwordField != null ? __instance.passwordField.text : string.Empty;
                var isNewSave = IsNewSave(__instance);

                Plugin.Log.LogInfo(
                    $"[{nameof(HostMenuConfirmStartPatch)}] Start requested (new save: {isNewSave}, "
                    + $"test: {ApConnectionTest.Status}).");

                if (!ApEndpoint.TryResolveFromFields(ApEndpoint.CurrentHostAndPort(), slotName, password,
                                                     out var endpoint, out var problem))
                {
                    // An existing save: let them host; ApRuntime reports once the session is up.
                    if (!isNewSave)
                        return true;

                    // Fields unreadable, but this flow was just verified: the second pass here.
                    if (RecentlyVerified(null))
                        return true;

                    HostMenuArchipelagoControls.ShowResult(__instance,
                        $"A new Archipelago game needs the host and the slot name ({problem}) - or switch AP : OFF");
                    Update.FlashFields(__instance);
                    return false;
                }

                if (RecentlyVerified(KeyOf(endpoint)))
                {
                    Plugin.Log.LogInfo(
                        $"[{nameof(HostMenuConfirmStartPatch)}] These settings were verified moments ago; no second test.");
                    return true;
                }

                switch (ApConnectionTest.Status)
                {
                    case ApConnectionTest.TestStatus.Ok:
                        RememberVerified(endpoint);
                        return true;

                    case ApConnectionTest.TestStatus.Testing:
                        // Already in flight: swallow the press rather than start a second probe.
                        return false;

                    case ApConnectionTest.TestStatus.Failed when isNewSave:
                        // A new save never hosts unverified: test again, the fields may be fixed.
                        StartProbe(__instance, endpoint);
                        return false;

                    case ApConnectionTest.TestStatus.Failed:
                        // They saw the flash and pressed again: consent to host anyway.
                        Plugin.Log.LogWarning(
                            $"[{nameof(HostMenuConfirmStartPatch)}] Hosting without a verified Archipelago connection "
                            + $"({ApConnectionTest.LastError}). The mod will keep retrying in the background.");
                        _bypass = true;
                        return true;

                    default:
                        StartProbe(__instance, endpoint);
                        return false;
                }
            }
        }

        private static void StartProbe(HostMenuConfirm menu, ApEndpoint endpoint)
        {
            HostMenuArchipelagoControls.ShowTesting(menu, $"Checking the connection to {endpoint.Host}:{endpoint.Port}");

            Plugin.Log.LogInfo(
                $"[{nameof(HostMenuConfirmStartPatch)}] Testing the Archipelago connection to "
                + $"{endpoint.Host}:{endpoint.Port} as '{endpoint.SlotName}'...");

            // This probe belongs to Continue, so the Update postfix may act on its result.
            _autoContinuePending = false;
            HostMenuArchipelagoControls.NoteTested(endpoint.SlotName);
            HostMenuArchipelagoControls.Owner = HostMenuArchipelagoControls.ProbeOwner.Continue;
            ApConnectionTest.Start(endpoint);
        }

        // A save the game has already written carries a file uid, a new one does not.
        private static bool IsNewSave(HostMenuConfirm menu)
        {
            var save = menu.saveData;
            return save == null || string.IsNullOrEmpty(save.filenameUid);
        }

        [HarmonyPatch(typeof(HostMenuConfirm), "Update")]
        internal static class Update
        {
            private static void Postfix(HostMenuConfirm __instance)
            {
                // Before the early returns: the test button's result must keep updating.
                HostMenuArchipelagoControls.Tick(__instance);

                if (_bypass || !ModConfig.ArchipelagoEnabled.Value)
                    return;

                // A probe from the test button must never launch the session.
                if (HostMenuArchipelagoControls.Owner
                    != HostMenuArchipelagoControls.ProbeOwner.Continue)
                    return;

                switch (ApConnectionTest.Status)
                {
                    case ApConnectionTest.TestStatus.Ok when !_autoContinuePending:
                        _autoContinuePending = true;
                        HostMenuArchipelagoControls.ShowResult(__instance, "Connected");
                        Plugin.Log.LogInfo(
                            $"[{nameof(HostMenuConfirmStartPatch)}] Archipelago connection verified; starting the session.");
                        __instance.ActionStart();
                        break;

                    case ApConnectionTest.TestStatus.Failed when !_autoContinuePending:
                        _autoContinuePending = true;
                        if (IsNewSave(__instance))
                        {
                            Plugin.Log.LogWarning(
                                $"[{nameof(HostMenuConfirmStartPatch)}] Archipelago connection failed: {ApConnectionTest.LastError}. "
                                + "A new Archipelago game waits for a working connection: fix the fields and press Continue "
                                + "again, or switch AP : OFF to play without Archipelago.");
                        }
                        else
                        {
                            Plugin.Log.LogWarning(
                                $"[{nameof(HostMenuConfirmStartPatch)}] Archipelago connection failed: {ApConnectionTest.LastError}. "
                                + "Check the address, the slot name and the password, or press Continue again to host anyway.");
                        }

                        HostMenuArchipelagoControls.ShowLastFailure(__instance);
                        TryFlash(__instance);
                        break;
                }
            }

            // Flash fails to JIT on 1.48 (no gameNameFlasher), so it stays in its own method.
            internal static void FlashFields(HostMenuConfirm menu)
            {
                TryFlash(menu);
            }

            private static void TryFlash(HostMenuConfirm menu)
            {
                try
                {
                    Flash(menu);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogInfo(
                        $"[{nameof(HostMenuConfirmStartPatch)}] No warning flash on this build of the game: {ex.Message}");
                }
            }

            private static void Flash(HostMenuConfirm menu)
            {
                if (menu.gameNameFlasher != null)
                    menu.gameNameFlasher.Flash();

                // The host:port row is a sibling of the GameName row, not a child of the menu.
                var gameNameRow = menu.gameNameField != null ? menu.gameNameField.transform.parent : null;
                var rows = gameNameRow != null ? gameNameRow.parent : null;
                var hostPortRow = rows != null ? rows.Find(HostMenuConfirmPatch.HostPortRowName) : null;
                if (hostPortRow == null)
                    return;

                var flasher = hostPortRow.GetComponentInChildren<InputWarningFlasher>();
                if (flasher != null)
                    flasher.Flash();
            }
        }

        [HarmonyPatch(typeof(HostMenuConfirm), nameof(HostMenuConfirm.OnEnable))]
        internal static class OnEnable
        {
            private static void Postfix()
            {
                // The details may have changed, so the old verdict and any bypass go.
                _bypass = false;
                _autoContinuePending = false;
                ApConnectionTest.Reset();
            }
        }
    }
}
