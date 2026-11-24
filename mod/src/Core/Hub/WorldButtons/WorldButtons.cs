using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Core.Hub
{
    // Mod-made buttons: copies of the game's push button, given fresh tickets of their own.
    internal partial class WorldButtons : MonoBehaviour
    {
        // Constructor required by Il2CppInterop for any type injected into IL2CPP.
        public WorldButtons(IntPtr ptr) : base(ptr)
        {
        }

        private const float ReachMetres = 4f;

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
    }
}
