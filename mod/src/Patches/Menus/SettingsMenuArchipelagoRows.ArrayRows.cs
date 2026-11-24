using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace BigWalkArchipelago.Patches.Menus
{
    internal static partial class SettingsMenuArchipelagoRows
    {
        private static bool? _arraysAsGuest;
        private static float _nextArrayRefresh;

        private static void KeepArrayRows()
        {
            if (_arrayRows.Count == 0 || _arrayRows[0].Row == null || !_arrayRows[0].Row.gameObject.activeInHierarchy)
                return;

            var guest = GourdNames.IsGuest;
            if (guest != _arraysAsGuest)
            {
                _arraysAsGuest = guest;
                foreach (var row in _arrayRows)
                {
                    if (!row.HostOnly)
                        continue;
                    if (row.Left != null)
                        row.Left.interactable = !guest;
                    if (row.Right != null)
                        row.Right.interactable = !guest;
                    if (row.Title != null)
                        row.Title.text = row.Caps;
                }
            }

            if (Time.unscaledTime >= _nextArrayRefresh)
            {
                _nextArrayRefresh = Time.unscaledTime + 1f;
                foreach (var row in _arrayRows)
                    row.Show();
            }
        }

        private sealed class ArrayRow
        {
            internal RectTransform Row;
            internal TMP_Text Title;
            internal string Caps;
            internal bool HostOnly;
            internal Button Left;
            internal Button Right;
            internal Action Show;
        }

        private static readonly System.Collections.Generic.List<ArrayRow> _arrayRows = new();

        private static ArrayRow BuildArrayRow(SettingsRow template, Transform parent, string title, string caps,
                                              bool hostOnly, Func<string> show, Action<int> step)
        {
            GameObject clone;
            var wasActive = template.gameObject.activeSelf;
            try
            {
                template.gameObject.SetActive(false);
                clone = UnityEngine.Object.Instantiate(template.gameObject, parent);
            }
            finally
            {
                template.gameObject.SetActive(wasActive);
            }

            clone.name = $"{title} (AP)";
            clone.transform.SetAsLastSibling();

            var settingsRow = clone.GetComponent<SettingsRow>();
            var value = settingsRow.arrayLabel;
            var left = settingsRow.leftButton;
            var right = settingsRow.rightButton;

            UnityEngine.Object.DestroyImmediate(settingsRow);
            var identifier = clone.GetComponent<SettingItemIdentifier>();
            if (identifier != null)
                UnityEngine.Object.DestroyImmediate(identifier);
            // The translation is removed, or a language change puts the game's text back.
            Retitle(clone, template.transform, title, value, left, right);

            var row = new ArrayRow
            {
                Row = clone.GetComponent<RectTransform>(),
                Caps = caps,
                HostOnly = hostOnly,
                Left = left,
                Right = right,
            };
            row.Show = () =>
            {
                // A guest's own .cfg says nothing of the host's DeathLink.
                if (value != null)
                    SetRaw(value, hostOnly && GourdNames.IsGuest ? "Set by the host" : show());
            };

            foreach (var text in clone.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.text == title)
                    row.Title = text;
            }

            if (left != null)
            {
                left.onClick = new Button.ButtonClickedEvent();
                left.onClick.AddListener((UnityAction)(() => { step(-1); row.Show(); }));
            }

            if (right != null)
            {
                right.onClick = new Button.ButtonClickedEvent();
                right.onClick.AddListener((UnityAction)(() => { step(+1); row.Show(); }));
            }

            row.Show();
            clone.SetActive(true);
            _arrayRows.Add(row);
            return row;
        }
    }
}
