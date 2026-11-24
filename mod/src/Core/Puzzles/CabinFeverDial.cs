using UnityEngine;

namespace BigWalkArchipelago.Core.Puzzles
{
    // Writes "??:??" over a hidden Cabin Fever wait, with one cell flashing a digit now and then.
    internal static class CabinFeverDial
    {
        private const float FlashEvery = 15f;
        private const float FlashFor = 0.25f;
        private const string Hidden = "??:??";

        // Debug (Alt + 5): shows the animation even while no wait runs.
        internal static bool Preview;

        // 0 Cabin Fever, 1 Cabin Fever Long, -1 another timer.
        private static readonly Dictionary<int, int> Rooms = new();
        private static readonly HashSet<int> Written = new();

        internal static void Apply(TimerTextController controller)
        {
            if (controller == null || controller.textField == null)
                return;
            var room = RoomOf(controller);
            if (room < 0)
                return;

            var timer = controller.peckEffectTimer;
            var running = timer != null && timer.GetTimeRemaining() > 0f;
            if (!Preview && !(running && CabinFeverWaits.HideTimer(room)))
            {
                // The game only writes its text when the second changes: make it write it again.
                if (Written.Remove(controller.GetInstanceID()))
                    controller.displayedSeconds = -1;
                return;
            }

            Written.Add(controller.GetInstanceID());
            var text = Frame(Time.time);
            if (controller.textField.text != text)
                controller.textField.text = text;
        }

        private static string Frame(float now)
        {
            var period = (int)(now / FlashEvery);
            if (now - period * FlashEvery >= FlashFor)
                return Hidden;
            var hash = unchecked((uint)period * 2654435761u);
            var cells = new[] { '?', '?', '?', '?' };
            cells[hash % 4] = (char)('0' + (hash >> 8) % 10);
            return $"{cells[0]}{cells[1]}:{cells[2]}{cells[3]}";
        }

        private static int RoomOf(TimerTextController controller)
        {
            var id = controller.GetInstanceID();
            if (Rooms.TryGetValue(id, out var known))
                return known;

            var room = -1;
            for (var t = controller.transform; t != null; t = t.parent)
            {
                if (t.name.StartsWith("CabinFeverLong", StringComparison.Ordinal))
                {
                    room = 1;
                    break;
                }
                if (t.name.StartsWith("CabinFever", StringComparison.Ordinal))
                {
                    room = 0;
                    break;
                }
            }
            Rooms[id] = room;
            return room;
        }
    }
}
