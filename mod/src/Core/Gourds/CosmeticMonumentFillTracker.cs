using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Gourds
{
    // Saves which monument slots hold a cosmetic gourd, keyed by home name, and restores them.
    internal class CosmeticMonumentFillTracker : MonoBehaviour
    {
        // Constructor required by Il2CppInterop for any type injected into IL2CPP.
        public CosmeticMonumentFillTracker(IntPtr ptr) : base(ptr)
        {
        }

        private const string SaveKeyPrefix = "ap_home_";

        // All 45 monument slots (apworld `data.TOWERS`), in the order deposits come back in.
        private static readonly (string Prefix, int Slots)[] MonumentSlots =
        {
            ("monoumentIntro", 4),
            ("monoument0", 5), ("monoument1", 5), ("monoument2", 5), ("monoument3", 5),
            ("monoumentFinal", 6),
            ("monoumentOverflow", 15),
        };

        // New save on a known slot: marks that many slots filled, in order; returns how many.
        internal static int PutDepositsBack(int count)
        {
            var marked = 0;
            // In the seed's order when the monuments open one after the other (`tower_mode`).
            foreach (var (prefix, slots) in MonumentOrder.OrderedMonuments() ?? MonumentSlots)
            {
                for (var slot = 0; slot < slots && marked < count; slot++)
                {
                    var key = $"{SaveKeyPrefix}{prefix}Slot{slot}";
                    if (SaveManager.GetIntValue(key, 0, false) != 0)
                        continue;

                    SaveManager.SetIntValue(key, 1);
                    marked++;
                }
            }

            // Monuments already loaded were checked before these keys existed: look at them again.
            var loaded = UnityEngine.Object.FindObjectsByType<PropHome>(FindObjectsSortMode.None);
            if (loaded != null)
            {
                foreach (var home in loaded)
                {
                    if (ReceivedItemSpawner.IsMonumentHome(home))
                        PendingRestoreCheck.Enqueue(home);
                }
            }

            return marked;
        }

        // Fed by PropHomeEnablePatch as homes stream in; drained once the world is ready.
        private static readonly Queue<PropHome> PendingRestoreCheck = new Queue<PropHome>();

        // Subscribed at once; the restore waits, as a pin before the peck manager exists fails.
        internal static void RegisterHome(PropHome home)
        {
            if (home == null)
                return;

            home.onChangeServer += (Action<PropHome, Prop, Prop>)OnHomeChanged;
            MonumentOrder.Learn(home);
            PendingRestoreCheck.Enqueue(home);
        }

        // Counted from the save's `ap_home_*` keys: monuments not streamed in yet still count.
        internal static int GetFilledMonumentCount()
        {
            var data = SaveManager.instance != null ? SaveManager.instance.currentData : null;
            if (data == null || data.entries == null)
                return 0;

            var count = 0;
            var entries = data.entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.value != 0 && entry.key != null
                    && entry.key.StartsWith(SaveKeyPrefix, StringComparison.Ordinal))
                    count++;
            }

            return count;
        }

        // How many slots of one monument hold a cosmetic gourd, by the same save entries.
        internal static int FilledSlots(string prefix)
        {
            var data = SaveManager.instance != null ? SaveManager.instance.currentData : null;
            if (data == null || data.entries == null)
                return 0;

            var start = SaveKeyPrefix + prefix + "Slot";
            var count = 0;
            var entries = data.entries;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.value != 0 && entry.key != null && entry.key.StartsWith(start, StringComparison.Ordinal))
                    count++;
            }

            return count;
        }

        private void Update()
        {
            if (!NetworkServer.active || !WorldManager.isReadyForEffects || PendingRestoreCheck.Count == 0)
                return;

            var restoredCount = 0;
            while (PendingRestoreCheck.Count > 0)
            {
                if (TryRestoreHome(PendingRestoreCheck.Dequeue()))
                    restoredCount++;
            }

            if (restoredCount > 0)
                Plugin.Log.LogInfo($"[{nameof(CosmeticMonumentFillTracker)}] {restoredCount} cosmetic gourd(s) restored to their monuments.");
        }

        private static bool TryRestoreHome(PropHome home)
        {
            // Never overwrite a home holding a real gourd or a clone restored just before.
            if (home == null || home.pinnedProp != null || !ReceivedItemSpawner.IsMonumentHome(home))
                return false;

            var key = SaveKeyPrefix + home.saveableHomeName;
            if (SaveManager.GetIntValue(key, 0, false) == 0)
                return false;

            return ReceivedItemSpawner.SpawnCosmeticPickupPinnedTo(home) != null;
        }

        private static void OnHomeChanged(PropHome propHome, Prop propBefore, Prop propAfter)
        {
            if (propHome == null || !ReceivedItemSpawner.IsMonumentHome(propHome))
                return;

            // The key reflects whether a cosmetic clone occupies this home right now.
            var isCosmeticNow = ReceivedItemSpawner.IsCosmeticClone(propAfter);
            var key = SaveKeyPrefix + propHome.saveableHomeName;
            var newValue = isCosmeticNow ? 1 : 0;

            if (SaveManager.GetIntValue(key, 0, false) == newValue)
                return;

            SaveManager.SetIntValue(key, newValue);
            Plugin.Log.LogInfo(
                $"[{nameof(CosmeticMonumentFillTracker)}] {key} = {newValue} (cosmetic gourd {(isCosmeticNow ? "deposited" : "removed")}).");
        }
    }
}
