using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core.Puzzles
{
    // Hides the objects of every locked puzzle need, found by prefab name; local on every machine.
    internal partial class PuzzleNeedHider : MonoBehaviour
    {
        // Required by Il2CppInterop for any injected type.
        public PuzzleNeedHider(IntPtr ptr) : base(ptr)
        {
        }

        private const float SettleSeconds = 4f;
        private const float RecheckIntervalSeconds = 1f;

        private readonly Dictionary<string, List<GameObject>> _objects = new Dictionary<string, List<GameObject>>();
        private readonly HashSet<int> _hiddenByUs = new HashSet<int>();

        private readonly HashSet<int> _inGauntlet = new HashSet<int>();

        private readonly Dictionary<int, string[]> _skipNeeds = new Dictionary<int, string[]>();

        // Never SetActive(false): Mirror only sends a joining guest the active objects.
        private readonly HashSet<int> _networked = new HashSet<int>();

        private readonly Dictionary<int, SoftState> _soft = new Dictionary<int, SoftState>();

        private const float StatusIntervalSeconds = 60f;

        private static PuzzleNeedHider _current;

        private float _nextStatus;
        private float _readySince = -1f;
        private bool _collected;
        private int _appliedVersion = -1;
        private float _nextRecheck;

        private void Awake()
        {
            _current = this;
        }

        private void Update()
        {
            if (!WorldManager.isReadyForEffects)
            {
                // Needs survive the world: the connection outlives a return to the menu.
                if (_collected)
                {
                    SessionJournal.Write("world", "gone", withPosition: false);
                    HandsJournal.WorldGone();
                }

                _readySince = -1f;
                _collected = false;
                _appliedVersion = -1;
                _objects.Clear();
                _inGauntlet.Clear();
                _skipNeeds.Clear();
                _networked.Clear();
                _soft.Clear();

                // _hiddenByUs is kept: the world can blink out of ready and come back the same.
                return;
            }

            if (_readySince < 0f)
                _readySince = Time.time;

            if (!_collected)
            {
                if (Time.time - _readySince < SettleSeconds)
                    return;

                Collect();
                _collected = true;
                SessionJournal.Write("world", "ready");
                HandsJournal.WorldReady();

                // A save hosted with Archipelago off must be vanilla.
                if (NetworkServer.active)
                {
                    if (ModConfig.ArchipelagoEnabled.Value)
                    {
                        PuzzleNeeds.Rearm();
                        if (AbilityLocks.Enabled)
                            AbilityLocks.Rearm();
                    }
                    else
                    {
                        PuzzleNeeds.Forget();
                        AbilityLocks.Forget();
                    }
                }
            }

            HandsJournal.Tick();

            if (_appliedVersion == PuzzleNeeds.Version && Time.time < _nextRecheck)
                return;

            _nextRecheck = Time.time + RecheckIntervalSeconds;
            Apply();
            _appliedVersion = PuzzleNeeds.Version;

            if (Time.time >= _nextStatus)
            {
                _nextStatus = Time.time + StatusIntervalSeconds;
                LogStatus(detailed: false);
            }
        }
    }
}
