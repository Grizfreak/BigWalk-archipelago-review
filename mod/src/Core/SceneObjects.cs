using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal static class SceneObjects
    {
        internal static string PathOf(Transform transform)
        {
            var parts = new List<string>();
            for (var t = transform; t != null; t = t.parent)
                parts.Add(t.name);
            parts.Reverse();
            return string.Join("/", parts);
        }

        internal static bool IsHeld(Prop prop)
        {
            var players = PlayerCharacter.allPlayerCharacters;
            if (players == null)
                return false;

            foreach (var pc in players)
            {
                if (pc != null && pc.hands != null && pc.hands.heldProp == prop)
                    return true;
            }

            return false;
        }
    }
}
