using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;

namespace BigWalkArchipelago.Core.Net
{
    internal sealed partial class ApConnection
    {
        // A Bounce tagged "DeathLink"; sending needs no tag of ours.
        internal bool SendDeathLink(string cause)
        {
            if (_status != ConnectionStatus.Connected || _deathLink == null)
                return false;

            try
            {
                _deathLink.SendDeathLink(new DeathLink(SlotName, cause));
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Failed to send a DeathLink: {ex.Message}");
                return false;
            }
        }

        // Subscribes to the "DeathLink" tag, for `death_link` on receive or both.
        internal void ListenForDeathLinks(bool listen)
        {
            if (_status != ConnectionStatus.Connected || _deathLink == null)
                return;

            try
            {
                if (listen)
                    _deathLink.EnableDeathLink();
                else
                    _deathLink.DisableDeathLink();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Could not change the DeathLink tag: {ex.Message}");
            }
        }

        // TrapLink: Bounce packets tagged "TrapLink", {"time", "source", "trap_name"}.

        private readonly ConcurrentQueue<(string source, string trap)> _trapLinksReceived = new();
        private bool _trapLinkHooked;

        // Asks the server for the TrapLinks of the multiworld (the tag), or stops.
        internal void ListenForTrapLinks(bool listen)
        {
            if (_status != ConnectionStatus.Connected || _session == null)
                return;

            try
            {
                var tags = new List<string>(_session.ConnectionInfo.Tags ?? Array.Empty<string>());
                tags.Remove("TrapLink");
                if (listen)
                    tags.Add("TrapLink");
                _session.ConnectionInfo.UpdateConnectionOptions(tags.ToArray());

                if (listen && !_trapLinkHooked)
                {
                    _trapLinkHooked = true;
                    var own = SlotName;
                    _session.Socket.PacketReceived += packet =>
                    {
                        // Managed types only, off Unity's thread: queued for the main loop.
                        if (packet is not Archipelago.MultiClient.Net.Packets.BouncedPacket bounced
                            || bounced.Tags == null || !bounced.Tags.Contains("TrapLink") || bounced.Data == null)
                            return;
                        var source = bounced.Data.TryGetValue("source", out var s) ? s?.ToString() : null;
                        var trap = bounced.Data.TryGetValue("trap_name", out var t) ? t?.ToString() : null;
                        if (!string.IsNullOrEmpty(trap) && source != own)
                            _trapLinksReceived.Enqueue((source ?? "?", trap));
                    };
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Could not change the TrapLink tag: {ex.Message}");
            }
        }

        internal bool SendTrapLink(string trapName)
        {
            if (_status != ConnectionStatus.Connected || _session == null)
                return false;

            try
            {
                _session.Socket.SendPacket(new Archipelago.MultiClient.Net.Packets.BouncePacket
                {
                    Tags = new List<string> { "TrapLink" },
                    Data = new Dictionary<string, Newtonsoft.Json.Linq.JToken>
                    {
                        ["time"] = (double)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0,
                        ["source"] = SlotName,
                        ["trap_name"] = trapName,
                    },
                });
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ApConnection)}] Failed to send a TrapLink: {ex.Message}");
                return false;
            }
        }

        internal bool TryDequeueTrapLink(out string source, out string trap)
        {
            var found = _trapLinksReceived.TryDequeue(out var pair);
            source = pair.source;
            trap = pair.trap;
            return found;
        }

        internal bool TryDequeueDeathLink(out string cause)
        {
            return _deathLinksReceived.TryDequeue(out cause);
        }
    }
}
