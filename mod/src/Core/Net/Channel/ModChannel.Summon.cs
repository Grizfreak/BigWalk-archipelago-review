using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Net
{
    internal partial class ModChannel
    {
        // Called from OnClientMessage's try, so nothing escapes it either.
        private static void OnSummon(NetworkReader reader)
        {
            if (reader.ReadByte() != ProtocolVersion)
                return;

            var position = new Vector3(
                NetworkReaderExtensions.ReadFloat(reader),
                NetworkReaderExtensions.ReadFloat(reader),
                NetworkReaderExtensions.ReadFloat(reader));
            var yaw = NetworkReaderExtensions.ReadFloat(reader);

#if DEBUG
            BigWalkArchipelago.Debug.LoopbackGuest.OnSummoned(position, Quaternion.Euler(0f, yaw, 0f));
#endif
        }
    }
}
