using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Mod-made buttons: copies of the game's push button, given fresh tickets of their own.
    internal class WorldButtons : MonoBehaviour
    {
        // Constructor required by Il2CppInterop for any type injected into IL2CPP.
        public WorldButtons(IntPtr ptr) : base(ptr)
        {
        }

        private const float ReachMetres = 4f;
        private const float CheckEverySeconds = 0f;

        internal sealed class Button
        {
            internal int Slot;

            // The tower whose colour the plate takes, when it is not the icon's.
            internal string Tint;

            // A guest's press runs on the host: the effect belongs to the host's world.
            internal bool HostSide;

            // A push button, or a light switch, which stays on or off until pressed again.
            internal string Template = PushButton;
            internal int LastState;
            internal string Label;
            internal Vector3 Position;
            internal Action<PlayerCharacter> OnPress;
            internal GameObject Root;

            // Measured from the renderers: the game-button copy and the fallback differ in size.
            internal Vector3 Centre;
            internal float Radius = 0.55f;
            internal Bounds Bounds;

            // False for the fallback, which the mod aims and clicks itself.
            internal bool Native;

            internal string Icon;
        }

        // A ticket block clear of the game's own, with a fixed stride so every machine agrees.
        private const ushort FirstTicket = 0xE000;
        private const int TicketsPerButton = 8;

        private static readonly Dictionary<int, Button> NativeStates = new Dictionary<int, Button>();

        internal static bool HasNative => NativeStates.Count > 0;

        private static readonly List<Button> Buttons = new List<Button>();

        // By slot, so a button's tickets are the same on every machine whatever the creation order.
        private static readonly Dictionary<int, Button> Slots = new Dictionary<int, Button>();

        // False for a button whose world went away with it, so it gets made again.
        internal static bool Has(int slot) => Slots.TryGetValue(slot, out var button) && button.Root != null;

        internal static void Remove(int slot)
        {
            if (!Slots.TryGetValue(slot, out var button))
                return;

            Slots.Remove(slot);
            Buttons.Remove(button);
            if (button.Root != null)
                UnityEngine.Object.Destroy(button.Root);

            var gone = new List<int>();
            foreach (var pair in NativeStates)
            {
                if (pair.Value == button)
                    gone.Add(pair.Key);
            }

            foreach (var id in gone)
                NativeStates.Remove(id);
        }

        // The world went away with its objects: forget the buttons without touching them.
        internal static void Forget()
        {
            Slots.Clear();
            Buttons.Clear();
            NativeStates.Clear();
            _focused = null;
        }
        private static Material _material;
        private static Material _plateMaterial;
        private static Button _focused;

        // A world reload destroys the button, and the caller adds it again.
        internal static Button Add(int slot, string label, Vector3 position, Quaternion rotation,
            Action<PlayerCharacter> onPress, string icon = null, string tint = null, bool hostSide = false,
            string template = PushButton)
        {
            if (Slots.ContainsKey(slot))
                Remove(slot);

            var button = new Button { Slot = slot, Label = label, Position = position, OnPress = onPress, Icon = icon, Tint = tint, HostSide = hostSide, Template = template };
            button.Root = Build(position, rotation, slot, button);
            Slots[slot] = button;
            Measure(button);
            if (icon != null)
                AddIcon(button, icon);
            Buttons.Add(button);
            Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] '{label}' placed at {position}.");
            return button;
        }

        private const float IconGap = 0.06f;
        private const float IconScale = 0.6f;
        private const float IconSink = 0.10f;

        // A picture rebuilt from the Black Tower's EndingGateIndicator meshes, no components.
        private static void AddIcon(Button button, string tower)
        {
            try
            {
                var container = FindIndicatorContainer();
                if (container == null)
                {
                    Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] No tower-icon panel is loaded to copy; '{button.Label}' has no icon.");
                    return;
                }

                var icon = new GameObject("AP world button icon");
                icon.transform.SetParent(button.Root.transform, false);
                icon.transform.localPosition = Vector3.zero;
                icon.transform.localRotation = Quaternion.identity;
                icon.transform.localScale = Vector3.one * IconScale;

                var made = 0;
                // All on the light bezel, where black is not lost.
                var sphere = tower is "gauntlet" or "drawbridge" or "dome";
                var beige = tower == "hub";
                var silhouette = beige ? "white" : tower;
                var bezel = "EndingGateIndicator_base_white";
                var names = sphere
                    ? new[] { bezel, "EndingGateIndicator_light" }
                    : new[] { bezel, "EndingGateIndicator_light", "EndingGateIndicator_tower_" + silhouette };
                Renderer bezelCopy = null;
                foreach (var name in names)
                {
                    var source = container.Find(name);
                    var filter = source != null ? source.GetComponent<MeshFilter>() : null;
                    var renderer = source != null ? source.GetComponent<MeshRenderer>() : null;
                    if (filter == null || filter.sharedMesh == null || renderer == null)
                        continue;

                    var part = new GameObject(name);
                    part.transform.SetParent(icon.transform, false);
                    part.transform.localPosition = container.InverseTransformPoint(source.position);
                    part.transform.localRotation = Quaternion.Inverse(container.rotation) * source.rotation;
                    part.transform.localScale = source.localScale;
                    part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                    var copy = part.AddComponent<MeshRenderer>();
                    copy.sharedMaterials = renderer.sharedMaterials;

                    // The game sets the silhouettes' colour at run time, so each copy gets its own.
                    if (name == bezel)
                        bezelCopy = copy;

                    if (!beige && tower != "black" && name.EndsWith("_tower_" + silhouette, StringComparison.Ordinal))
                    {
                        var block = new MaterialPropertyBlock();
                        copy.GetPropertyBlock(block);
                        var colour = IconColour(tower);
                        block.SetColor("_TintColor", colour);
                        block.SetColor("_Color", colour);
                        block.SetColor("_BaseColor", colour);
                        copy.SetPropertyBlock(block);
                    }

                    made++;
                }

                if (tower == "drawbridge" && bezelCopy != null)
                    AddDrawbridge(icon, bezelCopy.bounds, button.Root.transform.forward);
                else if (sphere && bezelCopy != null)
                    AddSphere(icon, bezelCopy.bounds, button.Root.transform.forward, PlateColour(tower));

                // Lifted by its measured size: the icon is bigger than the button.
                var drawn = false;
                var iconBounds = new Bounds(icon.transform.position, Vector3.zero);
                foreach (var renderer in icon.GetComponentsInChildren<Renderer>(true))
                {
                    if (!drawn)
                        iconBounds = renderer.bounds;
                    else
                        iconBounds.Encapsulate(renderer.bounds);
                    drawn = true;
                }

                if (drawn)
                    icon.transform.position += Vector3.up * (button.Bounds.max.y - iconBounds.min.y + IconGap);

                // Into the wall: the root faces out of it, so back is along its forward.
                icon.transform.position -= button.Root.transform.forward * IconSink;

                Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] Icon '{tower}' for '{button.Label}': {made} of 3 part(s) copied.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(WorldButtons)}] No icon for '{button.Label}': {ex.Message}");
            }
        }

        // The panel has no drawbridge: its red bridge on two piers, in cubes.
        private static void AddDrawbridge(GameObject icon, Bounds bezel, Vector3 outward)
        {
            var width = Mathf.Max(bezel.size.x, bezel.size.z);
            var toIcon = 1f / Mathf.Max(0.0001f, icon.transform.lossyScale.x);
            var upright = Quaternion.LookRotation(outward, Vector3.up);
            var middle = bezel.center + outward * (width * 0.16f);
            var red = new Color(0.72f, 0.14f, 0.12f);
            var stone = new Color(0.38f, 0.31f, 0.20f);

            void Block(float x, float y, float sizeX, float sizeY, Color colour)
            {
                var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                block.name = "drawbridge part";
                block.transform.SetParent(icon.transform, false);
                block.transform.localScale = new Vector3(sizeX, sizeY, 0.20f) * (width * toIcon);
                var at = middle + upright * new Vector3(x, y, 0f) * width;
                block.transform.SetPositionAndRotation(at, upright);
                Paint(block, colour);
            }

            Block(-0.12f, -0.15f, 0.09f, 0.20f, stone);
            Block(0.12f, -0.15f, 0.09f, 0.20f, stone);
            Block(0f, -0.03f, 0.50f, 0.05f, red);
            Block(0f, 0.06f, 0.50f, 0.02f, red);
            Block(-0.24f, 0.015f, 0.02f, 0.09f, red);
            Block(0f, 0.015f, 0.02f, 0.09f, red);
            Block(0.24f, 0.015f, 0.02f, 0.09f, red);
        }

        private static void AddSphere(GameObject icon, Bounds bezel, Vector3 outward, Color colour)
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "gauntlet sphere";
            sphere.transform.SetParent(icon.transform, false);
            var width = Mathf.Max(bezel.size.x, bezel.size.z);
            sphere.transform.localScale = Vector3.one * (width * 0.42f / Mathf.Max(0.0001f, icon.transform.lossyScale.x));
            // A little below the middle: centred, it ran into the light at the top.
            sphere.transform.position = bezel.center + outward * (width * 0.04f) + Vector3.down * (width * 0.08f);
            Paint(sphere, colour);
        }

        private static Color IconColour(string tower)
        {
            switch (tower)
            {
                case "red": return new Color(0.80f, 0.22f, 0.18f);
                case "green": return new Color(0.14f, 0.68f, 0.34f);
                case "blue": return new Color(0.16f, 0.50f, 0.88f);
                case "yellow": return new Color(0.88f, 0.78f, 0.16f);
                case "black": return new Color(0.74f, 0.64f, 0.44f);
                default: return new Color(0.92f, 0.92f, 0.92f);
            }
        }

        private static Transform FindIndicatorContainer()
        {
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer != null && renderer.gameObject.name == "EndingGateIndicator_base" && renderer.transform.parent != null
                    && renderer.transform.parent.Find("EndingGateIndicator_light") != null)
                    return renderer.transform.parent;
            }

            return null;
        }

        // Everything drawn but the post, which is not the button.
        private static void Measure(Button button)
        {
            var any = false;
            var bounds = new Bounds(button.Position + Vector3.up * 1f, Vector3.zero);
            foreach (var renderer in button.Root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer.gameObject.name == "post")
                    continue;

                if (!any)
                    bounds = renderer.bounds;
                else
                    bounds.Encapsulate(renderer.bounds);
                any = true;
            }

            button.Bounds = bounds;
            button.Centre = any ? bounds.center : button.Position + Vector3.up * 1f;
            // Some slack around the drawn box, so a button is easy to aim at.
            button.Radius = Mathf.Max(0.45f, bounds.extents.magnitude + 0.2f);
        }

        internal static void Clear()
        {
            foreach (var button in Buttons)
            {
                if (button.Root != null)
                    UnityEngine.Object.Destroy(button.Root);
            }

            Buttons.Clear();
            Slots.Clear();
            NativeStates.Clear();
            _focused = null;
        }

        // The delay lets the button's click be heard before the player is sent away.
        private const float TeleportDelay = 0.4f;

        private static PlayerCharacter _pendingPlayer;
        private static Vector3 _pendingDestination;
        private static Quaternion _pendingRotation;
        private static float _pendingAt = -1f;

        internal static void TeleportSoon(PlayerCharacter player, Vector3 destination, Quaternion rotation)
        {
            _pendingPlayer = player;
            _pendingDestination = destination;
            _pendingRotation = rotation;
            _pendingAt = Time.unscaledTime + TeleportDelay;
        }

        private static void RunPendingTeleport()
        {
            if (_pendingAt < 0f || Time.unscaledTime < _pendingAt)
                return;

            _pendingAt = -1f;
            TeleportTo(_pendingPlayer, _pendingDestination, _pendingRotation);
            _pendingPlayer = null;
        }

        internal static void TeleportTo(PlayerCharacter player, Vector3 destination, Quaternion rotation)
        {
            if (player == null || player.grease == null)
            {
                Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] No local player to move.");
                return;
            }

            var marker = new GameObject("World button destination");
            try
            {
                marker.transform.SetPositionAndRotation(destination, rotation);
                player.grease.Teleport(marker.transform);
                Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] Teleported to {destination}.");
            }
            finally
            {
                UnityEngine.Object.Destroy(marker);
            }
        }

        private void Update()
        {
            RunPendingTeleport();
            _focused = null;
            if (Buttons.Count == 0 || !WorldManager.isReadyForEffects)
                return;

            var player = LocalPlayer.Find();
            var camera = Camera.main;
            if (player == null || camera == null)
                return;

            // The nearest button along the view ray, within a hand's width and arm's reach.
            var origin = camera.transform.position;
            var direction = camera.transform.forward;
            var bestAlong = float.MaxValue;
            foreach (var button in Buttons)
            {
                var toCentre = button.Centre - origin;
                var along = Vector3.Dot(toCentre, direction);
                if (along < 0.3f || along > ReachMetres)
                    continue;

                var miss = (toCentre - direction * along).magnitude;
                if (miss > button.Radius || along >= bestAlong)
                    continue;

                bestAlong = along;
                _focused = button;
            }

            if (_focused != null && !_focused.Native && Pressed())
            {
                try
                {
                    _focused.OnPress?.Invoke(player);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(WorldButtons)}] '{_focused.Label}' failed: {ex.Message}");
                }
            }
        }

        // The game's own use input, whatever the player bound it to.
        private static bool Pressed()
        {
            return Rewired.ReInput.isReady
                   && Rewired.ReInput.players.GetPlayer(0).GetButtonDown(RewiredConsts.Action.use);
        }

        // Only the game button's meshes and materials: no network reference or press logic.
        private static GameObject Build(Vector3 position, Quaternion rotation, int index, Button button)
        {
            var root = new GameObject("AP world button");
            root.transform.SetPositionAndRotation(position, rotation);

            if (TryBuildNative(root, index, button))
            {
                button.Native = true;
                return root;
            }

            if (TryBuildFromGameButton(root))
            {
                AddPost(root);
                return root;
            }

            return BuildFromPrimitives(root);
        }

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

        // The colour of the tower the button stands for; green without a picture.
        private static Color PlateColour(string icon)
        {
            switch (icon)
            {
                case "red": return new Color(0.80f, 0.22f, 0.18f);
                case "green": return new Color(0.14f, 0.68f, 0.34f);
                case "blue": return new Color(0.16f, 0.50f, 0.88f);
                case "yellow": return new Color(0.88f, 0.78f, 0.16f);
                case "black": return new Color(0.16f, 0.16f, 0.19f);
                case "hub": return new Color(0.74f, 0.64f, 0.44f);
                case "gauntlet": return new Color(0.04f, 0.04f, 0.05f);
                case "drawbridge": return new Color(0.42f, 0.27f, 0.14f);
                case "dome": return new Color(0.36f, 0.72f, 0.20f);
                default: return new Color(0.23f, 0.81f, 0.39f);
            }
        }

        // The game's plate is yellow in the mesh, so a plain slab of the same size replaces it.
        private static void ReplacePlate(GameObject target, Color colour)
        {
            Renderer plate = null;
            var biggest = 0f;
            foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                var size = renderer.bounds.size.sqrMagnitude;
                if (size > biggest)
                {
                    biggest = size;
                    plate = renderer;
                }
            }

            var filter = plate != null ? plate.GetComponent<MeshFilter>() : null;
            if (filter == null || filter.sharedMesh == null)
                return;

            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "AP button plate";
            slab.transform.SetParent(target.transform, false);
            slab.transform.rotation = plate.transform.rotation;
            slab.transform.position = plate.transform.TransformPoint(filter.sharedMesh.bounds.center);
            slab.transform.localScale = Vector3.Scale(filter.sharedMesh.bounds.size, plate.transform.lossyScale);
            // A copy: the game animates the plate material on press, lighting all that share it.
            if (plate.sharedMaterial != null && _plateMaterial == null)
                _plateMaterial = new Material(plate.sharedMaterial);
            if (_plateMaterial != null)
                _material = _plateMaterial;

            Paint(slab, colour);
            plate.enabled = false;
            Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] Plate '{plate.gameObject.name}' replaced by a {colour} slab.");
        }

        // The slots a table can have, so two slots never share a block of tickets.
        private const int MaxSlots = 40;

        // Skips blocks a scene object already holds; the scene is the same on every machine.
        private static int FreeTicketBlock(int slot)
        {
            var office = LobbyNetworking.TicketOffice.instance;
            for (var round = 0; round < 64; round++)
            {
                var first = FirstTicket + (slot + round * MaxSlots) * TicketsPerButton;
                if (first + TicketsPerButton > ushort.MaxValue)
                    break;

                var free = true;
                for (var i = 0; i < TicketsPerButton && free; i++)
                {
                    var ticket = (ushort)(first + i);
                    free = (office == null || !office.tickets.ContainsKey(ticket)) && !SceneTickets().Contains(ticket);
                }

                if (free)
                {
                    if (round > 0)
                        Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] Slot {slot}: tickets taken in the game, using block {round}.");
                    return first;
                }
            }

            return FirstTicket + slot * TicketsPerButton;
        }

        private static HashSet<ushort> _sceneTickets;
        private static float _sceneTicketsAt = -1f;

        // Disabled objects only register in OnEnable, so a hidden button's ticket looked free.
        internal static HashSet<ushort> SceneTickets()
        {
            if (_sceneTickets != null && Time.unscaledTime - _sceneTicketsAt < 5f)
                return _sceneTickets;

            var taken = new HashSet<ushort>();
            Collect<PeckSwitch>(taken, item => item.ticket);
            Collect<TrackedPeckState>(taken, item => item.ticket);
            Collect<PropHome>(taken, item => item.ticket);
            Collect<PlayerPose>(taken, item => item.ticket);
            Collect<StickyPlatform>(taken, item => item.ticket);
            _sceneTickets = taken;
            _sceneTicketsAt = Time.unscaledTime;
            return taken;
        }

        private static void Collect<T>(HashSet<ushort> taken, Func<T, ushort> ticketOf) where T : Component
        {
            foreach (var item in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // The mod's buttons hang under "AP ..." roots: a slot keeps its own block.
                if (item == null || item.transform.root.name.StartsWith("AP ", StringComparison.Ordinal))
                    continue;
                // Scene objects only, alike on every machine: run-time spawns may not be there yet.
                var identity = item.GetComponentInParent<Mirror.NetworkIdentity>(true);
                if (identity != null && identity.sceneId == 0)
                    continue;
                var ticket = ticketOf(item);
                if (ticket != 0)
                    taken.Add(ticket);
            }
        }

        // Runs a native button's effect when this machine's own player pressed it.
        internal static void OnState(TrackedPeckState state, PeckContext context)
        {
            if (!NativeStates.TryGetValue(state.GetInstanceID(), out var button))
                return;

            // A push button acts when it goes down; a light switch on every flip.
            var flipped = context.state != button.LastState;
            button.LastState = context.state;
            if (button.Template == LightSwitch ? !flipped : context.state < 1)
                return;

            var who = context.playerIdentity;
            Plugin.Log.LogInfo(
                $"[{nameof(WorldButtons)}] '{button.Label}' pressed (state {context.state}) by "
                + $"'{(who != null ? who.name : "<nobody>")}', local={(who != null && who.isLocalPlayer)}.");
            // A teleport from a guest's press is sent back to the guest's own machine.
            if (who != null && !who.isLocalPlayer && Mirror.NetworkServer.active && button.HostSide)
            {
                RunPress(button, who.GetComponent<PlayerCharacter>());
                return;
            }

            if (who != null && !who.isLocalPlayer && Mirror.NetworkServer.active)
            {
                Net.ModChannel.SendPress(who.connectionToClient, button.Slot);
                return;
            }

            if (who == null || !who.isLocalPlayer)
                return;

            RunPress(button);
        }

        // On a guest: the host says its player pressed the button in this slot.
        internal static void RunPress(int slot)
        {
            if (Slots.TryGetValue(slot, out var button))
                RunPress(button);
        }

        private static void RunPress(Button button, PlayerCharacter presser = null)
        {
            try
            {
                button.OnPress?.Invoke(presser != null ? presser : LocalPlayer.Find());
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(WorldButtons)}] '{button.Label}' failed: {ex.Message}");
            }
        }

        private const float PostHeight = 1.0f;

        private static void AddPost(GameObject root)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "post";
            post.transform.SetParent(root.transform, false);
            post.transform.localPosition = new Vector3(0f, PostHeight * 0.5f - 0.1f, 0f);
            post.transform.localScale = new Vector3(0.14f, PostHeight * 0.5f - 0.05f, 0.14f);
            Paint(post, new Color(0.12f, 0.2f, 0.17f));
        }

        private static bool TryBuildFromGameButton(GameObject root)
        {
            var template = FindPushButton(root.transform.position);
            if (template == null)
                return false;

            var made = 0;
            foreach (var source in template.GetComponentsInChildren<MeshRenderer>(true))
            {
                var filter = source != null ? source.GetComponent<MeshFilter>() : null;
                if (filter == null || filter.sharedMesh == null)
                    continue;

                var part = new GameObject(source.gameObject.name);
                part.transform.SetParent(root.transform, false);
                part.transform.localPosition = template.transform.InverseTransformPoint(source.transform.position)
                                               + Vector3.up * PostHeight;
                part.transform.localRotation = Quaternion.Inverse(template.transform.rotation) * source.transform.rotation;
                part.transform.localScale = source.transform.lossyScale;

                part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var renderer = part.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = source.sharedMaterials;

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_TintColor", new Color(0.23f, 0.81f, 0.39f));
                block.SetColor("_Color", new Color(0.23f, 0.81f, 0.39f));
                block.SetColor("_BaseColor", new Color(0.23f, 0.81f, 0.39f));
                renderer.SetPropertyBlock(block);
                made++;
            }

            return made > 0;
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
            if (ModConfig.DebugModeEnabled.Value)
                Describe(found);
            return template;
        }

        // Logs a prop's objects, components and switch targets, to learn it before copying it.
        private static void Describe(GameObject prop)
        {
            try
            {
                var parent = prop.transform.parent;
                Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] '{prop.name}' under '{(parent != null ? parent.name : "-")}':");
                if (parent != null)
                {
                    foreach (var component in parent.GetComponents<Component>())
                        Plugin.Log.LogInfo($"[{nameof(WorldButtons)}]   parent has {component.GetIl2CppType().Name}");
                }

                foreach (var t in prop.GetComponentsInChildren<Transform>(true))
                {
                    var names = new List<string>();
                    foreach (var component in t.GetComponents<Component>())
                        names.Add(component.GetIl2CppType().Name);
                    Plugin.Log.LogInfo($"[{nameof(WorldButtons)}]   {t.name}: {string.Join(", ", names)}");

                    var peckSwitch = t.GetComponent<PeckSwitch>();
                    if (peckSwitch != null)
                    {
                        var target = peckSwitch.trackedStateSystem;
                        Plugin.Log.LogInfo(
                            $"[{nameof(WorldButtons)}]     switch -> '{(target != null ? target.name + " under " + (target.transform.parent != null ? target.transform.parent.name : "-") : "<none>")}', "
                            + $"mode {peckSwitch.stateMode}, specific {peckSwitch.specificState}, wrap {peckSwitch.wrapTotal}, up {(peckSwitch.upSwitch != null ? peckSwitch.upSwitch.name : "-")}");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(WorldButtons)}] Could not describe '{prop.name}': {ex.Message}");
            }
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

        // No colliders, so nothing catches on them.
        private static GameObject BuildFromPrimitives(GameObject root)
        {

            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "post";
            post.transform.SetParent(root.transform, false);
            post.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            post.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);

            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = "cap";
            cap.transform.SetParent(root.transform, false);
            cap.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            cap.transform.localScale = new Vector3(0.28f, 0.06f, 0.28f);

            Paint(post, new Color(0.16f, 0.28f, 0.24f));
            Paint(cap, new Color(0.23f, 0.81f, 0.39f));
            return root;
        }

        // Made anew, a button flashes and loses the press it is in the middle of.
        internal static void Recolour(int slot, string tint)
        {
            if (!Slots.TryGetValue(slot, out var button) || button.Root == null)
                return;

            button.Tint = tint;
            foreach (var renderer in button.Root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null && renderer.gameObject.name == "AP button plate")
                    Paint(renderer.gameObject, PlateColour(tint));
            }
        }

        internal static GameObject MakeSlab(string name, Vector3 position, Quaternion rotation, Vector3 size, Color colour)
        {
            var slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = name;
            slab.transform.SetPositionAndRotation(position, rotation);
            slab.transform.localScale = size;
            Paint(slab, colour);
            return slab;
        }

        private static void Paint(GameObject part, Color colour)
        {
            var collider = part.GetComponent<Collider>();
            if (collider != null)
                UnityEngine.Object.Destroy(collider);

            var renderer = part.GetComponent<Renderer>();
            if (renderer == null)
                return;

            var material = FindGameMaterial();
            if (material != null)
                renderer.sharedMaterial = material;

            // The vertex-colour shader multiplies this over the mesh colour; a primitive has none.
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_TintColor", colour);
            block.SetColor("_Color", colour);
            block.SetColor("_BaseColor", colour);
            renderer.SetPropertyBlock(block);
        }

        // The engine's default material may be stripped from the build and render pink.
        private static Material FindGameMaterial()
        {
            if (_material != null)
                return _material;

            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var material = renderer != null ? renderer.sharedMaterial : null;
                if (material != null && material.shader != null && material.shader.name.Contains("VertexColors"))
                {
                    _material = material;
                    Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] Borrowing material '{material.name}' ({material.shader.name}).");
                    return _material;
                }
            }

            Plugin.Log.LogInfo($"[{nameof(WorldButtons)}] No game material found; the engine's default will be used.");
            return null;
        }
    }
}
