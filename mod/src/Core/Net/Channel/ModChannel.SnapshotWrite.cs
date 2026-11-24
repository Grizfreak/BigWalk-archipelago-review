using Mirror;

namespace BigWalkArchipelago.Core.Net
{
    internal partial class ModChannel
    {
        internal static void SendSnapshotNow()
        {
            if (NetworkServer.active)
                SendSnapshotToAnnounced();
        }

        private static void SendSnapshotToAnnounced()
        {
            foreach (var connection in NetworkServer.connections.Values)
            {
                if (connection != null && Announced.Contains(connection.connectionId))
                    SendSnapshot(connection);
            }
        }

        private static void SendSnapshot(NetworkConnection connection)
        {
            try
            {
                var writer = new NetworkWriter();
                NetworkWriterExtensions.WriteUShort(writer, MessageId);
                writer.WriteByte(KindSnapshot);
                writer.WriteByte(ProtocolVersion);

                NetworkWriterExtensions.WriteString(writer, ApRuntime.StatusMessage ?? string.Empty);
                NetworkWriterExtensions.WriteBool(writer, ApRuntime.StatusIsWarning);
                NetworkWriterExtensions.WriteString(writer, ApRuntime.GoalLine ?? string.Empty);
                NetworkWriterExtensions.WriteBool(writer, ApRuntime.GoalLineIsWarning);

                var lines = ApNotices.Lines;
                var count = Math.Min(lines.Count, byte.MaxValue);
                writer.WriteByte((byte)count);
                for (var i = 0; i < count; i++)
                {
                    NetworkWriterExtensions.WriteString(writer, lines[i].Display ?? string.Empty);
                    NetworkWriterExtensions.WriteBool(writer, lines[i].Warning);
                }

                NetworkWriterExtensions.WriteBool(writer, RadioStations.ItemsInPlay);
                var granted = new List<SavableSystem>();
                foreach (var system in RadioStations.All)
                {
                    if (RadioStations.IsGranted(system))
                        granted.Add(system);
                }

                writer.WriteByte((byte)granted.Count);
                foreach (var system in granted)
                    writer.WriteByte((byte)(int)system);

                NetworkWriterExtensions.WriteBool(writer, PuzzleNeeds.Enabled);
                var lockedNeeds = PuzzleNeeds.LockedNeeds();
                writer.WriteByte((byte)lockedNeeds.Count);
                foreach (var need in lockedNeeds)
                    NetworkWriterExtensions.WriteString(writer, need);

                NetworkWriterExtensions.WriteBool(writer, GauntletStairways.Enabled);
                NetworkWriterExtensions.WriteBool(writer, GauntletStairways.PartsHidden);
                NetworkWriterExtensions.WriteUShort(writer, (ushort)TeleportButtons.HostMask());
                NetworkWriterExtensions.WriteUShort(writer, (ushort)CabinFeverWaits.Seconds(0));
                NetworkWriterExtensions.WriteUShort(writer, (ushort)CabinFeverWaits.Seconds(1));
                writer.WriteByte((byte)((CabinFeverWaits.Help(0) ? 1 : 0) | (CabinFeverWaits.Help(1) ? 2 : 0)
                                        | (TileThiefHelper.HostMode << 2)
                                        | (CabinFeverWaits.HideTimer(0) ? 16 : 0) | (CabinFeverWaits.HideTimer(1) ? 32 : 0)));
                NetworkWriterExtensions.WriteUShort(writer, ResyncStations.HostState());
                NetworkWriterExtensions.WriteString(writer, GourdNames.Current);
                writer.WriteByte(PackChecks.HostMode);
                writer.WriteByte(PackChecks.HostObjectMode);
                NetworkWriterExtensions.WriteBool(writer, FastGolf.HostLightweight);
                var kinds = new List<string>(PackChecks.HostCountedKinds);
                writer.WriteByte((byte)kinds.Count);
                foreach (var kind in kinds)
                    NetworkWriterExtensions.WriteString(writer, kind);
                var collected = new List<string>(PackChecks.HostCollected);
                writer.WriteByte((byte)Math.Min(collected.Count, byte.MaxValue));
                for (var i = 0; i < collected.Count && i < byte.MaxValue; i++)
                    NetworkWriterExtensions.WriteString(writer, collected[i]);
                var colours = Palette.HostColours;
                writer.WriteByte((byte)Math.Min(colours.Length, byte.MaxValue));
                for (var i = 0; i < colours.Length && i < byte.MaxValue; i++)
                    writer.WriteByte((byte)colours[i]);
                NetworkWriterExtensions.WriteString(writer, ObjectiveBoard.HostText());
                writer.WriteByte(ObjectiveBoard.HostLocks());
                writer.WriteByte(MonumentOrder.HostMask());
                for (var kind = 0; kind < ApSounds.Counts.Length; kind++)
                    writer.WriteByte(ApSounds.Counts[kind]);
                NetworkWriterExtensions.WriteBool(writer, AbilityLocks.Enabled);
                NetworkWriterExtensions.WriteUShort(writer, AbilityLocks.HostMask());
                writer.WriteByte(HintBoards.HostFlags());
                var zoneLocked = new List<string>(ZoneLocks.HostLocked);
                writer.WriteByte((byte)Math.Min(zoneLocked.Count, byte.MaxValue));
                for (var i = 0; i < zoneLocked.Count && i < byte.MaxValue; i++)
                    NetworkWriterExtensions.WriteString(writer, zoneLocked[i]);
                var pegTiles = CustomTiles.HostTiles;
                writer.WriteByte((byte)Math.Min(pegTiles.Length, byte.MaxValue));
                for (var i = 0; i < pegTiles.Length && i < byte.MaxValue; i++)
                    NetworkWriterExtensions.WriteString(writer, pegTiles[i]);
                var tileSounds = TileSounds.HostManifest();
                writer.WriteByte((byte)tileSounds.Count);
                foreach (var pair in tileSounds)
                {
                    writer.WriteByte((byte)pair.Key);
                    NetworkWriterExtensions.WriteULong(writer, pair.Value);
                }
                var drawings = CustomTiles.HostManifest();
                writer.WriteByte((byte)Math.Min(drawings.Count, byte.MaxValue));
                var written = 0;
                foreach (var pair in drawings)
                {
                    if (written++ == byte.MaxValue)
                        break;
                    NetworkWriterExtensions.WriteString(writer, pair.Key);
                    NetworkWriterExtensions.WriteULong(writer, pair.Value);
                }

                connection.Send(writer.ToArraySegment(), 0);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"[{nameof(ModChannel)}] Could not send a snapshot to guest #{connection.connectionId}: {ex.Message}");
            }
        }
    }
}
