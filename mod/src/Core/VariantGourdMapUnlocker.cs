using System;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Keeps variant gourds revealed: GourdMap.Initialize hides them again on every zone change.
    internal class VariantGourdMapUnlocker : MonoBehaviour
    {
        // Constructor required by Il2CppInterop for any type injected into IL2CPP.
        public VariantGourdMapUnlocker(IntPtr ptr) : base(ptr)
        {
        }

        private const float PollIntervalSeconds = 2f;
        private float _nextPollTime;

        // A safety net: GourdMap.Initialize is what hides the flags, and it says so.
        private const float QuietPollSeconds = 60f;
        private static bool _dirty = true;
        private float _nextQuietPoll;

        internal static void FlagsReset() => _dirty = true;

        private void Update()
        {
            if (!WorldManager.isReadyForEffects)
            {
                _dirty = true;
                return;
            }

            if (Time.time < _nextPollTime)
                return;

            _nextPollTime = Time.time + PollIntervalSeconds;
            if (!_dirty && Time.time < _nextQuietPoll)
                return;

            // The host reveals only once its slot says `include_big_game_puzzles` from_start.
            var isGuest = Mirror.NetworkClient.active && !Mirror.NetworkServer.active;
            var slot = Net.ApRuntime.SlotData;
            if (!isGuest && ModConfig.ArchipelagoEnabled.Value && (slot == null || slot.IncludeBigGamePuzzles != "from_start"))
                return;
            _dirty = false;
            _nextQuietPoll = Time.time + QuietPollSeconds;
            VariantGourdRevealer.RevealAll();
        }
    }
}
