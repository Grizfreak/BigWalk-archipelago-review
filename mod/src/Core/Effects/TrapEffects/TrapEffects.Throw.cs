using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    internal static partial class TrapEffects
    {
        private const float ThrowHoldSeconds = 1f;

        private static PlayerCharacter _throwPlayer;
        private static float _throwAt = -1f;
        private static double _throwWindUpStart;
        private static bool _cancelRefusedLogged;

        private static void DropHeld(PlayerCharacter player)
        {
            if (player.hands == null || !player.hands.isHoldingSomething)
            {
                Plugin.Log.LogInfo($"{Tag} Big Drop: hands already empty.");
                return;
            }

            player.decisions?.ClearWindUp();
            player.actions.ActionDropPropOrPlayer();
        }

        private static void StartThrow(PlayerCharacter player)
        {
            if (player.hands == null || !player.hands.isHoldingSomething)
            {
                Plugin.Log.LogInfo($"{Tag} Big Throw: nothing in the hands to throw.");
                return;
            }

            _throwWindUpStart = NetworkTime.time;
            TrapIsThrowing = true;
            try { player.decisions.SetWindUpStartTime(_throwWindUpStart); }
            finally { TrapIsThrowing = false; }
            _throwPlayer = player;
            _cancelRefusedLogged = false;
            var windUp = player.tunings != null ? Mathf.Max(0f, player.tunings.maxWindUpDuration) : 1f;
            _throwAt = Time.unscaledTime + windUp + ThrowHoldSeconds;
        }

        // The game's cancel clears the wind-up, so it is put back every frame until the throw.
        private static void HoldWindUp()
        {
            var player = _throwPlayer;
            try
            {
                if (player == null || player.decisions == null || player.hands == null || !player.hands.isHoldingSomething)
                    return;

                if (player.decisions.localWindUpStartTime == _throwWindUpStart)
                    return;

                TrapIsThrowing = true;
                try { player.decisions.SetWindUpStartTime(_throwWindUpStart); }
                finally { TrapIsThrowing = false; }
                if (!_cancelRefusedLogged)
                {
                    _cancelRefusedLogged = true;
                    Plugin.Log.LogInfo($"{Tag} Big Throw: the wind-up was cancelled or changed; put back.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogInfo($"{Tag} Big Throw: could not hold the wind-up ({ex.Message}).");
            }
        }

        // Lets the trap's own throw through the Throwing lock (AbilityLockPatches).
        internal static bool TrapIsThrowing { get; private set; }

        private static void FinishThrow()
        {
            var player = _throwPlayer;
            _throwAt = -1f;
            _throwPlayer = null;
            TrapIsThrowing = true;

            try
            {
                if (player == null || player.hands == null)
                    return;

                if (!player.hands.isHoldingSomething)
                {
                    player.decisions?.ClearWindUp();
                    Plugin.Log.LogInfo($"{Tag} Big Throw: the hands were empty by the end of the wind-up.");
                    return;
                }

                player.actions.ActionDropPropOrPlayer();
                Plugin.Log.LogInfo($"{Tag} Big Throw: thrown.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Big Throw failed at the end of the wind-up: {ex.Message}");
            }
            finally
            {
                TrapIsThrowing = false;
            }
        }
    }
}
