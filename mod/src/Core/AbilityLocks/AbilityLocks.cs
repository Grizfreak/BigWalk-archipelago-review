using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace BigWalkArchipelago.Core
{
    // Mod side of `lock_abilities`: the host keeps the ledger, every machine blocks its own player.
    internal static class AbilityLocks
    {
        internal const string Jumping = "jumping";
        internal const string Crouching = "crouching";
        internal const string ChairSitting = "chair_sitting";
        internal const string FloorSitting = "floor_sitting";
        internal const string LeftPointing = "left_pointing";
        internal const string LeftRaising = "left_raising";
        internal const string LeftExtending = "left_extending";
        internal const string RightPointing = "right_pointing";
        internal const string RightRaising = "right_raising";
        internal const string RightExtending = "right_extending";
        internal const string Throwing = "throwing";
        internal const string Carrying = "carrying";
        internal const string Running = "running";
        // Not an ability: chair sitting for the chairlift and train, sent so guests know it too.
        internal const string RideSitting = "ride_sitting";

        // Order matches the snapshot's mask bits.
        internal static readonly string[] All =
        {
            Jumping, Crouching, ChairSitting, FloorSitting,
            LeftPointing, LeftRaising, LeftExtending, RightPointing, RightRaising, RightExtending,
            Throwing, Carrying, Running, RideSitting,
        };

        // Legs and Seat grant two abilities; a progressive arm grants raise, point, then extend.
        private static readonly Dictionary<string, string[]> Grants = new Dictionary<string, string[]>
        {
            { "legs", new[] { Jumping, Crouching } },
            { "seat", new[] { ChairSitting, FloorSitting } },
            { "throwing", new[] { Throwing } },
            { "carrying", new[] { Carrying } },
            { "running", new[] { Running } },
        };

        private static readonly Dictionary<string, string[][]> Progressive = new Dictionary<string, string[][]>
        {
            { "left_arm", new[] { new[] { LeftRaising }, new[] { LeftPointing }, new[] { LeftExtending } } },
            { "right_arm", new[] { new[] { RightRaising }, new[] { RightPointing }, new[] { RightExtending } } },
            { "arms", new[] { new[] { LeftRaising, RightRaising }, new[] { LeftPointing, RightPointing }, new[] { LeftExtending, RightExtending } } },
        };

        private const string KeyPrefix = "ap_ability_";

        // Running is left out of the count, as in the apworld.
        private const string ItemsKey = "ap_ability_items";

        internal static int ItemsReceived => SaveManager.GetIntValue(ItemsKey, 0, false);

        private static readonly HashSet<string> Locked = new HashSet<string>();

        private static string[] _keys = Array.Empty<string>();
        private static long _itemIdBase;
        private static bool _mirrored;

        internal static int Version { get; private set; }

        internal static bool Enabled { get; private set; }

        internal static bool Active => Enabled || _mirrored;

        internal static bool IsLocked(string ability) => Locked.Contains(ability);

        // Without `lock_running` the seed holds no Running item.
        private static bool _lockRunning;
        private static bool _ridesRequireSeat;

        internal static void Configure(string mode, bool lockRunning, bool ridesRequireSeat, string[] keysInIdOrder, long itemIdBase)
        {
            Enabled = mode == "handed" || mode == "ambidextrous";
            _lockRunning = lockRunning;
            _ridesRequireSeat = ridesRequireSeat;
            _keys = keysInIdOrder ?? Array.Empty<string>();
            _itemIdBase = itemIdBase;
            Rearm();
            Plugin.Log.LogInfo(
                $"[{nameof(AbilityLocks)}] lock_abilities {mode}; "
                + (Locked.Count == 0 ? "nothing locked." : $"locked until their item arrives: {string.Join(", ", Locked)}."));
        }

        internal static void Rearm()
        {
            Locked.Clear();
            if (Enabled)
            {
                foreach (var ability in All)
                {
                    if (ability != RideSitting && !IsGranted(ability) && (ability != Running || _lockRunning))
                        Locked.Add(ability);
                }

                if (_ridesRequireSeat && Locked.Contains(ChairSitting))
                    Locked.Add(RideSitting);
            }

            Version++;
        }

        internal static bool TryResolveItem(long itemId, out string key)
        {
            key = null;
            var index = itemId - _itemIdBase;
            if (!Enabled || index < 0 || index >= _keys.Length)
                return false;

            key = _keys[index];
            return true;
        }

        internal static bool Grant(string key)
        {
            if (!NetworkServer.active)
            {
                Plugin.Log.LogWarning($"[{nameof(AbilityLocks)}] Ignored: {key} received while not host.");
                return false;
            }

            foreach (var ability in AbilitiesOf(key))
            {
                if (Array.IndexOf(All, ability) < 0)
                {
                    Plugin.Log.LogWarning($"[{nameof(AbilityLocks)}] '{ability}' is an ability this build does not know; ignored.");
                    continue;
                }

                SaveManager.SetIntValue(KeyPrefix + ability, 1);
                Locked.Remove(ability);
            }

            if (!Locked.Contains(ChairSitting))
                Locked.Remove(RideSitting);

            Version++;
            if (key != "running")
                SaveManager.SetIntValue(ItemsKey, ItemsReceived + 1);
            Plugin.Log.LogInfo($"[{nameof(AbilityLocks)}] {key} granted.");
            SessionJournal.Write("ability", $"{key} granted");
            return true;
        }

        // The ledger is in the save, so a reconnect that replays nothing still counts right.
        private static string[] AbilitiesOf(string key)
        {
            if (Grants.TryGetValue(key, out var abilities))
                return abilities;
            if (Progressive.TryGetValue(key, out var tiers))
            {
                foreach (var tier in tiers)
                {
                    if (!Array.TrueForAll(tier, IsGranted))
                        return tier;
                }

                return Array.Empty<string>();
            }

            return new[] { key };
        }

        private static bool IsGranted(string ability) => SaveManager.GetIntValue(KeyPrefix + ability, 0, false) != 0;

        internal static void ClearLedger()
        {
            if (ItemsReceived != 0)
                SaveManager.SetIntValue(ItemsKey, 0);
            foreach (var ability in All)
            {
                if (IsGranted(ability))
                    SaveManager.SetIntValue(KeyPrefix + ability, 0);
            }
        }

        internal static void Forget()
        {
            Enabled = false;
            _keys = Array.Empty<string>();
            _mirrored = false;
            if (Locked.Count == 0)
                return;

            Locked.Clear();
            Version++;
        }

        internal static ushort HostMask()
        {
            ushort mask = 0;
            for (var i = 0; i < All.Length; i++)
            {
                if (Locked.Contains(All[i]))
                    mask |= (ushort)(1 << i);
            }

            return mask;
        }

        internal static void ApplyFromHost(bool enabled, ushort mask)
        {
            if (!enabled && !_mirrored)
                return;

            _mirrored = enabled;
            if (HostMask() == mask)
                return;

            Locked.Clear();
            for (var i = 0; i < All.Length; i++)
            {
                if ((mask & (1 << i)) != 0)
                    Locked.Add(All[i]);
            }

            Version++;
            Plugin.Log.LogInfo(
                $"[{nameof(AbilityLocks)}] The host says locked: {(Locked.Count == 0 ? "nothing" : string.Join(", ", Locked))}.");
        }

        internal static void ForgetMirror()
        {
            if (_mirrored)
                Forget();
        }

        // Poses that drive or carry something are not furniture, or puzzles would lock.
        private static readonly string[] FurnitureWords = { "chair", "couch", "bench", "stump", "seat", "sofa", "stool" };
        private static readonly string[] NeverFurniture = { "chairlift", "trainsystem", "climbaboard", "goodbye", "turnstile", "telescope", "listening" };
        private static readonly string[] AlwaysFurniture = { "carousel" };

        private static readonly string[] RideWords = { "chairlift", "trainsystem" };

        internal static bool IsRide(PlayerPose pose)
        {
            if (pose == null)
                return false;

            var path = PathOf(pose.transform, null);
            return Array.Exists(RideWords, word => path.Contains(word));
        }

        internal static bool IsFurniture(PlayerPose pose)
        {
            if (pose == null)
                return false;

            foreach (var word in AlwaysFurniture)
            {
                if (PathOf(pose.transform, null).Contains(word))
                    return true;
            }

            if (pose.onEnterSwitch != null || pose.onExitSwitch != null || pose.directSystemControl != null)
                return false;

            // A seat that can be picked up is furniture whatever it is called.
            var prop = pose.GetComponentInParent<Prop>(true);
            var path = PathOf(pose.transform, prop != null ? prop.transform : null);
            // A carried seat hangs from the holder's hand, outside its chair.
            var holder = pose.GetComponentInParent<PlayerCharacter>(true);
            var held = holder != null ? HeldBy(holder) : null;
            if (held != null)
            {
                prop = held;
                path = PathOf(held.transform, null);
            }

            foreach (var word in NeverFurniture)
            {
                if (path.Contains(word))
                    return false;
            }

            if (prop != null && prop.GetComponent<PlayerCharacter>() == null)
                return true;

            foreach (var word in FurnitureWords)
            {
                if (path.Contains(word))
                    return true;
            }

            return false;
        }

        internal static string Describe(PlayerPose pose) => pose == null ? "(none)" : PathOf(pose.transform, null);

        // A remote holder only has the synced record.
        private static Prop HeldBy(PlayerCharacter pc)
        {
            if (pc.hands != null && pc.hands.heldProp != null)
                return pc.hands.heldProp;

            try
            {
                var identity = pc.playerNetworking != null ? pc.playerNetworking.playerHeldInformation.identity : null;
                return identity != null ? identity.GetComponent<Prop>() : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string PathOf(Transform transform, Transform top)
        {
            var path = transform.name;
            for (var node = transform; node != top && node.parent != null;)
            {
                node = node.parent;
                path = node.name + "/" + path;
            }

            return path.ToLowerInvariant();
        }
    }

    // Re-applied every second since the game rebuilds its maps; jump and sit stay on while seated.
    internal class AbilityLockEnforcer : MonoBehaviour
    {
        public AbilityLockEnforcer(IntPtr ptr) : base(ptr)
        {
        }

        private static readonly (int Action, string Ability)[] RewiredLocks =
        {
            (RewiredConsts.Action.jump, AbilityLocks.Jumping),
            (RewiredConsts.Action.crouch, AbilityLocks.Crouching),
            (RewiredConsts.Action.sit, AbilityLocks.FloorSitting),
            (RewiredConsts.Action.waveLeft, AbilityLocks.LeftRaising),
            (RewiredConsts.Action.waveRight, AbilityLocks.RightRaising),
            (RewiredConsts.Action.sprint, AbilityLocks.Running),
        };

        private bool _touched;
        private float _next;
        private int _appliedVersion = -1;
        private bool _wasPosing;
        private bool _wasSeated;

        private void Update()
        {
            try
            {
                if (!Rewired.ReInput.isReady)
                    return;

                var character = LocalCharacter();
                var posing = IsPosing(character);
                var seated = IsSeated(character);
                if (_appliedVersion == AbilityLocks.Version && posing == _wasPosing && seated == _wasSeated
                    && Time.unscaledTime < _next)
                    return;

                _next = Time.unscaledTime + 1f;
                _appliedVersion = AbilityLocks.Version;
                _wasPosing = posing;
                _wasSeated = seated;

                if (!AbilityLocks.Active && !_touched)
                    return;

                Apply(posing, seated);
                _touched = AbilityLocks.Active;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[{nameof(AbilityLockEnforcer)}] {ex.Message}");
                _next = Time.unscaledTime + 5f;
            }
        }

        private static void Apply(bool posing, bool seated)
        {
            var player = Rewired.ReInput.players.GetPlayer(0);
            var maps = new Il2CppSystem.Collections.Generic.List<Rewired.ControllerMap>();
            player.controllers.maps.GetAllMaps(maps);
            var elements = new Il2CppSystem.Collections.Generic.List<Rewired.ActionElementMap>();
            foreach (var (action, ability) in RewiredLocks)
            {
                var wayUp = (action == RewiredConsts.Action.jump && (posing || seated))
                            || (action == RewiredConsts.Action.sit && seated);
                var enabled = !AbilityLocks.IsLocked(ability) || wayUp;
                for (var m = 0; m < maps.Count; m++)
                {
                    elements.Clear();
                    maps[m].GetElementMapsWithAction(action, elements);
                    for (var e = 0; e < elements.Count; e++)
                    {
                        if (elements[e].enabled != enabled)
                            elements[e].enabled = enabled;
                    }
                }
            }
        }

        private static PlayerCharacter LocalCharacter()
        {
            var local = Mirror.NetworkClient.localPlayer;
            return local != null ? local.GetComponent<PlayerCharacter>() : null;
        }

        private static bool IsPosing(PlayerCharacter character)
        {
            return character != null && character.poser != null && character.poser.currentPose != null;
        }

        private static bool IsSeated(PlayerCharacter character)
        {
            return character != null && character.sitter != null && character.sitter.isSittingLocal;
        }
    }
}
