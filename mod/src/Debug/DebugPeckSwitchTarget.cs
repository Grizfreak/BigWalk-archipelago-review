using UnityEngine;

namespace BigWalkArchipelago.Debug
{
    // Logs the TrackedPeckState a PeckSwitch really drives (its trackedStateSystem reference).
    internal static class DebugPeckSwitchTarget
    {
        internal static void DumpNearby(float radius)
        {
            var localPlayer = Core.LocalPlayer.Find();
            if (localPlayer == null)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugPeckSwitchTarget)}] Local player not found.");
                return;
            }

            var origin = localPlayer.transform.position;
            var sqrRadius = radius * radius;

            var all = UnityEngine.Object.FindObjectsByType<PeckSwitch>(FindObjectsSortMode.None);
            if (all == null || all.Length == 0)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugPeckSwitchTarget)}] No PeckSwitch found in the current area.");
                return;
            }

            foreach (var sw in all)
            {
                if (sw == null)
                    continue;

                var sqrDistance = (sw.transform.position - origin).sqrMagnitude;
                if (sqrDistance > sqrRadius)
                    continue;

                var target = sw.trackedStateSystem;
                if (target == null)
                {
                    Plugin.Log.LogInfo(
                        $"[{nameof(DebugPeckSwitchTarget)}]   {sw.gameObject.name} (PeckSwitch), trackedStateSystem=<null>, specificState={sw.specificState}");
                    continue;
                }

                var saveIdentity = target.saveIdentity;
                var saveGuid = saveIdentity != null ? saveIdentity.saveGuid : "<no SaveIdentity>";
                var savableSystem = target.savableSystem;
                var key = savableSystem != SavableSystem.NotSavable ? savableSystem.ToString() : saveGuid;
                var currentValue = SaveManager.GetIntValue(key, -12345, false);

                Plugin.Log.LogInfo(
                    $"[{nameof(DebugPeckSwitchTarget)}]   {sw.gameObject.name} (PeckSwitch, specificState={sw.specificState}) -> " +
                    $"trackedStateSystem={target.gameObject.name}, savableSystem={savableSystem}, saveGuid={saveGuid}, " +
                    $"actual key='{key}', SaveManager[{key}]={currentValue}");
            }
        }
    }
}
