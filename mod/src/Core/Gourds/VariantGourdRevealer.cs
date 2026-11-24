using System.Collections.Generic;
namespace BigWalkArchipelago.Core.Gourds
{
    // Reveals the purple variant gourds on the map, which GourdMap.Initialize hides on each load.
    internal static class VariantGourdRevealer
    {
        internal static int RevealAll()
        {
            var flags = UnityEngine.Object.FindObjectsByType<GourdFlag>(UnityEngine.FindObjectsSortMode.None);
            if (flags == null || flags.Length == 0)
                return 0;

            // Hidden flags first: most polls find none, and then no gourd is searched.
            var hidden = new List<GourdFlag>();
            foreach (var flag in flags)
            {
                if (flag != null && flag.gourdState == GourdFlag.GourdState.Hidden)
                    hidden.Add(flag);
            }
            if (hidden.Count == 0)
                return 0;

            var variants = new HashSet<SaveablePropName>();
            foreach (var gourd in UnityEngine.Object.FindObjectsByType<RewardGourd>(UnityEngine.FindObjectsSortMode.None))
            {
                if (gourd != null && gourd.isVariantChallenge && gourd.prop != null)
                    variants.Add(gourd.prop.saveablePropName);
            }

            var count = 0;
            foreach (var flag in hidden)
            {
                if (!variants.Contains(flag.saveablePropName))
                    continue;

                // Not the static GourdMap.refreshFlag delegate: invoking it froze the game later.
                flag.SetState(GourdFlag.GourdState.Locked);
                count++;
            }

            return count;
        }
    }
}
