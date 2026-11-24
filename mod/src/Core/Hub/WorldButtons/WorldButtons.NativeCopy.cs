using UnityEngine;

namespace BigWalkArchipelago.Core.Hub
{
    internal partial class WorldButtons
    {
        // Built under an inactive parent so nothing registers with the original's references.
        private static bool TryBuildNative(GameObject root, int index, Button button)
        {
            var template = GetTemplate(button.Template);
            if (template == null)
                return false;

            var holder = new GameObject("AP button holder");
            holder.SetActive(false);
            try
            {
                var clone = UnityEngine.Object.Instantiate(template, holder.transform);
                clone.name = "AP native button";

                var ticket = FreeTicketBlock(index);
                var firstTicket = ticket;
                // A switch used from afar reaches the host by shellReference: point it at the copy.
                foreach (var peckSwitch in clone.GetComponentsInChildren<PeckSwitch>(true))
                {
                    peckSwitch.useTicket = true;
                    peckSwitch.ticket = (ushort)ticket;
                    peckSwitch.shellReference = new SeaShell.ShellReference((ushort)ticket);
                    ticket++;
                }

                // SaveIdentity would tie the copy to the original's save and network entry.
                foreach (var identity in clone.GetComponentsInChildren<SaveIdentity>(true))
                    UnityEngine.Object.DestroyImmediate(identity);

                foreach (var lamp in clone.GetComponentsInChildren<Light>(true))
                    UnityEngine.Object.DestroyImmediate(lamp.gameObject);

                TrackedPeckState own = null;
                foreach (var state in clone.GetComponentsInChildren<TrackedPeckState>(true))
                {
                    state.saveIdentity = null;
                    state.savableSystem = SavableSystem.NotSavable;
                    // A fresh ticket too: the original's gave "Duplicate ticket" and lost presses.
                    state.ticket = (ushort)ticket;
                    state.shellReference = new SeaShell.ShellReference((ushort)ticket);
                    ticket++;
                    NativeStates[state.GetInstanceID()] = button;
                    own ??= state;
                }

                // A switch driving a state outside the copy would still drive the original's.
                foreach (var peckSwitch in clone.GetComponentsInChildren<PeckSwitch>(true))
                {
                    var target = peckSwitch.trackedStateSystem;
                    if (own != null && (target == null || !target.transform.IsChildOf(clone.transform)))
                        peckSwitch.trackedStateSystem = own;
                }

                clone.transform.SetParent(root.transform, false);
                clone.transform.localPosition = Vector3.zero;
                clone.transform.localRotation = Quaternion.identity;
                // The hidden plate's press effect threw on each press: removed before it wakes.
                if (button.Template == PushButton)
                {
                    foreach (var effect in clone.GetComponentsInChildren<PeckEffectMaterialProperty>(true))
                        UnityEngine.Object.DestroyImmediate(effect);
                }

                clone.SetActive(true);
                if (button.Template == PushButton)
                {
                    ReplacePlate(clone, PlateColour(button.Tint ?? button.Icon));

                }

                Plugin.Log.LogInfo(
                    $"[{nameof(WorldButtons)}] '{template.name}' copied as a native button, tickets {firstTicket}..{ticket - 1}.");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(WorldButtons)}] The native copy failed ({ex.Message}); using the mod's own button.");
                return false;
            }
            finally
            {
                UnityEngine.Object.Destroy(holder);
            }
        }

        // The hub's buttons are the plain ones; a tower's copy came out wrong.
        private static readonly Vector3 TemplateNear = new Vector3(-219.29f, 33.12f, -521.17f);
        private const float TemplateReach = 40f;

        internal const string PushButton = "BasicPushButton";
        internal const string LightSwitch = "BasicLightSwitch";

        private static readonly Dictionary<string, GameObject> Templates = new Dictionary<string, GameObject>();

        // Sets from the mod are not presses: they carry no player.
        internal static TrackedPeckState StateOf(int slot)
        {
            if (!Slots.TryGetValue(slot, out var button) || button.Root == null)
                return null;

            foreach (var state in button.Root.GetComponentsInChildren<TrackedPeckState>(true))
            {
                if (state != null && NativeStates.ContainsKey(state.GetInstanceID()))
                    return state;
            }

            return null;
        }

        // A disabled copy of the hub's original, kept for the session so it outlives that world.
        private static GameObject GetTemplate(string kind)
        {
            if (Templates.TryGetValue(kind, out var kept) && kept != null)
                return kept;

            var found = FindPushButton(TemplateNear, kind);
            if (found == null)
                return null;

            if ((found.transform.position - TemplateNear).magnitude > TemplateReach)
            {
                Plugin.Log.LogInfo(
                    $"[{nameof(WorldButtons)}] No hub push button loaded; '{found.name}' at {found.transform.position} used once.");
                return found;
            }

            var holder = new GameObject("AP template holder");
            holder.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(holder);
            var template = UnityEngine.Object.Instantiate(found, holder.transform);
            template.name = "AP template";
            ReceivedItemSpawner.OwnMaterials(template);
            Templates[kind] = template;
            Plugin.Log.LogInfo(
                $"[{nameof(WorldButtons)}] Template kept: '{found.name}' at {found.transform.position}.");
            return template;
        }

        private static GameObject FindPushButton(Vector3 near, string kind = PushButton)
        {
            GameObject best = null;
            var bestDistance = float.MaxValue;
            foreach (var peckSwitch in UnityEngine.Object.FindObjectsByType<PeckSwitch>(FindObjectsSortMode.None))
            {
                if (peckSwitch == null)
                    continue;

                GameObject root = null;
                var ours = false;
                for (var t = peckSwitch.transform; t != null; t = t.parent)
                {
                    if (t.name.StartsWith(kind, StringComparison.Ordinal))
                        root = t.gameObject;

                    // One of the mod's own buttons: copying it would copy registered tickets.
                    if (t.name.StartsWith("AP ", StringComparison.Ordinal))
                        ours = true;
                }

                if (ours || root == null || !root.activeInHierarchy)
                    continue;

                var distance = (root.transform.position - near).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = root;
                }
            }

            return best;
        }
    }
}
