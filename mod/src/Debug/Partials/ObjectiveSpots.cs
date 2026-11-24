using System.Globalization;

namespace BigWalkArchipelago.Core.Checks
{
    internal static partial class ObjectiveSpots
    {
        // Debug key (Alt + 2): forgets the two latches, to try them again.
        internal static void ResetLatches()
        {
            SaveManager.SetIntValue(Latch("big_climb"), 0);
            SaveManager.SetIntValue(Latch("big_party"), 0);
            SaveManager.SetIntValue(Latch("big_doof"), 0);
            SaveManager.SetIntValue(Latch("big_makeover"), 0);
            _doofSince = -1f;
            Looks.Clear();
            Painted.Clear();
            _atTheTopSince = -1f;
            _dockedSince = -1f;
            _partyPlaying = false;
            Plugin.Log.LogInfo($"{Tag} big_climb and big_party forgotten.");
            ApNotices.Post("Debug: climb and club forgotten");
        }

        // Debug key (Alt + 1): where the local player stands, written for the .cfg line of a place.
        internal static void Mark()
        {
            var player = LocalPlayer.Find();
            if (player == null)
                return;

            var at = player.transform.position;
            var line = string.Format(CultureInfo.InvariantCulture, "{0:0.0}, {1:0.0}, {2:0.0}, 20", at.x, at.y, at.z);
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(BepInEx.Paths.BepInExRootPath, "objective-spots.txt"),
                $"{DateTime.Now:HH:mm:ss}  {line}\n");
            Plugin.Log.LogInfo($"{Tag} Marked: {line}");
            ApNotices.Post($"Spot marked: {line}");
        }
    }
}
