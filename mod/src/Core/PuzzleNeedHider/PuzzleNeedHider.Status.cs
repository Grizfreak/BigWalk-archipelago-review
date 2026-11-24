using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal partial class PuzzleNeedHider : MonoBehaviour
    {
        internal static void LogStatus(bool detailed)
        {
            var self = _current;
            if (self == null || !self._collected)
            {
                Plugin.Log.LogInfo($"[{nameof(PuzzleNeedHider)}] {Stamp()} status: no world collected yet.");
                return;
            }

            var role = NetworkServer.active ? "host" : NetworkClient.active ? "guest" : "solo";
            var locked = PuzzleNeeds.LockedNeeds();
            var perNeed = self._objects.OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => (need: p.Key, total: p.Value.Count(o => o != null),
                    showing: p.Value.Count(o => o != null && self.IsShowing(o))))
                .ToList();

            var summary = $"{locked.Count} locked of {perNeed.Count}, "
                          + $"{perNeed.Sum(n => n.showing)} of {perNeed.Sum(n => n.total)} objects showing";
            Plugin.Log.LogInfo($"[{nameof(PuzzleNeedHider)}] {Stamp()} status ({role}): {summary}.");
            SessionJournal.Write(detailed ? "mark" : "status", $"{summary} | locked: {string.Join(",", locked)}");

            if (!detailed)
                return;

            foreach (var (need, total, showing) in perNeed)
                Plugin.Log.LogInfo(
                    $"[{nameof(PuzzleNeedHider)}]   {need}: {(IsLocked(need) ? "locked" : "open  ")} {showing}/{total} showing");
        }

        // A replicated object stays active, so ask what this hider made invisible.
        private bool IsShowing(GameObject go) =>
            go.activeSelf && !(_networked.Contains(go.GetInstanceID()) && _hiddenByUs.Contains(go.GetInstanceID()));

        internal static string Stamp() => DateTime.Now.ToString("HH:mm:ss");
    }
}
