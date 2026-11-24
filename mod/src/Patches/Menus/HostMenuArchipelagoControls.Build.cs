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
    }
}
