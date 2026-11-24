using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Ctrl+F8 puts the next puzzle screen mask on this machine for five seconds.
    internal static class DebugMaskTrap
    {
        private const string Tag = "[" + nameof(DebugMaskTrap) + "]";
        private const float Seconds = 5f;

        private static int _next;
        private static PeckEffectMask _on;
        private static float _offAt;

        internal static void Next()
        {
            if (_on != null)
                Off();

            var kinds = new[] { PeckEffectMask.MaskType.Binoculars, PeckEffectMask.MaskType.Telescope, PeckEffectMask.MaskType.Blindfold };
            var kind = kinds[_next++ % kinds.Length];

            foreach (var mask in UnityEngine.Object.FindObjectsByType<PeckEffectMask>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mask == null || mask.maskType != kind)
                    continue;
                mask.SetMask(true);
                _on = mask;
                _offAt = Time.unscaledTime + Seconds;
                Plugin.Log.LogInfo($"{Tag} {kind} on for {Seconds:0}s (from {mask.gameObject.name}).");
                return;
            }

            Plugin.Log.LogInfo($"{Tag} No {kind} mask in this world.");
        }

        // Ctrl+F9 dazes the local player as after a big fall.
        internal static void Daze()
        {
            var player = Core.LocalPlayer.Find();
            var faller = player != null ? player.faller : null;
            if (faller == null)
            {
                Plugin.Log.LogInfo($"{Tag} No local player to daze.");
                return;
            }
            faller.TriggerFall();
            Plugin.Log.LogInfo($"{Tag} TriggerFall on {player.gameObject.name}: dazed={faller.isDazed}.");
        }

        internal static void Tick()
        {
            if (_on != null && Time.unscaledTime >= _offAt)
                Off();
        }

        private static void Off()
        {
            try
            {
                _on.SetMask(false);
                Plugin.Log.LogInfo($"{Tag} {_on.maskType} off.");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Could not take the mask off: {ex.Message}");
            }
            _on = null;
        }
    }
}
