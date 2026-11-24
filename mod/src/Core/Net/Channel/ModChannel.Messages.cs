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
        internal static void SendPress(NetworkConnection connection, int slot)
        {
            if (connection == null || !Announced.Contains(connection.connectionId))
                return;

            try
            {
                var writer = new NetworkWriter();
                NetworkWriterExtensions.WriteUShort(writer, MessageId);
                writer.WriteByte(KindPress);
                writer.WriteByte(ProtocolVersion);
                writer.WriteByte((byte)slot);
                connection.Send(writer.ToArraySegment(), 0);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ModChannel)}] Could not send a button press to guest #{connection.connectionId}: {ex.Message}");
            }
        }

        internal static void SendEffect(NetworkConnection connection, string key, int seconds, Vector3? destination, float yaw)
        {
            if (connection == null || !Announced.Contains(connection.connectionId))
                return;

            try
            {
                var writer = new NetworkWriter();
                NetworkWriterExtensions.WriteUShort(writer, MessageId);
                writer.WriteByte(KindEffect);
                writer.WriteByte(ProtocolVersion);
                NetworkWriterExtensions.WriteString(writer, key);
                NetworkWriterExtensions.WriteUShort(writer, (ushort)Math.Clamp(seconds, 0, ushort.MaxValue));
                NetworkWriterExtensions.WriteBool(writer, destination != null);
                var point = destination ?? Vector3.zero;
                NetworkWriterExtensions.WriteFloat(writer, point.x);
                NetworkWriterExtensions.WriteFloat(writer, point.y);
                NetworkWriterExtensions.WriteFloat(writer, point.z);
                NetworkWriterExtensions.WriteFloat(writer, yaw);
                connection.Send(writer.ToArraySegment(), 0);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ModChannel)}] Could not send {key} to guest #{connection.connectionId}: {ex.Message}");
            }
        }

        internal static bool SendStatus(bool masked)
        {
            if (!_helloSent || !_hostHasMod)
                return false;

            try
            {
                var connection = NetworkClient.connection;
                if (connection == null)
                    return false;

                var writer = new NetworkWriter();
                NetworkWriterExtensions.WriteUShort(writer, MessageId);
                writer.WriteByte(KindStatus);
                writer.WriteByte(ProtocolVersion);
                NetworkWriterExtensions.WriteBool(writer, masked);
                connection.Send(writer.ToArraySegment(), 0);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ModChannel)}] Could not tell the host about the mask: {ex.Message}");
                return false;
            }
        }

        internal static void SendMap(string text)
        {
            foreach (var connection in NetworkServer.connections.Values)
            {
                if (connection == null || !Announced.Contains(connection.connectionId))
                    continue;
                try
                {
                    var writer = new NetworkWriter();
                    NetworkWriterExtensions.WriteUShort(writer, MessageId);
                    writer.WriteByte(KindMap);
                    writer.WriteByte(ProtocolVersion);
                    NetworkWriterExtensions.WriteString(writer, text);
                    connection.Send(writer.ToArraySegment(), 0);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(ModChannel)}] Could not send the map to guest #{connection.connectionId}: {ex.Message}");
                }
            }
        }

        internal static void SendNotice(NetworkConnection connection, string text, bool warning)
        {
            if (connection == null || !Announced.Contains(connection.connectionId))
                return;

            try
            {
                var writer = new NetworkWriter();
                NetworkWriterExtensions.WriteUShort(writer, MessageId);
                writer.WriteByte(KindNotice);
                writer.WriteByte(ProtocolVersion);
                NetworkWriterExtensions.WriteString(writer, text);
                NetworkWriterExtensions.WriteBool(writer, warning);
                connection.Send(writer.ToArraySegment(), 0);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ModChannel)}] Could not send a notice to guest #{connection.connectionId}: {ex.Message}");
            }
        }
    }
}
