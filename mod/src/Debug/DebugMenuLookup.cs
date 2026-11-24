using System.Text;
using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Dumps the hosting screen's UI hierarchy while it is shown.
    internal static class DebugMenuLookup
    {
        internal static void DumpHostMenuConfirm()
        {
            var menu = UnityEngine.Object.FindObjectOfType<HostMenuConfirm>(true);
            if (menu == null)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugMenuLookup)}] No active HostMenuConfirm, open the game-creation screen first.");
                return;
            }

            Plugin.Log.LogInfo($"[{nameof(DebugMenuLookup)}] HostMenuConfirm found on '{menu.gameObject.name}'.");
            Plugin.Log.LogInfo(
                $"[{nameof(DebugMenuLookup)}]   gameNameField -> '{DescribeGameObject(menu.gameNameField?.gameObject)}'");
            Plugin.Log.LogInfo(
                $"[{nameof(DebugMenuLookup)}]   passwordField -> '{DescribeGameObject(menu.passwordField?.gameObject)}'");
            Plugin.Log.LogInfo(
                $"[{nameof(DebugMenuLookup)}]   passwordRequired = {menu.passwordRequired}");

            // Start from the highest RectTransform ancestor to see the whole panel.
            var root = menu.transform;
            var current = root;
            var guard = 0;
            while (current.parent != null && current.parent.GetComponent<RectTransform>() != null && guard++ < 20)
                current = current.parent;

            Dump(current, 0);
        }

        // Anchors and pivots vary, so compare world-space spans to tell if two elements overlap.
        private static string Describe(RectTransform rect)
        {
            var size = rect.rect;
            var scale = rect.lossyScale;
            var centre = rect.position;

            // rect.rect is already relative to the pivot: do not apply the pivot again.
            var left = centre.x + size.x * scale.x;
            var bottom = centre.y + size.y * scale.y;

            return $", anchoredPosition={rect.anchoredPosition} sizeDelta={rect.sizeDelta}"
                 + $" pivot={rect.pivot} anchors={rect.anchorMin}..{rect.anchorMax}"
                 + $" worldX=[{left:0.#}..{left + size.width * scale.x:0.#}]"
                 + $" worldY=[{bottom:0.#}..{bottom + size.height * scale.y:0.#}]";
        }

        private static void Dump(Transform t, int depth)
        {
            if (t == null || depth > 12)
                return;

            var rect = t.GetComponent<RectTransform>();
            var rectInfo = rect != null ? Describe(rect) : string.Empty;

            Plugin.Log.LogInfo(
                $"[{nameof(DebugMenuLookup)}] {new string(' ', depth * 2)}{t.name}, components: {DescribeComponents(t.gameObject)}{rectInfo}");

            for (var i = 0; i < t.childCount; i++)
                Dump(t.GetChild(i), depth + 1);
        }

        private static string DescribeGameObject(GameObject go)
        {
            return go == null ? "<null>" : $"{go.name} (path: {DescribePath(go.transform)})";
        }

        private static string DescribePath(Transform t)
        {
            var name = t.name;
            var current = t.parent;
            var guard = 0;
            while (current != null && guard++ < 20)
            {
                name = current.name + "/" + name;
                current = current.parent;
            }

            return name;
        }

        private static string DescribeComponents(GameObject go)
        {
            var components = go.GetComponents<Component>();
            var names = new StringBuilder();
            foreach (var c in components)
            {
                if (c == null)
                    continue;
                if (names.Length > 0)
                    names.Append(", ");
                names.Append(c.GetIl2CppType().Name);
            }

            return names.ToString();
        }
    }
}
