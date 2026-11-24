using UnityEngine;

namespace BigWalkArchipelago.Core.Effects
{
    internal static partial class TrapEffects
    {
        // The Silent Gauntlet's white only; BigGhostPatch sets it after PlayerSpeechless.Update.
        internal const string Ghost = "ghost";
        private static float _ghostUntil = -1f;

        internal static bool GhostActive => _ghostUntil >= 0f;

        // Each player takes the next one's colours in netId order: every machine swaps alike.
        internal const string Paint = "paint";
        private static float _paintUntil = -1f;
        private static readonly List<(PlayerCharacter target, PlayerLookSet.LookColor head, PlayerLookSet.LookColor torso, PlayerLookSet.LookColor legs)> Painted = new();
        private static readonly List<(PlayerCharacter target, PlayerLookSet.LookColor head, PlayerLookSet.LookColor torso, PlayerLookSet.LookColor legs)> OwnColours = new();

        private static void StartPaint(int seconds)
        {
            var players = new List<PlayerCharacter>();
            foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None))
            {
                if (player != null && player.looks != null)
                    players.Add(player);
            }
            if (players.Count < 2)
            {
                Plugin.Log.LogInfo($"{Tag} Big Paint: alone, nothing to swap.");
                return;
            }
            players.Sort((a, b) => a.netId.CompareTo(b.netId));

            EndPaint();
            for (var i = 0; i < players.Count; i++)
            {
                var source = players[(i + 1) % players.Count].looks;
                Painted.Add((players[i], source.headColor, source.torsoColor, source.legsColor));
                var own = players[i].looks;
                OwnColours.Add((players[i], own.headColor, own.torsoColor, own.legsColor));
            }
            _paintUntil = Time.unscaledTime + seconds;
            HoldPaint();
        }

        internal static bool PaintActive => _paintUntil >= 0f;

        // Swapped in as the game sets it: setting it every frame from outside made it flicker.

        internal static bool TryPaintColour(PlayerLooks looks, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<PlayerLooks.LooksRenderer> part, out PlayerLookSet.LookColor colour)
        {
            foreach (var (target, head, torso, legs) in Painted)
            {
                if (target == null || target.looks == null || part == null || target.looks.Pointer != looks.Pointer)
                    continue;
                if (looks.headRenderers != null && part.Pointer == looks.headRenderers.Pointer) { colour = head; return true; }
                if (looks.torsoRenderers != null && part.Pointer == looks.torsoRenderers.Pointer) { colour = torso; return true; }
                if (looks.legsRenderers != null && part.Pointer == looks.legsRenderers.Pointer) { colour = legs; return true; }
            }
            colour = default;
            return false;
        }

        private static void HoldPaint()
        {
            foreach (var (target, head, torso, legs) in Painted)
            {
                var looks = target != null ? target.looks : null;
                if (looks == null)
                    continue;
                looks.SetColor(looks.headRenderers, head);
                looks.SetColor(looks.torsoRenderers, torso);
                looks.SetColor(looks.legsRenderers, legs);
            }
        }

        private static void EndPaint()
        {
            if (Painted.Count == 0)
            {
                _paintUntil = -1f;
                return;
            }
            // Off first, or the patch would swap out the colours put back below.
            _paintUntil = -1f;
            foreach (var (target, head, torso, legs) in OwnColours)
            {
                try
                {
                    var looks = target != null ? target.looks : null;
                    if (looks == null)
                        continue;
                    looks.SetColor(looks.headRenderers, head);
                    looks.SetColor(looks.torsoRenderers, torso);
                    looks.SetColor(looks.legsRenderers, legs);
                    looks.RefreshLook();
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"{Tag} Big Paint end: {ex.Message}");
                }
            }
            Painted.Clear();
            OwnColours.Clear();
            _paintUntil = -1f;
            Plugin.Log.LogInfo($"{Tag} Big Paint over.");
        }

        // The game writes whiteness only when it changes, so each one's own is set back at the end.
        private static void EndGhost()
        {
            if (_ghostUntil < 0f)
                return;
            _ghostUntil = -1f;
            try
            {
                foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None))
                {
                    if (player != null && player.looks != null)
                        player.looks.SetSpeechlessness(player.speechless != null ? player.speechless.speechlessness : 0f);
                }
                Plugin.Log.LogInfo($"{Tag} Big Ghost over.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"{Tag} Big Ghost end: {ex.Message}");
            }
        }
    }
}
