using System;
using System.Globalization;
using BigWalkArchipelago.Core.Net;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Checks
{
    internal static partial class ObjectiveSpots
    {
        private static void CheckTheParty()
        {
            PropHome dock = null;
            var homes = PropHome.allPropHomes;
            if (homes != null)
            {
                foreach (var home in homes)
                {
                    if (home != null && home.name == "FmRadioDockPropHome")
                    {
                        dock = home;
                        break;
                    }
                }
            }

            if (dock == null || dock.pinnedProp == null || (EveryoneGathers && !Gathered(dock.transform.position, PartyRadius)))
            {
                _dockedSince = -1f;
                _partyPlaying = false;
                _loggedDocked = false;
                return;
            }

            if (_dockedSince < 0f)
                _dockedSince = Time.unscaledTime;

            // SpeakerStageLogic is not saved, so its state is logged for the tests.
            var parent = dock.transform.parent;
            var logic = parent != null ? parent.Find("SpeakerStageLogic") : null;
            var logicState = logic != null ? logic.GetComponent<TrackedPeckState>() : null;
            var radio = logic != null ? logic.Find("RadioSystem") : null;
            var radioState = radio != null ? radio.GetComponent<TrackedPeckState>() : null;
            var amplitude = SpeakersAmplitude(parent);
            // The music only plays near the speakers, so a far host also reads the radio's dial.
            var playing = amplitude > 0.0005f || TunedOnAGrantedStation(radioState);
            var seen = $"logic {(logicState != null ? logicState.currentPeckContext.state.ToString() : "?")}, "
                       + $"radio system {(radioState != null ? radioState.currentPeckContext.state.ToString() : "?")}, "
                       + $"music {(playing ? "playing" : "silent")}";
            if (!_loggedDocked || seen != _lastSeen)
            {
                _loggedDocked = true;
                _lastSeen = seen;
                Plugin.Log.LogInfo($"{Tag} A radio ({dock.pinnedProp.name}) is in the Speaker Stage's dock: {seen}.");
            }

            // A radio with a locked station, or none, is no party.
            _partyPlaying = playing;
            if (!playing)
            {
                _dockedSince = Time.unscaledTime;
                return;
            }

            if (Time.unscaledTime - _dockedSince < PartySeconds)
                return;

            SaveManager.SetIntValue(Latch("big_party"), 1);
            _dockedSince = -1f;
            _partyPlaying = false;
            Plugin.Log.LogInfo($"{Tag} big_party is done: a radio has been in the Speaker Stage's dock for {PartySeconds} s.");
            ApNotices.Post("Big Party: the party is on");
        }

        private static bool TunedOnAGrantedStation(TrackedPeckState radioSystem)
        {
            if (radioSystem == null || !RadioStations.ItemsInPlay || !int.TryParse(radioSystem.currentPeckContext.state.ToString(), out var state)
                || state < 1)
                return false;

            foreach (var station in RadioStations.All)
            {
                if (RadioStations.LearnedDialPosition(station) == state - 1)
                    return RadioStations.IsGranted(station);
            }

            return false;
        }

        // The loudest of the two speakers' music players (`SpeakerStage_Speaker` and `(1)`).
        private static float SpeakersAmplitude(Transform stage)
        {
            if (stage == null)
                return 0f;

            var loudest = 0f;
            foreach (var name in new[] { "SpeakerStage_Speaker", "SpeakerStage_Speaker (1)" })
            {
                var speaker = stage.Find(name);
                var player = speaker != null ? speaker.Find("MusicPlayer") : null;
                var music = player != null ? player.GetComponent<MusicPlayer>() : null;
                if (music != null)
                    loudest = Mathf.Max(loudest, music.Amplitude);
            }

            return loudest;
        }

        // How near the Speaker Stage's dock everyone stands for the party, with `everyone_gathers`.
        private const float PartyRadius = 30f;
    }
}
