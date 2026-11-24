using System;
using System.Collections.Generic;
using BigWalkArchipelago.Core.Net;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal static partial class MapTracker
    {
        // Renderers off only: the game keeps the flag and may set its state again.
        private static void Hide(GourdFlag flag)
        {
            HiddenFlags[flag.GetInstanceID()] = flag;
            foreach (var renderer in flag.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
        }

        private static void Show(GourdFlag flag)
        {
            if (!HiddenFlags.Remove(flag.GetInstanceID()))
                return;
            foreach (var renderer in flag.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = true;
        }

        private static void SetGrey(PropertyBlockHelper helper, bool grey)
        {
            var settings = helper != null ? helper.colorSettings : null;
            if (settings == null || settings.Length == 0)
                return;
            var id = helper.GetInstanceID();
            if (grey == Greyed.ContainsKey(id))
                return;

            if (grey)
            {
                var original = new Color[settings.Length];
                for (var i = 0; i < settings.Length; i++)
                {
                    // An element of an IL2CPP struct array comes back as a copy.
                    var setting = settings[i];
                    original[i] = setting.color;
                    setting.color = Color.Lerp(setting.color, Grey, GreyAmount);
                    settings[i] = setting;
                }
                Original[id] = original;
                Greyed[id] = helper;
            }
            else
            {
                if (Original.TryGetValue(id, out var original))
                {
                    for (var i = 0; i < settings.Length && i < original.Length; i++)
                    {
                        var setting = settings[i];
                        setting.color = original[i];
                        settings[i] = setting;
                    }
                }
                Original.Remove(id);
                Greyed.Remove(id);
            }
            helper.Refresh();
        }

        private static void ApplyFlags(Dictionary<string, byte> states)
        {
            foreach (var flag in _map.GetComponentsInChildren<GourdFlag>(true))
            {
                if (flag == null || !states.TryGetValue("puzzle:" + flag.saveablePropName, out var state))
                    continue;
                var hinted = (state & HintBit) != 0;
                if ((state & Visible) == 0)
                {
                    Hide(flag);
                    FlagStick(flag, false);
                    continue;
                }
                FlagStick(flag, hinted);
                Show(flag);
                foreach (var helper in flag.GetComponentsInChildren<PropertyBlockHelper>(true))
                    SetGrey(helper, (state & GreyBit) != 0);
            }
        }

        private static void FlagStick(GourdFlag flag, bool hinted)
        {
            var key = flag.GetInstanceID();
            FlagSticks.TryGetValue(key, out var stick);
            if (hinted && stick == null)
            {
                var foot = flag.transform.position;
                stick = WorldButtons.MakeSlab("AP hint stick", foot + Vector3.down * 0.05f, Quaternion.identity,
                    new Vector3(0.02f, 0.3f, 0.02f), Gold);
                stick.transform.SetParent(flag.transform, true);
                FlagSticks[key] = stick;
                Glowing.Add(stick.GetComponent<Renderer>());
            }
            else if (!hinted && stick != null)
            {
                Glowing.Remove(stick.GetComponent<Renderer>());
                UnityEngine.Object.Destroy(stick);
                FlagSticks.Remove(key);
            }
        }

        private static void RestoreAll()
        {
            DestroyMarkers();
            foreach (var helper in new List<PropertyBlockHelper>(Greyed.Values))
            {
                if (helper != null)
                    SetGrey(helper, false);
            }
            Greyed.Clear();
            Original.Clear();
            foreach (var flag in new List<GourdFlag>(HiddenFlags.Values))
            {
                if (flag != null)
                    Show(flag);
            }
            HiddenFlags.Clear();
        }
    }
}
