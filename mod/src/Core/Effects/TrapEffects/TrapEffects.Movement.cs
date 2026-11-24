using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    internal static partial class TrapEffects
    {
        private const float SpeedFactor = 1.75f;
        private const float JumpFactor = 1.8f;

        private static PlayerTunings _moveTunings;
        private static float[] _moveBase;
        private static float _speedUntil = -1f;
        private static float _jumpUntil = -1f;
        private static float _sleepUntil = -1f;
        private static float _sleepStart;
        private static PlayerCharacter _sleeper;

        // Big Speed, Big Jump or Big Sleep holds this machine's tunings (BigRunCompat).
        internal static bool MovementActive => _moveTunings != null;

        private const float AsleepSeconds = 2f;
        private const float AwakeSeconds = 5f;

        // Lasting effects received before this machine's player existed, and until when.
        private static readonly Dictionary<string, float> PendingMovement = new();

        // While on, BigSlidePatch lets the velocity drift towards what the controls ask for.
        internal const string Slide = "slide";
        private static float _slideUntil = -1f;

        // Share of the asked-for velocity taken each fixed step (50 a second).
        internal const float SlideGrip = 0.025f;

        internal static bool SlideActive => _slideUntil >= 0f;

        private static void StartSlide(int seconds)
        {
            _slideUntil = Mathf.Max(_slideUntil, Time.unscaledTime + seconds);
            Plugin.Log.LogInfo($"{Tag} Big Slide: the ground slips for {seconds}s (grip {SlideGrip}).");
        }

        private static void EndSlide()
        {
            if (_slideUntil < 0f)
                return;
            _slideUntil = -1f;
            Plugin.Log.LogInfo($"{Tag} Big Slide over.");
        }

        // All three write the same tunings from the base values, so none can leave them changed.
        private static void StartMovement(PlayerCharacter player, int seconds, string key)
        {
            var tunings = player.tunings;
            if (tunings == null)
            {
                Plugin.Log.LogInfo($"{Tag} {DisplayName(key)}: the player has no tunings.");
                return;
            }

            if (_moveTunings != null && _moveTunings.Pointer != tunings.Pointer)
                RestoreMovement();

            if (_moveTunings == null)
            {
                _moveTunings = tunings;
                _moveBase = new[]
                {
                    tunings.forwardSpeed, tunings.forwardSprintSpeed, tunings.crouchForwardSpeed,
                    tunings.crouchForwardSprintSpeed, tunings.swimForwardSpeed, tunings.swimForwardSprintSpeed,
                    tunings.forwardSprintGhostSpeed, tunings.jumpForce, tunings.maxUpwardsVelocity,
                    tunings.mouseLookSpeed, tunings.stickLookSpeed,
                };
            }

            var until = Time.unscaledTime + seconds;
            switch (key)
            {
                case Speed:
                    _speedUntil = until;
                    break;
                case Jump:
                    _jumpUntil = until;
                    break;
                case Sleep:
                    if (_sleepUntil < 0f)
                        _sleepStart = Time.unscaledTime;
                    _sleepUntil = until;
                    _sleeper = player;
                    break;
            }

            ApplyMovement();
        }

        private static void PlayPendingMovement(float now)
        {
            var player = LocalPlayer.Find();
            if (player == null)
                return;

            foreach (var pair in new List<KeyValuePair<string, float>>(PendingMovement))
            {
                var left = Mathf.CeilToInt(pair.Value - now);
                if (left > 0)
                {
                    StartMovement(player, left, pair.Key);
                    Plugin.Log.LogInfo($"{Tag} {DisplayName(pair.Key)} played now that the player is here ({left}s left).");
                }
            }

            PendingMovement.Clear();
        }

        private static void TickMovement(float now)
        {
            if (_speedUntil >= 0f && now >= _speedUntil)
            {
                _speedUntil = -1f;
                Plugin.Log.LogInfo($"{Tag} Big Speed over.");
            }

            if (_jumpUntil >= 0f && now >= _jumpUntil)
            {
                _jumpUntil = -1f;
                Plugin.Log.LogInfo($"{Tag} Big Jump over.");
            }

            if (_sleepUntil >= 0f && now >= _sleepUntil)
            {
                _sleepUntil = -1f;
                SetAsleep(false);
                Plugin.Log.LogInfo($"{Tag} Big Sleep over.");
            }

            if (_moveTunings == null)
                return;

            if (_speedUntil < 0f && _jumpUntil < 0f && _sleepUntil < 0f)
                RestoreMovement();
            else
                ApplyMovement();
        }

        private static void ApplyMovement()
        {
            var t = _moveTunings;
            var b = _moveBase;
            if (t == null || b == null)
                return;

            try
            {
                var frozen = _sleepUntil >= 0f
                             && (Time.unscaledTime - _sleepStart) % (AsleepSeconds + AwakeSeconds) < AsleepSeconds;
                SetAsleep(frozen);
                var move = frozen ? 0f : _speedUntil >= 0f ? SpeedFactor : 1f;
                var jump = frozen ? 0f : _jumpUntil >= 0f ? JumpFactor : 1f;
                t.forwardSpeed = b[0] * move;
                t.forwardSprintSpeed = b[1] * move;
                t.crouchForwardSpeed = b[2] * move;
                t.crouchForwardSprintSpeed = b[3] * move;
                t.swimForwardSpeed = b[4] * move;
                t.swimForwardSprintSpeed = b[5] * move;
                t.forwardSprintGhostSpeed = b[6] * move;
                t.jumpForce = b[7] * jump;
                t.maxUpwardsVelocity = frozen ? b[8] : b[8] * jump;

                // The camera is frozen too: looking round woke the walker up.
                t.mouseLookSpeed = frozen ? 0f : b[9];
                t.stickLookSpeed = frozen ? 0f : b[10];
            }
            catch (Exception ex)
            {
                // The player object went with its world.
                Plugin.Log.LogInfo($"{Tag} Movement effects dropped (the player is gone: {ex.Message}).");
                _moveTunings = null;
                _moveBase = null;
            }
        }

        private static bool _asleep;

        // Big Sleep has the walker asleep now; TrapPatches blocks crouching meanwhile.
        internal static bool Asleep => _asleep;

        private static void SetAsleep(bool asleep)
        {
            if (_asleep == asleep)
                return;

            _asleep = asleep;
            try
            {
                if (_sleeper != null && _sleeper.sleeper != null)
                {
                    _sleeper.sleeper.forceSleeping = asleep;

                    // Not fully: the game only animates when sleepiness changes between frames.
                    if (asleep)
                        _sleeper.sleeper.smoothSleepiness = Mathf.Max(_sleeper.sleeper.smoothSleepiness, 0.85f);
                    else
                        // forceSleeping zeroed the idle clock, which would keep the walker asleep.
                        _sleeper.sleeper.RecordAction();
                }
            }
            catch (Exception)
            {
                // The player went with its world.
            }

            if (!asleep && _sleepUntil < 0f)
                _sleeper = null;
        }

        private static void RestoreMovement()
        {
            var t = _moveTunings;
            var b = _moveBase;
            _moveTunings = null;
            _moveBase = null;
            if (t == null || b == null)
                return;

            try
            {
                t.forwardSpeed = b[0];
                t.forwardSprintSpeed = b[1];
                t.crouchForwardSpeed = b[2];
                t.crouchForwardSprintSpeed = b[3];
                t.swimForwardSpeed = b[4];
                t.swimForwardSprintSpeed = b[5];
                t.forwardSprintGhostSpeed = b[6];
                t.jumpForce = b[7];
                t.maxUpwardsVelocity = b[8];
                t.mouseLookSpeed = b[9];
                t.stickLookSpeed = b[10];
            }
            catch (Exception)
            {
                // The player object went with its world: nothing left to put back.
            }
        }
    }
}
