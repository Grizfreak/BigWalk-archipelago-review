using UnityEngine;

namespace BigWalkArchipelago.Core.Abilities
{
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
