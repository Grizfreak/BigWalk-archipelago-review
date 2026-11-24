using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Holds the Gauntlet stairways shut until their items arrive (`locked_stages`).
    internal static class GauntletStairways
    {
        private const string KeyPrefix = "ap_gauntlet_";
        private const string GateName = "GourdTowerGateNetworking";
        private const string GatePath = "GourdTower_Gate All Hold";
        private const string WallPath = "GourdTower_Gate Challenge Complete";
        private const string GauntletPath = "SilentGauntlet";

        // The level a system opens is its position here.
        internal static readonly SavableSystem[] Chambers =
        {
            SavableSystem.GauntletChamber0,
            SavableSystem.GauntletChamber1,
            SavableSystem.GauntletChamber2,
            SavableSystem.GauntletChamber3,
            SavableSystem.GauntletChamber4,
            SavableSystem.GauntletChamber5,
            SavableSystem.GauntletChamber6,
        };

        // The wall inside a stage is held too, or it would look like progress that is not.
        private static readonly TrackedPeckState[] GateByLevel = new TrackedPeckState[7];
        private static readonly TrackedPeckState[] WallByLevel = new TrackedPeckState[7];

        private static readonly Dictionary<int, int> LevelByInstance = new();

        private static readonly HashSet<int> NotAGate = new();

        private static bool _puzzlesRequired = true;

        // Finding the gates reads every saved state of the world, so retry only now and then.
        private const float DiscoverIntervalSeconds = 10f;
        private static float _nextDiscoverAt;

        // Not one of PuzzleNeeds' own: no item, ledger or overlay line belongs to it.
        internal const string ButtonsNeed = "gauntlet_stairs";

        private static bool _mirrored;
        private static bool _partsLocked;
        private static bool _partsMirrored;

        // False until slot_data arrives; a guest never holds anything.
        internal static bool Enabled { get; private set; }

        internal static bool ButtonsHidden => Enabled || _mirrored;

        internal static bool PartsHidden => _partsLocked || _partsMirrored;

        internal static void ApplyFromHost(bool lockedStages, bool partsLocked)
        {
            _partsMirrored = partsLocked;

            if (!lockedStages && !_mirrored)
                return;

            _mirrored = lockedStages;
        }

        internal static void ForgetMirror()
        {
            _mirrored = false;
            _partsMirrored = false;
        }

        internal static bool IsChamber(SavableSystem system)
        {
            return Array.IndexOf(Chambers, system) >= 0;
        }

        internal static void Configure(bool lockedStages, bool puzzlesRequired, bool partsLocked)
        {
            Enabled = lockedStages;
            _puzzlesRequired = puzzlesRequired;
            _partsLocked = partsLocked;
            Plugin.Log.LogInfo(
                $"[{nameof(GauntletStairways)}] "
                + (Enabled
                    ? "locked stages: each stairway stays shut until its item arrives"
                      + (_puzzlesRequired ? "; a stage's puzzle opens its wall." : "; a stage's item opens its wall too.")
                    : "vanilla stairways."));

            Enforce();
        }

        // The slot's setting stays: the connection outlives a return to the menu.
        internal static void ForgetGates()
        {
            LevelByInstance.Clear();
            NotAGate.Clear();
            Array.Clear(GateByLevel, 0, GateByLevel.Length);
            Array.Clear(WallByLevel, 0, WallByLevel.Length);
            _nextDiscoverAt = 0f;
        }

        internal static void Forget()
        {
            Enabled = false;
            _partsLocked = false;
            ForgetGates();
        }

        internal static bool IsGranted(int level)
        {
            return SaveManager.GetIntValue(KeyPrefix + level, 0, false) != 0;
        }

        private static bool PuzzleDone(int level)
        {
            return SaveManager.GetIntValue(Chambers[level].ToString(), 0, false) != 0;
        }

        // Runs on every state change; gates are learnt here because walls open during load.
        internal static bool IsHeldShut(TrackedPeckState state, out int level)
        {
            level = -1;
            if ((!Enabled && !ObjectiveBoard.HoldsTheLastStairway) || state == null)
                return false;

            var id = state.GetInstanceID();
            if (!LevelByInstance.TryGetValue(id, out level))
            {
                if (NotAGate.Contains(id))
                    return false;

                if (!TryRegister(state, out level))
                {
                    NotAGate.Add(id);
                    return false;
                }
            }

            return Enabled && !IsGranted(level) || IsHeldByObjectives(level, state);
        }

        // `lock_endings`: the stairway to the bell chamber waits for the other objectives.
        private static bool IsHeldByObjectives(int level, TrackedPeckState state) =>
            level == Chambers.Length - 1 && NetworkServer.active && ObjectiveBoard.HoldsTheLastStairway
            && ObjectiveBoard.Locked("big_goodbye") && state != null && GateByLevel[level] == state;

        internal static int LowestShut()
        {
            for (var level = 0; level < Chambers.Length; level++)
            {
                if (!IsGranted(level))
                    return level;
            }

            return -1;
        }

        internal static bool Grant(int level)
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogWarning($"[{nameof(GauntletStairways)}] Ignored: stage {level + 1} stairway received while not host.");
                return false;
            }

            SaveManager.SetIntValue(KeyPrefix + level, 1);
            Plugin.Log.LogInfo($"[{nameof(GauntletStairways)}] Stage {level + 1} stairway granted.");
            Open(level);
            return true;
        }

        // A save rebound to another seed replays every item, so the old ledger goes.
        internal static void ClearLedger()
        {
            for (var level = 0; level < Chambers.Length; level++)
                SaveManager.SetIntValue(KeyPrefix + level, 0);
        }

        // Cheap enough to run every couple of seconds, which also catches gates that load late.
        internal static void Enforce()
        {
            if (!NetworkServer.active || !WorldManager.isReadyForEffects)
                return;

            if (!Enabled)
            {
                if (ObjectiveBoard.HoldsTheLastStairway && ObjectiveBoard.Locked("big_goodbye"))
                {
                    Discover();
                    ShutGate(GateByLevel[Chambers.Length - 1], "Stage 7 stairway (the objectives are not done)");
                }

                return;
            }

            Discover();
            for (var level = 0; level < Chambers.Length; level++)
            {
                if (IsGranted(level))
                    Open(level);
                else
                    Shut(level);
            }
        }

        // The hold patch always lets a lowering through.
        private static void Shut(int level)
        {
            ShutGate(GateByLevel[level], $"Stage {level + 1} stairway");
            ShutGate(WallByLevel[level], $"Stage {level + 1} wall");
        }

        private static void ShutGate(TrackedPeckState gate, string what)
        {
            if (gate == null || gate.currentPeckContext.state < 1)
                return;

            gate.SetState(0);
            Plugin.Log.LogInfo($"[{nameof(GauntletStairways)}] {what} shut again: its item has not arrived.");
        }

        private static void Open(int level)
        {
            OpenGate(GateByLevel[level], $"Stage {level + 1} stairway");

            // Solved before the item, the wall opens now; solved after, the game opens it.
            if (!_puzzlesRequired || PuzzleDone(level))
                OpenGate(WallByLevel[level], $"Stage {level + 1} wall");
        }

        internal static void DebugOpenLast()
        {
            Discover();
            var gate = GateByLevel[Chambers.Length - 1];
            if (gate == null)
            {
                Plugin.Log.LogInfo($"[{nameof(GauntletStairways)}] Debug: the last stairway is not in this world.");
                return;
            }

            Plugin.Log.LogInfo($"[{nameof(GauntletStairways)}] Debug: asking the last stairway to open (state {gate.currentPeckContext.state}).");
            gate.SetState(1);
            Plugin.Log.LogInfo($"[{nameof(GauntletStairways)}] Debug: the last stairway is now at state {gate.currentPeckContext.state}.");
        }

        private static void OpenGate(TrackedPeckState gate, string what)
        {
            if (gate == null || gate.currentPeckContext.state >= 1)
                return;

            gate.SetState(1);
            Plugin.Log.LogInfo($"[{nameof(GauntletStairways)}] {what} opened.");
        }

        private static void Discover()
        {
            var missing = false;
            foreach (var gate in GateByLevel)
                missing |= gate == null;
            foreach (var wall in WallByLevel)
                missing |= wall == null;

            if (!missing || Time.unscaledTime < _nextDiscoverAt)
                return;

            _nextDiscoverAt = Time.unscaledTime + DiscoverIntervalSeconds;

            var loaded = UnityEngine.Object.FindObjectsByType<TrackedPeckState>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (loaded == null)
                return;

            foreach (var state in loaded)
            {
                try
                {
                    if (state != null)
                        TryRegister(state, out _);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[{nameof(GauntletStairways)}] A gate was skipped: {ex.Message}");
                }
            }
        }

        private static bool TryRegister(TrackedPeckState state, out int level)
        {
            level = -1;
            if (state.gameObject.name != GateName)
                return false;

            var path = PathOf(state.transform);
            if (!path.Contains(GauntletPath) || !TryLevelOf(path, out level))
                return false;

            if (path.Contains(GatePath))
                GateByLevel[level] = state;
            else if (path.Contains(WallPath))
                WallByLevel[level] = state;
            else
                return false;

            LevelByInstance[state.GetInstanceID()] = level;
            return true;
        }

        // The stages have no gourd, so PuzzleFailures names their puzzles by this.
        internal static bool TryStageOf(Transform transform, out int level)
        {
            level = -1;
            if (transform == null)
                return false;

            var path = PathOf(transform);
            return path.Contains(GauntletPath) && TryLevelOf(path, out level);
        }

        internal static bool IsStageSolved(int level) => level >= 0 && level < Chambers.Length && PuzzleDone(level);

        private static bool TryLevelOf(string path, out int level)
        {
            level = -1;
            foreach (var part in path.Split('/'))
            {
                if (part.Length == 6 && part.StartsWith("Level", StringComparison.Ordinal)
                    && int.TryParse(part.Substring(5), out level) && level >= 0 && level < Chambers.Length)
                    return true;
            }

            return false;
        }

        private static string PathOf(Transform transform)
        {
            var parts = new List<string>();
            for (var t = transform; t != null; t = t.parent)
                parts.Add(t.name);
            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
