using System;

namespace BigWalkArchipelago.Core.Net
{
    // Slot and password come from the save (the game stores them); host:port from the config.
    internal readonly struct ApEndpoint
    {
        // Players often paste just a hostname.
        private const int DefaultPort = 38281;

        internal string Host { get; }
        internal int Port { get; }
        internal string SlotName { get; }
        internal string Password { get; }
        internal string Uuid { get; }

        private ApEndpoint(string host, int port, string slotName, string password, string uuid)
        {
            Host = host;
            Port = port;
            SlotName = slotName;
            Password = password;
            Uuid = uuid;
        }

        internal static bool TryResolve(out ApEndpoint endpoint, out string problem)
        {
            endpoint = default;

            var save = SaveManager.instance != null ? SaveManager.instance.currentData : null;
            if (save == null)
            {
                problem = "no save loaded yet";
                return false;
            }

            // The game's per-save id, so the server sees the same client on every reconnect.
            var uuid = !string.IsNullOrWhiteSpace(save.filenameUid) ? save.filenameUid : save.slotName;

            return TryBuild(CurrentHostAndPort(), save.slotName, save.password, uuid, out endpoint, out problem);
        }

        // From the hosting screen: the save being set up is not the loaded one.
        internal static bool TryResolveFromFields(string rawHostPort, string slotName, string password,
                                                  out ApEndpoint endpoint, out string problem)
        {
            return TryBuild(rawHostPort, slotName, password, slotName, out endpoint, out problem);
        }

        internal static string CurrentHostAndPort()
        {
            var raw = ApSessionConfig.HostAndPort;
            return string.IsNullOrWhiteSpace(raw) ? ModConfig.ArchipelagoHostPort.Value : raw;
        }

        private static bool TryBuild(string rawHostPort, string slotName, string password, string uuid,
                                     out ApEndpoint endpoint, out string problem)
        {
            endpoint = default;

            if (!TryParseHostPort(rawHostPort, out var host, out var port))
            {
                problem = $"no usable Archipelago address (got '{rawHostPort}')";
                return false;
            }

            if (string.IsNullOrWhiteSpace(slotName))
            {
                problem = "the slot name (the save's name field on the hosting screen) is empty";
                return false;
            }

            endpoint = new ApEndpoint(host, port, slotName.Trim(), password ?? string.Empty,
                                      string.IsNullOrWhiteSpace(uuid) ? slotName.Trim() : uuid);
            problem = string.Empty;
            return true;
        }

        // Only used to explain a failure: archipelago.gg rooms each have their own port.
        internal static bool HasExplicitPort(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var value = raw.Trim();
            foreach (var scheme in new[] { "wss://", "ws://" })
            {
                if (value.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                {
                    value = value.Substring(scheme.Length);
                    break;
                }
            }

            value = value.TrimEnd('/');
            var separator = value.LastIndexOf(':');
            return separator >= 0 && separator < value.Length - 1;
        }

        internal static bool IsArchipelagoGg(string host)
        {
            return !string.IsNullOrEmpty(host)
                && host.Trim().EndsWith("archipelago.gg", StringComparison.OrdinalIgnoreCase);
        }

        internal const int DefaultPortForDisplay = DefaultPort;

        private static bool TryParseHostPort(string raw, out string host, out int port)
        {
            host = string.Empty;
            port = DefaultPort;

            if (string.IsNullOrWhiteSpace(raw))
                return false;

            var value = raw.Trim();

            foreach (var scheme in new[] { "wss://", "ws://" })
            {
                if (value.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                {
                    value = value.Substring(scheme.Length);
                    break;
                }
            }

            value = value.TrimEnd('/');

            var separator = value.LastIndexOf(':');
            if (separator >= 0)
            {
                var portText = value.Substring(separator + 1);
                host = value.Substring(0, separator);

                // "archipelago.gg:" is the first-launch prefill: fall back to the default port.
                if (portText.Length > 0 && (!int.TryParse(portText, out port) || port is < 1 or > 65535))
                    return false;

                if (portText.Length == 0)
                    port = DefaultPort;
            }
            else
            {
                host = value;
            }

            return host.Length > 0;
        }
    }
}
