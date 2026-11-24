using System;
using BigWalkArchipelago.Core;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BigWalkArchipelago.Patches
{
    internal static partial class SettingsMenuArchipelagoRows
    {
        // The settings have no text field, so the hosting screen's save name field is copied.
        private static GameObject _inputTemplate;
        private static TMP_InputField _gourdField;
        private static Transform _gourdTitle;

        private static GameObject InputTemplate()
        {
            if (_inputTemplate != null)
                return _inputTemplate;

            foreach (var menu in UnityEngine.Object.FindObjectsByType<HostMenuConfirm>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var row = menu != null && menu.gameNameField != null ? menu.gameNameField.transform.parent : null;
                if (row == null)
                    continue;

                var wasActive = row.gameObject.activeSelf;
                try
                {
                    row.gameObject.SetActive(false);
                    _inputTemplate = UnityEngine.Object.Instantiate(row.gameObject);
                }
                finally
                {
                    row.gameObject.SetActive(wasActive);
                }

                _inputTemplate.name = "Archipelago text field template (AP)";
                UnityEngine.Object.DontDestroyOnLoad(_inputTemplate);
                break;
            }

            return _inputTemplate;
        }

        // A guest may never open the main menu's settings, so the template is looked for later.
        private static Transform _rows;
        private static float _nextTemplateLook;

        private static void CatchUpGourdNameRow()
        {
            if (_inputTemplate == null)
            {
                if (Time.unscaledTime < _nextTemplateLook)
                    return;
                _nextTemplateLook = Time.unscaledTime + 1f;
                if (InputTemplate() == null)
                    return;
            }

            if (_rows == null || _gourdRow != null)
                return;

            // Once per category: a line that failed to build is not retried every frame.
            var rows = _rows;
            _rows = null;
            try
            {
                BuildGourdNameRow(rows);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Could not add the gourd name line late: {ex.Message}");
            }
        }

        private static void BuildGourdNameRow(Transform rows)
        {
            var template = InputTemplate();
            if (template == null)
            {
                Plugin.Log.LogInfo($"{Tag} No hosting screen seen yet to copy a text field from; no gourd name line this time.");
                return;
            }

            var clone = UnityEngine.Object.Instantiate(template, rows);
            clone.name = "Gourd name (AP)";
            clone.transform.SetAsLastSibling();

            _gourdTitle = clone.transform.Find("GameNameTitle");
            HostMenuConfirmPatch.SetStaticLabel(_gourdTitle, "GOURD NAME :");

            var input = clone.transform.Find("GameNameInput");
            _gourdField = input != null ? input.GetComponent<TMP_InputField>() : null;
            if (_gourdField != null)
            {
                HostMenuConfirmPatch.SetStaticLabel(input.Find("Text Area/Placeholder"), GourdNames.Default);

                // Instantiate copies the persistent listeners, so replace them instead of adding.
                _gourdField.onValueChanged = new TMP_InputField.OnChangeEvent();
                _gourdField.onEndEdit = new TMP_InputField.SubmitEvent();
                _gourdField.onSubmit = new TMP_InputField.SubmitEvent();
                _gourdField.characterLimit = GourdNames.MaxLength;
                _gourdField.text = GourdNames.Current;

                _gourdField.onEndEdit.AddListener((UnityAction<string>)(value =>
                {
                    if (GourdNames.IsGuest)
                        return;
                    GourdNames.Set(value);
                    _gourdField.text = GourdNames.Current;
                    Plugin.Log.LogInfo($"{Tag} The gourds are now called '{GourdNames.Current}'.");
                }));
            }

            _gourdRow = clone.GetComponent<RectTransform>();

            // The copied row loses the game's caps font, so it borrows this title's.
            var model = _gourdTitle != null ? _gourdTitle.GetComponent<TMP_Text>() : null;
            if (model != null)
            {
                foreach (var row in _arrayRows)
                {
                    var text = row.Title;
                    if (text == null)
                        continue;
                    text.font = model.font;
                    text.fontSharedMaterial = model.fontSharedMaterial;
                    text.fontStyle = model.fontStyle;
                    text.text = row.Caps;
                }

                _sizeTitle = _arrayRows.Count > 0 && _arrayRows[0].Title != null ? _arrayRows[0].Title.rectTransform : null;
                _arraysAsGuest = null;
            }

            clone.SetActive(true);
            Plugin.Log.LogInfo($"{Tag} Gourd name line added to Settings > Archipelago.");
        }

        private static bool _shownAsGuest;

        private static RectTransform _gourdRow;
        private static RectTransform _sizeRow;
        private static RectTransform _sizeTitle;

        // GetWorldCorners reads zeros under IL2CPP, so the edge is computed from the rect.
        private static float LeftEdge(RectTransform r)
        {
            var text = r.GetComponent<TMP_Text>();
            var left = text != null ? text.textBounds.min.x : r.rect.xMin;
            return r.TransformPoint(new Vector3(left, 0f, 0f)).x;
        }
        private static readonly Vector3[] Corners = new Vector3[4];

        // The panel has no layout component, so the gourd row is placed by hand every frame.
        private static void PlaceGourdRow()
        {
            FitRows();
            var below = _arrayRows.Count > 0 ? _arrayRows[_arrayRows.Count - 1].Row : _sizeRow;
            if (_gourdRow == null || below == null)
                return;

            var element = _gourdRow.GetComponent<LayoutElement>() ?? _gourdRow.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;

            var sizeTop = below.anchoredPosition.y + below.sizeDelta.y * (1f - below.pivot.y);
            var sizeBottom = sizeTop - below.sizeDelta.y;
            var sizeLeft = below.anchoredPosition.x - below.sizeDelta.x * below.pivot.x;
            _gourdRow.anchorMin = _gourdRow.anchorMax = below.anchorMin;
            _gourdRow.pivot = new Vector2(0f, 1f);
            _gourdRow.sizeDelta = new Vector2(below.sizeDelta.x, _gourdRow.sizeDelta.y);
            _gourdRow.anchoredPosition = new Vector2(sizeLeft, sizeBottom - 20f);

            var gourdTitle = _gourdTitle != null ? _gourdTitle.GetComponent<RectTransform>() : null;
            if (_sizeTitle != null && gourdTitle != null)
            {
                var shift = LeftEdge(_sizeTitle) - LeftEdge(gourdTitle);
                _gourdRow.position += new Vector3(shift, 0f, 0f);
                if (!_shiftLogged)
                {
                    _shiftLogged = true;
                    Plugin.Log.LogInfo($"{Tag} Title shift: size title left {LeftEdge(_sizeTitle)} (bounds {_sizeTitle.GetComponent<TMP_Text>()?.textBounds}, rect {_sizeTitle.rect}), gourd title left {LeftEdge(gourdTitle)} (rect {gourdTitle.rect}), shift {shift}.");
                }
            }
            else if (!_shiftLogged)
            {
                _shiftLogged = true;
                Plugin.Log.LogInfo($"{Tag} Title shift skipped: size title {(_sizeTitle != null ? "found" : "missing")}, gourd title {(gourdTitle != null ? "found" : "missing")}.");
            }
            if (!_layoutLogged)
            {
                _layoutLogged = true;
                LogLayout(Vector3.zero, 0f, Vector3.zero, Vector3.zero);
            }
        }

        private static bool _layoutLogged;
        private static bool _shiftLogged;
        private static bool _rowsFitted;

        // Past six arrow rows, tighter spacing keeps the gourd name row on the page.
        private static void FitRows()
        {
            if (_rowsFitted || _arrayRows.Count < 7)
                return;
            var parent = _arrayRows[0].Row.parent;
            var group = parent != null ? parent.GetComponent<VerticalLayoutGroup>() : null;
            var height = _arrayRows[0].Row.rect.height;
            if (group == null || height <= 0f)
                return;

            _rowsFitted = true;
            var count = _arrayRows.Count;
            var before = group.spacing;
            group.spacing = (6 * height + 5 * before - count * height) / (count - 1);
            LayoutRebuilder.MarkLayoutForRebuild(parent.GetComponent<RectTransform>());
            Plugin.Log.LogInfo($"{Tag} {count} lines of {height:0} px: spacing {before:0.#} -> {group.spacing:0.#}.");
        }

        private static void LogLayout(Vector3 sizeBottomLeft, float sizeHeight, Vector3 gourdTopLeft, Vector3 target)
        {
            var lines = new System.Collections.Generic.List<string>
            {
                $"{Tag} Layout: size line bottom-left {sizeBottomLeft} height {sizeHeight}; gourd line top-left was {gourdTopLeft}, moved to {target}, now {_gourdRow.position}.",
            };

            void Describe(Transform t, string indent)
            {
                var types = new System.Collections.Generic.List<string>();
                foreach (var c in t.GetComponents<Component>())
                {
                    if (c != null)
                        types.Add(c.GetIl2CppType().Name);
                }

                var r = t.GetComponent<RectTransform>();
                lines.Add(r == null
                    ? $"{indent}{t.name}: {string.Join(",", types)}"
                    : $"{indent}{t.name} active={t.gameObject.activeSelf} pos={r.anchoredPosition} size={r.sizeDelta} anchors={r.anchorMin}-{r.anchorMax} pivot={r.pivot} world={r.position}: {string.Join(",", types)}");
            }

            var parent = _sizeRow.parent;
            for (var t = parent; t != null && lines.Count < 8; t = t.parent)
                Describe(t, "  up ");
            for (var i = 0; i < parent.childCount; i++)
                Describe(parent.GetChild(i), "  child ");

            Plugin.Log.LogInfo(string.Join(Environment.NewLine, lines));
        }

        private static void KeepGourdNameRow()
        {
            if (_gourdField == null || !_gourdField.gameObject.activeInHierarchy)
                return;

            PlaceGourdRow();

            var guest = GourdNames.IsGuest;
            _gourdField.interactable = !guest;
            if (guest && !_gourdField.isFocused && _gourdField.text != GourdNames.Current)
                _gourdField.text = GourdNames.Current;

            if (guest != _shownAsGuest)
            {
                _shownAsGuest = guest;
                HostMenuConfirmPatch.SetStaticLabel(_gourdTitle, guest ? "GOURD NAME (THE HOST'S IN THIS GAME) :" : "GOURD NAME :");

                // Otherwise leaving the field would save the host's name as this player's.
                if (!guest)
                    _gourdField.text = GourdNames.Current;
            }
        }
    }
}
