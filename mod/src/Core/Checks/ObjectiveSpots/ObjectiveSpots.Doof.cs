using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Checks
{
    internal static partial class ObjectiveSpots
    {
        private const string DoofRoot = "LandmarksNonChallenge/DoofChamber/Positioner";
        private static readonly string[] DoofSwitches =
        {
            DoofRoot + "/FXControls/BasicLightSwitchHouse/LightSwitchPeckLogic",
            DoofRoot + "/FXControls/BasicLightSwitchRing/LightSwitchPeckLogic",
            DoofRoot + "/FXControls/ColouredSpotSwitches/BasicLightSwitchPurple/LightSwitchPeckLogic",
            DoofRoot + "/FXControls/ColouredSpotSwitches/BasicLightSwitchReds/LightSwitchPeckLogic",
            DoofRoot + "/FXControls/ColouredSpotSwitches/BasicLightSwitchWhiteSpots/LightSwitchPeckLogic",
        };
        private const float DoofSeconds = 10f;
        private const float DoofRadius = 25f;
        private static float _doofSince = -1f;
        private static TrackedPeckState[] _doofStates;

        // The smoke machine's push button, held or not, from its two switches' pecks.
        private const string SmokeDown = DoofRoot + "/FXControls/BasicPushButton/PeckSwitchTrigger";
        private const string SmokeUp = SmokeDown + "/UpSwitch";
        private static bool _smokeHeld;

        // Every peck: only the two switches already found are compared, nothing is looked for.
        internal static void OnPeck(PeckSwitch peckSwitch)
        {
            if (peckSwitch == null || !NetworkServer.active)
                return;

            var at = peckSwitch.transform;
            if (Found.TryGetValue(SmokeDown, out var down) && down == at)
                _smokeHeld = true;
            else if (Found.TryGetValue(SmokeUp, out var up) && up == at)
                _smokeHeld = false;
            else
                return;

            Plugin.Log.LogInfo($"{Tag} The Doof Chamber's smoke button is {(_smokeHeld ? "held" : "let go")}.");
        }

        // For the map tracker: where the two rooms are, once loaded.
        internal static Vector3? DoofCentre() => Find(DoofRoot)?.position;
        internal static Vector3? SalonCentre() => Find(SalonRoot)?.position;

        private static void CheckTheDoof()
        {
            var centre = Find(DoofRoot);
            if (centre == null || !FindTheSwitches())
            {
                _doofSince = -1f;
                return;
            }

            var lit = 0;
            foreach (var state in _doofStates)
            {
                if (state != null && state.currentPeckContext != null && state.currentPeckContext.compressedState != 0)
                    lit++;
            }

            // The button's two switches are looked for with the rest: found once the room is.
            Find(SmokeDown);
            if (lit < _doofStates.Length || !_smokeHeld || !Gathered(centre.position, DoofRadius))
            {
                _doofSince = -1f;
                return;
            }

            if (_doofSince < 0f)
            {
                _doofSince = Time.unscaledTime;
                Plugin.Log.LogInfo($"{Tag} The Doof Chamber's {lit} lights are on and its smoke is going: {DoofSeconds} s to go.");
            }

            if (Time.unscaledTime - _doofSince < DoofSeconds)
                return;

            SaveManager.SetIntValue(Latch("big_doof"), 1);
            _doofSince = -1f;
            Plugin.Log.LogInfo($"{Tag} big_doof is done: the Doof Chamber's lights all on and its smoke going for {DoofSeconds} s.");
            ApNotices.Post("Big Doof: the Doof Chamber is lit");
        }

        private static bool FindTheSwitches()
        {
            if (_doofStates != null && Array.TrueForAll(_doofStates, state => state != null))
                return true;

            _doofStates = new TrackedPeckState[DoofSwitches.Length];
            for (var i = 0; i < DoofSwitches.Length; i++)
            {
                var t = Find(DoofSwitches[i]);
                _doofStates[i] = t != null ? t.GetComponent<TrackedPeckState>() : null;
            }

            return Array.TrueForAll(_doofStates, state => state != null);
        }
    }
}
