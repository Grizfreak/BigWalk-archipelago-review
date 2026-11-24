using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    internal static partial class TrapEffects
    {
        // URP render scale lowered, upscaled by nearest pixel; 0.1 is the lowest it takes.
        internal const string Pixels = "pixels";
        private const float PixelsScale = 0.1f;
        private static float _pixelsUntil = -1f;
        private static float _pixelsOwnScale;
        private static UnityEngine.Rendering.Universal.UpscalingFilterSelection _pixelsOwnFilter;

        private static UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset UrpAsset()
        {
            var asset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
            return asset != null ? asset.TryCast<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>() : null;
        }

        private static void StartPixels(int seconds)
        {
            if (Gentle(Pixels))
                return;
            var asset = UrpAsset();
            if (asset == null)
            {
                Plugin.Log.LogWarning($"{Tag} Big Pixels: no URP asset.");
                return;
            }
            if (_pixelsUntil < 0f)
            {
                _pixelsOwnScale = asset.renderScale;
                _pixelsOwnFilter = asset.upscalingFilter;
            }
            _pixelsUntil = Mathf.Max(_pixelsUntil, Time.unscaledTime + seconds);
            HoldPixels(asset);
            Plugin.Log.LogInfo($"{Tag} Big Pixels: render scale {_pixelsOwnScale:F2} -> {PixelsScale:F2}, filter {_pixelsOwnFilter} -> Point.");
        }

        private static void HoldPixels(UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset asset)
        {
            if (asset == null)
                return;
            if (asset.renderScale != PixelsScale)
                asset.renderScale = PixelsScale;
            if (asset.upscalingFilter != UnityEngine.Rendering.Universal.UpscalingFilterSelection.Point)
                asset.upscalingFilter = UnityEngine.Rendering.Universal.UpscalingFilterSelection.Point;
        }

        private static void EndPixels()
        {
            if (_pixelsUntil < 0f)
                return;
            _pixelsUntil = -1f;
            try
            {
                var asset = UrpAsset();
                if (asset != null)
                {
                    asset.renderScale = _pixelsOwnScale;
                    asset.upscalingFilter = _pixelsOwnFilter;
                }
                Plugin.Log.LogInfo($"{Tag} Big Pixels over.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Big Pixels end: {ex.Message}");
            }
        }

        internal const string Goggles = "goggles";
        private const float GogglesFactor = 0.45f;
        private const float GogglesEase = 1.5f;
        private static float _gogglesStart = -1f;
        private static float _gogglesUntil = -1f;
        private static float _gogglesOwnFov;
        private static float _gogglesApplied;

        private static void StartGoggles(int seconds)
        {
            if (Gentle(Goggles))
                return;
            var now = Time.unscaledTime;
            if (_gogglesUntil < 0f)
            {
                _gogglesOwnFov = PlayerCameraMinder.fovFromSettings;
                _gogglesApplied = _gogglesOwnFov;
                _gogglesStart = now;
            }
            _gogglesUntil = Mathf.Max(_gogglesUntil, now + seconds);
            Plugin.Log.LogInfo($"{Tag} Big Goggles: field of view {_gogglesOwnFov:F0} towards {_gogglesOwnFov * GogglesFactor:F0}.");
        }

        private static void StepGoggles(float now)
        {
            // The player changed the setting meanwhile: that becomes their own.
            if (PlayerCameraMinder.fovFromSettings != _gogglesApplied)
                _gogglesOwnFov = PlayerCameraMinder.fovFromSettings / Mathf.Max(0.01f, _gogglesApplied / _gogglesOwnFov);

            float amount;
            if (now < _gogglesStart + GogglesEase)
                amount = (now - _gogglesStart) / GogglesEase;
            else if (now < _gogglesUntil)
                amount = 1f;
            else
                amount = 1f - (now - _gogglesUntil) / GogglesEase;
            if (amount <= 0f && now >= _gogglesUntil)
            {
                EndGoggles();
                return;
            }
            amount = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(amount));
            _gogglesApplied = _gogglesOwnFov * Mathf.Lerp(1f, GogglesFactor, amount);
            PlayerCameraMinder.fovFromSettings = _gogglesApplied;
        }

        private static void EndGoggles()
        {
            if (_gogglesUntil < 0f)
                return;
            _gogglesUntil = -1f;
            _gogglesStart = -1f;
            PlayerCameraMinder.fovFromSettings = _gogglesOwnFov;
            Plugin.Log.LogInfo($"{Tag} Big Goggles over: field of view {_gogglesOwnFov:F0}.");
        }
    }
}
