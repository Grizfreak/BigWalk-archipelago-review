using System;
using System.Collections.Generic;
using System.Reflection;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal static partial class ReceivedItemSpawner
    {
        // Picked by netId, the same on every machine. Channels stay under 0xFF, which blooms white.
        private static readonly string[] CosmeticGourdColorsHtml =
        {
            "#C83C3C", // red
            "#D2782D", // orange
            "#D2B42D", // yellow
            "#50A046", // green
            "#3C78C8", // blue
            "#8C50B4", // purple
        };

        private static void ApplyCosmeticColor(RewardGourd rewardGourd)
        {
            // netId is 0 before NetworkServer.Spawn; the spawn path re-applies once it exists.
            var identity = rewardGourd.GetComponent<NetworkIdentity>();
            var netId = identity != null ? identity.netId : 0u;
            var html = CosmeticGourdColorsHtml[(int)(netId % (uint)CosmeticGourdColorsHtml.Length)];
            if (!ColorUtility.TryParseHtmlString(html, out var color))
                return;

            LogColorSettingsOnce(rewardGourd);

            rewardGourd.isVariantChallenge = true;
            rewardGourd.variantChallengeColor = color;
            WriteHelperColor(rewardGourd, color);
            _pendingColorRefresh = rewardGourd;
        }

        // Writes where RewardGourd.Awake writes: Refresh() pushes colorSettings, not the field.
        private static void WriteHelperColor(RewardGourd rewardGourd, Color color)
        {
            var helper = rewardGourd.propertyBlockHelper;
            var settings = helper != null ? helper.colorSettings : null;
            if (settings == null || settings.Length == 0)
                return;

            // An element of an IL2CPP struct array comes back as a copy: changed, then stored back.
            var setting = settings[0];
            setting.color = color;
            settings[0] = setting;

            // Read back once per session: a write through the interop indexer could silently fail.
            if (!_helperWriteChecked && rewardGourd.GetComponent<NetworkIdentity>()?.netId > 0)
            {
                _helperWriteChecked = true;
                var now = settings[0].color;
                Plugin.Log.LogInfo(
                    $"[{nameof(ReceivedItemSpawner)}] Gourd colour written to '{settings[0].propertyName}': asked {color}, "
                    + $"the helper now holds {now} ({(now == color ? "took" : "DID NOT TAKE")}).");
            }
        }

        private static bool _helperWriteChecked;

        // Applied after activation: Awake may push variantChallengeColor; if not, this does.
        private static void RefreshCosmeticColor()
        {
            var target = _pendingColorRefresh;
            _pendingColorRefresh = null;
            if (target == null)
                return;

            try
            {
                target.propertyBlockHelper?.Refresh();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ReceivedItemSpawner)}] Could not refresh the gourd's colour: {ex.Message}");
            }
        }

        // On a client, Mirror's spawn payload runs after the handler and restores the old look.
        internal static void ReapplyCosmeticLook(RewardGourd rewardGourd)
        {
            if (rewardGourd == null)
                return;

            ApplyCosmeticColor(rewardGourd);
            RefreshCosmeticColor();
        }

        // Logged once per session: the shader properties the game drives, for the next fix.
        private static void LogColorSettingsOnce(RewardGourd rewardGourd)
        {
            if (_colorSettingsLogged)
                return;

            _colorSettingsLogged = true;

            var helper = rewardGourd.propertyBlockHelper;
            if (helper == null)
            {
                Plugin.Log.LogInfo($"[{nameof(ReceivedItemSpawner)}] Cloned gourd has no PropertyBlockHelper.");
                return;
            }

            var settings = helper.colorSettings;
            if (settings == null || settings.Length == 0)
            {
                Plugin.Log.LogInfo($"[{nameof(ReceivedItemSpawner)}] PropertyBlockHelper exposes no colour setting.");
                return;
            }

            for (var i = 0; i < settings.Length; i++)
                Plugin.Log.LogInfo(
                    $"[{nameof(ReceivedItemSpawner)}] colorSettings[{i}] '{settings[i].propertyName}' = {settings[i].color}");
        }

        private static RewardGourd _pendingColorRefresh;
        private static bool _colorSettingsLogged;
    }
}
