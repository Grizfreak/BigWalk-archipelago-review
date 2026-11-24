using TMPro;
using UnityEngine;

namespace BigWalkArchipelago.Patches.Menus
{
    // Archipelago toggle and connection test on the hosting screen; a failed test never blocks.
    internal static partial class HostMenuArchipelagoControls
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
