using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.Attributes;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Tiles
{
    // Makes peg tiles of any kind at run time; the asset id says kind and slot to guests.
    internal static class PegTileCloner
    {
        private const uint AssetBase = 0xB16_D000;
        internal const int Slots = 8;
        private const int TicketBase = 0xC400;
        private const int TicketsPerTile = 4;
        internal const string NameSuffix = "(AP tile)";

        private static List<PropGroup> _kinds;

        internal static List<PropGroup> Kinds
        {
            get
            {
                if (_kinds != null)
                    return _kinds;

                _kinds = new List<PropGroup>();
                foreach (PropGroup kind in Enum.GetValues(typeof(PropGroup)))
                {
                    var name = kind.ToString();
                    if (name.StartsWith("PegTile", StringComparison.Ordinal) && name != "PegTile" && !_kinds.Contains(kind))
                        _kinds.Add(kind);
                }

                _kinds.Sort((a, b) => ((int)a).CompareTo((int)b));
                return _kinds;
            }
        }

        // Speaker tiles get their own id range, shifted by the kinds' count.
        private static int Index(PropGroup kind, bool speaker) => Kinds.IndexOf(kind) + (speaker ? Kinds.Count : 0);

        internal static uint AssetIdFor(PropGroup kind, bool speaker, int slot) =>
            AssetBase + (uint)(Index(kind, speaker) * Slots + slot);

        internal static bool TryDecode(uint assetId, out PropGroup kind, out bool speaker, out int slot)
        {
            kind = default;
            speaker = false;
            slot = 0;
            if (assetId < AssetBase)
                return false;

            var offset = (int)(assetId - AssetBase);
            var index = offset / Slots;
            if (index >= Kinds.Count * 2)
                return false;

            speaker = index >= Kinds.Count;
            kind = Kinds[index % Kinds.Count];
            slot = offset % Slots;
            return true;
        }

        internal static IEnumerable<uint> AllAssetIds()
        {
            for (var index = 0; index < Kinds.Count * 2; index++)
            {
                for (var slot = 0; slot < Slots; slot++)
                    yield return AssetBase + (uint)(index * Slots + slot);
            }
        }

        private static int FirstTicket(PropGroup kind, bool speaker, int slot) =>
            TicketBase + (Index(kind, speaker) * Slots + slot) * TicketsPerTile;

        // On the host: a slot whose tickets nothing in the game holds.
        internal static bool SlotIsFree(PropGroup kind, bool speaker, int slot)
        {
            var office = LobbyNetworking.TicketOffice.instance;
            if (office == null)
                return true;

            var first = FirstTicket(kind, speaker, slot);
            for (var i = 0; i < TicketsPerTile; i++)
            {
                if (office.tickets.ContainsKey((ushort)(first + i)))
                    return false;
            }

            return true;
        }

        // One template per kind and sort: a copy keeps its source's look (speaker, glyph, pose).
        private static readonly Dictionary<(PropGroup, bool), Prop> Templates = new();

        private static bool IsSpeaker(Prop prop) => prop.name.Contains("Speaker", StringComparison.Ordinal);

        private static Prop Template(PropGroup kind, bool speaker)
        {
            if (Templates.TryGetValue((kind, speaker), out var kept) && kept != null)
                return kept;

            var family = Family(kind);
            Prop exact = null, sameFamily = null;
            foreach (var prop in UnityEngine.Object.FindObjectsByType<Prop>(FindObjectsSortMode.None))
            {
                if (prop == null || prop.propGroups == null || !prop.propGroups.Contains(PropGroup.PegTile)
                    || prop.name.Contains("(AP", StringComparison.Ordinal) || IsSpeaker(prop) != speaker)
                    continue;

                if (prop.propGroups.Contains(kind))
                {
                    exact = prop;
                    break;
                }

                if (sameFamily == null)
                {
                    foreach (var group in prop.propGroups)
                    {
                        if (group != PropGroup.PegTile && Family(group) == family)
                        {
                            sameFamily = prop;
                            break;
                        }
                    }
                }
            }

            var source = exact ?? sameFamily;
            if (source == null)
                return null;

            var blocks = ReceivedItemSpawner.CapturePropertyBlocks(source.gameObject);
            var wasActive = source.gameObject.activeSelf;
            GameObject copy;
            try
            {
                source.gameObject.SetActive(false);
                copy = UnityEngine.Object.Instantiate(source.gameObject);
            }
            finally
            {
                if (wasActive)
                    source.gameObject.SetActive(true);
            }

            copy.name = $"PegTileProp {(speaker ? "Speaker " : string.Empty)}{kind} (AP template)";
            copy.SetActive(false);
            ReceivedItemSpawner.OwnMaterials(copy);
            UnityEngine.Object.DontDestroyOnLoad(copy);
            ReceivedItemSpawner.ApplyPropertyBlocks(copy, blocks);
            var identity = copy.GetComponent<NetworkIdentity>();
            if (identity != null)
                identity.sceneId = 0;

            var template = copy.GetComponent<Prop>();
            Templates[(kind, speaker)] = template;
            Plugin.Log.LogInfo(
                $"[{nameof(PegTileCloner)}] Template for {kind} kept from '{source.name}'{(exact == null ? " (same family)" : string.Empty)}.");
            return template;
        }

        // "PegTileD2" -> "PegTileD", "PegTilePriestA6" -> "PegTilePriestA".
        internal static string Family(PropGroup kind) => kind.ToString().TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');

        // Same on host and guest.
        internal static Prop Build(PropGroup kind, bool speaker, int slot, Vector3 position)
        {
            var template = Template(kind, speaker);
            if (template == null)
            {
                Plugin.Log.LogInfo($"[{nameof(PegTileCloner)}] No peg tile loaded to copy.");
                return null;
            }

            var blocks = ReceivedItemSpawner.CapturePropertyBlocks(template.gameObject);
            var clone = UnityEngine.Object.Instantiate(template.gameObject, position, Quaternion.identity);
            clone.name = $"PegTileProp {(speaker ? "Speaker " : string.Empty)}{kind} {NameSuffix}";
            ReceivedItemSpawner.ApplyPropertyBlocks(clone, blocks);

            foreach (var saved in clone.GetComponentsInChildren<SaveIdentity>(true))
                UnityEngine.Object.DestroyImmediate(saved);

            var ticket = FirstTicket(kind, speaker, slot);
            foreach (var peckSwitch in clone.GetComponentsInChildren<PeckSwitch>(true))
            {
                peckSwitch.useTicket = true;
                peckSwitch.ticket = (ushort)ticket;
                peckSwitch.shellReference = new SeaShell.ShellReference((ushort)ticket);
                ticket++;
            }

            foreach (var state in clone.GetComponentsInChildren<TrackedPeckState>(true))
            {
                state.saveIdentity = null;
                state.savableSystem = SavableSystem.NotSavable;
                state.ticket = (ushort)ticket;
                state.shellReference = new SeaShell.ShellReference((ushort)ticket);
                ticket++;
            }

            var identity = clone.GetComponent<NetworkIdentity>();
            if (identity != null)
            {
                identity.sceneId = 0;
                ReceivedItemSpawner.GetPrivatePropertySetter<NetworkIdentity>("hasSpawned")
                    ?.Invoke(identity, new object[] { false });
            }

            var prop = clone.GetComponent<Prop>();
            if (prop == null)
            {
                UnityEngine.Object.Destroy(clone);
                return null;
            }

            for (var i = prop.propGroups.Count - 1; i >= 0; i--)
            {
                var group = prop.propGroups[i];
                if (group != PropGroup.PegTile && group.ToString().StartsWith("PegTile", StringComparison.Ordinal))
                    prop.propGroups.RemoveAt(i);
            }

            prop.propGroups.Add(kind);
            ReceivedItemSpawner.NeutralizeProgression(prop);

            clone.SetActive(true);
            if (identity != null)
                ReceivedItemSpawner.GetPrivatePropertySetter<NetworkIdentity>(nameof(NetworkIdentity.SpawnedFromInstantiate))
                    ?.Invoke(identity, new object[] { false });

            foreach (var renderer in clone.GetComponentsInChildren<PegTileRenderer>(true))
                renderer.SetAndRefresh(kind);

            foreach (var connector in clone.GetComponentsInChildren<PegTileMusicConnector>(true))
                connector.SetPegTile(kind);

            ReceivedItemSpawner.ClearLightmapReferences(clone);
            ReceivedItemSpawner.FixMaterialessRenderers(clone);
            ReceivedItemSpawner.RefreshPropertyBlockHelpers(clone);
            LogLook(clone, kind, speaker);
            Redraws.Add((prop, kind, Time.time + 0.05f, Time.time + 1f));
            return prop;
        }

        // Redrawn a frame and a second later: some came out blank or magenta when made in bulk.
        private static readonly List<(Prop prop, PropGroup kind, float first, float last)> Redraws = new();

        internal static void RedrawPending()
        {
            for (var i = Redraws.Count - 1; i >= 0; i--)
            {
                var (prop, kind, first, last) = Redraws[i];
                if (prop == null)
                {
                    Redraws.RemoveAt(i);
                    continue;
                }

                if (Time.time < first)
                    continue;

                foreach (var renderer in prop.GetComponentsInChildren<PegTileRenderer>(true))
                    renderer.SetAndRefresh(kind);

                if (Time.time >= last)
                {
                    LogLook(prop.gameObject, kind, false);
                    Redraws.RemoveAt(i);
                }
                else
                {
                    Redraws[i] = (prop, kind, last, last);
                }
            }
        }

        // A magenta part shows here as no material or an error shader.
        private static void LogLook(GameObject clone, PropGroup kind, bool speaker)
        {
            if (!ModConfig.DebugModeEnabled.Value)
                return;

            var parts = new List<string>();
            foreach (var renderer in clone.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || !renderer.enabled)
                    continue;

                var materials = new List<string>();
                foreach (var material in renderer.sharedMaterials)
                    materials.Add(material != null ? $"{material.name}/{(material.shader != null ? material.shader.name : "-")}" : "NONE");
                parts.Add($"{renderer.name}:[{string.Join(" | ", materials)}]");
            }

            Plugin.Log.LogInfo($"[{nameof(PegTileCloner)}] {(speaker ? "speaker " : string.Empty)}{kind} drawn with {string.Join(", ", parts)}");
        }

        // On the host: builds and spawns for everyone. Null when nothing could be made.
        [HideFromIl2Cpp]
        internal static Prop Spawn(PropGroup kind, bool speaker, int slot, Vector3 position)
        {
            if (!NetworkServer.active)
                return null;

            var prop = Build(kind, speaker, slot, position);
            if (prop == null)
                return null;

            NetworkServer.Spawn(prop.gameObject, AssetIdFor(kind, speaker, slot));
            prop.SetLoose();
            return prop;
        }
    }

    // On every client, the host's own included: builds the peg tiles the host spawns.
    internal class PegTileSpawnHandler : MonoBehaviour
    {
        public PegTileSpawnHandler(IntPtr ptr) : base(ptr)
        {
        }

        private float _nextCheck;

        private void Update()
        {
            PegTileCloner.RedrawPending();

            if (!NetworkClient.active || Time.time < _nextCheck)
                return;

            _nextCheck = Time.time + 1f;
            try
            {
                var handlers = NetworkClient.spawnHandlers;
                var first = PegTileCloner.AssetIdFor(PegTileCloner.Kinds[0], false, 0);
                if (handlers != null && handlers.ContainsKey(first))
                    return;

                var spawn = (SpawnDelegate)Build;
                var unspawn = (UnSpawnDelegate)Unbuild;
                var count = 0;
                foreach (var assetId in PegTileCloner.AllAssetIds())
                {
                    NetworkClient.RegisterSpawnHandler(assetId, spawn, unspawn);
                    count++;
                }

                Plugin.Log.LogInfo($"[{nameof(PegTileSpawnHandler)}] Peg tile spawn handlers registered ({count}).");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(PegTileSpawnHandler)}] Could not register: {ex.Message}");
                _nextCheck = Time.time + 30f;
            }
        }

        private static GameObject Build(Vector3 position, uint assetId)
        {
            if (!PegTileCloner.TryDecode(assetId, out var kind, out var speaker, out var slot))
                return null;

            var prop = PegTileCloner.Build(kind, speaker, slot, position);
            Plugin.Log.LogInfo(
                $"[{nameof(PegTileSpawnHandler)}] {(speaker ? "speaker " : string.Empty)}{kind} #{slot} {(prop != null ? "built" : "could not be built")}.");
            return prop != null ? prop.gameObject : null;
        }

        private static void Unbuild(GameObject tile)
        {
            if (tile != null)
                Destroy(tile);
        }
    }
}
