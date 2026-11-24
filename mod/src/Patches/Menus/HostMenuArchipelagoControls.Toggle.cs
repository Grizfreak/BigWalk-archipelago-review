using TMPro;
using UnityEngine.UI;

namespace BigWalkArchipelago.Patches.Menus
{
    // The toggle and what follows it: host field, test button, slot name lock.
    internal static partial class HostMenuArchipelagoControls
    {
        private static void OnToggle(HostMenuConfirm menu)
        {
            var next = !ModConfig.ArchipelagoEnabled.Value;
            ModConfig.ArchipelagoEnabled.Value = next;

            // A test result belongs to the settings that produced it.
            ApConnectionTest.Reset();
            SetResult(menu, string.Empty);

            HostMenuConfirmPatch.ApplyFieldLabels(menu, next);
            RefreshToggleLabel(menu);
            ApplyEnabledState(menu);

            Plugin.Log.LogInfo(
                $"[{nameof(HostMenuArchipelagoControls)}] Archipelago switched {(next ? "on" : "off")} from the hosting screen.");
        }

        private static void RefreshToggleLabel(HostMenuConfirm menu)
        {
            var toggle = Find(menu, ToggleName);
            if (toggle != null)
                // Short: "ARCHIPELAGO" does not fit the button.
                SetLabel(toggle, ModConfig.ArchipelagoEnabled.Value ? "AP : ON" : "AP : OFF");
        }

        // With Archipelago off, the host:port field and test button are greyed out.
        private static void ApplyEnabledState(HostMenuConfirm menu)
        {
            var on = ModConfig.ArchipelagoEnabled.Value;

            var hostRow = HostMenuConfirmPatch.FindHostPortRow(menu);
            if (hostRow != null)
            {
                var input = hostRow.Find("GameNameInput");
                var field = input != null ? input.GetComponent<TMP_InputField>() : null;
                if (field != null)
                    field.interactable = on;

                // TMP alpha, not CanvasGroup: CanvasGroup is not in the interop assemblies.
                foreach (var label in hostRow.GetComponentsInChildren<TextMeshProUGUI>(true))
                    label.alpha = on ? 1f : 0.4f;
            }

            var test = Find(menu, TestName);
            var button = test != null ? test.GetComponent<Button>() : null;
            if (button != null)
                button.interactable = on;

            ApplySlotNameLock(menu, on);
        }

        // The slot name is the save name: locked once a save exists, so it cannot switch slots.
        private static void ApplySlotNameLock(HostMenuConfirm menu, bool archipelagoOn)
        {
            var field = menu.gameNameField;
            if (field == null)
                return;

            var save = menu.saveData;
            var uid = save != null ? save.filenameUid : null;
            var alreadyExists = !string.IsNullOrEmpty(uid);

            var locked = archipelagoOn && alreadyExists;
            if (field.interactable == !locked)
                return;

            // Re-locking restores SaveData.slotName, so an edit made while off is discarded.
            if (locked && save != null && !string.IsNullOrEmpty(save.slotName) && field.text != save.slotName)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(HostMenuArchipelagoControls)}] Slot name '{field.text}' discarded; this save is "
                    + $"'{save.slotName}' and Archipelago is back on.");
                field.text = save.slotName;
            }

            field.interactable = !locked;

            WarnIfSlotRenamed(menu, save, locked);

            var deleteShown = menu.deleteButton != null && menu.deleteButton.gameObject.activeInHierarchy;
            Plugin.Log.LogInfo(
                $"[{nameof(HostMenuArchipelagoControls)}] Slot name field {(locked ? "locked" : "editable")} "
                + $"(archipelago={archipelagoOn}, filenameUid={(alreadyExists ? "set" : "empty")}, "
                + $"deleteButton={(deleteShown ? "shown" : "hidden")}).");
        }

        // The lock cannot stop a rename made with Archipelago off: warns from ApItemCursor's slot.
        private static void WarnIfSlotRenamed(HostMenuConfirm menu, SaveData save, bool locked)
        {
            if (!locked || save == null)
                return;

            string recorded = null;
            try
            {
                save.TryGetStringValue(Core.Net.ApItemCursor.SlotKey, out recorded);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(HostMenuArchipelagoControls)}] Could not read this save's recorded slot: {ex.Message}");
                return;
            }

            if (string.IsNullOrEmpty(recorded) || recorded == save.slotName)
                return;

            SetResult(menu, $"This save last connected as '{recorded}'");
            Plugin.Log.LogWarning(
                $"[{nameof(HostMenuArchipelagoControls)}] This save connected as '{recorded}' before and is now "
                + $"called '{save.slotName}'; it will connect under the new name.");
        }
    }
}
