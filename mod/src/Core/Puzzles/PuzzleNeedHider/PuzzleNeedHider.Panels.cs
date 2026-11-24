using UnityEngine;

namespace BigWalkArchipelago.Core.Puzzles
{
    internal partial class PuzzleNeedHider : MonoBehaviour
    {
        private static readonly string[] PanelNeeds =
        {
            PuzzleNeeds.IconPanels, PuzzleNeeds.DrawingPanels, PuzzleNeeds.PosePanels, PuzzleNeeds.SoundPanels,
            PuzzleNeeds.PointPanels,
        };

        // A tile's PropGroup family decides its need; speaker tiles are told apart by their rack.
        private static void CollectPanels(Dictionary<string, Dictionary<int, GameObject>> found, HashSet<int> gauntletIds)
        {
            var renderers = FindObjectsByType<PegTileRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (renderers == null)
                return;

            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.GetComponent<Prop>() == null)
                    continue;

                var path = SceneObjects.PathOf(renderer.transform);
                if (!path.StartsWith(PuzzleRoot, StringComparison.Ordinal) || IsExcluded(path))
                    continue;

                var need = PanelNeed(renderer.propGroup.ToString(), path);
                if (need == null)
                    continue;

                found[need][renderer.gameObject.GetInstanceID()] = renderer.gameObject;
                if (path.Contains(GauntletRoot))
                    gauntletIds.Add(renderer.gameObject.GetInstanceID());
            }
        }

        private static string PanelNeed(string group, string path)
        {
            if (path.Contains("PegTileSetSpeaker"))
                return PuzzleNeeds.SoundPanels;
            if (group.StartsWith("PegTilePriest", StringComparison.Ordinal))
                return PuzzleNeeds.DrawingPanels;

            const string stem = "PegTile";
            if (group.Length <= stem.Length + 1 || !char.IsDigit(group[stem.Length + 1]))
                return null;

            switch (group[stem.Length])
            {
                case 'A': return PuzzleNeeds.IconPanels;
                case 'B': return PuzzleNeeds.PointPanels;
                case 'C': return PuzzleNeeds.DrawingPanels;
                case 'D': return PuzzleNeeds.PosePanels;
                default: return null;
            }
        }
    }
}
