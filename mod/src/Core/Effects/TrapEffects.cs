using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    // Plays traps and bonuses on this machine's own player: the game is owner-authoritative.
    internal static partial class TrapEffects
    {
        internal const string Drop = "drop";
        internal const string Throw = "throw";
        internal const string Trip = "trip";
        internal const string Meeting = "meeting";
        internal const string Night = "night";
        internal const string Day = "day";
        internal const string Speed = "speed";
        internal const string Jump = "jump";
        internal const string Sleep = "sleep";
        internal const string Load = "load";

        // DeathLink received: what is held is dropped and the player is dazed as after a big fall.
        internal const string Knockout = "knockout";

        // The blindfold helmet on every unmasked player for the trap's duration (host only).
        internal const string Mask = "mask";

        // A flare in front of every player, in the Archipelago colours.
        internal const string Flare = "flare";

        // A puzzle's confetti and its sound around every player: filler, only a show.
        internal const string Confetti = "confetti";

        // A clock in every player's hands, on their own computer's time (host only).
        internal const string BigTime = "time";

        // The nearest puzzle still holding its gourd is reset (host only, Traps.FireReset).
        internal const string Reset = "reset";

        // Bonuses get the item sound, not the trap alert.
        internal static bool IsBonus(string key) => key == Day || key == Speed || key == Jump || key == Confetti;

        // Every effect this build plays, so a key from a newer apworld is refused by name.
        internal static readonly string[] Known = { Drop, Throw, Trip, Meeting, Night, Sleep, Load, Mask, Flare, BigTime, Silence, Slide, Ghost, Paint, Air, Goggles, Pixels, Head, Brain, Arms, Smell, Brother, Reset, Break, Day, Speed, Jump, Confetti, Knockout };

        // Gentle effects: traps that are hard to look at are not shown on this screen.
        private static bool Gentle(string key)
        {
            if (!ModConfig.GentleEffects.Value)
                return false;
            Plugin.Log.LogInfo($"{Tag} Gentle effects on this machine: {DisplayName(key)} is not shown.");
            return true;
        }

        internal static string DisplayName(string key)
        {
            if (key == Knockout)
                return "Knock-out";
            return string.IsNullOrEmpty(key) ? "?" : "Big " + char.ToUpperInvariant(key[0]) + key.Substring(1);
        }

        private const string Tag = "[" + nameof(TrapEffects) + "]";

        // Puzzle screen masks (binoculars, telescope, blindfold); masked players are not moved.
        private static readonly HashSet<int> ActiveMasks = new();

        internal static bool LocalMasked => ActiveMasks.Count > 0;

        internal static void NoteMask(PeckEffectMask mask, bool active)
        {
            if (mask == null)
                return;

            var id = mask.GetInstanceID();
            var changed = active ? ActiveMasks.Add(id) : ActiveMasks.Remove(id);
            if (changed)
                Plugin.Log.LogInfo($"{Tag} Screen mask {mask.maskType} {(active ? "on" : "off")} ('{mask.gameObject.name}'); {ActiveMasks.Count} on.");
        }

        internal static void Play(string key, int seconds, Vector3? destination, float yaw)
        {
            var player = LocalPlayer.Find();
            if (player == null)
            {
                // A guest told on joining, before its player exists: kept for when it does.
                if (key is Speed or Jump or Sleep)
                {
                    PendingMovement[key] = Time.unscaledTime + seconds;
                    Plugin.Log.LogInfo($"{Tag} {DisplayName(key)}: no local player yet; kept for {seconds}s.");
                }
                else
                {
                    Plugin.Log.LogInfo($"{Tag} {DisplayName(key)}: no local player, nothing played.");
                }

                return;
            }

            try
            {
                switch (key)
                {
                    case Drop:
                        DropHeld(player);
                        break;
                    case Slide:
                        StartSlide(seconds);
                        break;
                    case Ghost:
                        _ghostUntil = Time.unscaledTime + seconds;
                        break;
                    case Paint:
                        StartPaint(seconds);
                        break;
                    case Goggles:
                        StartGoggles(seconds);
                        break;
                    case Pixels:
                        StartPixels(seconds);
                        break;
                    case Head:
                        StartHead(seconds);
                        break;
                    case Brain:
                        StartBrain(seconds);
                        break;
                    case Arms:
                        StartArms(seconds);
                        break;
                    case Smell:
                        StartSmell(seconds);
                        break;
                    case Brother:
                        StartBrother(seconds);
                        break;
                    case Break:
                        StartBreak(destination);
                        break;
                    case Air:
                        StartCountdown(key, player, Vector3.zero, Quaternion.identity);
                        _countdownLaunch = true;
                        break;
                    case Silence:
                        StartSilence(player, seconds);
                        break;
                    case Flare:
                        BigFlare.Play();
                        break;
                    case Confetti:
                        BigConfetti.Play();
                        break;
                    case Knockout:
                        DropHeld(player);
                        if (player.faller != null)
                            player.faller.TriggerFall();
                        break;
                    case Throw:
                        StartThrow(player);
                        break;
                    case Trip:
                    case Meeting:
                        if (destination != null)
                            StartCountdown(key, player, destination.Value, Quaternion.Euler(0f, yaw, 0f));
                        else
                            Plugin.Log.LogInfo($"{Tag} {DisplayName(key)}: no destination for this player (spared).");
                        break;
                    case Night:
                        StartSky(MidnightHour); // the same hour for everyone
                        break;
                    case Day:
                        StartSky(NoonHour);
                        break;
                    case Speed:
                    case Jump:
                    case Sleep:
                        StartMovement(player, seconds, key);
                        break;
                    default:
                        Plugin.Log.LogWarning($"{Tag} Unknown effect '{key}', ignored (a newer apworld than this mod?).");
                        return;
                }

                Plugin.Log.LogInfo($"{Tag} {DisplayName(key)} played ({seconds}s).");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} {DisplayName(key)} failed: {ex.Message}");
            }
        }

        internal static void Tick()
        {
            var now = Time.unscaledTime;

            if (_throwAt >= 0f && now >= _throwAt)
                FinishThrow();
            else if (_throwAt >= 0f)
                HoldWindUp();

            if (PendingMovement.Count > 0)
                PlayPendingMovement(now);

            TickMovement(now);

            if (_slideUntil >= 0f && now >= _slideUntil)
                EndSlide();

            if (_ghostUntil >= 0f && now >= _ghostUntil)
                EndGhost();

            if (_paintUntil >= 0f && now >= _paintUntil)
                EndPaint();

            if (_gogglesUntil >= 0f)
                StepGoggles(now);

            if (_headUntil >= 0f)
                StepHead(now);

            if (_armsUntil >= 0f)
                StepArms(now);

            if (_smellUntil >= 0f)
                StepSmell(now);

            if (_brotherUntil >= 0f)
                StepBrother(now);

            if (_breakUntil >= 0f)
                StepBreak(now);

            if (_pixelsUntil >= 0f)
            {
                if (now >= _pixelsUntil)
                    EndPixels();
                else
                    HoldPixels(UrpAsset());
            }

            if (_silenceUntil >= 0f)
            {
                if (now >= _silenceUntil)
                    EndSilence();
                else
                    HoldSilence();
            }

            if (_skyRunning)
                StepSky();

            if (_countdownEnd >= 0f)
                StepCountdown();

            if (_launchUntil >= 0f)
                HoldLaunch();
        }

        // After the animation, which places the head again every frame.
        internal static void LateTick()
        {
            LateBrain();
            LateArms();
            if (_headUntil < 0f || _headFactor <= 1f)
                return;
            var lift = Vector3.up * (_headFactor - 1f) * HeadLift;
            foreach (var head in Heads.Values)
            {
                if (head.bone != null)
                    head.bone.position += lift;
            }
        }

        // The world is going away: everything is put back while the objects still exist.
        internal static void EndAll()
        {
            _throwAt = -1f;
            _throwPlayer = null;
            _speedUntil = _jumpUntil = _sleepUntil = -1f;
            SetAsleep(false);
            RestoreMovement();
            EndSlide();
            EndSilence();
            EndGhost();
            EndPaint();
            EndGoggles();
            EndPixels();
            EndHead();
            EndBrain();
            EndArms();
            EndSmell();
            EndBrother();
            _countdownEnd = -1f;
            _countdownPlayer = null;

            if (_skyRunning)
            {
                // Never leave the clock frozen part way.
                _skyRunning = false;
                try { SkyManager.ClearFixedTime(); }
                catch (Exception) { }
            }

            ActiveMasks.Clear();
        }

        internal static string Describe()
        {
            var now = Time.unscaledTime;
            return $"masked={LocalMasked} ({ActiveMasks.Count}), "
                   + $"throw in {(_throwAt >= 0f ? (_throwAt - now).ToString("F1") + "s" : "-")}, "
                   + $"speed {(_speedUntil >= 0f ? (_speedUntil - now).ToString("F0") + "s left" : "off")}, "
                   + $"jump {(_jumpUntil >= 0f ? (_jumpUntil - now).ToString("F0") + "s left" : "off")}, "
                   + $"sleep {(_sleepUntil >= 0f ? (_sleepUntil - now).ToString("F0") + "s left" : "off")}, "
                   + $"sky {(_skyRunning ? $"fast forward to {_skyTo:F2}h" : "free")}, "
                   + $"hour {SafeHour():F2}";
        }

        private static float SafeHour()
        {
            try
            {
                return SkyManager.GetCurrentTime();
            }
            catch (Exception)
            {
                return -1f;
            }
        }
    }
}
