namespace BigWalkArchipelago.Core
{
    internal static class LocalPlayer
    {
        internal static PlayerCharacter Find()
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
