using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Keys
{
    internal static partial class KeyCustody
    {
        // blockGrabbing does not stop a full monument launching its key, so the release is refused.
        private static readonly Dictionary<int, SaveablePropName> KeyByLogic = new();
        private static readonly HashSet<int> NotAReleaseLogic = new();

        internal static bool IsReleaseHeld(TrackedPeckState state, out SaveablePropName propName)
        {
            propName = SaveablePropName.notSavable;
            if (!KeysAreItems || state == null)
                return false;

            var id = state.GetInstanceID();
            if (!KeyByLogic.TryGetValue(id, out propName))
            {
                if (NotAReleaseLogic.Contains(id))
                    return false;

                var name = state.gameObject.name;
                if (name != "KeyStoneLogic" && name != "KeyScrewLogic")
                {
                    NotAReleaseLogic.Add(id);
                    return false;
                }

                // Not cached while its key is not loaded: asked again next time.
                if (!TryFindKeyOf(state, out propName))
                    return false;

                KeyByLogic[id] = propName;
            }

            return !IsGranted(propName);
        }

        // The key whose stone shares the logic's parent (BigKeyInStone, BigKeyInBox, BigKeyInScrew)
        private static bool TryFindKeyOf(TrackedPeckState logic, out SaveablePropName propName)
        {
            propName = SaveablePropName.notSavable;
            var parent = logic.transform.parent;
            if (parent == null)
                return false;

            foreach (var prop in GourdRegistry.LoadedBigKeys())
            {
                var stone = prop.startHome;
                if (stone != null && stone.transform.IsChildOf(parent))
                {
                    propName = prop.saveablePropName;
                    return true;
                }
            }

            return false;
        }

        internal static void ForgetReleaseLogics()
        {
            KeyByLogic.Clear();
            NotAReleaseLogic.Clear();
        }
    }
}
