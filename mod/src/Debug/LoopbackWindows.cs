using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BigWalkArchipelago.Debug
{
    // Hands the keyboard focus to the next game instance; only the foreground process can give it.
    internal static class LoopbackWindows
    {
        private const string Tag = "[" + nameof(LoopbackWindows) + "]";
        private const int SwRestore = 9;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr window);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr window, int command);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr window);

        internal static void FocusOtherInstance()
        {
            using var self = Process.GetCurrentProcess();

            // The next instance in process order, wrapping around.
            var windows = new List<Process>();
            foreach (var candidate in Process.GetProcessesByName(self.ProcessName))
            {
                if (candidate.Id != self.Id && candidate.MainWindowHandle != IntPtr.Zero)
                    windows.Add(candidate);
                else
                    candidate.Dispose();
            }

            windows.Sort((x, y) => x.Id.CompareTo(y.Id));
            var other = windows.Find(p => p.Id > self.Id) ?? (windows.Count > 0 ? windows[0] : null);
            foreach (var window in windows)
            {
                if (window != other)
                    window.Dispose();
            }

            if (other == null)
            {
                Plugin.Log.LogInfo($"{Tag} No other instance of the game with a window; nothing to switch to.");
                return;
            }

            using (other)
            {
                var window = other.MainWindowHandle;
                if (IsIconic(window))
                    ShowWindow(window, SwRestore);

                var switched = SetForegroundWindow(window);
                Plugin.Log.LogInfo(switched
                    ? $"{Tag} Focus handed to process {other.Id}."
                    : $"{Tag} Windows refused to hand the focus to process {other.Id}.");
            }
        }
    }
}
