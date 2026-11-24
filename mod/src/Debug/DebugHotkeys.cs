using HouseCulling;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BigWalkArchipelago.Debug
{
    // Only added when the debug tools are on, so nothing polls in normal play.
    internal class DebugHotkeys : MonoBehaviour
    {
        private static int _nextNeed;

        // Required by Il2CppInterop for any injected type.
        public DebugHotkeys(IntPtr ptr) : base(ptr)
        {
        }

        private GameObject _lastCosmeticPickup;
        private float _lastCosmeticPickupCheckTime = -1f;
        private const float CosmeticPickupRecheckDelaySeconds = 3f;

        private void Update()
        {
            DebugNetworkLookup.DumpPrefabsOnce();
            DebugPegTileDump.DumpOnRequest();
            DebugTicketDump.DumpOnRequest();
            DebugTunnelDoors.TickRequests();
            DebugBigLanding.Tick();

            if (_lastCosmeticPickupCheckTime >= 0f && Time.time >= _lastCosmeticPickupCheckTime)
            {
                RecheckCosmeticPickup();
                _lastCosmeticPickupCheckTime = -1f;
            }

            // Plain-letter keys would fire while typing: skip them while a field has focus.
            var focused = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (focused != null && focused.GetComponent<TMP_InputField>() != null)
                return;

            // The loopback guest without the mod only keeps the keys that do not touch the world.
            if (LoopbackGuest.IsVanilla)
            {
                UpdateLoopbackKeys();
                return;
            }

            if (ModConfig.ToggleFlightKey.Value.IsDown())
                DebugFlightTool.Toggle();

            if (ModConfig.UnlockNextKey.Value.IsDown())
                DebugGourdUnlocker.UnlockNext();

            if (ModConfig.SimulateReceivedItemKey.Value.IsDown())
                DebugItemSimulator.SimulateReceiveNext();

            if (ModConfig.SimulateGadgetItemKey.Value.IsDown())
                DebugItemSimulator.SimulateReceiveNextGadget();

            if (ModConfig.DumpHeldItemKey.Value.IsDown())
                DebugHeldItemLookup.LogHeldItem();

            if (ModConfig.DumpCosmeticGadgetsKey.Value.IsDown())
                DebugPropLookup.DumpCosmeticGadgets();

            if (ModConfig.DumpNearbyKey.Value.IsDown())
                DebugGourdLookup.LogNearby(50);

            if (ModConfig.DumpMonumentHomesKey.Value.IsDown())
                DebugGourdLookup.LogMonumentHomes();

            if (ModConfig.DumpNearbyCombinatorKey.Value.IsDown())
            {
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Combinator lookup key pressed; calling LogNearby...");
                DebugPeckCombinatorLookup.DumpNearby(30f);
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] LogNearby returned.");
            }

            if (ModConfig.DumpAllSaveEntriesKey.Value.IsDown())
                DebugSaveDump.DumpAll();

            if (ModConfig.DumpNearbyKeywordKey.Value.IsDown())
                DebugComponentLookup.DumpNearbyByKeyword(30f);

            if (ModConfig.DumpNearbyPeckDevHelperKey.Value.IsDown())
                DebugPeckDevHelper.DumpNearby(30f);

            if (ModConfig.TriggerUnlocksKey.Value.IsDown())
                DebugPeckDevHelper.Trigger(new PeckDevHelper.UnlockRules { unlocks = true });

            if (ModConfig.TriggerGourdKey.Value.IsDown())
                DebugPeckDevHelper.Trigger(new PeckDevHelper.UnlockRules { gourd = true });

            if (ModConfig.DumpSpawnHubGateKey.Value.IsDown())
                DebugTrackedPeckStateLookup.DumpSpawnHubGate();

            if (ModConfig.DumpPeckSwitchTargetKey.Value.IsDown())
                DebugPeckSwitchTarget.DumpNearby(30f);

            if (ModConfig.RevealVariantGourdsKey.Value.IsDown())
                DebugVariantGourdReveal.RevealAll();

            if (ModConfig.FireRemotePeckSwitchKey.Value.IsDown())
                DebugPeckFire.FireByName(ModConfig.RemotePeckSwitchName.Value);

            // IsPressed, not IsDown: this tool needs every held frame and the release.
            DebugTrackedStateHold.Update(
                ModConfig.HoldTrackedStateKey.Value.IsPressed(),
                ModConfig.HoldTrackedStateName.Value,
                ModConfig.HoldTrackedStateValue.Value,
                30f);

            if (ModConfig.DumpIndicatorsKey.Value.IsDown())
                DebugButtonPrototype.DumpIndicators();

            if (ModConfig.MarkSpotKey.Value.IsDown())
                DebugButtonPrototype.MarkAimedSpot();

            if (ModConfig.SpawnTestButtonKey.Value.IsDown())
                DebugButtonPrototype.SpawnInFrontOfPlayer();

            if (ModConfig.EnableSkipAidsKey.Value.IsDown())
                DebugSkipAids.EnableAll();

            if (ModConfig.DumpPegTilesKey.Value.IsDown())
                DebugPegTileDump.Dump();

            if (ModConfig.LogStatesKey.Value.IsDown())
                DebugStateLog.Toggle();

            if (ModConfig.DumpTimersKey.Value.IsDown())
                DebugTimerDump.Dump();

            if (ModConfig.DumpGauntletKey.Value.IsDown())
            {
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Gauntlet dump key pressed; calling Dump...");
                DebugGauntletDump.Dump(ModConfig.DumpGauntletRadius.Value, string.Empty);
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Dump returned.");
            }

            if (ModConfig.ForceNearbyCombinatorKey.Value.IsDown())
            {
                // Logged before the call, to tell a key never seen from a call that died early.
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Combinator force key pressed; calling ForceNearby...");
                DebugPeckCombinatorForce.ForceNearby(120f);
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] ForceNearby returned.");
            }

            if (ModConfig.ForceEndingFlagsKey.Value.IsDown())
                DebugForceEndingFlags.ForceAll();

            if (ModConfig.ApplyBigKeyOverflowKey.Value.IsDown())
                DebugItemSimulator.SimulateReceive(SaveablePropName.bigKeyOverflow);

            if (ModConfig.SpawnCosmeticPickupKey.Value.IsDown())
            {
                _lastCosmeticPickup = ReceivedItemSpawner.SpawnCosmeticPickup(toPlayer: true);
                _lastCosmeticPickupCheckTime = _lastCosmeticPickup != null ? Time.time + CosmeticPickupRecheckDelaySeconds : -1f;
            }

            if (ModConfig.ToggleNeverCullKey.Value.IsDown())
                ToggleNeverCull();

            if (ModConfig.ForceCosmeticPinKey.Value.IsDown())
                DebugCosmeticPinForce.ForceNearby(30f);

            if (ModConfig.DumpHostMenuConfirmKey.Value.IsDown())
                DebugMenuLookup.DumpHostMenuConfirm();

            if (ModConfig.DumpRadioKey.Value.IsDown())
            {
                // Logged before the call, like the combinator key above.
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Radio dump key pressed; calling Dump...");
                DebugRadioLookup.Dump();
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Dump returned.");
            }

            if (ModConfig.DumpKeysKey.Value.IsDown())
            {
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Big key dump key pressed; calling Dump...");
                DebugKeyLookup.Dump();
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Dump returned.");
            }

            if (ModConfig.ForceBigKeyDoorKey.Value.IsDown())
            {
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Big key door key pressed; calling ForceOne...");
                DebugBigKeyDoorForce.ForceOne(ModConfig.BigKeyDoorName.Value);
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] ForceOne returned.");
            }

            if (ModConfig.GrantBigKeyItemKey.Value.IsDown())
            {
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Big key item key pressed; calling GrantOne...");
                DebugBigKeyDoorForce.GrantOne(ModConfig.BigKeyDoorName.Value);
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] GrantOne returned.");
            }

            if (ModConfig.GrantBigKeyFeatureKey.Value.IsDown())
            {
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Big key feature key pressed; calling GrantFeature...");
                DebugBigKeyDoorForce.GrantFeature(ModConfig.BigKeyDoorName.Value);
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] GrantFeature returned.");
            }

            if (ModConfig.DumpPropsKey.Value.IsDown())
            {
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Prop inventory key pressed; calling Dump...");
                DebugPropLookup.Dump();
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Dump returned.");
            }

            if (ModConfig.DumpGourdRosterKey.Value.IsDown())
            {
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Gourd roster key pressed; calling LogAllLoaded...");
                DebugGourdLookup.LogAllLoaded();
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] LogAllLoaded returned.");
            }

            if (ModConfig.PositionPingKey.Value.IsDown())
                DebugPositionPing.Log();

            if (ModConfig.LookAtKey.Value.IsDown())
                DebugLookAt.Log();

            if (ModConfig.DaylightKey.Value.IsDown())
                DebugDaylight.Toggle();

            if (ModConfig.LockButtonsKey.Value.IsDown())
            {
                var lockThem = PuzzleNeeds.All.Any(need => !PuzzleNeeds.IsLocked(need));
                foreach (var need in PuzzleNeeds.All)
                    PuzzleNeeds.SetLocked(need, lockThem);
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Every puzzle need {(lockThem ? "locked" : "unlocked")}.");
                SessionJournal.Write("debug", $"every need {(lockThem ? "locked" : "unlocked")} by hand");
            }

            if (ModConfig.SendDeathLinkKey.Value.IsDown())
                Core.Net.ApRuntime.SendDeathLink();

            if (ModConfig.FireTrapKey.Value.IsDown())
                DebugTraps.FireNext();

            if (ModConfig.TrapStateKey.Value.IsDown())
                DebugTraps.LogState();

            if (ModConfig.MarkTripSpotKey.Value.IsDown())
                DebugTraps.MarkTripSpot();

            if (ModConfig.PickupCensusKey.Value.IsDown())
                DebugPickupCensus.Dump();

            if (ModConfig.ColourProbeKey.Value.IsDown())
                DebugColourProbe.Dump();

            if (ModConfig.AudioProbeKey.Value.IsDown())
                DebugAudioProbe.Dump();

            if (ModConfig.FillMonumentKey.Value.IsDown())
                MonumentOrder.DebugFillNext();

            if (ModConfig.ResetObjectiveSpotsKey.Value.IsDown())
                ObjectiveSpots.ResetLatches();

            if (ModConfig.MarkObjectiveSpotKey.Value.IsDown())
                ObjectiveSpots.Mark();

            if (ModConfig.ApFeedKey.Value.IsDown())
                DebugApFeed.Next();

            if (ModConfig.FakeObjectivesKey.Value.IsDown())
                DebugApFeed.NextObjectives();

            if (ModConfig.LatchGoalFlagsKey.Value.IsDown())
                Core.Net.ApGoalFlags.DebugLatchBoth();

            if (ModConfig.LaserBeamKey.Value.IsDown())
                DebugLaserBeam.Cycle();

            if (ModConfig.OpenLastStairwayKey.Value.IsDown())
                GauntletStairways.DebugOpenLast();

            if (ModConfig.MonumentModeKey.Value.IsDown())
                DebugApFeed.NextMonumentMode();

            if (ModConfig.GrantMonumentKey.Value.IsDown())
                DebugApFeed.GrantMonument();

            if (ModConfig.SoundCandidatesKey.Value.IsDown())
                DebugApFeed.NextCandidate();

            if (ModConfig.AudioNextKey.Value.IsDown())
                DebugAudioProbe.Cycle(1);

            if (ModConfig.AudioPrevKey.Value.IsDown())
                DebugAudioProbe.Cycle(-1);

            if (ModConfig.BuoyTintKey.Value.IsDown())
                DebugBuoyTint.Next();

            if (ModConfig.FlareTintKey.Value.IsDown())
                DebugFlareTint.Next();
            DebugFlareTint.Tick();

            if (ModConfig.MaskTrapKey.Value.IsDown())
                DebugMaskTrap.Next();
            DebugMaskTrap.Tick();

            if (ModConfig.DazeKey.Value.IsDown())
                DebugMaskTrap.Daze();

            if (ModConfig.PickupTeleportKey.Value.IsDown())
                DebugPickupCensus.TeleportNext();

            if (ModConfig.NeedStatusKey.Value.IsDown())
                PuzzleNeedHider.LogStatus(detailed: true);

            if (ModConfig.LockNextNeedKey.Value.IsDown())
            {
                var need = PuzzleNeeds.All[_nextNeed % PuzzleNeeds.All.Length];
                _nextNeed++;
                var lockIt = !PuzzleNeeds.IsLocked(need);
                PuzzleNeeds.SetLocked(need, lockIt);
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Need {need} {(lockIt ? "locked" : "unlocked")}.");
                SessionJournal.Write("debug", $"{need} {(lockIt ? "locked" : "unlocked")} by hand");
            }

            UpdateLoopbackKeys();
        }

        // Shared with the loopback guest without the mod, which runs nothing else from this file.
        private static void UpdateLoopbackKeys()
        {
            if (ModConfig.DumpNetworkKey.Value.IsDown())
            {
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Network dump key pressed; calling Dump...");
                DebugNetworkLookup.Dump();
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Dump returned.");
            }

            // One key, two jobs: the guest joins, anyone else starts a guest.
            if (ModConfig.LoopbackJoinKey.Value.IsDown())
            {
                if (LoopbackGuest.IsGuest)
                {
                    Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Loopback key pressed in the guest; calling Join...");
                    LoopbackGuest.Join();
                    Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Join returned.");
                }
                else
                {
                    Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Loopback key pressed; calling LaunchGuest...");
                    LoopbackLauncher.LaunchGuest(withoutTheMod: false);
                    Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] LaunchGuest returned.");
                }
            }

            if (ModConfig.LoopbackVanillaGuestKey.Value.IsDown() && !LoopbackGuest.IsGuest)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Vanilla guest key pressed; calling LaunchGuest...");
                LoopbackLauncher.LaunchGuest(withoutTheMod: true);
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] LaunchGuest returned.");
            }

            if (ModConfig.LoopbackFocusKey.Value.IsDown())
            {
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Loopback focus key pressed; calling FocusOtherInstance...");
                LoopbackWindows.FocusOtherInstance();
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] FocusOtherInstance returned.");
            }

            if (ModConfig.LoopbackSummonKey.Value.IsDown())
            {
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Loopback summon key pressed; calling SummonGuest...");
                LoopbackGuest.SummonGuest();
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] SummonGuest returned.");
            }
        }

        private void RecheckCosmeticPickup()
        {
            var clone = _lastCosmeticPickup;
            if (clone == null)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] Recheck {CosmeticPickupRecheckDelaySeconds}s after spawn, clone == null (destroyed, or Unity reference invalidated).");
                return;
            }

            var renderer = clone.GetComponentInChildren<Renderer>(true);
            Plugin.Log.LogInfo(
                $"[{nameof(DebugHotkeys)}] Recheck {CosmeticPickupRecheckDelaySeconds}s after spawn, clone still referenced: " +
                $"name={clone.name}, activeSelf={clone.activeSelf}, activeInHierarchy={clone.activeInHierarchy}, " +
                $"position={clone.transform.position}, renderer.enabled={(renderer != null ? renderer.enabled.ToString() : "<no renderer>")}, " +
                $"renderer.isVisible={(renderer != null ? renderer.isVisible.ToString() : "n/a")}.");
        }

        private static void ToggleNeverCull()
        {
            var instance = CullingAgent.Instance;
            if (instance == null)
            {
                Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] CullingAgent.Instance not found.");
                return;
            }

            instance.neverCull = !instance.neverCull;
            Plugin.Log.LogInfo($"[{nameof(DebugHotkeys)}] CullingAgent.neverCull = {instance.neverCull}.");
        }
    }
}
