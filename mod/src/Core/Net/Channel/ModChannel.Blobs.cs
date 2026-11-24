using Mirror;

namespace BigWalkArchipelago.Core.Net
{
    internal partial class ModChannel
    {
        private static void PumpBlobs()
        {
            var guests = new List<NetworkConnection>();
            foreach (var connection in NetworkServer.connections.Values)
            {
                if (connection != null && Announced.Contains(connection.connectionId))
                    guests.Add(connection);
            }
            TileSounds.PumpHost(guests);
            CustomTiles.PumpHost(guests);
        }

        internal static void SendBlobChunk(NetworkConnection connection, byte channel, string key, ulong hash, byte[] compressed, int offset, int count)
        {
            try
            {
                var chunk = new byte[count];
                Buffer.BlockCopy(compressed, offset, chunk, 0, count);
                var writer = new NetworkWriter();
                NetworkWriterExtensions.WriteUShort(writer, MessageId);
                writer.WriteByte(KindBlob);
                writer.WriteByte(ProtocolVersion);
                writer.WriteByte(channel);
                NetworkWriterExtensions.WriteString(writer, key);
                NetworkWriterExtensions.WriteULong(writer, hash);
                NetworkWriterExtensions.WriteInt(writer, compressed.Length);
                NetworkWriterExtensions.WriteInt(writer, offset);
                NetworkWriterExtensions.WriteInt(writer, count);
                writer.WriteBytes((Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<byte>)chunk, 0, count);
                connection.Send(writer.ToArraySegment(), 0);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ModChannel)}] Could not send '{key}' to guest #{connection.connectionId}: {ex.Message}");
            }
        }
    }
}
