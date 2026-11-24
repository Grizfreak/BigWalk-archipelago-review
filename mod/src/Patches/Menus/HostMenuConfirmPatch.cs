using System;
using BigWalkArchipelago.Core;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace BigWalkArchipelago.Patches.Menus
{
    // Adds an Archipelago host:port field next to the save name on the hosting screen.
    [HarmonyPatch(typeof(HostMenuConfirm), nameof(HostMenuConfirm.OnEnable))]
    internal static class HostMenuConfirmPatch
    {
        // Also used by HostMenuConfirmStartPatch to flash this field when the test fails.
        internal const string HostPortRowName = "ApHostPortRow";
        private const float HalfRowWidth = 399.42f;
        private const float ColumnOffsetX = 489.4f; // Password -> LastPlayed

        private static void Postfix(HostMenuConfirm __instance)
        {
            // passwordRequired only gates Continue; missing on 1.48, so set by reflection.
            TrySetPasswordOptional(__instance);

            try
            {
                InjectHostPortField(__instance);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(HostMenuConfirmPatch)}] Failed to inject the host:port field, ignored: {ex.Message}");
            }

            // Separate try, after the field: losing the buttons must not cost the field.
            try
            {
                HostMenuArchipelagoControls.Inject(__instance);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(HostMenuConfirmPatch)}] Failed to inject the Archipelago controls, ignored: {ex.Message}");
            }
        }

        // Looked up at run time, so its absence on a build is a no-op rather than a JIT crash.
        private static void TrySetPasswordOptional(HostMenuConfirm menu)
        {
            const string MemberName = "passwordRequired";

            try
            {
                var property = AccessTools.Property(typeof(HostMenuConfirm), MemberName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(menu, false, null);
                    return;
                }

                var field = AccessTools.Field(typeof(HostMenuConfirm), MemberName);
                if (field != null)
                {
                    field.SetValue(menu, false);
                    return;
                }

                // Harmless: the password field just stays mandatory on this build.
                Plugin.Log.LogInfo(
                    $"[{nameof(HostMenuConfirmPatch)}] '{MemberName}' absent on this build of the game: the password stays mandatory on the hosting screen.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(HostMenuConfirmPatch)}] Could not make the password optional, ignored: {ex.Message}");
            }
        }

        private static void InjectHostPortField(HostMenuConfirm menu)
        {
            var gameNameInput = menu.gameNameField;
            var gameNameRow = gameNameInput != null ? gameNameInput.transform.parent : null;
            var parent = gameNameRow != null ? gameNameRow.parent : null;
            if (gameNameRow == null || parent == null || gameNameRow.name != "GameName")
                return;

            // OnEnable runs again when the screen is reopened: don't clone twice.
            if (parent.Find(HostPortRowName) != null)
                return;

            var gameNameRect = gameNameRow.GetComponent<RectTransform>();
            var originalWidth = gameNameRect.sizeDelta.x;
            var scale = HalfRowWidth / originalWidth;

            ScaleRowWidth(gameNameRow, scale, HalfRowWidth);

            var clone = UnityEngine.Object.Instantiate(gameNameRow.gameObject, parent);
            clone.name = HostPortRowName;

            var cloneRect = clone.transform.GetComponent<RectTransform>();
            cloneRect.anchoredPosition = new Vector2(
                gameNameRect.anchoredPosition.x + ColumnOffsetX,
                gameNameRect.anchoredPosition.y);

            RetitleClone(clone.transform);
            RelabelOriginalFields(menu, gameNameRow);

            Plugin.Log.LogInfo($"[{nameof(HostMenuConfirmPatch)}] host:port field injected on '{menu.gameObject.name}'.");
        }

        // Only the labels change: the two fields become the AP slot name and password.
        private static void RelabelOriginalFields(HostMenuConfirm menu, Transform gameNameRow)
        {
            ApplyFieldLabels(menu, ModConfig.ArchipelagoEnabled.Value, gameNameRow);
        }

        // With Archipelago off, the fields go back to the game's save name and password.
        internal static void ApplyFieldLabels(HostMenuConfirm menu, bool archipelagoOn, Transform gameNameRow = null)
        {
            gameNameRow ??= menu.gameNameField != null ? menu.gameNameField.transform.parent : null;
            if (gameNameRow == null)
                return;

            var passwordRow = menu.passwordField != null ? menu.passwordField.transform.parent : null;
            var passwordTitle = passwordRow != null && passwordRow.name == "Password"
                ? passwordRow.Find("PasswordTitle")
                : null;

            if (archipelagoOn)
            {
                SetStaticLabel(gameNameRow.Find("GameNameTitle"), "SLOT NAME :");
                SetStaticLabel(passwordTitle, "ARCHIPELAGO PASSWORD :");
                return;
            }

            RestoreLocalizedLabel(gameNameRow.Find("GameNameTitle"));
            RestoreLocalizedLabel(passwordTitle);
        }

        // Found each time: OnEnable can run again, and a held reference may be destroyed.
        internal static Transform FindHostPortRow(HostMenuConfirm menu)
        {
            var gameNameRow = menu.gameNameField != null ? menu.gameNameField.transform.parent : null;
            var rows = gameNameRow != null ? gameNameRow.parent : null;
            return rows != null ? rows.Find(HostPortRowName) : null;
        }

        // No LayoutGroup here: the children have fixed sizes and must be scaled too.
        private static void ScaleRowWidth(Transform row, float scale, float newRowWidth)
        {
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(newRowWidth, rowRect.sizeDelta.y);

            for (var i = 0; i < row.childCount; i++)
            {
                var childRect = row.GetChild(i).GetComponent<RectTransform>();
                if (childRect == null)
                    continue;

                childRect.sizeDelta = new Vector2(childRect.sizeDelta.x * scale, childRect.sizeDelta.y);
            }
        }

        private static void RetitleClone(Transform clone)
        {
            var title = clone.Find("GameNameTitle");
            SetStaticLabel(title, "ARCHIPELAGO HOST :");

            var input = clone.Find("GameNameInput");
            if (input == null)
                return;

            var placeholder = input.Find("Text Area/Placeholder");
            SetStaticLabel(placeholder, "host:port");

            var tmpInput = input.GetComponent<TMP_InputField>();
            if (tmpInput == null)
                return;

            // The cloned save-name limit is too short for a host:port.
            tmpInput.characterLimit = 0;

            var lastValue = ModConfig.ArchipelagoHostPort.Value;
            tmpInput.text = lastValue;
            ApSessionConfig.HostAndPort = lastValue;
            tmpInput.onValueChanged.AddListener((UnityAction<string>)(value =>
            {
                ApSessionConfig.HostAndPort = value;
                ModConfig.ArchipelagoHostPort.Value = value;
            }));
        }

        // LocalizedText would overwrite our text on the next language refresh.
        internal static void SetStaticLabel(Transform target, string text)
        {
            if (target == null)
                return;

            var localized = target.GetComponent<LocalizedText>();
            if (localized != null)
                localized.enabled = false;

            var label = target.GetComponent<TextMeshProUGUI>();
            if (label != null)
                label.text = text;
        }

        // Gives the game's localized label back, for the player's own language.
        internal static void RestoreLocalizedLabel(Transform target)
        {
            if (target == null)
                return;

            var localized = target.GetComponent<LocalizedText>();
            if (localized == null)
                return;

            localized.enabled = true;

            try
            {
                localized.Refresh();
            }
            catch (Exception ex)
            {
                // Not fatal: the label keeps its text until the next refresh.
                Plugin.Log.LogInfo(
                    $"[{nameof(HostMenuConfirmPatch)}] Could not refresh a restored label: {ex.Message}");
            }
        }
    }
}
