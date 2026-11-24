using System;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Paints the big keys on every machine: a MaterialPropertyBlock tint is local and never synced.
    internal class KeyColourPainter : MonoBehaviour
    {
        // Constructor required by Il2CppInterop for any type injected into IL2CPP.
        public KeyColourPainter(IntPtr ptr) : base(ptr)
        {
        }

        private const float PaintIntervalSeconds = 1f;

        private bool _worldWasReady;
        private float _nextPaint;

        private void Update()
        {
            if (!WorldManager.isReadyForEffects)
            {
                _worldWasReady = false;
                return;
            }

            if (!_worldWasReady)
            {
                _worldWasReady = true;
                KeyColours.Rearm();
                _nextPaint = 0f;
            }

            if (Time.time < _nextPaint)
                return;

            _nextPaint = Time.time + PaintIntervalSeconds;

            try
            {
                KeyColours.PaintOnce();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(KeyColourPainter)}] Painting failed, ignored: {ex.Message}");
            }
        }
    }
}
