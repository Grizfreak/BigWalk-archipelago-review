namespace BigWalkArchipelago.Debug
{
    // Simulates a received item through the real ItemApplier path, on the nearest uncollected prop.
    internal static class DebugItemSimulator
    {
        internal static void SimulateReceiveNext()
        {
            var prop = DebugGourdLookup.FindNearestUncollectedProp();
            if (prop == null)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(DebugItemSimulator)}] No more uncollected gourd/big key in the current area.");
                return;
            }

            Plugin.Log.LogInfo(
                $"[{nameof(DebugItemSimulator)}] Simulating item reception: {prop.saveablePropName}");
            Dispatch(prop.saveablePropName);
        }

        // For a big key that is not in the loaded area.
        internal static void SimulateReceive(SaveablePropName propName)
        {
            Plugin.Log.LogInfo(
                $"[{nameof(DebugItemSimulator)}] Simulating item reception (targeted): {propName}");
            Dispatch(propName);
        }

        private static int _nextGadgetIndex;
        // Kinds an item can give: not the trap tomato nor the Big Mask helmet.
        private static readonly GadgetKind[] GadgetKinds = System.Array.FindAll(
            (GadgetKind[])System.Enum.GetValues(typeof(GadgetKind)), kind => kind != GadgetKind.Pomodoro && kind != GadgetKind.Blindfold);

        internal static void SimulateReceiveNextGadget()
        {
            var kind = GadgetKinds[_nextGadgetIndex];
            _nextGadgetIndex = (_nextGadgetIndex + 1) % GadgetKinds.Length;

            Plugin.Log.LogInfo(
                $"[{nameof(DebugItemSimulator)}] Simulating item reception: {kind}");

            // Counted like a real item, so the reload persistence can be tested.
            ApItemCursor.CountGadgetReceived(kind);
            ItemApplier.ApplyGadgetItem(kind, toPlayer: true);
        }

        // ApplyGourdItem is generic; the prop name only picks which path to take.
        private static void Dispatch(SaveablePropName propName)
        {
            if (GourdRegistry.IsBigKey(propName))
                ItemApplier.ApplyBigKeyItem(propName);
            else
                ItemApplier.ApplyGourdItem(toPlayer: true);
        }
    }
}
