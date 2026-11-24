using System;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // The mod's version in a corner of the menus, so players can check they run the same build.
    internal class ModVersionLabel : MonoBehaviour
    {
        // Constructor required by Il2CppInterop for any type injected into IL2CPP.
        public ModVersionLabel(IntPtr ptr) : base(ptr)
        {
        }

        private const int Margin = 10;
        private const int FontSize = 14;

        private static readonly Color TextColor = new(0.8f, 0.8f, 0.8f, 0.85f);
        private static readonly Color ShadowColor = new(0f, 0f, 0f, 0.7f);

        private static readonly string Text = $"{Plugin.PluginName} v{Plugin.PluginVersion}";

        // Built on first use: GUI.skin is only valid inside OnGUI.
        private GUIStyle _style;

        private void OnGUI()
        {
            if (WorldManager.isReadyForEffects && !WorldMenuOpen())
                return;

            _style ??= new GUIStyle(GUI.skin.label) { fontSize = FontSize, wordWrap = false };

            var size = _style.CalcSize(new GUIContent(Text));
            var rect = new Rect(Screen.width - size.x - Margin, Screen.height - size.y - Margin, size.x, size.y);

            _style.normal.textColor = ShadowColor;
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), Text, _style);

            _style.normal.textColor = TextColor;
            GUI.Label(rect, Text, _style);
        }

        // In the world, the pause and settings menus too.
        private static bool WorldMenuOpen()
        {
            var menus = WorldMenuManager.instance;
            if (menus == null)
                return false;
            return Shown(menus.pauseMenuHost) || Shown(menus.pauseMenuClient) || Shown(menus.settingsMenu);
        }

        private static bool Shown(Component menu) => menu != null && menu.gameObject.activeInHierarchy;
    }
}
