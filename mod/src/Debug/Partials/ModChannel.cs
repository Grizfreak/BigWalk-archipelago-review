using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.Attributes;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    internal partial class ModChannel
    {
        internal static int SendSummon(Vector3 position, Quaternion rotation)
        {
            var sent = 0;
            foreach (var connection in NetworkServer.connections.Values)
            {
                if (connection == null || !Announced.Contains(connection.connectionId) || !IsFromThisMachine(connection))
                    continue;

                try
                {
                    // Several guests each a step further back, so they do not land in one another.
                    var spot = position + rotation * Vector3.back * (1.5f * sent);
                    var writer = new NetworkWriter();
                    NetworkWriterExtensions.WriteUShort(writer, MessageId);
                    writer.WriteByte(KindSummon);
                    writer.WriteByte(ProtocolVersion);
                    NetworkWriterExtensions.WriteFloat(writer, spot.x);
                    NetworkWriterExtensions.WriteFloat(writer, spot.y);
                    NetworkWriterExtensions.WriteFloat(writer, spot.z);
                    NetworkWriterExtensions.WriteFloat(writer, rotation.eulerAngles.y);
                    connection.Send(writer.ToArraySegment(), 0);
                    sent++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(
                        $"[{nameof(ModChannel)}] Could not send the summon to guest #{connection.connectionId}: {ex.Message}");
                }
            }

            return sent;
        }

        // Connection 0 is the host's own client; others carry the remote address.
        private static bool IsFromThisMachine(NetworkConnectionToClient connection)
        {
            if (connection.connectionId == NetworkConnection.LocalConnectionId)
                return false;
            if (!System.Net.IPAddress.TryParse(connection.address, out var address))
                return false;
            if (address.IsIPv4MappedToIPv6)
                address = address.MapToIPv4();
            return System.Net.IPAddress.IsLoopback(address);
        }
    }
}
