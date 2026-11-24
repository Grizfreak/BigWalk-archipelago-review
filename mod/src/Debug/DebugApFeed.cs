using BigWalkArchipelago.Core;
using BigWalkArchipelago.Core.Net;

namespace BigWalkArchipelago.Debug
{
    // Plays the overlay lines and sounds one at a time, without a server.
    internal static class DebugApFeed
    {
        private static int _next;
        private static int _candidate;
        private static int _fake;
        private static int _mode;

        internal static void NextMonumentMode()
        {
            MonumentOrder.DebugFirstClosed = false;
            switch (_mode++ % 4)
            {
                case 0:
                    MonumentOrder.ShuffleDebugOrder();
                    MonumentOrder.DebugMode = "in_order";
                    break;
                case 1:
                    MonumentOrder.DebugMode = "by_item";
                    break;
                case 2:
                    MonumentOrder.DebugMode = "by_item";
                    MonumentOrder.DebugFirstClosed = true;
                    break;
                default:
                    MonumentOrder.DebugMode = null;
                    break;
            }

            ApNotices.Post(MonumentOrder.DebugMode == null ? "Debug: the monuments are all open" : "Debug monuments: " + MonumentOrder.DebugMode + " (order: " + string.Join(", ", System.Array.ConvertAll(MonumentOrder.DebugOrder, prop => prop.Replace("bigKey", string.Empty).Replace("Zone", string.Empty))) + ")" + (MonumentOrder.DebugFirstClosed ? " (none open at the start)" : string.Empty));
        }

        internal static void GrantMonument() => MonumentOrder.Grant();

        internal static void NextObjectives()
        {
            switch (_fake++ % 4)
            {
                case 0:
                    ApObjectives.DebugKeys = new[] { "big_wall", "big_goodbye", "big_collection" };
                    break;
                case 1:
                    ApObjectives.DebugKeys = new[] { "big_goodbye", "big_game" };
                    break;
                case 2:
                    ApObjectives.DebugKeys = new[]
                    {
                        "big_wall", "big_goodbye", "big_game", "big_collection", "big_keys", "big_radio", "big_towers",
                        "big_climb", "big_party",
                    };
                    break;
                default:
                    ApObjectives.DebugKeys = null;
                    break;
            }

            ApNotices.Post(ApObjectives.DebugKeys == null ? "Debug: no objectives" : "Debug objectives: " + string.Join(", ", ApObjectives.DebugKeys));
        }

        private static readonly string[] Candidates =
        {
            "sfx_prop_gourd_place_picker", "sfx_prop_gourd_purple_place_picker", "sfx_prop_gourd_cradle_place_random",
            "sfx_prop_gourdholder_place_random", "sfx_prop_gourd_release_random", "sfx_prop_gourd_cradle_remove_random",
            "sfx_prop_gourdholder_remove_random",
            "sfx_prop_walkieTalkie_transmission_on_random", "sfx_prop_walkieTalkie_transmission_off_random",
            "sfx_radio_broadcast_on_random", "sfx_radio_broadcast_button_on_random",
            "sfx_prop_cowbell_small_clang_random", "sfx_prop_cowbell_med_clang_random", "sfx_prop_cowbell_large_clang_random",
            "sfx_prop_sign_text_write_random", "sfx_prop_sign_text_open_random", "sfx_prop_laptop_keystroke_random",
            "sfx_signalFlag_hoist_up_head_random", "sfx_prop_coordinatesTracker_coordinates_random",
            "sfx_prop_masterSequence_speaker_place_random", "sfx_masterSequence_speaker_on_random",
            "sfx_prop_radio_channel_change_random", "sfx_prop_radio_on_random", "sfx_fireworks_explode_cue",
        };

        internal static void NextCandidate()
        {
            var name = Candidates[_candidate++ % Candidates.Length];
            var played = ApSounds.PlayNamed(name);
            ApNotices.Post($"Sound {(_candidate - 1) % Candidates.Length + 1}/{Candidates.Length}: {name}{(played ? "" : " (not found)")}");
        }

        internal static void Next()
        {
            ApSounds.ForgetBursts();
            switch (_next++ % 10)
            {
                case 0:
                    ApNotices.Post("Received: Gourd");
                    ApSounds.Received();
                    break;
                case 1:
                    ApSounds.CheckSent();
                    ApNotices.Post("(check sound, no line)");
                    break;
                case 2:
                    ApNotices.Post("DeathLink sent: took a big fall");
                    ApSounds.DeathLinkAlert();
                    break;
                case 3:
                    ApNotices.Post("DeathLink from Alice: Big Drop");
                    ApSounds.DeathLinkAlert();
                    break;
                case 4:
                    ApNotices.Post("DeathLink forgiven 1/3: took a big fall");
                    break;
                case 5:
                    ApNotices.Post("Big Trip: nowhere to go yet", warning: true);
                    break;
                case 6:
                    ApNotices.Post("DeathLink is off in the Settings (the seed has it on)", warning: true);
                    break;
                case 7:
                    ApNotices.Post("Drawbridge: opens when its item arrives");
                    break;
                case 8:
                    ApNotices.Post("Objective done: Big Party (on the beach)");
                    ApSounds.Confetti();
                    break;
                default:
                    ApNotices.Post("Received: Big Drop");
                    ApSounds.DeathLinkAlert();
                    break;
            }
        }
    }
}
