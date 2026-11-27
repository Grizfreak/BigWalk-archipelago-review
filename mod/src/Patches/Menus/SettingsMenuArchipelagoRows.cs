using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BigWalkArchipelago.Patches.Menus
{
    // Rows copied from the game's own; their SettingsRow script is removed so no setting changes.
    [HarmonyPatch(typeof(SettingsMenu), nameof(SettingsMenu.Start))]
    internal static partial class SettingsMenuArchipelagoRows
    {
        private const string Tag = "[" + nameof(SettingsMenuArchipelagoRows) + "]";

        // Point sizes; 22 is the .cfg's default.
        private static readonly (string label, int size)[] Sizes =
        {
            ("Small", 16), ("Medium", 22), ("Large", 28), ("Huge", 36),
        };

        private const string CategoryName = "Archipelago category (AP)";
        private const string ButtonName = "Archipelago button (AP)";

        private static void Postfix(SettingsMenu __instance)
        {
            try
            {
                var general = __instance != null ? __instance.catagoryGeneral : null;
                if (general == null || general.rows == null)
                    return;

                SettingsRow template = null;
                foreach (var row in general.rows)
                {
                    if (row != null && row.leftButton != null && row.rightButton != null && row.arrayLabel != null)
                    {
                        template = row;
                        break;
                    }
                }

                if (template == null)
                {
                    Plugin.Log.LogWarning($"{Tag} No left/right line in Settings > General to copy; no Archipelago settings.");
                    return;
                }

                if (general.transform.parent.Find(CategoryName) != null)
                    return;

                var rows = BuildCategory(__instance, general, template);
                if (rows != null)
                {
                    _arrayRows.Clear();
                    // The pause menu builds its own rows, which need fitting again.
                    _rowsFitted = false;
                    BuildSizeRow(template, rows);
                    BuildDeathLinkRows(template, rows);
                    BuildHintRow(template, rows);
                    BuildResyncRow(template, rows);
                    BuildTilePoolRow(template, rows);
                    BuildFlareRow(template, rows);
                    BuildMessagesRows(template, rows);
                    _rows = rows;
                    BuildGourdNameRow(rows);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Could not add the Archipelago settings: {ex}");
            }
        }

        // The button makes ours the active category, so the game hides it when another is chosen.
        private static Transform BuildCategory(SettingsMenu menu, SettingsCatagory general, SettingsRow template)
        {
            GameObject panel;
            var wasActive = general.gameObject.activeSelf;
            try
            {
                general.gameObject.SetActive(false);
                panel = UnityEngine.Object.Instantiate(general.gameObject, general.transform.parent);
            }
            finally
            {
                general.gameObject.SetActive(wasActive);
            }

            panel.name = CategoryName;
            var category = panel.GetComponent<SettingsCatagory>();

            var rows = Twin(panel.transform, general.transform, template.transform.parent) ?? panel.transform;
            for (var i = rows.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(rows.GetChild(i).gameObject);
            category.rows = new SettingsRow[0];

            var source = general.catagoryButton != null ? general.catagoryButton.gameObject : null;
            if (source == null)
                return rows;

            var button = UnityEngine.Object.Instantiate(source, source.transform.parent);
            button.name = ButtonName;
            button.transform.SetAsLastSibling();
            _menu = menu;
            _button = button.transform;
            Retitle(button, source.transform, "Archipelago", null, null, null);

            var click = button.GetComponent<Button>();
            if (click != null)
            {
                click.onClick = new Button.ButtonClickedEvent();
                click.onClick.AddListener((UnityAction)(() =>
                {
                    var active = menu.activeCatagory;
                    if (active != null && active != category)
                        active.gameObject.SetActive(false);
                    panel.SetActive(true);
                    menu.activeCatagory = category;
                }));
                category.catagoryButton = click;
            }

            return rows;
        }

        // The game lays out the category buttons after Start, so ours is placed again every frame.
        private static SettingsMenu _menu;
        private static Transform _button;

        internal static void KeepPlaced()
        {
            CatchUpGourdNameRow();
            KeepArrayRows();
            KeepGourdNameRow();

            if (_menu == null || _button == null || !_button.gameObject.activeInHierarchy)
                return;

            var buttons = new System.Collections.Generic.List<Transform>();
            foreach (var category in new[] { _menu.catagoryGeneral, _menu.catagoryAudio, _menu.catagoryMicrophone, _menu.catagoryGraphics,
                                             _menu.catagoryDisplay, _menu.catagoryControls, _menu.catagoryRebind })
            {
                var button = category != null ? category.catagoryButton : null;
                if (button != null && button.transform != _button && button.gameObject.activeInHierarchy)
                    buttons.Add(button.transform);
            }

            if (buttons.Count < 2)
                return;

            buttons.Sort((x, y) => x.position.y.CompareTo(y.position.y));
            var step = buttons[1].position - buttons[0].position;
            if (step.sqrMagnitude < 0.0001f)
                return;
            _button.position = buttons[0].position - step;
        }

        private static Transform Twin(Transform copy, Transform source, Transform inSource)
        {
            if (inSource == null)
                return null;

            var path = new System.Collections.Generic.List<string>();
            for (var t = inSource; t != null && t != source; t = t.parent)
                path.Insert(0, t.name);
            return path.Count == 0 ? copy : copy.Find(string.Join("/", path));
        }

        private static void Retitle(GameObject clone, Transform source, string text, LocalizedText value, Button left, Button right)
        {
            foreach (var localized in clone.GetComponentsInChildren<LocalizedText>(true))
            {
                if (localized == value || Under(localized.transform, left) || Under(localized.transform, right))
                    continue;

                var element = localized.textElement != null ? localized.textElement : localized.GetComponent<TMP_Text>();
                var place = element != null ? Twin(source, clone.transform, element.transform) : null;
                var original = place != null ? place.GetComponent<TMP_Text>() : null;
                UnityEngine.Object.DestroyImmediate(localized);
                if (element == null)
                    continue;

                if (original != null)
                {
                    element.font = original.font;
                    element.fontSharedMaterial = original.fontSharedMaterial;
                    element.fontStyle = original.fontStyle;
                    element.fontSize = original.fontSize;
                    element.characterSpacing = original.characterSpacing;
                }

                element.text = text;
            }
        }

        private static bool Under(Transform t, Button button)
        {
            return button != null && t.IsChildOf(button.transform);
        }

        private static void SetRaw(LocalizedText localized, string text)
        {
            if (localized == null)
                return;

            localized.displayType = LocalizedText.DisplayType.RawValue;
            localized.rawValue = text;
            localized.key = string.Empty;
            try
            {
                localized.Refresh();
            }
            catch (Exception)
            {
                // Not initialised yet (the row is still off): it shows the raw value when it wakes.
                var element = localized.textElement != null ? localized.textElement : localized.GetComponent<TMP_Text>();
                if (element != null)
                    element.text = text;
            }
        }
    }
}
