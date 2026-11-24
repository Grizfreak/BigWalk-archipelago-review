using HarmonyLib;

namespace BigWalkArchipelago.Patches.Puzzles
{
    // `lock_endings`: the secret room's printer prints nothing while the other objectives are open.
    [HarmonyPatch(typeof(GoodbyePrinter), "OnQueuePeck")]
    internal static class GoodbyePrinterHoldPatch
    {
        // Refused print, asked again once the lock is lifted; otherwise the request would be lost.
        private static GoodbyePrinter _refusedPrinter;
        private static PeckContext _refusedContext;

        private static bool Prefix(GoodbyePrinter __instance, PeckContext __0)
        {
            if (!ObjectiveBoard.HoldsTheLastStairway || !ObjectiveBoard.Locked("big_game"))
                return true;

            Plugin.Log.LogInfo($"[{nameof(GoodbyePrinterHoldPatch)}] The printer stays shut: the objectives are not done.");
            ObjectiveBoard.SayEndingIsShut("big_game");
            _refusedPrinter = __instance;
            _refusedContext = __0;
            return false;
        }

        // On the host: once the lock is off, the refused request is made again.
        internal static void ReplayIfUnlocked()
        {
            if (_refusedPrinter == null || !Mirror.NetworkServer.active)
                return;

            if (ObjectiveBoard.HoldsTheLastStairway && ObjectiveBoard.Locked("big_game"))
                return;

            var printer = _refusedPrinter;
            var context = _refusedContext;
            _refusedPrinter = null;
            _refusedContext = null;
            try
            {
                Plugin.Log.LogInfo($"[{nameof(GoodbyePrinterHoldPatch)}] The lock is off: the refused print is asked again.");
                ObjectiveBoard.Say("Big Game: the printer works again");
                printer.OnQueuePeck(context);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(GoodbyePrinterHoldPatch)}] The refused print could not be asked again: {ex.Message}");
            }
        }
    }
}
