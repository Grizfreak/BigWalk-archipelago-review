using System;
using BigWalkArchipelago.Core;
using BigWalkArchipelago.Core.Net;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BigWalkArchipelago.Patches
{
    // Archipelago toggle and connection test on the hosting screen; a failed test never blocks.
    internal static class HostMenuArchipelagoControls
    {
        private const string ToggleName = "ApArchipelagoToggle";
        private const string TestName = "ApTestButton";
        private const string ResultName = "ApTestResult";

        // Card columns are 489.40 apart; Continue has its own anchor, so clones are offset from it.
        private const float ColumnOffsetX = 489.40f;
        private const float SpacingFactor = 1.25f;

        // DeleteButton's pivot is (1, 0.5): its x is its right edge, so the toggle sits left of it.
        private const float ToggleOffsetX = 340.00f;
        private const float ToggleWidth = 240f;
        private const float ToggleFontScale = 0.62f;

        // Below TEST CONNECTION and above SUPPRIMER: it is the test button's answer.
        private const float ResultOffsetFactor = 0.34f;

        // Wraps and shrinks to fit between TEST CONNECTION and SUPPRIMER; not measured on screen.
        private const float ResultWidth = 460f;
        private const float ResultHeight = 66f;
        private const float ResultFontScale = 0.62f;
        private const float ResultMinFontScale = 0.5f;

        // What the line says while a probe is out, before the moving dots; null when not testing.
        private static string _testingText;
        private static int _lastDots = -1;

        internal enum ProbeOwner
        {
            None,
            Continue,
            TestButton,
        }

        // Continue and the test button want opposite things from TestStatus.Ok: not a bool.
        internal static ProbeOwner Owner { get; set; } = ProbeOwner.None;

        private static string _testedSlotName = string.Empty;

        internal static void Inject(HostMenuConfirm menu)
        {
            var continueButton = menu.ContinueButton;
            var template = continueButton != null ? continueButton.gameObject : null;
            if (template == null)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(HostMenuArchipelagoControls)}] No ContinueButton on this build of the game: "
                    + "the Archipelago toggle and test button are not added.");
                return;
            }

            var parent = template.transform.parent;
            var templateRect = template.GetComponent<RectTransform>();
            if (parent == null || templateRect == null)
                return;

            // OnEnable runs again when the screen is reopened: do not clone again.
            if (parent.Find(ToggleName) != null)
            {
                // Reopened, not rebuilt: the state shown is read from the config and put back.
                RefreshToggleLabel(menu);
                ApplyEnabledState(menu);
                SetResult(menu, string.Empty);
                return;
            }

            var step = templateRect.sizeDelta.y * SpacingFactor;
            var origin = templateRect.anchoredPosition;

            // Facing Continue, one column across.
            TryBuild(() => BuildButton(template, parent, ToggleName, origin + new Vector2(ToggleOffsetX, 0f),
                                       null, () => OnToggle(menu), ToggleWidth, ToggleFontScale), ToggleName);

            // One row up, where LastPlayed was; labelled now, as only the toggle is refreshed.
            TryBuild(() => BuildButton(template, parent, TestName, origin + new Vector2(ColumnOffsetX, step),
                                       "TEST CONNECTION", () => OnTest(menu)), TestName);

            // Under the toggle, in the band above the card's bottom divider that nothing else uses.
            TryBuild(() => BuildResultLabel(template, parent,
                                            origin + new Vector2(ColumnOffsetX, step * ResultOffsetFactor)),
                     ResultName);

            TryBuild(() => HideLastPlayed(parent), "LastPlayed");

            RefreshToggleLabel(menu);
            ApplyEnabledState(menu);

            Plugin.Log.LogInfo(
                $"[{nameof(HostMenuArchipelagoControls)}] Archipelago toggle and test button injected on "
                + $"'{menu.gameObject.name}' (step {step:0.#}, column {ColumnOffsetX:0.#} from Continue at {origin}).");
        }

        // Each control built on its own, so one failing leaves the others standing.
        private static void TryBuild(Action build, string what)
        {
            try
            {
                build();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(HostMenuArchipelagoControls)}] Could not add '{what}', ignored: {ex.Message}");
            }
        }

        private static GameObject BuildButton(GameObject template, Transform parent, string name,
                                              Vector2 anchoredPosition, string label, Action onClick,
                                              float width = 0f, float fontScale = 0f)
        {
            var clone = UnityEngine.Object.Instantiate(template, parent);
            clone.name = name;

            var rect = clone.GetComponent<RectTransform>();
            rect.anchoredPosition = anchoredPosition;

            if (width > 0f)
                Narrow(rect, width);

            if (label != null)
                SetLabel(clone, label);

            if (fontScale > 0f)
            {
                var text = clone.GetComponentInChildren<TextMeshProUGUI>(true);
                if (text != null)
                {
                    // Auto-sizing would undo this on the next layout pass.
                    text.enableAutoSizing = false;
                    text.fontSize *= fontScale;
                }
            }

            // Transparent, not disabled: a disabled Graphic stops being a raycast target.
            var arrow = clone.GetComponent<Image>();
            if (arrow != null)
            {
                var c = arrow.color;
                arrow.color = new Color(c.r, c.g, c.b, 0f);
            }

            var button = clone.GetComponent<Button>();
            if (button != null)
            {
                // Instantiate copies the persistent ActionStart listener; a new event drops it.
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener((UnityAction)(() => onClick()));
            }

            return clone;
        }

        // No LayoutGroup here: children have a fixed sizeDelta; a zero one is a stretched anchor.
        private static void Narrow(RectTransform rect, float width)
        {
            var scale = width / rect.sizeDelta.x;
            rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);

            for (var i = 0; i < rect.childCount; i++)
            {
                var child = rect.GetChild(i).GetComponent<RectTransform>();
                if (child == null || Mathf.Approximately(child.sizeDelta.x, 0f))
                    continue;

                child.sizeDelta = new Vector2(child.sizeDelta.x * scale, child.sizeDelta.y);
            }
        }

        // Deactivated, not moved: its row is the only free place in the right column.
        private static void HideLastPlayed(Transform card)
        {
            var lastPlayed = card.Find("LastPlayed");
            if (lastPlayed == null)
                return;

            lastPlayed.gameObject.SetActive(false);
            Plugin.Log.LogInfo(
                $"[{nameof(HostMenuArchipelagoControls)}] 'LastPlayed' hidden to make room for the test button.");
        }

        private static GameObject BuildResultLabel(GameObject template, Transform parent, Vector2 anchoredPosition)
        {
            var clone = UnityEngine.Object.Instantiate(template, parent);
            clone.name = ResultName;
            clone.GetComponent<RectTransform>().anchoredPosition = anchoredPosition;

            // Only ever read: everything that lets it be clicked, hovered or selected is removed.
            var button = clone.GetComponent<Button>();
            if (button != null)
                UnityEngine.Object.Destroy(button);

            var managed = clone.GetComponent<ManagedButton>();
            if (managed != null)
                UnityEngine.Object.Destroy(managed);

            foreach (var image in clone.GetComponentsInChildren<Image>(true))
                image.enabled = false;

            FitResultText(clone);
            SetLabel(clone, string.Empty);
            return clone;
        }

        // Best effort: if it fails, the one-line label stays, ugly but readable.
        private static void FitResultText(GameObject clone)
        {
            try
            {
                var rect = clone.GetComponent<RectTransform>();
                if (rect != null)
                    rect.sizeDelta = new Vector2(ResultWidth, ResultHeight);

                var label = clone.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label == null)
                    return;

                // The text fills its parent exactly, whatever the source button did with its child.
                var textRect = label.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                }

                var size = label.fontSize * ResultFontScale;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.enableAutoSizing = true;
                label.fontSizeMax = size;
                label.fontSizeMin = size * ResultMinFontScale;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogInfo($"[{nameof(HostMenuArchipelagoControls)}] Result line left on one line: {ex.Message}");
            }
        }

        // While a probe is out the line says so, with dots that keep moving.
        internal static void ShowTesting(HostMenuConfirm menu, string text)
        {
            _testingText = text;
            _lastDots = -1;
            SetResult(menu, text + "...");
        }

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

        private static void SetResult(HostMenuConfirm menu, string text)
        {
            var label = Find(menu, ResultName);
            if (label != null)
                SetLabel(label, text);
        }

        private static GameObject Find(HostMenuConfirm menu, string name)
        {
            var parent = menu.ContinueButton != null ? menu.ContinueButton.transform.parent : null;
            var found = parent != null ? parent.Find(name) : null;
            return found != null ? found.gameObject : null;
        }

        private static void SetLabel(GameObject target, string text)
        {
            var label = target.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                HostMenuConfirmPatch.SetStaticLabel(label.transform, text);
        }
    }
}
