using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    internal static partial class Traps
    {
        // The floor under a point, looked for from a little above it.
        internal static Vector3 Ground(Vector3 point)
        {
            if (Physics.Raycast(point + Vector3.up * 2.5f, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 0.8f;
            return point + Vector3.up * 0.5f;
        }
    }
}
