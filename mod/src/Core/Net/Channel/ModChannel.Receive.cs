using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    internal partial class ModChannel
    {
        // Same rule as OnServerMessage: nothing may escape it.
        private static void OnClientMessage(NetworkConnection connection, NetworkReader reader, int channelId)
        {
            try
            {
                var kind = reader.ReadByte();
                if (kind == KindSummon)
                {
                    OnSummon(reader);
                    return;
                }

                if (kind == KindPress)
                {
                    if (reader.ReadByte() == ProtocolVersion)
                        WorldButtons.RunPress(reader.ReadByte());
                    return;
                }

                if (kind == KindEffect)
                {
                    if (reader.ReadByte() != ProtocolVersion)
                        return;

                    var key = NetworkReaderExtensions.ReadString(reader);
                    var seconds = NetworkReaderExtensions.ReadUShort(reader);
                    var hasDestination = NetworkReaderExtensions.ReadBool(reader);
                    var point = new Vector3(
                        NetworkReaderExtensions.ReadFloat(reader),
                        NetworkReaderExtensions.ReadFloat(reader),
                        NetworkReaderExtensions.ReadFloat(reader));
                    var yaw = NetworkReaderExtensions.ReadFloat(reader);
                    TrapEffects.Play(key, seconds, hasDestination ? point : null, yaw);
                    return;
                }

                if (kind == KindMap)
                {
                    if (reader.ReadByte() == ProtocolVersion)
                        MapTracker.ApplyFromHost(NetworkReaderExtensions.ReadString(reader));
                    return;
                }

                if (kind == KindBlob)
                {
                    if (reader.ReadByte() != ProtocolVersion)
                        return;
                    var channel = reader.ReadByte();
                    var key = NetworkReaderExtensions.ReadString(reader);
                    var hash = NetworkReaderExtensions.ReadULong(reader);
                    var total = NetworkReaderExtensions.ReadInt(reader);
                    var offset = NetworkReaderExtensions.ReadInt(reader);
                    var length = NetworkReaderExtensions.ReadInt(reader);
                    // Bounded before reading: no chunk is larger than the host cuts them.
                    if (length <= 0 || length > BlobChannel.ChunkBytes || key == null || key.Length > BlobChannel.MaxKeyLength)
                        return;
                    var raw = NetworkReaderExtensions.ReadBytes(reader, length);
                    var data = new byte[length];
                    for (var i = 0; i < length; i++)
                        data[i] = raw[i];
                    if (channel == 0)
                        TileSounds.ReceiveChunk(key, hash, total, offset, data);
                    else if (channel == 1)
                        CustomTiles.ReceiveChunk(key, hash, total, offset, data);
                    return;
                }

                if (kind == KindNotice)
                {
                    if (reader.ReadByte() == ProtocolVersion)
                    {
                        var text = NetworkReaderExtensions.ReadString(reader);
                        ApNotices.Post(text, warning: NetworkReaderExtensions.ReadBool(reader));
                    }

                    return;
                }

                if (kind != KindSnapshot)
                    return;

                var version = reader.ReadByte();
                if (version != ProtocolVersion)
                {
                    _hostIsAnotherBuild = true;
                    if (!_versionMismatchLogged)
                    {
                        _versionMismatchLogged = true;
                        Plugin.Log.LogWarning(
                            $"[{nameof(ModChannel)}] The host speaks protocol v{version}, this build v{ProtocolVersion}; "
                            + "ignoring it. Both players need the same build of the mod.");
                    }
                    return;
                }

                var status = NetworkReaderExtensions.ReadString(reader);
                var statusWarning = NetworkReaderExtensions.ReadBool(reader);
                var goal = NetworkReaderExtensions.ReadString(reader);
                var goalWarning = NetworkReaderExtensions.ReadBool(reader);

                var lines = new List<MirroredLine>();
                var count = reader.ReadByte();
                for (var i = 0; i < count; i++)
                {
                    lines.Add(new MirroredLine
                    {
                        Text = NetworkReaderExtensions.ReadString(reader),
                        Warning = NetworkReaderExtensions.ReadBool(reader),
                    });
                }

                var radioItemsInPlay = NetworkReaderExtensions.ReadBool(reader);
                var granted = new List<SavableSystem>();
                var grantedCount = reader.ReadByte();
                for (var i = 0; i < grantedCount; i++)
                    granted.Add((SavableSystem)reader.ReadByte());

                var needsEnabled = NetworkReaderExtensions.ReadBool(reader);
                var lockedNeeds = new List<string>();
                var lockedCount = reader.ReadByte();
                for (var i = 0; i < lockedCount; i++)
                    lockedNeeds.Add(NetworkReaderExtensions.ReadString(reader));

                var gauntletLocked = NetworkReaderExtensions.ReadBool(reader);
                var gauntletPartsLocked = NetworkReaderExtensions.ReadBool(reader);
                var teleportMask = NetworkReaderExtensions.ReadUShort(reader);
                var cabinFever = NetworkReaderExtensions.ReadUShort(reader);
                var cabinFeverLong = NetworkReaderExtensions.ReadUShort(reader);
                var cabinFeverHelp = reader.ReadByte();
                var resyncStations = NetworkReaderExtensions.ReadUShort(reader);
                var gourdName = NetworkReaderExtensions.ReadString(reader);
                var packMode = reader.ReadByte();
                var objectMode = reader.ReadByte();
                var golfLightweight = NetworkReaderExtensions.ReadBool(reader);
                var objectKinds = new List<string>();
                var objectKindCount = reader.ReadByte();
                for (var i = 0; i < objectKindCount; i++)
                    objectKinds.Add(NetworkReaderExtensions.ReadString(reader));
                var collectedPacks = new List<string>();
                var collectedCount = reader.ReadByte();
                for (var i = 0; i < collectedCount; i++)
                    collectedPacks.Add(NetworkReaderExtensions.ReadString(reader));
                var colours = new List<byte>();
                var colourCount = reader.ReadByte();
                for (var i = 0; i < colourCount; i++)
                    colours.Add(reader.ReadByte());
                var boardText = NetworkReaderExtensions.ReadString(reader);
                var endingLocks = reader.ReadByte();
                var monumentMask = reader.ReadByte();
                var soundCounts = new byte[ApSounds.Counts.Length];
                for (var sound = 0; sound < soundCounts.Length; sound++)
                    soundCounts[sound] = reader.ReadByte();
                var abilitiesEnabled = NetworkReaderExtensions.ReadBool(reader);
                var abilityMask = NetworkReaderExtensions.ReadUShort(reader);
                var hintFlags = reader.ReadByte();
                var zoneLocked = new List<string>();
                var zoneLockedCount = reader.ReadByte();
                for (var i = 0; i < zoneLockedCount; i++)
                    zoneLocked.Add(NetworkReaderExtensions.ReadString(reader));
                var pegTiles = new List<string>();
                var pegTileCount = reader.ReadByte();
                for (var i = 0; i < pegTileCount; i++)
                    pegTiles.Add(NetworkReaderExtensions.ReadString(reader));
                var tileSounds = new Dictionary<int, ulong>();
                var tileSoundCount = reader.ReadByte();
                for (var i = 0; i < tileSoundCount; i++)
                {
                    var soundKind = reader.ReadByte();
                    var hash = NetworkReaderExtensions.ReadULong(reader);
                    if (soundKind < 9)
                        tileSounds[soundKind] = hash;
                }
                var drawings = new Dictionary<string, ulong>();
                var drawingCount = reader.ReadByte();
                for (var i = 0; i < drawingCount; i++)
                {
                    var key = NetworkReaderExtensions.ReadString(reader);
                    var hash = NetworkReaderExtensions.ReadULong(reader);
                    if (!string.IsNullOrEmpty(key) && key.Length <= BlobChannel.MaxKeyLength)
                        drawings[key] = hash;
                }

                var first = _receivedAt < 0f;

                MirroredStatus = string.IsNullOrEmpty(status) ? null : status;
                MirroredStatusIsWarning = statusWarning;
                MirroredGoal = string.IsNullOrEmpty(goal) ? null : goal;
                MirroredGoalIsWarning = goalWarning;

                // A guest hears what the host hears, except on the first snapshot.
                if (!first)
                {
                    for (var sound = 0; sound < soundCounts.Length; sound++)
                    {
                        if (soundCounts[sound] != _heardCounts[sound])
                            ApSounds.PlayForGuest(sound);
                    }
                }

                System.Array.Copy(soundCounts, _heardCounts, soundCounts.Length);

                MirroredLines.Clear();
                MirroredLines.AddRange(lines);
                _receivedAt = Time.unscaledTime;

                if (first)
                    Plugin.Log.LogInfo(
                        $"[{nameof(ModChannel)}] First word from the host: '{MirroredStatus}'. Showing the overlay here too.");

                RadioStations.ApplyFromHost(radioItemsInPlay, granted);
                PuzzleNeeds.ApplyFromHost(needsEnabled, lockedNeeds);
                GauntletStairways.ApplyFromHost(gauntletLocked, gauntletPartsLocked);
                TeleportButtons.ApplyFromHost(teleportMask);
                CabinFeverWaits.ApplyFromHost(cabinFever, cabinFeverLong, (cabinFeverHelp & 1) != 0, (cabinFeverHelp & 2) != 0,
                    (cabinFeverHelp & 16) != 0, (cabinFeverHelp & 32) != 0);
                TileThiefHelper.ApplyFromHost((cabinFeverHelp >> 2) & 3);
                ResyncStations.ApplyFromHost(resyncStations);
                PackChecks.ApplyFromHost(packMode, objectMode, objectKinds, collectedPacks);
                FastGolf.ApplyFromHost(golfLightweight);
                Palette.ApplyFromHost(colours);
                GourdNames.ApplyFromHost(gourdName);
                ObjectiveBoard.ApplyFromHost(boardText, endingLocks);
                MonumentOrder.ApplyFromHost(monumentMask);
                AbilityLocks.ApplyFromHost(abilitiesEnabled, abilityMask);
                HintBoards.ApplyFromHost(hintFlags);
                ZoneLocks.ApplyFromHost(zoneLocked);
                CustomTiles.ApplyFromHost(pegTiles);
                CustomTiles.ApplyDrawingsFromHost(drawings);
                TileSounds.ApplyManifestFromHost(tileSounds);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(ModChannel)}] Unreadable snapshot from the host, ignored: {ex.Message}");
            }
        }
    }
}
