using Mirror;

namespace BigWalkArchipelago.Core
{
    // The gourds' on-screen name, set by the host and sent to guests.
    internal static class GourdNames
    {
        internal const string Default = "Gourd";
        internal const int MaxLength = 24;

        private static string _mirrored;
        private static string _seed;

        internal static string Current
        {
            get
            {
                if (NetworkClient.active && !NetworkServer.active)
                    return string.IsNullOrEmpty(_mirrored) ? Default : _mirrored;

                var name = CleanedConfig(ModConfig.GourdName.Value);
                if (name.Length > 0 && name != Default)
                    return name;
                return string.IsNullOrEmpty(_seed) ? Default : _seed;
            }
        }

        internal static void Configure(Net.ApSlotData slot)
        {
            var name = Clean(slot.GourdName).Trim();
            if (name.Length > MaxLength)
                name = name.Substring(0, MaxLength);
            _seed = name.Length == 0 ? null : name;
            Net.ModChannel.SendSnapshotNow();
        }

        internal static bool IsGuest => NetworkClient.active && !NetworkServer.active;

        // Same filter as the apworld's gourd_name.py; TextMeshPro reads `<` and `>` as tags.
        private const string AllowedSigns = "'-_.,!?&+:;()@";

        internal static string Clean(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            // NFKC first, as the apworld does.
            try
            {
                name = name.Normalize(System.Text.NormalizationForm.FormKC);
            }
            catch (ArgumentException)
            {
                // A lone surrogate cannot be normalised; the filter below drops it anyway.
            }

            var text = new System.Text.StringBuilder();
            foreach (var character in name)
            {
                if (char.IsWhiteSpace(character))
                {
                    text.Append(' ');
                    continue;
                }

                var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character);
                var letter = char.IsLetterOrDigit(character)
                             || category == System.Globalization.UnicodeCategory.NonSpacingMark
                             || category == System.Globalization.UnicodeCategory.SpacingCombiningMark
                             || category == System.Globalization.UnicodeCategory.EnclosingMark
                             || category == System.Globalization.UnicodeCategory.LetterNumber
                             || category == System.Globalization.UnicodeCategory.OtherNumber;
                if (letter || AllowedSigns.IndexOf(character) >= 0)
                    text.Append(character);
            }

            var cleaned = string.Join(" ", text.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            // A name needs a letter: digits and signs alone are no name.
            foreach (var character in cleaned)
            {
                if (char.IsLetter(character))
                    return cleaned;
            }

            return string.Empty;
        }

        internal static void Set(string name)
        {
            name = Clean(name).Trim();
            if (name.Length > MaxLength)
                name = name.Substring(0, MaxLength);
            ModConfig.GourdName.Value = name.Length == 0 ? Default : name;
            Net.ModChannel.SendSnapshotNow();
        }

        internal static void ApplyFromHost(string name)
        {
            _mirrored = Clean(name);
        }

        // The .cfg text can be edited by hand: clean it once and cache the result.
        private static string _lastRaw;
        private static string _lastCleaned = string.Empty;

        private static string CleanedConfig(string raw)
        {
            if (!ReferenceEquals(raw, _lastRaw) && raw != _lastRaw)
            {
                _lastRaw = raw;
                _lastCleaned = Clean(raw);
                if (_lastCleaned.Length > MaxLength)
                    _lastCleaned = _lastCleaned.Substring(0, MaxLength).TrimEnd();
            }

            return _lastCleaned;
        }

        internal static void ForgetMirror()
        {
            _mirrored = null;
        }

        // Matched by id: the server may already send the seed's name.
        internal static string Display(long itemId, string itemName)
        {
            if (itemId == Net.ApLocationIds.GourdItemId)
                return Current;
            if (Net.ApLocationIds.TryResolveGadgetItem(itemId, out var kind) && kind == GadgetKind.GourdCarton)
                return Current + " Carton";
            return itemName;
        }

        internal static string Plural => Current == Default ? "gourds" : Current + "s";
    }
}
