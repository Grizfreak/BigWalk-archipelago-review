using System;
using System.Collections.Generic;
using System.Reflection;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Items
{
    internal static partial class ReceivedItemSpawner
    {
        // Pending hand-overs: items may go to different players in the same tick.
        private static readonly List<PendingHandover> Pending = new();

        private sealed class PendingHandover
        {
            internal Prop Prop;
            internal PlayerCharacter Recipient;
            internal float NotBefore;
            internal bool DropAsked;
        }

        // The drop has to reach the player's machine first, or the new hold lands in full hands.
        private const float DropSettleSeconds = 0.5f;

        internal static void QueueHandover(Prop prop, PlayerCharacter recipient)
        {
            if (prop == null || recipient == null)
                return;

            if (!ModConfig.PutGourdInHands.Value)
            {
                ItemRecipients.Settled(recipient);
                return;
            }

            Pending.Add(new PendingHandover { Prop = prop, Recipient = recipient, NotBefore = Time.time });
        }

        // Host only, at least one frame after the spawn.
        internal static void DrainPendingHandover()
        {
            using var activity = ModActivity.Enter("handover");
            for (var i = Pending.Count - 1; i >= 0; i--)
            {
                var entry = Pending[i];
                if (Time.time < entry.NotBefore)
                    continue;

                if (entry.Prop == null || entry.Recipient == null || entry.Recipient.hands == null)
                {
                    ItemRecipients.Settled(entry.Recipient);
                    Pending.RemoveAt(i);
                    continue;
                }

                // All hands were full: this player lets go first, once, and gets it after.
                var held = entry.Recipient.hands.heldProp;
                if (held != null && held != entry.Prop && !entry.DropAsked)
                {
                    ReleaseFromHands(held);
                    entry.DropAsked = true;
                    entry.NotBefore = Time.time + DropSettleSeconds;
                    Plugin.Log.LogInfo(
                        $"[{nameof(ReceivedItemSpawner)}] {entry.Recipient.gameObject.name} had full hands; "
                        + "they drop what they held and take the new item next.");
                    continue;
                }

                Pending.RemoveAt(i);
                ItemRecipients.Settled(entry.Recipient);

                var identity = entry.Prop.GetComponent<NetworkIdentity>();
                if (TryPutInHands(entry.Prop, entry.Recipient))
                    Plugin.Log.LogInfo(
                        $"[{nameof(ReceivedItemSpawner)}] Item handed to {entry.Recipient.gameObject.name} "
                        + $"(netId {(identity != null ? identity.netId : 0)}).");
            }
        }

        internal static void ForgetPendingHandovers()
        {
            Pending.Clear();
            ItemRecipients.Forget();
        }

        // Server side only: the hold is a server SyncVar, and a client PickUp only fools itself.
        private static bool TryPutInHands(Prop prop, PlayerCharacter recipient)
        {
            if (prop == null || recipient == null || !NetworkServer.active)
                return false;

            try
            {
                return TryGiveTo(recipient, prop);
            }
            catch (Exception ex)
            {
                // Cosmetic: the item exists and can be picked up, so failing costs nothing.
                Plugin.Log.LogWarning($"[{nameof(ReceivedItemSpawner)}] Could not hand the item over: {ex.Message}");
                return false;
            }
        }

        // PickUp only acts locally; the hold must also be published on the network.
        private static bool TryGiveTo(PlayerCharacter player, Prop prop)
        {
            var hands = player != null ? player.hands : null;
            if (hands == null || hands.isHoldingSomething || !hands.IsSafeToPickUp(prop))
                return false;

            // Local effects, on this machine.
            hands.PickUp(prop);

            // The game's CmdPickUp sets NetworkplayerHeldInformation on the server, so does this.
            try
            {
                // Numbered past the player's last action: OnSetHeld drops a value below its latest.
                var networking = player.playerNetworking;
                var hold = new PlayerHeldInformation(prop);
                hold.actionNumber = networking.playerHeldInformation.actionNumber + 1;
                networking.NetworkplayerHeldInformation = hold;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ReceivedItemSpawner)}] Could not publish the hold to the other players: {ex.Message}");
            }

            if (!HoldIsPublished(player, prop))
                Plugin.Log.LogWarning(
                    $"[{nameof(ReceivedItemSpawner)}] The hold did not take on the network; the other player will not see this gourd in anyone's hands.");

            return true;
        }

        // A late guest can get a hold before its prop; a fresh number makes it take the hold again.
        internal static void RepublishHolds()
        {
            if (!NetworkServer.active)
                return;

            var all = PlayerCharacter.allPlayerCharacters;
            if (all == null)
                return;

            foreach (var player in all)
            {
                try
                {
                    var networking = player != null ? player.playerNetworking : null;
                    if (networking == null)
                        continue;

                    var held = networking.playerHeldInformation;
                    if (held.identity == null)
                        continue;

                    held.actionNumber = held.actionNumber + 1;
                    networking.NetworkplayerHeldInformation = held;
                    Plugin.Log.LogInfo(
                        $"[{nameof(ReceivedItemSpawner)}] Hold of '{player.name}' said again for a guest that joined.");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(ReceivedItemSpawner)}] Could not say a hold again: {ex.Message}");
                }
            }
        }

        // "Published" means the networked field: the only one another machine can read.
        private static bool HoldIsPublished(PlayerCharacter player, Prop prop)
        {
            try
            {
                var networking = player.playerNetworking;
                var identity = prop.GetComponent<NetworkIdentity>();
                if (networking == null || identity == null)
                    return false;

                var held = networking.playerHeldInformation;
                return held.identity != null && held.identity == identity;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ReceivedItemSpawner)}] Could not read the published hold: {ex.Message}");
                return false;
            }
        }

        private static PlayerCharacter FindLocalPlayerCharacter()
        {
            var all = PlayerCharacter.allPlayerCharacters;
            if (all == null)
                return null;

            foreach (var pc in all)
            {
                if (pc != null && pc.isLocalPlayer)
                    return pc;
            }

            return null;
        }
    }
}
