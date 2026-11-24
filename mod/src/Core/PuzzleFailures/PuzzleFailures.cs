using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Failed puzzles as a DeathLink trigger (`puzzle_failed`); only the host acts on them.
    internal static partial class PuzzleFailures
    {
        private const string Tag = "[" + nameof(PuzzleFailures) + "]";

        // One failure per part per few seconds, none while a new world re-asserts its states.
        private const float Cooldown = 3f;
        private const float WorldSettleSeconds = 8f;
        private const int ScreenFailure = 3;

        // A clamp opens slowly; the check goes once the gourd is loose.
        private const float TomatoGraceSeconds = 15f;

        // A door closing pecks the failure switch and turns the puzzle off, in either order.
        private const int ResetFrames = 3;

        private const string ChamberPrefix = "GauntletChamber";

        private static World _world;
        private static readonly Dictionary<int, float> LastFired = new();

        private static World Current
        {
            get
            {
                if (_world == null && WorldManager.isReadyForEffects)
                    _world = Build();
                return _world;
            }
        }

        // `byNobody` when the context names no player and no action.
        internal static void OnPeck(PeckSwitch peckSwitch, bool byNobody)
        {
            if (peckSwitch == null || !NetworkServer.active)
                return;

            var world = Current;
            if (world == null)
                return;

            var id = peckSwitch.GetInstanceID();
            if (world.SyncSuccess.TryGetValue(id, out var controller))
            {
                if (world.SyncDone.Add(controller))
                    Plugin.Log.LogInfo($"{Tag} Done: {world.SyncControllers[controller]}; its buttons no longer count.");
                return;
            }

            if (!world.FailureSwitches.TryGetValue(id, out var puzzle))
                return;

            if (byNobody && world.ResetBySetOff.Contains(id))
            {
                Plugin.Log.LogInfo($"{Tag} Reset, not failed: {puzzle} went inactive (no player pressed anything).");
                return;
            }

            // Pointer's Paradise: a door closing pecks the switch with the closer's context.
            if (world.ResetBySetOff.Contains(id))
            {
                if (world.OffFrame.TryGetValue(id, out var off) && Time.frameCount - off <= ResetFrames)
                {
                    Plugin.Log.LogInfo($"{Tag} Reset, not failed: {puzzle} had just gone inactive.");
                    return;
                }

                world.PendingFrame[id] = Time.frameCount;
                return;
            }

            Fail(id, puzzle, world);
        }

        internal static void OnState(TrackedPeckState state, int value)
        {
            if (state == null || !NetworkServer.active)
                return;

            var world = Current;
            if (world == null)
                return;

            var id = state.GetInstanceID();
            if (world.ActiveOf.TryGetValue(id, out var failureSwitch))
            {
                if (value == 0)
                    world.OffFrame[failureSwitch] = Time.frameCount;
                if (value == 0 && world.PendingFrame.TryGetValue(failureSwitch, out var frame) && Time.frameCount - frame <= ResetFrames)
                {
                    world.PendingFrame.Remove(failureSwitch);
                    Plugin.Log.LogInfo($"{Tag} Reset, not failed: {world.FailureSwitches[failureSwitch]} was closed by a player.");
                }
                return;
            }
            if (world.Screens.TryGetValue(id, out var puzzle))
            {
                if (value == ScreenFailure)
                    Fail(id, puzzle, world);
                return;
            }

            if (world.Tomatoes.TryGetValue(id, out var tomato))
            {
                var started = world.LastState.TryGetValue(id, out var tomatoWas) && tomatoWas != 0;
                world.LastState[id] = value;
                if (started && value == 0 && !Solved(tomato.Value))
                    world.TomatoesOut[id] = Time.unscaledTime + TomatoGraceSeconds;
                else if (value != 0)
                    world.TomatoesOut.Remove(id);
                return;
            }

            if (!world.SyncButtons.TryGetValue(id, out var controller))
                return;

            var before = world.LastState.TryGetValue(id, out var was) ? was : 0;
            world.LastState[id] = value;
            if (before != 0 && value == 0 && !world.SyncDone.Contains(controller))
                Fail(id, world.SyncControllers[controller] + " (a synchronised button let go)", world);
        }

        // A wrong press waits a few frames for a door that may be the real cause.
        internal static void TickPresses()
        {
            var world = _world;
            if (world == null || world.PendingFrame.Count == 0)
                return;

            foreach (var id in new List<int>(world.PendingFrame.Keys))
            {
                if (Time.frameCount - world.PendingFrame[id] <= ResetFrames)
                    continue;

                world.PendingFrame.Remove(id);
                Fail(id, world.FailureSwitches[id], world);
            }
        }

        // A tomato that ran out fails if still unsolved after its grace.
        internal static void Tick()
        {
            var world = _world;
            if (world == null || world.TomatoesOut.Count == 0 || !NetworkServer.active)
                return;

            foreach (var id in new List<int>(world.TomatoesOut.Keys))
            {
                if (Time.unscaledTime < world.TomatoesOut[id])
                    continue;

                world.TomatoesOut.Remove(id);
                var tomato = world.Tomatoes[id];
                if (Solved(tomato.Value))
                    Plugin.Log.LogInfo($"{Tag} Solved in time: {tomato.Key} (its check went after the tomato ran out).");
                else
                    Fail(id, tomato.Key + " (the timed tomato ran out)", world);
            }
        }

        internal static void ForgetWorld()
        {
            _world = null;
            LastFired.Clear();
        }
    }
}
