using System;
using System.Collections.Generic;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    internal partial class WorldButtons
    {
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
    }
}
